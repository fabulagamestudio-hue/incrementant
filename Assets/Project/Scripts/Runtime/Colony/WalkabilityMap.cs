using System;
using System.Collections.Generic;
using Fabula.Incrementant.World;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Cache de <see cref="AntNavigation.IsWalkable"/> para a grade inteira, usado pelo BFS dos campos de navegação.
    /// <see cref="Refresh"/> recalcula só os blocos cuja versão mudou (<see cref="PixelGrid.GetChunkVersion"/>); depois
    /// dele o cache é igual à regra exata, célula a célula. Compara com o valor antigo e guarda se algo mudou de fato,
    /// separando túnel de céu (<see cref="TakeChanges"/>).
    /// </summary>
    public sealed class WalkabilityMap
    {
        readonly bool[] _walkable;
        readonly int[] _builtVersion;

        public PixelGrid Grid { get; }

        public WalkabilityMap(PixelGrid grid)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _walkable = new bool[grid.Width * grid.Height];
            _builtVersion = new int[grid.ChunkColumns * grid.ChunkRows];
            Array.Fill(_builtVersion, -1);
        }

        public bool IsWalkableAt(int index) => _walkable[index];

        public bool IsWalkable(int x, int y) =>
            (uint)x < (uint)Grid.Width && (uint)y < (uint)Grid.Height && _walkable[y * Grid.Width + x];

        public void Refresh()
        {
            PixelGrid grid = Grid;
            int w = grid.Width;
            for (int cy = 0; cy < grid.ChunkRows; cy++)
            for (int cx = 0; cx < grid.ChunkColumns; cx++)
            {
                int version = grid.GetChunkVersion(cx, cy);
                int c = cy * grid.ChunkColumns + cx;
                if (_builtVersion[c] == version)
                    continue;
                _builtVersion[c] = version;

                int x0 = cx << PixelGrid.ChunkShift;
                int y0 = cy << PixelGrid.ChunkShift;
                int x1 = Math.Min(w, x0 + PixelGrid.ChunkSize);
                int y1 = Math.Min(grid.Height, y0 + PixelGrid.ChunkSize);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    int i = y * w + x;
                    bool walkable = AntNavigation.IsWalkable(grid, x, y);
                    if (_walkable[i] == walkable)
                        continue;
                    _walkable[i] = walkable;
                    if (_changedCells.Count < MaxTrackedChanges)
                        _changedCells.Add(i);
                    else
                        ChangesOverflowed = true;
                    if (y <= grid.GetSurfaceY(x))
                        _changedUnderground = true;
                    else
                        _changedSurface = true;
                }
            }
        }

        bool _changedUnderground;
        bool _changedSurface;

        /// <summary>Acima disto o registro de células alteradas é abandonado (<see cref="ChangesOverflowed"/>): refazer é mais barato.</summary>
        public const int MaxTrackedChanges = 5000;
        readonly List<int> _changedCells = new List<int>();

        /// <summary>
        /// Índices das células que mudaram de caminhável desde <see cref="ClearChangedCells"/> (pode repetir; uma célula que
        /// mudou e voltou também aparece). <see cref="FlowField.Update"/> usa a lista para corrigir só o que a mudança afeta.
        /// </summary>
        public List<int> ChangedCells => _changedCells;

        /// <summary>Mudaram células demais para o registro: quem o usa deve refazer do zero.</summary>
        public bool ChangesOverflowed { get; private set; }

        /// <summary>Esquece as mudanças registradas: os campos já foram acertados com o estado atual.</summary>
        public void ClearChangedCells()
        {
            _changedCells.Clear();
            ChangesOverflowed = false;
        }

        /// <summary>
        /// Devolve (e zera) se alguma célula mudou de caminhável desde a última chamada: abaixo do solo original (túnel) e/ou
        /// acima dele (céu, monte). Grão caindo no céu muda o céu o tempo todo e quase nunca o túnel; quem só depende do
        /// túnel pode ignorar a segunda.
        /// </summary>
        public void TakeChanges(out bool underground, out bool surface)
        {
            underground = _changedUnderground;
            surface = _changedSurface;
            _changedUnderground = false;
            _changedSurface = false;
        }
    }
}
