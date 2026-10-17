using System;
using UnityEngine;

namespace Fabula.Incrementant.World
{
    [Serializable]
    public sealed class TerrainSettings
    {
        [SerializeField, Min(32)] int _width = 320;
        [SerializeField, Min(32)] int _height = 200;
        [Tooltip("Altura média do solo, em fração da altura do mapa.")]
        [SerializeField, Range(0.1f, 0.95f)] float _surfaceLevel = 0.74f;
        [SerializeField, Min(0f)] float _surfaceAmplitude = 5f;
        [Tooltip("Escala do ruído dos bolsões de pedra e areia (menor = bolsões maiores).")]
        [SerializeField, Min(0.001f)] float _pocketScale = 0.045f;
        [SerializeField, Range(0f, 1f)] float _rockThreshold = 0.74f;
        [SerializeField, Range(0f, 1f)] float _sandThreshold = 0.72f;
        [Tooltip("Posição horizontal da entrada do formigueiro, em fração da largura.")]
        [SerializeField, Range(0.05f, 0.95f)] float _entranceX01 = 0.3f;
        [SerializeField, Min(4)] int _shaftDepth = 26;
        [SerializeField] Vector2Int _chamberSize = new Vector2Int(20, 8);
        [Tooltip("Faixa do despejo, em células à direita da entrada (mín, máx).")]
        [SerializeField] Vector2Int _dumpOffset = new Vector2Int(16, 70);
        [Tooltip("Raio em volta do ninho inicial sem areia, para evitar desabamento no começo.")]
        [SerializeField, Min(0)] int _nestSafeRadius = 22;
        [Tooltip("Pilhas de terra escavada (Spoil) dos dois lados da entrada: altura e comprimento da encosta de fora, em células. Altura 0 desliga.")]
        [SerializeField] Vector2Int _entranceMound = new Vector2Int(4, 8);
        [Header("Laguinhos")]
        [Tooltip("Laguinhos na superfície a cada 1000 colunas do mundo. 0 desliga.")]
        [SerializeField, Min(0f)] float _lakesPer1000Columns = 4f;
        [Tooltip("Largura do laguinho, em células (mín, máx).")]
        [SerializeField] Vector2Int _lakeWidth = new Vector2Int(16, 34);
        [Tooltip("Profundidade do laguinho no centro, em células (mín, máx).")]
        [SerializeField] Vector2Int _lakeDepth = new Vector2Int(3, 7);
        [Tooltip("Distância mínima, em células, entre um laguinho e a entrada (somada à faixa de despejo).")]
        [SerializeField, Min(0)] int _lakeClearance = 12;
        [Header("Mundo expansível")]
        [Tooltip("Largura máxima do mundo, em células. O mapa cresce para os lados até aqui; a área inicial (Width) fica no centro.")]
        [SerializeField, Min(32)] int _maxWidth = 960;
        [Tooltip("Altura máxima do mundo, em células. O mapa cresce para baixo até aqui; a área inicial (Height) fica no topo.")]
        [SerializeField, Min(32)] int _maxHeight = 520;
        [Tooltip("O mundo é gerado em blocos deste tamanho (células).")]
        [SerializeField, Range(8, 128)] int _chunkSize = 32;
        [Tooltip("Folga, em células, além da borda da câmera que já fica gerada.")]
        [SerializeField, Min(0)] int _generationMargin = 24;

        public int Width => _width;
        public int Height => _height;
        public float SurfaceLevel => _surfaceLevel;
        public float SurfaceAmplitude => _surfaceAmplitude;
        public float PocketScale => _pocketScale;
        public float RockThreshold => _rockThreshold;
        public float SandThreshold => _sandThreshold;
        public float EntranceX01 => _entranceX01;
        public int ShaftDepth => _shaftDepth;
        public Vector2Int ChamberSize => _chamberSize;
        public Vector2Int DumpOffset => _dumpOffset;
        public int NestSafeRadius => _nestSafeRadius;
        public Vector2Int EntranceMound => _entranceMound;
        public float LakesPer1000Columns => _lakesPer1000Columns;
        public Vector2Int LakeWidth => _lakeWidth;
        public Vector2Int LakeDepth => _lakeDepth;
        public int LakeClearance => _lakeClearance;
        public int WorldWidth => Mathf.Max(_maxWidth, _width);
        public int WorldHeight => Mathf.Max(_maxHeight, _height);
        public int ChunkSize => _chunkSize;
        public int GenerationMargin => _generationMargin;

