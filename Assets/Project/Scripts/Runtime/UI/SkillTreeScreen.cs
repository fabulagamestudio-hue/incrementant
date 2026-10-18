using System;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
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
        string _message = "";
        UnityAction[] _queenHandlers;
        UnityAction[] _hiveHandlers;

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
                _queenHandlers[i] = () => { _selectedQueen = node; _message = ""; };
            }
            _hiveHandlers = new UnityAction[_hiveNodes.Length];
            for (int i = 0; i < _hiveNodes.Length; i++)
            {
                int index = i;
                _hiveHandlers[i] = () => { _selectedHive = index; _message = ""; };
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
            _message = BlockText(colony.TryBuy(_selectedQueen));
        }

        static string BlockText(BuyBlock block)
        {
            switch (block)
            {
                case BuyBlock.Owned: return "JÁ COMPRADO";
                case BuyBlock.MissingRequirement: return "FALTA UM PRÉ-REQUISITO";
                case BuyBlock.NotAvailableYet: return "EM BREVE: O JOGO AINDA NÃO TEM ESSE SISTEMA";
                case BuyBlock.InsufficientNutrients: return "NUTRIENTES INSUFICIENTES";
                default: return "COMPRADO";
            }
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }
            AntColony colony = _game.Colony;
            if (colony == null)
                return;
            _hivePage.SetActive(!_queenTabOpen);
            _queenPage.SetActive(_queenTabOpen);
            _hiveTabImage.sprite = _queenTabOpen ? _tabInactive : _tabActive;
            _queenTabImage.sprite = _queenTabOpen ? _tabActive : _tabInactive;
            _nutrients.text = $"{colony.Nutrients} N";
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
            _title.text = $"{def.Id} · {def.Name.ToUpperInvariant()}";
            _effect.text = def.Effect.ToUpperInvariant();
            string requires = def.Requires.Count == 0 ? "NENHUM" : string.Join(" E ", def.Requires);
            _info.text = _message.Length > 0 ? _message : $"REQUISITO: {requires}";
            bool owned = colony.Tree.Owns(_selectedQueen);
            _buyLabel.text = owned ? "ADQUIRIDO" : !def.Implemented ? "EM BREVE" : $"DESBLOQUEAR · {def.Cost} N";
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
            _title.text = $"{node.Id} · {node.Name.ToUpperInvariant()}";
            _effect.text = node.Effect.ToUpperInvariant();
            _info.text = $"REQUISITO: {node.Requires}  ·  CUSTO: {node.Cost} N";
            _buyLabel.text = "EM BREVE";
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
