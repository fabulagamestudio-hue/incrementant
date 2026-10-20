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
            Assert.AreEqual(65, chambers);
            Assert.AreEqual(38, galleries);
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
        public void DepthOpensTheRings()
        {
            var tree = new QueenTree();
            var board = new UpgradeBoard();
            board.Refresh(tree, 0, 0);
            Assert.AreEqual(0, board.Depth);
            Assert.IsTrue(board.RingOpen(1));
            Assert.IsFalse(board.RingOpen(2));
            foreach (QueenNode node in new[] { QueenNode.Q1, QueenNode.O1, QueenNode.O2, QueenNode.L0, QueenNode.F0 })
                Assert.AreEqual(BuyBlock.None, tree.Grant(node));
            board.Refresh(tree, 0, 0);
            Assert.AreEqual(5, board.Depth);
            Assert.IsTrue(board.RingOpen(2));
            board.SetLevelForPreview("H+1", 10); // completa: 10 níveis + 3
            board.Refresh(tree, 0, 0);
            Assert.AreEqual(18, board.Depth);
            Assert.IsFalse(board.RingOpen(3));
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
            Assert.AreEqual(UpgradeStatus.Available, board.Status(UpgradeCatalog.Get("C1"))); // com sistema, requisito e anel A1 abertos
            Assert.AreEqual(UpgradeStatus.Locked, board.Status(UpgradeCatalog.Get("C2"))); // sem sistema
            Assert.IsTrue(UpgradeBoard.HasSystem(UpgradeCatalog.Get("Q1")));
            Assert.IsTrue(UpgradeBoard.HasSystem(UpgradeCatalog.Get("O3")));
            Assert.IsFalse(UpgradeBoard.HasSystem(UpgradeCatalog.Get("L2")));
            Assert.IsFalse(UpgradeBoard.HasSystem(UpgradeCatalog.Get("D+1")));
        }

        [TestCase(UpgradeTab.Hive)]
        [TestCase(UpgradeTab.Queen)]
        public void LayoutKeepsNodesApart(UpgradeTab tab)
        {
            Dictionary<string, Vector2> positions = UpgradeLayout.Positions(tab);
            var nodes = new List<KeyValuePair<string, Vector2>>();
            foreach (KeyValuePair<string, Vector2> pair in positions)
            {
                if (UpgradeCatalog.Get(pair.Key).IsCenter)
                    continue;
                Assert.GreaterOrEqual(pair.Value.magnitude, 250f, $"{pair.Key} cobre a câmara central");
                Assert.LessOrEqual(pair.Value.magnitude, UpgradeLayout.ContentRadius() - 100f, $"{pair.Key} sai do conteúdo");
                nodes.Add(pair);
            }
            for (int i = 0; i < nodes.Count; i++)
            for (int j = i + 1; j < nodes.Count; j++)
                Assert.GreaterOrEqual(Vector2.Distance(nodes[i].Value, nodes[j].Value), 125f,
                                      $"{nodes[i].Key} e {nodes[j].Key} se sobrepõem");
        }
    }
}