        /// <summary>Área inicial dentro do mundo: no topo (céu) e centralizada na horizontal.</summary>
        public RectInt StartArea => new RectInt((WorldWidth - _width) / 2, WorldHeight - _height, _width, _height);
    }

    /// <summary>Pontos de referência do mapa gerado, consumidos pela colônia.</summary>
    public readonly struct TerrainLayout
    {
        public readonly int EntranceX;
        public readonly Vector2Int Nest;
        public readonly int DumpMinX;
        public readonly int DumpMaxX;

        public TerrainLayout(int entranceX, Vector2Int nest, int dumpMinX, int dumpMaxX)
        {
            EntranceX = entranceX;
            Nest = nest;
            DumpMinX = dumpMinX;
            DumpMaxX = dumpMaxX;
        }

        /// <summary>
        /// Faixa de despejo espelhada em relação à entrada (as formigas dividem a terra entre os dois lados), cortada
        /// nos limites do mapa. Não existe se sair inteira do mapa ou encostar na faixa original (faixa sobre a entrada).
        /// </summary>
        public bool TryGetMirrorDump(int gridWidth, out int minX, out int maxX)
        {
            minX = Mathf.Max(0, 2 * EntranceX - DumpMaxX);
            maxX = Mathf.Min(gridWidth - 1, 2 * EntranceX - DumpMinX);
            bool overlaps = maxX >= DumpMinX && minX <= DumpMaxX;
            return minX <= maxX && !overlaps;
        }
    }

    /// <summary>
    /// Gera o terreno por região, de forma determinística pela seed e pela posição: qualquer pedaço gerado depois
    /// casa com o vizinho. O mundo tem tamanho fixo (o limite do mapa); a área inicial fica no alto e ao centro, e o
    /// resto é gerado sob demanda (<see cref="Fill"/>) quando a câmera se aproxima.
    /// </summary>
    public sealed class TerrainGenerator
    {
        readonly TerrainSettings _settings;
        readonly float _rockSeed;
        readonly float _sandSeed;
        readonly int[] _surface;
        // Nível da água do laguinho por coluna (-1 = sem laguinho). A água ocupa (_surface, _lakeLevel].
        readonly int[] _lakeLevel;
        readonly int _entranceX;
        readonly int _chamberY;
        readonly RectInt _sandFreeZone;

        public int WorldWidth { get; }
        public int WorldHeight { get; }
        public RectInt StartArea { get; }

        /// <param name="startArea">Área gerada de início; nível do solo, entrada e despejo são relativos a ela.</param>
        public TerrainGenerator(TerrainSettings settings, int seed, int worldWidth, int worldHeight, RectInt startArea)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            WorldWidth = worldWidth;
            WorldHeight = worldHeight;
            StartArea = startArea;

            // Mesma ordem de sorteio de antes: a mesma seed repete o mesmo mapa.
            var rng = new System.Random(seed);
            float surfaceSeed = rng.Next(0, 10000);
            _rockSeed = rng.Next(0, 10000);
            _sandSeed = rng.Next(0, 10000);

            int h = startArea.height;
            int baseSurface = startArea.yMin + Mathf.Clamp(Mathf.RoundToInt(h * settings.SurfaceLevel), 8, h - 4);
            _entranceX = startArea.xMin + Mathf.Clamp(Mathf.RoundToInt(startArea.width * settings.EntranceX01), 4, startArea.width - 5);

