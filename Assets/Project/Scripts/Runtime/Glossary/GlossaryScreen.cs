using System.Collections.Generic;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Texts;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.Glossary
{
    /// <summary>
    /// Janela do glossário: à esquerda a lista de entradas agrupadas por categoria (<see cref="GlossaryContent"/>), à direita o
    /// título, a imagem (quando a entrada tem) e o texto da selecionada. Abre pelo botão da barra inferior e pelo menu do ESC; o fundo escurecido barra os
    /// cliques no mundo, e enquanto aberta o jogo fica pausado e com o input travado (devolvidos como estavam ao fechar). O ESC
    /// que a fecha é tratado pelo <c>AntFarmHud</c>. Montada por <c>Incrementant/Setup/Montar HUD da partida</c>.
    /// </summary>
    public sealed class GlossaryScreen : MonoBehaviour
    {
        [SerializeField] AntFarmGame _game;
        [SerializeField] RectTransform _listContent;
        [SerializeField] ScrollRect _listScroll;
        [SerializeField] Text _headingTemplate;
        [SerializeField] Button _itemTemplate;
        [SerializeField] Sprite _itemNormal;
        [SerializeField] Sprite _itemSelected;
        [SerializeField] ScrollRect _detailScroll;
        [SerializeField] Text _title;
        [SerializeField] Text _category;
        [SerializeField] Text _body;
        [SerializeField] Button _close;
        [Tooltip("Quadro da imagem da entrada, entre a categoria e o texto; some na entrada sem imagem. Opcional.")]
        [SerializeField] Image _picture;
        [Tooltip("Imagens das entradas: o nome de cada sprite é o id da entrada (Art/UI/Glossary, ligadas pelo setup do HUD).")]
        [SerializeField] Sprite[] _images;

        readonly List<Text> _headings = new List<Text>();
        readonly List<Button> _items = new List<Button>();
        readonly List<string> _entries = new List<string>();
        readonly List<string> _entryCategories = new List<string>();
        readonly Dictionary<string, Sprite> _imageByEntry = new Dictionary<string, Sprite>();
        int _selected;
        bool _wasPaused, _wasLocked;
        bool _valid;

        public bool IsOpen => gameObject.activeSelf;

        void Awake()
        {
            _valid = _game != null && _listContent != null && _headingTemplate != null && _itemTemplate != null && _title != null &&
                     _category != null && _body != null && _close != null;
            if (!_valid)
            {
                Debug.LogError($"{name}: GlossaryScreen com referências faltando (rode Incrementant/Setup/Montar HUD da partida).", this);
                return;
            }
            _headingTemplate.gameObject.SetActive(false);
            _itemTemplate.gameObject.SetActive(false);
            if (_images != null)
            {
                foreach (Sprite image in _images)
                {
                    if (image != null)
                        _imageByEntry[image.name] = image;
                }
            }
            BuildList();
        }

        void OnEnable()
        {
            if (!_valid)
                return;
            _close.onClick.AddListener(Close);
            Loc.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (!_valid)
                return;
            _close.onClick.RemoveListener(Close);
            Loc.LanguageChanged -= Refresh;
        }

        /// <summary>Abre na última entrada vista (a primeira, na primeira vez).</summary>
        public void Open()
        {
            if (IsOpen)
                return;
            // A janela começa desligada: o Awake (validação e lista) só roda ao ligar.
            gameObject.SetActive(true);
            if (!_valid)
            {
                gameObject.SetActive(false);
                return;
            }
            _wasPaused = _game.Paused;
            _wasLocked = _game.InputLocked;
            _game.SetPaused(true);
            _game.InputLocked = true;
            Select(_selected);
        }

        public void Close()
        {
            if (!IsOpen || !_valid)
                return;
            gameObject.SetActive(false);
            _game.InputLocked = _wasLocked;
            _game.SetPaused(_wasPaused);
        }

        void BuildList()
        {
            foreach (GlossaryContent.Category category in GlossaryContent.Categories)
            {
                Text heading = Instantiate(_headingTemplate, _listContent);
                heading.name = "Categoria_" + category.Id;
                heading.gameObject.SetActive(true);
                _headings.Add(heading);
                foreach (string entry in category.Entries)
                {
                    Button item = Instantiate(_itemTemplate, _listContent);
                    item.name = "Entrada_" + entry;
                    item.gameObject.SetActive(true);
                    int index = _items.Count;
                    item.onClick.AddListener(() => Select(index));
                    _items.Add(item);
                    _entries.Add(entry);
                    _entryCategories.Add(category.Id);
                }
            }
        }

        void Select(int index)
        {
            _selected = Mathf.Clamp(index, 0, _entries.Count - 1);
            Refresh();
            if (_detailScroll != null)
                _detailScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>Reescreve os rótulos da lista e o detalhe (também quando o idioma muda).</summary>
        void Refresh()
        {
            if (_entries.Count == 0)
                return;
            for (int i = 0; i < _headings.Count; i++)
                _headings[i].text = Loc.T(GlossaryContent.CategoryKey(GlossaryContent.Categories[i].Id)).ToUpperInvariant();
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].GetComponentInChildren<Text>().text = Loc.T(GlossaryContent.TitleKey(_entries[i]));
                ((Image)_items[i].targetGraphic).sprite = i == _selected ? _itemSelected : _itemNormal;
            }
            _title.text = Loc.T(GlossaryContent.TitleKey(_entries[_selected]));
            _category.text = Loc.T(GlossaryContent.CategoryKey(_entryCategories[_selected]));
            _body.text = Loc.T(GlossaryContent.TextKey(_entries[_selected]));
            if (_picture != null)
            {
                _imageByEntry.TryGetValue(_entries[_selected], out Sprite image);
                _picture.sprite = image;
                _picture.gameObject.SetActive(image != null);
            }
        }
    }
}
