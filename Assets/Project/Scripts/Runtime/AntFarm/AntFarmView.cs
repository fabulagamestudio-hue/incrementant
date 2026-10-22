using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Settings;
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
        GlowMushroom,
        FarmRoom,
        SandStore,
        MushroomStore,
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
    public sealed partial class AntFarmView : MonoBehaviour
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
        [Tooltip("Cor da área de resíduos no modo folhas em água (as outras: Waste Room Color = restos em folhas, Waste Dirt Color = terra em areia e água).")]
        [SerializeField] Color32 _wasteLeavesColor = new Color32(90, 170, 150, 255);
        [SerializeField] Color32 _wasteDirtColor = new Color32(190, 120, 70, 255);
        [Tooltip("Cor da sala Gotas de Orvalho pintada e do pincel no modo sala Gotas de Orvalho.")]
        [SerializeField] Color32 _dewRoomColor = new Color32(90, 190, 230, 255);
        [Tooltip("Cor do depósito de alimentos pintado e do pincel no modo depósito de alimentos.")]
        [SerializeField] Color32 _foodStoreColor = new Color32(150, 210, 60, 255);
        [Tooltip("Cor do depósito de construção pintado e do pincel no modo depósito de construção.")]
        [SerializeField] Color32 _blockStoreColor = new Color32(220, 140, 70, 255);
        [Tooltip("Cor do depósito de água pintado e do pincel no modo depósito de água.")]
        [SerializeField] Color32 _waterStoreColor = new Color32(70, 120, 235, 255);
        [Tooltip("Cor do depósito de areia pintado e do pincel no modo depósito de areia.")]
        [SerializeField] Color32 _sandStoreColor = new Color32(230, 200, 110, 255);
        [Tooltip("Cor do depósito de cogumelos pintado e do pincel no modo depósito de cogumelos.")]
        [SerializeField] Color32 _mushroomStoreColor = new Color32(200, 150, 220, 255);
        [Tooltip("Cor da fazenda de pulgões pintada e do pincel no modo fazenda.")]
        [SerializeField] Color32 _farmRoomColor = new Color32(140, 200, 120, 255);
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
        [Header("Luz")]
        [Tooltip("Nível de luz do lado de fora (acima do solo original), que entra pelas bocas do formigueiro (0 a 16; perde 1 por célula de túnel e Solid Light Cost por célula de terra).")]
        [SerializeField, Range(0, LightLevels)] int _outsideLight = LightLevels;
        [Tooltip("Níveis que a luz perde ao entrar numa célula de terra (no túnel perde 1).")]
        [SerializeField, Range(1, 8)] int _solidLightCost = 4;
        [Tooltip("Brilho mínimo do túnel longe da luz: a penumbra do formigueiro (0 = preto). A terra longe da luz fica sempre preta.")]
        [SerializeField, Range(0f, 0.5f)] float _interiorAmbient = 0.22f;
        [Tooltip("Nível de luz do cogumelo luminoso (0 a 16, o teto da regra de limite): espalha pela mesma regra da luz de fora, perdendo 1 por célula de túnel e Solid Light Cost por célula de terra.")]
        [SerializeField, Range(0, LightLevels)] int _glowMushroomLight = LightLevels;
        [Tooltip("Níveis que a luz do cogumelo perde ao entrar numa célula de terra (a de fora perde Solid Light Cost). Menor = a luz entra mais nas paredes.")]
        [SerializeField, Range(1, 8)] int _glowSolidLightCost = 4;
        [Tooltip("Multiplicador da cor do cogumelo luminoso no shader (acima de 1 = HDR): é o que o bloom do pós-processamento pega. Só com Tile Material.")]
        [SerializeField, Range(1f, 8f)] float _glowMushroomEmission = 2.5f;
        [Tooltip("Cor do cogumelo luminoso: o cubo, a marca do pincel, o contorno do pincel e o tom da luz que sai dele.")]
        [SerializeField] Color32 _glowMushroomColor = new Color32(240, 214, 130, 255);
        [Tooltip("Quanto a cor do cogumelo tinge a luz em volta (0 = luz branca, 1 = multiplica a cor inteira, com o canal mais forte em 1 para não passar do branco e entrar no bloom). Só com Tile Material.")]
        [SerializeField, Range(0f, 1f)] float _glowColorStrength = 1f;
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
        [Tooltip("Atlas RGB desenhado: 128 x 128 por linha, empilhadas de baixo para cima na ordem de CellMaterial, com a parede de fundo do túnel por último; Read/Write ligado. Vazio: gerado por código (CellTileArt).")]
        [SerializeField] Texture2D _tileAtlas;
        [SerializeField, Range(0f, 2f)] float _tileDetail = 1f;
        [Tooltip("Quanto do tom de cada célula (variação de cor por posição) passa por cima do atlas: 1 = o tom inteiro, 0 = só a arte do atlas.")]
        [SerializeField, Range(0f, 1f)] float _tileToneStrength = 0.5f;
        [Tooltip("Depuração: pinta cada célula pela distância com sinal até a borda terra/vazio (quente = terra, frio = vazio, claro = perto da borda). Só com Tile Material.")]
        [SerializeField] bool _debugSignedDistance;

        /// <summary>A janela cresce em passos deste tamanho, para não recriar a textura a cada mudança de zoom.</summary>
        const int WindowStep = 16;

        /// <summary>
        /// Alcance da distância com sinal até a borda terra/vazio, em células (distância de xadrez): de 1 a este valor dentro
        /// da terra, de -1 a -este valor no vazio; o valor do alcance vale "este ou mais longe". Fica bem abaixo do bloco
        /// (<see cref="PixelGrid.ChunkSize"/>), para a versão do bloco e dos 8 vizinhos bastar para saber quando refazer.
        /// </summary>
        public const int SignedDistanceRange = 4;
        /// <summary>Valor gravado no canal G da textura de materiais para a distância 0: o canal guarda 128 + distância.</summary>
        public const byte SignedDistanceZero = 128;
        /// <summary>Bytes por célula na textura de materiais (RGBA32) e no cache: material, distância com sinal, luz e "sem luz".</summary>
        public const int TileBytes = 4;

        SpriteRenderer _windowRenderer;
        Texture2D _texture;
        Sprite _sprite;
        Color32[] _pixels;
        // Tiles: TileBytes por célula da janela (R = material, CellTileArt.SkyId = céu; G = 128 + distância com sinal até
        // a borda; B = luz; A = 255 quando a cor já vem iluminada da CPU) e o material instanciado do shader.
        Texture2D _materialTexture;
        byte[] _materialIds;
        Material _tileInstance;
        TerrainTileAtlas _atlas;
        static readonly int WindowOriginId = Shader.PropertyToID("_WindowOrigin");
        static readonly int CellMaterialsId = Shader.PropertyToID("_CellMaterials");
        static readonly int DebugSignedDistanceId = Shader.PropertyToID("_DebugSignedDistance");
        static readonly int InteriorAmbientId = Shader.PropertyToID("_InteriorAmbient");
        static readonly int GlowMaterialId = Shader.PropertyToID("_GlowMaterialId");
        static readonly int GlowEmissionId = Shader.PropertyToID("_GlowEmission");
        static readonly int GlowLightTintId = Shader.PropertyToID("_GlowLightTint");
        Color32[] _skyRows;
        int _width;
        int _height;
        int _originX;
        int _originY;
        bool _plantsAsSprites;
        // Distância com sinal: célula sólida para a borda (ver IsEdgeOpen) e tabela de área somada delas, no bloco com folga.
        bool[] _edgeSolid;
        int[] _solidSum;
        // Cache de cor (e, com tiles, dos TileBytes da textura) de cada célula da grade inteira. Só os blocos cuja versão visual
        // mudou (PixelGrid.GetChunkRenderVersion, ou um vizinho por causa da luz) são redesenhados; a janela é cópia do cache.
        PixelGrid _cachedGrid;
        bool _cachedHasColony;
        Color32[] _cellColors;
        byte[] _cellTiles;
        // Luz de cada célula no último desenho do bloco dela (só muda com a forma do terreno e com as fontes, que sobem a
        // versão do bloco: PixelGrid.DirtyShape).
        byte[] _cellLight;
        // A luz da célula veio de um cogumelo luminoso (no último desenho do bloco dela), para o canal A da textura.
        bool[] _cellGlowLight;
        int[] _drawnVersion;
        long _renderLogSeen;
        // Valor de ShowRooms com que o cache foi desenhado: mudou, refaz tudo.
        bool _drawnRooms = true;

        /// <summary>
        /// Desenha a pintura das salas e depósitos (opção "Mostrar ambientes"). Desligada, as salas continuam valendo, só não
        /// aparecem; as marcas de cavar e de obra seguem visíveis.
        /// </summary>
        public bool ShowRooms { get; set; } = true;

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
            _atlas = TerrainTileAtlas.Create(_tileAtlas, TileReferences(), this);
            _tileInstance = new Material(_tileMaterial) { name = "TerrainTiles (instância)", hideFlags = HideFlags.DontSave };
            _tileInstance.SetTexture("_TileAtlas", _atlas.Texture);
            _tileInstance.SetTexture("_MaterialColors", _atlas.Colors);
            _tileInstance.SetFloat("_TileCount", CellTileArt.RowCount);
            _tileInstance.SetFloat("_BackTileId", CellTileArt.BackgroundRow);
            _tileInstance.SetFloat("_TileSize", CellTileArt.TileSize);
            _tileInstance.SetFloat("_TexelsPerCell", CellTileArt.TexelsPerCell);
            _tileInstance.SetFloat("_DetailStrength", _tileDetail);
            return _tileInstance;
        }

        /// <summary>Atlas do terreno em uso (null sem Tile Material): o bloco carregado lê o mesmo.</summary>
        public TerrainTileAtlas TileAtlas => _atlas;

        /// <summary>Força do tom da célula sobre o atlas (<see cref="CellMaterials.ShadedColor(CellMaterial, byte, float)"/>).</summary>
        public float TileToneStrength => _tileToneStrength;

        /// <summary>
        /// Cor de referência de cada linha do atlas: a que <see cref="ComputeCell"/> grava numa célula do material sem tom
        /// nem marca (o cogumelo usa a própria cor; a parede de fundo, a do túnel). Salas construídas (REQ-058, ainda sem
        /// lógica): o bloco é terra posta pelas formigas, então usa a cor da terra; o fundo da sala, a do túnel.
        /// </summary>
        Color32[] TileReferences()
        {
            var references = new Color32[CellTileArt.RowCount];
            for (int row = 0; row < CellTileArt.MaterialCount; row++)
                references[row] = CellMaterials.BaseColor((CellMaterial)row);
            references[(int)CellMaterial.Empty] = _tunnelColor;
            references[(int)CellMaterial.GlowMushroom] = _glowMushroomColor;
            references[CellTileArt.BackgroundRow] = _tunnelColor;
            for (int style = 0; style < CellTileArt.RoomStyleCount; style++)
            {
                references[CellTileArt.RoomBlockRow((RoomStyle)style)] = CellMaterials.BaseColor(CellMaterial.Dirt);
                references[CellTileArt.RoomBackRow((RoomStyle)style)] = _tunnelColor;
            }
            return references;
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
            {
                _tileInstance.SetVector(WindowOriginId, new Vector4(_originX, _originY, _width, _height));
                _tileInstance.SetFloat(DebugSignedDistanceId, _debugSignedDistance ? 1f : 0f);
                _tileInstance.SetFloat(InteriorAmbientId, _interiorAmbient);
                _tileInstance.SetFloat(GlowMaterialId, (float)CellMaterial.GlowMushroom);
                _tileInstance.SetFloat(GlowEmissionId, _glowMushroomEmission);
                _tileInstance.SetColor(GlowLightTintId, GlowLightTint());
            }

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

        /// <summary>Modo (<c>WasteMode</c> como byte) com que o pincel Resíduos pinta: dá a cor do contorno do pincel.</summary>
        public byte WasteBrushKind { get; set; }

        /// <summary>Cor da área de resíduos por modo (0 restos em folhas, 1 folhas em água, 2 terra em areia e água).</summary>
        Color32 WasteColor(byte kind) => kind == 1 ? _wasteLeavesColor : kind == 2 ? _wasteDirtColor : _wasteRoomColor;

        Color32 BrushColor(BrushTint brush)
        {
            switch (brush)
            {
                case BrushTint.Build: return _buildMarkColor;
                case BrushTint.QueenRoom: return _queenRoomColor;
                case BrushTint.NutrientRoom: return _nutrientRoomColor;
                case BrushTint.NurseryRoom: return _nurseryRoomColor;
                case BrushTint.WasteRoom: return WasteColor((byte)WasteBrushKind);
                case BrushTint.DewRoom: return _dewRoomColor;
                case BrushTint.FoodStore: return _foodStoreColor;
                case BrushTint.BlockStore: return _blockStoreColor;
                case BrushTint.WaterStore: return _waterStoreColor;
                case BrushTint.GlowMushroom: return _glowMushroomColor;
                case BrushTint.FarmRoom: return _farmRoomColor;
                case BrushTint.SandStore: return _sandStoreColor;
                case BrushTint.MushroomStore: return _mushroomStoreColor;
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
                _materialIds = new byte[TileBytes * width * height];
                _materialTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
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
            bool fresh = false;
            if (_cachedGrid != grid || _cellColors == null || _cellColors.Length != grid.Width * grid.Height ||
                (tiles && _cellTiles == null) || _cachedHasColony != hasColony)
            {
                _cachedGrid = grid;
                _cachedHasColony = hasColony;
                _cellColors = new Color32[grid.Width * grid.Height];
                _cellTiles = tiles ? new byte[TileBytes * grid.Width * grid.Height] : null;
                _cellLight = new byte[grid.Width * grid.Height];
                _cellGlowLight = new bool[grid.Width * grid.Height];
                _drawnVersion = new int[grid.ChunkColumns * grid.ChunkRows];
                for (int i = 0; i < _drawnVersion.Length; i++)
                    _drawnVersion[i] = -1;
                _renderLogSeen = grid.RenderLogCount;
                fresh = true;
            }
            // Mudanças demais desde o último frame para o registro circular (o que não foi lido se perdeu) ou a pintura das
            // salas ligou/desligou: refaz tudo.
            if (grid.RenderLogCount - _renderLogSeen > PixelGrid.RenderLogCapacity || _drawnRooms != ShowRooms)
            {
                _drawnRooms = ShowRooms;
                for (int i = 0; i < _drawnVersion.Length; i++)
                    _drawnVersion[i] = -1;
                _renderLogSeen = grid.RenderLogCount;
            }
            RefreshBeams(grid, fresh);

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
                // A luz e a distância com sinal leem menos que um bloco em volta: os 8 vizinhos entram na versão.
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
        /// baixo (o tom da água depende da água acima) em vez do bloco inteiro; a luz e a borda não mudam (o que muda as duas
        /// sobe a versão do bloco), valem as guardadas. Célula fora da janela só invalida o bloco dela, que é refeito quando a
        /// janela chegar lá.
        /// </summary>
        void ApplyRenderLog(PixelGrid grid, AntColony colony, RectInt window)
        {
            long count = grid.RenderLogCount;
            for (long sequence = _renderLogSeen; sequence < count; sequence++)
            {
                int cell = grid.GetRenderLogCell(sequence);
                int x = cell % grid.Width, y = cell / grid.Width;
                RedrawCell(grid, colony, window, x, y);
                RedrawCell(grid, colony, window, x, y - 1);
            }
            _renderLogSeen = count;
        }

        void RedrawCell(PixelGrid grid, AntColony colony, RectInt window, int x, int y)
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
            _cellColors[index] = ComputeCell(grid, colony, x, y, _cellLight[index], out byte tileId, out bool lit);
            if (_cellTiles != null)
            {
                _cellTiles[TileBytes * index] = tileId;
                _cellTiles[TileBytes * index + 3] = LightFlags(lit, _cellGlowLight[index]);
            }
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
                    System.Array.Copy(_cellTiles, TileBytes * source, _materialIds, TileBytes * wy * _width, TileBytes * _width);
            }
        }

        void DrawChunk(PixelGrid grid, AntColony colony, int cx, int cy)
        {
            int x0 = cx << PixelGrid.ChunkShift;
            int y0 = cy << PixelGrid.ChunkShift;
            int w = Mathf.Min(PixelGrid.ChunkSize, grid.Width - x0);
            int h = Mathf.Min(PixelGrid.ChunkSize, grid.Height - y0);
            ComputeLight(grid, x0, y0, w, h);
            bool tiles = _cellTiles != null;
            if (tiles)
                ComputeSolidity(grid, x0, y0, w, h);
            for (int ly = 0; ly < h; ly++)
            {
                int y = y0 + ly;
                int row = y * grid.Width + x0;
                for (int lx = 0; lx < w; lx++)
                {
                    int x = x0 + lx;
                    byte light = LightAt(lx, ly, w);
                    _cellLight[row + lx] = light;
                    _cellGlowLight[row + lx] = GlowLightAt(lx, ly, w);
                    _cellColors[row + lx] = ComputeCell(grid, colony, x, y, light, out byte tileId, out bool lit);
                    if (tiles)
                    {
                        int t = TileBytes * (row + lx);
                        _cellTiles[t] = tileId;
                        _cellTiles[t + 1] = (byte)(SignedDistanceZero + SignedDistance(lx, ly, w));
                        _cellTiles[t + 2] = LightByte(light);
                        _cellTiles[t + 3] = LightFlags(lit, _cellGlowLight[row + lx]);
                    }
                }
            }
        }

        /// <summary>
        /// Lado vazio da borda que a distância com sinal mede: célula sem material, ou planta quando as plantas são desenhadas
        /// à parte (a célula dela aparece como céu). Monte acima do solo original conta como terra.
        /// </summary>
        bool IsEdgeOpen(PixelGrid grid, int x, int y)
        {
            CellMaterial material = grid.Get(x, y);
            return material == CellMaterial.Empty || (_plantsAsSprites && CellMaterials.IsPlant(material));
        }

        /// <summary>
        /// Distância com sinal: marca as células sólidas do bloco mais <see cref="SignedDistanceRange"/> - 1 de folga e monta
        /// a tabela de área somada delas. Fora da grade vale a célula da beira mais próxima, para a beira do mundo não virar borda.
        /// </summary>
        void ComputeSolidity(PixelGrid grid, int ox, int oy, int w, int h)
        {
            int margin = SignedDistanceRange - 1;
            int stride = w + 2 * margin;
            int rows = h + 2 * margin;
            if (_edgeSolid == null || _edgeSolid.Length < stride * rows)
            {
                _edgeSolid = new bool[stride * rows];
                _solidSum = new int[(stride + 1) * (rows + 1)];
            }
            System.Array.Clear(_solidSum, 0, (stride + 1) * (rows + 1));

            int sumStride = stride + 1;
            for (int r = 0; r < rows; r++)
            {
                int y = Mathf.Clamp(oy - margin + r, 0, grid.Height - 1);
                int rowSum = 0;
                for (int c = 0; c < stride; c++)
                {
                    int x = Mathf.Clamp(ox - margin + c, 0, grid.Width - 1);
                    bool solid = !IsEdgeOpen(grid, x, y);
                    _edgeSolid[r * stride + c] = solid;
                    rowSum += solid ? 1 : 0;
                    _solidSum[(r + 1) * sumStride + c + 1] = _solidSum[r * sumStride + c + 1] + rowSum;
                }
            }
        }

        /// <summary>
        /// Distância de xadrez com sinal até a célula do outro lado da borda mais próxima: 1 a <see cref="SignedDistanceRange"/>
        /// na terra, -1 a -<see cref="SignedDistanceRange"/> no vazio (o alcance vale "esse ou mais"). Precisa de <see cref="ComputeSolidity"/>.
        /// </summary>
        int SignedDistance(int lx, int ly, int w)
        {
            int margin = SignedDistanceRange - 1;
            int stride = w + 2 * margin;
            int sumStride = stride + 1;
            int cx = lx + margin, cy = ly + margin;
            bool solid = _edgeSolid[cy * stride + cx];
            for (int k = 1; k <= margin; k++)
            {
                // Sólidas no quadrado de lado 2k + 1 em volta da célula, em O(1) pela tabela de área somada.
                int x0 = cx - k, x1 = cx + k + 1, y0 = cy - k, y1 = cy + k + 1;
                int count = _solidSum[y1 * sumStride + x1] - _solidSum[y0 * sumStride + x1]
                            - _solidSum[y1 * sumStride + x0] + _solidSum[y0 * sumStride + x0];
                if (solid ? count < (2 * k + 1) * (2 * k + 1) : count > 0)
                    return solid ? k : -k;
            }
            return solid ? SignedDistanceRange : -SignedDistanceRange;
        }

        /// <summary>
        /// Cor (e material de tile) de uma célula com nível de luz <paramref name="light"/>. Sem tiles a luz vem aplicada aqui,
        /// por célula; com tiles quem ilumina é o shader, por texel, menos na terra com marca ou pintura de sala por cima:
        /// essa já sai iluminada daqui (<paramref name="lit"/>), para a marca não escurecer com a terra.
        /// </summary>
        Color32 ComputeCell(PixelGrid grid, AntColony colony, int x, int y, byte light, out byte tileId, out bool lit)
        {
            float faintRoom = _queenRoomStrength * 0.5f;
            CellMaterial material = grid.Get(x, y);
            Color32 color;
            bool solid = false;
            // Luz aplicada aqui ao túnel (1 = o shader ilumina).
            float tunnelLight = 1f;
            bool tunnelPaint = false;
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
                    // Fundo de túnel com leve granulação fixa por posição. Com pintura por cima (marca de obra, sala,
                    // depósito), a luz sai daqui e a pintura vem depois dela, para ficar visível no breu.
                    int n = ((x * 73856093) ^ (y * 19349663)) & 15;
                    color = new Color32((byte)(_tunnelColor.r + n), (byte)(_tunnelColor.g + n / 2), (byte)(_tunnelColor.b + n / 3), 255);
                    tunnelPaint = grid.IsBuildMarked(x, y) || (_drawnRooms && HasRoomPaint(grid, x, y));
                    tunnelLight = _cellTiles == null || tunnelPaint ? LightFactor(light, true) : 1f;
                    color = Darken(color, tunnelLight);
                    if (grid.IsBuildMarked(x, y))
                        color = Color32.Lerp(color, grid.IsGlowMushroomMarked(x, y) ? _glowMushroomColor : _buildMarkColor, _markStrength);
                    if (_drawnRooms)
                        color = PaintRooms(color, grid, x, y,
                                           colony != null && colony.IsQueenArea(x, y) ? _queenRoomStrength : faintRoom, _nutrientRoomStrength);
                    // Sala pronta (REQ-058): o vão desenha o fundo do ambiente; a cor continua a do túnel (a referência da linha).
                    if (_cellTiles != null && grid.IsRoomFinished(x, y) && PaintBudget.TryGetRoomAt(grid, x, y, out PaintRoom room))
                        tileId = (byte)CellTileArt.RoomBackRow(CellTileArt.StyleOf(room));
                }
                if (grid.HasNutrient(x, y))
                    color = Darken(NutrientAt(x, y), tunnelLight);
                byte stored = grid.GetStored(x, y);
                if (stored != StoredItems.None)
                    color = StoredAt(color, stored, grid.GetStoredShade(x, y), x, y, tunnelLight);
                if (grid.HasWater(x, y))
                    color = WaterOver(color, Darken(grid.HasWater(x, y + 1) ? _waterColor : _waterSurfaceColor, tunnelLight));
            }
            else
            {
                color = material == CellMaterial.GlowMushroom ? _glowMushroomColor
                    : CellMaterials.ShadedColor(material, grid.GetShade(x, y), _cellTiles != null ? _tileToneStrength : 1f);
                // Reboco de sala construída: o bloco do ambiente, sobre a cor da terra (a referência da linha).
                if (_cellTiles != null && RoomLining.TryGetRoom(grid.GetLining(x, y), out PaintRoom lined))
                {
                    tileId = (byte)CellTileArt.RoomBlockRow(CellTileArt.StyleOf(lined));
                    color = CellMaterials.ShadedColor(CellMaterial.Dirt, grid.GetShade(x, y), _tileToneStrength);
                }
                byte wetness = grid.GetWetness(x, y);
                if (wetness != 0)
                    color = Darken(color, Mathf.Max(0.4f, 1f - wetness * _wetDarkening));
                solid = true;
            }
            // O cogumelo luminoso brilha com a própria cor: sai daqui já "iluminado" e o shader não o escurece.
            bool glow = material == CellMaterial.GlowMushroom;
            bool overlay = solid && (grid.IsMarked(x, y) || (_drawnRooms && HasRoomPaint(grid, x, y)));
            lit = _cellTiles != null && (overlay || tunnelPaint || glow);
            if (solid && !glow && (_cellTiles == null || overlay))
                color = Darken(color, LightFactor(light, false));
            // A marca de cavar e a pintura de sala vêm depois da luz: não escurecem com ela.
            if (overlay)
            {
                if (grid.IsMarked(x, y))
                    color = Color32.Lerp(color, _markColor, _markStrength);
                if (_drawnRooms)
                    color = PaintRooms(color, grid, x, y, faintRoom, _nutrientRoomStrength * 0.5f);
            }
            return color;
        }

        /// <summary>Pintura das salas e depósitos por cima da cor: <paramref name="queen"/> é a força da sala da rainha, <paramref name="other"/> a dos demais.</summary>
        Color32 PaintRooms(Color32 color, PixelGrid grid, int x, int y, float queen, float other)
        {
            if (grid.IsQueenRoom(x, y))
                color = Color32.Lerp(color, _queenRoomColor, queen);
            if (grid.IsNutrientRoom(x, y))
                color = Color32.Lerp(color, _nutrientRoomColor, other);
            if (grid.IsNurseryRoom(x, y))
                color = Color32.Lerp(color, _nurseryRoomColor, other);
            if (grid.IsWasteRoom(x, y))
                color = Color32.Lerp(color, WasteColor(grid.GetWasteKind(x, y)), other);
            if (grid.IsDewRoom(x, y))
                color = Color32.Lerp(color, _dewRoomColor, other);
            if (grid.IsFarmRoom(x, y))
                color = Color32.Lerp(color, _farmRoomColor, other);
            StorageKind store = grid.GetStorageRoom(x, y);
            if (store != StorageKind.None)
                color = Color32.Lerp(color, StoreColor(store), other);
            return color;
        }

        /// <summary>Célula pintada como sala ou depósito (qualquer ambiente).</summary>
        static bool HasRoomPaint(PixelGrid grid, int x, int y) =>
            grid.IsQueenRoom(x, y) || grid.IsNutrientRoom(x, y) || grid.IsNurseryRoom(x, y) ||
            grid.IsWasteRoom(x, y) || grid.IsDewRoom(x, y) || grid.IsFarmRoom(x, y) || grid.GetStorageRoom(x, y) != StorageKind.None;

        Color32 StoreColor(StorageKind kind)
        {
            switch (kind)
            {
                case StorageKind.Food: return _foodStoreColor;
                case StorageKind.Blocks: return _blockStoreColor;
                case StorageKind.Sand: return _sandStoreColor;
                case StorageKind.Mushroom: return _mushroomStoreColor;
                default: return _waterStoreColor;
            }
        }

        /// <summary>
        /// Item guardado (depósito ou resíduo): a cor do material com o tom dele, misturada com o fundo para não parecer
        /// parede (a célula continua livre para passar); a água guardada usa a cor da água, com um granulado por posição.
        /// </summary>
        /// <param name="light">Luz já aplicada ao fundo (1 = o shader ilumina): o item recebe a mesma.</param>
        Color32 StoredAt(Color32 background, byte item, byte shade, int x, int y, float light)
        {
            if (item == StoredItems.Water)
            {
                int n = ((x * 19349663) ^ (y * 83492791)) & 15;
                Color32 water = new Color32((byte)(_waterColor.r + n), (byte)(_waterColor.g + n), (byte)Mathf.Min(255, _waterColor.b + n), 255);
                return Color32.Lerp(background, Darken(water, light), _storedStrength);
            }
            return Color32.Lerp(background, Darken(CellMaterials.ShadedColor(StoredItems.MaterialOf(item), shade), light), _storedStrength);
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

        // Put/Blend recebem coordenadas de célula da grade e descartam o que cai fora da janela. O que elas desenham (pincel,
        // estacas, gotas, formigas de fallback) fica acima da luz: LightUnderOverlay ilumina o fundo antes.
        void Put(int x, int y, Color32 color)
        {
            x -= _originX;
            y -= _originY;
            if ((uint)x < (uint)_width && (uint)y < (uint)_height)
            {
                LightUnderOverlay(y * _width + x);
                _pixels[y * _width + x] = color;
            }
        }

        void Blend(int x, int y, Color32 color, float t)
        {
            x -= _originX;
            y -= _originY;
            if ((uint)x < (uint)_width && (uint)y < (uint)_height)
            {
                LightUnderOverlay(y * _width + x);
                _pixels[y * _width + x] = Color32.Lerp(_pixels[y * _width + x], color, t);
            }
        }

        /// <summary>Como <see cref="Blend"/>, mas sobre céu transparente pinta a cor com alfa (o cenário aparece atrás).</summary>
        void BlendOver(int x, int y, Color32 color, float t)
        {
            x -= _originX;
            y -= _originY;
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height)
                return;
            int i = y * _width + x;
            LightUnderOverlay(i);
            Color32 under = _pixels[i];
            if (under.a == 0)
                _pixels[i] = new Color32(color.r, color.g, color.b, (byte)(255 * t));
            else
                _pixels[i] = Color32.Lerp(under, new Color32(color.r, color.g, color.b, under.a), t);
        }

        // Cor e luz vêm dos campos serializados: mudar no Inspector com o jogo rodando redesenha tudo.
        void OnValidate() => _cachedGrid = null;

        void OnDestroy()
        {
            ReleaseTexture();
            if (_tileInstance != null)
                Destroy(_tileInstance);
            _atlas?.Destroy();
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