            _surface = new int[worldWidth];
            for (int x = 0; x < worldWidth; x++)
            {
                float n = (Mathf.PerlinNoise(surfaceSeed + x * 0.025f, 0.5f) - 0.5f) * 2f
                          + (Mathf.PerlinNoise(surfaceSeed + x * 0.12f, 7.3f) - 0.5f) * 0.5f;
                // Achata o relevo perto da entrada para o ninho começar nivelado.
                float flatten = Mathf.Clamp01(Mathf.Abs(x - _entranceX) / 12f);
                _surface[x] = Mathf.Clamp(baseSurface + Mathf.RoundToInt(n * settings.SurfaceAmplitude * flatten),
                                          startArea.yMin + 4, worldHeight - 2);
            }

            _lakeLevel = new int[worldWidth];
            for (int x = 0; x < worldWidth; x++)
                _lakeLevel[x] = -1;
            CarveLakes(seed);

            int top = _surface[_entranceX];
            _chamberY = Mathf.Max(top - settings.ShaftDepth, startArea.yMin + settings.ChamberSize.y + 2);
            int rx = Mathf.Max(2, settings.ChamberSize.x / 2);
            int ry = Mathf.Max(2, settings.ChamberSize.y / 2);
            int safe = settings.NestSafeRadius;
            // Sem areia em volta do ninho inicial, senão ele desaba nos primeiros segundos.
            int zoneMinY = _chamberY - ry - safe;
            _sandFreeZone = new RectInt(_entranceX - rx - safe, zoneMinY, 2 * (rx + safe) + 1, top - zoneMinY + 1);
        }

        /// <summary>
        /// Sorteia os laguinhos (gerador próprio, para não mudar o resto do mapa da mesma seed) longe da entrada, das
        /// pilhas e da faixa de despejo, e afunda o relevo em bacia. O nível da água é a borda mais baixa, então a água
        /// fica contida; o fundo da bacia vira o solo original da coluna (a água fica "no céu", fora do formigueiro).
        /// </summary>
        void CarveLakes(int seed)
        {
            int count = Mathf.RoundToInt(WorldWidth * 0.001f * _settings.LakesPer1000Columns);
            if (count <= 0)
                return;

            var rng = new System.Random(seed ^ 0x1A4E5);
            int keepOut = Mathf.Max(_settings.DumpOffset.y, 44) + _settings.LakeClearance;
            int minWidth = Mathf.Max(5, Mathf.Min(_settings.LakeWidth.x, _settings.LakeWidth.y));
            int maxWidth = Mathf.Max(minWidth, _settings.LakeWidth.y);
            int minDepth = Mathf.Max(1, Mathf.Min(_settings.LakeDepth.x, _settings.LakeDepth.y));
            int maxDepth = Mathf.Max(minDepth, _settings.LakeDepth.y);
            const int gap = 8;

            for (int placed = 0, attempt = 0; placed < count && attempt < count * 20; attempt++)
            {
                int width = rng.Next(minWidth, maxWidth + 1);
                int depth = rng.Next(minDepth, maxDepth + 1);
                int left = rng.Next(3, Mathf.Max(4, WorldWidth - width - 3));
                int right = left + width - 1; // colunas da bacia; as bordas ficam em left - 1 e right + 1
                if (right + 1 > WorldWidth - 3 || (left - keepOut <= _entranceX && right + keepOut >= _entranceX))
                    continue;
                bool overlaps = false;
                for (int x = left - gap; x <= right + gap && !overlaps; x++)
                    overlaps = x >= 0 && x < WorldWidth && _lakeLevel[x] >= 0;
                if (overlaps)
                    continue;

                int level = Mathf.Min(_surface[left - 1], _surface[right + 1]);
                float half = (width + 1) * 0.5f;
                float center = (left + right) * 0.5f;
                for (int x = left; x <= right; x++)
                {
                    float t = (x - center) / half;
                    int bed = Mathf.Min(_surface[x], level - Mathf.Max(1, Mathf.RoundToInt(depth * (1f - t * t))));
                    _surface[x] = Mathf.Max(bed, StartArea.yMin + 4);
                    _lakeLevel[x] = level;
                }
                placed++;
            }
        }

        /// <summary>Gera a grade inteira de uma vez (mundo = grade). Usado pela ferramenta de Editor e pelos testes.</summary>
        public static TerrainLayout Generate(PixelGrid grid, TerrainSettings settings, int seed)
        {
            var generator = new TerrainGenerator(settings, seed, grid.Width, grid.Height, new RectInt(0, 0, grid.Width, grid.Height));
            return generator.Begin(grid);
        }

