using Fabula.Incrementant.Texts;
using Fabula.Incrementant.Audio;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Glossary;
using Fabula.Incrementant.Juice;
using System;
using Fabula.Incrementant.Settings;
using Fabula.Incrementant.Tutorial;
using Fabula.Incrementant.World;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// HUD da partida (uGUI): cartões de nutrientes, formigas e rainha, menu Colônia com o submenu Pintar, e controles de
    /// velocidade. Só lê o estado de <see cref="AntFarmGame"/> e chama a API pública dele; a hierarquia é montada pelo
    /// Editor (<c>Incrementant/Setup/Montar HUD da partida</c>), que também liga as referências abaixo.
    /// </summary>
    public sealed class AntFarmHud : MonoBehaviour
    {
        [SerializeField] AntFarmGame _game;

        [Header("Cartões")]
        [SerializeField] Text _nutrientsValue;
        [SerializeField] Text _nutrientsDetail;
        [SerializeField] Text _antsValue;
        [SerializeField] Text _antsDetail;
        [SerializeField] Text _queenValue;
        [SerializeField] Text _queenDetail;
        [SerializeField] Text _hint;
        [Header("Alerta dos cupins")]
        [SerializeField] GameObject _raidAlert;
        [SerializeField] Text _raidAlertText;

        [Header("Canto: FPS e tempo jogado")]
        [SerializeField] Text _stats;

        [Header("Menu Colônia")]
        [SerializeField] Button _paintButton;
        [SerializeField] Image _paintButtonImage;
        [SerializeField] GameObject _paintPanel;
        [Tooltip("Todos os pincéis, na ordem de AntFarmGame; cada botão mora no painel Pintar ou no Ambientes.")]
        [SerializeField] Button[] _brushButtons;
        [SerializeField] Image[] _brushImages;
        [Tooltip("Preenchimento da barra de área livre de cada pincel de ambiente (KI-043), na ordem de AntFarmGame; vazio nos outros. Opcional: cenas antigas não têm.")]
        [SerializeField] Image[] _brushFills;

        [Header("Menu Ambientes (salas: rainha, nutrientes, berçário e resíduos)")]
        [SerializeField] Button _environmentButton;
        [SerializeField] Image _environmentButtonImage;
        [SerializeField] GameObject _environmentPanel;
        [Tooltip("Botão DEPÓSITO (última linha do Ambientes) e o submenu dos depósitos, aberto ao lado dele.")]
        [SerializeField] Button _depotButton;
        [SerializeField] Image _depotButtonImage;
        [SerializeField] GameObject _depotPanel;
        [Tooltip("Botão RESÍDUOS (linha acima do DEPÓSITO) e o submenu dos três modos da área de resíduos, aberto ao lado dele.")]
        [SerializeField] Button _wasteButton;
        [SerializeField] Image _wasteButtonImage;
        [SerializeField] GameObject _wastePanel;

        [Header("Menu Rainha (política: modo e tipo da próxima formiga)")]
        [SerializeField] Button _queenButton;
        [SerializeField] Image _queenButtonImage;
        [SerializeField] GameObject _queenPanel;
        [SerializeField] Button[] _modeButtons;
        [SerializeField] Image[] _modeImages;
        [SerializeField] Button[] _typeButtons;
        [SerializeField] Image[] _typeImages;
        [SerializeField] Text[] _typeLabels;
        [Tooltip("Postura diante dos cupins: defensiva, ofensiva (ordem de QueenStance).")]
        [SerializeField] Button[] _stanceButtons;
        [SerializeField] Image[] _stanceImages;
        [SerializeField] Text _queenState;
        [SerializeField] Text _queenNext;
        [SerializeField] Text _queenBrood;
        [SerializeField] Text _queenSpace;

        [Header("Menu Evolução (tela da árvore: SkillTreeScreen)")]
        [SerializeField] Button _treeButton;
        [SerializeField] Image _treeButtonImage;
        [SerializeField] GameObject _treePanel;

        [Header("Menu do ESC (pausa e configurações)")]
        [SerializeField] PauseMenu _pauseMenu;

        [Header("Tutorial (opcional: cenas antigas não têm)")]
        [SerializeField] TutorialDirector _tutorial;

        [Header("Canto inferior esquerdo")]
        [SerializeField] Button _pauseButton;
        [SerializeField] Image _pauseImage;
        [Tooltip("Sino: recolhe as formigas ao formigueiro e as manda de volta ao trabalho (opcional: cenas antigas não têm).")]
        [SerializeField] Button _recallButton;
        [SerializeField] Image _recallImage;
        [Tooltip("Botão ao lado da pausa: abre o painel da colônia (estatísticas e configurações). Opcional: cenas antigas não têm.")]
        [SerializeField] Button _colonyButton;
        [SerializeField] Image _colonyImage;
        [SerializeField] ColonyPanel _colonyPanel;
        [Tooltip("Mostrar ambientes: liga e desliga a pintura das salas no terreno (GameSettingsData.ShowRooms). Opcional: cenas antigas não têm.")]
        [SerializeField] Button _roomsButton;
        [SerializeField] Image _roomsImage;
        [Tooltip("Botão ? da barra inferior e a janela do glossário (também aberta pelo menu do ESC). Opcional: cenas antigas não têm.")]
        [SerializeField] Button _glossaryButton;
        [SerializeField] GlossaryScreen _glossary;

        [Header("Sprites de estado")]
        [SerializeField] Sprite _buttonIdle;
        [SerializeField] Sprite _buttonActive;
        [SerializeField] Sprite _iconButtonIdle;
        [SerializeField] Sprite _iconButtonActive;

        [Header("Cursor do dedo")]
        [Tooltip("Cursor do sistema enquanto o pincel Dedo está ativo e o mouse está sobre o mapa (textura do tipo Cursor).")]
        [SerializeField] Texture2D _fingerCursor;
        [Tooltip("Ponta do dedo na textura, em pixels a partir do canto superior esquerdo.")]
        [SerializeField] Vector2 _fingerCursorHotspot = new Vector2(11, 1);

        bool _fingerCursorShown;
        HudTooltips _tooltips;
        ColonyMenuDrawer _menuDrawer;

        const string DefaultHintKey = "hud.hint";
        const float TextRefreshSeconds = 0.25f;

        /// <summary>Papéis que a rainha pode criar, na ordem dos botões de tipo.</summary>
        public static AntRole[] CreatableRoles => AntRoles.All;

        /// <summary>Nome do papel em maiúsculas, no idioma atual (o setup do HUD também chama isto, no Editor, para o rótulo estático).</summary>
        public static string RoleLabel(AntRole role) => Loc.T(RoleKey(role)).ToUpperInvariant();

        public static string StanceLabel(QueenStance stance) => Loc.T(StanceKey(stance));

        public static string ModeLabel(QueenMode mode) => Loc.T(ModeKey(mode));

        // Chaves da tabela de textos; o setup do HUD usa as mesmas para montar os rótulos estáticos.
        public static string RoleKey(AntRole role) => "role." + role.ToString().ToLowerInvariant();

        public static string StanceKey(QueenStance stance) =>
            stance == QueenStance.Offensive ? "queen.stance.offensive" : "queen.stance.defensive";

        public static string ModeKey(QueenMode mode) =>
            mode == QueenMode.Automatic ? "queen.mode.auto" : mode == QueenMode.Specific ? "queen.mode.specific" : "queen.mode.paused";

        public static string WasteModeKey(WasteMode mode) =>
            mode == WasteMode.LeavesToWater ? "waste.mode.leaves" : mode == WasteMode.DirtToWater ? "waste.mode.dirt"
                : mode == WasteMode.LeavesToNutrient ? "waste.mode.nutrient" : "waste.mode.remains";

        enum Menu { None, Paint, Environment, Queen, Tree, Colony }

        /// <summary>Peças do HUD que o tutorial destaca.</summary>
        public enum Spot
        {
            PaintMenu, EnvironmentMenu, EvolutionMenu, NutrientsCard, AntsCard, QueenCard, QueenMenu,
            PauseButton, ColonyButton, RecallButton, RoomsButton, GlossaryButton,
        }

        public bool TreeOpen => _treePanel != null && _treePanel.activeSelf;
        public bool ColonyPanelOpen => _colonyPanel != null && _colonyPanel.gameObject.activeSelf;

        /// <summary>Retângulo da peça; null se a cena não a tem (botões opcionais de cenas antigas).</summary>
        public RectTransform SpotRect(Spot spot)
        {
            Component c;
            switch (spot)
            {
                case Spot.PaintMenu: c = _paintButton; break;
                case Spot.EnvironmentMenu: c = _environmentButton; break;
                case Spot.EvolutionMenu: c = _treeButton; break;
                case Spot.QueenMenu: c = _queenButton; break;
                case Spot.NutrientsCard: return (RectTransform)_nutrientsValue.transform.parent;
                case Spot.AntsCard: return (RectTransform)_antsValue.transform.parent;
                case Spot.QueenCard: return (RectTransform)_queenValue.transform.parent;
                case Spot.PauseButton: c = _pauseButton; break;
                case Spot.ColonyButton: c = _colonyButton; break;
                case Spot.RecallButton: c = _recallButton; break;
                case Spot.GlossaryButton: c = _glossaryButton; break;
                default: c = _roomsButton; break;
            }
            return c != null ? (RectTransform)c.transform : null;
        }

        UnityAction[] _brushHandlers;
        UnityAction[] _modeHandlers;
        UnityAction[] _stanceHandlers;
        UnityAction[] _typeHandlers;
        float _nextTextRefresh;
        float _playedSeconds;
        float _statsElapsed;
        int _statsFrames;
        Menu _menu;

        void Awake()
        {
            if (_game == null || _paintButton == null || _paintPanel == null || _pauseButton == null ||
                _brushButtons == null || _brushButtons.Length != AntFarmGame.BrushCount ||
                _queenButton == null || _queenPanel == null || _environmentButton == null || _environmentPanel == null ||
                _depotButton == null || _depotButtonImage == null || _depotPanel == null ||
                _wasteButton == null || _wasteButtonImage == null || _wastePanel == null ||
                _modeButtons == null || _modeButtons.Length != 3 ||
                _stanceButtons == null || _stanceButtons.Length != 2 || _stanceImages == null || _stanceImages.Length != 2 ||
                _typeButtons == null || _typeButtons.Length != CreatableRoles.Length || _typeLabels == null ||
                _typeLabels.Length != CreatableRoles.Length ||
                _treeButton == null || _treePanel == null || _pauseMenu == null || _raidAlert == null || _raidAlertText == null)
            {
                Debug.LogError($"{name}: AntFarmHud com referências faltando (rode Incrementant/Setup/Montar HUD da partida).", this);
                enabled = false;
                return;
            }
            _brushHandlers = new UnityAction[_brushButtons.Length];
            for (int i = 0; i < _brushHandlers.Length; i++)
            {
                int index = i;
                _brushHandlers[i] = () => _game.SetBrush(index);
            }
            _modeHandlers = new UnityAction[_modeButtons.Length];
            for (int i = 0; i < _modeHandlers.Length; i++)
            {
                int index = i;
                _modeHandlers[i] = () => SetMode((QueenMode)index);
            }
            _stanceHandlers = new UnityAction[_stanceButtons.Length];
            for (int i = 0; i < _stanceHandlers.Length; i++)
            {
                int index = i;
                _stanceHandlers[i] = () => SetStance((QueenStance)index);
            }
            _typeHandlers = new UnityAction[_typeButtons.Length];
            for (int i = 0; i < _typeHandlers.Length; i++)
            {
                int index = i;
                _typeHandlers[i] = () => ChooseType(CreatableRoles[index]);
            }
            _paintPanel.SetActive(false);
            _environmentPanel.SetActive(false);
            _depotPanel.SetActive(false);
            _wastePanel.SetActive(false);
            _queenPanel.SetActive(false);
            _treePanel.SetActive(false);
            _pauseMenu.gameObject.SetActive(false);
            _raidAlert.SetActive(false);
            if (_colonyPanel != null)
            {
                _colonyPanel.gameObject.SetActive(false);
                PanelAnimator.Attach(_colonyPanel.gameObject, PanelAnimator.PanelStyle.Pop);
            }
            // Entrada dos painéis: submenus e alerta com pop; telas cheias só com fade (têm fundo que não pode se mover).
            PanelAnimator.Attach(_paintPanel, PanelAnimator.PanelStyle.Pop);
            PanelAnimator.Attach(_environmentPanel, PanelAnimator.PanelStyle.Pop);
            PanelAnimator.Attach(_depotPanel, PanelAnimator.PanelStyle.Pop);
            PanelAnimator.Attach(_wastePanel, PanelAnimator.PanelStyle.Pop);
            PanelAnimator.Attach(_queenPanel, PanelAnimator.PanelStyle.Pop);
            PanelAnimator.Attach(_raidAlert, PanelAnimator.PanelStyle.Pop);
            PanelAnimator.Attach(_treePanel, PanelAnimator.PanelStyle.Fade);
            PanelAnimator.Attach(_pauseMenu.gameObject, PanelAnimator.PanelStyle.Fade);
            // Cliques de todos os botões da tela (painéis, menu do ESC, árvore, configurações).
            ButtonSfx.AttachAll(transform.root);
            ButtonJuice.AttachAll(transform.root);
            // A coluna do menu Colônia é o pai dos botões dela (sem campo próprio: cenas montadas antes da gaveta servem).
            _menuDrawer = new ColonyMenuDrawer((RectTransform)_paintButton.transform.parent);
            _tooltips = new HudTooltips(_raidAlertText.font);
            _floatingText = FloatingText.Create(_raidAlertText.font, _game.WorldCamera, GetComponentInParent<CanvasScaler>());
            AttachTooltips();
            if (_colonyPanel != null)
                _colonyPanel.AttachTooltips(_tooltips);
        }

        void OnDestroy()
        {
            _tooltips?.Dispose();
            if (_floatingText != null)
                Destroy(_floatingText.gameObject);
        }

        FloatingText _floatingText;

        /// <summary>Fatos visuais da colônia que viram número flutuante: nutriente consumido pela rainha e formiga ferida.</summary>
        void OnColonyFx(FxId id, Vector2Int cell, CellMaterial material)
        {
            if (_floatingText == null)
                return;
            if (id == FxId.NutrientGain)
                _floatingText.Gain(1, _game.CellToWorld(cell));
            else if (id == FxId.AntHurt)
                _floatingText.Hurt(1, _game.CellToWorld(cell));
        }

        /// <summary>Gasto real (compra, ovo) vira "-N" logo abaixo do cartão de Nutrientes, somado numa janela.</summary>
        void OnNutrientsSpent(int amount)
        {
            if (_floatingText == null)
                return;
            Vector2 card = RectTransformUtility.WorldToScreenPoint(null, _nutrientsValue.rectTransform.position);
            Canvas canvas = _nutrientsValue.canvas;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            _floatingText.Spent(amount, card + new Vector2(0f, -36f * scale));
        }

        /// <summary>Clique recusado no mundo (rainha sem vaga/nutrientes, dedo com o jogo pausado): "!" na célula.</summary>
        void OnWorldDenied(Vector2Int cell)
        {
            if (_floatingText != null)
                _floatingText.SpawnWorld(_game.CellToWorld(cell), "!", FloatingText.LossColor);
        }

        /// <summary>Dicas dos botões do menu Colônia, da velocidade e dos cartões; as das salas e dos cartões incluem o estado da produção.</summary>
        void AttachTooltips()
        {
            _tooltips.Attach(_paintButton, () => Loc.T("tip.menu.paint"));
            _tooltips.Attach(_environmentButton, () => Loc.T("tip.menu.environments"));
            _tooltips.Attach(_depotButton, () => Loc.T("tip.menu.depots"));
            _tooltips.Attach(_wasteButton, () => Loc.T("tip.brush.waste"));
            _tooltips.Attach(_queenButton, () => Loc.T("tip.menu.queen"));
            _tooltips.Attach(_treeButton, () => Loc.T("tip.menu.evolution"));
            _tooltips.Attach(_pauseButton, () => Loc.T("tip.pause"));
            if (_recallButton != null)
                _tooltips.Attach(_recallButton, () => Loc.T(_game.Colony != null && _game.Colony.Recalled ? "tip.recall.off" : "tip.recall.on"));
            if (_colonyButton != null)
                _tooltips.Attach(_colonyButton, () => Loc.T("tip.menu.colony"));
            if (_roomsButton != null)
                _tooltips.Attach(_roomsButton, () => Loc.T(GameSettings.Current.ShowRooms ? "tip.rooms.hide" : "tip.rooms.show"));
            if (_glossaryButton != null)
                _tooltips.Attach(_glossaryButton, () => Loc.T("tip.glossary"));
            for (int i = 0; i < _brushButtons.Length; i++)
            {
                int index = i;
                _tooltips.Attach(_brushButtons[i], () => HudTooltipTexts.Brush(_game, index));
            }
            for (int i = 0; i < _modeButtons.Length; i++)
            {
                var mode = (QueenMode)i;
                _tooltips.Attach(_modeButtons[i], () => HudTooltipTexts.Mode(mode));
            }
            for (int i = 0; i < _stanceButtons.Length; i++)
            {
                var stance = (QueenStance)i;
                _tooltips.Attach(_stanceButtons[i], () => HudTooltipTexts.Stance(stance));
            }
            for (int i = 0; i < _typeButtons.Length; i++)
            {
                AntRole role = CreatableRoles[i];
                _tooltips.Attach(_typeButtons[i], () => HudTooltipTexts.Role(_game.Colony, role));
            }
            AttachCardTooltip(_nutrientsValue, () => HudTooltipTexts.NutrientsCard(_game.Colony, _game.Grid));
            AttachCardTooltip(_antsValue, () => HudTooltipTexts.AntsCard(_game.Colony));
            AttachCardTooltip(_queenValue, () => HudTooltipTexts.QueenCard(_game.Colony, _game.Grid));
            _tooltips.SetWorldSource(HoveredRoomKey, () => TryGetHoveredRoom(out PaintRoom room) ? HudTooltipTexts.PaintedRoom(_game, room) : null);
        }

        /// <summary>Ambiente pintado sob o mouse no mapa, só com a pintura dos ambientes à mostra (botão Mostrar ambientes).</summary>
        bool TryGetHoveredRoom(out PaintRoom room)
        {
            room = default;
            if (!GameSettings.Current.ShowRooms || !_game.PointerOverWorld || _game.Grid == null)
                return false;
            Vector2Int cell = _game.PointerCell;
            return PaintBudget.TryGetRoomAt(_game.Grid, cell.x, cell.y, out room);
        }

        /// <summary>Chave da dica do mapa: o ambiente + 1, 0 = nenhum; trocar de ambiente recomeça a espera.</summary>
        int HoveredRoomKey() => TryGetHoveredRoom(out PaintRoom room) ? (int)room + 1 : 0;

        /// <summary>O cartão é o pai do texto do valor; ele precisa receber o raio do mouse para a dica aparecer.</summary>
        void AttachCardTooltip(Text value, Func<string> text)
        {
            Transform card = value.transform.parent;
            Graphic graphic = card.GetComponent<Graphic>();
            if (graphic != null)
                graphic.raycastTarget = true;
            _tooltips.Attach(card, text);
        }

        void OnEnable()
        {
            _game.ColonyFx += OnColonyFx;
            _game.ColonyNutrientsSpent += OnNutrientsSpent;
            _game.WorldDenied += OnWorldDenied;
            _paintButton.onClick.AddListener(TogglePaint);
            _pauseButton.onClick.AddListener(TogglePause);
            if (_recallButton != null)
                _recallButton.onClick.AddListener(ToggleRecall);
            if (_colonyButton != null && _colonyPanel != null)
                _colonyButton.onClick.AddListener(ToggleColony);
            if (_roomsButton != null)
                _roomsButton.onClick.AddListener(ToggleShowRooms);
            if (_glossaryButton != null && _glossary != null)
                _glossaryButton.onClick.AddListener(_glossary.Open);
            _environmentButton.onClick.AddListener(ToggleEnvironment);
            _depotButton.onClick.AddListener(ToggleDepot);
            _wasteButton.onClick.AddListener(ToggleWaste);
            _queenButton.onClick.AddListener(ToggleQueen);
            _treeButton.onClick.AddListener(ToggleTree);
            for (int i = 0; i < _modeButtons.Length; i++)
                _modeButtons[i].onClick.AddListener(_modeHandlers[i]);
            for (int i = 0; i < _stanceButtons.Length; i++)
                _stanceButtons[i].onClick.AddListener(_stanceHandlers[i]);
            for (int i = 0; i < _typeButtons.Length; i++)
                _typeButtons[i].onClick.AddListener(_typeHandlers[i]);
            for (int i = 0; i < _brushButtons.Length; i++)
                _brushButtons[i].onClick.AddListener(_brushHandlers[i]);
        }

        void OnDisable()
        {
            _game.ColonyFx -= OnColonyFx;
            _game.ColonyNutrientsSpent -= OnNutrientsSpent;
            _game.WorldDenied -= OnWorldDenied;
            SetFingerCursor(false);
            _paintButton.onClick.RemoveListener(TogglePaint);
            _pauseButton.onClick.RemoveListener(TogglePause);
            if (_recallButton != null)
                _recallButton.onClick.RemoveListener(ToggleRecall);
            if (_colonyButton != null && _colonyPanel != null)
                _colonyButton.onClick.RemoveListener(ToggleColony);
            if (_roomsButton != null)
                _roomsButton.onClick.RemoveListener(ToggleShowRooms);
            if (_glossaryButton != null && _glossary != null)
                _glossaryButton.onClick.RemoveListener(_glossary.Open);
            _environmentButton.onClick.RemoveListener(ToggleEnvironment);
            _depotButton.onClick.RemoveListener(ToggleDepot);
            _wasteButton.onClick.RemoveListener(ToggleWaste);
            _queenButton.onClick.RemoveListener(ToggleQueen);
            _treeButton.onClick.RemoveListener(ToggleTree);
            for (int i = 0; i < _modeButtons.Length; i++)
                _modeButtons[i].onClick.RemoveListener(_modeHandlers[i]);
            for (int i = 0; i < _stanceButtons.Length; i++)
                _stanceButtons[i].onClick.RemoveListener(_stanceHandlers[i]);
            for (int i = 0; i < _typeButtons.Length; i++)
                _typeButtons[i].onClick.RemoveListener(_typeHandlers[i]);
            for (int i = 0; i < _brushButtons.Length; i++)
                _brushButtons[i].onClick.RemoveListener(_brushHandlers[i]);
        }

        void TogglePaint() => OpenMenu(_menu == Menu.Paint ? Menu.None : Menu.Paint);

        void ToggleEnvironment() => OpenMenu(_menu == Menu.Environment ? Menu.None : Menu.Environment);

        void ToggleQueen() => OpenMenu(_menu == Menu.Queen ? Menu.None : Menu.Queen);

        /// <summary>DEPÓSITO, dentro do Ambientes: abre ou fecha o submenu dos três depósitos ao lado.</summary>
        void ToggleDepot()
        {
            _depotOpen = !_depotOpen;
            _wasteOpen &= !_depotOpen;
            RefreshBrushSubmenus();
        }

        /// <summary>RESÍDUOS, dentro do Ambientes: abre ou fecha o submenu dos três modos da área de resíduos (um submenu por vez).</summary>
        void ToggleWaste()
        {
            _wasteOpen = !_wasteOpen;
            _depotOpen &= !_wasteOpen;
            RefreshBrushSubmenus();
        }

        // Submenus DEPÓSITO e RESÍDUOS abertos dentro do Ambientes (no máximo um). Ao abrir o Ambientes, volta aberto o do pincel atual.
        bool _depotOpen;
        bool _wasteOpen;

        void RefreshBrushSubmenus()
        {
            _depotPanel.SetActive(_menu == Menu.Environment && _depotOpen);
            _wastePanel.SetActive(_menu == Menu.Environment && _wasteOpen);
        }

        /// <summary>O submenu dos depósitos está aberto (para o tutorial e as dicas).</summary>
        public bool DepotMenuOpen => _menu == Menu.Environment && _depotOpen;

        /// <summary>O submenu RESÍDUOS está aberto (para o tutorial e as dicas).</summary>
        public bool WasteMenuOpen => _menu == Menu.Environment && _wasteOpen;

        void ToggleTree() => OpenMenu(_menu == Menu.Tree ? Menu.None : Menu.Tree);

        void ToggleColony() => OpenMenu(_menu == Menu.Colony ? Menu.None : Menu.Colony);

        /// <summary>Um submenu por vez, ao lado do menu Colônia (a árvore abre no centro da tela).</summary>
        void OpenMenu(Menu menu)
        {
            _menu = menu;
            _paintPanel.SetActive(menu == Menu.Paint);
            _environmentPanel.SetActive(menu == Menu.Environment);
            if (menu == Menu.Environment)
            {
                _depotOpen = AntFarmGame.IsDepotBrush(_game.BrushIndex);
                _wasteOpen = AntFarmGame.IsWasteBrush(_game.BrushIndex);
            }
            RefreshBrushSubmenus();
            _queenPanel.SetActive(menu == Menu.Queen);
            _treePanel.SetActive(menu == Menu.Tree);
            if (_colonyPanel != null)
                _colonyPanel.gameObject.SetActive(menu == Menu.Colony);
            _nextTextRefresh = 0f;
        }

        void SetMode(QueenMode mode)
        {
            AntColony colony = _game.Colony;
            if (colony != null)
                colony.SetQueenPolicy(mode, colony.QueenTarget);
        }

        void SetStance(QueenStance stance)
        {
            AntColony colony = _game.Colony;
            if (colony != null)
                colony.QueenStance = stance;
        }

        /// <summary>Escolher um tipo já põe a rainha no modo específico.</summary>
        void ChooseType(AntRole role)
        {
            AntColony colony = _game.Colony;
            if (colony != null && colony.Tree.IsRoleUnlocked(role))
                colony.SetQueenPolicy(QueenMode.Specific, role);
        }

        void TogglePause() => _game.SetPaused(!_game.Paused);

        void ToggleRecall()
        {
            AntColony colony = _game.Colony;
            if (colony == null)
                return;
            colony.Recalled = !colony.Recalled;
            GameAudio.Play(SfxId.EmergencyBell);
        }

        /// <summary>
        /// Mostrar ambientes é uma preferência do jogador (vale para as próximas partidas), então grava o arquivo de
        /// configurações a cada clique; o <see cref="AntFarmGame"/> lê o valor e redesenha o terreno.
        /// </summary>
        void ToggleShowRooms()
        {
            GameSettings.Current.ShowRooms = !GameSettings.Current.ShowRooms;
            GameSettings.NotifyChanged();
            GameSettings.Save();
        }

        /// <summary>
        /// Um ESC fecha a camada de cima: glossário → configurações → menu → jogo; sem menu aberto, fecha a tela de evolução ou o
        /// submenu da colônia; sem nada aberto, abre o menu. Fica tudo aqui para duas telas não reagirem ao mesmo ESC.
        /// </summary>
        void HandleEscape()
        {
            if (_tutorial != null && _tutorial.ModalOpen)
                return; // o pop-up central do tutorial é modal
            if (VersionHistoryScreen.BlocksEscape)
                return; // o histórico de versões fecha sozinho
            if (_glossary != null && _glossary.IsOpen)
                _glossary.Close(); // aberto pelo menu do ESC, volta para ele
            else if (_pauseMenu.IsOpen)
                _pauseMenu.Back();
            else if (_menu != Menu.None || _treePanel.activeSelf || ColonyPanelOpen)
                OpenMenu(Menu.None);
            else
                _pauseMenu.Open();
        }

        void SetFingerCursor(bool shown)
        {
            if (_fingerCursor == null || shown == _fingerCursorShown)
                return;
            _fingerCursorShown = shown;
            Cursor.SetCursor(shown ? _fingerCursor : null, shown ? _fingerCursorHotspot : Vector2.zero, CursorMode.Auto);
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                HandleEscape();

            SetFingerCursor(_game.FingerSelected && _game.PointerOverWorld);
            _tooltips.Tick();
            // Recuada até o cursor chegar perto; aberta enquanto um submenu dela está aberto ou o tutorial fala dela.
            bool submenuOpen = _menu == Menu.Paint || _menu == Menu.Environment || _menu == Menu.Queen;
            _menuDrawer.Tick(submenuOpen || (_tutorial != null && _tutorial.HoldsColonyMenu));

            // Tempo de relógio real desta sessão (ignora pausa e velocidade); FPS médio entre duas atualizações do texto.
            _playedSeconds += Time.unscaledDeltaTime;
            _statsFrames++;
            _statsElapsed += Time.unscaledDeltaTime;
            bool showStats = GameSettings.Current.ShowStats;
            if (_stats != null && _stats.enabled != showStats)
                _stats.enabled = showStats;
            if (_stats != null && _statsElapsed >= TextRefreshSeconds)
            {
                _stats.text = Loc.T("hud.stats", Mathf.RoundToInt(_statsFrames / _statsElapsed), FormatPlayTime(_playedSeconds));
                _statsFrames = 0;
                _statsElapsed = 0f;
            }

            AntColony colony = _game.Colony;
            if (colony == null)
                return;

            // A tela de evolução se fecha sozinha (ESC ou VOLTAR): o menu volta a ficar sem submenu aberto.
            if (_menu == Menu.Tree && !_treePanel.activeSelf)
                _menu = Menu.None;
            // O painel da colônia também fecha sozinho (botão X).
            if (_menu == Menu.Colony && !ColonyPanelOpen)
                _menu = Menu.None;
            RefreshStates();
            if (Time.unscaledTime < _nextTextRefresh)
                return;
            _nextTextRefresh = Time.unscaledTime + TextRefreshSeconds;
            RefreshTexts(colony);
        }

        static string FormatPlayTime(float seconds)
        {
            int total = (int)seconds;
            return $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}";
        }

        void RefreshStates()
        {
            _paintButtonImage.sprite = _menu == Menu.Paint ? _buttonActive : _buttonIdle;
            _environmentButtonImage.sprite = _menu == Menu.Environment ? _buttonActive : _buttonIdle;
            _depotButtonImage.sprite = DepotMenuOpen || AntFarmGame.IsDepotBrush(_game.BrushIndex) ? _buttonActive : _buttonIdle;
            _wasteButtonImage.sprite = WasteMenuOpen || AntFarmGame.IsWasteBrush(_game.BrushIndex) ? _buttonActive : _buttonIdle;
            // A área de resíduos só abre depois do primeiro ataque de cupins; a sala de folhas está livre desde o começo.
            bool wasteUnlocked = _game.Colony != null;
            if (_wasteButton.interactable != wasteUnlocked)
                _wasteButton.interactable = wasteUnlocked;
            _queenButtonImage.sprite = _menu == Menu.Queen ? _buttonActive : _buttonIdle;
            _treeButtonImage.sprite = _menu == Menu.Tree ? _buttonActive : _buttonIdle;
            AntColony colony = _game.Colony;

            for (int i = 0; i < _modeImages.Length; i++)
                _modeImages[i].sprite = i == (int)colony.QueenMode ? _buttonActive : _buttonIdle;
            for (int i = 0; i < _stanceImages.Length; i++)
                _stanceImages[i].sprite = i == (int)colony.QueenStance ? _buttonActive : _buttonIdle;
            for (int i = 0; i < _typeImages.Length; i++)
            {
                AntRole role = CreatableRoles[i];
                _typeImages[i].sprite = colony.QueenMode == QueenMode.Specific && colony.QueenTarget == role &&
                                        colony.Tree.IsRoleUnlocked(role) ? _buttonActive : _buttonIdle;
                _typeImages[i].color = colony.Tree.IsRoleUnlocked(role) ? Color.white : BlockedTint;
            }

            for (int i = 0; i < _brushImages.Length; i++)
            {
                _brushImages[i].sprite = i == _game.BrushIndex ? _buttonActive : _buttonIdle;
                // A área de resíduos só abre depois do primeiro ataque de cupins.
                bool unlocked = _game.IsBrushUnlocked(i);
                if (_brushButtons[i].interactable != unlocked)
                    _brushButtons[i].interactable = unlocked;
            }
            RefreshAreaBars(colony);
            _pauseImage.sprite = _game.Paused ? _iconButtonActive : _iconButtonIdle;
            if (_recallImage != null)
                _recallImage.sprite = colony.Recalled ? _iconButtonActive : _iconButtonIdle;
            if (_colonyImage != null)
                _colonyImage.sprite = _menu == Menu.Colony ? _iconButtonActive : _iconButtonIdle;
            if (_roomsImage != null)
                _roomsImage.sprite = GameSettings.Current.ShowRooms ? _iconButtonActive : _iconButtonIdle;
        }

        static readonly Color BlockedTint = new Color(0.62f, 0.62f, 0.62f);
        static readonly Color AreaFree = new Color(0.55f, 0.78f, 0.35f);
        static readonly Color AreaLow = new Color(0.93f, 0.64f, 0.22f);
        static readonly Color AreaTrack = new Color(0f, 0f, 0f, 0.35f);
        static readonly Color AreaFullTrack = new Color(0.75f, 0.2f, 0.15f, 0.75f);
        // Abaixo desta parte livre a barra fica laranja (aviso antes de acabar).
        const float AreaLowFraction = 0.2f;

        /// <summary>
        /// Barras de área livre dos pincéis de ambiente (KI-043): o preenchimento encolhe pela âncora conforme o limite é
        /// usado; sem área, o fundo fica vermelho e o pincel recusa pintar (o mesmo aviso da rainha sem nutrientes).
        /// </summary>
        void RefreshAreaBars(AntColony colony)
        {
            if (_brushFills == null || _menu != Menu.Environment)
                return;
            for (int i = 0; i < _brushFills.Length; i++)
            {
                Image fill = _brushFills[i];
                if (fill == null || !AntFarmGame.TryGetPaintRoom(i, out PaintRoom room))
                    continue;
                float free = colony.PaintBudget.AvailableFraction(room);
                RectTransform rect = fill.rectTransform;
                if (!Mathf.Approximately(rect.anchorMax.x, free))
                    rect.anchorMax = new Vector2(free, 1f);
                fill.color = free < AreaLowFraction ? AreaLow : AreaFree;
                if (fill.transform.parent.TryGetComponent(out Image track))
                    track.color = free <= 0f ? AreaFullTrack : AreaTrack;
            }
        }

        // Último valor mostrado, para pulsar o contador só quando ele muda (-1 = ainda sem leitura, não pulsa).
        int _shownNutrients = -1;
        int _shownAnts = -1;

        void PulseCounters(AntColony colony)
        {
            int nutrients = colony.Nutrients;
            if (_shownNutrients >= 0 && nutrients != _shownNutrients)
            {
                // Os números vêm dos fatos (ganho junto da rainha, gasto em OnNutrientsSpent); aqui só o contador reage.
                Pulse.Play(_nutrientsValue, nutrients > _shownNutrients ? Pulse.Gain : Pulse.Loss);
            }
            _shownNutrients = nutrients;

            int ants = colony.Ants.Count;
            if (_shownAnts >= 0 && ants > _shownAnts)
                Pulse.Play(_antsValue, Pulse.Gain);
            _shownAnts = ants;
        }

        void RefreshTexts(AntColony colony)
        {
            PulseCounters(colony);
            _nutrientsValue.text = Loc.T("hud.nutrients.value", colony.Nutrients);
            _nutrientsDetail.text = Loc.T("hud.nutrients.consumed", colony.TotalNutrients);

            _antsValue.text = Loc.T("hud.ants.adults", colony.Ants.Count, colony.AdultCapacity);
            _antsDetail.text = Loc.T("hud.brood", colony.Brood.Eggs, colony.Brood.Larvae, colony.Brood.Pupae);

            _queenValue.text = QueenStatus(colony);
            _queenDetail.text = colony.QueenRoomSize > 0 ? Loc.T("hud.queen.room", colony.QueenRoomSize) : Loc.T("hud.queen.room.none");

            AntRole? next = colony.NextAntRole;
            _queenState.text = BreedingText(colony);
            _queenNext.text = next.HasValue ? Loc.T("hud.queen.next", RoleLabel(next.Value), colony.CostOf(next.Value)) : Loc.T("hud.queen.next.none");
            _queenBrood.text = Loc.T("hud.brood", colony.Brood.Eggs, colony.Brood.Larvae, colony.Brood.Pupae);
            _queenSpace.text = Loc.T("hud.queen.space", colony.Ants.Count + colony.Brood.Count, colony.AdultCapacity, colony.Brood.Count, colony.NurserySlots);
            for (int i = 0; i < _typeLabels.Length; i++)
            {
                AntRole role = CreatableRoles[i];
                if (!colony.Tree.IsRoleUnlocked(role))
                {
                    QueenNode unlock = QueenTree.UnlockNodeOf(role).Value;
                    _typeLabels[i].text = Loc.T("hud.type.locked", RoleLabel(role), unlock);
                    continue;
                }
                int reserved = colony.Brood.CountOf(role);
                string count = reserved > 0 ? $"×{colony.CountOf(role)}+{reserved}" : $"×{colony.CountOf(role)}";
                _typeLabels[i].text = Loc.T("hud.type.count", RoleLabel(role), count, colony.CostOf(role));
            }

            string status = _game.SaveStatus;
            _hint.text = status.Length > 0 ? status.ToUpperInvariant() : Loc.T(DefaultHintKey);
            RefreshRaidAlert(colony);
        }

        int _seenRaids = -1;
        float _raidOverUntil;
        bool _raidOverFirst;
        const float RaidOverSeconds = 6f;

        /// <summary>Alerta no topo: ataque iminente (contagem), cupins atacando (quantos e o que fazem) e o fim do ataque.</summary>
        void RefreshRaidAlert(AntColony colony)
        {
            // Ataque que acabou de terminar: avisa por alguns segundos (o primeiro libera o soldado e a área de resíduos).
            if (_seenRaids >= 0 && colony.RaidsSurvived > _seenRaids)
            {
                _raidOverUntil = Time.unscaledTime + RaidOverSeconds;
                _raidOverFirst = _seenRaids == 0;
            }
            _seenRaids = colony.RaidsSurvived;

            string text = null;
            switch (colony.RaidState)
            {
                case RaidState.Warning:
                    text = colony.RaidHeldByStorm
                        ? Loc.T("raid.storm")
                        : Loc.T("raid.warning", Mathf.CeilToInt(colony.SecondsToNextRaid));
                    break;
                case RaidState.Active:
                    text = Loc.T("raid.active", colony.LiveTermites, ObjectiveText(colony.RaidObjective));
                    break;
                default:
                    if (Time.unscaledTime < _raidOverUntil)
                        text = Loc.T(_raidOverFirst ? "raid.over.first" : "raid.over");
                    break;
            }
            bool show = text != null;
            if (_raidAlert.activeSelf != show)
                _raidAlert.SetActive(show);
            if (show)
                _raidAlertText.text = text;
        }

        static string ObjectiveText(RaidObjective objective)
        {
            switch (objective)
            {
                case RaidObjective.Brood: return Loc.T("raid.obj.brood");
                case RaidObjective.Aphids: return Loc.T("raid.obj.aphids");
                case RaidObjective.Farms: return Loc.T("raid.obj.farms");
                case RaidObjective.Sectors: return Loc.T("raid.obj.sectors");
                case RaidObjective.Walls: return Loc.T("raid.obj.walls");
                case RaidObjective.Queen: return Loc.T("raid.obj.queen");
                default: return Loc.T("raid.obj.leave");
            }
        }

        /// <summary>Estado da reprodução em palavras, sem sugerir defeito da rainha (seção 11 do design).</summary>
        static string BreedingText(AntColony colony)
        {
            switch (colony.Breeding)
            {
                case BreedingState.Paused:
                    return Loc.T("breed.paused");
                case BreedingState.WaitingQueenRoom:
                    return Loc.T(colony.QueenRoomSize == 0 ? "breed.noqueenroom" : "breed.queenout");
                case BreedingState.WaitingNursery:
                    return Loc.T(colony.NurserySlots == 0 ? "breed.nonursery" : "breed.nurseryfull");
                case BreedingState.WaitingExpansion:
                    return Loc.T("breed.expand");
                case BreedingState.WaitingResources:
                    return Loc.T("breed.resources", colony.MinNutrientReserve);
                default:
                    return Loc.T("breed.next", Mathf.CeilToInt(colony.Brood.SecondsToNextLay));
            }
        }

        static string QueenStatus(AntColony colony)
        {
            if (colony.QueenRoomSize == 0)
                return Loc.T("queen.status.noroom");
            if (colony.QueenInRoom)
                return Loc.T("queen.status.inroom");
            return Loc.T(colony.QueenCanReachRoom ? "queen.status.going" : "queen.status.nopath");
        }
    }
}
