using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>Modo do pincel, para a cor do contorno desenhado na textura.</summary>
    public enum BrushTint
    {
        Dig,
        Build,
        QueenRoom,
        NutrientRoom,
        NurseryRoom,
        WasteRoom,
        /// <summary>O dedo não pinta: o contorno é só a mira.</summary>
        Finger,
        DewRoom,
        FoodStore,
        BlockStore,
        WaterStore,
    }

    /// <summary>
    /// Desenha grade, formigas e pincel numa textura (1 célula = 1 texel, filtro Point) que cobre só uma janela da
    /// grade em volta da vista da câmera, para o custo não crescer com o mundo. A janela é exibida por um
    /// SpriteRenderer filho (criado no Play) com 1 unidade de mundo por célula; o SpriteRenderer deste objeto
    /// fica só para o preview do mapa no Editor. A posição deste objeto é a origem do mapa (célula 0,0).
    /// Com <b>Tile Material</b> (shader <c>Incrementant/TerrainTiles</c>), cada célula aparece como um bloco de
    /// 16x16 texels com a textura do material (<see cref="CellTileArt"/>); a cor por célula continua vindo daqui.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AntFarmView : MonoBehaviour
    {
        [SerializeField] Color32 _skyTop = new Color32(120, 170, 215, 255);
        [SerializeField] Color32 _skyBottom = new Color32(190, 220, 235, 255);
        [SerializeField] Color32 _tunnelColor = new Color32(54, 37, 27, 255);
        [SerializeField] Color32 _markColor = new Color32(255, 196, 40, 255);
        [SerializeField, Range(0f, 1f)] float _markStrength = 0.45f;
        [Tooltip("Cor das marcas de construção (células vazias que as formigas vão preencher) e do pincel no modo construir.")]
        [SerializeField] Color32 _buildMarkColor = new Color32(70, 190, 255, 255);
        [Tooltip("Cor da sala da rainha pintada e do pincel no modo sala da rainha.")]
        [SerializeField] Color32 _queenRoomColor = new Color32(214, 96, 230, 255);
        [Tooltip("Força da tinta na sala em que a rainha fica; salas menores (ignoradas) e células ainda maciças ficam com metade.")]
        [SerializeField, Range(0f, 1f)] float _queenRoomStrength = 0.4f;
        [Tooltip("Cor da sala de nutrientes pintada (slots livres) e do pincel no modo sala de nutrientes.")]
        [SerializeField] Color32 _nutrientRoomColor = new Color32(120, 200, 90, 255);
        [SerializeField] Color32 _nurseryRoomColor = new Color32(240, 160, 190, 255);
        [Tooltip("Cor da área de resíduos pintada e do pincel no modo área de resíduos.")]
        [SerializeField] Color32 _wasteRoomColor = new Color32(170, 150, 90, 255);
        [Tooltip("Cor da sala Gotas de Orvalho pintada e do pincel no modo sala Gotas de Orvalho.")]
        [SerializeField] Color32 _dewRoomColor = new Color32(90, 190, 230, 255);
        [Tooltip("Cor do depósito de alimentos pintado e do pincel no modo depósito de alimentos.")]
        [SerializeField] Color32 _foodStoreColor = new Color32(150, 210, 60, 255);
        [Tooltip("Cor do depósito de construção pintado e do pincel no modo depósito de construção.")]
        [SerializeField] Color32 _blockStoreColor = new Color32(220, 140, 70, 255);
        [Tooltip("Cor do depósito de água pintado e do pincel no modo depósito de água.")]
        [SerializeField] Color32 _waterStoreColor = new Color32(70, 120, 235, 255);
        [Tooltip("Quanto o item guardado (depósitos e resíduos) mistura a cor dele com o fundo do túnel: 1 = a cor do material.")]
        [SerializeField, Range(0f, 1f)] float _storedStrength = 0.8f;
        [Tooltip("Força da tinta da sala de nutrientes; células ainda maciças ficam com metade.")]
        [SerializeField, Range(0f, 1f)] float _nutrientRoomStrength = 0.35f;
        [Tooltip("Cor do nutriente pronto que brotou de um canteiro (e carregado até a rainha, no fallback sem sprites).")]
        [SerializeField] Color32 _nutrientColor = new Color32(232, 214, 92, 255);
        [SerializeField] Color32 _antBody = new Color32(20, 12, 10, 255);
        [SerializeField] Color32 _antHead = new Color32(48, 26, 18, 255);
        [SerializeField] Color32 _brushColor = new Color32(255, 255, 255, 255);
        [SerializeField] Color32 _dumpMarkerColor = new Color32(255, 255, 255, 255);
        [Header("Névoa")]
        [Tooltip("Ligada, só a terra até Reveal Radius células de uma célula aberta (túnel ou céu) aparece; o resto é preto.")]
        [SerializeField] bool _fogOfWar = true;
        [Tooltip("Quantas células ao lado de uma célula aberta continuam visíveis, escurecendo até o preto.")]
        [SerializeField, Range(1, 8)] int _revealRadius = 5;
        [Header("Chuva")]
        [SerializeField] Color32 _lightDropColor = new Color32(225, 238, 252, 255);
        [SerializeField] Color32 _heavyDropColor = new Color32(205, 225, 250, 255);
        [SerializeField, Range(0f, 1f)] float _dropOpacity = 0.85f;
        [Tooltip("Comprimento do risco da gota, em múltiplos da queda por passo.")]
        [SerializeField, Range(1f, 4f)] float _dropStreak = 2f;
        [Tooltip("Quanto cada nível de umidade escurece a célula (terra molhada pela chuva leve).")]
        [SerializeField, Range(0f, 0.3f)] float _wetDarkening = 0.1f;
        [Header("Água")]
        [SerializeField] Color32 _waterColor = new Color32(44, 104, 196, 255);
        [Tooltip("Cor da linha de cima da água (superfície da poça).")]
        [SerializeField] Color32 _waterSurfaceColor = new Color32(120, 180, 235, 255);
        [SerializeField, Range(0f, 1f)] float _waterOpacity = 0.7f;
        [Header("Tiles 16x16")]
        [Tooltip("Material com o shader Incrementant/TerrainTiles. Vazio: 1 célula = 1 texel liso, como antes.")]
        [SerializeField] Material _tileMaterial;
        [Tooltip("Atlas desenhado (64 x 64 por material, empilhados na vertical na ordem de CellMaterial, cinza 128 = sem mudança). Vazio: gerado por código (CellTileArt).")]
        [SerializeField] Texture2D _tileAtlas;
        [SerializeField, Range(0f, 2f)] float _tileDetail = 1f;
        [Tooltip("Escurecimento do contorno de 1 texel do lado sólido, onde a célula encosta no vazio.")]
        [SerializeField, Range(0f, 1f)] float _tileEdgeDarkening = 0.5f;

        /// <summary>A janela cresce em passos deste tamanho, para não recriar a textura a cada mudança de zoom.</summary>
        const int WindowStep = 16;

        SpriteRenderer _windowRenderer;
        Texture2D _texture;
        Sprite _sprite;
        Color32[] _pixels;
        // Tiles: material de cada célula da janela (CellTileArt.SkyId = céu) e o material instanciado do shader.
        Texture2D _materialTexture;
        byte[] _materialIds;
        Material _tileInstance;
        Texture2D _generatedAtlas;
        static readonly int WindowOriginId = Shader.PropertyToID("_WindowOrigin");
        static readonly int CellMaterialsId = Shader.PropertyToID("_CellMaterials");
        Color32[] _skyRows;
        int _width;
        int _height;
        int _originX;
        int _originY;
        bool _plantsAsSprites;
        // Névoa: célula aberta (sem material, ou acima do solo original) e a distância horizontal até uma, por linha.
        bool[] _open;
        byte[] _fogRows;
        // Tabela de área somada de _open (uma linha e uma coluna a mais, de zeros): ver FogDistance.
        int[] _openSum;
        // Cache de cor (e de material, com tiles) de cada célula da grade inteira. Só os blocos cuja versão visual mudou
        // (PixelGrid.GetChunkRenderVersion, ou um vizinho por causa da névoa) são redesenhados; a janela é cópia do cache.
        PixelGrid _cachedGrid;
        bool _cachedHasColony;
        Color32[] _cellColors;
        byte[] _cellTiles;
        // Distância da névoa de cada célula no último desenho do bloco dela (só muda com a forma do terreno).
        byte[] _cellFog;
        int[] _drawnVersion;
        long _renderLogSeen;

        /// <summary>Blocos redesenhados no último <see cref="Render"/> e blocos da janela (diagnóstico do painel F11).</summary>
        public int LastRedrawnChunks { get; private set; }
        public int LastWindowChunks { get; private set; }
        public int LastRedrawnCells { get; private set; }
        public Vector2Int LastWindowSize => new Vector2Int(_width, _height);

        /// <param name="transparentSky">Deixa o céu transparente, para um cenário desenhado atrás (SurfaceSceneryView) aparecer.</param>
        /// <param name="skyBaseY">Linha onde o gradiente do céu começa (cor de baixo); o topo da grade tem a cor de cima.</param>
        /// <param name="plantsAsSprites">As plantas colhíveis são desenhadas à parte, com sprites: as células delas ficam como céu.</param>
        public void Initialize(PixelGrid grid, bool transparentSky = false, int skyBaseY = 0, bool plantsAsSprites = false)
        {
            _plantsAsSprites = plantsAsSprites;
            if (_windowRenderer == null)
            {
                var own = GetComponent<SpriteRenderer>();
                var go = new GameObject("TerrainWindow") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                _windowRenderer = go.AddComponent<SpriteRenderer>();
                _windowRenderer.sortingLayerID = own.sortingLayerID;
                _windowRenderer.sortingOrder = own.sortingOrder;
                _windowRenderer.sharedMaterial = CreateTileMaterial() ?? own.sharedMaterial;
                // O sprite próprio é só o preview do Editor (PNG do modo Image); no Play quem desenha é a janela.
                own.enabled = false;
            }

            int top = grid.Height - 1;
            _skyRows = new Color32[grid.Height];
            for (int y = 0; y < grid.Height; y++)
                _skyRows[y] = transparentSky
                    ? new Color32(0, 0, 0, 0)
                    : Color32.Lerp(_skyBottom, _skyTop, Mathf.Clamp01((y - skyBaseY) / (float)Mathf.Max(1, top - skyBaseY)));
        }

        /// <summary>Tiles ligados: instância do material com o atlas e os parâmetros; null sem Tile Material.</summary>
        Material CreateTileMaterial()
        {
            if (_tileMaterial == null)
                return null;
            Texture atlas = _tileAtlas;
            if (atlas == null)
                atlas = _generatedAtlas = CellTileArt.CreateAtlas();
            _tileInstance = new Material(_tileMaterial) { name = "TerrainTiles (instância)", hideFlags = HideFlags.DontSave };
            _tileInstance.SetTexture("_TileAtlas", atlas);
            _tileInstance.SetFloat("_TileCount", CellTileArt.TileCount);
            _tileInstance.SetFloat("_TileSize", CellTileArt.TileSize);
            _tileInstance.SetFloat("_TexelsPerCell", CellTileArt.TexelsPerCell);
            _tileInstance.SetFloat("_DetailStrength", _tileDetail);
            _tileInstance.SetFloat("_EdgeDarkening", _tileEdgeDarkening);
            return _tileInstance;
        }

        /// <summary>Recorte da grade (em células) que cobre <paramref name="view"/> (retângulo local ao mapa) com folga.</summary>
        public static RectInt WindowFor(Rect view, PixelGrid grid)
        {
            int width = Mathf.Min(grid.Width, RoundUp(Mathf.CeilToInt(view.width) + 2, WindowStep));
            int height = Mathf.Min(grid.Height, RoundUp(Mathf.CeilToInt(view.height) + 2, WindowStep));
            int x = Mathf.Clamp(Mathf.FloorToInt(view.center.x - width * 0.5f), 0, grid.Width - width);
            int y = Mathf.Clamp(Mathf.FloorToInt(view.center.y - height * 0.5f), 0, grid.Height - height);
            return new RectInt(x, y, width, height);
        }

        static int RoundUp(int value, int step) => (value + step - 1) / step * step;

        /// <param name="window">Recorte da grade a desenhar (ver <see cref="WindowFor"/>).</param>
        /// <param name="drawAnts">Desenha as formigas na textura (fallback de 3 pixels quando não há AntSwarmView).</param>
        /// <param name="drops">Gotas de chuva em queda (opcional).</param>
        /// <param name="brush">Modo do pincel, só para a cor do contorno.</param>
        /// <param name="colony">Colônia, para destacar a sala em que a rainha fica e desenhar a rainha no fallback (opcional).</param>
        public void Render(PixelGrid grid, IReadOnlyList<Ant> ants, IReadOnlyList<Vector2Int> dumpStrips, Vector2Int brushCell, int brushRadius,
                           bool showBrush, bool drawAnts, RectInt window, IReadOnlyList<RainDrop> drops = null,
                           BrushTint brush = BrushTint.Dig, AntColony colony = null)
        {
            if (_windowRenderer == null || _skyRows.Length != grid.Height)
                Initialize(grid);
            if (_texture == null || window.width != _width || window.height != _height)
                CreateTexture(window.width, window.height);
            _originX = window.x;
            _originY = window.y;
            _windowRenderer.transform.localPosition = new Vector3(_originX, _originY, 0f);
            if (_tileInstance != null)
                _tileInstance.SetVector(WindowOriginId, new Vector4(_originX, _originY, _width, _height));

            RefreshCells(grid, colony, window);
            CopyWindow(grid.Width);
            DrawDumpMarkers(grid, dumpStrips);
            if (drops != null)
                DrawDrops(drops);
            if (drawAnts)
            {
                DrawAnts(ants);
                if (colony != null)
                    DrawQueen(colony.Queen);
            }
            if (showBrush)
                DrawBrush(brushCell, brushRadius, BrushColor(brush));

            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
            if (_tileInstance != null)
            {
                _materialTexture.SetPixelData(_materialIds, 0);
                _materialTexture.Apply(false);
            }
        }

        Color32 BrushColor(BrushTint brush)
        {
            switch (brush)
            {
                case BrushTint.Build: return _buildMarkColor;
                case BrushTint.QueenRoom: return _queenRoomColor;
                case BrushTint.NutrientRoom: return _nutrientRoomColor;
                case BrushTint.NurseryRoom: return _nurseryRoomColor;
                case BrushTint.WasteRoom: return _wasteRoomColor;
                case BrushTint.DewRoom: return _dewRoomColor;
                case BrushTint.FoodStore: return _foodStoreColor;
                case BrushTint.BlockStore: return _blockStoreColor;
                case BrushTint.WaterStore: return _waterStoreColor;
                default: return _brushColor;
            }
        }

        void CreateTexture(int width, int height)
        {
            ReleaseTexture();
            _width = width;
            _height = height;
            _pixels = new Color32[width * height];
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "AntFarmWorld",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _sprite = Sprite.Create(_texture, new Rect(0, 0, width, height), Vector2.zero, 1f, 0, SpriteMeshType.FullRect);
            _sprite.name = "AntFarmWorld";
            _windowRenderer.sprite = _sprite;
            if (_tileInstance != null)
            {
                _materialIds = new byte[width * height];
                _materialTexture = new Texture2D(width, height, TextureFormat.R8, false, true)
                {
                    name = "AntFarmCellMaterials",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _tileInstance.SetTexture(CellMaterialsId, _materialTexture);
            }
        }

        /// <summary>Redesenha no cache os blocos da janela cuja versão visual (ou a de um vizinho) mudou desde o último desenho.</summary>
        void RefreshCells(PixelGrid grid, AntColony colony, RectInt window)
        {
            bool hasColony = colony != null;
            bool tiles = _tileInstance != null;
            if (_cachedGrid != grid || _cellColors == null || _cellColors.Length != grid.Width * grid.Height ||
                (tiles && _cellTiles == null) || _cachedHasColony != hasColony)
            {
                _cachedGrid = grid;
                _cachedHasColony = hasColony;
                _cellColors = new Color32[grid.Width * grid.Height];
                _cellTiles = tiles ? new byte[grid.Width * grid.Height] : null;
                _cellFog = new byte[grid.Width * grid.Height];
                _drawnVersion = new int[grid.ChunkColumns * grid.ChunkRows];
                for (int i = 0; i < _drawnVersion.Length; i++)
                    _drawnVersion[i] = -1;
                _renderLogSeen = grid.RenderLogCount;
            }
            // Mudanças demais desde o último frame para o registro circular: o que não foi lido se perdeu, refaz tudo.
            if (grid.RenderLogCount - _renderLogSeen > PixelGrid.RenderLogCapacity)
            {
                for (int i = 0; i < _drawnVersion.Length; i++)
                    _drawnVersion[i] = -1;
                _renderLogSeen = grid.RenderLogCount;
            }

            int cx0 = window.xMin >> PixelGrid.ChunkShift;
            int cx1 = (window.xMax - 1) >> PixelGrid.ChunkShift;
            int cy0 = window.yMin >> PixelGrid.ChunkShift;
            int cy1 = (window.yMax - 1) >> PixelGrid.ChunkShift;
            LastRedrawnChunks = 0;
            LastRedrawnCells = 0;
            LastWindowChunks = (cx1 - cx0 + 1) * (cy1 - cy0 + 1);
            for (int cy = cy0; cy <= cy1; cy++)
            for (int cx = cx0; cx <= cx1; cx++)
            {
                // A névoa lê até _revealRadius células em volta (menos que um bloco): os 8 vizinhos entram na versão.
                int version = 0;
                for (int ny = Mathf.Max(0, cy - 1); ny <= Mathf.Min(grid.ChunkRows - 1, cy + 1); ny++)
                for (int nx = Mathf.Max(0, cx - 1); nx <= Mathf.Min(grid.ChunkColumns - 1, cx + 1); nx++)
                    version = Mathf.Max(version, grid.GetChunkRenderVersion(nx, ny));
                int index = cy * grid.ChunkColumns + cx;
                if (_drawnVersion[index] == version)
                    continue;
                DrawChunk(grid, colony, cx, cy);
                _drawnVersion[index] = version;
                LastRedrawnChunks++;
            }

            ApplyRenderLog(grid, colony, window);
        }

        /// <summary>
        /// Mudanças de aparência de uma célula só (água, umidade, marcas, salas, depósitos, nutriente): refaz a célula e a de
        /// baixo (o tom da água depende da água acima) em vez do bloco inteiro; a névoa não muda, vale a guardada. Célula fora da
        /// janela só invalida o bloco dela, que é refeito quando a janela chegar lá.
        /// </summary>
        void ApplyRenderLog(PixelGrid grid, AntColony colony, RectInt window)
        {
            long count = grid.RenderLogCount;
            int radius = _fogOfWar ? _revealRadius : 0;
            for (long sequence = _renderLogSeen; sequence < count; sequence++)
            {
                int cell = grid.GetRenderLogCell(sequence);
                int x = cell % grid.Width, y = cell / grid.Width;
                RedrawCell(grid, colony, window, radius, x, y);
                RedrawCell(grid, colony, window, radius, x, y - 1);
            }
            _renderLogSeen = count;
        }

        void RedrawCell(PixelGrid grid, AntColony colony, RectInt window, int radius, int x, int y)
        {
            if ((uint)x >= (uint)grid.Width || (uint)y >= (uint)grid.Height)
                return;
            int chunk = (y >> PixelGrid.ChunkShift) * grid.ChunkColumns + (x >> PixelGrid.ChunkShift);
            if (_drawnVersion[chunk] < 0)
                return;
            if (x < window.xMin || x >= window.xMax || y < window.yMin || y >= window.yMax)
            {
                _drawnVersion[chunk] = -1;
                return;
            }
            int index = y * grid.Width + x;
            _cellColors[index] = ComputeCell(grid, colony, x, y, radius > 0 ? _cellFog[index] : 0, radius, out byte tileId);
            if (_cellTiles != null)
                _cellTiles[index] = tileId;
            LastRedrawnCells++;
        }

        /// <summary>Copia a janela do cache para a textura (as camadas que mudam todo frame são desenhadas por cima).</summary>
        void CopyWindow(int gridWidth)
        {
            bool tiles = _materialIds != null && _cellTiles != null;
            for (int wy = 0; wy < _height; wy++)
            {
                int source = (_originY + wy) * gridWidth + _originX;
                System.Array.Copy(_cellColors, source, _pixels, wy * _width, _width);
                if (tiles)
                    System.Array.Copy(_cellTiles, source, _materialIds, wy * _width, _width);
            }
        }

        void DrawChunk(PixelGrid grid, AntColony colony, int cx, int cy)
        {
            int radius = _fogOfWar ? _revealRadius : 0;
            int x0 = cx << PixelGrid.ChunkShift;
            int y0 = cy << PixelGrid.ChunkShift;
            int w = Mathf.Min(PixelGrid.ChunkSize, grid.Width - x0);
            int h = Mathf.Min(PixelGrid.ChunkSize, grid.Height - y0);
            if (radius > 0)
                ComputeOpenness(grid, radius, x0, y0, w, h);
            int stride = w + 2 * radius;
            bool tiles = _cellTiles != null;
            for (int ly = 0; ly < h; ly++)
            {
                int y = y0 + ly;
                int row = y * grid.Width + x0;
                for (int lx = 0; lx < w; lx++)
                {
                    int x = x0 + lx;
                    int fogDistance = 0;
                    if (radius > 0)
                    {
                        fogDistance = FogDistance(lx, ly, radius, stride);
                        _cellFog[row + lx] = (byte)fogDistance;
                    }
                    _cellColors[row + lx] = ComputeCell(grid, colony, x, y, fogDistance, radius, out byte tileId);
                    if (tiles)
                        _cellTiles[row + lx] = tileId;
                }
            }
        }

        /// <summary>Cor (e material de tile) de uma célula, dada a distância da névoa dela (0 = sem névoa).</summary>
        Color32 ComputeCell(PixelGrid grid, AntColony colony, int x, int y, int fogDistance, int radius, out byte tileId)
        {
            float faintRoom = _queenRoomStrength * 0.5f;
            CellMaterial material = grid.Get(x, y);
            Color32 color;
            bool solid = false;
            tileId = (byte)material;
            if (_plantsAsSprites && CellMaterials.IsPlant(material))
            {
                color = _skyRows[y];
                tileId = CellTileArt.SkyId;
            }
            else if (material == CellMaterial.Empty)
            {
                if (y > grid.GetSurfaceY(x))
                {
                    color = _skyRows[y];
                    tileId = CellTileArt.SkyId;
                    if (grid.IsBuildMarked(x, y))
                        color = Color32.Lerp(color, _buildMarkColor, _markStrength);
                }
                else
                {
                    // Fundo de túnel com leve granulação fixa por posição.
                    int n = ((x * 73856093) ^ (y * 19349663)) & 15;
                    color = new Color32((byte)(_tunnelColor.r + n), (byte)(_tunnelColor.g + n / 2), (byte)(_tunnelColor.b + n / 3), 255);
                    if (grid.IsBuildMarked(x, y))
                        color = Color32.Lerp(color, _buildMarkColor, _markStrength);
                    if (grid.IsQueenRoom(x, y))
                        color = Color32.Lerp(color, _queenRoomColor,
                                             colony != null && colony.IsQueenArea(x, y) ? _queenRoomStrength : faintRoom);
                    if (grid.IsNutrientRoom(x, y))
                        color = Color32.Lerp(color, _nutrientRoomColor, _nutrientRoomStrength);
                    if (grid.IsNurseryRoom(x, y))
                        color = Color32.Lerp(color, _nurseryRoomColor, _nutrientRoomStrength);
                    if (grid.IsWasteRoom(x, y))
                        color = Color32.Lerp(color, _wasteRoomColor, _nutrientRoomStrength);
                    if (grid.IsDewRoom(x, y))
                        color = Color32.Lerp(color, _dewRoomColor, _nutrientRoomStrength);
                    StorageKind store = grid.GetStorageRoom(x, y);
                    if (store != StorageKind.None)
                        color = Color32.Lerp(color, StoreColor(store), _nutrientRoomStrength);
                }
                if (grid.HasNutrient(x, y))
                    color = NutrientAt(x, y);
                byte stored = grid.GetStored(x, y);
                if (stored != StoredItems.None)
                    color = StoredAt(color, stored, grid.GetStoredShade(x, y), x, y);
                if (grid.HasWater(x, y))
                    color = WaterOver(color, grid.HasWater(x, y + 1) ? _waterColor : _waterSurfaceColor);
            }
            else
            {
                color = CellMaterials.ShadedColor(material, grid.GetShade(x, y));
                byte wetness = grid.GetWetness(x, y);
                if (wetness != 0)
                    color = Darken(color, Mathf.Max(0.4f, 1f - wetness * _wetDarkening));
                solid = true;
            }
            if (radius > 0 && fogDistance > 0)
                color = Darken(color, fogDistance > radius ? 0f : 1f - fogDistance / (float)(radius + 1));
            // A marca de cavar e a pintura de sala vêm depois da névoa: não escurecem com ela.
            if (solid)
            {
                if (grid.IsMarked(x, y))
                    color = Color32.Lerp(color, _markColor, _markStrength);
                if (grid.IsQueenRoom(x, y))
                    color = Color32.Lerp(color, _queenRoomColor, faintRoom);
                if (grid.IsNutrientRoom(x, y))
                    color = Color32.Lerp(color, _nutrientRoomColor, _nutrientRoomStrength * 0.5f);
                if (grid.IsNurseryRoom(x, y))
                    color = Color32.Lerp(color, _nurseryRoomColor, _nutrientRoomStrength * 0.5f);
                if (grid.IsWasteRoom(x, y))
                    color = Color32.Lerp(color, _wasteRoomColor, _nutrientRoomStrength * 0.5f);
                if (grid.IsDewRoom(x, y))
                    color = Color32.Lerp(color, _dewRoomColor, _nutrientRoomStrength * 0.5f);
                StorageKind store = grid.GetStorageRoom(x, y);
                if (store != StorageKind.None)
                    color = Color32.Lerp(color, StoreColor(store), _nutrientRoomStrength * 0.5f);
            }
            return color;
        }

        /// <summary>
        /// Névoa: marca as células abertas do bloco (origem <paramref name="ox"/>, <paramref name="oy"/>, tamanho
        /// <paramref name="w"/> x <paramref name="h"/>) mais <paramref name="radius"/> de folga (o que está fora do bloco
        /// também ilumina a borda) e a menor distância horizontal até uma delas, em cada célula.
        /// </summary>
        void ComputeOpenness(PixelGrid grid, int radius, int ox, int oy, int w, int h)
        {
            int stride = w + 2 * radius;
            int rows = h + 2 * radius;
            if (_open == null || _open.Length < stride * rows)
            {
                _open = new bool[stride * rows];
                _fogRows = new byte[stride * rows];
                _openSum = new int[(stride + 1) * (rows + 1)];
            }
            // A tabela reaproveitada precisa da linha e da coluna de zeros do tamanho atual.
            System.Array.Clear(_openSum, 0, (stride + 1) * (rows + 1));

            int sumStride = stride + 1;
            for (int r = 0; r < rows; r++)
            {
                int y = oy - radius + r;
                int rowSum = 0;
                for (int c = 0; c < stride; c++)
                {
                    int x = ox - radius + c;
                    bool open = grid.InBounds(x, y) &&
                                (grid.Get(x, y) == CellMaterial.Empty || y > grid.GetSurfaceY(x));
                    _open[r * stride + c] = open;
                    // Soma acumulada 2D (tabela de área somada): conta as abertas de qualquer quadrado em O(1).
                    rowSum += open ? 1 : 0;
                    _openSum[(r + 1) * sumStride + c + 1] = _openSum[r * sumStride + c + 1] + rowSum;
                }
            }

            // Distância horizontal (até radius + 1 = longe) para cada coluna do bloco, em todas as linhas com folga.
            for (int r = 0; r < rows; r++)
            for (int wx = 0; wx < w; wx++)
            {
                int best = radius + 1;
                int center = r * stride + wx + radius;
                for (int dx = 0; dx <= radius && dx < best; dx++)
                {
                    if (_open[center + dx] || _open[center - dx])
                        best = dx;
                }
                _fogRows[r * stride + wx] = (byte)best;
            }
        }

        /// <summary>Distância de xadrez (0 = aberta) até a célula aberta mais próxima; radius + 1 = fora do alcance.</summary>
        int FogDistance(int wx, int wy, int radius, int stride)
        {
            // Nenhuma aberta no quadrado (2r+1)²: longe, sem varrer as linhas (o caso comum, no fundo da terra).
            // As coordenadas do quadrado, no espaço com folga, vão de (wx, wy) a (wx + 2r, wy + 2r).
            int side = 2 * radius + 1;
            int sumStride = stride + 1;
            int open = _openSum[(wy + side) * sumStride + wx + side] - _openSum[wy * sumStride + wx + side]
                       - _openSum[(wy + side) * sumStride + wx] + _openSum[wy * sumStride + wx];
            if (open == 0)
                return radius + 1;

            // Do centro para fora: sai assim que o próximo dy já não melhora o melhor achado.
            int best = radius + 1;
            int center = wy + radius;
            for (int a = 0; a <= radius && a < best; a++)
            {
                int d = Mathf.Max(a, _fogRows[(center + a) * stride + wx]);
                if (d < best)
                    best = d;
                if (a == 0)
                    continue;
                d = Mathf.Max(a, _fogRows[(center - a) * stride + wx]);
                if (d < best)
                    best = d;
            }
            return best;
        }

        Color32 StoreColor(StorageKind kind)
        {
            switch (kind)
            {
                case StorageKind.Food: return _foodStoreColor;
                case StorageKind.Blocks: return _blockStoreColor;
                default: return _waterStoreColor;
            }
        }

        /// <summary>
        /// Item guardado (depósito ou resíduo): a cor do material com o tom dele, misturada com o fundo para não parecer
        /// parede (a célula continua livre para passar); a água guardada usa a cor da água, com um granulado por posição.
        /// </summary>
        Color32 StoredAt(Color32 background, byte item, byte shade, int x, int y)
        {
            if (item == StoredItems.Water)
            {
                int n = ((x * 19349663) ^ (y * 83492791)) & 15;
                Color32 water = new Color32((byte)(_waterColor.r + n), (byte)(_waterColor.g + n), (byte)Mathf.Min(255, _waterColor.b + n), 255);
                return Color32.Lerp(background, water, _storedStrength);
            }
            return Color32.Lerp(background, CellMaterials.ShadedColor(StoredItems.MaterialOf(item), shade), _storedStrength);
        }

        /// <summary>Nutriente pronto: a cor fixa com um leve granulado por posição, para vários juntos não virarem um bloco liso.</summary>
        Color32 NutrientAt(int x, int y)
        {
            Color32 baseColor = _nutrientColor;
            int n = ((x * 19349663) ^ (y * 83492791)) & 15;
            return new Color32((byte)Mathf.Max(0, baseColor.r - n), (byte)Mathf.Max(0, baseColor.g - n),
                               (byte)Mathf.Max(0, baseColor.b - n / 2), 255);
        }

        void DrawDumpMarkers(PixelGrid grid, IReadOnlyList<Vector2Int> dumpStrips)
        {
            // Duas estacas discretas por faixa onde as formigas despejam terra (um par de faixas por abertura).
            for (int i = 0; i < dumpStrips.Count; i++)
            {
                DrawStake(grid, dumpStrips[i].x);
                DrawStake(grid, dumpStrips[i].y);
            }
        }

        void DrawStake(PixelGrid grid, int x)
        {
            int baseY = grid.GetSurfaceY(x) + 1;
            for (int i = 0; i < 4; i++)
                Blend(x, baseY + i + 6, _dumpMarkerColor, 0.5f);
        }

        void DrawAnts(IReadOnlyList<Ant> ants)
        {
            for (int i = 0; i < ants.Count; i++)
            {
                Ant ant = ants[i];
                Put(ant.X - ant.FacingX, ant.Y, _antBody);
                Put(ant.X, ant.Y, _antBody);
                Put(ant.X + ant.FacingX, ant.Y, _antHead);
                if (ant.State == AntState.Carrying || ant.State == AntState.Hauling)
                    Put(ant.X, ant.Y + 1, CellMaterials.ShadedColor(ant.Carried, ant.CarriedShade));
                else if (ant.State == AntState.Delivering)
                    Put(ant.X, ant.Y + 1, _nutrientColor);
            }
        }

        /// <summary>Fallback da rainha: o dobro da formiga de 3 pixels (6 x 2).</summary>
        void DrawQueen(Ant queen)
        {
            int f = queen.FacingX >= 0 ? 1 : -1;
            for (int i = -2; i <= 3; i++)
            {
                Color32 color = i >= 2 ? _antHead : _antBody;
                Put(queen.X + i * f, queen.Y, color);
                Put(queen.X + i * f, queen.Y + 1, color);
            }
        }

        void DrawDrops(IReadOnlyList<RainDrop> drops)
        {
            // Cada gota é um risco vertical proporcional à queda por passo, terminando na posição atual.
            for (int i = 0; i < drops.Count; i++)
            {
                RainDrop drop = drops[i];
                Color32 color = drop.Heavy ? _heavyDropColor : _lightDropColor;
                int length = Mathf.Max(1, Mathf.RoundToInt(drop.Speed * _dropStreak));
                for (int k = 0; k < length; k++)
                    BlendOver(drop.X, drop.Y + k, color, _dropOpacity * (1f - k / (float)(length + 1)));
            }
        }

        /// <summary>Água translúcida por cima do fundo; sobre o céu transparente, pinta com alfa (o cenário aparece atrás).</summary>
        Color32 WaterOver(Color32 under, Color32 water) =>
            under.a == 0
                ? new Color32(water.r, water.g, water.b, (byte)(255 * _waterOpacity))
                : Color32.Lerp(under, new Color32(water.r, water.g, water.b, under.a), _waterOpacity);

        static Color32 Darken(Color32 c, float f) =>
            new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), c.a);

        void DrawBrush(Vector2Int center, int radius, Color32 color)
        {
            int r2 = radius * radius;
            for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (!Inside(dx, dy, r2))
                    continue;
                bool edge = !Inside(dx + 1, dy, r2) || !Inside(dx - 1, dy, r2) ||
                            !Inside(dx, dy + 1, r2) || !Inside(dx, dy - 1, r2);
                if (edge)
                    Blend(center.x + dx, center.y + dy, color, 0.6f);
            }
        }

        static bool Inside(int dx, int dy, int r2) => dx * dx + dy * dy <= r2;

        // Put/Blend recebem coordenadas de célula da grade e descartam o que cai fora da janela.
        void Put(int x, int y, Color32 color)
        {
            x -= _originX;
            y -= _originY;
            if ((uint)x < (uint)_width && (uint)y < (uint)_height)
                _pixels[y * _width + x] = color;
        }

        void Blend(int x, int y, Color32 color, float t)
        {
            x -= _originX;
            y -= _originY;
            if ((uint)x < (uint)_width && (uint)y < (uint)_height)
                _pixels[y * _width + x] = Color32.Lerp(_pixels[y * _width + x], color, t);
        }

        /// <summary>Como <see cref="Blend"/>, mas sobre céu transparente pinta a cor com alfa (o cenário aparece atrás).</summary>
        void BlendOver(int x, int y, Color32 color, float t)
        {
            x -= _originX;
            y -= _originY;
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height)
                return;
            int i = y * _width + x;
            Color32 under = _pixels[i];
            if (under.a == 0)
                _pixels[i] = new Color32(color.r, color.g, color.b, (byte)(255 * t));
            else
                _pixels[i] = Color32.Lerp(under, new Color32(color.r, color.g, color.b, under.a), t);
        }

        // Cor e névoa vêm dos campos serializados: mudar no Inspector com o jogo rodando redesenha tudo.
        void OnValidate() => _cachedGrid = null;

        void OnDestroy()
        {
            ReleaseTexture();
            if (_tileInstance != null)
                Destroy(_tileInstance);
            if (_generatedAtlas != null)
                Destroy(_generatedAtlas);
        }

        void ReleaseTexture()
        {
            if (_sprite != null)
                Destroy(_sprite);
            if (_texture != null)
                Destroy(_texture);
            if (_materialTexture != null)
                Destroy(_materialTexture);
            _sprite = null;
            _texture = null;
            _materialTexture = null;
        }
    }
}
