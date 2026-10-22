using Fabula.Incrementant.Texts;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using Fabula.Incrementant.Audio;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Diagnostics;
using Fabula.Incrementant.Juice;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.Settings;
using Fabula.Incrementant.UI;
using Fabula.Incrementant.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>De onde vem o terreno ao dar Play. Serializado como int; não reordene.</summary>
    public enum MapSource
    {
        /// <summary>Gerado em runtime por <see cref="TerrainGenerator"/> (ninho e despejo também).</summary>
        Procedural = 0,
        /// <summary>Lido da imagem da cena, com ninho e despejo pelos marcadores.</summary>
        Image = 1,
    }

    /// <summary>
    /// Ponto de entrada do protótipo do formigário: monta o mapa (procedural ou imagem da cena),
    /// move a câmera (limitada ao mundo) e gera o mapa procedural sob demanda conforme ela se aproxima das bordas,
    /// roda a simulação em passo fixo, lê o input de marcação e desenha o HUD (IMGUI, provisório).
    /// </summary>
    public sealed class AntFarmGame : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        [SerializeField] AntFarmView _view;
        [Tooltip("Formigas em sprite (arte em resolução maior que o terreno). Vazio = formigas de 3 pixels na textura.")]
        [SerializeField] AntSwarmView _antsView;
        [SerializeField] BroodView _broodView;
        [Tooltip("Animação dos canteiros da sala de nutrientes. Vazio: é criado como filho deste objeto ao iniciar.")]
        [SerializeField] CultivationView _cultivationView;
        [Tooltip("Cenário da superfície na escala das formigas. Vazio = só o céu em gradiente da textura do terreno.")]
        [SerializeField] SurfaceSceneryView _scenery;
        [Tooltip("Procedural: mapa novo gerado no Play. Image: usa a imagem e os marcadores abaixo.")]
        [SerializeField] MapSource _mapSource = MapSource.Procedural;
        [Header("Mapa por imagem (modo Image)")]
        [Tooltip("Imagem do mapa (1 pixel = 1 célula; cores da paleta de TerrainMapCodec). Precisa de Read/Write ativo.")]
        [SerializeField] Texture2D _map;
        [Tooltip("Onde as formigas nascem. Posição em unidades de mundo (1 unidade = 1 célula).")]
        [SerializeField] Transform _nestMarker;
        [Tooltip("Início e fim (em X) da faixa onde as formigas despejam a terra.")]
        [SerializeField] Transform _dumpStartMarker;
        [SerializeField] Transform _dumpEndMarker;
        [Header("Geração procedural (modo Procedural)")]
        [SerializeField] TerrainSettings _terrainSettings = new TerrainSettings();
        [SerializeField] int _seed = 1234;
        [SerializeField] bool _randomizeSeed = true;
        [Header("Simulação")]
        [SerializeField] ColonySettings _colonySettings = new ColonySettings();
        [Tooltip("Ataques de cupins (a partir de Min Ants formigas, a cada 1 a 5 minutos). Desligado, nunca vêm.")]
        [SerializeField] bool _termiteRaids = true;
        [SerializeField] RaidSettings _raidSettings = new RaidSettings();
        [SerializeField, Min(1f)] float _ticksPerSecond = 60f;
        [Tooltip("Limite de passos por frame, para um frame lento não virar uma espiral de atraso.")]
        [SerializeField, Min(1)] int _maxTicksPerFrame = 12;
        [Tooltip("Tempo máximo de simulação por frame (ms). Quando um passo custa caro (chuva forte no 8x), o frame para de " +
                 "simular ao chegar aqui e o jogo anda mais devagar que a velocidade escolhida, em vez de derrubar o FPS.")]
        [SerializeField, Min(1f)] float _maxSimMsPerFrame = 25f;
        [SerializeField, Range(1, 30)] int _brushRadius = 4;
        [Header("Chuva")]
        [Tooltip("Tempo ao dar Play e ao gerar outro mapa. No Play, troque com C ou pelo HUD.")]
        [SerializeField] Weather _weather = Weather.Clear;
        [SerializeField] RainSettings _rainSettings = new RainSettings();
        [Header("Água")]
        [SerializeField] WaterSettings _waterSettings = new WaterSettings();
        [Header("Vegetação")]
        [Tooltip("Grama e mudas da superfície que crescem com o tempo e são colhidas pelas jardineiras para virar nutrientes.")]
        [SerializeField] VegetationSettings _vegetationSettings = new VegetationSettings();
        [Header("Câmera")]
        [Tooltip("Folga, em células, do enquadramento inicial e acima do topo do mundo.")]
        [SerializeField, Min(0f)] float _cameraMargin = 4f;
        [Tooltip("Quanto a câmera sobe acima do topo do mundo, em células (além da folga): espaço de céu para elementos grandes do cenário.")]
        [SerializeField, Min(0f)] float _cameraSkyHeadroom = 120f;
        [SerializeField] MapCamera _cameraControl = new MapCamera();
        [Header("Som espacial da colônia")]
        [Tooltip("Até onde o som da colônia é ouvido, em meias-telas a partir do centro da câmera (1 = borda da vista). O alcance acompanha o zoom.")]
        [SerializeField, Min(0.5f)] float _sfxHearingRange = 1.5f;
        [Tooltip("Parte da meia-tela, a partir do centro da câmera, em que o som toca no volume cheio.")]
        [SerializeField, Range(0f, 1f)] float _sfxFullVolumeRadius = 0.3f;
        [Tooltip("Meia altura da vista (células) até a qual o som não perde volume pelo zoom; mais afastada, cai na mesma proporção (o dobro da vista, metade do volume).")]
        [SerializeField, Min(1f)] float _sfxNearHalfHeight = 60f;
        [Tooltip("Volume mínimo dos sons com a câmera toda afastada.")]
        [SerializeField, Range(0f, 1f)] float _sfxFarZoomVolume = 0.1f;
        [Tooltip("Quanto o lado da tela desloca o som entre os alto-falantes (0 = sem estéreo).")]
        [SerializeField, Range(0f, 1f)] float _sfxPanAmount = 0.6f;
        [Header("Vitrine do menu principal")]
        [Tooltip("Cena do menu principal: o formigueiro já vem escavado e a colônia trabalha sozinha, sem input, cronômetro nem HUD. Desligado na cena da partida.")]
        [SerializeField] bool _showcase;
        [Tooltip("Meia altura da vista na vitrine, em células.")]
        [SerializeField, Min(8f)] float _showcaseHalfHeight = 76f;
        [Tooltip("Onde o ninho fica na tela da vitrine (0..1, a partir do canto inferior esquerdo).")]
        [SerializeField] Vector2 _showcaseNestOnScreen = new Vector2(0.70f, 0.45f);

        static readonly int[] SpeedSteps = { 1, 3, 8 };
        /// <summary>
        /// O que o pincel faz: marcar terra para as formigas cavarem, espaço vazio para elas preencherem, a sala onde
        /// a rainha pode ficar ou a sala onde os nutrientes são guardados. Plantas não se marcam: as jardineiras escolhem
        /// sozinhas o que colher. A ordem casa com <see cref="BrushTint"/>.
        /// </summary>
        enum BrushMode
        {
            Dig, Build, QueenRoom, NutrientRoom, NurseryRoom, WasteRoom, Finger, DewRoom, FoodStore, BlockStore, WaterStore, GlowMushroom,
            FarmRoom, SandStore, MushroomStore,
        }

        static readonly string[] BrushLabels =
        {
            "Cavar", "Construir", "Sala da rainha", "Sala de nutrientes", "Berçário", "Resíduos", "Dedo", "Gotas de orvalho",
            "Depósito de alimentos", "Depósito de construção", "Depósito de água", "Cogumelo luminoso", "Fazenda de pulgões",
            "Depósito de areia", "Depósito de cogumelos",
        };
        static readonly string[] WeatherLabels = { "Seco", "Chuva leve", "Chuva forte" };

        PixelGrid _grid;
        GranularSimulation _simulation;
        RainSimulation _rain;
        WaterSimulation _water;
        VegetationSimulation _vegetation;
        AntColony _colony;
        MenuShowcase _showcaseDirector;
        TerrainLayout _layout;
        TerrainGenerator _generator;
        RectInt _startArea;
        float _accumulator;
        int _speedIndex;
        Vector2Int _brushCell;
        BrushMode _brushMode;
        bool _pointerOverWorld;
        bool _paused;
        /// <summary>HUD de depuração (IMGUI, com contadores de simulação); o HUD de jogo é o <c>AntFarmHud</c> (uGUI). F2 alterna (F1 é o cheat de formigas).</summary>
        [SerializeField] bool _showDebugHud;
        Rect _hudRect = new Rect(10, 10, 340, 640);
        /// <summary>Painel de estatísticas de desempenho e simulação (F11); desligado por padrão.</summary>
        bool _showStats;
        /// <summary>Linhas dos blocos (<see cref="PixelGrid.ChunkSize"/> células) por cima do mapa (Ctrl+F11), para depuração.</summary>
        bool _showChunkLines;
        static readonly Color ChunkLineColor = new Color(0.2f, 0.9f, 1f, 0.35f);
        static readonly Color ChunkLineMajorColor = new Color(1f, 0.85f, 0.2f, 0.6f);
        /// <summary>De quantos em quantos blocos a linha é destacada (ajuda a contar).</summary>
        const int ChunkMajorEvery = 4;
        readonly SimStatsOverlay _statsOverlay = new SimStatsOverlay();
        readonly TermiteMarkers _termiteMarkers = new TermiteMarkers();

        void OnDestroy()
        {
            _termiteMarkers.Destroy();
            if (_colony != null)
            {
                _colony.Sfx -= PlayColonySfx;
                _colony.Fx -= PlayColonyFx;
                _colony.NutrientsSpent -= RelayNutrientsSpent;
            }
            // O serviço de áudio sobrevive às cenas: a chuva da partida não pode continuar no menu.
            GameAudio.SetLoop(SfxId.Rain, 0f);
        }

        /// <summary>Som da colônia: o que tem célula soa pela distância ao centro da câmera e pelo zoom; o resto, igual em todo o mapa.</summary>
        void PlayColonySfx(SfxId id, Vector2Int? cell)
        {
            if (cell == null || _camera == null)
            {
                GameAudio.Play(id);
                return;
            }
            float volume = SpatialSfxVolume(cell.Value, out float pan);
            GameAudio.Play(id, volume, pan);
        }

        /// <summary>
        /// Volume (0 a 1) e lado de um som na célula, pela vista atual. A distância é medida em meias-telas (elipse da vista),
        /// então aproximar o zoom encolhe o alcance em células; o zoom afastado ainda abaixa tudo, para a colônia inteira
        /// à vista não virar um ruído só.
        /// </summary>
        float SpatialSfxVolume(Vector2Int cell, out float pan)
        {
            float halfHeight = Mathf.Max(0.01f, _camera.orthographicSize);
            float halfWidth = halfHeight * Mathf.Max(0.01f, _camera.aspect);
            Vector3 offset = CellToWorld(cell) - _camera.transform.position;
            float dx = offset.x / halfWidth, dy = offset.y / halfHeight;
            pan = Mathf.Clamp(dx, -1f, 1f) * _sfxPanAmount;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            float reach = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_sfxFullVolumeRadius, _sfxHearingRange, distance));
            float zoom = Mathf.Clamp(_sfxNearHalfHeight / halfHeight, _sfxFarZoomVolume, 1f);
            return reach * zoom;
        }

        WorldFx _worldFx;

        /// <summary>Repasse dos fatos visuais da colônia para quem desenha por cima (HUD: números flutuantes). Só na partida, não no menu.</summary>
        public event Action<FxId, Vector2Int, CellMaterial> ColonyFx;

        /// <summary>Câmera do mundo (para ancorar textos da UI em células).</summary>
        public Camera WorldCamera => _camera;

        /// <summary>Centro da célula no mundo.</summary>
        public Vector3 CellToWorld(Vector2Int cell) => _view.transform.position + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

        void PlayColonyFx(FxId id, Vector2Int cell, CellMaterial material)
        {
            _worldFx?.Play(id, cell, material);
            ColonyFx?.Invoke(id, cell, material);
        }

        /// <summary>Repasse de <c>AntColony.NutrientsSpent</c> (quanto foi gasto) para o HUD. Só na partida.</summary>
        public event Action<int> ColonyNutrientsSpent;

        void RelayNutrientsSpent(int amount) => ColonyNutrientsSpent?.Invoke(amount);

        /// <summary>Clique no mundo recusado (na célula dada): par visual do <c>SfxId.UiDeny</c>, desenhado pelo HUD.</summary>
        public event Action<Vector2Int> WorldDenied;

        void DenyAt(Vector2Int cell)
        {
            GameAudio.Play(SfxId.UiDeny);
            WorldDenied?.Invoke(cell);
        }

        /// <summary>Chuva em laço conforme o clima (nada no fundo do menu); o volume de efeitos é aplicado pelo <see cref="GameAudio"/>.</summary>
        void ApplyWeatherSound()
        {
            float level = _showcase ? 0f : _weather == Weather.Clear ? 0f : _weather == Weather.LightRain ? 0.35f : 0.8f;
            GameAudio.SetLoop(SfxId.Rain, level);
        }
        const float StatsPanelWidth = 430f;
        const float StatsPanelHeight = 800f;
        readonly MatchTimer _matchTimer = new MatchTimer();
        bool _gameOverRecorded;
        // Barras de progresso do que é produzido (KI-026); criado ao iniciar, sem referência na cena.
        ProgressBarsView _progressBars;
        // Abaixo disto (pixels da tela por célula; o zoom inicial é 16) as barras somem: de longe viram riscos ilegíveis.
        const float BarsMinPixelsPerCell = 10f;
        string _saveStatus;
        float _saveStatusUntil;

        readonly Stopwatch _stopwatch = new Stopwatch();
        float _simMsSmoothed;
        float _renderMsSmoothed;

        public PixelGrid Grid => _grid;
        public AntColony Colony => _colony;
        public MapSource Source => _mapSource;
        public Texture2D Map => _map;
        public AntFarmView View => _view;
        public MatchTimer Timer => _matchTimer;
        public TerrainLayout Layout => _layout;

        /// <summary>Sobe a cada mapa novo ou save carregado: quem guarda posições do mundo (tutorial) descarta o que tinha.</summary>
        public int Generation { get; private set; }

        /// <summary>A partida começou de um save (Continuar no menu principal): o tutorial não roda.</summary>
        public bool StartedFromSave { get; private set; }

        /// <summary>Vitrine do menu principal: sem jogador, sem HUD de partida.</summary>
        public bool Showcase => _showcase;

        // ---------------------------------------------------------------- enquadramento da câmera (tutorial)

        /// <summary>Meia altura da vista (células) que enquadra <paramref name="cells"/> ocupando <paramref name="fill"/> da tela.</summary>
        float FitHalfHeight(Rect cells, float fill)
        {
            float aspect = Mathf.Max(0.01f, _camera.aspect);
            return Mathf.Max(cells.height * 0.5f / fill, cells.width * 0.5f / (fill * aspect));
        }

        /// <summary>
        /// Desloca a câmera com suavidade até enquadrar <paramref name="cells"/> (em células da grade), ocupando
        /// <paramref name="fill"/> da tela, com o centro dele em <paramref name="screenCenter01"/> (0..1 da tela).
        /// </summary>
        public void FrameCells(Rect cells, float seconds, float fill = 0.5f, Vector2? screenCenter01 = null)
        {
            Vector2 at = screenCenter01 ?? new Vector2(0.5f, 0.5f);
            _cameraControl.HalfHeightRange(Mathf.Max(0.01f, _camera.aspect), out float min, out float max);
            float half = Mathf.Clamp(FitHalfHeight(cells, fill), min, max);
            float aspect = Mathf.Max(0.01f, _camera.aspect);
            Vector3 origin = _view.transform.position;
            var center = new Vector2(origin.x + cells.center.x - (at.x - 0.5f) * 2f * half * aspect,
                                     origin.y + cells.center.y - (at.y - 0.5f) * 2f * half);
            _cameraControl.MoveTo(_camera, center, half, seconds);
        }

        /// <summary>Começa pela vista mais aberta e desce suavemente até <see cref="FrameCells"/>.</summary>
        public void FrameCellsFromOverview(Rect cells, float seconds, float fill = 0.5f, Vector2? screenCenter01 = null)
        {
            _cameraControl.SnapToOverview(_camera);
            FrameCells(cells, seconds, fill, screenCenter01);
        }

        public static int SpeedCount => SpeedSteps.Length;
        public static int SpeedMultiplier(int index) => SpeedSteps[index];
        public int SpeedIndex => _speedIndex;
        public bool Paused => _paused;

        /// <summary>Escolhe a velocidade (índice de <see cref="SpeedMultiplier"/>) e retoma a partida se estava pausada.</summary>
        public void SetSpeed(int index)
        {
            _speedIndex = Mathf.Clamp(index, 0, SpeedSteps.Length - 1);
            _paused = false;
        }

        /// <summary>Pausa a simulação (o cronômetro, a câmera e o input seguem em tempo real).</summary>
        public void SetPaused(bool paused) => _paused = paused;

        /// <summary>
        /// Com um menu em tela cheia aberto (ESC), atalhos de teclado, câmera e pincel ficam travados. Quem abre o menu
        /// liga e quem fecha desliga.
        /// </summary>
        public bool InputLocked { get; set; }

        public static int BrushCount => BrushLabels.Length;
        public static string BrushLabel(int index) => BrushLabels[index];

        /// <summary>O pincel pinta um dos depósitos (alimentos, construção, água): mora no submenu DEPÓSITO do HUD.</summary>
        /// <summary>
        /// Modo com que o pincel Resíduos pinta (submenu RESÍDUOS do HUD). Cada área guarda o modo com que foi pintada;
        /// pintar por cima com outro modo troca o da célula. Não vai para o save (volta ao padrão).
        /// </summary>
        public WasteMode WasteBrushMode { get; set; }

        /// <summary>Índice do pincel Resíduos (o HUD abre o submenu do modo da área com ele escolhido).</summary>
        public const int WasteBrushIndex = (int)BrushMode.WasteRoom;

        public static bool IsDepotBrush(int index) =>
            index >= (int)BrushMode.FoodStore && index <= (int)BrushMode.WaterStore || index == (int)BrushMode.SandStore ||
            index == (int)BrushMode.MushroomStore;

        /// <summary>Índice do pincel da fazenda de pulgões (liberado pela câmara A0).</summary>
        public const int FarmBrushIndex = (int)BrushMode.FarmRoom;

        /// <summary>O ambiente com limite de área (KI-043) que o pincel pinta; false para cavar, construir e dedo.</summary>
        public static bool TryGetPaintRoom(int index, out PaintRoom room)
        {
            switch ((BrushMode)index)
            {
                case BrushMode.QueenRoom: room = PaintRoom.Queen; return true;
                case BrushMode.NutrientRoom: room = PaintRoom.Nutrient; return true;
                case BrushMode.NurseryRoom: room = PaintRoom.Nursery; return true;
                case BrushMode.WasteRoom: room = PaintRoom.Waste; return true;
                case BrushMode.DewRoom: room = PaintRoom.Dew; return true;
                case BrushMode.FoodStore: room = PaintRoom.FoodStore; return true;
                case BrushMode.BlockStore: room = PaintRoom.BlockStore; return true;
                case BrushMode.WaterStore: room = PaintRoom.WaterStore; return true;
                case BrushMode.FarmRoom: room = PaintRoom.Farm; return true;
                case BrushMode.SandStore: room = PaintRoom.SandStore; return true;
                case BrushMode.MushroomStore: room = PaintRoom.MushroomStore; return true;
                default: room = default; return false;
            }
        }
        public int BrushIndex => (int)_brushMode;

        /// <summary>O pincel atual é o dedo (o HUD troca o cursor do sistema enquanto ele estiver ativo).</summary>
        public bool FingerSelected => _brushMode == BrushMode.Finger;

        /// <summary>O ponteiro está sobre o mapa, e não sobre o HUD, e o input está liberado.</summary>
        public bool PointerOverWorld => _pointerOverWorld;

        /// <summary>
        /// O pincel pode ser usado agora: a área de resíduos só depois do primeiro ataque de cupins; a fazenda de pulgões só com
        /// a câmara A0 comprada.
        /// </summary>
        public bool IsBrushUnlocked(int index)
        {
            switch ((BrushMode)index)
            {
                case BrushMode.WasteRoom: return _colony != null && _colony.WasteUnlocked;
                case BrushMode.FarmRoom: return _colony != null && _colony.FarmUnlocked;
                default: return true;
            }
        }

        public void SetBrush(int index)
        {
            index = Mathf.Clamp(index, 0, BrushLabels.Length - 1);
            if (IsBrushUnlocked(index))
                _brushMode = (BrushMode)index;
        }

        /// <summary>Último aviso de salvar/carregar enquanto ainda vale (5 s); vazio depois.</summary>
        public string SaveStatus => !string.IsNullOrEmpty(_saveStatus) && Time.unscaledTime < _saveStatusUntil ? _saveStatus : string.Empty;

        void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null || _view == null)
            {
                Debug.LogError($"{name}: AntFarmGame precisa de uma Camera e de um AntFarmView atribuídos no Inspector " +
                               "(rode Incrementant/Setup/Criar cena AntFarm).", this);
                enabled = false;
                return;
            }
            if (_mapSource == MapSource.Image && !ValidateMapReferences())
            {
                enabled = false;
                return;
            }
            if (_cultivationView == null)
            {
                // Cenas montadas antes dos canteiros não têm o componente: os sprites são gerados por código, então basta criá-lo.
                var cultivation = new GameObject("Cultivation");
                cultivation.transform.SetParent(transform, false);
                _cultivationView = cultivation.AddComponent<CultivationView>();
            }
            if (!_showcase)
            {
                var bars = new GameObject("ProgressBars");
                bars.transform.SetParent(transform, false);
                _progressBars = bars.AddComponent<ProgressBarsView>();
            }
            // Continuar (menu principal): o save é lido antes de gerar o mundo, para não gerar o mapa duas vezes.
            GameSnapshot continued = _showcase ? null : ReadSaveRequestedByMenu();
            StartedFromSave = continued != null;
            Regenerate(continued);
        }

        /// <summary>O save rápido, se o menu principal pediu Continuar e ele serve; senão null (colônia nova).</summary>
        GameSnapshot ReadSaveRequestedByMenu()
        {
            if (!Menu.GameLaunch.ConsumeContinue())
                return null;
            try
            {
                string path = SaveStorage.QuickSavePath;
                if (!System.IO.File.Exists(path))
                    return null;
                GameSnapshot snapshot = SaveStorage.Read(path);
                string problem = DescribeIncompatibility(snapshot);
                if (problem != null)
                {
                    Debug.LogWarning($"{name}: o save não pôde ser continuado ({problem}); começando uma colônia nova.", this);
                    return null;
                }
                ShowSaveStatus(Loc.T("save.loaded", MatchTimer.Format(snapshot.MatchSeconds)));
                return snapshot;
            }
            catch (Exception e)
            {
                Debug.LogError($"{name}: falha ao continuar de '{SaveStorage.QuickSavePath}': {e}", this);
                return null;
            }
        }

        bool ValidateMapReferences()
        {
            if (_map == null)
            {
                Debug.LogError($"{name}: modo de mapa Image sem imagem atribuída em Map " +
                               "(atribua um PNG de Assets/Project/Art/Maps ou troque para Procedural).", this);
                return false;
            }
            if (!_map.isReadable)
            {
                Debug.LogError($"{name}: o mapa '{_map.name}' precisa de Read/Write ativo no importador " +
                               "(mapas em Assets/Project/Art/Maps recebem isso automaticamente).", _map);
                return false;
            }
            if (_nestMarker == null || _dumpStartMarker == null || _dumpEndMarker == null)
            {
                Debug.LogError($"{name}: no modo Image, os marcadores de ninho e de despejo são obrigatórios " +
                               "(rode Incrementant/Setup/Criar cena AntFarm).", this);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gera um mapa procedural (ou recarrega a imagem da cena) e reinicia a colônia e o cronômetro. Com
        /// <paramref name="snapshot"/>, usa a seed do save e aplica o estado salvo por cima do mapa regenerado.
        /// </summary>
        void Regenerate(GameSnapshot snapshot = null)
        {
            // A colônia que sai de cena entra no histórico de durações (KI-040); a que acabou já entrou em Update.
            if (!_showcase && _colony != null && !_gameOverRecorded)
                ColonyHistory.Record(_seed, _matchTimer.Seconds, _colony.GameOver);
            _gameOverRecorded = false;
            if (snapshot != null)
                _seed = snapshot.Seed;
            else if (_randomizeSeed)
                _seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            if (_mapSource == MapSource.Image)
            {
                _grid = TerrainMapCodec.Decode(_map.GetPixels32(), _map.width, _map.height);
                _layout = LayoutFromMarkers();
                _generator = null;
                _startArea = new RectInt(0, 0, _grid.Width, _grid.Height);
            }
            else
            {
                // O mundo inteiro é alocado de uma vez, mas só a área inicial é gerada; o resto vem com a câmera.
                _startArea = _terrainSettings.StartArea;
                _grid = new PixelGrid(_terrainSettings.WorldWidth, _terrainSettings.WorldHeight);
                _generator = new TerrainGenerator(_terrainSettings, _seed, _grid.Width, _grid.Height, _startArea, _terrainSettings.SkyHeadroom);
                _layout = _generator.Begin(_grid);
            }
            _simulation = new GranularSimulation();
            // O fundo do menu não tem tempestade automática (e nem cupins).
            _rain = new RainSimulation(_rainSettings, _seed) { Weather = _weather, AutomaticStorms = _rainSettings.AutomaticStorms && !_showcase };
            _water = new WaterSimulation(_waterSettings, _grid, _seed);
            snapshot?.ApplyTo(_grid);
            snapshot?.ApplyTo(_rain);
            snapshot?.ApplyTo(_water);
            _weather = _rain.Weather;
            // As plantas nascem em EnsureGenerated (Populate), coluna a coluna; as do save já estão nas células. Com o
            // cenário, a forma de cada planta na grade é a silhueta do sprite de grama/trevo sorteado para ela.
            _vegetation = _scenery != null
                ? new VegetationSimulation(_vegetationSettings, _seed, _layout, _grid.Width,
                                           _scenery.PlantShapes(PlantKind.Grass), _scenery.PlantShapes(PlantKind.Leafy))
                : new VegetationSimulation(_vegetationSettings, _seed, _layout, _grid.Width);
            if (snapshot != null)
            {
                _vegetation.MarkPopulated(_grid.Active);
                snapshot.ApplyTo(_vegetation);
            }
            if (_colony != null)
            {
                _colony.Sfx -= PlayColonySfx;
                _colony.Fx -= PlayColonyFx;
                _colony.NutrientsSpent -= RelayNutrientsSpent;
            }
            _colony = new AntColony(_grid, _colonySettings, _layout, _seed)
            {
                RaidSettings = _raidSettings, RaidsEnabled = _termiteRaids,
            };
            _colony.AttachVegetation(_vegetation);
            snapshot?.ApplyTo(_colony);
            // O fundo do menu é mudo; na partida a colônia avisa os sons e o jogo toca.
            if (!_showcase)
            {
                _colony.Sfx += PlayColonySfx;
                if (_worldFx == null)
                    _worldFx = WorldFx.Create(transform, _camera, _view.transform);
                _colony.Fx += PlayColonyFx;
                _colony.NutrientsSpent += RelayNutrientsSpent;
            }
            ApplyWeatherSound();
            _showcaseDirector = _showcase ? MenuShowcase.Prepare(_grid, _colony, _layout, _seed) : null;
            // Mapa novo ou save sem área de resíduos liberada: o pincel volta a um que existe.
            if (!IsBrushUnlocked((int)_brushMode))
                _brushMode = BrushMode.Dig;
            _matchTimer.Set(snapshot?.MatchSeconds ?? 0.0);
            _accumulator = 0f;
            Generation++;

            Vector3 origin = _view.transform.position;
            float skyAbove = _cameraMargin + _cameraSkyHeadroom;
            var bounds = new Rect(origin.x, origin.y, _grid.Width, _grid.Height + skyAbove);
            // A vista inicial enquadra só o solo e o céu de sempre; as linhas de céu extras (SkyHeadroom) ficam para cima.
            int skyHeadroom = _generator != null ? _terrainSettings.SkyHeadroom : 0;
            var startView = new Rect(origin.x + _startArea.x, origin.y + _startArea.y, _startArea.width, _startArea.height - skyHeadroom);
            // Começa perto, no ninho (zoom inicial por célula em MapCamera); o zoom abre até a área inicial inteira.
            var focus = new Vector2(origin.x + _layout.Nest.x + 0.5f, origin.y + _layout.Nest.y + 0.5f);
            _cameraControl.Reset(_camera, bounds, startView, _cameraMargin, focus);
            if (_showcase)
                FrameShowcase(origin);

            _view.Initialize(_grid, transparentSky: _scenery != null, skyBaseY: _startArea.yMin, plantsAsSprites: _scenery != null);
            if (_antsView != null)
                _antsView.SetTileAtlas(_view.TileAtlas, _view.TileToneStrength);
            if (_scenery != null)
            {
                // Laguinhos: no procedural vêm do gerador (inclusive os ainda não gerados); na imagem, da água pintada.
                TerrainGenerator generator = _generator;
                PixelGrid grid = _grid;
                Func<int, bool> isLake = generator != null
                    ? x => generator.GetLakeLevel(x) >= 0
                    : (Func<int, bool>)(x => grid.HasWater(x, grid.GetSurfaceY(x) + 1));
                _scenery.Build(_grid, _layout, _seed, origin, _startArea.yMin, _camera.transform.position, isLake,
                               Mathf.CeilToInt(skyAbove) + 1);
            }
            EnsureGenerated();
        }

        /// <summary>
        /// Gera os blocos do mundo que a vista (mais a folga) alcança e ainda não existem. A área ativa é sempre um
        /// retângulo que vai até o topo: cresce para os lados e para baixo, até os limites do mundo.
        /// </summary>
        void EnsureGenerated()
        {
            if (_generator != null)
                ExpandToView();
            _vegetation.Populate(_grid);
        }

        void ExpandToView()
        {
            Rect view = LocalViewRect();
            int chunk = _terrainSettings.ChunkSize;
            int margin = _terrainSettings.GenerationMargin;
            int xMin = Mathf.Max(0, FloorTo(Mathf.FloorToInt(view.xMin) - margin, chunk));
            int xMax = Mathf.Min(_grid.Width, CeilTo(Mathf.CeilToInt(view.xMax) + margin, chunk));
            int yMin = Mathf.Max(0, FloorTo(Mathf.FloorToInt(view.yMin) - margin, chunk));

            _generator.Expand(_grid, new RectInt(xMin, yMin, xMax - xMin, _grid.Height - yMin));
        }

        /// <summary>Enquadra o ninho na posição de tela da vitrine, já no zoom dela (sem transição visível: a primeira vista já é a final).</summary>
        void FrameShowcase(Vector3 origin)
        {
            float aspect = Mathf.Max(0.01f, _camera.aspect);
            _cameraControl.HalfHeightRange(aspect, out float min, out float max);
            float half = Mathf.Clamp(_showcaseHalfHeight, min, max);
            Vector2 nest = new Vector2(origin.x + _layout.Nest.x + 0.5f, origin.y + _layout.Nest.y + 0.5f);
            var center = new Vector2(nest.x - (_showcaseNestOnScreen.x - 0.5f) * 2f * half * aspect,
                                     nest.y - (_showcaseNestOnScreen.y - 0.5f) * 2f * half);
            _cameraControl.MoveTo(_camera, center, half, 0.01f);
        }

        static int FloorTo(int value, int step) => Mathf.FloorToInt(value / (float)step) * step;

        static int CeilTo(int value, int step) => Mathf.CeilToInt(value / (float)step) * step;

        /// <summary>Vista da câmera em células, relativa à origem do mapa.</summary>
        Rect LocalViewRect()
        {
            Rect view = MapCamera.ViewRect(_camera);
            Vector3 origin = _view.transform.position;
            view.position -= new Vector2(origin.x, origin.y);
            return view;
        }

        // ---------------------------------------------------------------- save / load

        /// <summary>Grava a partida no save rápido. Devolve false (com a causa no HUD) se não conseguiu.</summary>
        public bool SaveGame()
        {
            if (_colony == null)
                return false;
            try
            {
                GameSnapshot snapshot = GameSnapshot.Capture(_grid, _colony, _rain, _water, (int)_mapSource, _seed, _matchTimer.Seconds,
                                                             _vegetation);
                SaveStorage.Write(SaveStorage.QuickSavePath, snapshot);
                ShowSaveStatus(Loc.T("save.saved", MatchTimer.Format(_matchTimer.Seconds)));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"{name}: falha ao salvar em '{SaveStorage.QuickSavePath}': {e}", this);
                ShowSaveStatus(Loc.T("save.failsave", e.Message));
                return false;
            }
        }

        /// <summary>
        /// Carrega o save rápido. O arquivo é lido e conferido antes de mexer na partida atual: se não servir
        /// (inexistente, corrompido, de outro modo de mapa ou tamanho de mundo), a partida continua intacta.
        /// </summary>
        public bool LoadGame()
        {
            if (_colony == null)
                return false;
            try
            {
                string path = SaveStorage.QuickSavePath;
                if (!System.IO.File.Exists(path))
                {
                    ShowSaveStatus(Loc.T("save.none"));
                    return false;
                }
                GameSnapshot snapshot = SaveStorage.Read(path);
                string problem = DescribeIncompatibility(snapshot);
                if (problem != null)
                {
                    ShowSaveStatus(problem);
                    return false;
                }
                Regenerate(snapshot);
                ShowSaveStatus(Loc.T("save.loaded", MatchTimer.Format(_matchTimer.Seconds)));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"{name}: falha ao carregar '{SaveStorage.QuickSavePath}': {e}", this);
                ShowSaveStatus(Loc.T("save.failload", e.Message));
                return false;
            }
        }

        string DescribeIncompatibility(GameSnapshot snapshot)
        {
            if (snapshot.MapSource != (int)_mapSource)
                return Loc.T("save.othermode");
            int width = _mapSource == MapSource.Image ? _map.width : _terrainSettings.WorldWidth;
            int height = _mapSource == MapSource.Image ? _map.height : _terrainSettings.WorldHeight;
            if (snapshot.GridWidth != width || snapshot.GridHeight != height)
                return Loc.T("save.otherworld", snapshot.GridWidth, snapshot.GridHeight, width, height);
            return null;
        }

        void ShowSaveStatus(string message)
        {
            _saveStatus = message;
            _saveStatusUntil = Time.unscaledTime + 5f;
        }

        void Update()
        {
            if (!_showcase && (_colony == null || !_colony.GameOver))
                _matchTimer.Advance(Time.unscaledDeltaTime);
            if (!_showcase && !_gameOverRecorded && _colony != null && _colony.GameOver)
            {
                _gameOverRecorded = true;
                ColonyHistory.Record(_seed, _matchTimer.Seconds, true);
            }
            // O deslocamento automático (tutorial) anda mesmo com o input travado e toma o lugar do input do jogador.
            bool cameraAutomatic = _cameraControl.StepTransition(_camera, Time.unscaledDeltaTime);
            if (!InputLocked && !cameraAutomatic && !_showcase)
                _cameraControl.Update(_camera, Time.unscaledDeltaTime, PointerOverHud());
            EnsureGenerated();
            if (InputLocked || _showcase)
                _pointerOverWorld = false;
            else
                HandleInput();

            float tickDuration = 1f / _ticksPerSecond;
            if (!_paused)
                _accumulator += Time.deltaTime * SpeedSteps[_speedIndex];
            int maxTicks = _maxTicksPerFrame * SpeedSteps[_speedIndex];
            int ticks = 0;

            SimProfiler profiler = _colony.Profiler;
            _stopwatch.Restart();
            bool budgetHit = false;
            while (_accumulator >= tickDuration && ticks < maxTicks)
            {
                // Orçamento de tempo: se mais um passo (pela média dos deste frame) passar do limite, este é o último. O
                // limite só de passos não basta: com o passo caro, 96 passos no 8x davam frames de ~400 ms, cada um
                // pedindo outros 96 (espiral de atraso). O primeiro passo sempre roda, para a simulação nunca travar.
                double elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;
                bool lastByBudget = ticks > 0 && elapsedMs + elapsedMs / ticks > _maxSimMsPerFrame;
                // Só o último passo do frame refaz os campos de navegação (ver AntColony.DeferFieldRefresh).
                _colony.DeferFieldRefresh = !lastByBudget && ticks + 1 < maxTicks && _accumulator - tickDuration >= tickDuration;
                long stageStart = SimProfiler.Now();
                _rain.Step(_grid, tickDuration, SpeedSteps[_speedIndex]);
                _colony.Raining = _rain.Weather == Weather.HeavyRain;
                if (_weather != _rain.Weather)
                {
                    _weather = _rain.Weather;
                    ApplyWeatherSound();
                }
                profiler.Add(ProfileStage.Rain, stageStart);
                stageStart = SimProfiler.Now();
                _water.Step(_grid, tickDuration);
                profiler.Add(ProfileStage.Water, stageStart);
                stageStart = SimProfiler.Now();
                _vegetation.GrowthRate = _colony.Effects.OutdoorGrowthRate;
                _vegetation.Step(_grid, tickDuration);
                profiler.Add(ProfileStage.Vegetation, stageStart);
                stageStart = SimProfiler.Now();
                _simulation.Step(_grid);
                profiler.Add(ProfileStage.Granular, stageStart);
                stageStart = SimProfiler.Now();
                _colony.GameSpeed = SpeedSteps[_speedIndex];
                _colony.Tick(tickDuration);
                _showcaseDirector?.Tick(tickDuration);
                profiler.Add(ProfileStage.ColonyTick, stageStart);
                profiler.Count(ProfileCounter.Ticks);
                _accumulator -= tickDuration;
                ticks++;
                if (lastByBudget)
                {
                    budgetHit = true;
                    break;
                }
            }
            _colony.DeferFieldRefresh = false;
            // O atraso que sobrou é descartado: o jogo fica mais lento que a velocidade pedida, mas o frame não cresce.
            bool tickLimitHit = ticks == maxTicks || budgetHit;
            if (tickLimitHit)
                _accumulator = 0f;
            _statsOverlay.NoteFrame(Time.unscaledDeltaTime, tickLimitHit && !_paused);
            _simMsSmoothed = Mathf.Lerp(_simMsSmoothed, (float)_stopwatch.Elapsed.TotalMilliseconds, 0.1f);

            _stopwatch.Restart();
            bool spriteAnts = _antsView != null && _antsView.HasFrames;
            RectInt window = AntFarmView.WindowFor(LocalViewRect(), _grid);
            long renderStart = SimProfiler.Now();
            _view.WasteBrushKind = (byte)WasteBrushMode;
            // Com "Mostrar ambientes" desligado, a pintura das salas volta enquanto um pincel de ambiente está na mão.
            _view.ShowRooms = GameSettings.Current.ShowRooms || TryGetPaintRoom((int)_brushMode, out _);
            _view.Render(_grid, _colony.Ants, _colony.DumpStrips,_brushCell, _brushMode == BrushMode.Finger ? _colony.RaidSettings.FingerReach : _brushRadius, _pointerOverWorld, !spriteAnts, window, _rain.Drops,
                         (BrushTint)(int)_brushMode, _colony);
            profiler.Add(ProfileStage.RenderTerrain, renderStart);
            renderStart = SimProfiler.Now();
            if (spriteAnts)
            {
                _antsView.SetBrood(_colony.Brood, _broodView);
                _antsView.Render(_grid, _colony.Ants, _view.transform.position, _colony.SimulationTime + _accumulator, Time.deltaTime,
                                 _colony.Queen, _colony.Brood.LayProgress, _colony.Breeding == BreedingState.Available);
                _antsView.RenderTermites(_grid, _colony.Termites, _view.transform.position, _colony.SimulationTime + _accumulator,
                                         Time.deltaTime);
                _antsView.RenderAphids(_grid, _colony.Aphids, _view.transform.position, _colony.SimulationTime + _accumulator,
                                       Time.deltaTime);
                _antsView.RenderSpits(_colony.Spits, _view.transform.position);
            }
            _termiteMarkers.Update(_camera, _view.transform.position, _colony.Termites, _colony.SimulationTime + _accumulator);
            profiler.Add(ProfileStage.RenderAnts, renderStart);
            renderStart = SimProfiler.Now();
            if (_broodView != null && _broodView.HasFrames)
                _broodView.Render(_colony.Brood.Brood, _colony.NurserySpots, _view.transform.position);
            _cultivationView.Render(_grid, _colony.NutrientCureSeconds, _view.transform.position, Time.time);
            RenderProgressBars(_view.transform.position);
            profiler.Add(ProfileStage.RenderBrood, renderStart);
            renderStart = SimProfiler.Now();
            if (_scenery != null)
            {
                _scenery.UpdateParallax(_camera);
                _scenery.UpdatePlants(_grid, _vegetation);
            }
            profiler.Add(ProfileStage.RenderScenery, renderStart);
            _renderMsSmoothed = Mathf.Lerp(_renderMsSmoothed, (float)_stopwatch.Elapsed.TotalMilliseconds, 0.1f);
            profiler.EndFrame(Time.unscaledDeltaTime);
        }

        TerrainLayout LayoutFromMarkers()
        {
            Vector2Int nest = WorldToCell(_nestMarker.position);
            int a = Mathf.Clamp(WorldToCell(_dumpStartMarker.position).x, 0, _grid.Width - 1);
            int b = Mathf.Clamp(WorldToCell(_dumpEndMarker.position).x, 0, _grid.Width - 1);
            return new TerrainLayout(nest.x, nest, Mathf.Min(a, b), Mathf.Max(a, b));
        }

        Vector2Int WorldToCell(Vector3 world)
        {
            Vector3 local = world - _view.transform.position;
            return new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
        }

        /// <summary>Enquadra um retângulo do mapa numa câmera ortográfica. Usado pelo setup da cena e pelo Inspector no Editor.</summary>
        public static void FrameCamera(Camera camera, Vector3 mapOrigin, int width, int height, float margin)
        {
            float halfHeight = height * 0.5f + margin;
            float halfWidth = width * 0.5f + margin;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(halfHeight, halfWidth / Mathf.Max(0.01f, camera.aspect));
            camera.transform.position = new Vector3(mapOrigin.x + width * 0.5f, mapOrigin.y + height * 0.5f, mapOrigin.z - 10f);
        }

        void OnDrawGizmos()
        {
            if (_view == null)
                return;
            Vector3 origin = _view.transform.position;
            bool image = _mapSource == MapSource.Image && _map != null;
            if (!image)
            {
                // Procedural: limite do mundo (até onde o mapa cresce) e área inicial. Ninho e despejo vêm do gerador.
                Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
                Gizmos.DrawWireCube(origin + new Vector3(_terrainSettings.WorldWidth, _terrainSettings.WorldHeight) * 0.5f,
                                    new Vector3(_terrainSettings.WorldWidth, _terrainSettings.WorldHeight));
                RectInt start = _terrainSettings.StartArea;
                Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
                Gizmos.DrawWireCube(origin + new Vector3(start.center.x, start.center.y), new Vector3(start.width, start.height));
                return;
            }

            int width = _map.width;
            int height = _map.height;
            Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
            Gizmos.DrawWireCube(origin + new Vector3(width, height) * 0.5f, new Vector3(width, height));
            if (_nestMarker != null)
            {
                Gizmos.color = new Color(1f, 0.3f, 0.2f);
                Gizmos.DrawWireSphere(_nestMarker.position, 3f);
            }
            if (_dumpStartMarker != null && _dumpEndMarker != null)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f);
                DrawDumpLine(_dumpStartMarker.position.x, origin.y, height);
                DrawDumpLine(_dumpEndMarker.position.x, origin.y, height);
                if (_nestMarker != null)
                {
                    // Espelho da faixa em relação ao ninho: o segundo lado do despejo (calculado, não editável).
                    float axis = _nestMarker.position.x * 2f;
                    Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.4f);
                    DrawDumpLine(axis - _dumpStartMarker.position.x, origin.y, height);
                    DrawDumpLine(axis - _dumpEndMarker.position.x, origin.y, height);
                }
            }
        }

        static void DrawDumpLine(float x, float bottom, int height) =>
            Gizmos.DrawLine(new Vector3(x, bottom, 0f), new Vector3(x, bottom + height, 0f));

        const int CheatWorkers = 50;
        const int CheatCutters = 50;

        /// <summary>
        /// Cheat de teste (F1): +50 operárias e +50 cortadeiras, nascidas onde a rainha está. Não cobra nutrientes e ignora
        /// a capacidade adulta e a vaga de berçário (a rainha só volta a pôr ovos quando a população cair abaixo do limite).
        /// Só em Editor e builds de desenvolvimento.
        /// </summary>
        void SpawnCheatAnts()
        {
            if (_colony == null || !Debug.isDebugBuild)
                return;
            for (int i = 0; i < CheatWorkers; i++)
                _colony.Spawn(AntRole.Worker);
            for (int i = 0; i < CheatCutters; i++)
                _colony.Spawn(AntRole.Cutter);
            Debug.Log($"[AntFarmGame] Cheat F1: +{CheatWorkers} operárias e +{CheatCutters} cortadeiras (total {_colony.Ants.Count}).");
        }

        /// <summary>
        /// Cheat de teste (F4): começa um ataque de cupins agora (com o alerta), mesmo antes do mínimo de formigas. Só em Editor
        /// e builds de desenvolvimento.
        /// </summary>
        void ForceTermiteRaid()
        {
            if (_colony == null || !Debug.isDebugBuild)
                return;
            _colony.ForceRaid();
            Debug.Log($"[AntFarmGame] Cheat F4: ataque de cupins em {_raidSettings.WarningSeconds:0} s.");
        }

        void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) SetSpeed(0);
                if (keyboard.digit2Key.wasPressedThisFrame) SetSpeed(1);
                if (keyboard.digit3Key.wasPressedThisFrame) SetSpeed(2);
                if (keyboard.pKey.wasPressedThisFrame) SetPaused(!_paused);
                if (keyboard.f1Key.wasPressedThisFrame) SpawnCheatAnts();
                if (keyboard.f2Key.wasPressedThisFrame) _showDebugHud = !_showDebugHud;
                if (keyboard.f4Key.wasPressedThisFrame) ForceTermiteRaid();
                if (keyboard.f11Key.wasPressedThisFrame)
                {
                    // F11: estatísticas; Ctrl+F11: linhas dos blocos 16x16 do mapa.
                    if (keyboard.ctrlKey.isPressed)
                        _showChunkLines = !_showChunkLines;
                    else
                        _showStats = !_showStats;
                }
                if (keyboard.bKey.wasPressedThisFrame)
                {
                    int next = ((int)_brushMode + 1) % BrushLabels.Length;
                    _brushMode = (BrushMode)(IsBrushUnlocked(next) ? next : 0);
                }
                if (keyboard.cKey.wasPressedThisFrame)
                    SetWeather((Weather)(((int)_weather + 1) % WeatherLabels.Length));
                if (keyboard.rKey.wasPressedThisFrame)
                    Regenerate();
                if (keyboard.f5Key.wasPressedThisFrame)
                    SaveGame();
                if (keyboard.f9Key.wasPressedThisFrame && LoadGame())
                    return;
            }

            Mouse mouse = Mouse.current;
            _pointerOverWorld = false;
            if (mouse == null || PointerOverHud())
                return;

            Vector2 screen = mouse.position.ReadValue();
            // Scroll sozinho é zoom (MapCamera); Ctrl+scroll muda o pincel.
            bool ctrl = keyboard != null && keyboard.ctrlKey.isPressed;
            float scroll = ctrl ? mouse.scroll.ReadValue().y : 0f;
            if (scroll > 0f) _brushRadius = Mathf.Min(_brushRadius + 1, 30);
            else if (scroll < 0f) _brushRadius = Mathf.Max(_brushRadius - 1, 1);

            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            Vector3 local = world - _view.transform.position;
            _brushCell = new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
            _pointerOverWorld = _grid.InBounds(_brushCell.x, _brushCell.y);
            if (!_pointerOverWorld)
                return;

            if (_brushMode == BrushMode.Finger)
            {
                // O dedo não pinta: cada clique fere o cupim mais perto da mira; na rainha, força um ovo.
                if (mouse.leftButton.wasPressedThisFrame && !_colony.GameOver && _paused)
                    DenyAt(_brushCell); // jogo pausado: o dedo não fere nem força ovo
                else if (mouse.leftButton.wasPressedThisFrame && !_colony.GameOver)
                {
                    if (!_colony.HitTermiteAt(_brushCell.x, _brushCell.y) && _colony.IsQueenAt(_brushCell.x, _brushCell.y)
                        && !_colony.ForceLay())
                        DenyAt(_brushCell);
                }
            }
            else if (mouse.leftButton.isPressed)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    _strokeDenied = false;
                    GameAudio.Play(SfxId.Mark);
                }
                PaintMarks(true);
            }
            else if (mouse.rightButton.isPressed && _brushMode != BrushMode.Finger)
            {
                if (mouse.rightButton.wasPressedThisFrame)
                    GameAudio.Play(SfxId.Unmark);
                PaintMarks(false);
            }
        }

        /// <summary>
        /// Barras de progresso (KI-026) do que a colônia produz: canteiros brotando, crias no berçário e a gota da sala de orvalho.
        /// A da rainha é desenhada pela <see cref="AntSwarmView"/>.
        /// </summary>
        void RenderProgressBars(Vector3 origin)
        {
            if (_progressBars == null)
                return;
            _progressBars.Begin();
            if (_camera.pixelHeight / (2f * _camera.orthographicSize) < BarsMinPixelsPerCell)
            {
                _progressBars.End(); // câmera afastada: nenhuma barra pedida, todas escondidas
                return;
            }
            float growSeconds = _colony.NutrientCureSeconds;
            for (int k = 0; k < _grid.BedCount; k++)
            {
                if (!_grid.TryGetBed(k, out int x, out int y))
                    break;
                if (_grid.GetBedStage(x, y) != BedStage.Growing || growSeconds <= 0f)
                    continue;
                _progressBars.Add(origin + new Vector3(x + 0.5f, y + 5f, 0f), 4f, 1f - _grid.GetBedGrow(x, y) / growSeconds);
            }
            IReadOnlyList<BroodRecord> brood = _colony.Brood.Brood;
            IReadOnlyList<Vector2Int> spots = _colony.NurserySpots;
            for (int i = 0; i < brood.Count; i++)
            {
                BroodRecord b = brood[i];
                if (b.Phase != EggPhase.Placed || b.Slot < 0 || spots.Count == 0)
                    continue;
                Vector2Int cell = spots[b.Slot % spots.Count];
                _progressBars.Add(origin + new Vector3(cell.x + 0.5f, cell.y + 3.5f, 0f), 3f, _colony.Brood.StageProgress(b));
            }
            float dew = _colony.DewProgress;
            if (dew >= 0f)
            {
                // Uma barra por ponto de queda, do tamanho de uma coluna (cada ponto solta a sua gota).
                for (int k = 0; k < _colony.DewDropCellCount; k++)
                {
                    Vector2Int dewCell = _colony.DewDropCell(k);
                    _progressBars.Add(origin + new Vector3(dewCell.x + 0.5f, dewCell.y + 3f, 0f), 1f, dew, _colony.DewBlocked);
                }
            }
            _progressBars.End();
        }

        /// <summary>Recomeça a partida num mapa novo (botão da tela de game over e tecla R).</summary>
        public void NewMap() => Regenerate();

        void SetWeather(Weather weather)
        {
            _weather = weather;
            _rain.Weather = weather;
            ApplyWeatherSound();
        }

        bool PointerOverHud()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return false;
            if (_colony != null && _colony.GameOver)
                return true; // a tela de game over cobre o mundo
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return true;
            if (!_showDebugHud)
                return false;
            Vector2 screen = mouse.position.ReadValue();
            // IMGUI usa y para baixo.
            return _hudRect.Contains(new Vector2(screen.x, Screen.height - screen.y));
        }

        // O traço atual (botão esquerdo apertado) já foi recusado pelo limite do ambiente: um aviso por traço, não por frame.
        bool _strokeDenied;

        void PaintMarks(bool marked)
        {
            // Limite do ambiente (KI-043): só ao pintar; apagar é livre.
            PaintRoom room = default;
            bool limited = marked && TryGetPaintRoom((int)_brushMode, out room);
            PaintBudget budget = _colony.PaintBudget;
            // Sala nova acima do máximo: o traço inteiro é recusado. A falta de área é vista célula a célula (abaixo), para
            // repintar a área que já existe (o modo dos resíduos) continuar valendo com o limite cheio.
            if (limited && budget.Check(room, _brushCell.x, _brushCell.y, _brushRadius) == PaintBlock.NoRooms)
            {
                Deny();
                return;
            }
            bool denied = false;
            if (_brushMode == BrushMode.GlowMushroom && marked)
            {
                // Um cogumelo por célula, só na mira: o círculo do pincel encheria o túnel de cogumelos.
                if (!_grid.IsGlowMushroomMarked(_brushCell.x, _brushCell.y) && !_grid.SetGlowMushroomMarked(_brushCell.x, _brushCell.y, true))
                    Deny();
                return;
            }
            int r2 = _brushRadius * _brushRadius;
            for (int dy = -_brushRadius; dy <= _brushRadius; dy++)
            for (int dx = -_brushRadius; dx <= _brushRadius; dx++)
            {
                if (dx * dx + dy * dy > r2)
                    continue;
                int x = _brushCell.x + dx, y = _brushCell.y + dy;
                // O traço pode esgotar a área no meio: cada célula nova ainda confere o que sobrou.
                if (limited && budget.Available(room) <= 0 && !PaintBudget.IsRoom(_grid, room, x, y))
                {
                    denied = true;
                    continue;
                }
                switch (_brushMode)
                {
                    case BrushMode.Build:
                        // Trava das configurações: não marca obra por cima de sala pintada (apagar a marca continua livre).
                        if (marked && GameSettings.Current.BlockBuildOnRooms && _grid.IsAnyRoom(x, y))
                            break;
                        _grid.SetBuildMarked(x, y, marked);
                        break;
                    case BrushMode.QueenRoom:
                        _grid.SetQueenRoom(x, y, marked);
                        break;
                    case BrushMode.NutrientRoom:
                        _grid.SetNutrientRoom(x, y, marked);
                        break;
                    case BrushMode.NurseryRoom:
                        _grid.SetNurseryRoom(x, y, marked);
                        break;
                    case BrushMode.WasteRoom:
                        _grid.SetWasteRoom(x, y, marked, (byte)WasteBrushMode);
                        break;
                    case BrushMode.DewRoom:
                        _grid.SetDewRoom(x, y, marked);
                        break;
                    case BrushMode.FoodStore:
                        _grid.SetStorageRoom(x, y, StorageKind.Food, marked);
                        break;
                    case BrushMode.BlockStore:
                        _grid.SetStorageRoom(x, y, StorageKind.Blocks, marked);
                        break;
                    case BrushMode.WaterStore:
                        _grid.SetStorageRoom(x, y, StorageKind.Water, marked);
                        break;
                    case BrushMode.SandStore:
                        _grid.SetStorageRoom(x, y, StorageKind.Sand, marked);
                        break;
                    case BrushMode.MushroomStore:
                        _grid.SetStorageRoom(x, y, StorageKind.Mushroom, marked);
                        break;
                    case BrushMode.FarmRoom:
                        _grid.SetFarmRoom(x, y, marked);
                        break;
                    case BrushMode.GlowMushroom:
                        // Só desmarca (marcar é na mira, acima); a obra comum fica.
                        _grid.SetGlowMushroomMarked(x, y, false);
                        break;
                    default:
                        // Cavar não marca plantas: quem as colhe é a jardineira, por conta própria.
                        if (!CellMaterials.IsAnyPlant(_grid.Get(x, y)))
                            _grid.SetMarked(x, y, marked);
                        break;
                }
            }
            if (denied)
                Deny();

            // Mesmo aviso da rainha sem nutrientes: som de recusa e "!" na mira, uma vez por traço.
            void Deny()
            {
                if (_strokeDenied)
                    return;
                _strokeDenied = true;
                DenyAt(_brushCell);
            }
        }

        void OnGUI()
        {
            if (_grid != null && _showChunkLines && Event.current.type == EventType.Repaint)
                DrawChunkLines();
            if (_colony != null && _showStats)
            {
                // Canto inferior direito: o HUD de jogo ocupa o topo e a esquerda.
                var statsRect = new Rect(Screen.width - StatsPanelWidth - 10f, Screen.height - StatsPanelHeight - 10f, StatsPanelWidth, StatsPanelHeight);
                _statsOverlay.Draw(statsRect, _colony, _grid, _vegetation, SpeedSteps[_speedIndex], _paused, _ticksPerSecond, _maxTicksPerFrame, _maxSimMsPerFrame);
            }
            if (_colony == null || !_showDebugHud)
                return;

            GUILayout.BeginArea(_hudRect, GUI.skin.box);
            GUILayout.Label("<b>Formigário — protótipo</b>", RichLabel());
            GUILayout.Label($"Tempo de partida: {MatchTimer.Format(_matchTimer.Seconds)}");
            GUILayout.Label($"Formigas: {_colony.Ants.Count}");
            GUILayout.Label($"<b>Nutrientes: {_colony.Nutrients}</b>   (consumidos pela rainha: {_colony.TotalNutrients})", RichLabel());
            GUILayout.Label($"Sala de nutrientes: {NutrientRoomStatus()}");
            GUILayout.Label($"Gotas de orvalho: {DewRoomStatus()}");
            GUILayout.Label($"Fazenda de pulgões: {FarmStatus()}");
            GUILayout.Label($"Plantas: {_vegetation.PlantCount} · colhidos: {_colony.HarvestedPixels} px · cresceram: {_vegetation.GrownCells} px");
            GUILayout.Label($"Terra retirada: {_colony.TotalDelivered}");
            GUILayout.Label($"Depósitos (guardado/vagas): alimentos {DepotLine(StorageKind.Food)} · construção {DepotLine(StorageKind.Blocks)} · " +
                            $"água {DepotLine(StorageKind.Water)} · areia {DepotLine(StorageKind.Sand)} · cogumelos {DepotLine(StorageKind.Mushroom)} · " +
                            $"soltos {_colony.LooseItems}");
            GUILayout.Label($"Pixels marcados: {_grid.MarkedCount} para cavar · {_grid.BuildMarkedCount} para construir");
            GUILayout.Label($"Rainha: {QueenStatus()}");
            GUILayout.Label($"Cupins  [F4]: {RaidStatus()}");
            GUILayout.Label($"Criação: {_colony.Breeding} · ovos {_colony.Brood.Eggs} larvas {_colony.Brood.Larvae} pupas {_colony.Brood.Pupae} · vagas {_colony.AdultCapacity} (berçário {_colony.NurserySlots})");
            GUILayout.Label($"Tempo  [C]   dissolvidos: {_rain.DissolvedCells} · compactados: {_rain.PackedCells}");
            GUILayout.Label($"Água: {_grid.WaterCount} px · terra amolecida: {_water.ErodedCells}");
            int weather = GUILayout.Toolbar((int)_weather, WeatherLabels);
            if (weather != (int)_weather)
                SetWeather((Weather)weather);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Salvar  [F5]"))
                SaveGame();
            if (GUILayout.Button("Carregar  [F9]"))
                LoadGame();
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_saveStatus) && Time.unscaledTime < _saveStatusUntil)
                GUILayout.Label(_saveStatus);
            GUILayout.Label("Pincel  [B]");
            SetBrush(GUILayout.SelectionGrid((int)_brushMode, BrushLabels, 3));
            GUILayout.Label($"Velocidade: x{SpeedSteps[_speedIndex]}  [1/2/3]   Pincel: {_brushRadius}  [Ctrl+scroll]");
            GUILayout.Label(_mapSource == MapSource.Image ?"Esq.: marcar · Dir.: desmarcar · R: reiniciar mapa" : "Esq.: marcar · Dir.: desmarcar · R: novo mapa");
            GUILayout.Label("WASD / botão do meio: mover · Q/E ou scroll: zoom");
            RectInt active = _grid.Active;
            GUILayout.Label($"Mapa gerado: {active.width}x{active.height} de {_grid.Width}x{_grid.Height}");
            GUILayout.Label($"Sim: {_simMsSmoothed:0.00} ms · Render: {_renderMsSmoothed:0.00} ms · {1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f):0} fps");
            GUILayout.EndArea();
        }

        /// <summary>
        /// Linhas verticais e horizontais nas fronteiras dos blocos da grade, só na parte visível (câmera ortográfica: são
        /// retângulos de 1 px na tela). Destaca a cada <see cref="ChunkMajorEvery"/> blocos.
        /// </summary>
        void DrawChunkLines()
        {
            Vector3 origin = _view.transform.position;
            Vector3 bottomLeft = _camera.ScreenToWorldPoint(new Vector3(0f, 0f, -_camera.transform.position.z)) - origin;
            Vector3 topRight = _camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, -_camera.transform.position.z)) - origin;
            int size = PixelGrid.ChunkSize;
            int cx0 = Mathf.Clamp(Mathf.FloorToInt(bottomLeft.x / size), 0, _grid.ChunkColumns);
            int cx1 = Mathf.Clamp(Mathf.CeilToInt(topRight.x / size), 0, _grid.ChunkColumns);
            int cy0 = Mathf.Clamp(Mathf.FloorToInt(bottomLeft.y / size), 0, _grid.ChunkRows);
            int cy1 = Mathf.Clamp(Mathf.CeilToInt(topRight.y / size), 0, _grid.ChunkRows);
            float gridBottom = _camera.WorldToScreenPoint(origin).y;
            float gridTop = _camera.WorldToScreenPoint(origin + new Vector3(0f, _grid.Height, 0f)).y;
            float gridLeft = _camera.WorldToScreenPoint(origin).x;
            float gridRight = _camera.WorldToScreenPoint(origin + new Vector3(_grid.Width, 0f, 0f)).x;
            Color previous = GUI.color;
            Texture2D white = Texture2D.whiteTexture;
            for (int cx = cx0; cx <= cx1; cx++)
            {
                float x = _camera.WorldToScreenPoint(origin + new Vector3(cx * size, 0f, 0f)).x;
                GUI.color = cx % ChunkMajorEvery == 0 ? ChunkLineMajorColor : ChunkLineColor;
                GUI.DrawTexture(new Rect(x, Screen.height - gridTop, 1f, gridTop - gridBottom), white);
            }
            for (int cy = cy0; cy <= cy1; cy++)
            {
                float y = Screen.height - _camera.WorldToScreenPoint(origin + new Vector3(0f, cy * size, 0f)).y;
                GUI.color = cy % ChunkMajorEvery == 0 ? ChunkLineMajorColor : ChunkLineColor;
                GUI.DrawTexture(new Rect(gridLeft, y, gridRight - gridLeft, 1f), white);
            }
            GUI.color = previous;
        }

        string NutrientRoomStatus()
        {
            if (_grid.NutrientRoomCount == 0 && _grid.NutrientCount == 0)
                return "nenhuma (pinte uma com o pincel Sala de nutrientes)";
            if (_grid.BedCount == 0)
                return _colony.BedSpots > 0
                    ? $"{_grid.NutrientCount} prontos · sem canteiro (as formigas trazem terra fofa ao cavar)"
                    : $"{_grid.NutrientCount} prontos · sem lugar para canteiro (cave a sala: chão firme e 2 de altura)";
            string paused = _colony.HarvestPaused ? " — colheita pausada" : "";
            return $"{_grid.BedCount} canteiros: {_colony.BedsAwaitingPlant} sem planta, {_colony.BedsAwaitingWater} sem água, " +
                   $"{_grid.GrowingBedCount} brotando · {_grid.NutrientCount} prontos{paused}";
        }

        string DepotLine(StorageKind kind) =>
            _colony.HasDepot(kind) ? $"{_colony.DepotStock(kind)}/{_colony.DepotFree(kind)}" : "—";

        string DewRoomStatus()
        {
            if (_grid.DewRoomCount == 0)
                return "nenhuma (pinte uma com o pincel Gotas de orvalho)";
            if (_colony.DewSoilSpots > 0)
                return $"etapa 1: terra fofa ({_colony.DewSoilSpots} pontos)";
            if (_colony.DewLeafSpots > 0)
                return $"etapa 2: folha ({_colony.DewLeafSpots} pontos)";
            return _colony.DewActive ? $"pronta · {_colony.DewDrops} gotas" : "sem lugar para terra fofa (cave a sala: 4 de altura)";
        }

        string FarmStatus()
        {
            if (_grid.FarmRoomCount == 0)
                return _colony.FarmUnlocked ? "nenhuma (pinte uma com o pincel Fazenda de pulgões)" : "bloqueada (câmara A0)";
            return $"{_colony.Aphids.Count}/{_colony.AphidCap} pulgões · {_colony.StallCount} baias · {_colony.TroughsWaiting} cochos vazios · " +
                   $"{_colony.AphidMilkings} ordenhas · {_colony.AphidDrips} gotas · cogumelos {_colony.MushroomItems} · pretos {_colony.AphidsCorrupted}";
        }

        string RaidStatus()
        {
            switch (_colony.RaidState)
            {
                case RaidState.Warning:
                    return $"ataque em {Mathf.CeilToInt(_colony.SecondsToNextRaid)} s";
                case RaidState.Active:
                    return $"{_colony.LiveTermites} vivos ({_colony.RaidObjective}) · mortos {_colony.TermitesKilled} · formigas mortas {_colony.AntsKilledInCombat}";
                default:
                    if (!_termiteRaids)
                        return "desligados";
                    return _colony.RaidsUnlocked
                        ? $"próximo em {Mathf.CeilToInt(_colony.SecondsToNextRaid)} s · {_colony.RaidsSurvived} repelidos · até {_colony.TermiteCap:P0} das formigas"
                        : $"começam com {_raidSettings.MinAnts} formigas";
            }
        }

        string QueenStatus()
        {
            if (_colony.QueenRoomSize == 0)
                return _grid.QueenRoomCount == 0 ? "sem sala (pinte uma com o pincel Sala da rainha)" : "sala pintada ainda maciça (cave-a)";
            if (_colony.QueenInRoom)
                return $"na sala ({_colony.QueenRoomSize} px)";
            return _colony.QueenCanReachRoom
                ? $"indo para a sala ({_colony.QueenRoomSize} px)"
                : "sala sem caminho até ela (ligue por um túnel)";
        }

        static GUIStyle _richLabel;

        static GUIStyle RichLabel()
        {
            if (_richLabel == null)
                _richLabel = new GUIStyle(GUI.skin.label) { richText = true };
            return _richLabel;
        }
    }
}