        /// <summary>
        /// Grava o relevo de todas as colunas, restringe a área ativa à inicial, gera essa área e cava o ninho.
        /// A grade precisa ter o tamanho do mundo.
        /// </summary>
        public TerrainLayout Begin(PixelGrid grid)
        {
            if (grid.Width != WorldWidth || grid.Height != WorldHeight)
                throw new ArgumentException($"A grade ({grid.Width}x{grid.Height}) precisa ter o tamanho do mundo ({WorldWidth}x{WorldHeight}).", nameof(grid));

            for (int x = 0; x < WorldWidth; x++)
                grid.SetSurfaceY(x, _surface[x]);
            grid.SetActive(StartArea);
            Fill(grid, StartArea);

            Vector2Int nest = CarveStarterNest(grid);
            PileEntranceMounds(grid);
            int dumpMin = Mathf.Clamp(_entranceX + _settings.DumpOffset.x, StartArea.xMin + 2, StartArea.xMax - 3);
            int dumpMax = Mathf.Clamp(_entranceX + _settings.DumpOffset.y, dumpMin, StartArea.xMax - 3);
            return new TerrainLayout(_entranceX, nest, dumpMin, dumpMax);
        }

        /// <summary>
        /// Une <paramref name="target"/> (estendido até o topo do mundo) à área ativa e gera só o que entrou: faixas à
        /// esquerda e à direita (altura toda), embaixo e em cima. Devolve false se nada mudou.
        /// </summary>
        public bool Expand(PixelGrid grid, RectInt target)
        {
            RectInt before = grid.Active;
            target.yMax = WorldHeight;
            if (!grid.ExpandActive(target))
                return false;

            RectInt after = grid.Active;
            Fill(grid, new RectInt(after.xMin, after.yMin, before.xMin - after.xMin, after.height));
            Fill(grid, new RectInt(before.xMax, after.yMin, after.xMax - before.xMax, after.height));
            Fill(grid, new RectInt(before.xMin, after.yMin, before.width, before.yMin - after.yMin));
            Fill(grid, new RectInt(before.xMin, before.yMax, before.width, after.yMax - before.yMax));
            return true;
        }

        /// <summary>Gera as células de <paramref name="area"/>, que já precisa estar dentro da área ativa da grade.</summary>
        public void Fill(PixelGrid grid, RectInt area)
        {
            int xMin = Mathf.Max(0, area.xMin), xMax = Mathf.Min(WorldWidth, area.xMax);
            int yMin = Mathf.Max(0, area.yMin), yMax = Mathf.Min(WorldHeight, area.yMax);
            for (int x = xMin; x < xMax; x++)
            for (int y = yMin; y < yMax; y++)
            {
                CellMaterial material = CellAt(x, y, out byte shade);
                grid.Set(x, y, material, shade);
                if (material == CellMaterial.Empty && y > _surface[x] && y <= _lakeLevel[x])
                    grid.SetWater(x, y, true);
            }
        }

        /// <summary>Nível da água do laguinho na coluna (-1 = sem laguinho).</summary>
        public int GetLakeLevel(int x) => (uint)x < (uint)WorldWidth ? _lakeLevel[x] : -1;

        CellMaterial CellAt(int x, int y, out byte shade)
        {
            // Moldura indestrutível nos limites do mundo, como o vidro de um formigário.
            if (x == 0 || x == WorldWidth - 1 || y == 0)
            {
                shade = 128;
                return CellMaterial.Bedrock;
            }

            int surface = _surface[x];
            if (y > surface)
            {
                shade = 0;
                return CellMaterial.Empty;
            }

            float scale = _settings.PocketScale;
            int depth = surface - y;
            // Fundo de laguinho: areia compactada, que não absorve a água (WaterSimulation.IsPorous).
            CellMaterial material = depth >= 2 ? CellMaterial.Dirt : _lakeLevel[x] >= 0 ? CellMaterial.PackedSand : CellMaterial.Grass;
            if (depth > 6 && Mathf.PerlinNoise(_rockSeed + x * scale, _rockSeed + y * scale * 1.4f) > _settings.RockThreshold)
                material = CellMaterial.Rock;
            else if (depth > 4 && Mathf.PerlinNoise(_sandSeed + x * scale * 1.2f, _sandSeed + y * scale * 1.6f) > _settings.SandThreshold)
                material = _sandFreeZone.Contains(new Vector2Int(x, y)) ? CellMaterial.Dirt : CellMaterial.Sand;

            shade = TerrainMapCodec.ShadeAt(x, y, surface);
            return material;
        }

