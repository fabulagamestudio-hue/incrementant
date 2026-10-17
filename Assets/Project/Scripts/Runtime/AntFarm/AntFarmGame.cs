using System.Diagnostics;
using System;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using UnityEngine;
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
        [SerializeField, Min(1f)] float _ticksPerSecond = 60f;
        [Tooltip("Limite de passos por frame, para um frame lento não virar uma espiral de atraso.")]
        [SerializeField, Min(1)] int _maxTicksPerFrame = 12;
        [SerializeField, Range(1, 30)] int _brushRadius = 4;
        [Header("Chuva")]
        [Tooltip("Tempo ao dar Play e ao gerar outro mapa. No Play, troque com C ou pelo HUD.")]
        [SerializeField] Weather _weather = Weather.Clear;
        [SerializeField] RainSettings _rainSettings = new RainSettings();
        [Header("Água")]
        [SerializeField] WaterSettings _waterSettings = new WaterSettings();
        [Header("Vegetação")]
        [Tooltip("Grama e mudas da superfície que crescem com o tempo e são colhidas (pincel Colher) para virar nutrientes.")]
        [SerializeField] VegetationSettings _vegetationSettings = new VegetationSettings();
        [Header("Câmera")]
        [Tooltip("Folga, em células, do enquadramento inicial e acima do topo do mundo.")]
        [SerializeField, Min(0f)] float _cameraMargin = 4f;
        [Tooltip("Quanto a câmera sobe acima do topo do mundo, em células (além da folga): espaço de céu para elementos grandes do cenário.")]
        [SerializeField, Min(0f)] float _cameraSkyHeadroom = 120f;
        [SerializeField] MapCamera _cameraControl = new MapCamera();

        static readonly int[] SpeedSteps = { 1, 3, 8 };
        /// <summary>
        /// O que o pincel faz: marcar terra para as formigas cavarem, espaço vazio para elas preencherem, a sala onde
        /// a rainha pode ficar, plantas para colher ou a sala onde os nutrientes são guardados. A ordem casa com
        /// <see cref="BrushTint"/>.
        /// </summary>
        enum BrushMode { Dig, Build, QueenRoom, Harvest, NutrientRoom }

        static readonly string[] BrushLabels = { "Cavar", "Construir", "Sala da rainha", "Colher", "Sala de nutrientes" };
        static readonly string[] WeatherLabels = { "Seco", "Chuva leve", "Chuva forte" };

        PixelGrid _grid;
        GranularSimulation _simulation;
        RainSimulation _rain;
        WaterSimulation _water;
        VegetationSimulation _vegetation;
        AntColony _colony;
        TerrainLayout _layout;
        TerrainGenerator _generator;
        RectInt _startArea;
        float _accumulator;
        int _speedIndex;
        Vector2Int _brushCell;
        BrushMode _brushMode;
        bool _pointerOverWorld;
        Rect _hudRect = new Rect(10, 10, 340, 640);
        readonly MatchTimer _matchTimer = new MatchTimer();
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
            Regenerate();
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
                _generator = new TerrainGenerator(_terrainSettings, _seed, _grid.Width, _grid.Height, _startArea);
                _layout = _generator.Begin(_grid);
            }
            _simulation = new GranularSimulation();
            _rain = new RainSimulation(_rainSettings, _seed) { Weather = _weather };
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
            _colony = new AntColony(_grid, _colonySettings, _layout, _seed);
            snapshot?.ApplyTo(_colony);
            _matchTimer.Set(snapshot?.MatchSeconds ?? 0.0);
            _accumulator = 0f;

            Vector3 origin = _view.transform.position;
            float skyAbove = _cameraMargin + _cameraSkyHeadroom;
            var bounds = new Rect(origin.x, origin.y, _grid.Width, _grid.Height + skyAbove);
            var startView = new Rect(origin.x + _startArea.x, origin.y + _startArea.y, _startArea.width, _startArea.height);
            _cameraControl.Reset(_camera, bounds, startView, _cameraMargin);

            _view.Initialize(_grid, transparentSky: _scenery != null, skyBaseY: _startArea.yMin, plantsAsSprites: _scenery != null);
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
                ShowSaveStatus($"Partida salva ({MatchTimer.Format(_matchTimer.Seconds)}).");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"{name}: falha ao salvar em '{SaveStorage.QuickSavePath}': {e}", this);
                ShowSaveStatus($"Falha ao salvar: {e.Message}");
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
                    ShowSaveStatus("Nenhum save encontrado.");
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
                ShowSaveStatus($"Partida carregada ({MatchTimer.Format(_matchTimer.Seconds)}).");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"{name}: falha ao carregar '{SaveStorage.QuickSavePath}': {e}", this);
                ShowSaveStatus($"Falha ao carregar: {e.Message}");
                return false;
            }
        }

        string DescribeIncompatibility(GameSnapshot snapshot)
        {
            if (snapshot.MapSource != (int)_mapSource)
                return "O save é de outro modo de mapa (Procedural/Image).";
            int width = _mapSource == MapSource.Image ? _map.width : _terrainSettings.WorldWidth;
            int height = _mapSource == MapSource.Image ? _map.height : _terrainSettings.WorldHeight;
            if (snapshot.GridWidth != width || snapshot.GridHeight != height)
                return $"O save é de um mundo {snapshot.GridWidth}x{snapshot.GridHeight}; o atual é {width}x{height}.";
            return null;
        }

        void ShowSaveStatus(string message)
        {
            _saveStatus = message;
            _saveStatusUntil = Time.unscaledTime + 5f;
        }

        void Update()
        {
            _matchTimer.Advance(Time.unscaledDeltaTime);
            _cameraControl.Update(_camera, Time.unscaledDeltaTime, PointerOverHud());
            EnsureGenerated();
            HandleInput();

            float tickDuration = 1f / _ticksPerSecond;
            _accumulator += Time.deltaTime * SpeedSteps[_speedIndex];
            int maxTicks = _maxTicksPerFrame * SpeedSteps[_speedIndex];
            int ticks = 0;

            _stopwatch.Restart();
            while (_accumulator >= tickDuration && ticks < maxTicks)
            {
                _rain.Step(_grid, tickDuration);
                _water.Step(_grid, tickDuration);
                _vegetation.Step(_grid, tickDuration);
                _simulation.Step(_grid);
                _colony.Tick(tickDuration);
                _accumulator -= tickDuration;
                ticks++;
            }
            if (ticks == maxTicks)
                _accumulator = 0f;
            _simMsSmoothed = Mathf.Lerp(_simMsSmoothed, (float)_stopwatch.Elapsed.TotalMilliseconds, 0.1f);

            _stopwatch.Restart();
            bool spriteAnts = _antsView != null && _antsView.HasFrames;
            RectInt window = AntFarmView.WindowFor(LocalViewRect(), _grid);
            _view.Render(_grid, _colony.Ants, _colony.DumpStrips,_brushCell, _brushRadius, _pointerOverWorld, !spriteAnts, window, _rain.Drops,
                         (BrushTint)(int)_brushMode, _colony);
            if (spriteAnts)
                _antsView.Render(_grid, _colony.Ants, _view.transform.position, _colony.SimulationTime + _accumulator, Time.deltaTime,
                                 _colony.Queen);
            if (_scenery != null)
            {
                _scenery.UpdateParallax(_camera);
                _scenery.UpdatePlants(_grid, _vegetation);
            }
            _renderMsSmoothed = Mathf.Lerp(_renderMsSmoothed, (float)_stopwatch.Elapsed.TotalMilliseconds, 0.1f);
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

        void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame)
                    _colony.TryBuyAnt();
                if (keyboard.digit1Key.wasPressedThisFrame) _speedIndex = 0;
                if (keyboard.digit2Key.wasPressedThisFrame) _speedIndex = 1;
                if (keyboard.digit3Key.wasPressedThisFrame) _speedIndex = 2;
                if (keyboard.bKey.wasPressedThisFrame)
                    _brushMode = (BrushMode)(((int)_brushMode + 1) % BrushLabels.Length);
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
            // Ctrl+scroll é zoom (MapCamera); scroll sozinho muda o pincel.
            bool ctrl = keyboard != null && keyboard.ctrlKey.isPressed;
            float scroll = ctrl ? 0f : mouse.scroll.ReadValue().y;
            if (scroll > 0f) _brushRadius = Mathf.Min(_brushRadius + 1, 30);
            else if (scroll < 0f) _brushRadius = Mathf.Max(_brushRadius - 1, 1);

            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            Vector3 local = world - _view.transform.position;
            _brushCell = new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
            _pointerOverWorld = _grid.InBounds(_brushCell.x, _brushCell.y);
            if (!_pointerOverWorld)
                return;

            if (mouse.leftButton.isPressed)
                PaintMarks(true);
            else if (mouse.rightButton.isPressed)
                PaintMarks(false);
        }

        void SetWeather(Weather weather)
        {
            _weather = weather;
            _rain.Weather = weather;
        }

        bool PointerOverHud()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return false;
            Vector2 screen = mouse.position.ReadValue();
            // IMGUI usa y para baixo.
            return _hudRect.Contains(new Vector2(screen.x, Screen.height - screen.y));
        }

        void PaintMarks(bool marked)
        {
            int r2 = _brushRadius * _brushRadius;
            for (int dy = -_brushRadius; dy <= _brushRadius; dy++)
            for (int dx = -_brushRadius; dx <= _brushRadius; dx++)
            {
                if (dx * dx + dy * dy > r2)
                    continue;
                int x = _brushCell.x + dx, y = _brushCell.y + dy;
                switch (_brushMode)
                {
                    case BrushMode.Build:
                        _grid.SetBuildMarked(x, y, marked);
                        break;
                    case BrushMode.QueenRoom:
                        _grid.SetQueenRoom(x, y, marked);
                        break;
                    case BrushMode.NutrientRoom:
                        _grid.SetNutrientRoom(x, y, marked);
                        break;
                    default:
                        // Cavar e colher usam a mesma marca, mas cada pincel só mexe no próprio tipo de célula.
                        if (CellMaterials.IsPlant(_grid.Get(x, y)) == (_brushMode == BrushMode.Harvest))
                            _grid.SetMarked(x, y, marked);
                        break;
                }
            }
        }

        void OnGUI()
        {
            if (_colony == null)
                return;

            GUILayout.BeginArea(_hudRect, GUI.skin.box);
            GUILayout.Label("<b>Formigário — protótipo</b>", RichLabel());
            GUILayout.Label($"Tempo de partida: {MatchTimer.Format(_matchTimer.Seconds)}");
            GUILayout.Label($"Formigas: {_colony.Ants.Count}   (afogadas: {_colony.DrownedAnts})");
            GUILayout.Label($"<b>Nutrientes: {_colony.Nutrients}</b>   (consumidos pela rainha: {_colony.TotalNutrients})", RichLabel());
            GUILayout.Label($"Sala de nutrientes: {NutrientRoomStatus()}");
            GUILayout.Label($"Plantas: {_vegetation.PlantCount} · colhidos: {_colony.HarvestedPixels} px · cresceram: {_vegetation.GrownCells} px");
            GUILayout.Label($"Terra retirada: {_colony.TotalDelivered}");
            GUILayout.Label($"Pixels marcados: {_grid.MarkedCount} para cavar/colher · {_grid.BuildMarkedCount} para construir");
            GUILayout.Label($"Rainha: {QueenStatus()}");
            GUI.enabled = _colony.Nutrients >= _colony.NextAntCost;
            if (GUILayout.Button($"Nova formiga — {_colony.NextAntCost} nutrientes  [Espaço]"))
                _colony.TryBuyAnt();
            GUI.enabled = true;
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
            _brushMode = (BrushMode)GUILayout.SelectionGrid((int)_brushMode, BrushLabels, 3);
            GUILayout.Label($"Velocidade: x{SpeedSteps[_speedIndex]}  [1/2/3]   Pincel: {_brushRadius}  [scroll]");
            GUILayout.Label(_mapSource == MapSource.Image ?"Esq.: marcar · Dir.: desmarcar · R: reiniciar mapa" : "Esq.: marcar · Dir.: desmarcar · R: novo mapa");
            GUILayout.Label("WASD / botão do meio: mover · Q/E ou Ctrl+scroll: zoom");
            RectInt active = _grid.Active;
            GUILayout.Label($"Mapa gerado: {active.width}x{active.height} de {_grid.Width}x{_grid.Height}");
            GUILayout.Label($"Sim: {_simMsSmoothed:0.00} ms · Render: {_renderMsSmoothed:0.00} ms · {1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f):0} fps");
            GUILayout.EndArea();
        }

        string NutrientRoomStatus()
        {
            if (_grid.NutrientRoomCount == 0 && _grid.NutrientCount == 0)
                return "nenhuma (pinte uma com o pincel Sala de nutrientes)";
            string full = _colony.FreeNutrientSlots == 0 ? " — cheia: a colheita para" : "";
            int curing = _grid.NutrientCount - _grid.ReadyNutrientCount;
            string curingText = curing > 0 ? $" ({curing} curando)" : "";
            return $"{_grid.NutrientCount} guardados{curingText} · {_colony.FreeNutrientSlots} slots livres{full}";
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
