using Fabula.Incrementant.Texts;
using Fabula.Incrementant.Audio;
using System;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Tela de evolução (referência <c>Refs de Telas/22-arvore-formigueiro-radial.png</c>): duas abas, a árvore do
    /// formigueiro (só exibição, os sistemas ainda não existem) e a da rainha e classes (compra de verdade por
    /// <see cref="AntColony.TryBuy"/>). Cobre a tela inteira; ESC ou VOLTAR a fecham. A hierarquia é montada pelo Editor
    /// (<c>Incrementant/Setup/Montar HUD da partida</c>), que liga as referências abaixo. Não pausa o jogo (regra em
    /// aberto no design, seção 34).
    /// </summary>
    public sealed class SkillTreeScreen : MonoBehaviour
    {
        /// <summary>Ligação entre dois nós: <see cref="Parent"/> -1 = o centro da árvore.</summary>
        [Serializable]
        public struct Link
        {
            public Image Image;
            public int Parent;
            public int Child;
        }

        [SerializeField] AntFarmGame _game;

        [Header("Abas")]
        [SerializeField] Button _hiveTab;
        [SerializeField] Image _hiveTabImage;
        [SerializeField] GameObject _hivePage;
        [SerializeField] Button _queenTab;
        [SerializeField] Image _queenTabImage;
        [SerializeField] GameObject _queenPage;
        [SerializeField] Sprite _tabActive;
        [SerializeField] Sprite _tabInactive;

        [Header("Cabeçalho")]
        [SerializeField] Text _nutrients;
        [SerializeField] Button _backButton;

        [Header("Nós e ligações (nós da rainha na ordem de QueenNode; nós do formigueiro na ordem de HiveTreeInfo)")]
        [SerializeField] SkillNodeWidget[] _queenNodes;
        [SerializeField] SkillNodeWidget[] _hiveNodes;
        [SerializeField] Link[] _queenLinks;
        [SerializeField] Link[] _hiveLinks;

        [Header("Sprites de estado")]
        [SerializeField] Sprite _diamondAcquired;
        [SerializeField] Sprite _diamondAvailable;
        [SerializeField] Sprite _diamondLocked;
        [SerializeField] Sprite _diamondRoot;
        [SerializeField] Sprite _linkAcquired;
        [SerializeField] Sprite _linkAvailable;
        [SerializeField] Sprite _linkLocked;

        [Header("Detalhe")]
        [SerializeField] Text _title;
        [SerializeField] Text _effect;
        [SerializeField] Text _info;
        [SerializeField] Button _buyButton;
        [SerializeField] Image _buyImage;
        [SerializeField] Text _buyLabel;

        static readonly Color BuyEnabled = Color.white;
        static readonly Color BuyDisabled = new Color(0.45f, 0.45f, 0.45f);

        bool _queenTabOpen = true;
        QueenNode _selectedQueen = QueenNode.Q1;
        int _selectedHive;
        // Chave do aviso da última compra recusada (traduzida a cada quadro); vazio = sem aviso.
        string _message = "";
        UnityAction[] _queenHandlers;
        UnityAction[] _hiveHandlers;

        /// <summary>Quantas vezes o jogador clicou num nó (o tutorial espera a primeira consulta).</summary>
        public int NodeSelections { get; private set; }

        public RectTransform HiveTabRect => (RectTransform)_hiveTab.transform;
        public RectTransform QueenTabRect => (RectTransform)_queenTab.transform;
        /// <summary>Painel com nome, efeito, requisito e botão de compra do nó escolhido.</summary>
        public RectTransform DetailRect => (RectTransform)_title.transform.parent;

        void Awake()
        {
            if (_game == null || _queenNodes == null || _queenNodes.Length != QueenTree.All.Count ||
                _hiveNodes == null || _hiveNodes.Length != HiveTreeInfo.All.Count || _buyButton == null ||
                _hiveTab == null || _queenTab == null || _backButton == null || _title == null)
            {
                Debug.LogError($"{name}: SkillTreeScreen com referências faltando (rode Incrementant/Setup/Montar HUD da partida).", this);
                enabled = false;
                return;
            }
            _queenHandlers = new UnityAction[_queenNodes.Length];
            for (int i = 0; i < _queenNodes.Length; i++)
            {
                var node = (QueenNode)i;
                _queenHandlers[i] = () => { _selectedQueen = node; _message = ""; NodeSelections++; };
            }
            _hiveHandlers = new UnityAction[_hiveNodes.Length];
            for (int i = 0; i < _hiveNodes.Length; i++)
            {
                int index = i;
                _hiveHandlers[i] = () => { _selectedHive = index; _message = ""; NodeSelections++; };
            }
        }

        void OnEnable()
        {
            if (!enabled)
                return;
            _hiveTab.onClick.AddListener(OpenHive);
            _queenTab.onClick.AddListener(OpenQueen);
            _backButton.onClick.AddListener(Close);
            _buyButton.onClick.AddListener(Buy);
            for (int i = 0; i < _queenNodes.Length; i++)
                _queenNodes[i].Button.onClick.AddListener(_queenHandlers[i]);
            for (int i = 0; i < _hiveNodes.Length; i++)
                _hiveNodes[i].Button.onClick.AddListener(_hiveHandlers[i]);
        }

        void OnDisable()
        {
            if (_hiveTab == null || _queenHandlers == null)
                return;
            _hiveTab.onClick.RemoveListener(OpenHive);
            _queenTab.onClick.RemoveListener(OpenQueen);
            _backButton.onClick.RemoveListener(Close);
            _buyButton.onClick.RemoveListener(Buy);
            for (int i = 0; i < _queenNodes.Length; i++)
                _queenNodes[i].Button.onClick.RemoveListener(_queenHandlers[i]);
            for (int i = 0; i < _hiveNodes.Length; i++)
                _hiveNodes[i].Button.onClick.RemoveListener(_hiveHandlers[i]);
        }

        void OpenHive() { _queenTabOpen = false; _message = ""; }

        void OpenQueen() { _queenTabOpen = true; _message = ""; }

        void Close() => gameObject.SetActive(false);

        void Buy()
        {
            AntColony colony = _game.Colony;
            if (!_queenTabOpen || colony == null)
                return;
            BuyBlock block = colony.TryBuy(_selectedQueen);
            _message = BlockKey(block);
            if (block != BuyBlock.None)
                GameAudio.Play(SfxId.UiDeny);
        }

        static string BlockKey(BuyBlock block)
        {
            switch (block)
            {
                case BuyBlock.Owned: return "buy.owned";
                case BuyBlock.MissingRequirement: return "buy.missing";
                case BuyBlock.NotAvailableYet: return "buy.soon";
                case BuyBlock.InsufficientNutrients: return "buy.insufficient";
                case BuyBlock.NeedsFirstRaid: return "buy.raid";
                default: return "buy.done";
            }
        }

        void Update()
        {
            // O ESC que fecha esta tela é tratado pelo AntFarmHud (uma camada por ESC).
            AntColony colony = _game.Colony;
            if (colony == null)
                return;
            _hivePage.SetActive(!_queenTabOpen);
            _queenPage.SetActive(_queenTabOpen);
            _hiveTabImage.sprite = _queenTabOpen ? _tabInactive : _tabActive;
            _queenTabImage.sprite = _queenTabOpen ? _tabActive : _tabInactive;
            _nutrients.text = Loc.T("hud.nutrients.value", colony.Nutrients);
            if (_queenTabOpen)
                RefreshQueen(colony);
            else
                RefreshHive();
        }

        // ---------------------------------------------------------------- rainha e classes

        static SkillNodeState StateOf(AntColony colony, QueenNode node)
        {
            if (colony.Tree.Owns(node))
                return SkillNodeState.Acquired;
            return colony.Tree.Check(node) == BuyBlock.None ? SkillNodeState.Available : SkillNodeState.Locked;
        }

        void RefreshQueen(AntColony colony)
        {
            for (int i = 0; i < _queenNodes.Length; i++)
            {
                var node = (QueenNode)i;
                _queenNodes[i].Apply(StateOf(colony, node), node == _selectedQueen, _diamondAcquired, _diamondAvailable,
                                     _diamondLocked, _diamondRoot);
            }
            foreach (Link link in _queenLinks)
            {
                var child = (QueenNode)link.Child;
                bool parentOwned = link.Parent < 0 || colony.Tree.Owns((QueenNode)link.Parent);
                link.Image.sprite = LinkSprite(parentOwned, colony.Tree.Owns(child), QueenTree.Def(child).Implemented);
            }

            QueenNodeDef def = QueenTree.Def(_selectedQueen);
            _title.text = $"{def.Id} · {Loc.QueenName(def.Node).ToUpperInvariant()}";
            _effect.text = Loc.QueenEffect(def.Node).ToUpperInvariant();
            string requires = def.Requires.Count == 0 ? Loc.T("tree.none") : string.Join(" " + Loc.T("tree.and") + " ", def.Requires);
            if (colony.Tree.Check(_selectedQueen) == BuyBlock.NeedsFirstRaid)
                requires = Loc.T("tree.firstraid");
            _info.text = _message.Length > 0 ? Loc.T(_message) : Loc.T("tree.requires", requires);
            bool owned = colony.Tree.Owns(_selectedQueen);
            _buyLabel.text = owned ? Loc.T("tree.acquired") : !def.Implemented ? Loc.T("tree.soon") : Loc.T("tree.unlock.cost", def.Cost);
            _buyImage.color = !owned && colony.Tree.Check(_selectedQueen) == BuyBlock.None && colony.Nutrients >= def.Cost
                ? BuyEnabled : BuyDisabled;
        }

        // ---------------------------------------------------------------- formigueiro (só exibição)

        void RefreshHive()
        {
            for (int i = 0; i < _hiveNodes.Length; i++)
                _hiveNodes[i].Apply(SkillNodeState.Locked, i == _selectedHive, _diamondAcquired, _diamondAvailable,
                                    _diamondLocked, _diamondRoot);
            foreach (Link link in _hiveLinks)
                link.Image.sprite = _linkLocked;

            HiveTreeInfo.Node node = HiveTreeInfo.All[_selectedHive];
            _title.text = $"{node.Id} · {Loc.HiveName(node.Id).ToUpperInvariant()}";
            _effect.text = Loc.HiveEffect(node.Id).ToUpperInvariant();
            // "Nenhum" e o " e " entre nós vêm do texto-fonte em português.
            string requires = node.Requires == "Nenhum" ? Loc.T("tree.none") : node.Requires.Replace(" e ", " " + Loc.T("tree.and") + " ");
            _info.text = Loc.T("tree.requires.cost", requires, node.Cost);
            _buyLabel.text = Loc.T("tree.soon");
            _buyImage.color = BuyDisabled;
        }

        Sprite LinkSprite(bool parentOwned, bool childOwned, bool childImplemented)
        {
            if (parentOwned && childOwned)
                return _linkAcquired;
            return parentOwned && childImplemented ? _linkAvailable : _linkLocked;
        }
    }
}
