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
    /// Tela Evolução (documento 46 do design): um formigueiro em corte arrastável (<see cref="TreeViewport"/>) que desce da
    /// Câmara real, com uma coluna por domínio, faixas por nível do formigueiro (as fechadas cobertas de terra) e câmaras e
    /// galerias (documento 39). Dentro da coluna as ligações aparecem sempre; as que cruzam colunas ou saem da raiz só com
    /// o nó selecionado. A compra vai para <see cref="AntColony.TryBuy"/> (nós da rainha com sistema) ou
    /// <see cref="AntColony.TryBuyUpgrade"/> (câmaras e galerias vivas); o resto mostra EM BREVE.
    /// Cobre a tela inteira; ESC ou VOLTAR a fecham. A hierarquia é montada pelo Editor
    /// (<c>Incrementant/Setup/Montar HUD da partida</c>), que liga as referências abaixo. Não pausa o jogo (regra em
    /// aberto no design, seção 34).
    /// </summary>
    public sealed class SkillTreeScreen : MonoBehaviour
    {
        /// <summary>
        /// Ligação de dependência desenhada pelos túneis do formigueiro: índices em <see cref="NodesOf"/>; <see cref="Parent"/>
        /// -1 = a Câmara real. <see cref="Cross"/>: liga colunas diferentes (ou a raiz) e só aparece com o nó selecionado.
        /// </summary>
        [Serializable]
        public struct Link
        {
            public TunnelPathGraphic Path;
            public int Parent;
            public int Child;
            public bool Cross;
        }

        /// <summary>Peças do formigueiro (montadas pelo setup na ordem de <see cref="NodesOf"/> e de <see cref="UpgradeCatalog.Domains"/>).</summary>
        [Serializable]
        public sealed class TreeView
        {
            public TreeViewport Viewport;
            public SkillNodeWidget Root;
            public SkillNodeWidget[] Nodes;
            public Link[] Links;
            /// <summary>Por nível do formigueiro (índice = nível): terra escura do nível fechado, rótulo e aviso de profundidade.</summary>
            public Image[] TierShades;
            public Text[] TierLabels;
            public Text[] TierWarnings;
            /// <summary>Nome e progresso do salão de cada domínio, na ordem das colunas.</summary>
            public Text[] HallLabels;
            /// <summary>Nó cujas ligações estão na frente (destacadas); -2 = nenhum ainda.</summary>
            [NonSerialized] public int Highlighted = -2;
        }

        [SerializeField] AntFarmGame _game;

        [Header("Formigueiro")]
        [SerializeField] TreeView _tree = new TreeView();

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
        [SerializeField] Color _closedTier = new Color(0.08f, 0.05f, 0.03f, 0.78f);

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
        List<UpgradeDef> _defs;
        UpgradeLayout _layout;
        // Nós de cada coluna (índices em _defs) e o progresso mostrado no salão, para só refazer o texto quando muda.
        List<int>[] _domainNodes;
        int[] _hallOwned;
        string _hallLanguage;
        static HashSet<string> _legacyHive;

        // Posição e altura do título, do efeito e do bloco de baixo como o setup os montou: o detalhe desce o que vem depois
        // quando o título ou o efeito ocupam mais linhas que a caixa (LayoutDetail).
        Vector2 _effectPos, _infoPos;
        float _titleHeight, _effectHeight;

        int _selected;
        int _amount;
        // Chave do aviso da última compra recusada (traduzida a cada quadro); vazio = sem aviso.
        string _message = "";
        UnityAction[] _nodeHandlers, _amountHandlers;
        readonly StringBuilder _text = new StringBuilder(256);

        /// <summary>Nós desenhados (todos menos os da Câmara real), na ordem da tabela, que é a ordem dos widgets.</summary>
        public static List<UpgradeDef> NodesOf()
        {
            var list = new List<UpgradeDef>();
            foreach (UpgradeDef def in UpgradeCatalog.All)
                if (def.Domain != UpgradeDomain.Root)
                    list.Add(def);
            return list;
        }

        void Awake()
        {
            _defs = NodesOf();
            if (_game == null || !Valid(_tree, _defs.Count) || _buyButton == null ||
                _backButton == null || _title == null || _amountButtons == null || _amountButtons.Length != Amounts.Length)
            {
                Debug.LogError($"{name}: SkillTreeScreen com referências faltando (rode Incrementant/Setup/Montar HUD da partida).", this);
                enabled = false;
                return;
            }
            _titleHeight = _title.rectTransform.sizeDelta.y;
            _effectHeight = _effect.rectTransform.sizeDelta.y;
            _effectPos = _effect.rectTransform.anchoredPosition;
            _infoPos = _info.rectTransform.anchoredPosition;
            _layout = new UpgradeLayout();
            _selected = Math.Max(0, _defs.FindIndex(d => d.Id == "Q1"));
            _domainNodes = new List<int>[UpgradeCatalog.Domains.Length];
            _hallOwned = new int[_domainNodes.Length];
            for (int c = 0; c < _domainNodes.Length; c++)
                _domainNodes[c] = new List<int>();
            for (int i = 0; i < _defs.Count; i++)
                _domainNodes[_layout.Column(_defs[i].Domain)].Add(i);
            _nodeHandlers = new UnityAction[_defs.Count];
            for (int i = 0; i < _defs.Count; i++)
            {
                int index = i;
                _nodeHandlers[i] = () => Select(index);
            }
            _amountHandlers = new UnityAction[Amounts.Length];
            for (int i = 0; i < Amounts.Length; i++)
            {
                int index = i;
                _amountHandlers[i] = () => _amount = index;
            }
        }

        static bool Valid(TreeView view, int count)
        {
            int tiers = UpgradeCatalog.TierCount;
            return view != null && view.Viewport != null && view.Nodes != null && view.Nodes.Length == count && view.Links != null &&
                   view.TierShades != null && view.TierShades.Length == tiers && view.TierLabels != null &&
                   view.TierLabels.Length == tiers && view.TierWarnings != null && view.TierWarnings.Length == tiers &&
                   view.HallLabels != null && view.HallLabels.Length == UpgradeCatalog.Domains.Length;
        }

        void OnEnable()
        {
            if (!enabled)
                return;
            _backButton.onClick.AddListener(Close);
            _buyButton.onClick.AddListener(Buy);
            for (int i = 0; i < _nodeHandlers.Length; i++)
                _tree.Nodes[i].Button.onClick.AddListener(_nodeHandlers[i]);
            for (int i = 0; i < _amountButtons.Length; i++)
                _amountButtons[i].onClick.AddListener(_amountHandlers[i]);
            // Toda abertura começa enquadrando a Câmara real, no topo.
            _tree.Viewport.ResetView();
        }

        void OnDisable()
        {
            if (_nodeHandlers == null)
                return;
            _backButton.onClick.RemoveListener(Close);
            _buyButton.onClick.RemoveListener(Buy);
            for (int i = 0; i < _nodeHandlers.Length; i++)
                _tree.Nodes[i].Button.onClick.RemoveListener(_nodeHandlers[i]);
            for (int i = 0; i < _amountButtons.Length; i++)
                _amountButtons[i].onClick.RemoveListener(_amountHandlers[i]);
        }

        void Close() => gameObject.SetActive(false);

        void Select(int index)
        {
            _selected = index;
            _message = "";
        }

        UpgradeDef Selected => _defs[_selected];

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
            _nutrients.text = Loc.T("hud.nutrients.value", colony.Nutrients);
            if (_depth != null)
                _depth.text = Loc.T("tree.depth", _board.Depth);
            RefreshTree(_tree, _defs, _selected);
            RefreshDetail(colony, Selected);
        }

        // ---------------------------------------------------------------- árvore

        void RefreshTree(TreeView view, List<UpgradeDef> defs, int selected)
        {
            view.Root?.Apply(SkillNodeState.Acquired, false, _diamondAcquired, _diamondAvailable, _diamondLocked, _diamondRoot);
            for (int i = 0; i < defs.Count; i++)
            {
                UpgradeDef def = defs[i];
                SkillNodeWidget widget = view.Nodes[i];
                widget.Apply(StateOf(def), i == selected, _diamondAcquired, _diamondAvailable, _diamondLocked, _diamondRoot);
                widget.SetBuried(!_board.TierOpen(def.Tier) && !UpgradeBoard.IgnoresTier(def));
                if (def.Kind == UpgradeKind.Gallery)
                    widget.SetLevel(LevelText(def));
            }
            RefreshLinks(view, defs, selected);
            RefreshLegend();
            for (int tier = 0; tier < view.TierShades.Length; tier++)
            {
                bool closed = !_board.TierOpen(tier);
                view.TierShades[tier].enabled = closed;
                view.TierShades[tier].color = _closedTier;
                view.TierWarnings[tier].text = closed ? Loc.T("tree.ring.closed", UpgradeCatalog.DepthToOpenTier(tier)) : "";
                view.TierLabels[tier].text = tier == 0 ? Loc.T("tree.root.hive") : Loc.T("tree.tier", tier);
            }
            RefreshHalls(view, defs);
        }

        /// <summary>Nome do domínio e quantos nós dele o jogador já tem (câmara comprada ou galeria com nível).</summary>
        void RefreshHalls(TreeView view, List<UpgradeDef> defs)
        {
            bool language = _hallLanguage != Loc.Code;
            _hallLanguage = Loc.Code;
            for (int c = 0; c < _domainNodes.Length; c++)
            {
                int owned = 0;
                foreach (int i in _domainNodes[c])
                    if (_board.Owns(defs[i]) || defs[i].Kind == UpgradeKind.Gallery && _board.Level(defs[i]) > 0)
                        owned++;
                if (owned == _hallOwned[c] && !language)
                    continue;
                _hallOwned[c] = owned;
                string name = Loc.DomainName(UpgradeCatalog.Domains[c].Domain);
                int total = _domainNodes[c].Count;
                view.HallLabels[c].text = total == 0 ? name : $"{name}  {owned}/{total}";
            }
        }

        /// <summary>
        /// Cor de cada ligação pelo estado; as do nó selecionado pulsam: verde no requisito que o jogador já tem, vermelho no
        /// que falta e azul no que o nó libera. As destacadas vão para a frente das outras (o mesmo túnel leva várias linhas).
        /// </summary>
        void RefreshLinks(TreeView view, List<UpgradeDef> defs, int selected)
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
                // Ligações entre colunas (e as da raiz) só aparecem com o nó selecionado, para a árvore não virar um novelo.
                link.Path.enabled = highlight || !link.Cross;
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
            LayoutDetail();

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

        /// <summary>
        /// Desce o efeito e o bloco de baixo (requisitos, custo, aviso) quando o título ou o efeito passam da altura da caixa
        /// montada pelo setup. Antes, um efeito de 4 linhas (a lixeira, W0) escrevia por cima dos requisitos (relato do
        /// desenvolvedor em 2026-10-21).
        /// </summary>
        void LayoutDetail()
        {
            float titleExtra = Mathf.Max(0f, _title.preferredHeight - _titleHeight);
            float effectHeight = Mathf.Max(_effectHeight, _effect.preferredHeight);
            RectTransform effect = _effect.rectTransform;
            effect.anchoredPosition = new Vector2(_effectPos.x, _effectPos.y - titleExtra);
            effect.sizeDelta = new Vector2(effect.sizeDelta.x, effectHeight);
            _info.rectTransform.anchoredPosition = new Vector2(_infoPos.x, _infoPos.y - titleExtra - (effectHeight - _effectHeight));
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
                if (target.Domain != def.Domain && target.Domain != UpgradeDomain.Root)
                    parts.Append(" (").Append(Loc.DomainName(target.Domain)).Append(')');
                parts.Append("</color>");
            }
            if (def.Condition != UpgradeCondition.None)
            {
                if (parts.Length > 0)
                    parts.Append(", ");
                parts.Append("<color=").Append(_board.ConditionMet(def.Condition) ? MetColor : MissingColor).Append('>')
                     .Append(Loc.T(ConditionKey(def.Condition))).Append("</color>");
            }
            if (!_board.TierOpen(def.Tier) && !UpgradeBoard.IgnoresTier(def))
                parts.Append(", <color=").Append(MissingColor).Append('>')
                     .Append(Loc.T("tree.depth", UpgradeCatalog.DepthToOpenTier(def.Tier))).Append("</color>");
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
