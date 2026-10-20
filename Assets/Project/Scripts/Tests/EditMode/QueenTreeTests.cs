using System.IO;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Árvore da rainha (seções 22 a 28 do design): compra única, pré-requisitos, classes liberadas e efeitos.</summary>
    public sealed class QueenTreeTests
    {
        const float Dt = 0.1f;
        static readonly TerrainLayout Layout = new TerrainLayout(20, new Vector2Int(20, 2), 30, 35);

        /// <summary>Solo de terra, corredor de x = 2 a 57 e berçário de <paramref name="nurseryWidth"/> colunas (3 linhas, 16 células por vaga).</summary>
        static PixelGrid MakeWorld(int nurseryWidth)
        {
            var grid = new PixelGrid(60, 24);
            for (int x = 0; x < 60; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, 20);
                for (int y = 1; y <= 20; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int x = 2; x < 58; x++)
            for (int y = 1; y <= 8; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 10; x < 10 + nurseryWidth; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetNurseryRoom(x, y, true));
            // A rainha só põe ovos na sala dela.
            for (int x = 40; x < 44; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true));
            return grid;
        }

        static AntColony MakeColony(int nurseryWidth, int nutrients)
        {
            var colony = new AntColony(MakeWorld(nurseryWidth), new ColonySettings(), Layout, seed: 1);
            colony.RestoreNutrients(nutrients, 0, 0);
            Tick(colony, 1f);
            return colony;
        }

        static void Tick(AntColony colony, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
                colony.Tick(Dt);
        }

        [Test]
        public void AtTheStart_OnlyTheCommonClassIsUnlocked_AndTheFreeNodesAreOwned()
        {
            var tree = new QueenTree();
            Assert.IsTrue(tree.Owns(QueenNode.Q0) && tree.Owns(QueenNode.O0), "Postura inicial e comum já vêm adquiridas.");
            Assert.IsTrue(tree.IsRoleUnlocked(AntRole.Worker));
            foreach (AntRole role in new[] { AntRole.Carrier, AntRole.Cutter, AntRole.Digger, AntRole.Gardener, AntRole.Nurse })
                Assert.IsFalse(tree.IsRoleUnlocked(role), $"{role} só depois de comprar o nó.");
            Assert.AreEqual(0, QueenTree.Def(QueenNode.Q0).Cost);
            Assert.AreEqual(26, QueenTree.All.Count);
        }

        [Test]
        public void TheCostsAndRequirementsFollowTheDesign()
        {
            Assert.AreEqual(150, QueenTree.Def(QueenNode.Q1).Cost);
            Assert.AreEqual(450, QueenTree.Def(QueenNode.Q2).Cost);
            Assert.AreEqual(240, QueenTree.Def(QueenNode.O3).Cost);
            Assert.AreEqual(500, QueenTree.Def(QueenNode.S3).Cost);
            CollectionAssert.AreEquivalent(new[] { QueenNode.S1, QueenNode.S2 }, QueenTree.Def(QueenNode.S3).Requires);
            CollectionAssert.AreEqual(new[] { QueenNode.L0 }, QueenTree.Def(QueenNode.L3).Requires, "L3 exige só L0 (proposta do design).");
            CollectionAssert.AreEqual(new[] { QueenNode.O0 }, QueenTree.Def(QueenNode.O3).Requires, "As melhorias da comum são independentes.");
            // Custo total por ramo (seção 28).
            int[] totals = { 600, 440, 800, 380, 780, 720, 760, 1160 };
            for (int b = 0; b < QueenTree.BranchCount; b++)
            {
                int sum = 0;
                foreach (QueenNodeDef d in QueenTree.NodesOf((QueenBranch)b))
                    sum += d.Cost;
                Assert.AreEqual(totals[b], sum, QueenTree.BranchLabel((QueenBranch)b));
            }
        }

        [Test]
        public void Buying_DebitsOnce_AndANodeNeverChargesAgain()
        {
            AntColony colony = MakeColony(21, 500);
            Assert.AreEqual(BuyBlock.None, colony.TryBuy(QueenNode.Q1));
            Assert.AreEqual(350, colony.Nutrients);
            Assert.IsTrue(colony.Tree.Owns(QueenNode.Q1));
            Assert.AreEqual(BuyBlock.Owned, colony.TryBuy(QueenNode.Q1));
            Assert.AreEqual(350, colony.Nutrients, "Comprar de novo não cobra.");
        }

        [Test]
        public void WithoutBalanceOrRequirement_NothingIsDebitedOrApplied()
        {
            AntColony colony = MakeColony(21, 100);
            Assert.AreEqual(BuyBlock.InsufficientNutrients, colony.TryBuy(QueenNode.Q1));
            Assert.IsFalse(colony.Tree.Owns(QueenNode.Q1));
            Assert.AreEqual(100, colony.Nutrients);

            colony.RestoreNutrients(1000, 0, 0);
            Assert.AreEqual(BuyBlock.MissingRequirement, colony.TryBuy(QueenNode.Q2), "Q2 exige Q1.");
            Assert.AreEqual(BuyBlock.MissingRequirement, colony.TryBuy(QueenNode.E1), "E1 exige E0.");
            Assert.AreEqual(1000, colony.Nutrients);
            Assert.IsFalse(colony.Tree.Owns(QueenNode.Q2));
        }

        [Test]
        public void ANodeWhoseSystemDoesNotExistYet_CannotBeBought()
        {
            AntColony colony = MakeColony(21, 5000);
            Assert.AreEqual(BuyBlock.NotAvailableYet, colony.TryBuy(QueenNode.S3));
            Assert.AreEqual(BuyBlock.NotAvailableYet, colony.TryBuy(QueenNode.N2));
            Assert.AreEqual(5000, colony.Nutrients, "Sem bônus invisível: nada é cobrado.");
        }

        [Test]
        public void TheSoldier_OnlyOpensAfterTheFirstTermiteRaid()
        {
            AntColony colony = MakeColony(21, 5000);
            Assert.AreEqual(BuyBlock.NeedsFirstRaid, colony.TryBuy(QueenNode.S0));
            Assert.IsFalse(colony.Tree.IsRoleUnlocked(AntRole.Soldier));

            colony.RestoreRaids(raidsUnlocked: true, raidsSurvived: 1, secondsToNextRaid: 120f);
            Assert.AreEqual(BuyBlock.None, colony.TryBuy(QueenNode.S0));
            Assert.IsTrue(colony.Tree.IsRoleUnlocked(AntRole.Soldier));
            Assert.AreEqual(18f, colony.Tree.MaxHealth(AntRole.Soldier));
            Assert.AreEqual(BuyBlock.None, colony.TryBuy(QueenNode.S2));
            Assert.AreEqual(27f, colony.Tree.MaxHealth(AntRole.Soldier));
        }

        [Test]
        public void BuyingAClassNode_UnlocksTheRole_AndTheAutomaticOnlyUsesUnlockedRoles()
        {
            AntColony colony = MakeColony(21, 1000);
            for (int i = 0; i < 6; i++)
                Assert.AreEqual(AntRole.Worker, colony.NextAntRole, "Só a comum está liberada.");

            Assert.AreEqual(BuyBlock.None, colony.TryBuy(QueenNode.L0));
            Assert.IsTrue(colony.Tree.IsRoleUnlocked(AntRole.Carrier));
            Assert.IsFalse(colony.Tree.IsRoleUnlocked(AntRole.Cutter));
            for (int i = 0; i < 10; i++)
            {
                AntRole next = colony.NextAntRole.Value;
                Assert.IsTrue(next == AntRole.Worker || next == AntRole.Carrier, $"{next} ainda está bloqueada.");
                colony.Spawn(next);
            }
            Assert.Greater(colony.CountOf(AntRole.Carrier), 0, "A carregadora liberada entra no automático.");
        }

        [Test]
        public void ALockedSpecificTarget_FallsBackToTheAutomaticChoice()
        {
            AntColony colony = MakeColony(21, 1000);
            colony.SetQueenPolicy(QueenMode.Specific, AntRole.Digger);
            Assert.AreEqual(AntRole.Worker, colony.NextAntRole);
            colony.TryBuy(QueenNode.E0);
            Assert.AreEqual(AntRole.Digger, colony.NextAntRole);
        }

        [Test]
        public void Q1_ShortensTheLayInterval_To12Seconds()
        {
            AntColony slow = MakeColony(21, 5000);
            AntColony fast = MakeColony(21, 5000);
            fast.TryBuy(QueenNode.Q1);
            // O relógio recomeça no ciclo em que o ovo sai: 18 s sem Q1, 12 s com Q1.
            for (float t = 0f; t < 25f && slow.Brood.Count == 0; t += Dt)
                slow.Tick(Dt);
            for (float t = 0f; t < 25f && fast.Brood.Count == 0; t += Dt)
                fast.Tick(Dt);
            Assert.AreEqual(1, slow.Brood.Count);
            Assert.AreEqual(1, fast.Brood.Count);
            Assert.AreEqual(18f, slow.Brood.SecondsToNextLay, 1e-3f);
            Assert.AreEqual(12f, fast.Brood.SecondsToNextLay, 1e-3f);
        }

        [Test]
        public void Q2_LaysUpToThreeEggsPerCycle_PayingEachOne()
        {
            AntColony colony = MakeColony(21, 5000);
            colony.TryBuy(QueenNode.Q1);
            colony.TryBuy(QueenNode.Q2);
            int before = colony.Nutrients;
            // O primeiro ciclo sai aos 18 s da partida (o relógio inicial é o da configuração).
            for (float t = 0f; t < 25f && colony.Brood.Count == 0; t += Dt)
                colony.Tick(Dt);
            Assert.AreEqual(3, colony.Brood.Count, "Um lote de três ovos no mesmo ciclo.");
            int spent = 0;
            foreach (BroodRecord b in colony.Brood.Brood)
                spent += colony.CostOf(b.Role);
            Assert.AreEqual(before - spent, colony.Nutrients, "Cada ovo paga o custo do seu tipo, uma vez.");
        }

        [Test]
        public void Q2_ProducesAPartialLot_WhenTheNurseryOnlyHasTwoSlots()
        {
            AntColony colony = MakeColony(14, 5000); // 14 x 3 = 42 células / 16 = 2 vagas
            Assert.AreEqual(2, colony.NurserySlots);
            colony.TryBuy(QueenNode.Q1);
            colony.TryBuy(QueenNode.Q2);
            for (float t = 0f; t < 25f && colony.Brood.Count == 0; t += Dt)
                colony.Tick(Dt);
            Assert.AreEqual(2, colony.Brood.Count, "Lote parcial: só cabem dois.");
        }

        [Test]
        public void Q2_ProducesAPartialLot_WhenTheFoodOnlyCoversOneOrTwo()
        {
            // Reserva de 30 N + cria de 18 N (comum): 80 N cobrem dois ovos (30 + 36 = 66 ≤ 80), não três (84 > 80).
            AntColony colony = MakeColony(21, 80);
            colony.RestoreNutrients(80, 0, 0);
            colony.Tree.Restore(1UL << (int)QueenNode.Q1 | 1UL << (int)QueenNode.Q2);
            for (float t = 0f; t < 25f && colony.Brood.Count == 0; t += Dt)
                colony.Tick(Dt);
            Assert.AreEqual(2, colony.Brood.Count);
            Assert.AreEqual(80 - 36, colony.Nutrients);
        }

        [Test]
        public void TheSpeedAndDigUpgrades_ChangeOnlyTheirOwnRole()
        {
            var tree = new QueenTree();
            Assert.AreEqual(1f, tree.SpeedFactor(AntRole.Worker));
            tree.Restore(1UL << (int)QueenNode.O2 | 1UL << (int)QueenNode.L3 | 1UL << (int)QueenNode.E1);
            Assert.AreEqual(30f / 24f, tree.SpeedFactor(AntRole.Worker), 1e-5f, "O2: 24 → 30 células/s.");
            Assert.AreEqual(35f / 28f, tree.SpeedFactor(AntRole.Carrier), 1e-5f, "L3: 28 → 35 células/s.");
            Assert.AreEqual(1f, tree.SpeedFactor(AntRole.Cutter));
            Assert.AreEqual(2f / 3f, tree.DigTimeFactor(AntRole.Digger), 1e-5f, "E1: 32 → 48 células/s.");
            Assert.AreEqual(1f, tree.DigTimeFactor(AntRole.Worker));
        }

        [Test]
        public void ThePurchasedNodes_SurviveTheSave_AndOldSavesKeepTheClassesTheyHad()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout = TerrainGenerator.Generate(grid, settings, 7);
            var colony = new AntColony(grid, new ColonySettings(), layout, 7);
            colony.RestoreNutrients(2000, 0, 0);
            Assert.AreEqual(BuyBlock.None, colony.TryBuy(QueenNode.Q1));
            Assert.AreEqual(BuyBlock.None, colony.TryBuy(QueenNode.E0));

            var rain = new RainSimulation(new RainSettings(), 7);
            var water = new WaterSimulation(new WaterSettings(), grid, 7);
            GameSnapshot saved = GameSnapshot.Capture(grid, colony, rain, water, 0, 7, 0.0);
            var stream = new MemoryStream();
            saved.Write(stream);
            stream.Position = 0;
            GameSnapshot loaded = GameSnapshot.Read(stream);

            var colony2 = new AntColony(grid, new ColonySettings(), layout, 7);
            loaded.ApplyTo(colony2);
            Assert.IsTrue(colony2.Tree.Owns(QueenNode.Q1) && colony2.Tree.Owns(QueenNode.E0));
            Assert.IsFalse(colony2.Tree.Owns(QueenNode.L0));

            // Saves da versão 8 não guardam a árvore: as classes que o jogo já tinha continuam liberadas.
            var legacy = new QueenTree();
            legacy.Restore(QueenTree.LegacyMask);
            foreach (AntRole role in AntRoles.All)
            {
                if (role != AntRole.Nurse && role != AntRole.Soldier) // cuidadora e soldado não existiam nesses saves
                    Assert.IsTrue(legacy.IsRoleUnlocked(role), $"{role} liberada num save antigo.");
            }
        }
    }
}
