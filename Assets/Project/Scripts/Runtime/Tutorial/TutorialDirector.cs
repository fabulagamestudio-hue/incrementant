using Fabula.Incrementant.Texts;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.Tutorial
{
    /// <summary>
    /// Tutorial de todo jogo novo: um tour da interface. Cada etapa destaca uma peça do HUD (cartões, menus, barra inferior) e
    /// explica como lê-la e o que ela faz; o jogador avança com Próximo/Voltar ou pula, e pode clicar em tudo no meio do
    /// caminho. O jogo fica pausado e sem ataques de cupins até o fim. Os pop-ups e botões vêm montados pelo setup do HUD
    /// (<c>Incrementant/Setup/Montar HUD da partida</c>); as molduras de destaque são criadas aqui.
    /// Regras e roteiro: <c>TutorialGuide.md</c>.
    /// </summary>
    public sealed class TutorialDirector : MonoBehaviour
    {
        [SerializeField] AntFarmGame _game;
        [SerializeField] AntFarmHud _hud;

        [Header("Pop-up central (boas-vindas e despedida)")]
        [SerializeField] GameObject _modal;
        [SerializeField] RectTransform _modalPanel;
        [SerializeField] Text _modalTitle;
        [SerializeField] Text _modalText;
        [SerializeField] Button _modalButton;
        [SerializeField] Text _modalButtonLabel;

        [Header("Pop-up lateral (ao lado da peça explicada)")]
        [SerializeField] GameObject _side;
        [SerializeField] RectTransform _sidePanel;
        [SerializeField] Text _sideTitle;
        [SerializeField] Text _sideText;
        [SerializeField] Text _sideCounter;
        [SerializeField] Button _nextButton;
        [SerializeField] Text _nextLabel;
        [SerializeField] Button _backButton;

        [Header("Botão fixo")]
        [SerializeField] Button _skipButton;

        enum Anchor { Right, Ui }

        /// <summary>Uma parada do tour: peça destacada (ou nenhuma), título e texto (chaves da tabela).</summary>
        readonly struct Stop
        {
            public readonly AntFarmHud.Spot? Spot;
            public readonly string Title, Text;

            public Stop(AntFarmHud.Spot? spot, string title, string text)
            {
                Spot = spot;
                Title = title;
                Text = text;
            }
        }

        // Ordem do tour: ler a tela de cima para baixo, depois os menus, a barra inferior e o que não tem botão.
        static readonly Stop[] Stops =
        {
            new Stop(null, "tutorial.tour.camera.title", "tutorial.tour.camera"),
            new Stop(AntFarmHud.Spot.NutrientsCard, "tutorial.tour.nutrients.title", "tutorial.tour.nutrients"),
            new Stop(AntFarmHud.Spot.AntsCard, "tutorial.tour.ants.title", "tutorial.tour.ants"),
            new Stop(AntFarmHud.Spot.QueenCard, "tutorial.tour.queencard.title", "tutorial.tour.queencard"),
            new Stop(AntFarmHud.Spot.PaintMenu, "tutorial.tour.tools.title", "tutorial.tour.tools"),
            new Stop(AntFarmHud.Spot.EnvironmentMenu, "tutorial.tour.rooms.title", "tutorial.tour.rooms"),
            new Stop(AntFarmHud.Spot.QueenMenu, "tutorial.tour.queen.title", "tutorial.tour.queen"),
            new Stop(AntFarmHud.Spot.EvolutionMenu, "tutorial.tour.evolution.title", "tutorial.tour.evolution"),
            new Stop(AntFarmHud.Spot.PauseButton, "tutorial.tour.pause.title", "tutorial.tour.pause"),
            new Stop(AntFarmHud.Spot.ColonyButton, "tutorial.tour.colony.title", "tutorial.tour.colony"),
            new Stop(AntFarmHud.Spot.RecallButton, "tutorial.tour.bell.title", "tutorial.tour.bell"),
            new Stop(AntFarmHud.Spot.RoomsButton, "tutorial.tour.showrooms.title", "tutorial.tour.showrooms"),
            new Stop(null, "tutorial.tour.threats.title", "tutorial.tour.threats"),
            new Stop(AntFarmHud.Spot.GlossaryButton, "tutorial.tour.tips.title", "tutorial.tour.tips"),
        };

        const int Welcome = -1;
        static int Farewell => Stops.Length;

        const float CameraSeconds = 1.8f;
        const float SideWidth = 600f, ModalWidth = 900f, PopupGap = 28f, ScreenMargin = 20f;
        // Borda direita da coluna do menu Colônia, em unidades do canvas a partir da esquerda (layout do setup do HUD).
        const float ColonyMenuRight = 300f;
        static readonly Color Gold = new Color(1f, 0.66f, 0.1f);

        int _index;
        bool _begun;
        int _generation;
        bool _raidsWere;
        bool _inputLocked;
        string _lastSideText, _lastModalText;

        RectTransform _root;
        HighlightFrame _frame;
        readonly Vector3[] _corners = new Vector3[4];

        /// <summary>O pop-up central está na tela: o ESC não abre o menu e o mundo fica travado.</summary>
        public bool ModalOpen => _begun && (_index == Welcome || _index == Farewell);

        /// <summary>A parada atual explica um botão do menu Colônia: o HUD mantém a coluna aberta (ela fica recuada fora disso).</summary>
        public bool HoldsColonyMenu
        {
            get
            {
                if (!_begun || ModalOpen || !Stops[_index].Spot.HasValue)
                    return false;
                AntFarmHud.Spot spot = Stops[_index].Spot.Value;
                return spot == AntFarmHud.Spot.PaintMenu || spot == AntFarmHud.Spot.EnvironmentMenu ||
                       spot == AntFarmHud.Spot.QueenMenu || spot == AntFarmHud.Spot.EvolutionMenu;
            }
        }

        // ---------------------------------------------------------------- ciclo de vida

        void Awake()
        {
            if (_game == null || _hud == null || _modal == null || _modalPanel == null || _modalTitle == null || _modalText == null ||
                _modalButton == null || _modalButtonLabel == null || _side == null || _sidePanel == null || _sideTitle == null ||
                _sideText == null || _sideCounter == null || _nextButton == null || _nextLabel == null || _backButton == null ||
                _skipButton == null)
            {
                Debug.LogError($"{name}: TutorialDirector com referências faltando (rode Incrementant/Setup/Montar HUD da partida).", this);
                enabled = false;
                return;
            }
            _root = (RectTransform)transform;
            HideAll();
        }

        void OnEnable()
        {
            if (_modalButton == null)
                return;
            _modalButton.onClick.AddListener(Next);
            _nextButton.onClick.AddListener(Next);
            _backButton.onClick.AddListener(Back);
            _skipButton.onClick.AddListener(Finish);
        }

        void OnDisable()
        {
            if (_modalButton == null)
                return;
            _modalButton.onClick.RemoveListener(Next);
            _nextButton.onClick.RemoveListener(Next);
            _backButton.onClick.RemoveListener(Back);
            _skipButton.onClick.RemoveListener(Finish);
        }

        void Start()
        {
            // Todo jogo novo abre com o tutorial; só a vitrine do menu principal e a partida continuada de um save ficam sem.
            if (_game.Showcase || _game.StartedFromSave || _game.Colony == null)
            {
                enabled = false;
                return;
            }
            _generation = _game.Generation;
            _frame = HighlightFrame.Create(_root, "Destaque");
            // Sem ataque durante a explicação: o relógio dos cupins só volta a andar quando o tutorial termina.
            _raidsWere = _game.Colony.RaidsEnabled;
            _game.Colony.RaidsEnabled = false;
            // Pausa no começo; se o jogador despausar durante o tour (para testar a pausa, por exemplo), a escolha é dele.
            _game.SetPaused(true);
            // Abre pela vista mais ampla e desce até o abrigo da rainha; o pop-up fica na parte de cima da tela.
            Vector2Int nest = _game.Layout.Nest;
            _game.FrameCellsFromOverview(new Rect(nest.x - 20, nest.y - 10, 40, 22), CameraSeconds, 0.42f, new Vector2(0.5f, 0.3f));
            _begun = true;
            _index = Welcome;
        }

        void HideAll()
        {
            _modal.SetActive(false);
            _side.SetActive(false);
            _skipButton.gameObject.SetActive(false);
        }

        void Next()
        {
            if (_index >= Farewell)
                Finish();
            else
                _index++;
        }

        void Back()
        {
            if (_index > 0)
                _index--;
        }

        void Finish()
        {
            if (_begun)
            {
                if (_game.Colony != null && _generation == _game.Generation)
                    _game.Colony.RaidsEnabled = _raidsWere;
                _game.SetPaused(false);
            }
            SetInputLocked(false);
            HideAll();
            _frame?.Hide();
            _begun = false;
            enabled = false;
        }

        void SetInputLocked(bool locked)
        {
            if (_inputLocked == locked)
                return;
            _inputLocked = locked;
            _game.InputLocked = locked;
        }

        // ---------------------------------------------------------------- um quadro

        void Update()
        {
            if (!_begun)
                return;
            if (_game.Colony == null || _generation != _game.Generation)
            {
                // Mapa novo ou save carregado no meio do tour: o tutorial termina (só volta no próximo Novo jogo).
                Finish();
                return;
            }

            bool modal = ModalOpen;
            SetInputLocked(modal);
            if (_modal.activeSelf != modal)
                _modal.SetActive(modal);
            if (_side.activeSelf == modal)
                _side.SetActive(!modal);
            bool skip = _index != Farewell; // na despedida, fechar já encerra o tutorial
            if (_skipButton.gameObject.activeSelf != skip)
                _skipButton.gameObject.SetActive(skip);

            RectTransform target = null;
            if (modal)
                ShowModal();
            else
            {
                Stop stop = Stops[_index];
                target = stop.Spot.HasValue ? _hud.SpotRect(stop.Spot.Value) : null;
                // A tela de evolução cobre o HUD: a moldura não aponta para um botão escondido debaixo dela.
                if (target != null && (!target.gameObject.activeInHierarchy || _hud.TreeOpen))
                    target = null;
                ShowSide(stop, target);
            }
            ShowHighlight(target);
        }

        // ---------------------------------------------------------------- apresentação

        void ShowModal()
        {
            bool welcome = _index == Welcome;
            string text = Loc.T(welcome ? "tutorial.welcome" : "tutorial.farewell");
            _modalTitle.text = Loc.T(welcome ? "tutorial.title.welcome" : "tutorial.title.farewell");
            if (_lastModalText != text)
            {
                _lastModalText = text;
                _modalText.text = text;
            }
            _modalButtonLabel.text = Loc.T(welcome ? "tutorial.start" : "tutorial.explore");
            // título (32 + folga), texto, botão (56 + folga) e margens.
            _modalPanel.sizeDelta = new Vector2(ModalWidth, 36f + 64f + TextHeight(_modalText, text) + 32f + 56f + 36f);
            _modalPanel.anchorMin = _modalPanel.anchorMax = _modalPanel.pivot = new Vector2(0.5f, 0.5f);
            // Parte de cima da tela: a câmera enquadra o abrigo embaixo.
            _modalPanel.anchoredPosition = new Vector2(0f, _root.rect.height * 0.5f - _modalPanel.sizeDelta.y * 0.5f - 90f);
        }

        void ShowSide(Stop stop, RectTransform target)
        {
            string text = Loc.T(stop.Text);
            _sideTitle.text = Loc.T(stop.Title);
            if (_lastSideText != text)
            {
                _lastSideText = text;
                _sideText.text = text;
            }
            _sideCounter.text = $"{_index + 1}/{Stops.Length}";
            _nextLabel.text = Loc.T("tutorial.next");
            _backButton.interactable = _index > 0;
            // título (24 + folga), texto, botões (48 + folga) e margens.
            float height = 28f + 40f + TextHeight(_sideText, text) + 24f + 48f + 28f;
            _sidePanel.sizeDelta = new Vector2(SideWidth, height);
            _sidePanel.anchorMin = _sidePanel.anchorMax = _sidePanel.pivot = new Vector2(0.5f, 0.5f);
            _sidePanel.anchoredPosition = SidePosition(_sidePanel.sizeDelta, target);
        }

        float TextHeight(Text text, string content)
        {
            Vector2 size = text.rectTransform.rect.size;
            TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(size.x, 0f));
            return text.cachedTextGenerator.GetPreferredHeight(content, settings) / text.pixelsPerUnit;
        }

        /// <summary>
        /// Onde o pop-up fica. Cartões do topo: embaixo do cartão. Botões da barra inferior: acima dela. Menu Colônia (no alto à
        /// esquerda) e paradas sem peça: na direita da tela, para não cobrir os submenus que o jogador abrir.
        /// </summary>
        Vector2 SidePosition(Vector2 size, RectTransform target)
        {
            Rect canvas = _root.rect;
            float x = canvas.xMax - ScreenMargin - size.x * 0.5f;
            float y = canvas.center.y;
            if (target != null)
            {
                Rect t = LocalRectOf(target);
                if (t.center.y > canvas.yMax - 150f)
                {
                    // Nunca sobre a coluna do menu Colônia (esquerda, logo abaixo do cartão Nutrientes).
                    x = Mathf.Max(t.center.x, canvas.xMin + ColonyMenuRight + size.x * 0.5f);
                    y = t.yMin - PopupGap - size.y * 0.5f;
                }
                else if (t.center.y < canvas.yMin + 150f)
                {
                    x = t.xMin + size.x * 0.5f;
                    y = t.yMax + PopupGap + 40f + size.y * 0.5f;
                }
            }
            x = Mathf.Clamp(x, canvas.xMin + ScreenMargin + size.x * 0.5f, canvas.xMax - ScreenMargin - size.x * 0.5f);
            y = Mathf.Clamp(y, canvas.yMin + ScreenMargin + size.y * 0.5f, canvas.yMax - ScreenMargin - size.y * 0.5f);
            return new Vector2(x, y);
        }

        Rect LocalRectOf(RectTransform target)
        {
            target.GetWorldCorners(_corners);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, _corners[0], null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, _corners[2], null, out Vector2 max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        void ShowHighlight(RectTransform target)
        {
            if (target == null)
            {
                _frame.Hide();
                return;
            }
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 6f);
            // Opaca e grossa: precisa se destacar até sobre a borda acesa dos botões.
            _frame.Show(LocalRectOf(target), 8f + 6f * pulse, new Color(Gold.r, Gold.g, Gold.b, 0.8f + 0.2f * pulse));
        }

        // ---------------------------------------------------------------- moldura de destaque

        /// <summary>Moldura pulsante de 4 barras, desenhada em volta de um retângulo do canvas.</summary>
        sealed class HighlightFrame
        {
            const float Thickness = 8f;
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
