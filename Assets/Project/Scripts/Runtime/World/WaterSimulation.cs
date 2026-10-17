using System;
using UnityEngine;

namespace Fabula.Incrementant.World
{
    [Serializable]
    public sealed class WaterSettings
    {
        [Header("Escoamento")]
        [Tooltip("Até quantas células para cada lado a água procura um lugar mais baixo para escorrer. Água sem lugar " +
                 "mais baixo ao alcance fica parada (um degrau de 1 célula numa poça mais larga que isso não se desfaz).")]
        [Range(1, 128)] public int HorizontalReach = 64;
        [Tooltip("Células que a água anda na horizontal por passo, rumo ao lugar mais baixo encontrado.")]
        [Range(1, 8)] public int FlowSpeed = 2;

        [Header("Erosão (dentro do formigueiro)")]
        [Tooltip("Segundos seguidos encostada na água até a terra fixa (natural ou construída) virar terra mole.")]
        [Min(0.1f)] public float ErosionSeconds = 6f;
        [Tooltip("A cada quantos segundos simulados a erosão é verificada.")]
        [Min(0.02f)] public float CheckInterval = 0.25f;
    }

    /// <summary>
    /// Água em passo fixo, no estilo "falling sand": cai reto, senão em diagonal, senão procura na horizontal (até
    /// <see cref="WaterSettings.HorizontalReach"/>) um ponto onde possa cair e anda até ele. Sem esse ponto ela fica
    /// parada, então poças assentam em linhas horizontais e os blocos delas dormem.
    /// Varre de baixo para cima, com a direção alternando por linha e por passo; uma célula que já se moveu no passo
    /// não se move de novo. Só processa blocos com água que foram tocados, ou que têm um bloco tocado na mesma faixa a
    /// até <see cref="WaterSettings.HorizontalReach"/> células (a procura alcança até lá).
    /// A cada <see cref="WaterSettings.CheckInterval"/>: dentro do formigueiro (abaixo do solo original), a terra fixa
    /// encostada na água (4 vizinhos) há <see cref="WaterSettings.ErosionSeconds"/> seguidos vira
    /// <see cref="CellMaterial.Mud"/>, que cai pela física granular, e a água que a molhou é consumida (solo já solto
    /// ou mole só absorve a água). Só ali a água é consumida: no céu (laguinhos, poças da chuva) ela não infiltra nem
    /// evapora, só escorre.
    /// </summary>
    public sealed class WaterSimulation
    {
        static readonly int[] NeighborDx = { 1, -1, 0, 0 };
        static readonly int[] NeighborDy = { 0, 0, 1, -1 };

        readonly WaterSettings _settings;
        // Carimbo do passo em que a célula de destino recebeu água: não anda duas vezes no mesmo passo.
        readonly int[] _moved;
        // Contato com a água por célula de terra: quantas verificações seguidas e a última em que encostou.
        readonly ushort[] _soak;
        readonly int[] _soakCheck;
        int _tick;
        int _check;
        float _checkTimer;

