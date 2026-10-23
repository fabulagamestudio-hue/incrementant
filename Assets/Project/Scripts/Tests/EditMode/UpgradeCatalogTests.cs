using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Upgrades;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Tabela, fórmulas e layout das melhorias do documento 39 (valores da planilha aprovada em 2026-10-19).</summary>
    public sealed class UpgradeCatalogTests
    {
        [Test]
        public void TheCatalogHasTheNodesOfTheDocument()
        {
            int chambers = 0, galleries = 0;
            foreach (UpgradeDef def in UpgradeCatalog.All)
            {
                if (def.Kind == UpgradeKind.Chamber)
                    chambers++;
                else
                    galleries++;
            }
            // 65 do documento 39 + R2 (pedido do desenvolvedor em 2026-10-20, KI-045) + K0 e W0 (classes de 2026-10-21) + a
            // fazenda de pulgões (A0, A1), os depósitos de areia (D6) e de cogumelos (D7) e a fazendeira (Z0, Z1), de 2026-10-22.
            Assert.AreEqual(79, chambers); // + V0, V1, Y0, Y1, Y2 (minérios e forja, 2026-10-23)
            // 38 do documento 39 + as 6 de área pintável e a de salas extras (KI-043) + A+1, A+2, A+3 e Z+1 (2026-10-22).
            Assert.AreEqual(55, galleries); // + V+1, Y+1, Y+2, O+4, E+4, S+5
        }

        [Test]
        public void EveryQueenNodeHasACatalogNodeWithTheSameCost()
        {
            foreach (QueenNodeDef queen in QueenTree.All)
            {
                UpgradeDef def = UpgradeCatalog.Of(queen.Node);
                Assert.AreEqual(UpgradeTab.Queen, def.Tab, queen.Id);
                Assert.AreEqual(queen.Cost, def.Cost, queen.Id);
                Assert.AreEqual(queen.Node, def.QueenNode, queen.Id);
            }
        }

        [Test]
        public void RequirementsExistPointToGalleriesWhenTheyAskALevelAndHaveNoCycle()
        {
            foreach (UpgradeDef def in UpgradeCatalog.All)
            {
                foreach (UpgradeRequirement r in def.Requires)
                {
                    UpgradeDef target = UpgradeCatalog.Find(r.Id);
                    Assert.IsNotNull(target, $"{def.Id} exige {r.Id}, que não existe");
                    Assert.AreNotEqual(def.Id, r.Id);
                    if (r.Level > 0)
                    {
                        Assert.AreEqual(UpgradeKind.Gallery, target.Kind, $"{def.Id} pede nível de {r.Id}");
                        Assert.IsTrue(!target.Capped || r.Level <= target.Cap, $"{def.Id} pede {r} acima do teto");
                    }
                }
            }
            var state = new Dictionary<string, int>(); // 0 = não visto, 1 = no caminho, 2 = pronto
            foreach (UpgradeDef def in UpgradeCatalog.All)
                AssertAcyclic(def, state);
        }

        static void AssertAcyclic(UpgradeDef def, Dictionary<string, int> state)
        {
            state.TryGetValue(def.Id, out int mark);
            if (mark == 2)
                return;
            Assert.AreNotEqual(1, mark, $"Ciclo de requisitos passando por {def.Id}");
            state[def.Id] = 1;
            foreach (UpgradeRequirement r in def.Requires)
                AssertAcyclic(UpgradeCatalog.Get(r.Id), state);
            state[def.Id] = 2;
        }

        [Test]
        public void DeepNewChambersHaveACrossOrLevelRequirement()
        {
            foreach (UpgradeDef def in UpgradeCatalog.All)
            {
                // Regra do documento 39 para as câmaras novas; as das seções 17 a 28 mantêm os requisitos do design revisado
                // e as galerias ficam presas à câmara de que dependem.
                if (def.Ring < 3 || !def.IsNew || def.Kind != UpgradeKind.Chamber)
                    continue;
                bool spread = false;
                foreach (UpgradeRequirement r in def.Requires)
                    spread |= r.Level > 0 || UpgradeCatalog.Get(r.Id).Tab != def.Tab;
                spread |= def.Condition != UpgradeCondition.None;
                Assert.IsTrue(spread, $"{def.Id} (anel {def.Ring}) só depende do próprio ramo");
            }
        }

        [Test]
        public void GalleryCapsFollowTheirType()
        {
            foreach (UpgradeDef def in UpgradeCatalog.All)
            {
                if (def.Kind != UpgradeKind.Gallery)
                    continue;
                Assert.AreEqual(def.Type == GalleryType.Physical, def.Capped, def.Id);
                if (def.Capped && !def.Additive && def.PerLevel < 0)
                    Assert.LessOrEqual(-def.PerLevel * def.Cap, 0.5f + 1e-4f, $"{def.Id} reduz mais de 50%");
            }
        }

        [TestCase("H+1", 1, 60)]
        [TestCase("H+1", 10, 894)]
        [TestCase("C+1", 10, 141)]
        [TestCase("C+1", 25, 1145)]
        [TestCase("C+3", 2, 203)]
        [TestCase("S+1", 10, 1197)]
        public void LevelCostsMatchTheSpreadsheet(string id, int level, int cost)
        {
            Assert.AreEqual(cost, UpgradeCatalog.LevelCost(UpgradeCatalog.Get(id), level));
        }

        [Test]
        public void CappedGalleriesStopAtTheCap()
        {
            UpgradeDef def = UpgradeCatalog.Get("O+2");
            Assert.AreEqual(0, UpgradeCatalog.LevelCost(def, 11));
            long total = UpgradeCatalog.CostOfLevels(def, 0, 50, out int bought);
            Assert.AreEqual(10, bought);
            Assert.AreEqual(8189, total); // planilha: custo total até o teto
            Assert.AreEqual(2f, UpgradeCatalog.ValueAt(def, 10), 1e-3f);
        }

        [Test]
        public void MaxAffordableNeverOverspends()
        {
            UpgradeDef def = UpgradeCatalog.Get("C+1");
            long spent = UpgradeCatalog.MaxAffordable(def, 0, 200, out int bought);
            Assert.AreEqual(4, bought); // 40 + 46 + 53 + 61 = 200
            Assert.LessOrEqual(spent, 200);
            Assert.Greater(spent + UpgradeCatalog.LevelCost(def, bought + 1), 200);
        }

        [Test]
        public void EconomyMilestonesMultiplyTheBonus()
        {
            UpgradeDef def = UpgradeCatalog.Get("C+1");
            Assert.AreEqual(1f + 0.08f * 9f, UpgradeCatalog.ValueAt(def, 9), 1e-4f);
            Assert.AreEqual(1f + 0.08f * 10f * 1.5f, UpgradeCatalog.ValueAt(def, 10), 1e-4f);
            Assert.AreEqual(5.5f, UpgradeCatalog.ValueAt(def, 25), 1e-4f);
            Assert.AreEqual(10, UpgradeCatalog.NextMilestone(def, 3));
            Assert.AreEqual(25, UpgradeCatalog.NextMilestone(def, 20));
            Assert.AreEqual(30, UpgradeCatalog.NextMilestone(def, 25));
            Assert.AreEqual(0, UpgradeCatalog.NextMilestone(UpgradeCatalog.Get("O+1"), 3));
        }

        [Test]
        public void DepthOpensTheTiers()
        {
            var tree = new QueenTree();
            var board = new UpgradeBoard();
            board.Refresh(tree, 0, 0);
            Assert.AreEqual(0, board.Depth);
            Assert.IsTrue(board.TierOpen(1));
            Assert.IsFalse(board.TierOpen(2));
            foreach (QueenNode node in new[] { QueenNode.Q1, QueenNode.O1, QueenNode.O2, QueenNode.L0, QueenNode.F0 })
                Assert.AreEqual(BuyBlock.None, tree.Grant(node));
            board.Refresh(tree, 0, 0);
            Assert.AreEqual(5, board.Depth);
            Assert.IsTrue(board.TierOpen(2));
            board.SetLevelForPreview("H+1", 10); // completa: 10 níveis + 3
            board.Refresh(tree, 0, 0);
            Assert.AreEqual(18, board.Depth);
            Assert.IsFalse(board.TierOpen(3));
        }

        [Test]
        public void BoardFollowsTheQueenTreeAndTheFreeBrushes()
        {
            var tree = new QueenTree();
            var board = new UpgradeBoard();
            board.Refresh(tree, 0, 0);
            Assert.AreEqual(UpgradeStatus.Owned, board.Status(UpgradeCatalog.Get("C0")));
            Assert.AreEqual(UpgradeStatus.Owned, board.Status(UpgradeCatalog.Get("D4")));
            Assert.AreEqual(UpgradeStatus.Available, board.Status(UpgradeCatalog.Get("Q1")));
            Assert.AreEqual(UpgradeStatus.Locked, board.Status(UpgradeCatalog.Get("S0"))); // espera o primeiro ataque
            Assert.AreEqual(UpgradeStatus.Available, board.Status(UpgradeCatalog.Get("C1"))); // com sistema, requisito e nível 1 abertos
            Assert.AreEqual(UpgradeStatus.Locked, board.Status(UpgradeCatalog.Get("C2"))); // sem sistema
            Assert.IsTrue(UpgradeBoard.HasSystem(UpgradeCatalog.Get("Q1")));
            Assert.IsTrue(UpgradeBoard.HasSystem(UpgradeCatalog.Get("O3")));
            Assert.IsFalse(UpgradeBoard.HasSystem(UpgradeCatalog.Get("L2")));
            Assert.IsFalse(UpgradeBoard.HasSystem(UpgradeCatalog.Get("D+1")));
        }

        [Test]
        public void LayoutKeepsNodesApart()
        {
            var layout = new UpgradeLayout();
            var nodes = new List<KeyValuePair<string, Vector2>>();
            foreach (KeyValuePair<string, Vector2> pair in layout.Positions)
            {
                UpgradeDef def = UpgradeCatalog.Get(pair.Key);
                if (def.Domain == UpgradeDomain.Root)
                {
                    Assert.AreEqual(layout.Root, pair.Value, pair.Key);
                    continue;
                }
                Assert.IsTrue(layout.Bounds.Contains(pair.Value), $"{pair.Key} sai do conteúdo");
                nodes.Add(pair);
            }
            for (int i = 0; i < nodes.Count; i++)
            for (int j = i + 1; j < nodes.Count; j++)
                Assert.GreaterOrEqual(Vector2.Distance(nodes[i].Value, nodes[j].Value), 125f,
                                      $"{nodes[i].Key} e {nodes[j].Key} se sobrepõem");
        }

        // ---------------------------------------------------------------- formigueiro em níveis (documento 46)

        [Test]
        public void EveryNodeHasAPlaceInTheAnthill()
        {
            foreach (UpgradeDef def in UpgradeCatalog.All)
            {
                Assert.IsTrue(def.Placed, $"{def.Id} sem domínio e nível no formigueiro");
                Assert.Less(def.Tier, UpgradeCatalog.TierCount, $"{def.Id} abaixo do último nível");
                // A raiz é exatamente a câmara central das duas abas antigas.
                Assert.AreEqual(def.IsCenter, def.Domain == UpgradeDomain.Root, def.Id);
                if (def.Domain == UpgradeDomain.Root)
                    Assert.AreEqual(0, def.Tier, def.Id);
            }
        }

        [Test]
        public void TunnelsOnlyGoDown()
        {
            foreach (UpgradeDef def in UpgradeCatalog.All)
            foreach (UpgradeRequirement r in def.Requires)
            {
                UpgradeDef target = UpgradeCatalog.Get(r.Id);
                Assert.GreaterOrEqual(def.Tier, target.Tier, $"{def.Id} (nível {def.Tier}) fica acima do requisito {r.Id} (nível {target.Tier})");
            }
        }

        [Test]
        public void NodesNeverSitAboveTheHallOfTheirDomain()
        {
            foreach (UpgradeDef def in UpgradeCatalog.All)
                Assert.GreaterOrEqual(def.Tier, UpgradeCatalog.OriginTier(def.Domain), $"{def.Id} acima do salão de {def.Domain}");
        }

        [Test]
        public void SideChambersHoldAtMostEightNodes()
        {
            var counts = new Dictionary<(UpgradeDomain, int), int>();
            foreach (UpgradeDef def in UpgradeCatalog.All)
            {
                if (def.Domain == UpgradeDomain.Root)
                    continue;
                counts.TryGetValue((def.Domain, def.Tier), out int n);
                counts[(def.Domain, def.Tier)] = n + 1;
            }
            foreach (KeyValuePair<(UpgradeDomain, int), int> pair in counts)
                Assert.LessOrEqual(pair.Value, UpgradeCatalog.MaxNodesPerSideChamber, $"{pair.Key.Item1} nível {pair.Key.Item2}");
        }

        [Test]
        public void EveryDomainIsOneColumn()
        {
            var seen = new HashSet<UpgradeDomain>();
            foreach (UpgradeDomainDef d in UpgradeCatalog.Domains)
            {
                Assert.AreNotEqual(UpgradeDomain.Root, d.Domain, "a raiz fica no topo, fora das colunas");
                Assert.IsTrue(seen.Add(d.Domain), $"{d.Domain} em duas colunas");
                Assert.That(d.OriginTier, Is.InRange(0, UpgradeCatalog.TierCount - 1), d.Domain.ToString());
            }
            foreach (UpgradeDomain domain in System.Enum.GetValues(typeof(UpgradeDomain)))
                if (domain != UpgradeDomain.Root)
                    Assert.IsTrue(seen.Contains(domain), $"{domain} sem coluna");
        }

        [Test]
        public void EveryNodeSitsInsideTheBandOfItsTierAndNearItsColumn()
        {
            var layout = new UpgradeLayout();
            foreach (UpgradeDef def in UpgradeCatalog.All)
            {
                if (def.Domain == UpgradeDomain.Root)
                    continue;
                Vector2 p = layout.Positions[def.Id];
                // Abaixo do salão do domínio (que fica no topo da faixa) e acima do fim da faixa.
                Assert.Less(p.y, layout.TierTop(def.Tier) - UpgradeLayout.BandTop + UpgradeLayout.RowSpacing * 0.5f, def.Id);
                Assert.Greater(p.y, layout.TierBottom(def.Tier), def.Id);
                Assert.LessOrEqual(Mathf.Abs(p.x - layout.DomainX(def.Domain)), UpgradeLayout.NodeSpacing * 0.5f + 0.01f, def.Id);
            }
            // Colunas da esquerda para a direita, e níveis de cima para baixo.
            for (int c = 1; c < UpgradeCatalog.Domains.Length; c++)
                Assert.Greater(UpgradeLayout.ColumnX(c), UpgradeLayout.ColumnX(c - 1));
            for (int t = 1; t < UpgradeCatalog.TierCount; t++)
                Assert.AreEqual(layout.TierBottom(t - 1), layout.TierTop(t));
            Assert.Greater(layout.Root.y, UpgradeLayout.GalleryY);
            // O rótulo do nível, à esquerda, termina antes do salão (meia largura 170) e da câmara mais larga da primeira coluna.
            float labelEnd = layout.Bounds.xMin + UpgradeLayout.TierLabelInset + UpgradeLayout.TierLabelWidth;
            Assert.Less(labelEnd, UpgradeLayout.ColumnX(0) - UpgradeLayout.ColumnWidth * 0.5f);
        }

        [Test]
        public void TiersRepeatTheRingsExceptVitality()
        {
            // Documento 46, "Distribuição dos nós atuais": nível = anel do documento 39; O+2 desce por exigir B1.
            foreach (UpgradeDef def in UpgradeCatalog.All)
                Assert.AreEqual(def.Id == "O+2" ? 2 : def.Ring, def.Tier, def.Id);
            Assert.AreEqual(UpgradeDomain.Ants, UpgradeCatalog.Get("O+2").Domain);
            Assert.AreEqual(UpgradeDomain.Queen, UpgradeCatalog.Get("H+1").Domain);
            Assert.AreEqual(UpgradeDomain.Waste, UpgradeCatalog.Get("W0").Domain);
            Assert.AreEqual(UpgradeDomain.Farm, UpgradeCatalog.Get("Z0").Domain);
        }

        [Test]
        public void DeeperTiersNeverOpenEarlier()
        {
            Assert.AreEqual(0, UpgradeCatalog.DepthToOpenTier(0));
            Assert.AreEqual(0, UpgradeCatalog.DepthToOpenTier(1));
            for (int tier = 1; tier < UpgradeCatalog.TierCount; tier++)
                Assert.GreaterOrEqual(UpgradeCatalog.DepthToOpenTier(tier), UpgradeCatalog.DepthToOpenTier(tier - 1));
            // Os níveis 0 a 4 repetem os anéis de hoje.
            for (int ring = 0; ring <= 4; ring++)
                Assert.AreEqual(UpgradeCatalog.DepthToOpen(ring), UpgradeCatalog.DepthToOpenTier(ring));
        }
    }
}
