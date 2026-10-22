using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Texts;
using Iung.BuildAssistant.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Histórico de versões aberto pelo texto da versão (<see cref="BuildVersionOverlay"/>): à esquerda um botão por versão,
    /// a mais nova (a atual) no topo; à direita as notas da selecionada no idioma do jogo. O fundo escurecido cobre a tela e
    /// barra os cliques no mundo; na partida, pausa o jogo e trava a câmera como o menu do ESC, e devolve tudo ao fechar.
    /// Montado por <c>Incrementant/Setup/Criar overlay de versão</c>.
    /// </summary>
    public sealed class VersionHistoryScreen : MonoBehaviour
    {
        const string InputTimestampFormat = "yyyy-MM-dd HHmm";
        const string HeadingColor = "#7A4A1E";

        [SerializeField] RectTransform _listContent;
        [SerializeField] ScrollRect _listScroll;
        [SerializeField] Button _itemTemplate;
        [SerializeField] Sprite _itemNormal;
        [SerializeField] Sprite _itemSelected;
        [SerializeField] ScrollRect _detailScroll;
        [SerializeField] Text _title;
        [SerializeField] Text _date;
        [SerializeField] Text _body;
        [SerializeField] Button _close;

        readonly List<Button> _items = new List<Button>();
        IReadOnlyList<BuildAssistantChangelogEntry> _entries;
        int _selected = -1;
        AntFarmGame _game;
        bool _wasPaused;
        bool _wasLocked;
        static bool _open;
        static int _closedFrame = -1;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>Verdadeiro com a janela aberta e no frame em que o ESC a fechou: os outros menus ignoram esse ESC.</summary>
        public static bool BlocksEscape => _open || _closedFrame == Time.frameCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _open = false;
            _closedFrame = -1;
        }

        void Awake()
        {
            if (_listContent == null || _itemTemplate == null || _title == null || _date == null || _body == null || _close == null)
                Debug.LogError($"[VersionHistoryScreen] '{name}' com referências faltando; rode Incrementant/Setup/Criar overlay de versão.", this);
            else
                _itemTemplate.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (_close != null)
                _close.onClick.AddListener(Close);
            Loc.LanguageChanged += Refresh;
        }

        void OnDisable()
        {
            if (_close != null)
                _close.onClick.RemoveListener(Close);
            Loc.LanguageChanged -= Refresh;
            // Destruído aberto (troca de cena não destrói, mas sair do Play sim): solta o ESC dos outros menus.
            _open = false;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Close();
        }

        /// <summary>Abre com as versões dadas (a primeira é a atual) e seleciona a primeira.</summary>
        public void Open(IReadOnlyList<BuildAssistantChangelogEntry> entries)
        {
            _entries = entries ?? new List<BuildAssistantChangelogEntry>();
            if (!IsOpen)
            {
                _game = FindAnyObjectByType<AntFarmGame>();
                if (_game != null)
                {
                    _wasPaused = _game.Paused;
                    _wasLocked = _game.InputLocked;
                    _game.SetPaused(true);
                    _game.InputLocked = true;
                }
                gameObject.SetActive(true);
                _open = true;
            }
            BuildList();
            Select(0);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            gameObject.SetActive(false);
            _open = false;
            _closedFrame = Time.frameCount;
            if (_game != null)
            {
                _game.InputLocked = _wasLocked;
                _game.SetPaused(_wasPaused);
                _game = null;
            }
        }

        void BuildList()
        {
            foreach (Button item in _items)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            _items.Clear();
            for (int i = 0; i < _entries.Count; i++)
            {
                Button item = Instantiate(_itemTemplate, _listContent);
                item.name = "Versao" + i;
                item.gameObject.SetActive(true);
                int index = i;
                item.onClick.AddListener(() => Select(index));
                _items.Add(item);
            }
            if (_listScroll != null)
                _listScroll.verticalNormalizedPosition = 1f;
        }

        void Select(int index)
        {
            _selected = _entries.Count == 0 ? -1 : Mathf.Clamp(index, 0, _entries.Count - 1);
            Refresh();
            if (_detailScroll != null)
                _detailScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>Reescreve os rótulos da lista e o detalhe (também quando o idioma muda).</summary>
        void Refresh()
        {
            if (_entries == null)
                return;
            for (int i = 0; i < _items.Count; i++)
            {
                BuildAssistantChangelogEntry entry = _entries[i];
                Transform item = _items[i].transform;
                item.Find("Texto").GetComponent<Text>().text = entry.VersionLabel;
                string date = FormatDate(entry.Timestamp);
                item.Find("Data").GetComponent<Text>().text = i == 0 ? Loc.T("version.current") + (date.Length > 0 ? " · " + date : "") : date;
                ((Image)_items[i].targetGraphic).sprite = i == _selected ? _itemSelected : _itemNormal;
            }

            _title.gameObject.SetActive(_selected >= 0);
            _date.gameObject.SetActive(_selected >= 0);
            if (_selected < 0)
            {
                _body.text = Loc.T("version.empty");
                return;
            }
            BuildAssistantChangelogEntry selected = _entries[_selected];
            _title.text = selected.VersionLabel;
            _date.text = FormatDate(selected.Timestamp);
            _body.text = BuildNotes(selected.GetNotes(Loc.Code));
        }

        /// <summary>Notas em seções (novidades, ajustes, removido, bugs conhecidos), uma linha por item; seção vazia some.</summary>
        static string BuildNotes(BuildAssistantChangelogNotes notes)
        {
            var text = new StringBuilder();
            AppendSection(text, "version.added", notes.Added);
            AppendSection(text, "version.modified", notes.Modified);
            AppendSection(text, "version.removed", notes.Removed);
            AppendSection(text, "version.bugs", notes.KnownBugs);
            return text.Length > 0 ? text.ToString().TrimEnd() : Loc.T("version.nonotes");
        }

        static void AppendSection(StringBuilder text, string headingKey, string notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return;
            text.Append("<color=").Append(HeadingColor).Append('>').Append(Loc.T(headingKey)).Append("</color>\n");
            foreach (string line in notes.Split('\n'))
            {
                string item = line.Trim();
                if (item.Length > 0)
                    text.Append("- ").Append(item).Append('\n');
            }
            text.Append('\n');
        }

        static string FormatDate(string timestamp) =>
            System.DateTime.TryParseExact(timestamp, InputTimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
                                          out System.DateTime date)
                ? date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                : "";
    }
}