        /// <param name="seed">Sem uso hoje (a água é determinística); mantido para quem cria a simulação com a seed do mapa.</param>
        public WaterSimulation(WaterSettings settings, PixelGrid grid, int seed)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));
            int cells = grid.Width * grid.Height;
            _moved = new int[cells];
            _soak = new ushort[cells];
            _soakCheck = new int[cells];
        }

        /// <summary>Células de terra fixa que a água amoleceu desde o começo do mapa.</summary>
        public int ErodedCells { get; private set; }

        /// <summary>Células de água que o solo absorveu dentro do formigueiro sem amolecer nada.</summary>
        public int AbsorbedCells { get; private set; }

        /// <summary>Repõe os contadores de um save (o tempo de contato da terra com a água não é salvo).</summary>
        public void RestoreCounters(int erodedCells, int absorbedCells = 0)
        {
            ErodedCells = erodedCells;
            AbsorbedCells = absorbedCells;
        }

        /// <summary>Solo já solto ou mole, que dentro do formigueiro só absorve a água (não amolece).</summary>
        public static bool IsPorous(CellMaterial material) =>
            material == CellMaterial.Dirt || material == CellMaterial.Grass || material == CellMaterial.Sand ||
            material == CellMaterial.Spoil || material == CellMaterial.Mud;

        public void Step(PixelGrid grid, float deltaTime)
        {
            if (grid.WaterCount > 0)
                Flow(grid);
            grid.AdvanceWaterWake();

            _checkTimer += deltaTime;
            if (_checkTimer < _settings.CheckInterval)
                return;
            _checkTimer -= _settings.CheckInterval;
            if (grid.WaterCount > 0)
                Check(grid);
        }

        // ---------------------------------------------------------------- escoamento

        void Flow(PixelGrid grid)
        {
            _tick++;
            RectInt area = grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift;
            int cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int chunkReach = (_settings.HorizontalReach + PixelGrid.ChunkSize - 1) >> PixelGrid.ChunkShift;

            for (int y = area.yMin; y < area.yMax; y++)
            {
                int cy = y >> PixelGrid.ChunkShift;
                if (grid.GetChunkRowWaterCount(cy) == 0)
                {
                    y |= PixelGrid.ChunkSize - 1;
                    continue;
                }
                if (((y + _tick) & 1) == 0)
                {
                    for (int cx = cxMin; cx <= cxMax; cx++)
                    {
                        if (!ShouldProcess(grid, cx, cy, cxMin, cxMax, chunkReach))
                            continue;
                        int x1 = Math.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                        for (int x = Math.Max(area.xMin, cx << PixelGrid.ChunkShift); x < x1; x++)
                            StepCell(grid, x, y);
                    }
                }
                else
                {
                    for (int cx = cxMax; cx >= cxMin; cx--)
                    {
                        if (!ShouldProcess(grid, cx, cy, cxMin, cxMax, chunkReach))
                            continue;
                        int x0 = Math.Max(area.xMin, cx << PixelGrid.ChunkShift);
                        for (int x = Math.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift) - 1; x >= x0; x--)
                            StepCell(grid, x, y);
                    }
                }
            }
        }

        static bool ShouldProcess(PixelGrid grid, int cx, int cy, int cxMin, int cxMax, int chunkReach)
        {
            if (grid.GetChunkWaterCount(cx, cy) == 0)
                return false;
            for (int c = Math.Max(cxMin, cx - chunkReach), end = Math.Min(cxMax, cx + chunkReach); c <= end; c++)
            {
                if (grid.IsWaterChunkAwake(c, cy))
                    return true;
            }
            return false;
        }

        void StepCell(PixelGrid grid, int x, int y)
        {
            int w = grid.Width;
            int i = y * w + x;
            if (!grid.HasWaterAt(i) || _moved[i] == _tick)
                return;

            if (grid.IsOpen(x, y - 1))
            {
                Move(grid, i, i - w);
                return;
            }

            int first = ((x ^ y ^ _tick) & 1) == 0 ? -1 : 1;
            if (TryDiagonal(grid, x, y, first) || TryDiagonal(grid, x, y, -first))
                return;
            // Vai para o ponto de queda mais perto; procurar só num lado por vez (alternando) faria a água no meio
            // de dois pontos à mesma distância ir e voltar para sempre.
            int a = FindDrop(grid, x, y, first);
            int b = FindDrop(grid, x, y, -first);
            if (a == 0 && b == 0)
                return;
            int dir = b == 0 || (a != 0 && a <= b) ? first : -first;
            int distance = dir == first ? a : b;
            int step = Math.Min(distance, Math.Max(1, _settings.FlowSpeed));
            Move(grid, i, grid.ToIndex(x + dir * step, y));
        }

        bool TryDiagonal(PixelGrid grid, int x, int y, int dx)
        {
            int nx = x + dx;
            // Lateral livre também, para não atravessar quinas.
            if (!grid.IsOpen(nx, y) || !grid.IsOpen(nx, y - 1))
                return false;
            Move(grid, grid.ToIndex(x, y), grid.ToIndex(nx, y - 1));
            return true;
        }

        /// <summary>
        /// Distância, na direção <paramref name="dx"/> e pelas células livres da mesma linha, até a primeira de onde dá
        /// para cair (0 = nenhuma ao alcance). A água anda até <see cref="WaterSettings.FlowSpeed"/> células rumo a ela.
        /// </summary>
        int FindDrop(PixelGrid grid, int x, int y, int dx)
        {
            int reach = _settings.HorizontalReach;
            for (int k = 1; k <= reach; k++)
            {
                int nx = x + dx * k;
                if (!grid.IsOpen(nx, y))
                    return 0;
                if (grid.IsOpen(nx, y - 1))
                    return k;
            }
            return 0;
        }

        void Move(PixelGrid grid, int from, int to)
        {
            grid.MoveWater(from, to);
            _moved[to] = _tick;
        }

        // ---------------------------------------------------------------- erosão

        void Check(PixelGrid grid)
        {
            _check++;
            int needed = Mathf.Max(1, Mathf.CeilToInt(_settings.ErosionSeconds / _settings.CheckInterval - 0.001f));
            RectInt area = grid.Active;
            int w = grid.Width;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            for (int cy = cyMin; cy <= cyMax; cy++)
            {
                if (grid.GetChunkRowWaterCount(cy) == 0)
                    continue;
                for (int cx = cxMin; cx <= cxMax; cx++)
                {
                    if (grid.GetChunkWaterCount(cx, cy) == 0)
                        continue;
                    int x0 = Math.Max(area.xMin, cx << PixelGrid.ChunkShift);
                    int x1 = Math.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                    int y0 = Math.Max(area.yMin, cy << PixelGrid.ChunkShift);
                    int y1 = Math.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                    for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        // Só a água dentro do formigueiro (abaixo do solo original) molha e é consumida.
                        if (y <= grid.GetSurfaceY(x) && grid.HasWaterAt(y * w + x))
                            Soak(grid, x, y, needed);
                    }
                }
            }
        }

        /// <summary>
        /// Conta mais uma verificação de contato para o solo em volta da água. No limite, a água que o molhou é consumida
        /// (fica no solo), para não formar lagos dentro do formigueiro: terra fixa ainda amolece (vira
        /// <see cref="CellMaterial.Mud"/>); solo já solto ou mole (<see cref="IsPorous"/>) só absorve a água, senão a
        /// terra mole que afunda formaria um piso que segura a poça. Pedra não absorve. Devolve true se consumiu a água.
        /// </summary>
        bool Soak(PixelGrid grid, int x, int y, int needed)
        {
            for (int k = 0; k < 4; k++)
            {
                int nx = x + NeighborDx[k], ny = y + NeighborDy[k];
                CellMaterial material = grid.Get(nx, ny);
                bool erodible = CellMaterials.IsErodible(material);
                if (!erodible && !IsPorous(material))
                    continue;
                int n = ny * grid.Width + nx;
                if (_soakCheck[n] == _check)
                    continue; // outra água já contou esta célula nesta verificação
                // Contato contínuo: se faltou numa verificação, recomeça.
                _soak[n] = _soakCheck[n] == _check - 1 ? (ushort)Math.Min(_soak[n] + 1, ushort.MaxValue) : (ushort)1;
                _soakCheck[n] = _check;
                if (_soak[n] < needed)
                    continue;
                _soak[n] = 0;
                grid.SetWater(x, y, false);
                if (erodible)
                {
                    grid.Set(nx, ny, CellMaterial.Mud, grid.GetShade(nx, ny));
                    ErodedCells++;
                }
                else
                {
                    AbsorbedCells++;
                }
                return true;
            }
            return false;
        }
    }
}