        Vector2Int CarveStarterNest(PixelGrid grid)
        {
            int top = _surface[_entranceX];
            int rx = Mathf.Max(2, _settings.ChamberSize.x / 2);
            int ry = Mathf.Max(2, _settings.ChamberSize.y / 2);

            for (int y = _chamberY; y <= top; y++)
            for (int x = _entranceX - 1; x <= _entranceX + 1; x++)
                grid.Set(x, y, CellMaterial.Empty, 0);

            for (int y = _chamberY - ry; y <= _chamberY + ry; y++)
            for (int x = _entranceX - rx; x <= _entranceX + rx; x++)
            {
                float dx = (x - _entranceX) / (float)rx;
                float dy = (y - _chamberY) / (float)ry;
                if (dx * dx + dy * dy <= 1f && grid.Get(x, y) != CellMaterial.Bedrock)
                    grid.Set(x, y, CellMaterial.Empty, 0);
            }

            int floorY = _chamberY;
            while (floorY > StartArea.yMin + 1 && !grid.IsSolid(_entranceX, floorY - 1))
                floorY--;
            return new Vector2Int(_entranceX, floorY);
        }

        /// <summary>
        /// Pilhas de <see cref="CellMaterial.Spoil"/> (a terra que saiu do poço) dos dois lados da entrada, acima do
        /// solo original. Encosta de dentro sobe 1 célula por coluna até o pico e a de fora desce em parábola.
        /// </summary>
        /// <remarks>
        /// Spoil é granular: um grão escorrega em diagonal quando a coluna vizinha está 2 ou mais abaixo dele. Por isso
        /// cada coluna fica no máximo 1 acima da vizinha de dentro (a borda ao lado do poço, sem terra, é o primeiro
        /// apoio: nada escorre para o poço) e, para fora, a pilha se espalha até encontrar o relevo.
        /// </remarks>
        void PileEntranceMounds(PixelGrid grid)
        {
            int height = _settings.EntranceMound.x;
            int slope = Mathf.Max(1, _settings.EntranceMound.y);
            if (height <= 0)
                return;

            PileMound(grid, -1, height, slope);
            PileMound(grid, 1, height, slope);
        }

        void PileMound(PixelGrid grid, int direction, int height, int slope)
        {
            const int rimOffset = 2; // poço em ±1, borda livre em ±2
            const int maxColumns = 40;
            int baseTop = _surface[_entranceX];
            int peak = height - 1;
            int previousTop = _surface[_entranceX + direction * rimOffset];
            for (int d = 0; d < maxColumns; d++)
            {
                int x = _entranceX + direction * (rimOffset + 1 + d);
                if (x <= StartArea.xMin || x >= StartArea.xMax - 1)
                    return;

                float t = Mathf.Min(1f, (d - peak) / (float)slope);
                int profile = baseTop + (d < peak ? d + 1 : Mathf.RoundToInt(height * (1f - t * t)));
                int surface = _surface[x];
                // Sobe no máximo 1 em relação à coluna de dentro e desce no máximo 1 (espalha até o relevo).
                int top = Mathf.Max(Mathf.Min(profile, previousTop + 1), previousTop - 1);
                if (top <= surface)
                {
                    if (d > peak)
                        return;
                    top = surface;
                }

                for (int y = surface + 1; y <= top && y < WorldHeight; y++)
                    grid.Set(x, y, CellMaterial.Spoil, TerrainMapCodec.ShadeAt(x, y, surface));
                previousTop = top;
            }
        }
    }
}
