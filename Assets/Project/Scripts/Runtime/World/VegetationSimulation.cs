using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fabula.Incrementant.World
{
    /// <summary>Forma de uma planta da superfície.</summary>
    public enum PlantKind : byte
    {
        /// <summary>Folha de grama: coluna de <see cref="CellMaterial.PlantGrass"/>.</summary>
        Grass = 0,
        /// <summary>Muda folhosa: caule de <see cref="CellMaterial.PlantLeaf"/> com folhas alternadas dos lados.</summary>
        Leafy = 1,
    }

    [Serializable]
    public sealed class VegetationSettings
    {
        [Tooltip("Largura (em células) de cada faixa da superfície onde nasce no máximo uma planta (a raiz dela). As " +
                 "touceiras de grama têm de 90 a 200 células de largura (arte 6x), então vizinhas se sobrepõem um pouco (a célula " +
                 "comum é de quem cresceu primeiro). Toda a vegetação da superfície vem daqui: o cenário perto não pinta plantas.")]
        [SerializeField, Min(4)] int _slotWidth = 100;
        [Tooltip("Chance de cada faixa ter uma planta.")]
        [SerializeField, Range(0f, 1f)] float _density = 0.85f;
        [Tooltip("Parte das plantas que são grama (o resto são mudas folhosas).")]
        [SerializeField, Range(0f, 1f)] float _grassShare = 0.6f;
        [Tooltip("Altura da grama, em células (mínimo e máximo do sorteio). Só sem arte: com sprites, a forma é a do sprite sorteado.")]
        [SerializeField] Vector2Int _grassHeight = new Vector2Int(8, 18);
        [Tooltip("Altura do caule da muda folhosa, em células (mínimo e máximo do sorteio). Só sem arte: com sprites, a forma é a do sprite sorteado.")]
        [SerializeField] Vector2Int _leafyHeight = new Vector2Int(6, 12);
        [Tooltip("Segundos (tempo simulado) de cada passo de crescimento: a planta inteira sobe 1 linha e a base ganha a " +
                 "linha seguinte da forma (a ponta sai primeiro). 60 = 1 minuto por passo no 1× (pedido do desenvolvedor em 2026-10-17).")]
        [SerializeField, Min(0.1f)] float _growSeconds = 60f;
        [Tooltip("Parte das linhas da forma que cada planta já tem para fora do chão no mapa novo (mínimo e máximo do " +
                 "sorteio). A ponta sai primeiro, então menos que 1 dá a planta inteira, só mais baixa (o resto enterrado).")]
        [SerializeField] Vector2 _initialGrowth = new Vector2(1f, 1f);
        [Tooltip("Células livres de plantas em volta da entrada e das faixas de despejo configuradas.")]
        [SerializeField, Min(0)] int _clearance = 30;
        [Tooltip("Parte da altura da forma que a planta precisa ter crescido (linhas para fora do chão) para as jardineiras " +
                 "a escolherem. 1 = só planta completa. Vale para escolher a planta; uma vez escolhida, a jardineira a colhe até o fim.")]
        [SerializeField, Range(0.05f, 1f)] float _minHarvestGrowth = 1f;

        public int SlotWidth => Mathf.Max(4, _slotWidth);
        public float Density => _density;
        public float GrassShare => _grassShare;
        public Vector2Int GrassHeight => _grassHeight;
        public Vector2Int LeafyHeight => _leafyHeight;
        public float GrowSeconds => _growSeconds;
        public Vector2 InitialGrowth => _initialGrowth;
        public int Clearance => _clearance;
        public float MinHarvestGrowth => _minHarvestGrowth;
    }

    /// <summary>
    /// Células que uma planta ocupa na forma completa, relativas à raiz (dx = coluna a partir da raiz, dy = linha a partir
    /// da linha logo acima do chão), em ordem de crescimento: de baixo para cima e, na mesma linha, do centro para fora.
    /// Com arte, é a silhueta do sprite reduzida à grade (<see cref="FromPixels"/>): cada célula é um bloco de pixels do
    /// desenho, e colher a célula apaga esse bloco.
    /// </summary>
    public sealed class PlantShape
    {
        /// <summary>Células em ordem de crescimento.</summary>
        public readonly Vector2Int[] Cells;
        public readonly int MinDx;
        public readonly int MaxDx;
        /// <summary>Linhas ocupadas (maior dy + 1).</summary>
        public readonly int Height;

        public PlantShape(IEnumerable<Vector2Int> cells)
        {
            var list = new List<Vector2Int>(cells);
            list.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y)
                              : Mathf.Abs(a.x) != Mathf.Abs(b.x) ? Mathf.Abs(a.x).CompareTo(Mathf.Abs(b.x))
                              : a.x.CompareTo(b.x));
            if (list.Count == 0)
                list.Add(Vector2Int.zero);
            Cells = list.ToArray();
            MinDx = int.MaxValue;
            MaxDx = int.MinValue;
            foreach (Vector2Int c in Cells)
            {
                MinDx = Mathf.Min(MinDx, c.x);
                MaxDx = Mathf.Max(MaxDx, c.x);
                Height = Mathf.Max(Height, c.y + 1);
            }
        }

        /// <summary>
        /// Silhueta de um desenho reduzida à grade. O pivô (<paramref name="pivotX"/>, <paramref name="pivotY"/>, em pixels)
        /// fica no centro da coluna da raiz, no topo do chão; a célula (dx, dy) cobre os pixels
        /// [pivotX + (dx − ½)·ppc, +ppc) × [pivotY + dy·ppc, +ppc). Entra a célula com pelo menos
        /// <paramref name="minOpaque"/> pixels opacos; o que fica abaixo do pivô (enterrado) não entra.
        /// </summary>
        public static PlantShape FromPixels(Color32[] pixels, int width, int height, int pivotX, int pivotY,
                                            int pixelsPerCell, int minOpaque = 4)
        {
            int half = pixelsPerCell / 2;
            int minDx = Mathf.FloorToInt((0 - pivotX + half) / (float)pixelsPerCell) - 1;
            int maxDx = Mathf.CeilToInt((width - pivotX + half) / (float)pixelsPerCell) + 1;
            int rows = Mathf.CeilToInt((height - pivotY) / (float)pixelsPerCell);
            var cells = new List<Vector2Int>();
            for (int dy = 0; dy < rows; dy++)
            for (int dx = minDx; dx <= maxDx; dx++)
            {
                int x0 = pivotX + dx * pixelsPerCell - half;
                int y0 = pivotY + dy * pixelsPerCell;
                int opaque = 0;
                for (int py = Mathf.Max(0, y0); py < Mathf.Min(height, y0 + pixelsPerCell); py++)
                for (int px = Mathf.Max(0, x0); px < Mathf.Min(width, x0 + pixelsPerCell); px++)
                {
                    if (pixels[py * width + px].a >= 128)
                        opaque++;
                }
                if (opaque >= minOpaque)
                    cells.Add(new Vector2Int(dx, dy));
            }
            return new PlantShape(cells);
        }

        /// <summary>Forma sem arte: grama = coluna; muda folhosa = caule com folha alternando os lados nas alturas pares a partir de 2.</summary>
        public static PlantShape Procedural(PlantKind kind, int height)
        {
            var cells = new List<Vector2Int>();
            for (int i = 0; i < height; i++)
            {
                cells.Add(new Vector2Int(0, i));
                if (kind == PlantKind.Leafy && i >= 2 && i % 2 == 0)
                    cells.Add(new Vector2Int((i / 2) % 2 == 0 ? 1 : -1, i));
            }
            return new PlantShape(cells);
        }
    }

    /// <summary>
    /// Plantas da superfície: grama e mudas folhosas feitas de pixels da grade (sólidos: as formigas sobem nelas e as
    /// colhem pedaço a pedaço, como a terra). Onde cada planta nasce e qual forma ela tem dependem só da seed e da coluna;
    /// o estado são as células da grade mais as linhas ainda enterradas de cada planta (<see cref="GetSunkRows"/>, que o
    /// save guarda). A planta cresce pela base: a cada <see cref="VegetationSettings.GrowSeconds"/> sobe inteira 1 linha
    /// (com os buracos e as marcas de colheita) e ganha embaixo a linha seguinte da forma, sem marca, então a ponta, que
    /// é onde as formigas colhem, nunca é tapada pelo que cresce (ver <c>Grow</c>). A colheita é das formigas
    /// (<c>AntColony</c>): qualquer pixel acima da raiz (<see cref="CanHarvest"/>), nunca a linha do chão.
    /// </summary>
    public sealed class VegetationSimulation
    {
        /// <summary>De quanto em quanto tempo simulado as plantas são conferidas.</summary>
        const float CheckInterval = 0.25f;

        readonly VegetationSettings _settings;
        readonly int _seed;
        readonly TerrainLayout _layout;
        readonly bool[] _populated;
        // Por coluna da raiz: linhas da forma ainda debaixo da terra (GetSunkRows).
        readonly int[] _sunk;
        readonly List<int> _plants = new List<int>();
        // Formas de arte de cada tipo; vazio = forma procedural (Procedural), guardada por altura.
        readonly PlantShape[] _grassShapes;
        readonly PlantShape[] _leafyShapes;
        readonly Dictionary<int, PlantShape> _proceduralShapes = new Dictionary<int, PlantShape>();
        RectInt _populatedArea;
        float _time;
        float _checkedTime;
        float _checkTimer;

        /// <summary>Pixels de planta que cresceram desde o começo (sem contar o que já nasce no mapa novo).</summary>
        public int GrownCells { get; private set; }

        /// <summary>Plantas na área ativa.</summary>
        public int PlantCount => _plants.Count;

        /// <summary>Colunas da raiz das plantas já nascidas (a linha da raiz é <c>GetSurfaceY(x) + 1</c>).</summary>
        public IReadOnlyList<int> Plants => _plants;

        /// <param name="grassShapes">
        /// Forma de cada variante de arte da grama (a silhueta do sprite, <see cref="PlantShape.FromPixels"/>); a planta
        /// sorteia uma. Null ou vazio = forma procedural com altura de <see cref="VegetationSettings.GrassHeight"/>.
        /// </param>
        /// <param name="leafyShapes">Idem para a muda folhosa.</param>
        public VegetationSimulation(VegetationSettings settings, int seed, TerrainLayout layout, int worldWidth,
                                    PlantShape[] grassShapes = null, PlantShape[] leafyShapes = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _seed = seed;
            _layout = layout;
            _populated = new bool[worldWidth];
            _sunk = new int[worldWidth];
            _grassShapes = grassShapes ?? Array.Empty<PlantShape>();
            _leafyShapes = leafyShapes ?? Array.Empty<PlantShape>();
        }

        // ---------------------------------------------------------------- regras da colheita

        /// <summary>
        /// Pixel de planta na linha logo acima do solo original da coluna. Fica sempre: é dele que a planta volta a
        /// crescer, e uma marca de colheita nele faz o que crescer já nascer marcado.
        /// </summary>
        public static bool IsRoot(PixelGrid grid, int x, int y) =>
            y == grid.GetSurfaceY(x) + 1 && CellMaterials.IsPlant(grid.Get(x, y));

        /// <summary>
        /// Pixel de planta que pode ser cortado agora: qualquer um, menos o da linha do chão da coluna (a raiz, de onde ela
        /// volta a crescer). Cortar no meio solta o pixel e, se a planta se partir, o pedaço sem raiz também cai
        /// (<see cref="DropDisconnected"/>). Antes só a ponta (sem planta em cima) podia ser colhida.
        /// </summary>
        public static bool CanHarvest(PixelGrid grid, int x, int y) =>
            CellMaterials.IsPlant(grid.Get(x, y)) && y != grid.GetSurfaceY(x) + 1;

        /// <summary>
        /// A planta com raiz em <paramref name="rootX"/> serve de alvo para as jardineiras: cresceu pelo menos
        /// <see cref="VegetationSettings.MinHarvestGrowth"/> da altura da forma e ainda tem o que cortar. Devolve em
        /// <paramref name="rows"/> a altura atual (linhas da forma para fora do chão), o tamanho que ordena os alvos.
        /// </summary>
        public bool IsHarvestable(PixelGrid grid, int rootX, out int rows)
        {
            rows = 0;
            if (!TryGetPlant(rootX, out _, out PlantShape shape, out _))
                return false;
            rows = shape.Height - Mathf.Min(GetSunkRows(rootX), shape.Height);
            int needed = Mathf.Max(1, Mathf.CeilToInt(_settings.MinHarvestGrowth * shape.Height - 0.0001f));
            return rows >= needed && TryGetHarvestRow(grid, rootX, out _, out _, out _);
        }

        /// <summary>
        /// Linha mais alta da planta com raiz em <paramref name="rootX"/> que ainda tem pixel cortável
        /// (<see cref="CanHarvest"/>), com as colunas mínima e máxima desses pixels. As jardineiras cortam sempre por ela:
        /// de cima para baixo.
        /// </summary>
        public bool TryGetHarvestRow(PixelGrid grid, int rootX, out int row, out int minX, out int maxX)
        {
            row = minX = maxX = 0;
            if (!TryGetPlant(rootX, out _, out PlantShape shape, out _))
                return false;
            int rootY = grid.GetSurfaceY(rootX) + 1;
            for (int y = Mathf.Min(grid.Height - 1, rootY + shape.Height - 1); y >= rootY; y--)
            {
                bool found = false;
                for (int x = rootX + shape.MinDx; x <= rootX + shape.MaxDx; x++)
                {
                    if (!grid.InBounds(x, y) || !CanHarvest(grid, x, y))
                        continue;
                    if (!found)
                        minX = x;
                    maxX = x;
                    found = true;
                }
                if (found)
                {
                    row = y;
                    return true;
                }
            }
            return false;
        }

        static readonly List<int> ComponentCells = new List<int>();
        // Visitados da busca em curso; e os que alguma busca anterior desta chamada já provou ligados à raiz.
        static readonly HashSet<int> Visited = new HashSet<int>();
        static readonly HashSet<int> KnownRooted = new HashSet<int>();
        static readonly Stack<int> Pending = new Stack<int>();
        // Ordem de visita dos vizinhos (índices de AntNavigation.Dx/Dy): o último a entrar na pilha sai primeiro, então
        // entram primeiro os de cima e por último os de baixo. A busca desce rumo à raiz.
        static readonly int[] DownwardLast = { 2, 4, 6, 0, 1, 3, 5, 7 };
        /// <summary>Acima disto de pixels visitados o conjunto de busca é recriado, para não carregar a capacidade de uma planta enorme.</summary>
        const int VisitedTrimThreshold = 2048;

        /// <summary>
        /// Depois que um pixel saiu de (<paramref name="x"/>, <paramref name="y"/>): todo pedaço de planta ao redor que
        /// perdeu a ligação (8 vizinhos) com a raiz vira pixel solto marcado e cai com a física granular, em vez de
        /// ficar flutuando no ar. Devolve quantos pixels soltou.
        /// A busca de cada vizinho desce rumo à raiz e para ao achá-la (o caso comum: a planta continua presa), então custa
        /// o caminho até o chão, não a planta inteira; só um pedaço solto é varrido por completo, para listar o que cai.
        /// </summary>
        public static int DropDisconnected(PixelGrid grid, int x, int y)
        {
            KnownRooted.Clear();
            int dropped = 0;
            for (int k = 0; k < 8; k++)
            {
                int nx = x + Colony.AntNavigation.Dx[k], ny = y + Colony.AntNavigation.Dy[k];
                if (!CellMaterials.IsPlant(grid.Get(nx, ny)) || KnownRooted.Contains(ny * grid.Width + nx))
                    continue;

                // Busca o pedaço; se tocar uma raiz (ou um pixel que já se sabe ligado a ela), está preso e a busca para ali.
                Visited.Clear();
                ComponentCells.Clear();
                bool rooted = false;
                Pending.Clear();
                Pending.Push(ny * grid.Width + nx);
                Visited.Add(ny * grid.Width + nx);
                while (Pending.Count > 0 && !rooted)
                {
                    int i = Pending.Pop();
                    int cx = i % grid.Width, cy = i / grid.Width;
                    ComponentCells.Add(i);
                    if (cy <= grid.GetSurfaceY(cx) + 1 || KnownRooted.Contains(i))
                    {
                        rooted = true;
                        break;
                    }
                    for (int n = 0; n < 8; n++)
                    {
                        int d = DownwardLast[n];
                        int mx = cx + Colony.AntNavigation.Dx[d], my = cy + Colony.AntNavigation.Dy[d];
                        int j = my * grid.Width + mx;
                        if (CellMaterials.IsPlant(grid.Get(mx, my)) && Visited.Add(j))
                            Pending.Push(j);
                    }
                }
                if (rooted)
                {
                    // Tudo que esta busca alcançou é do mesmo pedaço: as próximas partem sabendo disso.
                    KnownRooted.UnionWith(Visited);
                    continue;
                }

                foreach (int i in ComponentCells)
                {
                    int cx = i % grid.Width, cy = i / grid.Width;
                    CellMaterial debris = CellMaterials.ToDebris(grid.Get(cx, cy));
                    byte shade = grid.GetShade(cx, cy);
                    // Passa por Empty: planta -> material não-planta guardaria a planta atrás da célula.
                    grid.Set(cx, cy, CellMaterial.Empty, 0);
                    grid.Set(cx, cy, debris, shade);
                    grid.SetMarked(cx, cy, true);
                    dropped++;
                }
            }
            if (Visited.Count > VisitedTrimThreshold || KnownRooted.Count > VisitedTrimThreshold)
            {
                Visited.Clear();
                Visited.TrimExcess();
                KnownRooted.Clear();
                KnownRooted.TrimExcess();
            }
            return dropped;
        }

        // ---------------------------------------------------------------- layout

        /// <summary>
        /// Planta com raiz na coluna <paramref name="x"/>, pela seed (sem olhar a grade): cada faixa de
        /// <see cref="VegetationSettings.SlotWidth"/> células tem no máximo uma, e a forma inteira fica longe da entrada e
        /// do despejo.
        /// </summary>
        /// <param name="variant">Variante de arte sorteada (índice nas formas do construtor; 0 sem arte).</param>
        public bool TryGetPlant(int x, out PlantKind kind, out PlantShape shape, out int variant)
        {
            kind = PlantKind.Grass;
            shape = null;
            variant = 0;
            if (x < 0)
                return false;
            int width = _settings.SlotWidth;
            int slot = x / width;
            int plantX = slot * width + (int)(Hash(slot, 1) % (uint)(width - 3));
            if (x != plantX || Hash01(slot, 2) >= _settings.Density)
                return false;

            kind = Hash01(slot, 3) < _settings.GrassShare ? PlantKind.Grass : PlantKind.Leafy;
            PlantShape[] shapes = kind == PlantKind.Grass ? _grassShapes : _leafyShapes;
            if (shapes.Length > 0)
            {
                variant = (int)(Hash(slot, 8) % (uint)shapes.Length);
                shape = shapes[variant];
            }
            else
            {
                Vector2Int range = kind == PlantKind.Grass ? _settings.GrassHeight : _settings.LeafyHeight;
                int min = Mathf.Max(2, Mathf.Min(range.x, range.y));
                int max = Mathf.Max(min, range.y);
                int height = min + (int)(Hash(slot, 4) % (uint)(max - min + 1));
                int key = height * 2 + (int)kind;
                if (!_proceduralShapes.TryGetValue(key, out shape))
                    _proceduralShapes[key] = shape = PlantShape.Procedural(kind, height);
            }

            for (int cx = x + shape.MinDx; cx <= x + shape.MaxDx; cx++)
            {
                // A forma inteira precisa caber no mundo (senão ela nasceria cortada na borda) e longe da entrada e do despejo.
                if (cx < 1 || cx >= _populated.Length - 1 || NearLayout(cx))
                    return false;
            }
            return true;
        }

        bool NearLayout(int x)
        {
            int c = _settings.Clearance;
            if (Mathf.Abs(x - _layout.EntranceX) <= c + 2)
                return true;
            if (x >= _layout.DumpMinX - c && x <= _layout.DumpMaxX + c)
                return true;
            return _layout.TryGetMirrorDump(_populated.Length, out int min, out int max) && x >= min - c && x <= max + c;
        }

        public static CellMaterial MaterialOf(PlantKind kind) =>
            kind == PlantKind.Grass ? CellMaterial.PlantGrass : CellMaterial.PlantLeaf;

        // ---------------------------------------------------------------- crescimento

        /// <summary>
        /// Linhas da forma da planta com raiz em <paramref name="x"/> que ainda estão "debaixo da terra": a linha dy da forma
        /// fica na linha <c>raiz + dy − SunkRows</c> da grade (dy &lt; SunkRows não aparece). 0 = planta toda para fora.
        /// A planta cresce pela base: a ponta é a primeira a sair e cada passo empurra a planta inteira 1 linha para cima.
        /// </summary>
        public int GetSunkRows(int x) => x >= 0 && x < _sunk.Length ? _sunk[x] : 0;

        /// <summary>Restaura <see cref="GetSunkRows"/> (save). Não mexe na grade: as células salvas já estão lá.</summary>
        public void SetSunkRows(int x, int rows)
        {
            if (x >= 0 && x < _sunk.Length)
                _sunk[x] = Mathf.Max(0, rows);
        }

        /// <summary>
        /// Faz nascer as plantas das colunas da área ativa que ainda não foram vistas, com
        /// <see cref="VegetationSettings.InitialGrowth"/> das linhas da forma para fora do chão (sorteio por coluna; a
        /// ponta sai primeiro). Chame depois de gerar terreno novo; barato quando a área ativa não mudou.
        /// </summary>
        public void Populate(PixelGrid grid)
        {
            RectInt area = grid.Active;
            if (area.Equals(_populatedArea))
                return;
            _populatedArea = area;
            for (int x = area.xMin + 1; x < area.xMax - 1; x++)
            {
                if (_populated[x])
                    continue;
                int root = grid.GetSurfaceY(x) + 1;
                if (root < area.yMin || root >= area.yMax)
                    continue; // o solo desta coluna ainda não foi gerado
                bool hasPlant = TryGetPlant(x, out PlantKind kind, out PlantShape shape, out _);
                // Planta que passa da borda da área gerada espera: fora dela as células não existem (IsOpen é falso) e a
                // planta nasceria cortada ali para sempre. Sem marcar a coluna, é tentada de novo quando a área crescer.
                if (hasPlant && !Fits(area, x, shape))
                    continue;
                _populated[x] = true;
                if (!hasPlant)
                    continue;
                _plants.Add(x);
                Vector2 growth = _settings.InitialGrowth;
                float t = Mathf.Lerp(Mathf.Clamp01(growth.x), Mathf.Clamp01(growth.y), Hash01(x, 5));
                int rows = Mathf.Clamp(Mathf.RoundToInt(shape.Height * t), 1, shape.Height);
                Sprout(grid, x, shape, MaterialOf(kind), shape.Height - rows);
            }
        }

        /// <summary>A forma da planta com raiz em <paramref name="x"/> cabe inteira (com folga de 1 coluna) na área gerada.</summary>
        static bool Fits(RectInt area, int x, PlantShape shape) =>
            x + shape.MinDx > area.xMin && x + shape.MaxDx < area.xMax - 1;

        /// <summary>
        /// Dá as colunas de <paramref name="area"/> por vistas sem mexer na grade (ao carregar um save, as plantas
        /// salvas já estão nas células e não podem nascer de novo).
        /// </summary>
        public void MarkPopulated(RectInt area)
        {
            for (int x = Mathf.Max(area.xMin + 1, 0); x < Mathf.Min(area.xMax - 1, _populated.Length); x++)
            {
                if (_populated[x])
                    continue;
                bool hasPlant = TryGetPlant(x, out _, out PlantShape shape, out _);
                if (hasPlant && !Fits(area, x, shape))
                    continue; // não estava no save (não coube na área): nasce quando a área crescer
                _populated[x] = true;
                if (hasPlant)
                    _plants.Add(x);
            }
            _populatedArea = default; // o próximo Populate confere a área ativa de novo
        }

        public void Step(PixelGrid grid, float deltaTime)
        {
            _time += deltaTime;
            _checkTimer -= deltaTime;
            if (_checkTimer > 0f)
                return;
            _checkTimer += CheckInterval;

            float period = _settings.GrowSeconds;
            foreach (int x in _plants)
            {
                // Cada planta tem a própria fase, para não crescerem todas no mesmo instante.
                float phase = Hash01(x, 6) * period;
                int steps = Mathf.FloorToInt((_time + phase) / period) - Mathf.FloorToInt((_checkedTime + phase) / period);
                if (steps <= 0)
                    continue;
                TryGetPlant(x, out PlantKind kind, out PlantShape shape, out _);
                for (int i = 0; i < steps; i++)
                    GrownCells += Grow(grid, x, shape, MaterialOf(kind));
            }
            _checkedTime = _time;
        }

        /// <summary>
        /// Um passo de crescimento. Com linhas ainda enterradas, empurra a planta inteira 1 linha para cima (células,
        /// buracos colhidos e marcas de colheita vão junto) e põe a linha seguinte da forma na linha da raiz, sem marca.
        /// Toda para fora, para de crescer enquanto ainda tiver algum pixel que dê para colher; quando só sobra a linha do
        /// chão (que nunca é colhida), reinicia: volta inteira para debaixo da terra, sem nada para fora, e sobe de
        /// novo nos passos seguintes (pedido do desenvolvedor em 2026-10-17). Precisa de chão firme sob a raiz. Grão,
        /// construção ou pixel solto no caminho não a param: o pixel que cai numa célula ocupada cresce atrás do material
        /// (<see cref="PlaceCell"/>) e reaparece quando ela esvaziar. Devolve quantos pixels a planta ganhou.
        /// </summary>
        int Grow(PixelGrid grid, int x, PlantShape shape, CellMaterial material)
        {
            int rootY = grid.GetSurfaceY(x) + 1;
            if (!grid.IsSolid(x, rootY - 1))
                return 0;

            int sunk = Mathf.Min(_sunk[x], shape.Height);
            Vector2Int[] cells = shape.Cells;
            if (sunk == 0)
            {
                // Ainda há o que colher (algum pixel acima da linha do chão da coluna dele): espera.
                foreach (Vector2Int c in cells)
                {
                    int cx = x + c.x, cy = rootY + c.y;
                    if (cy > grid.GetSurfaceY(cx) + 1 && CellMaterials.IsPlant(grid.Get(cx, cy)))
                        return 0;
                }
                return Sprout(grid, x, shape, material, shape.Height);
            }

            // Sobe: de cima para baixo (Cells está em ordem de crescimento), cada célula presente vai 1 linha para cima.
            // Grão, construção ou pixel solto no caminho não seguram a planta: ela passa por trás deles (GetBehind),
            // como o grão que cai atravessa a planta. Só a borda da área ativa a faz esperar.
            for (int i = cells.Length - 1; i >= 0; i--)
            {
                Vector2Int c = cells[i];
                if (c.y < sunk)
                    break;
                int cx = x + c.x, cy = rootY + c.y - sunk;
                if (!CellMaterials.IsPlant(grid.Get(cx, cy)) && !CellMaterials.IsPlant(grid.GetBehind(cx, cy)))
                    continue;
                if (!grid.InBounds(cx, cy + 1))
                    return 0;
            }
            for (int i = cells.Length - 1; i >= 0; i--)
            {
                Vector2Int c = cells[i];
                if (c.y < sunk)
                    break;
                int cx = x + c.x, cy = rootY + c.y - sunk;
                CellMaterial here = grid.Get(cx, cy);
                if (!CellMaterials.IsPlant(here))
                {
                    // Pedaço coberto por terra ou grão (GetBehind) sobe junto: se ficasse na linha antiga, o sprite o
                    // redesenharia deslocado e ele reapareceria solto no ar quando a cobertura saísse.
                    CellMaterial hidden = grid.GetBehind(cx, cy);
                    if (!CellMaterials.IsPlant(hidden))
                        continue;
                    byte hiddenShade = grid.GetBehindShade(cx, cy);
                    grid.SetBehind(cx, cy, CellMaterial.Empty);
                    PlaceCell(grid, cx, cy + 1, hidden, hiddenShade);
                    continue;
                }
                CellMaterial above = grid.Get(cx, cy + 1);
                if (above == CellMaterial.Empty || CellMaterials.IsPlant(above))
                {
                    bool marked = grid.IsMarked(cx, cy);
                    grid.Set(cx, cy + 1, here, grid.GetShade(cx, cy));
                    grid.SetMarked(cx, cy + 1, marked);
                }
                else
                {
                    // Grão ou construção em cima: o pixel passa para trás dele (sem a marca de colheita, como o pixel que
                    // o grão cobre ao cair).
                    PlaceCell(grid, cx, cy + 1, here, grid.GetShade(cx, cy));
                }
                grid.Set(cx, cy, CellMaterial.Empty, 0);
            }

            sunk--;
            _sunk[x] = sunk;
            // Nasce o que acabou de sair da terra: em cada coluna, a célula da forma que agora cai na primeira linha
            // acima do solo dela. Na coluna da raiz é a linha da raiz; onde o chão é mais alto que a raiz, é a linha logo
            // acima desse chão (abaixo dela a forma continua enterrada, atrás do terreno).
            int grown = 0;
            foreach (Vector2Int c in cells)
            {
                if (c.y < sunk)
                    continue;
                int cx = x + c.x, cy = rootY + c.y - sunk;
                if (cy != Mathf.Max(rootY, grid.GetSurfaceY(cx) + 1))
                    continue;
                if (PlaceCell(grid, cx, cy, material, Shade(cx, c.y)))
                    grown++;
            }
            return grown;
        }

        /// <summary>
        /// Põe um pixel de planta em (<paramref name="cx"/>, <paramref name="cy"/>): à frente se a célula está vazia (com
        /// ou sem água, que ele expulsa); atrás do material se há grão, construção ou pixel solto ali
        /// (<see cref="PixelGrid.SetBehind"/>), para a planta reaparecer quando a célula esvaziar. Célula que já tem
        /// planta (de uma vizinha que se sobrepõe, à frente ou atrás) fica com quem chegou primeiro. Devolve se pôs.
        /// </summary>
        static bool PlaceCell(PixelGrid grid, int cx, int cy, CellMaterial plant, byte shade)
        {
            if (!grid.InBounds(cx, cy))
                return false;
            CellMaterial here = grid.Get(cx, cy);
            if (here == CellMaterial.Empty)
            {
                grid.Set(cx, cy, plant, shade);
                return true;
            }
            if (CellMaterials.IsPlant(here) || CellMaterials.IsPlant(grid.GetBehind(cx, cy)))
                return false;
            return grid.SetBehind(cx, cy, plant, shade);
        }

        /// <summary>
        /// Refaz a planta do zero com <paramref name="sunk"/> linhas enterradas: tira as células dela (e as marcas) e põe
        /// as linhas de cima da forma a partir da raiz, todas sem marca. Devolve o saldo de pixels (nunca negativo).
        /// </summary>
        int Sprout(PixelGrid grid, int x, PlantShape shape, CellMaterial material, int sunk)
        {
            int rootY = grid.GetSurfaceY(x) + 1;
            int oldSunk = Mathf.Min(_sunk[x], shape.Height);
            int removed = 0;
            foreach (Vector2Int c in shape.Cells)
            {
                if (c.y < oldSunk)
                    continue;
                int cx = x + c.x, cy = rootY + c.y - oldSunk;
                if (!CellMaterials.IsPlant(grid.Get(cx, cy)))
                {
                    // Pedaço coberto por terra (atrás dela): some junto, para não reaparecer fora do lugar ao cavar.
                    grid.SetBehind(cx, cy, CellMaterial.Empty);
                    continue;
                }
                grid.Set(cx, cy, CellMaterial.Empty, 0);
                removed++;
            }

            ClearStrayCells(grid, x, shape, rootY);

            _sunk[x] = sunk;
            int placed = 0;
            foreach (Vector2Int c in shape.Cells)
            {
                if (c.y < sunk)
                    continue;
                int cx = x + c.x, cy = rootY + c.y - sunk;
                if (cy <= grid.GetSurfaceY(cx))
                    continue;
                if (PlaceCell(grid, cx, cy, material, Shade(cx, c.y)))
                    placed++;
            }
            return Mathf.Max(0, placed - removed);
        }

        /// <summary>
        /// Tira o que sobrou de planta (à frente ou coberta por terra/grão) dentro da caixa da planta com raiz em
        /// <paramref name="x"/>, fora das colunas de plantas vizinhas. Com a planta toda reiniciada nada ali pertence a
        /// ela: restos de versões antigas do crescimento reapareceriam soltos no ar quando a cobertura saísse.
        /// </summary>
        void ClearStrayCells(PixelGrid grid, int x, PlantShape shape, int rootY)
        {
            for (int cx = x + shape.MinDx; cx <= x + shape.MaxDx; cx++)
            {
                if (OverlapsOtherPlant(x, cx))
                    continue;
                for (int cy = rootY; cy < rootY + shape.Height + 2; cy++)
                {
                    if (CellMaterials.IsPlant(grid.Get(cx, cy)))
                        grid.Set(cx, cy, CellMaterial.Empty, 0);
                    else if (CellMaterials.IsPlant(grid.GetBehind(cx, cy)))
                        grid.SetBehind(cx, cy, CellMaterial.Empty);
                }
            }
        }

        /// <summary>A coluna <paramref name="cx"/> também é coberta pela forma de outra planta que não a de <paramref name="x"/>.</summary>
        bool OverlapsOtherPlant(int x, int cx)
        {
            foreach (int other in _plants)
            {
                if (other == x || !TryGetPlant(other, out _, out PlantShape s, out _))
                    continue;
                if (cx >= other + s.MinDx && cx <= other + s.MaxDx)
                    return true;
            }
            return false;
        }

        /// <summary>Tom fixo de cada célula da forma (acompanha a célula quando a planta sobe).</summary>
        byte Shade(int cx, int dy) => (byte)(104 + Hash(cx * 31 + dy, 7) % 48);

        // ---------------------------------------------------------------- sorteio por posição

        uint Hash(int value, int salt)
        {
            unchecked
            {
                uint h = (uint)_seed * 0x9E3779B1u ^ (uint)value * 0x85EBCA77u ^ (uint)salt * 0xC2B2AE3Du;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                h *= 0x297A2D39u;
                h ^= h >> 15;
                return h;
            }
        }

        float Hash01(int value, int salt) => (Hash(value, salt) & 0xFFFFFF) / (float)0x1000000;
    }
}
