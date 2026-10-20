using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Upgrades;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Distribuição dos soldados em combate (KI-041), postos de guarda nas entradas (M2, KI-030) e velocidade por situação
    /// (L+3 e S+4, KI-038).
    /// </summary>
    public sealed class CombatSpreadTests
    {
        const float Dt = 0.1f;
        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);

        /// <summary>
        /// Solo de terra até y = 20 (céu acima), corredor de x 10 a 100 com 3 linhas (y 1 a 3) e um poço em x 50..51 até a
        /// superfície (e outro em x 90..91 com <paramref name="secondOpening"/>).
        /// </summary>
        static PixelGrid MakeWorld(bool secondOpening = false)
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
            foreach (int wellX in secondOpening ? new[] { 50, 90 } : new[] { 50 })
            for (int x = wellX; x <= wellX + 1; x++)
            for (int y = 4; y <= 20; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 88; x <= 98; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true));
            return grid;
        }

        static AntColony MakeColony(PixelGrid grid)
        {
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3) { RaidsEnabled = true };
            colony.RaidSettings.HuntQueen = false;
            colony.RestoreQueen(95, 2, 1);
            colony.RestoreRaids(raidsUnlocked: true, raidsSurvived: 1, secondsToNextRaid: 10000f);
            colony.Tree.Restore(1UL << (int)QueenNode.S0);
            Tick(colony, 1f);
            return colony;
        }

        static void Tick(AntColony colony, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
                colony.Tick(Dt);
        }

        /// <summary>Passos com os cupins parados no lugar (sem alvo no mapa eles iriam embora pelas laterais em segundos).</summary>
        static void TickFrozen(AntColony colony, float seconds, params Termite[] frozen)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                foreach (Termite termite in frozen)
                    termite.Body.Cooldown = 5f;
                colony.Tick(Dt);
            }
        }

        /// <summary>Passos finos: com 0,1 s a formiga anda no máximo 10 células/s e as velocidades altas se igualam.</summary>
        static void TickFine(AntColony colony, float seconds, Termite frozen = null)
        {
            for (float t = 0f; t < seconds; t += 0.02f)
            {
                if (frozen != null)
                    frozen.Body.Cooldown = 5f;
                colony.Tick(0.02f);
            }
        }

        static void Own(AntColony colony, string id, int level = 1) => colony.RestoreGalleries(new[] { id }, new[] { level });

        /// <summary>Cupim que fica "no ar" (não entra no combate nem anda), só para manter o ataque aberto.</summary>
        static Termite HoldRaidOpen(AntColony colony, int x)
        {
            Termite flying = colony.SpawnTermite(x, 21);
            flying.Phase = TermitePhase.Flying;
            flying.PhaseTimer = 10000f;
            return flying;
        }

        [Test]
        public void NewUpgradesHaveASystem_AndTheEffectsGrowWithTheLevel()
        {
            foreach (string id in new[] { "L+3", "S+4", "M2" })
                Assert.IsTrue(UpgradeBoard.HasSystem(UpgradeCatalog.Get(id)), id);

            var colony = MakeColony(MakeWorld());
            Assert.AreEqual(1f, colony.Effects.CarrierLoadedSpeedRate, 1e-5f);
            Assert.AreEqual(1f, colony.Effects.SoldierBattleSpeedRate, 1e-5f);
            Assert.IsFalse(colony.Effects.SoldierPosting);

            colony.RestoreGalleries(new[] { "L+3", "S+4", "M2" }, new[] { 3, 10, 1 });
            Assert.AreEqual(1.12f, colony.Effects.CarrierLoadedSpeedRate, 1e-4f);
            Assert.AreEqual(1.5f, colony.Effects.SoldierBattleSpeedRate, 1e-4f);
            Assert.IsTrue(colony.Effects.SoldierPosting);
        }

        static int SoldierDistanceAfter(int battleLevel)
        {
            AntColony colony = MakeColony(MakeWorld());
            if (battleLevel > 0)
                Own(colony, "S+4", battleLevel);
            Ant soldier = colony.SpawnAt(AntRole.Soldier, 95, 2);
            Termite far = colony.SpawnTermite(10, 21);
            far.Health = 1000f;
            TickFine(colony, 0.6f, far);
            Assert.AreEqual(AntState.Fighting, soldier.State);
            return Mathf.Abs(soldier.X - 95);
        }

        [Test]
        public void BattleFury_MakesTheSoldierWalkFasterWhileFighting()
        {
            int slow = SoldierDistanceAfter(0);
            int fast = SoldierDistanceAfter(10);
            Assert.Greater(fast, slow, "S+4 no nível 10 dá 50% mais velocidade em batalha.");
        }

        static int CarrierDistanceAfter(int level)
        {
            AntColony colony = MakeColony(MakeWorld());
            if (level > 0)
                Own(colony, "L+3", level);
            colony.RestoreGalleries(new[] { "L+3", "L3" }, new[] { level, 1 });
            Ant carrier = colony.SpawnAt(AntRole.Carrier, 95, 2);
            carrier.Carried = CellMaterial.Dirt;
            carrier.State = AntState.Carrying;
            TickFine(colony, 0.4f);
            return Mathf.Abs(carrier.X - 95) + Mathf.Abs(carrier.Y - 2);
        }

        [Test]
        public void LoadedSprint_MakesTheCarrierWalkFasterOnlyWhileCarrying()
        {
            int slow = CarrierDistanceAfter(0);
            int fast = CarrierDistanceAfter(10);
            Assert.Greater(fast, slow, "L+3 no nível 10 dá 40% mais velocidade com carga.");
        }

        [Test]
        public void Soldiers_SpreadAcrossTermites_AndNoTermiteGetsMoreThanTheCap()
        {
            AntColony colony = MakeColony(MakeWorld());
            int cap = colony.RaidSettings.MaxAttackersPerTermite;
            Assert.AreEqual(3, cap);
            for (int i = 0; i < 6; i++)
                colony.SpawnAt(AntRole.Soldier, 95, 2);
            Termite west = colony.SpawnTermite(15, 21);
            Termite east = colony.SpawnTermite(110, 21);
            west.Health = east.Health = 100000f;

            int maxAttackers = 0;
            bool bothEngaged = false;
            for (float t = 0f; t < 25f && !bothEngaged; t += Dt)
            {
                west.Body.Cooldown = east.Body.Cooldown = 5f;
                colony.Tick(Dt);
                maxAttackers = Mathf.Max(maxAttackers, Mathf.Max(west.Attackers, east.Attackers));
                bothEngaged = west.Attackers >= 2 && east.Attackers >= 2;
            }
            Assert.IsTrue(bothEngaged, "Os soldados se dividem entre os dois cupins em vez de irem todos ao mais perto.");
            Assert.LessOrEqual(maxAttackers, cap, "Nenhum cupim passa do teto de atacantes enquanto há outro livre.");
        }

        [Test]
        public void WithMoreSoldiersThanFreeSlots_TheExtrasStillJoinTheFight()
        {
            AntColony colony = MakeColony(MakeWorld());
            for (int i = 0; i < 5; i++)
                colony.SpawnAt(AntRole.Soldier, 95, 2);
            Termite lone = colony.SpawnTermite(100, 21);
            lone.Health = 100000f;
            bool crowded = false;
            for (float t = 0f; t < 25f && !crowded; t += Dt)
            {
                lone.Body.Cooldown = 5f;
                colony.Tick(Dt);
                crowded = lone.Attackers > colony.RaidSettings.MaxAttackersPerTermite;
            }
            Assert.IsTrue(crowded, "Com um só cupim, o teto é ignorado e todas as soldadas o atacam.");
        }

        [Test]
        public void GuardPosts_SoldiersWaitAtTheOpening_UntilATermiteGetsClose()
        {
            AntColony colony = MakeColony(MakeWorld());
            Own(colony, "M2");
            var soldiers = new Ant[4];
            for (int i = 0; i < soldiers.Length; i++)
                soldiers[i] = colony.SpawnAt(AntRole.Soldier, 95, 2);
            HoldRaidOpen(colony, 115);

            Tick(colony, 15f);
            foreach (Ant soldier in soldiers)
            {
                Assert.AreEqual(AntState.Fighting, soldier.State);
                Assert.IsTrue(soldier.OnGuard, $"Sem cupim por perto, o soldado espera no posto (x = {soldier.X}, y = {soldier.Y}).");
                bool atWestStrip = soldier.X >= 28 && soldier.X <= 44, atEastStrip = soldier.X >= 57 && soldier.X <= 73;
                Assert.IsTrue(atWestStrip || atEastStrip, $"O posto fica nas faixas ao lado da abertura (x = {soldier.X}).");
                Assert.Greater(soldier.Y, 15, "O posto é na superfície, fora do formigueiro.");
            }

            Termite near = colony.SpawnTermite(85, 21); // dentro de 30 células da abertura
            TickFrozen(colony, 0.5f, near);
            foreach (Ant soldier in soldiers)
                Assert.IsFalse(soldier.OnGuard, "O cupim chegou perto: o grupo sai para caçar.");
        }

        [Test]
        public void GuardPosts_SplitTheSoldiersAcrossTheOpenings_AndEachGroupLeavesOnItsOwn()
        {
            AntColony colony = MakeColony(MakeWorld(secondOpening: true));
            Own(colony, "M2");
            var soldiers = new Ant[4];
            for (int i = 0; i < soldiers.Length; i++)
                soldiers[i] = colony.SpawnAt(AntRole.Soldier, 95, 2);
            HoldRaidOpen(colony, 118);

            Tick(colony, 15f);
            int[] perGroup = new int[2];
            foreach (Ant soldier in soldiers)
            {
                Assert.GreaterOrEqual(soldier.GuardGroup, 0);
                Assert.Less(soldier.GuardGroup, 2);
                perGroup[soldier.GuardGroup]++;
            }
            Assert.AreEqual(2, perGroup[0], "Metade dos soldados em cada abertura.");
            Assert.AreEqual(2, perGroup[1]);

            Termite west = colony.SpawnTermite(5, 21); // só perto da abertura do oeste (x 50)
            west.Health = 100000f;
            TickFrozen(colony, 0.3f, west);
            foreach (Ant soldier in soldiers)
            {
                if (soldier.GuardGroup == 0)
                    Assert.IsFalse(soldier.OnGuard, "O grupo da abertura do cupim sai.");
                else
                    Assert.IsTrue(soldier.OnGuard, "O outro grupo continua esperando.");
            }
        }

        [Test]
        public void WithoutTheUpgrade_SoldiersDoNotWaitAtTheOpenings()
        {
            AntColony colony = MakeColony(MakeWorld());
            Ant soldier = colony.SpawnAt(AntRole.Soldier, 95, 2);
            HoldRaidOpen(colony, 115);
            Tick(colony, 3f);
            Assert.IsFalse(soldier.OnGuard);
            Assert.AreNotEqual(AntState.Fighting, soldier.State, "Só há cupim voando: o soldado segue trabalhando.");
        }
    }
}
