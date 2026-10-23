using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fabula.Incrementant.Upgrades
{
    /// <summary>
    /// Formigueiro em corte da tela Evolução (documento 46): a Câmara real no topo, uma galeria horizontal logo abaixo dela e
    /// uma coluna por domínio (<see cref="UpgradeCatalog.Domains"/>) que desce da galeria. Cada nível do formigueiro
    /// (<see cref="UpgradeDef.Tier"/>) é uma faixa horizontal; na faixa do nível de origem fica o salão do domínio (o rótulo) e,
    /// em cada nível com nós do domínio, uma câmara lateral com os nós em grade de até duas colunas (câmaras antes das
    /// galerias; o nó cujos pais ficam mais para um lado vai para a coluna desse lado). Coordenadas em pixels de UI, y para
    /// cima, topo do conteúdo em y = 0 e a coluna do meio em x = 0.
    /// </summary>
    public sealed class UpgradeLayout
    {
        public const float ColumnWidth = 420f;
        /// <summary>Distância entre linhas e entre as duas colunas de uma câmara lateral.</summary>
        public const float RowSpacing = 140f;
        public const float NodeSpacing = 130f;
        /// <summary>Folga da faixa acima dos nós (cabe o salão do domínio) e abaixo deles.</summary>
        public const float BandTop = 170f, BandBottom = 70f;
        /// <summary>
        /// Faixa do nível 0: a Câmara real sozinha no topo, a galeria abaixo dela e, pendurados na galeria como as outras
        /// colunas, os salões dos domínios ligados à raiz (<see cref="RootHallY"/>).
        /// </summary>
        public const float RootBandHeight = 540f;
        public const float RootY = -150f, GalleryY = -330f, RootHallY = -440f;
        /// <summary>Distância do topo da faixa ao centro do salão do domínio.</summary>
        public const float HallDrop = 85f;
        /// <summary>Margem lateral (à esquerda cabem os rótulos dos níveis, antes do salão da primeira coluna) e embaixo.</summary>
        public const float SideMargin = 660f, BottomMargin = 160f;
        /// <summary>Largura do rótulo do nível, alinhado à esquerda a partir da borda do conteúdo.</summary>
        public const float TierLabelWidth = 380f, TierLabelInset = 40f;

        readonly Dictionary<string, Vector2> _positions = new Dictionary<string, Vector2>();
        readonly float[] _tierTop;
        readonly float[] _tierBottom;
        readonly Dictionary<UpgradeDomain, int> _column = new Dictionary<UpgradeDomain, int>();

        /// <summary>Posição de cada nó (os da raiz ficam todos na Câmara real).</summary>
        public IReadOnlyDictionary<string, Vector2> Positions => _positions;
        public Vector2 Root { get; }
        /// <summary>Retângulo do conteúdo inteiro, com margem: o que a janela pode mostrar.</summary>
        public Rect Bounds { get; }

        public float TierTop(int tier) => _tierTop[tier];
        public float TierBottom(int tier) => _tierBottom[tier];
        public int Column(UpgradeDomain domain) => _column[domain];
        public static float ColumnX(int column) => (column - (UpgradeCatalog.Domains.Length - 1) * 0.5f) * ColumnWidth;
        public float DomainX(UpgradeDomain domain) => ColumnX(_column[domain]);
        /// <summary>Centro do salão do domínio, no nível de origem dele (no nível 0, abaixo da galeria).</summary>
        public Vector2 Hall(UpgradeDomain domain)
        {
            int origin = UpgradeCatalog.OriginTier(domain);
            return new Vector2(DomainX(domain), origin == 0 ? RootHallY : _tierTop[origin] - HallDrop);
        }

        public UpgradeLayout()
        {
            UpgradeDomainDef[] domains = UpgradeCatalog.Domains;
            for (int c = 0; c < domains.Length; c++)
                _column[domains[c].Domain] = c;

            // Câmaras laterais: (domínio, nível) → nós, câmaras antes das galerias, na ordem da tabela.
            var groups = new Dictionary<(UpgradeDomain, int), List<UpgradeDef>>();
            int tiers = UpgradeCatalog.TierCount;
            var rows = new int[tiers];
            foreach (UpgradeDef def in UpgradeCatalog.All)
            {
                if (def.Domain == UpgradeDomain.Root)
                    continue;
                if (!groups.TryGetValue((def.Domain, def.Tier), out List<UpgradeDef> list))
                    groups[(def.Domain, def.Tier)] = list = new List<UpgradeDef>();
                list.Add(def);
            }
            foreach (KeyValuePair<(UpgradeDomain, int), List<UpgradeDef>> pair in groups)
            {
                pair.Value.Sort((a, b) => a.Kind != b.Kind ? a.Kind.CompareTo(b.Kind) : IndexOf(a).CompareTo(IndexOf(b)));
                rows[pair.Key.Item2] = Math.Max(rows[pair.Key.Item2], (pair.Value.Count + 1) / 2);
            }

            // Faixas dos níveis, de cima para baixo; um nível sem nós ainda tem a altura de uma linha.
            _tierTop = new float[tiers];
            _tierBottom = new float[tiers];
            float y = 0f;
            for (int t = 0; t < tiers; t++)
            {
                _tierTop[t] = y;
                y -= t == 0 ? RootBandHeight : BandTop + Math.Max(1, rows[t]) * RowSpacing + BandBottom;
                _tierBottom[t] = y;
            }

            // A raiz fica sobre as colunas dos domínios ligados a ela (salão no nível 0).
            float sum = 0f;
            int count = 0;
            foreach (UpgradeDomainDef d in domains)
                if (d.OriginTier == 0)
                {
                    sum += DomainX(d.Domain);
                    count++;
                }
            Root = new Vector2(count > 0 ? sum / count : 0f, RootY);
            foreach (UpgradeDef def in UpgradeCatalog.All)
                if (def.Domain == UpgradeDomain.Root)
                    _positions[def.Id] = Root;

            // Do nível mais raso ao mais fundo: a coluna de cada nó olha a posição dos pais, que precisam estar prontos antes.
            for (int t = 1; t < tiers; t++)
                foreach (UpgradeDomainDef d in domains)
                    if (groups.TryGetValue((d.Domain, t), out List<UpgradeDef> list))
                        Place(list, DomainX(d.Domain), (_tierTop[t] - BandTop + _tierBottom[t] + BandBottom) * 0.5f);

            float half = ColumnX(domains.Length - 1) + SideMargin;
            Bounds = Rect.MinMaxRect(-half, _tierBottom[tiers - 1] - BottomMargin, half, 0f);
        }

        static int IndexOf(UpgradeDef def)
        {
            IReadOnlyList<UpgradeDef> all = UpgradeCatalog.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i] == def)
                    return i;
            return -1;
        }

        void Place(List<UpgradeDef> nodes, float axisX, float centerY)
        {
            int columns = nodes.Count == 1 ? 1 : 2;
            if (columns == 2)
                for (int i = 0; i + 1 < nodes.Count; i += 2)
                    if (ParentSide(nodes[i], axisX) > ParentSide(nodes[i + 1], axisX) + 1f)
                        (nodes[i], nodes[i + 1]) = (nodes[i + 1], nodes[i]);
            int rows = (nodes.Count + columns - 1) / columns;
            for (int i = 0; i < nodes.Count; i++)
            {
                int row = i / columns;
                int column = i % columns;
                // A última linha com um nó só fica no eixo do túnel.
                int inRow = Math.Min(columns, nodes.Count - row * columns);
                float lateral = inRow == 1 ? 0f : (column - 0.5f) * NodeSpacing;
                float down = (row - (rows - 1) * 0.5f) * RowSpacing;
                _positions[nodes[i].Id] = new Vector2(axisX + lateral, centerY - down);
            }
        }

        /// <summary>Média do deslocamento em x dos pais já posicionados, em relação ao eixo; 0 sem pais.</summary>
        float ParentSide(UpgradeDef def, float axisX)
        {
            float sum = 0f;
            int count = 0;
            foreach (UpgradeRequirement r in def.Requires)
                if (_positions.TryGetValue(r.Id, out Vector2 p))
                {
                    sum += p.x - axisX;
                    count++;
                }
            return count == 0 ? 0f : sum / count;
        }
    }
}
