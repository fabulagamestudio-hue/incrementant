using Fabula.Incrementant.Texts;
using Fabula.Incrementant.Audio;
using Fabula.Incrementant.Juice;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Upgrades;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Tela Evolução (documento 39 do design): duas abas, Formigueiro e Rainha e classes, cada uma um formigueiro radial
    /// arrastável (<see cref="TreeViewport"/>) com câmaras, galerias com nível, anéis de profundidade e túneis de
    /// dependência; os requisitos da outra aba aparecem como portais que levam até o nó. A compra de verdade só existe
    /// nos nós da árvore da rainha que já têm sistema (<see cref="AntColony.TryBuy"/>); o resto mostra EM BREVE.
    /// Cobre a tela inteira; ESC ou VOLTAR a fecham. A hierarquia é montada pelo Editor
    /// (<c>Incrementant/Setup/Montar HUD da partida</c>), que liga as referências abaixo. Não pausa o jogo (regra em
    /// aberto no design, seção 34).
    /// </summary>
    public sealed class SkillTreeScreen : MonoBehaviour
    {
        /// <summary>
        /// Ligação de dependência entre dois nós da aba, desenhada pelos túneis do formigueiro: índices em <see cref="NodesOf"/>;
        /// <see cref="Parent"/> -1 = a câmara central.
        /// </summary>
        [Serializable]
        public struct Link
        {
            public TunnelPathGraphic Path;
            public int Parent;
            public int Child;
        }

        /// <summary>Abertura ao lado de um nó para um requisito da outra aba; clicar leva até ele.</summary>
        [Serializable]
        public struct Portal
        {
            public Button Button;
            public string Target;
        }

        /// <summary>Peças de uma aba (montadas pelo setup na ordem de <see cref="NodesOf"/>).</summary>
        [Serializable]
        public sealed class TabView
        {
            public Button Tab;
            public Image TabImage;
            public GameObject Page;
            public TreeViewport Viewport;
            public SkillNodeWidget Root;
            public SkillNodeWidget[] Nodes;
            public Link[] Links;
            public Portal[] Portals;
            /// <summary>Terra escura dos anéis 1 a 4 (índice 0 = A1) e o aviso de profundidade de cada um.</summary>
            public RingGraphic[] Rings;
            public Text[] RingLabels;
            /// <summary>Nó cujas ligações estão na frente (destacadas); -2 = nenhum ainda.</summary>
            [NonSerialized] public int Highlighted = -2;
        }

        [SerializeField] AntFarmGame _game;

        [Header("Abas")]
        [SerializeField] TabView _hive = new TabView();
        [SerializeField] TabView _queen = new TabView();
        [SerializeField] Sprite _tabActive;
        [SerializeField] Sprite _tabInactive;

        [Header("Cabeçalho")]
        [SerializeField] Text _nutrients;
        [SerializeField] Text _depth;
        [SerializeField] Button _backButton;

        [Header("Sprites de estado")]
        [SerializeField] Sprite _diamondAcquired;
        [SerializeField] Sprite _diamondAvailable;
        [SerializeField] Sprite _diamondLocked;
        [SerializeField] Sprite _diamondRoot;
        [Header("Ligações (cores do miolo do traço)")]
        [SerializeField] Color _linkAcquired = new Color32(0x8a, 0xab, 0x56, 0xff);
        [SerializeField] Color _linkAvailable = new Color32(0xc1, 0xa1, 0x60, 0xff);
        [SerializeField] Color _linkLocked = new Color32(0x65, 0x51, 0x3f, 0xff);
        [Tooltip("Nó selecionado: requisito que o jogador já tem, que ainda falta e o que o nó libera. Pulsam.")]
        [SerializeField] Color _linkMet = new Color32(0x6c, 0xe0, 0x5a, 0xff);
        [SerializeField] Color _linkMissing = new Color32(0xf0, 0x4e, 0x3e, 0xff);
        [SerializeField] Color _linkUnlocks = new Color32(0x5a, 0xb4, 0xff, 0xff);
        [SerializeField, Min(0f)] float _linkPulseSpeed = 6f;
        [SerializeField, Min(0f)] float _linkCore = 2f;
        [SerializeField, Min(0f)] float _linkHighlightCore = 4f;
        [Tooltip("Amostras da legenda do rodapé, na ordem: tem o requisito, falta o requisito, o que libera.")]
        [SerializeField] TunnelPathGraphic[] _legendLines = Array.Empty<TunnelPathGraphic>();
        [SerializeField] Color _closedRing = new Color(0.08f, 0.05f, 0.03f, 0.72f);

        [Header("Detalhe")]
        [SerializeField] Text _title;
        [SerializeField] Text _effect;
        [SerializeField] Text _info;
        [SerializeField] Button _buyButton;
        [SerializeField] Image _buyImage;
        [SerializeField] Text _buyLabel;
        [Tooltip("Seletor ×1 / ×10 / MÁX das galerias (na ordem).")]
        [SerializeField] GameObject _amountRoot;
        [SerializeField] Button[] _amountButtons;
        [SerializeField] Image[] _amountImages;

        static readonly Color BuyEnabled = Color.white;
        static readonly Color BuyDisabled = new Color(0.45f, 0.45f, 0.45f);
        static readonly int[] Amounts = { 1, 10, 0 }; // 0 = MÁX
        const string MetColor = "#8CC759", MissingColor = "#D9775A";

        readonly UpgradeBoard _board = new UpgradeBoard();
        List<UpgradeDef> _hiveDefs;
        List<UpgradeDef> _queenDefs;
        Dictionary<string, Vector2> _hivePositions;
        Dictionary<string, Vector2> _queenPositions;
        static HashSet<string> _legacyHive;

        UpgradeTab _tab = UpgradeTab.Queen;
        int _selectedHive;
        int _selectedQueen;
        int _amount;
        // Chave do aviso da última compra recusada (traduzida a cada quadro); vazio = sem aviso.
        string _message = "";
        UnityAction[] _hiveHandlers, _queenHandlers, _hivePortalHandlers, _queenPortalHandlers, _amountHandlers;
        readonly StringBuilder _text = new StringBuilder(256);

        /// <summary>Quantas vezes o jogador clicou num nó (o tutorial espera a primeira consulta).</summary>
        public int NodeSelections { get; private set; }

        public RectTransform HiveTabRect => (RectTransform)_hive.Tab.transform;
        public RectTransform QueenTabRect => (RectTransform)_queen.Tab.transform;
        /// <summary>Painel com nome, efeito, requisito e botão de compra do nó escolhido.</summary>
        public RectTransform DetailRect => (RectTransform)_title.transform.parent;

        /// <summary>Nós desenhados na aba (todos menos os da câmara central), na ordem dos widgets.</summary>
        public static List<UpgradeDef> NodesOf(UpgradeTab tab)
        {
            List<UpgradeDef> list = UpgradeCatalog.OfTab(tab);
            list.RemoveAll(d => d.IsCenter);
            return list;
        }

        void Awake()
        {
            _hiveDefs = NodesOf(UpgradeTab.Hive);
            _queenDefs = NodesOf(UpgradeTab.Queen);
            if (_game == null || !Valid(_hive, _hiveDefs.Count) || !Valid(_queen, _queenDefs.Count) || _buyButton == null ||
                _backButton == null || _title == null || _amountButtons == null || _amountButtons.Length != Amounts.Length)
            {
                Debug.LogError($"{name}: SkillTreeScreen com referências faltando (rode Incrementant/Setup/Montar HUD da partida).", this);
                enabled = false;
                return;
            }
            _hivePositions = UpgradeLayout.Positions(UpgradeTab.Hive);
            _queenPositions = UpgradeLayout.Positions(UpgradeTab.Queen);
            _selectedQueen = Math.Max(0, _queenDefs.FindIndex(d => d.Id == "Q1"));
            _selectedHive = Math.Max(0, _hiveDefs.FindIndex(d => d.Id == "C1"));
            _hiveHandlers = NodeHandlers(UpgradeTab.Hive, _hiveDefs.Count);
            _queenHandlers = NodeHandlers(UpgradeTab.Queen, _queenDefs.Count);
            _hivePortalHandlers = PortalHandlers(_hive);
            _queenPortalHandlers = PortalHandlers(_queen);
            _amountHandlers = new UnityAction[Amounts.Length];
            for (int i = 0; i < Amounts.Length; i++)
            {
                int index = i;
                _amountHandlers[i] = () => _amount = index;
            }
        }

        static bool Valid(TabView view, int count) =>
            view != null && view.Tab != null && view.Page != null && view.Viewport != null && view.Nodes != null &&
            view.Nodes.Length == count && view.Rings != null && view.Rings.Length == 4 && view.RingLabels != null &&
            view.RingLabels.Length == 4;

        UnityAction[] NodeHandlers(UpgradeTab tab, int count)
        {
            var handlers = new UnityAction[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                handlers[i] = () => Select(tab, index, center: false);
            }
            return handlers;
        }

        UnityAction[] PortalHandlers(TabView view)
        {
            var handlers = new UnityAction[view.Portals?.Length ?? 0];
            for (int i = 0; i < handlers.Length; i++)
            {
                string target = view.Portals[i].Target;
                handlers[i] = () => GoTo(target);
            }
            return handlers;
        }

        void OnEnable()
        {
            if (!enabled)
                return;
            _hive.Tab.onClick.AddListener(OpenHive);
            _queen.Tab.onClick.AddListener(OpenQueen);
            _backButton.onClick.AddListener(Close);
            _buyButton.onClick.AddListener(Buy);
            Hook(_hive, _hiveHandlers, _hivePortalHandlers, true);
            Hook(_queen, _queenHandlers, _queenPortalHandlers, true);
            for (int i = 0; i < _amountButtons.Length; i++)
                _amountButtons[i].onClick.AddListener(_amountHandlers[i]);
            // Toda abertura começa enquadrando a câmara central.
            _hive.Viewport.ResetView();
            _queen.Viewport.ResetView();
        }

        void OnDisable()
        {
            if (_hiveHandlers == null)
                return;
            _hive.Tab.onClick.RemoveListener(OpenHive);
            _queen.Tab.onClick.RemoveListener(OpenQueen);
            _backButton.onClick.RemoveListener(Close);
            _buyButton.onClick.RemoveListener(Buy);
            Hook(_hive, _hiveHandlers, _hivePortalHandlers, false);
            Hook(_queen, _queenHandlers, _queenPortalHandlers, false);
            for (int i = 0; i < _amountButtons.Length; i++)
                _amountButtons[i].onClick.RemoveListener(_amountHandlers[i]);
        }

        static void Hook(TabView view, UnityAction[] nodes, UnityAction[] portals, bool add)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (add)
                    view.Nodes[i].Button.onClick.AddListener(nodes[i]);
                else
                    view.Nodes[i].Button.onClick.RemoveListener(nodes[i]);
            }
            for (int i = 0; i < portals.Length; i++)
            {
                if (add)
                    view.Portals[i].Button.onClick.AddListener(portals[i]);
                else
                    view.Portals[i].Button.onClick.RemoveListener(portals[i]);
            }
        }

        void OpenHive() { _tab = UpgradeTab.Hive; _message = ""; }

        void OpenQueen() { _tab = UpgradeTab.Queen; _message = ""; }

        void Close() => gameObject.SetActive(false);

        void Select(UpgradeTab tab, int index, bool center)
        {
            _tab = tab;
            if (tab == UpgradeTab.Hive)
                _selectedHive = index;
            else
                _selectedQueen = index;
            _message = "";
            NodeSelections++;
            if (center)
            {
                UpgradeDef def = Selected;
                Positions(tab).TryGetValue(def.Id, out Vector2 position);
                View(tab).Viewport.CenterOn(position);
            }
        }

        /// <summary>Portal: troca de aba e centraliza no nó exigido.</summary>
        void GoTo(string id)
        {
            UpgradeDef target = UpgradeCatalog.Find(id);
            if (target == null || target.IsCenter)
                return;
            int index = Defs(target.Tab).IndexOf(target);
            if (index >= 0)
                Select(target.Tab, index, center: true);
        }

        TabView View(UpgradeTab tab) => tab == UpgradeTab.Hive ? _hive : _queen;
        List<UpgradeDef> Defs(UpgradeTab tab) => tab == UpgradeTab.Hive ? _hiveDefs : _queenDefs;
        Dictionary<string, Vector2> Positions(UpgradeTab tab) => tab == UpgradeTab.Hive ? _hivePositions : _queenPositions;
        UpgradeDef Selected => _tab == UpgradeTab.Hive ? _hiveDefs[_selectedHive] : _queenDefs[_selectedQueen];

        void Buy()
        {
            AntColony colony = _game.Colony;
            if (colony == null)
                return;
            UpgradeDef def = Selected;
            BuyBlock block;
            if (UpgradeBoard.HasSystem(def) && def.QueenNode.HasValue)
                block = colony.TryBuy(def.QueenNode.Value);
            else if (UpgradeBoard.HasSystem(def))
                block = colony.TryBuyUpgrade(def, Amounts[_amount], out _);
            else
                block = _board.Owns(def) && def.Kind == UpgradeKind.Chamber ? BuyBlock.Owned : BuyBlock.NotAvailableYet;
            _message = BlockKey(block);
            if (block != BuyBlock.None)
            {
                GameAudio.Play(SfxId.UiDeny);
                ButtonJuice.Deny(_buyButton.gameObject);
            }
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
            _board.Refresh(colony.Tree, colony.RaidsSurvived, colony.Ants.Count, colony.Galleries);
            bool hive = _tab == UpgradeTab.Hive;
            _hive.Page.SetActive(hive);
            _queen.Page.SetActive(!hive);
            _hive.TabImage.sprite = hive ? _tabActive : _tabInactive;
            _queen.TabImage.sprite = hive ? _tabInactive : _tabActive;
            _nutrients.text = Loc.T("hud.nutrients.value", colony.Nutrients);
            if (_depth != null)
                _depth.text = Loc.T("tree.depth", _board.Depth);
            RefreshTab(hive ? _hive : _queen, Defs(_tab), hive ? _selectedHive : _selectedQueen);
            RefreshDetail(colony, Selected);
        }

        // ---------------------------------------------------------------- árvore

        void RefreshTab(TabView view, List<UpgradeDef> defs, int selected)
        {
            view.Root?.Apply(SkillNodeState.Acquired, false, _diamondAcquired, _diamondAvailable, _diamondLocked, _diamondRoot);
            for (int i = 0; i < defs.Count; i++)
            {
                UpgradeDef def = defs[i];
                SkillNodeWidget widget = view.Nodes[i];
                widget.Apply(StateOf(def), i == selected, _diamondAcquired, _diamondAvailable, _diamondLocked, _diamondRoot);
                widget.SetBuried(!_board.RingOpen(def.Ring) && !UpgradeBoard.IgnoresRing(def));
                if (def.Kind == UpgradeKind.Gallery)
                    widget.SetLevel(LevelText(def));
            }
            RefreshLinks(view, defs, selected);
            RefreshLegend();
            for (int ring = 0; ring < view.Rings.Length; ring++)
            {
                bool closed = !_board.RingOpen(ring + 1);
                view.Rings[ring].enabled = closed;
                view.Rings[ring].color = _closedRing;
                view.RingLabels[ring].text = closed ? Loc.T("tree.ring.closed", UpgradeCatalog.DepthToOpen(ring + 1)) : "";
            }
        }

        /// <summary>
        /// Cor de cada ligação pelo estado; as do nó selecionado pulsam: verde no requisito que o jogador já tem, vermelho no
        /// que falta e azul no que o nó libera. As destacadas vão para a frente das outras (o mesmo túnel leva várias linhas).
        /// </summary>
        void RefreshLinks(TabView view, List<UpgradeDef> defs, int selected)
        {
            bool reorder = view.Highlighted != selected;
            view.Highlighted = selected;
            foreach (Link link in view.Links)
            {
                if (link.Path == null)
                    continue;
                UpgradeDef child = defs[link.Child];
                bool parentOk = link.Parent < 0 || ParentMet(defs[link.Parent], child);
                Color color;
                bool highlight = true;
                if (link.Child == selected)
                    color = parentOk ? _linkMet : _linkMissing;
                else if (link.Parent == selected && selected >= 0)
                    color = _linkUnlocks;
                else
                {
                    highlight = false;
                    color = parentOk && _board.Owns(child) ? _linkAcquired
                          : parentOk && UpgradeBoard.HasSystem(child) ? _linkAvailable
                          : _linkLocked;
                }
                color = highlight ? Pulse(color) : color;
                color.a = 1f;
                link.Path.color = color;
                link.Path.Core = highlight ? _linkHighlightCore : _linkCore;
                if (reorder && highlight)
                    link.Path.transform.SetAsLastSibling();
            }
        }

        /// <summary>Cor pulsante de uma linha destacada (a mesma nas linhas e na legenda).</summary>
        Color Pulse(Color color)
        {
            float pulse = Vfx.Motion ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * _linkPulseSpeed) : 1f;
            Color result = Color.Lerp(color * 0.75f, Color.Lerp(color, Color.white, 0.35f), pulse);
            result.a = 1f;
            return result;
        }

        void RefreshLegend()
        {
            Color[] colors = { _linkMet, _linkMissing, _linkUnlocks };
            for (int i = 0; i < _legendLines.Length && i < colors.Length; i++)
            {
                if (_legendLines[i] == null)
                    continue;
                _legendLines[i].color = Pulse(colors[i]);
                _legendLines[i].Core = _linkHighlightCore;
            }
        }

        bool ParentMet(UpgradeDef parent, UpgradeDef child)
        {
            foreach (UpgradeRequirement r in child.Requires)
                if (r.Id == parent.Id)
                    return _board.Meets(r);
            return _board.Owns(parent);
        }

        SkillNodeState StateOf(UpgradeDef def)
        {
            switch (_board.Status(def))
            {
                case UpgradeStatus.Owned: return SkillNodeState.Acquired;
                case UpgradeStatus.Available: return SkillNodeState.Available;
                default: return SkillNodeState.Locked;
            }
        }

        string LevelText(UpgradeDef def)
        {
            int level = _board.Level(def);
            return def.Capped ? Loc.T("tree.level", level, def.Cap) : Loc.T("tree.level.open", level);
        }

        // ---------------------------------------------------------------- detalhe

        void RefreshDetail(AntColony colony, UpgradeDef def)
        {
            string kind = Loc.T(def.Kind == UpgradeKind.Gallery ? "tree.kind.gallery" : "tree.kind.chamber");
            _title.text = $"{NameOf(def).ToUpperInvariant()} · {kind}";
            _effect.text = EffectOf(def).ToUpperInvariant();

            _text.Clear();
            if (def.Kind == UpgradeKind.Gallery)
                AppendGalleryLines(colony, def);
            AppendRequirements(def);
            if (def.Kind == UpgradeKind.Chamber && !def.Initial)
                _text.Append('\n').Append(Loc.T("tree.cost", def.Cost));
            if (_message.Length > 0)
                _text.Append('\n').Append(Loc.T(_message));
            _info.text = _text.ToString();

            bool gallery = def.Kind == UpgradeKind.Gallery;
            _amountRoot.SetActive(gallery);
            if (gallery)
                for (int i = 0; i < _amountImages.Length; i++)
                    _amountImages[i].color = i == _amount ? BuyEnabled : BuyDisabled;

            bool owned = def.Kind == UpgradeKind.Chamber && _board.Owns(def);
            if (gallery && UpgradeBoard.HasSystem(def))
            {
                int level = _board.Level(def);
                int bought;
                long cost = Amounts[_amount] == 0
                    ? UpgradeCatalog.MaxAffordable(def, level, colony.Nutrients, out bought)
                    : UpgradeCatalog.CostOfLevels(def, level, Amounts[_amount], out bought);
                bool full = !_board.CanLevel(def);
                _buyLabel.text = full ? Loc.T("tree.acquired") : Loc.T("tree.unlock.cost", cost);
                _buyImage.color = !full && bought > 0 && _board.Unlockable(def) && colony.Nutrients >= cost ? BuyEnabled : BuyDisabled;
            }
            else if (UpgradeBoard.HasSystem(def) && !def.QueenNode.HasValue)
            {
                _buyLabel.text = owned ? Loc.T("tree.acquired") : Loc.T("tree.unlock.cost", def.Cost);
                _buyImage.color = !owned && _board.Unlockable(def) && colony.Nutrients >= def.Cost ? BuyEnabled : BuyDisabled;
            }
            else if (UpgradeBoard.HasSystem(def))
            {
                _buyLabel.text = owned ? Loc.T("tree.acquired") : Loc.T("tree.unlock.cost", def.Cost);
                _buyImage.color = !owned && colony.Tree.Check(def.QueenNode.Value) == BuyBlock.None && colony.Nutrients >= def.Cost
                    ? BuyEnabled : BuyDisabled;
            }
            else
            {
                _buyLabel.text = owned ? Loc.T("tree.acquired") : Loc.T("tree.soon");
                _buyImage.color = BuyDisabled;
            }
        }

        void AppendGalleryLines(AntColony colony, UpgradeDef def)
        {
            int level = _board.Level(def);
            bool full = def.Capped && level >= def.Cap;
            _text.Append(LevelText(def)).Append("  ·  ");
            _text.Append(full ? Loc.T("tree.value.full", Total(def, level))
                              : Loc.T("tree.value", Total(def, level), Total(def, level + 1)));
            int milestone = UpgradeCatalog.NextMilestone(def, level);
            if (milestone > 0)
                _text.Append("  ·  ").Append(Loc.T("tree.milestone", milestone));
            if (full)
                return;
            int bought;
            long cost = Amounts[_amount] == 0
                ? UpgradeCatalog.MaxAffordable(def, level, colony.Nutrients, out bought)
                : UpgradeCatalog.CostOfLevels(def, level, Amounts[_amount], out bought);
            _text.Append('\n').Append(Loc.T("tree.cost.levels", bought, cost));
        }

        void AppendRequirements(UpgradeDef def)
        {
            if (_text.Length > 0)
                _text.Append('\n');
            if (def.Requires.Count == 0 && def.Condition == UpgradeCondition.None)
            {
                _text.Append(Loc.T("tree.requires", Loc.T("tree.none")));
                return;
            }
            var parts = new StringBuilder();
            foreach (UpgradeRequirement r in def.Requires)
            {
                UpgradeDef target = UpgradeCatalog.Get(r.Id);
                if (parts.Length > 0)
                    parts.Append(", ");
                parts.Append("<color=").Append(_board.Meets(r) ? MetColor : MissingColor).Append('>').Append(NameOf(target));
                if (r.Level > 0)
                    parts.Append(' ').Append(Loc.T("tree.level.open", r.Level));
                if (target.Tab != def.Tab)
                    parts.Append(" (").Append(Loc.T(target.Tab == UpgradeTab.Hive ? "tree.tab.hive" : "tree.tab.queen")).Append(')');
                parts.Append("</color>");
            }
            if (def.Condition != UpgradeCondition.None)
            {
                if (parts.Length > 0)
                    parts.Append(", ");
                parts.Append("<color=").Append(_board.ConditionMet(def.Condition) ? MetColor : MissingColor).Append('>')
                     .Append(Loc.T(ConditionKey(def.Condition))).Append("</color>");
            }
            if (!_board.RingOpen(def.Ring) && !UpgradeBoard.IgnoresRing(def))
                parts.Append(", <color=").Append(MissingColor).Append('>')
                     .Append(Loc.T("tree.depth", UpgradeCatalog.DepthToOpen(def.Ring))).Append("</color>");
            _text.Append(Loc.T("tree.requires", parts.ToString()));
        }

        static string ConditionKey(UpgradeCondition condition)
        {
            switch (condition)
            {
                case UpgradeCondition.FirstRaid: return "tree.firstraid";
                case UpgradeCondition.FiveRaids: return "tree.fiveraids";
                default: return "tree.fiftyadults";
            }
        }

        /// <summary>
        /// Bônus acumulado da galeria no nível, no mesmo sentido do texto do efeito ("3% mais rápido" → 3%): o módulo do
        /// efeito por nível vezes o nível e o marco (modo %), ou a quantidade somada (modo absoluto, "+4").
        /// </summary>
        static string Total(UpgradeDef def, int level)
        {
            if (def.Capped)
                level = Math.Min(level, def.Cap);
            if (def.Additive)
                return "+" + (def.PerLevel * level).ToString("0.#", CultureInfo.InvariantCulture);
            float percent = Mathf.Abs(def.PerLevel) * level * UpgradeCatalog.MilestoneMultiplier(def, level) * 100f;
            return percent.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        // ---------------------------------------------------------------- textos

        /// <summary>Nome do nó: os da rainha e do formigueiro que já existiam mantêm as chaves antigas.</summary>
        public static string NameOf(UpgradeDef def)
        {
            if (def.QueenNode.HasValue)
                return Loc.QueenName(def.QueenNode.Value);
            return IsLegacyHive(def.Id) ? Loc.HiveName(def.Id) : Loc.UpgradeName(def.Id);
        }

        public static string EffectOf(UpgradeDef def)
        {
            if (def.QueenNode.HasValue)
                return Loc.QueenEffect(def.QueenNode.Value);
            return IsLegacyHive(def.Id) ? Loc.HiveEffect(def.Id) : Loc.UpgradeEffect(def.Id);
        }

        static bool IsLegacyHive(string id)
        {
            if (_legacyHive == null)
            {
                _legacyHive = new HashSet<string>();
                foreach (HiveTreeInfo.Node node in HiveTreeInfo.All)
                    _legacyHive.Add(node.Id);
            }
            return _legacyHive.Contains(id);
        }
    }
}
