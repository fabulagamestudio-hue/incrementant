using Fabula.Incrementant.Texts;
using System;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.UI;
using Fabula.Incrementant.World;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.Tutorial
{
    /// <summary>
    /// Tutorial da primeira abertura do jogo: abertura, escavar, nutrientes, berçário e rainha, árvore de evolução, inimigos e
    /// despedida. Só observa o jogo (marcas, salas pintadas, menus abertos) e conduz a câmera e a pausa; quem avança cada etapa
    /// é o que o jogador fez, não um relógio. Os pop-ups, o botão "Ver objetivo" e o de pular vêm montados pelo setup do HUD
    /// (<c>Incrementant/Setup/Montar HUD da partida</c>); destaques, contorno das câmaras e marcador da entrada são criados aqui.
    /// Regras e roteiro: <c>TutorialGuide.md</c>.
    /// </summary>
    public sealed class TutorialDirector : MonoBehaviour
    {
        [SerializeField] AntFarmGame _game;
        [SerializeField] AntFarmHud _hud;

        [Header("Pop-up central (abertura e despedida)")]
        [SerializeField] GameObject _modal;
        [SerializeField] RectTransform _modalPanel;
        [SerializeField] Text _modalTitle;
        [SerializeField] Text _modalText;
        [SerializeField] Button _modalButton;
        [SerializeField] Text _modalButtonLabel;

        [Header("Pop-up lateral (instruções ao lado do alvo)")]
        [SerializeField] GameObject _side;
        [SerializeField] RectTransform _sidePanel;
        [SerializeField] Text _sideText;
        [SerializeField] Button _sideButton;
        [SerializeField] Text _sideButtonLabel;

        [Header("Botões fixos")]
        [SerializeField] Button _viewGoalButton;
        [SerializeField] Button _skipButton;

        enum Step
        {
            Intro, ChooseDig, DigFirst, PaintNutrients, NutrientsInfo, DigSecond, PaintNursery, QueenInfo, BroodInfo,
            OpenTree, ExploreTree, ThreatArrival, ThreatDefense, Outro,
        }

        // Índices de pincel de AntFarmGame (BrushMode).
        const int BrushDig = 0, BrushNutrientRoom = 3, BrushNursery = 4;

        const float MarkedFractionToStart = 0.6f;
        const int PaintedMinimum = 16;
        const float CheckInterval = 0.5f;
        const float CameraSeconds = 1.4f;
        static readonly Vector2 LeftOfCenter = new Vector2(0.42f, 0.48f);

        const float SideWidth = 520f, ModalWidth = 900f, PopupGap = 28f, ScreenMargin = 20f;
        static readonly Color Gold = new Color(1f, 0.66f, 0.1f);
        static readonly Color Leaf = new Color(0.55f, 0.9f, 0.4f);

        const string TextIntro = "tutorial.intro";
        const string TextChooseDig = "tutorial.choosedig";
        const string TextMarkFirst = "tutorial.markfirst";
        const string TextWaitDig = "tutorial.waitdig";
        const string TextPaintNutrients = "tutorial.paintnutrients";
        const string TextNutrientsInfo = "tutorial.nutrientsinfo";
        const string TextMarkSecond = "tutorial.marksecond";
        const string TextPaintNursery = "tutorial.paintnursery";
        const string TextQueen = "tutorial.queen";
        const string TextBrood = "tutorial.brood";
        const string TextOpenTree = "tutorial.opentree";
        const string TextTreeTabs = "tutorial.treetabs";
        const string TextTreeDetail = "tutorial.treedetail";
        const string TextThreat = "tutorial.threat";
        const string TextDefense = "tutorial.defense";
        const string TextOutro = "tutorial.outro";

        enum AnchorKind { Right, Ui, World, BottomLeft }

        // ---- estado do roteiro
        Step _step;
        int _phase;
        bool _begun;
        int _generation;
        DigArea _a, _b;
        RectInt _shelter;
        Vector2Int _nest;
        bool _holdPause;
        bool _inputLocked;
        bool _raidsWere;
        bool _brushPending;
        int _pendingBrush;
        int _selectionBaseline;
        float _checkTimer;
        bool _dugCached;
        Rect _goal;
        float _goalFill;
        Vector2 _goalCenter;
        bool _hasGoal;
        Rect _entranceRect;
        bool _showMarker;
        Vector2 _entranceCell;

        // ---- apresentação pedida pelo passo, aplicada no fim de cada quadro
        enum Popup { None, Modal, Side }
        Popup _popup;
        string _title, _text, _buttonLabel;
        AnchorKind _anchor;
        RectTransform _anchorUi;
        Vector2 _anchorCell;
        RectTransform _highlightA, _highlightB;
        DigArea _overlayArea;
        bool _overlayGlow;

        // ---- visuais criados em código
        RectTransform _root;
        Canvas _rootCanvas;
        HighlightFrame _frameA, _frameB;
        RectTransform _worldRoot;
        RawImage _overlayImage;
        Texture2D _overlayTexture;
        DigArea _overlayBuilt;
        bool _overlayBuiltGlow;
        RectInt _overlayCells;
        Image _ring, _arrow;
        Sprite _ringSprite, _arrowSprite;
        string _lastSideText, _lastModalText;
        readonly Vector3[] _corners = new Vector3[4];

        /// <summary>O pop-up central está na tela: o ESC não abre o menu e o mundo fica travado.</summary>
        public bool ModalOpen => _popup == Popup.Modal && _modal != null && _modal.activeSelf;

        // ---------------------------------------------------------------- ciclo de vida

        void Awake()
        {
            if (_game == null || _hud == null || _modal == null || _modalPanel == null || _modalTitle == null || _modalText == null ||
                _modalButton == null || _modalButtonLabel == null || _side == null || _sidePanel == null || _sideText == null ||
                _sideButton == null || _sideButtonLabel == null || _viewGoalButton == null || _skipButton == null)
            {
                Debug.LogError($"{name}: TutorialDirector com referências faltando (rode Incrementant/Setup/Montar HUD da partida).", this);
                enabled = false;
                return;
            }
            _root = (RectTransform)transform;
            _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            HideAll();
        }

        void OnEnable()
        {
            if (_modalButton == null)
                return;
            _modalButton.onClick.AddListener(OnPopupButton);
            _sideButton.onClick.AddListener(OnPopupButton);
            _viewGoalButton.onClick.AddListener(ReturnToGoal);
            _skipButton.onClick.AddListener(Skip);
            _hud.BrushChosen += OnBrushChosen;
        }

        void OnDisable()
        {
            if (_modalButton == null)
                return;
            _modalButton.onClick.RemoveListener(OnPopupButton);
            _sideButton.onClick.RemoveListener(OnPopupButton);
            _viewGoalButton.onClick.RemoveListener(ReturnToGoal);
            _skipButton.onClick.RemoveListener(Skip);
            _hud.BrushChosen -= OnBrushChosen;
        }

        void OnDestroy()
        {
            if (_overlayTexture != null)
                Destroy(_overlayTexture);
            DestroySprite(_ringSprite);
            DestroySprite(_arrowSprite);
            if (_worldRoot != null)
                Destroy(_worldRoot.gameObject);
        }

        static void DestroySprite(Sprite sprite)
        {
            if (sprite == null)
                return;
            Destroy(sprite.texture);
            Destroy(sprite);
        }

        void Start()
        {
            // Já visto, vitrine do menu principal ou partida continuada de um save: sem tutorial.
            if (TutorialProgress.IsDone || _game.Showcase || _game.StartedFromSave)
            {
                enabled = false;
                return;
            }
            Begin();
        }

        void Begin()
        {
            AntColony colony = _game.Colony;
            if (colony == null)
            {
                enabled = false;
                return;
            }
            _generation = _game.Generation;
            PixelGrid grid = _game.Grid;
            _nest = _game.Layout.Nest;
            _shelter = TutorialPlanner.ShelterBounds(grid, _nest);
            _a = TutorialPlanner.Choose(grid, _nest, _shelter);
            _b = _a == null ? null : TutorialPlanner.Choose(grid, _nest, _shelter, new[] { _a.Bounds });
            if (_a == null)
            {
                Debug.LogWarning($"{name}: nenhuma câmara cabe ao lado do abrigo; o tutorial foi pulado.", this);
                Finish(false);
                return;
            }

            CreateVisuals();
            // Sem ataque durante a explicação: o relógio dos cupins só volta a andar quando o tutorial termina.
            _raidsWere = colony.RaidsEnabled;
            colony.RaidsEnabled = false;

            int top = grid.GetSurfaceY(_game.Layout.EntranceX);
            _entranceCell = new Vector2(_game.Layout.EntranceX + 0.5f, top + 2.5f);
            _entranceRect = new Rect(_game.Layout.EntranceX - 16, top - 8, 33, 30);
            _begun = true;
            Enter(Step.Intro);
        }

        void CreateVisuals()
        {
            _frameA = HighlightFrame.Create(_root, "DestaqueA");
            _frameB = HighlightFrame.Create(_root, "DestaqueB");

            // Contorno das câmaras: por baixo de todo o HUD (primeiro filho do canvas), para não cobrir os menus.
            var world = new GameObject("TutorialMundo", typeof(RectTransform));
            world.layer = gameObject.layer;
            world.transform.SetParent(_root.parent, false);
            world.transform.SetAsFirstSibling();
            _worldRoot = (RectTransform)world.transform;
            _worldRoot.anchorMin = Vector2.zero;
            _worldRoot.anchorMax = Vector2.one;
            _worldRoot.offsetMin = _worldRoot.offsetMax = Vector2.zero;
            var overlay = new GameObject("Contorno", typeof(RectTransform), typeof(RawImage));
            overlay.layer = gameObject.layer;
            overlay.transform.SetParent(_worldRoot, false);
            _overlayImage = overlay.GetComponent<RawImage>();
            _overlayImage.raycastTarget = false;
            var overlayRect = _overlayImage.rectTransform;
            overlayRect.anchorMin = overlayRect.anchorMax = overlayRect.pivot = new Vector2(0.5f, 0.5f);
            _overlayImage.enabled = false;

            // Marcador ilustrativo da entrada (os mesmos gráficos vermelhos das marcas de cupim).
            _ringSprite = TermiteMarkers.MakeRingSprite(64, 5f);
            _arrowSprite = TermiteMarkers.MakeArrowSprite(64);
            _ring = CreateMarkerImage("MarcadorAnel", _ringSprite);
            _arrow = CreateMarkerImage("MarcadorSeta", _arrowSprite);
            _arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f); // a seta do sprite aponta para cima
        }

        Image CreateMarkerImage(string objectName, Sprite sprite)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(_root, false);
            go.transform.SetAsFirstSibling();
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.92f, 0.12f, 0.1f, 0.95f);
            image.raycastTarget = false;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.gameObject.SetActive(false);
            return image;
        }

        void HideAll()
        {
            _modal.SetActive(false);
            _side.SetActive(false);
            _viewGoalButton.gameObject.SetActive(false);
            _skipButton.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- pausa e travas

        void HoldPause(bool hold)
        {
            _holdPause = hold;
            _game.SetPaused(hold);
        }

        /// <summary>Enquanto o passo pede pausa, reaplica-a (o jogador pode apertar P ou uma velocidade); o menu do ESC tem a vez dele.</summary>
        void EnforcePause()
        {
            if (_holdPause && !_game.Paused && !_game.InputLocked)
                _game.SetPaused(true);
        }

        void SetInputLocked(bool locked)
        {
            if (_inputLocked == locked)
                return;
            _inputLocked = locked;
            _game.InputLocked = locked;
        }

        void OnBrushChosen(int index)
        {
            if (_brushPending && index == _pendingBrush)
                _brushPending = false;
        }

        // ---------------------------------------------------------------- roteiro

        static Rect ToRect(RectInt r) => new Rect(r.x, r.y, r.width, r.height);

        static Rect Union(params Rect[] rects)
        {
            Rect result = rects[0];
            for (int i = 1; i < rects.Length; i++)
                result = Rect.MinMaxRect(Mathf.Min(result.xMin, rects[i].xMin), Mathf.Min(result.yMin, rects[i].yMin),
                                         Mathf.Max(result.xMax, rects[i].xMax), Mathf.Max(result.yMax, rects[i].yMax));
            return result;
        }

        static Rect Pad(Rect r, float cells) => new Rect(r.x - cells, r.y - cells, r.width + 2f * cells, r.height + 2f * cells);

        /// <summary>Move a câmera (suave) e guarda o enquadramento como objetivo do botão "Ver objetivo".</summary>
        void Frame(Rect cells, float fill, Vector2 center, bool fromOverview = false, bool offerButton = true)
        {
            _goal = cells;
            _goalFill = fill;
            _goalCenter = center;
            _hasGoal = offerButton;
            if (fromOverview)
                _game.FrameCellsFromOverview(cells, CameraSeconds + 0.4f, fill, center);
            else
                _game.FrameCells(cells, CameraSeconds, fill, center);
        }

        void ReturnToGoal()
        {
            if (_hasGoal)
                _game.FrameCells(_goal, CameraSeconds, _goalFill, _goalCenter);
        }

        void Enter(Step step)
        {
            _step = step;
            _phase = 0;
            _checkTimer = 0f;
            _dugCached = false;
            _hasGoal = false;
            _showMarker = false;
            switch (step)
            {
                case Step.Intro:
                    HoldPause(true);
                    // Abre pela vista mais ampla e desce até o abrigo; o pop-up fica na parte de cima da tela.
                    Frame(Pad(ToRect(_shelter), 1f), 0.42f, new Vector2(0.5f, 0.3f), fromOverview: true, offerButton: false);
                    break;
                case Step.ChooseDig:
                    _brushPending = true;
                    _pendingBrush = BrushDig;
                    break;
                case Step.DigFirst:
                    Frame(Pad(Union(ToRect(_shelter), _a.BoundsRect), 2f), 0.55f, LeftOfCenter);
                    break;
                case Step.PaintNutrients:
                case Step.NutrientsInfo:
                    EnsureFramed(Pad(Union(ToRect(_shelter), _a.BoundsRect), 2f), 0.55f);
                    if (step == Step.PaintNutrients)
                        HoldPause(false);
                    break;
                case Step.DigSecond:
                    Frame(Pad(Union(ToRect(_shelter), _a.BoundsRect, _b.BoundsRect), 2f), 0.62f, LeftOfCenter);
                    break;
                case Step.PaintNursery:
                    EnsureFramed(Pad(Union(ToRect(_shelter), _a.BoundsRect, _b.BoundsRect), 2f), 0.62f);
                    break;
                case Step.QueenInfo:
                case Step.BroodInfo:
                    if (step == Step.QueenInfo)
                    {
                        Ant queen = _game.Colony.Queen;
                        Frame(new Rect(queen.X - 14, queen.Y - 8, 28, 16), 0.6f, new Vector2(0.36f, 0.5f));
                    }
                    break;
                case Step.OpenTree:
                    HoldPause(false);
                    break;
                case Step.ExploreTree:
                    HoldPause(true);
                    _selectionBaseline = _hud.Tree != null ? _hud.Tree.NodeSelections : 0;
                    break;
                case Step.ThreatArrival:
                    HoldPause(false);
                    Frame(_entranceRect, 0.5f, LeftOfCenter);
                    break;
                case Step.ThreatDefense:
                    Frame(Union(Pad(ToRect(_shelter), 1f), _entranceRect), 0.62f, LeftOfCenter);
                    break;
                case Step.Outro:
                    HoldPause(true);
                    Frame(Union(Pad(ToRect(_shelter), 3f), _a.BoundsRect, _b != null ? _b.BoundsRect : _a.BoundsRect, _entranceRect),
                          0.62f, new Vector2(0.5f, 0.33f), offerButton: false);
                    break;
            }
        }

        void EnsureFramed(Rect cells, float fill)
        {
            if (!_game.IsFramed(cells, fill))
                Frame(cells, fill, LeftOfCenter);
            else
            {
                _goal = cells;
                _goalFill = fill;
                _goalCenter = LeftOfCenter;
                _hasGoal = true;
            }
        }

        void OnPopupButton()
        {
            switch (_step)
            {
                case Step.Intro: Enter(Step.ChooseDig); break;
                case Step.NutrientsInfo: Enter(_b != null ? Step.DigSecond : Step.OpenTree); break;
                case Step.QueenInfo: Enter(Step.BroodInfo); break;
                case Step.BroodInfo: Enter(Step.OpenTree); break;
                case Step.ThreatArrival: Enter(Step.ThreatDefense); break;
                case Step.ThreatDefense: Enter(Step.Outro); break;
                case Step.Outro: Finish(true); break;
            }
        }

        void Skip() => Finish(true);

        void Finish(bool markDone)
        {
            if (_begun)
            {
                if (_game.Colony != null && _generation == _game.Generation)
                    _game.Colony.RaidsEnabled = _raidsWere;
                if (_holdPause)
                    _game.SetPaused(false);
                _holdPause = false;
            }
            SetInputLocked(false);
            _popup = Popup.None;
            HideAll();
            if (_overlayImage != null)
                _overlayImage.enabled = false;
            _frameA?.Hide();
            _frameB?.Hide();
            if (_ring != null)
            {
                _ring.gameObject.SetActive(false);
                _arrow.gameObject.SetActive(false);
            }
            _begun = false;
            if (markDone)
                TutorialProgress.MarkDone();
            enabled = false;
        }

        // ---------------------------------------------------------------- um quadro

        void Update()
        {
            if (!_begun)
                return;
            AntColony colony = _game.Colony;
            if (colony == null || _generation != _game.Generation)
            {
                // Mapa novo ou save carregado no meio do caminho: as posições do roteiro deixaram de valer.
                Finish(true);
                return;
            }

            _popup = Popup.None;
            _highlightA = _highlightB = null;
            _overlayArea = null;
            _overlayGlow = false;
            _buttonLabel = null;
            _checkTimer += Time.unscaledDeltaTime;
            bool check = _checkTimer >= CheckInterval;
            if (check)
                _checkTimer = 0f;

            EnforcePause();
            UpdateStep(colony, check);
            Apply();
        }

        void UpdateStep(AntColony colony, bool check)
        {
            PixelGrid grid = _game.Grid;
            switch (_step)
            {
                case Step.Intro:
                    Say(Popup.Modal, TextIntro, "tutorial.start", title: "tutorial.title.intro");
                    break;

                case Step.ChooseDig:
                    Say(Popup.Side, TextChooseDig);
                    _highlightA = BrushTarget(BrushDig, false);
                    SetAnchorUi(_highlightA);
                    if (!_brushPending && _game.BrushIndex == BrushDig)
                        Enter(Step.DigFirst);
                    break;

                case Step.DigFirst:
                    UpdateDig(_a, TextMarkFirst, check, Step.PaintNutrients);
                    break;

                case Step.PaintNutrients:
                    _overlayArea = _a;
                    _overlayGlow = true;
                    Say(Popup.Side, TextPaintNutrients);
                    _highlightA = BrushTarget(BrushNutrientRoom, true);
                    SetAnchorUi(_highlightA);
                    if (TutorialPlanner.PaintedCount(grid, _a, (g, x, y) => g.IsNutrientRoom(x, y)) >= PaintedMinimum)
                        Enter(Step.NutrientsInfo);
                    break;

                case Step.NutrientsInfo:
                    _overlayArea = _a;
                    _overlayGlow = true;
                    Say(Popup.Side, TextNutrientsInfo, "tutorial.gotit");
                    _highlightA = _hud.SpotRect(AntFarmHud.Spot.NutrientsCard);
                    SetAnchorUi(_highlightA);
                    break;

                case Step.DigSecond:
                    UpdateDig(_b, TextMarkSecond, check, Step.PaintNursery);
                    break;

                case Step.PaintNursery:
                    _overlayArea = _b;
                    _overlayGlow = true;
                    Say(Popup.Side, TextPaintNursery);
                    _highlightA = BrushTarget(BrushNursery, true);
                    SetAnchorUi(_highlightA);
                    if (colony.NurserySlots >= 1 &&
                        TutorialPlanner.PaintedCount(grid, _b, (g, x, y) => g.IsNurseryRoom(x, y)) >= PaintedMinimum)
                        Enter(Step.QueenInfo);
                    break;

                case Step.QueenInfo:
                    Say(Popup.Side, TextQueen, "tutorial.continue");
                    Ant queen = colony.Queen;
                    SetAnchorWorld(new Vector2(queen.X + 0.5f, queen.Y + 0.5f));
                    break;

                case Step.BroodInfo:
                    Say(Popup.Side, TextBrood, "tutorial.gotit");
                    _highlightA = _hud.SpotRect(AntFarmHud.Spot.AntsCard);
                    SetAnchorUi(_highlightA);
                    break;

                case Step.OpenTree:
                    Say(Popup.Side, TextOpenTree);
                    _highlightA = _hud.SpotRect(AntFarmHud.Spot.EvolutionMenu);
                    SetAnchorUi(_highlightA);
                    if (_hud.TreeOpen)
                        Enter(Step.ExploreTree);
                    break;

                case Step.ExploreTree:
                    UpdateExploreTree();
                    break;

                case Step.ThreatArrival:
                    _showMarker = true;
                    Say(Popup.Side, TextThreat, "tutorial.continue");
                    break;

                case Step.ThreatDefense:
                    _showMarker = true;
                    Say(Popup.Side, TextDefense, "tutorial.gotit");
                    break;

                case Step.Outro:
                    Say(Popup.Modal, TextOutro, "tutorial.grow", title: "tutorial.title.outro");
                    break;
            }
        }

        /// <summary>Marcar e esperar: o contorno guia a escavação; o texto muda quando há marcas suficientes.</summary>
        void UpdateDig(DigArea area, string markText, bool check, Step next)
        {
            PixelGrid grid = _game.Grid;
            _overlayArea = area;
            if (check)
                _dugCached = TutorialPlanner.IsDugAndReachable(grid, _nest, _shelter, area);
            if (_dugCached)
            {
                Enter(next);
                return;
            }
            float fraction = TutorialPlanner.MarkedOrOpenFraction(grid, area);
            int marked = TutorialPlanner.MarkedCount(grid, area);
            if (_phase == 0 && fraction >= MarkedFractionToStart && marked > 0)
            {
                _phase = 1;
                HoldPause(false); // a simulação continua depois da marcação
            }
            else if (_phase == 1 && marked == 0)
                _phase = 0; // as marcas acabaram e ainda falta terra: pede mais marcação
            Say(Popup.Side, _phase == 0 ? markText : TextWaitDig);
            if (_phase == 0)
            {
                _highlightA = BrushTarget(BrushDig, false);
                if (_highlightA != null)
                    SetAnchorUi(_highlightA);
            }
        }

        void UpdateExploreTree()
        {
            SkillTreeScreen tree = _hud.Tree;
            if (!_hud.TreeOpen || tree == null)
            {
                // Fechou sem consultar nada: volta a pedir que abra. Fechou depois de consultar: a etapa acabou.
                Enter(_phase == 1 ? Step.ThreatArrival : Step.OpenTree);
                return;
            }
            if (_phase == 0 && tree.NodeSelections > _selectionBaseline)
                _phase = 1;
            if (_phase == 0)
            {
                Say(Popup.Side, TextTreeTabs);
                _highlightA = tree.HiveTabRect;
                _highlightB = tree.QueenTabRect;
            }
            else
            {
                Say(Popup.Side, TextTreeDetail);
                _highlightA = tree.DetailRect;
            }
            // A tela de evolução ocupa tudo: o pop-up vai para o canto livre embaixo à esquerda (os nós ficam em cima).
            _anchor = AnchorKind.BottomLeft;
        }

        /// <summary>Os três textos chegam como chaves da tabela e saem no idioma atual (trocar o idioma atualiza o pop-up aberto).</summary>
        void Say(Popup popup, string text, string button = null, string title = null)
        {
            _popup = popup;
            _text = Loc.T(text);
            _buttonLabel = button == null ? null : Loc.T(button);
            _title = title == null ? null : Loc.T(title);
            _anchor = AnchorKind.Right;
        }

        void SetAnchorUi(RectTransform target)
        {
            if (target == null)
                return;
            _anchor = AnchorKind.Ui;
            _anchorUi = target;
        }

        void SetAnchorWorld(Vector2 cell)
        {
            _anchor = AnchorKind.World;
            _anchorCell = cell;
        }

        /// <summary>
        /// O que destacar para o jogador escolher um pincel: com o pincel já ativo, nada; com o submenu aberto, o botão do pincel;
        /// senão, o botão do menu que o contém.
        /// </summary>
        RectTransform BrushTarget(int brush, bool environment)
        {
            if (_game.BrushIndex == brush && !(_brushPending && _pendingBrush == brush))
                return null;
            bool open = environment ? _hud.EnvironmentMenuOpen : _hud.PaintMenuOpen;
            return open ? _hud.BrushRect(brush) : _hud.SpotRect(environment ? AntFarmHud.Spot.EnvironmentMenu : AntFarmHud.Spot.PaintMenu);
        }

        // ---------------------------------------------------------------- apresentação

        void Apply()
        {
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 6f);

            bool modal = _popup == Popup.Modal;
            bool side = _popup == Popup.Side;
            SetInputLocked(modal);
            if (_modal.activeSelf != modal)
                _modal.SetActive(modal);
            if (_side.activeSelf != side)
                _side.SetActive(side);
            if (modal)
                ShowModal();
            if (side)
                ShowSide();

            _skipButton.gameObject.SetActive(!modal && !_hud.TreeOpen);
            bool offerGoal = _hasGoal && !modal && !_game.CameraMoving && !_hud.TreeOpen && !_game.IsFramed(_goal, _goalFill);
            if (_viewGoalButton.gameObject.activeSelf != offerGoal)
                _viewGoalButton.gameObject.SetActive(offerGoal);

            ShowHighlight(_frameA, _highlightA, pulse);
            ShowHighlight(_frameB, _highlightB, pulse);
            ShowOverlay(pulse);
            ShowMarker(pulse);
        }

        void ShowModal()
        {
            _modalTitle.text = _title ?? string.Empty;
            if (_lastModalText != _text)
            {
                _lastModalText = _text;
                _modalText.text = _text;
            }
            _modalButtonLabel.text = _buttonLabel ?? string.Empty;
            float textHeight = TextHeight(_modalText, _text);
            // título (32 + folga), texto, botão (56 + folga) e margens.
            _modalPanel.sizeDelta = new Vector2(ModalWidth, 36f + 64f + textHeight + 32f + 56f + 36f);
            _modalPanel.anchorMin = _modalPanel.anchorMax = _modalPanel.pivot = new Vector2(0.5f, 0.5f);
            // Parte de cima da tela: a câmera enquadra o abrigo embaixo.
            _modalPanel.anchoredPosition = new Vector2(0f, _root.rect.height * 0.5f - _modalPanel.sizeDelta.y * 0.5f - 90f);
        }

        void ShowSide()
        {
            if (_lastSideText != _text)
            {
                _lastSideText = _text;
                _sideText.text = _text;
            }
            bool button = _buttonLabel != null;
            _sideButton.gameObject.SetActive(button);
            if (button)
                _sideButtonLabel.text = _buttonLabel;
            float height = 28f + TextHeight(_sideText, _text) + (button ? 24f + 48f : 0f) + 28f;
            _sidePanel.sizeDelta = new Vector2(SideWidth, height);
            _sidePanel.anchorMin = _sidePanel.anchorMax = _sidePanel.pivot = new Vector2(0.5f, 0.5f);
            _sidePanel.anchoredPosition = SidePosition(_sidePanel.sizeDelta);
        }

        float TextHeight(Text text, string content)
        {
            Vector2 size = text.rectTransform.rect.size;
            TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(size.x, 0f));
            return text.cachedTextGenerator.GetPreferredHeight(content, settings) / text.pixelsPerUnit;
        }

        Vector2 SidePosition(Vector2 size)
        {
            Rect canvas = _root.rect;
            float x, y;
            switch (_anchor)
            {
                case AnchorKind.Ui when _anchorUi != null:
                {
                    Rect target = LocalRectOf(_anchorUi);
                    if (target.center.y > canvas.yMax - 150f)
                    {
                        // Cartões do topo: o pop-up desce para baixo deles, sem cobrir os cartões vizinhos.
                        x = target.xMin + size.x * 0.5f;
                        y = target.yMin - PopupGap - size.y * 0.5f;
                        break;
                    }
                    x = target.xMax + PopupGap + size.x * 0.5f;
                    if (x + size.x * 0.5f > canvas.xMax - ScreenMargin)
                        x = target.xMin - PopupGap - size.x * 0.5f;
                    y = target.center.y;
                    break;
                }
                case AnchorKind.World:
                {
                    Rect target = ScreenToLocal(_game.CellsToScreen(new Rect(_anchorCell.x - 0.5f, _anchorCell.y - 0.5f, 1f, 1f)));
                    x = target.center.x + 90f + size.x * 0.5f;
                    if (x + size.x * 0.5f > canvas.xMax - ScreenMargin)
                        x = target.center.x - 90f - size.x * 0.5f;
                    y = target.center.y;
                    break;
                }
                case AnchorKind.BottomLeft:
                    x = canvas.xMin + 40f + size.x * 0.5f;
                    y = canvas.yMin + 90f + size.y * 0.5f;
                    break;
                default:
                    x = canvas.xMax - ScreenMargin - size.x * 0.5f;
                    y = canvas.center.y;
                    break;
            }
            x = Mathf.Clamp(x, canvas.xMin + ScreenMargin + size.x * 0.5f, canvas.xMax - ScreenMargin - size.x * 0.5f);
            y = Mathf.Clamp(y, canvas.yMin + ScreenMargin + size.y * 0.5f, canvas.yMax - ScreenMargin - size.y * 0.5f);
            return new Vector2(x, y);
        }

        // ---- coordenadas: pixels de tela → espaço local do canvas (centro = 0,0)

        Rect ScreenToLocal(Rect screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen.min, null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen.max, null, out Vector2 max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        Rect LocalRectOf(RectTransform target)
        {
            target.GetWorldCorners(_corners);
            return ScreenToLocal(Rect.MinMaxRect(_corners[0].x, _corners[0].y, _corners[2].x, _corners[2].y));
        }

        void ShowHighlight(HighlightFrame frame, RectTransform target, float pulse)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                frame.Hide();
                return;
            }
            frame.Show(LocalRectOf(target), 6f + 4f * pulse, new Color(Gold.r, Gold.g, Gold.b, 0.45f + 0.55f * pulse));
        }

        // ---- contorno das câmaras (textura de 4 texels por célula: traço fino e miolo translúcido)

        const int TexelsPerCell = 4;

        void ShowOverlay(float pulse)
        {
            if (_overlayArea == null)
            {
                _overlayImage.enabled = false;
                return;
            }
            if (_overlayBuilt != _overlayArea || _overlayBuiltGlow != _overlayGlow)
                BuildOverlayTexture(_overlayArea, _overlayGlow);
            _overlayImage.enabled = true;
            Rect cells = new Rect(_overlayCells.x, _overlayCells.y, _overlayCells.width, _overlayCells.height);
            Rect local = ScreenToLocal(_game.CellsToScreen(cells));
            var rect = _overlayImage.rectTransform;
            rect.anchoredPosition = local.center;
            rect.sizeDelta = local.size;
            Color tint = _overlayGlow ? Leaf : Gold;
            _overlayImage.color = new Color(tint.r, tint.g, tint.b, _overlayGlow ? 0.55f + 0.3f * pulse : 0.6f + 0.4f * pulse);
        }

        void BuildOverlayTexture(DigArea area, bool glow)
        {
            Vector2Int[] shape = glow ? area.ChamberCells : area.Cells;
            RectInt box = glow ? area.ChamberBounds : area.Bounds;
            box = new RectInt(box.x - 1, box.y - 1, box.width + 2, box.height + 2);
            int w = box.width * TexelsPerCell, h = box.height * TexelsPerCell;
            var inside = new bool[w * h];
            foreach (Vector2Int cell in shape)
                for (int ty = 0; ty < TexelsPerCell; ty++)
                for (int tx = 0; tx < TexelsPerCell; tx++)
                    inside[((cell.y - box.y) * TexelsPerCell + ty) * w + (cell.x - box.x) * TexelsPerCell + tx] = true;

            var pixels = new Color32[w * h];
            byte fill = (byte)(glow ? 56 : 30);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (!inside[i])
                    continue;
                bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1 || !inside[i - 1] || !inside[i + 1] || !inside[i - w] || !inside[i + w];
                pixels[i] = new Color32(255, 255, 255, edge ? (byte)255 : fill);
            }
            if (_overlayTexture != null)
                Destroy(_overlayTexture);
            _overlayTexture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            _overlayTexture.SetPixels32(pixels);
            _overlayTexture.Apply();
            _overlayImage.texture = _overlayTexture;
            _overlayCells = box;
            _overlayBuilt = area;
            _overlayBuiltGlow = glow;
        }

        // ---- marcador da entrada

        void ShowMarker(float pulse)
        {
            _ring.gameObject.SetActive(_showMarker);
            _arrow.gameObject.SetActive(_showMarker);
            if (!_showMarker)
                return;
            Rect ring = ScreenToLocal(_game.CellsToScreen(new Rect(_entranceCell.x - 4f, _entranceCell.y - 4f, 8f, 8f)));
            float size = Mathf.Max(48f, ring.width) * (0.9f + 0.15f * pulse);
            _ring.rectTransform.anchoredPosition = ring.center;
            _ring.rectTransform.sizeDelta = new Vector2(size, size);
            float bob = Mathf.Sin(Time.unscaledTime * 4f) * 8f;
            _arrow.rectTransform.anchoredPosition = ring.center + new Vector2(0f, size * 0.5f + 36f + bob);
            _arrow.rectTransform.sizeDelta = new Vector2(48f, 48f);
        }

        // ---------------------------------------------------------------- moldura de destaque

        /// <summary>Moldura pulsante de 4 barras, desenhada em volta de um retângulo do canvas.</summary>
        sealed class HighlightFrame
        {
            const float Thickness = 6f;
            RectTransform _rect;
            Image[] _bars;

            public static HighlightFrame Create(RectTransform parent, string objectName)
            {
                var go = new GameObject(objectName, typeof(RectTransform));
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                go.transform.SetAsFirstSibling(); // abaixo dos pop-ups do tutorial
                var frame = new HighlightFrame { _rect = (RectTransform)go.transform, _bars = new Image[4] };
                frame._rect.anchorMin = frame._rect.anchorMax = frame._rect.pivot = new Vector2(0.5f, 0.5f);
                for (int i = 0; i < 4; i++)
                {
                    var bar = new GameObject("Barra" + i, typeof(RectTransform), typeof(Image));
                    bar.layer = go.layer;
                    bar.transform.SetParent(go.transform, false);
                    Image image = bar.GetComponent<Image>();
                    image.raycastTarget = false;
                    RectTransform r = image.rectTransform;
                    // 0 topo, 1 base, 2 esquerda, 3 direita.
                    r.anchorMin = new Vector2(i == 3 ? 1f : 0f, i == 1 ? 0f : i == 0 ? 1f : 0f);
                    r.anchorMax = new Vector2(i == 2 ? 0f : 1f, i == 0 ? 1f : i == 1 ? 0f : 1f);
                    r.pivot = new Vector2(i == 3 ? 1f : i == 2 ? 0f : 0.5f, i == 0 ? 1f : i == 1 ? 0f : 0.5f);
                    r.sizeDelta = i < 2 ? new Vector2(0f, Thickness) : new Vector2(Thickness, 0f);
                    r.anchoredPosition = Vector2.zero;
                    frame._bars[i] = image;
                }
                go.SetActive(false);
                return frame;
            }

            public void Show(Rect local, float pad, Color color)
            {
                if (!_rect.gameObject.activeSelf)
                    _rect.gameObject.SetActive(true);
                _rect.anchoredPosition = local.center;
                _rect.sizeDelta = local.size + new Vector2(pad * 2f, pad * 2f);
                foreach (Image bar in _bars)
                    bar.color = color;
            }

            public void Hide()
            {
                if (_rect != null && _rect.gameObject.activeSelf)
                    _rect.gameObject.SetActive(false);
            }
        }
    }
}
