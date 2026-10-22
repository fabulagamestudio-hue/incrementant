using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Cuspideira (ataque a distância) e lixeira (resíduos), pedidas pelo desenvolvedor em 2026-10-21.</summary>
    public sealed class SpitterAndMiddenTests
    {
        const float Dt = 0.1f;
        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);

        /// <summary>Solo de terra até y = 20, corredor de x 10 a 100 com 3 linhas (y 1 a 3) e um poço em x 50..51 até a superfície.</summary>
        static PixelGrid MakeWorld()
        {
            var grid = new PixelGrid(120, 40);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, 20);
                for (int y = 1; y <= 20; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int x = 10; x <= 100; x++)
            for (int y = 1; y <= 3; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 50; x <= 51; x++)
            for (int y = 4; y <= 20; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 88; x <= 98; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true));
            return grid;
        }

        static AntColony MakeColony(PixelGrid grid, params QueenNode[] owned)
        {
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3) { RaidsEnabled = true };
            colony.RaidSettings.HuntQueen = false;
            colony.RestoreQueen(95, 2, 1);
            colony.RestoreRaids(raidsUnlocked: true, raidsSurvived: 1, secondsToNextRaid: 10000f);
            ulong mask = 0;
            foreach (QueenNode node in owned)
                mask |= 1UL << (int)node;
            colony.Tree.Restore(mask);
            colony.Tick(Dt);
            return colony;
        }

        /// <summary>Passos com os cupins parados no lugar (sem alvo eles iriam embora).</summary>
        static void TickFrozen(AntColony colony, float seconds, Termite frozen)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                frozen.Body.Cooldown = 5f;
                colony.Tick(Dt);
            }
        }

        [Test]
        public void BothClassesWaitForTheFirstRaid_AndTheSpitterNeedsTheSoldier()
        {
            var tree = new QueenTree();
            Assert.AreEqual(BuyBlock.NeedsFirstRaid, tree.Check(QueenNode.K0));
            Assert.AreEqual(BuyBlock.NeedsFirstRaid, tree.Check(QueenNode.W0));
            tree.SoldiersAvailable = true;
            Assert.AreEqual(BuyBlock.MissingRequirement, tree.Check(QueenNode.K0), "A cuspideira exige o soldado (S0).");
            Assert.AreEqual(BuyBlock.None, tree.Grant(QueenNode.W0));
            Assert.AreEqual(BuyBlock.None, tree.Grant(QueenNode.S0));
            Assert.AreEqual(BuyBlock.None, tree.Grant(QueenNode.K0));
            Assert.IsTrue(tree.IsRoleUnlocked(AntRole.Spitter));
            Assert.IsTrue(tree.IsRoleUnlocked(AntRole.Midden));
            Assert.AreEqual(QueenNode.K0, QueenTree.UnlockNodeOf(AntRole.Spitter));
            Assert.AreEqual(QueenNode.W0, QueenTree.UnlockNodeOf(AntRole.Midden));
        }

        [Test]
        public void Spitter_KillsATermiteFromAfar_WithoutTakingABiteSlotOrABite()
        {
            AntColony colony = MakeColony(MakeWorld(), QueenNode.S0, QueenNode.K0);
            Ant spitter = colony.SpawnAt(AntRole.Spitter, 60, 2);
            Termite termite = colony.SpawnTermite(66, 2);
            termite.Health = 4f;

            bool spat = false;
            for (float t = 0f; t < 4f && termite.Alive; t += Dt)
            {
                termite.Body.Cooldown = 5f;
                colony.Tick(Dt);
                if (colony.Spits.Count > 0)
                {
                    spat = true;
                    Assert.AreEqual(0, termite.Attackers, "Cuspir não ocupa vaga de mordida.");
                    Assert.Greater(Mathf.Max(Mathf.Abs(termite.X - spitter.X), Mathf.Abs(termite.Y - spitter.Y)),
                                   colony.RaidSettings.AttackReach, "Cospe de fora do alcance da mordida.");
                }
            }
            Assert.IsTrue(spat, "A cuspideira cospe no cupim a 6 células.");
            Assert.IsFalse(termite.Alive, "Dois cuspes de 2 matam o cupim com 4 de vida.");
            Assert.AreEqual(0f, spitter.Wounds, "Ela não chegou perto da mordida do cupim.");
            Assert.LessOrEqual(spitter.X, 62, "Ela não precisa andar até o cupim.");
        }

        [Test]
        public void Spitter_DoesNotSpitThroughSolidBlocks()
        {
            PixelGrid grid = MakeWorld();
            for (int y = 1; y <= 3; y++)
                grid.Set(63, y, CellMaterial.Rock);
            AntColony colony = MakeColony(grid, QueenNode.S0, QueenNode.K0);
            colony.SpawnAt(AntRole.Spitter, 60, 2);
            Termite termite = colony.SpawnTermite(66, 2);
            float health = termite.Health;
            for (float t = 0f; t < 2f; t += Dt)
            {
                termite.Body.Cooldown = 5f;
                colony.Tick(Dt);
                Assert.AreEqual(0, colony.Spits.Count, "A rocha entre as duas fecha a linha do cuspe.");
            }
            Assert.AreEqual(health, termite.Health);
        }

        [Test]
        public void Spitter_WithTheLineClosed_StopsOutsideTheBiteInsteadOfClosingIn()
        {
            // Nicho lateral no poço (x 52..53, y 10): de dentro do poço a terra fecha a linha até o cupim escondido nele.
            PixelGrid grid = MakeWorld();
            for (int x = 52; x <= 53; x++)
                grid.Set(x, 10, CellMaterial.Empty);
            AntColony colony = MakeColony(grid, QueenNode.S0, QueenNode.K0);
            Ant spitter = colony.SpawnAt(AntRole.Spitter, 40, 2);
            Termite termite = colony.SpawnTermite(53, 10);
            termite.Health = 1000f;

            int closest = int.MaxValue;
            for (float t = 0f; t < 6f; t += Dt)
            {
                termite.Body.Cooldown = 5f;
                colony.Tick(Dt);
                closest = Mathf.Min(closest, Mathf.Max(Mathf.Abs(termite.X - spitter.X), Mathf.Abs(termite.Y - spitter.Y)));
            }
            Assert.Greater(closest, colony.RaidSettings.AttackReach, "A cuspideira não entra no alcance da mordida do cupim.");
            Assert.LessOrEqual(closest, colony.RaidSettings.AttackReach + 1, "Ela chega até a borda do alcance e espera.");
            Assert.AreEqual(0f, spitter.Wounds);
        }

        [Test]
        public void Midden_ClearsATermiteBody_WithoutAnyOtherAntAround()
        {
            AntColony colony = MakeColony(MakeWorld(), QueenNode.W0);
            // Sem as formigas iniciais: a comum também tira corpos e chegaria antes.
            foreach (Ant ant in colony.Ants)
                ant.Wounds = 1000f;
            colony.Tick(Dt);
            Assert.AreEqual(0, colony.Ants.Count);
            Ant midden = colony.SpawnAt(AntRole.Midden, 40, 2);
            Termite corpse = colony.SpawnTermite(30, 1);
            corpse.Phase = TermitePhase.Dead;
            corpse.Health = 0f;
            corpse.LeavesLeft = 0;
            corpse.RemainsLeft = 1;

            bool dismembered = false;
            for (float t = 0f; t < 15f && corpse.RemainsLeft > 0; t += Dt)
            {
                colony.Tick(Dt);
                dismembered |= midden.State == AntState.Dismembering;
            }
            Assert.IsTrue(dismembered, "A lixeira vai ao corpo do cupim.");
            Assert.AreEqual(0, corpse.RemainsLeft, "O resto foi tirado do corpo.");
            Assert.IsFalse(midden.ActingAsWorker, "Tirar corpo é trabalho do papel dela, não o da comum.");
        }
    }
}
