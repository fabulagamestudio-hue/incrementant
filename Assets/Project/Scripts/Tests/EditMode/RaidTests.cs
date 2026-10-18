using System.IO;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Ataques de cupins, combate, corpos e área de resíduos (pedido do desenvolvedor em 2026-10-18).</summary>
    public sealed class RaidTests
    {
        const float Dt = 0.1f;
        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);

        /// <summary>
        /// Solo de terra até y = 20 (céu acima), corredor de x 10 a 100 com 3 linhas (y 1 a 3) e um poço em x 50..51 até a
        /// superfície. Berçário em x 20..40, sala de nutrientes em x 60..80 (canteiros só com terra em x 62 e 68), área de
        /// resíduos em x 10..16 e sala da rainha em x 88..98.
        /// </summary>
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
            for (int x = 20; x <= 40; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetNurseryRoom(x, y, true));
            for (int x = 60; x <= 80; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetNutrientRoom(x, y, true));
            for (int x = 10; x <= 16; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetWasteRoom(x, y, true));
            for (int x = 88; x <= 98; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true));
            foreach (int bx in new[] { 62, 68 })
            {
                grid.Set(bx, 1, CellMaterial.Spoil);
                Assert.IsTrue(grid.SetBed(bx, 1, BedStage.Soil));
            }
            return grid;
        }

        static AntColony MakeColony(PixelGrid grid, bool raids = true)
        {
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3) { DeathEnabled = false, RaidsEnabled = raids };
            Tick(colony, 1f);
            return colony;
        }

        static void Tick(AntColony colony, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
                colony.Tick(Dt);
        }

        static bool TickUntil(AntColony colony, System.Func<bool> condition, float maxSeconds)
        {
            for (float t = 0f; t < maxSeconds; t += Dt)
            {
                if (condition())
                    return true;
                colony.Tick(Dt);
            }
            return condition();
        }

        /// <summary>Tira todas as formigas (como se tivessem morrido em combate), para os cupins agirem sozinhos.</summary>
        static void RemoveAllAnts(AntColony colony)
        {
            foreach (Ant ant in colony.Ants)
                ant.Wounds = 1000f;
            colony.Tick(Dt);
            Assert.AreEqual(0, colony.Ants.Count);
        }

        static int CountMaterial(PixelGrid grid, CellMaterial material)
        {
            int n = 0;
            RectInt area = grid.Active;
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
                n += grid.Get(x, y) == material ? 1 : 0;
            return n;
        }

        [Test]
        public void Raids_OnlyStartAfterTheMinimumOfAnts_WithAWarningFirst_AndBring40PercentAsTermites()
        {
            AntColony colony = MakeColony(MakeWorld());
            Tick(colony, 400f);
            Assert.IsFalse(colony.RaidsUnlocked, "Com 6 formigas, nada de ataque.");
            Assert.AreEqual(RaidState.Calm, colony.RaidState);

            for (int i = 0; i < 9; i++)
                colony.Spawn(AntRole.Worker);
            colony.Tick(Dt);
            Assert.IsTrue(colony.RaidsUnlocked, "15 formigas liberam os ataques.");
            Assert.That(colony.SecondsToNextRaid, Is.InRange(59f, 300f), "O próximo vem entre 1 e 5 minutos.");

            Assert.IsTrue(TickUntil(colony, () => colony.RaidState == RaidState.Warning, 305f), "Primeiro vem o alerta.");
            Assert.AreEqual(0, colony.Termites.Count, "Durante o alerta ainda não há cupins.");
            Assert.IsTrue(TickUntil(colony, () => colony.RaidState == RaidState.Active, 10f));
            Assert.AreEqual(6, colony.Termites.Count, "40% de 15 formigas.");
            foreach (Termite t in colony.Termites)
                Assert.AreEqual(TermitePhase.Flying, t.Phase, "Chegam voando.");

            Assert.IsTrue(TickUntil(colony, () => colony.Termites.Count == 0 || colony.Termites[0].Phase != TermitePhase.Flying, 5f));
            Tick(colony, 3f);
            foreach (Ant ant in colony.Ants)
                Assert.IsFalse(ant.State == AntState.Fighting && ant.Wounds == 0f, "Sem soldado, ninguém sai caçando cupim.");
        }

        /// <summary>Colônia com o soldado liberado (o primeiro ataque já passou), sem ataque marcado para os próximos minutos.</summary>
        static AntColony MakeSoldierColony(PixelGrid grid)
        {
            AntColony colony = MakeColony(grid);
            colony.RestoreRaids(raidsUnlocked: true, raidsSurvived: 1, secondsToNextRaid: 10000f);
            colony.Tree.Restore(1UL << (int)QueenNode.S0);
            Assert.IsTrue(colony.Tree.IsRoleUnlocked(AntRole.Soldier));
            return colony;
        }

        [Test]
        public void OnlySoldiers_ChaseTheTermites_TheOthersKeepWorking()
        {
            AntColony colony = MakeSoldierColony(MakeWorld());
            Ant soldier = colony.Spawn(AntRole.Soldier);
            colony.SpawnTermite(98, 1); // na outra ponta do corredor, longe de todas
            bool soldierFought = false, workerChased = false;
            for (float t = 0f; t < 4f; t += Dt)
            {
                colony.Tick(Dt);
                soldierFought |= soldier.State == AntState.Fighting;
                foreach (Ant ant in colony.Ants)
                    workerChased |= ant.Role != AntRole.Soldier && ant.State == AntState.Fighting && ant.Wounds == 0f;
            }
            Assert.IsTrue(soldierFought, "O soldado larga o que fazia e vai atrás do cupim.");
            Assert.IsFalse(workerChased, "As outras só lutam quando são mordidas.");
        }

        [Test]
        public void AWorker_DefendsItselfWhenBitten_AndGoesBackToWorkAfterwards()
        {
            AntColony colony = MakeColony(MakeWorld());
            Ant worker = colony.Ants[0];
            Termite termite = colony.SpawnTermite(worker.X, worker.Y);
            Assert.IsTrue(TickUntil(colony, () => termite.Health < colony.RaidSettings.TermiteHealth, 5f),
                          "Mordida, a comum morde de volta.");
            Assert.Greater(worker.Wounds + colony.AntsKilledInCombat, 0f, "Ela só reagiu depois de levar a mordida.");
            Assert.IsTrue(TickUntil(colony, () => colony.LiveTermites == 0, 600f), "O cupim morre ou termina o ataque e vai embora.");
            Tick(colony, colony.RaidSettings.AttackSeconds + 0.2f); // a última mordida ainda segura a formiga por um ciclo
            foreach (Ant ant in colony.Ants)
                Assert.AreNotEqual(AntState.Fighting, ant.State, "Sem cupim, voltam ao trabalho.");
        }

        [Test]
        public void TheTermiteShare_StartsAt40Percent_AndItsCeilingGrowsTo200Percent()
        {
            AntColony colony = MakeColony(MakeWorld());
            Assert.AreEqual(0.4f, colony.TermiteCap, 1e-4f, "O primeiro ataque vem com 40%.");
            colony.RestoreRaids(true, 3, 10000f);
            Assert.AreEqual(1.0f, colony.TermiteCap, 1e-4f, "+20 pontos por ataque.");
            colony.RestoreRaids(true, 8, 10000f);
            Assert.AreEqual(2.0f, colony.TermiteCap, 1e-4f);
            colony.RestoreRaids(true, 30, 10000f);
            Assert.AreEqual(2.0f, colony.TermiteCap, 1e-4f, "Nunca passa de 200%.");

            colony.RestoreRaids(true, 5, 10000f);
            for (int i = 0; i < 14; i++)
                colony.Spawn(AntRole.Worker);
            int ants = colony.Ants.Count;
            colony.ForceRaid(skipWarning: true);
            Assert.That(colony.LastRaidShare, Is.InRange(0.4f, 1.4f), "Sorteado entre o mínimo e o teto do momento.");
            Assert.That(colony.Termites.Count, Is.InRange(Mathf.CeilToInt(ants * 0.4f), Mathf.CeilToInt(ants * 1.4f)));
        }

        [Test]
        public void AForcedRaid_KeepsItsWarning_WhenTheColonyReachesTheMinimumDuringIt()
        {
            AntColony colony = MakeColony(MakeWorld());
            colony.ForceRaid();
            for (int i = 0; i < 9; i++)
                colony.Spawn(AntRole.Worker);
            colony.Tick(Dt);
            Assert.IsTrue(colony.RaidsUnlocked);
            Assert.AreEqual(RaidState.Warning, colony.RaidState);
            Assert.LessOrEqual(colony.SecondsToNextRaid, 8f, "O alerta do F4 não vira o intervalo do próximo ataque.");
            Assert.IsTrue(TickUntil(colony, () => colony.RaidState == RaidState.Active, 9f));
        }

        [Test]
        public void RaidsNeverStart_WhenDisabled()
        {
            AntColony colony = MakeColony(MakeWorld(), raids: false);
            for (int i = 0; i < 20; i++)
                colony.Spawn(AntRole.Worker);
            Tick(colony, 400f);
            Assert.AreEqual(RaidState.Calm, colony.RaidState);
            Assert.AreEqual(0, colony.Termites.Count);
        }

        [Test]
        public void Termites_DestroyTheBrood_ThenTheBeds_ThenTheWalls_SparingTheQueenRoom_AndThenLeave()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            RemoveAllAnts(colony);
            colony.RestoreBrood(new[]
            {
                new BroodRecord { Role = AntRole.Worker, Stage = BroodStage.Larva, Remaining = 500f },
                new BroodRecord { Role = AntRole.Worker, Stage = BroodStage.Pupa, Remaining = 500f },
            }, 500f);
            colony.SpawnTermite(45, 1);
            colony.SpawnTermite(46, 1);
            colony.SpawnTermite(55, 1);

            Assert.IsTrue(TickUntil(colony, () => colony.Brood.Count == 0, 60f), "Primeiro as crias.");
            Assert.AreEqual(2, colony.BroodDestroyed);
            Assert.IsTrue(TickUntil(colony, () => colony.BedsDestroyed == 2, 60f), "Depois os canteiros.");
            Assert.AreEqual(0, grid.BedCount);
            Assert.IsTrue(TickUntil(colony, () => colony.WallsDestroyed > 10, 120f), "Depois as paredes dos setores.");
            Assert.Greater(CountMaterial(grid, CellMaterial.Rubble), 0, "Parede e canteiro destruídos viram entulho solto.");

            Assert.IsTrue(TickUntil(colony, () => colony.RaidState == RaidState.Calm, 600f), "No fim, vão embora.");
            Assert.AreEqual(0, colony.LiveTermites);
            Assert.AreEqual(1, colony.RaidsSurvived);
            Assert.IsTrue(colony.WasteUnlocked, "O primeiro ataque libera a área de resíduos.");
            Assert.IsTrue(colony.Tree.SoldiersAvailable, "E o soldado.");
            for (int x = 88; x <= 98; x++)
            for (int y = 4; y <= 5; y++)
                Assert.AreEqual(CellMaterial.Dirt, grid.Get(x, y), $"A sala da rainha fica de fora ({x}, {y}).");
        }

        [Test]
        public void TheAnts_KillATermite_AndTurnTheCorpseIntoLeavesForTheBeds_AndRemainsForTheWasteRoom()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeSoldierColony(grid);
            for (int i = 0; i < 3; i++)
                colony.Spawn(AntRole.Soldier);
            colony.SpawnTermite(30, 1);

            Assert.IsTrue(TickUntil(colony, () => colony.TermitesKilled == 1, 60f), "Os soldados matam o cupim.");
            Assert.IsTrue(TickUntil(colony, () => colony.Termites.Count == 0, 120f), "O corpo é desmembrado até o fim.");
            Assert.IsTrue(TickUntil(colony, () => colony.HarvestedPixels >= 2 && colony.WasteDeposited >= 1, 120f),
                          $"As 2 folhas vão aos canteiros (plantadas {colony.HarvestedPixels}) e o resto à área de resíduos ({colony.WasteDeposited}).");
            Assert.Greater(CountMaterial(grid, CellMaterial.Refuse), 0);
            foreach (Ant ant in colony.Ants)
                Assert.AreNotEqual(AntState.Fighting, ant.State, "Sem cupim, voltam ao trabalho.");

            Assert.IsTrue(TickUntil(colony, () => CountMaterial(grid, CellMaterial.Refuse) == 0, 130f),
                          "O resíduo se decompõe sozinho (120 s).");
        }

        [Test]
        public void AnAntCanDieInCombat_EvenWithDeathDisabled()
        {
            AntColony colony = MakeColony(MakeWorld());
            Assert.IsFalse(colony.DeathEnabled);
            int before = colony.Ants.Count;
            for (int i = 0; i < 8; i++)
                colony.SpawnTermite(colony.Ants[0].X, colony.Ants[0].Y);
            Assert.IsTrue(TickUntil(colony, () => colony.AntsKilledInCombat > 0, 60f));
            Assert.Less(colony.Ants.Count, before);
        }

        [Test]
        public void WasteRoomAndRaidState_SurviveSaveAndLoad()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            colony.RestoreRaids(raidsUnlocked: true, raidsSurvived: 2, secondsToNextRaid: 77f);
            var rain = new RainSimulation(new RainSettings(), 3);
            var water = new WaterSimulation(new WaterSettings(), grid, 3);
            GameSnapshot snapshot = GameSnapshot.Capture(grid, colony, rain, water, 0, 3, 10.0);
            var stream = new MemoryStream();
            snapshot.Write(stream);
            stream.Position = 0;
            GameSnapshot read = GameSnapshot.Read(stream);

            var loadedGrid = new PixelGrid(grid.Width, grid.Height);
            for (int x = 0; x < grid.Width; x++)
                loadedGrid.SetSurfaceY(x, grid.GetSurfaceY(x));
            read.ApplyTo(loadedGrid);
            var loaded = new AntColony(loadedGrid, new ColonySettings(), Layout, seed: 3);
            read.ApplyTo(loaded);

            Assert.IsTrue(loadedGrid.IsWasteRoom(12, 2));
            Assert.IsFalse(loadedGrid.IsNurseryRoom(12, 2));
            Assert.IsTrue(loadedGrid.IsNurseryRoom(25, 2));
            Assert.AreEqual(grid.WasteRoomCount, loadedGrid.WasteRoomCount);
            Assert.IsTrue(loaded.RaidsUnlocked);
            Assert.AreEqual(2, loaded.RaidsSurvived);
            Assert.AreEqual(77f, loaded.SecondsToNextRaid, 1e-3f);
            Assert.IsTrue(loaded.Tree.SoldiersAvailable);
        }
    }
}
