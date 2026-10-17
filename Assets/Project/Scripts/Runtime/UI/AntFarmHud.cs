using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using UnityEngine;
using UnityEngine.Events;
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

        [Header("Menu Colônia")]
        [SerializeField] Button _paintButton;
        [SerializeField] Image _paintButtonImage;
        [SerializeField] GameObject _paintPanel;
        [SerializeField] Button[] _brushButtons;
        [SerializeField] Image[] _brushImages;

        [Header("Menu Rainha (política: modo e tipo da próxima formiga)")]
        [SerializeField] Button _queenButton;
        [SerializeField] Image _queenButtonImage;
        [SerializeField] GameObject _queenPanel;
        [SerializeField] Button[] _modeButtons;
        [SerializeField] Image[] _modeImages;
        [SerializeField] Button[] _typeButtons;
        [SerializeField] Image[] _typeImages;
        [SerializeField] Text[] _typeLabels;
        [SerializeField] Text _queenState;
        [SerializeField] Text _queenNext;
        [SerializeField] Text _queenBrood;
        [SerializeField] Text _queenSpace;

        [Header("Velocidade")]
        [SerializeField] Button _pauseButton;
        [SerializeField] Image _pauseImage;
        [SerializeField] Button[] _speedButtons;
        [SerializeField] Image[] _speedImages;

        [Header("Sprites de estado")]
        [SerializeField] Sprite _buttonIdle;
        [SerializeField] Sprite _buttonActive;
        [SerializeField] Sprite _iconButtonIdle;
        [SerializeField] Sprite _iconButtonActive;

        const string DefaultHint = "WASD / BOTÃO DO MEIO: MOVER CÂMERA";
        const float TextRefreshSeconds = 0.25f;

        /// <summary>Papéis que a rainha pode criar, na ordem dos botões de tipo.</summary>
        public static AntRole[] CreatableRoles => AntRoles.All;

        public static string RoleLabel(AntRole role) => AntRoles.Label(role).ToUpperInvariant();

        public static string ModeLabel(QueenMode mode) =>
            mode == QueenMode.Automatic ? "AUTOMÁTICO" : mode == QueenMode.Specific ? "TIPO ESPECÍFICO" : "PAUSADO";

        enum Menu { None, Paint, Queen }

        UnityAction[] _brushHandlers;
        UnityAction[] _speedHandlers;
        UnityAction[] _modeHandlers;
        UnityAction[] _typeHandlers;
        float _nextTextRefresh;
        Menu _menu;

        void Awake()
        {
            if (_game == null || _paintButton == null || _paintPanel == null || _pauseButton == null ||
                _brushButtons == null || _brushButtons.Length != AntFarmGame.BrushCount ||
                _speedButtons == null || _speedButtons.Length != AntFarmGame.SpeedCount ||
                _queenButton == null || _queenPanel == null ||
                _modeButtons == null || _modeButtons.Length != 3 ||
                _typeButtons == null || _typeButtons.Length != CreatableRoles.Length || _typeLabels == null ||
                _typeLabels.Length != CreatableRoles.Length)
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
            _speedHandlers = new UnityAction[_speedButtons.Length];
            for (int i = 0; i < _speedHandlers.Length; i++)
            {
                int index = i;
                _speedHandlers[i] = () => _game.SetSpeed(index);
            }
            _modeHandlers = new UnityAction[_modeButtons.Length];
            for (int i = 0; i < _modeHandlers.Length; i++)
            {
                int index = i;
                _modeHandlers[i] = () => SetMode((QueenMode)index);
            }
            _typeHandlers = new UnityAction[_typeButtons.Length];
            for (int i = 0; i < _typeHandlers.Length; i++)
            {
                int index = i;
                _typeHandlers[i] = () => ChooseType(CreatableRoles[index]);
            }
            _paintPanel.SetActive(false);
            _queenPanel.SetActive(false);
        }

        void OnEnable()
        {
            _paintButton.onClick.AddListener(TogglePaint);
            _pauseButton.onClick.AddListener(TogglePause);
            _queenButton.onClick.AddListener(ToggleQueen);
            for (int i = 0; i < _modeButtons.Length; i++)
                _modeButtons[i].onClick.AddListener(_modeHandlers[i]);
            for (int i = 0; i < _typeButtons.Length; i++)
                _typeButtons[i].onClick.AddListener(_typeHandlers[i]);
            for (int i = 0; i < _brushButtons.Length; i++)
                _brushButtons[i].onClick.AddListener(_brushHandlers[i]);
            for (int i = 0; i < _speedButtons.Length; i++)
                _speedButtons[i].onClick.AddListener(_speedHandlers[i]);
        }

        void OnDisable()
        {
            _paintButton.onClick.RemoveListener(TogglePaint);
            _pauseButton.onClick.RemoveListener(TogglePause);
            _queenButton.onClick.RemoveListener(ToggleQueen);
            for (int i = 0; i < _modeButtons.Length; i++)
                _modeButtons[i].onClick.RemoveListener(_modeHandlers[i]);
            for (int i = 0; i < _typeButtons.Length; i++)
                _typeButtons[i].onClick.RemoveListener(_typeHandlers[i]);
            for (int i = 0; i < _brushButtons.Length; i++)
                _brushButtons[i].onClick.RemoveListener(_brushHandlers[i]);
            for (int i = 0; i < _speedButtons.Length; i++)
                _speedButtons[i].onClick.RemoveListener(_speedHandlers[i]);
        }

        void TogglePaint() => OpenMenu(_menu == Menu.Paint ? Menu.None : Menu.Paint);

        void ToggleQueen() => OpenMenu(_menu == Menu.Queen ? Menu.None : Menu.Queen);

        /// <summary>Um submenu por vez, ao lado do menu Colônia.</summary>
        void OpenMenu(Menu menu)
        {
            _menu = menu;
            _paintPanel.SetActive(menu == Menu.Paint);
            _queenPanel.SetActive(menu == Menu.Queen);
        }

        void SetMode(QueenMode mode)
        {
            AntColony colony = _game.Colony;
            if (colony != null)
                colony.SetQueenPolicy(mode, colony.QueenTarget);
        }

        /// <summary>Escolher um tipo já põe a rainha no modo específico.</summary>
        void ChooseType(AntRole role) => _game.Colony?.SetQueenPolicy(QueenMode.Specific, role);

        void TogglePause() => _game.SetPaused(!_game.Paused);

        void Update()
        {
            AntColony colony = _game.Colony;
            if (colony == null)
                return;

            RefreshStates();
            if (Time.unscaledTime < _nextTextRefresh)
                return;
            _nextTextRefresh = Time.unscaledTime + TextRefreshSeconds;
            RefreshTexts(colony);
        }

        void RefreshStates()
        {
            _paintButtonImage.sprite = _menu == Menu.Paint ? _buttonActive : _buttonIdle;
            _queenButtonImage.sprite = _menu == Menu.Queen ? _buttonActive : _buttonIdle;
            AntColony colony = _game.Colony;
            for (int i = 0; i < _modeImages.Length; i++)
                _modeImages[i].sprite = i == (int)colony.QueenMode ? _buttonActive : _buttonIdle;
            for (int i = 0; i < _typeImages.Length; i++)
                _typeImages[i].sprite = colony.QueenMode == QueenMode.Specific && colony.QueenTarget == CreatableRoles[i]
                    ? _buttonActive : _buttonIdle;

            for (int i = 0; i < _brushImages.Length; i++)
                _brushImages[i].sprite = i == _game.BrushIndex ? _buttonActive : _buttonIdle;
            _pauseImage.sprite = _game.Paused ? _iconButtonActive : _iconButtonIdle;
            for (int i = 0; i < _speedImages.Length; i++)
                _speedImages[i].sprite = !_game.Paused && i == _game.SpeedIndex ? _iconButtonActive : _iconButtonIdle;
        }

        void RefreshTexts(AntColony colony)
        {
            _nutrientsValue.text = $"{colony.Nutrients} N";
            _nutrientsDetail.text = $"CONSUMIDOS: {colony.TotalNutrients}";

            _antsValue.text = $"ADULTAS {colony.Ants.Count} / {colony.AdultCapacity}";
            _antsDetail.text = $"OVOS {colony.Brood.Eggs} · LARVAS {colony.Brood.Larvae} · PUPAS {colony.Brood.Pupae}";

            _queenValue.text = QueenStatus(colony);
            _queenDetail.text = colony.QueenRoomSize > 0 ? $"SALA: {colony.QueenRoomSize} PX" : "SALA: —";

            AntRole? next = colony.NextAntRole;
            _queenState.text = BreedingText(colony);
            _queenNext.text = next.HasValue ? $"PRÓXIMO OVO: {RoleLabel(next.Value)}  ·  {colony.CostOf(next.Value)} N" : "PRÓXIMO OVO: —";
            _queenBrood.text = $"OVOS {colony.Brood.Eggs} · LARVAS {colony.Brood.Larvae} · PUPAS {colony.Brood.Pupae}";
            _queenSpace.text = $"ADULTAS {colony.Ants.Count + colony.Brood.Count} / {colony.AdultCapacity}  ·  BERÇÁRIO {colony.Brood.Count} / {colony.NurserySlots}";
            for (int i = 0; i < _typeLabels.Length; i++)
            {
                AntRole role = CreatableRoles[i];
                int reserved = colony.Brood.CountOf(role);
                string count = reserved > 0 ? $"×{colony.CountOf(role)} (+{reserved})" : $"×{colony.CountOf(role)}";
                _typeLabels[i].text = $"{RoleLabel(role)}  {count}  ·  {colony.CostOf(role)} N";
            }

            string status = _game.SaveStatus;
            _hint.text = status.Length > 0 ? status.ToUpperInvariant() : DefaultHint;
        }

        /// <summary>Estado da reprodução em palavras, sem sugerir defeito da rainha (seção 11 do design).</summary>
        static string BreedingText(AntColony colony)
        {
            switch (colony.Breeding)
            {
                case BreedingState.Paused:
                    return "PAUSADA: SEM OVOS NOVOS";
                case BreedingState.WaitingNursery:
                    return colony.NurserySlots == 0 ? "SEM BERÇÁRIO: PINTE E ESCAVE" : "BERÇÁRIO CHEIO";
                case BreedingState.WaitingExpansion:
                    return "SEM VAGA ADULTA: AMPLIE";
                case BreedingState.WaitingResources:
                    return $"AGUARDANDO RECURSOS (RES. {colony.MinNutrientReserve} N)";
                default:
                    return $"PRÓXIMO OVO EM {Mathf.CeilToInt(colony.Brood.SecondsToNextLay)} S";
            }
        }

        static string QueenStatus(AntColony colony)
        {
            if (colony.QueenRoomSize == 0)
                return "SEM SALA";
            if (colony.QueenInRoom)
                return "NA SALA";
            return colony.QueenCanReachRoom ? "INDO PARA A SALA" : "SEM CAMINHO";
        }
    }
}
