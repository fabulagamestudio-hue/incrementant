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
            // Mundo sem sala da rainha: uma sala fechada onde ela é posta, longe dos cupins (sem sala, ela vagaria até eles).
            if (grid.QueenRoomCount == 0)
            {
                for (int x = 93; x <= 97; x++)
                for (int y = 1; y <= 3; y++)
                {
                    grid.Set(x, y, CellMaterial.Empty);
                    Assert.IsTrue(grid.SetQueenRoom(x, y, true));
                }
            }
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3) { RaidsEnabled = raids };
            // Os testes antigos não tratam da caça à rainha (ela mora longe e morreria no meio da ordem dos objetivos).
            colony.RaidSettings.HuntQueen = false;
            // A rainha mora na sala dela, longe do caminho dos cupins (no corredor eles a morderiam e ela morreria no meio).
            colony.RestoreQueen(95, 2, 1);
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

        /// <summary>Itens guardados do material dado (resíduos e folhas da área de resíduos, desde 2026-10-19).</summary>
        static int CountStored(PixelGrid grid, CellMaterial material)
        {
            int n = 0;
            RectInt area = grid.Active;
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
                n += grid.GetStored(x, y) == StoredItems.Of(material) ? 1 : 0;
            return n;
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
            Assert.That(colony.SecondsToNextRaid, Is.InRange(119f, 600f), "O próximo vem entre 2 e 10 minutos.");

            Assert.IsTrue(TickUntil(colony, () => colony.RaidState == RaidState.Warning, 605f), "Primeiro vem o alerta.");
            Assert.AreEqual(0, colony.Termites.Count, "Durante o alerta ainda não há cupins.");
            Assert.IsTrue(TickUntil(colony, () => colony.RaidState == RaidState.Active, 10f));
            Assert.AreEqual(3, colony.Termites.Count, "20% de 15 formigas.");
            foreach (Termite t in colony.Termites)
                Assert.AreEqual(TermitePhase.Flying, t.Phase, "Chegam voando.");

            Assert.IsTrue(TickUntil(colony, () => colony.Termites.Count == 0 || colony.Termites[0].Phase != TermitePhase.Flying, 5f));
        }

        [Test]
        public void RaidInterval_IsInGameMinutes_SoFasterSpeedsDoNotShortenIt()
        {
            AntColony colony = MakeColony(MakeWorld());
            colony.RestoreRaids(raidsUnlocked: true, raidsSurvived: 1, secondsToNextRaid: 60f);
            for (int i = 0; i < 9; i++)
                colony.Spawn(AntRole.Worker);

            colony.GameSpeed = 8f;
            Tick(colony, 40f); // 40 s de simulação a 8x = 5 s de jogo
            Assert.AreEqual(RaidState.Calm, colony.RaidState);
            Assert.AreEqual(55f, colony.SecondsToNextRaid, 0.5f, "A 8x o relógio anda 1/8 da simulação.");

            colony.GameSpeed = 1f;
            Tick(colony, 56f);
            Assert.AreEqual(RaidState.Warning, colony.RaidState, "A 1x o minuto de jogo é o tempo da simulação.");
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
            colony.SpawnTermite(98, 21); // na superfície, fora do formigueiro e longe de todas
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
        public void HeavyRain_KillsEveryLiveTermite_AndEndsTheRaid()
        {
            AntColony colony = MakeColony(MakeWorld());
            Termite outside = colony.SpawnTermite(98, 21);
            Termite inside = colony.SpawnTermite(98, 1);
            colony.Tick(Dt);
            Assert.AreEqual(2, colony.LiveTermites, "Sem chuva eles continuam vivos.");

            colony.Raining = true;
            colony.Tick(Dt);
            Assert.IsFalse(outside.Alive);
            Assert.IsFalse(inside.Alive);
            Assert.AreEqual(0, colony.LiveTermites);
            Assert.AreEqual(2, colony.TermitesKilled);
            Assert.AreEqual(RaidState.Calm, colony.RaidState, "Sem cupins vivos o ataque acaba.");
        }

        [Test]
        public void NoRaidStartsWhileItRainsHeavily_ItComesWhenTheSkyClears()
        {
            AntColony colony = MakeColony(MakeWorld());
            for (int i = 0; i < 9; i++)
                colony.Spawn(AntRole.Worker);
            colony.RestoreRaids(true, 0, 0.5f);
            colony.Raining = true;
            Tick(colony, 10f);
            Assert.AreEqual(RaidState.Calm, colony.RaidState, "O alerta espera a chuva passar.");

            colony.Raining = false;
            Tick(colony, 1f);
            Assert.AreEqual(RaidState.Warning, colony.RaidState);
        }

        [Test]
        public void ATermiteInsideTheNest_MakesEveryAntHuntIt_AndTheyGoBackToWorkAfterwards()
        {
            AntColony colony = MakeSoldierColony(MakeWorld());
            Termite intruder = colony.SpawnTermite(98, 1); // dentro do formigueiro, longe das formigas
            bool workerChased = false;
            for (float t = 0f; t < 4f; t += Dt)
            {
                colony.Tick(Dt);
                foreach (Ant ant in colony.Ants)
                    workerChased |= ant.Role != AntRole.Soldier && ant.State == AntState.Fighting && ant.Wounds == 0f;
            }
            Assert.IsTrue(workerChased, "Com invasor dentro do formigueiro, todas largam o trabalho e vão atrás dele.");
            Assert.IsTrue(TickUntil(colony, () => !intruder.Alive, 120f), "As formigas matam o invasor.");
            Tick(colony, colony.RaidSettings.AttackSeconds + 0.2f);
            foreach (Ant ant in colony.Ants)
                Assert.AreNotEqual(AntState.Fighting, ant.State, "Sem invasor, voltam ao trabalho.");
        }

        [Test]
        public void TheQueen_BitesBackWithThreeTimesTheSoldiersDamage()
        {
            AntColony colony = MakeSoldierColony(MakeWorld());
            colony.RestoreState(0, 0, colony.SimulationTime, new Ant[0]); // sem formigas para morder no lugar dela
            Termite termite = colony.SpawnTermite(colony.Queen.X, colony.Queen.Y);
            termite.Health = 1000f;
            float soldier = colony.Tree.Damage(AntRole.Soldier) * colony.RaidSettings.AttackSeconds;
            Assert.AreEqual(9f, soldier * 3f, 1e-3f, "Soldado 3/s: a rainha dá 9 por mordida.");
            colony.Tick(Dt);
            Assert.LessOrEqual(termite.Health, 1000f - soldier * 3f, "Mordida de volta na hora em que é mordida.");
            Assert.Greater(colony.Queen.Wounds, 0f);
        }

        [Test]
        public void AnOffensiveQueen_LeavesToHuntAnIntruder_ADefensiveOneStays()
        {
            AntColony offensive = MakeColony(MakeWorld());
            offensive.QueenStance = QueenStance.Offensive;
            Termite far = offensive.SpawnTermite(offensive.Queen.X - 25, 1);
            far.Health = 1000f;
            Assert.IsTrue(TickUntil(offensive, () => Mathf.Abs(offensive.Queen.X - far.X) <= offensive.RaidSettings.AttackReach, 40f),
                          "A rainha ofensiva vai até o invasor.");

            AntColony defensive = MakeColony(MakeWorld());
            Termite other = defensive.SpawnTermite(defensive.Queen.X - 25, 1);
            other.Health = 1000f;
            Tick(defensive, 3f);
            Assert.GreaterOrEqual(defensive.Queen.X, 88, "A defensiva fica na sala dela, sem sair atrás do cupim.");
        }

        [Test]
        public void TheQueenDying_IsGameOver_AndTheSimulationStops()
        {
            AntColony colony = MakeColony(MakeWorld());
            // MakeColony desliga a caça à rainha; sem ela o cupim morde uma vez e segue para as paredes (KI-022).
            colony.RaidSettings.HuntQueen = true;
            colony.RestoreState(0, 0, colony.SimulationTime, new Ant[0]);
            Termite killer = colony.SpawnTermite(colony.Queen.X, colony.Queen.Y);
            killer.Health = 100000f;
            Assert.IsFalse(colony.GameOver);
            Assert.IsTrue(TickUntil(colony, () => colony.GameOver, 200f), "Mordida atrás de mordida, a rainha morre.");
            float time = colony.SimulationTime;
            Tick(colony, 1f);
            Assert.AreEqual(time, colony.SimulationTime, "Em game over a simulação para.");
        }

        [Test]
        public void TheFinger_HurtsTheTermiteNearTheClick()
        {
            AntColony colony = MakeColony(MakeWorld());
            Termite termite = colony.SpawnTermite(30, 2);
            Assert.IsFalse(colony.HitTermiteAt(60, 2), "Longe do cupim, o dedo erra.");
            Assert.IsTrue(colony.HitTermiteAt(31, 2));
            Assert.AreEqual(colony.RaidSettings.TermiteHealth - colony.RaidSettings.FingerDamage, termite.Health, 1e-3f);
            for (int i = 0; i < 6; i++)
                colony.HitTermiteAt(30, 2);
            Assert.IsFalse(termite.Alive, "Cliques sucessivos matam o cupim.");
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
        public void TheTermiteShare_StartsAt20Percent_AndItsCeilingGrowsSlowlyTo50Percent()
        {
            AntColony colony = MakeColony(MakeWorld());
            Assert.AreEqual(0.2f, colony.TermiteCap, 1e-4f, "O primeiro ataque vem com 20%.");
            colony.RestoreRaids(true, 3, 10000f);
            Assert.AreEqual(0.275f, colony.TermiteCap, 1e-4f, "+2,5 pontos por ataque.");
            colony.RestoreRaids(true, 12, 10000f);
            Assert.AreEqual(0.5f, colony.TermiteCap, 1e-4f);
            colony.RestoreRaids(true, 30, 10000f);
            Assert.AreEqual(0.5f, colony.TermiteCap, 1e-4f, "Nunca passa de 50%.");

            colony.RestoreRaids(true, 5, 10000f);
            for (int i = 0; i < 14; i++)
                colony.Spawn(AntRole.Worker);
            int ants = colony.Ants.Count;
            colony.ForceRaid(skipWarning: true);
            Assert.That(colony.LastRaidShare, Is.InRange(0.2f, 0.325f), "Sorteado entre o mínimo e o teto do momento.");
            Assert.That(colony.Termites.Count, Is.InRange(Mathf.CeilToInt(ants * 0.2f), Mathf.CeilToInt(ants * 0.325f)));
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
        public void Raids_WaitWhileTheColonyIsBelowTheMinimumOfAnts()
        {
            AntColony colony = MakeColony(MakeWorld());
            colony.RestoreRaids(true, 3, 1f); // desbloqueado, já com ataques passados e com o relógio quase no fim, mas com 6 formigas
            Tick(colony, 400f);
            Assert.AreEqual(RaidState.Calm, colony.RaidState, "Abaixo do mínimo, nenhum ataque vem.");
            Assert.AreEqual(0, colony.Termites.Count);

            for (int i = 0; i < 9; i++)
                colony.Spawn(AntRole.Worker);
            Assert.IsTrue(TickUntil(colony, () => colony.RaidState == RaidState.Warning, 5f), "Com o mínimo, o relógio vence.");
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
            Assert.AreEqual(1, CountStored(grid, CellMaterial.TermiteRemains), "O resto fica guardado na área como resto de cupim.");
            Assert.AreEqual(0, CountMaterial(grid, CellMaterial.TermiteRemains), "Guardado, ele não é parede.");
            foreach (Ant ant in colony.Ants)
                Assert.AreNotEqual(AntState.Fighting, ant.State, "Sem cupim, voltam ao trabalho.");

            Assert.IsTrue(TickUntil(colony, () => CountStored(grid, CellMaterial.TermiteRemains) == 0, 130f),
                          "O resto se decompõe sozinho (120 s).");
            Assert.AreEqual(1, colony.RemainsRotted, "E vira folhas na área de resíduos.");
        }

        /// <summary>
        /// Coloca resíduos na área (x 10..16, y 1..3) como material, como num save antigo, e recomeça a decomposição sem
        /// formigas: RescanWaste os troca por itens guardados.
        /// </summary>
        static AntColony MakeColonyWithWaste(PixelGrid grid, params (int x, int y, CellMaterial material)[] waste)
        {
            AntColony colony = MakeColony(grid);
            foreach (var (x, y, material) in waste)
                grid.Set(x, y, material);
            colony.RestoreState(0, 0, colony.SimulationTime, new Ant[0]);
            return colony;
        }

        [Test]
        public void WasteFromOldSaves_BecomesStoredItems_ThatDoNotBlockThePassage()
        {
            PixelGrid grid = MakeWorld();
            MakeColonyWithWaste(grid, (12, 1, CellMaterial.TermiteRemains), (15, 2, CellMaterial.Refuse));

            Assert.AreEqual(CellMaterial.Empty, grid.Get(12, 1), "O resto deixa de ser parede.");
            Assert.AreEqual(CellMaterial.Empty, grid.Get(15, 2));
            Assert.AreEqual(StoredItems.Of(CellMaterial.TermiteRemains), grid.GetStored(12, 1));
            Assert.AreEqual(StoredItems.Of(CellMaterial.Refuse), grid.GetStored(15, 2));
            Assert.IsTrue(AntNavigation.IsWalkable(grid, 12, 1), "A formiga passa por cima do resíduo guardado.");
        }

        [Test]
        public void TermiteRemains_RotIntoThreeStoredLeaves_InsideTheWasteRoom_WhileRubbleJustDisappears()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColonyWithWaste(grid, (12, 1, CellMaterial.TermiteRemains), (12, 2, CellMaterial.TermiteRemains),
                                                   (15, 1, CellMaterial.Refuse));

            Tick(colony, 119f);
            Assert.AreEqual(2, CountStored(grid, CellMaterial.TermiteRemains), "Antes de 120 s nada se decompõe.");
            Assert.AreEqual(0, CountStored(grid, CellMaterial.PlantLeaf));

            Tick(colony, 2f);
            Assert.AreEqual(0, CountStored(grid, CellMaterial.TermiteRemains));
            Assert.AreEqual(0, CountStored(grid, CellMaterial.Refuse), "O entulho só some.");
            Assert.AreEqual(2, colony.RemainsRotted);

            int leaves = 0;
            for (int x = 0; x < grid.Width; x++)
            for (int y = 1; y < grid.Height; y++)
            {
                if (grid.GetStored(x, y) != StoredItems.Of(CellMaterial.PlantLeaf))
                    continue;
                leaves++;
                Assert.IsTrue(grid.IsWasteRoom(x, y), $"A folha ({x}, {y}) fica guardada dentro da área de resíduos.");
                Assert.AreEqual(CellMaterial.Empty, grid.Get(x, y), "Sem virar parede.");
            }
            Assert.AreEqual(6, leaves, "3 folhas por resto de cupim.");
            Assert.AreEqual(0, CountMaterial(grid, CellMaterial.FallenLeaf), "Nenhuma folha solta caindo pela área.");
        }

        [Test]
        public void TermiteRemains_WaitForEmptyCells_InTheWasteRoom_BeforeRotting()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColonyWithWaste(grid, (12, 1, CellMaterial.TermiteRemains));
            // Área tomada (rocha em toda célula menos a do resto): sem lugar para as outras 2 folhas.
            for (int x = 10; x <= 16; x++)
            for (int y = 1; y <= 3; y++)
                if (x != 12 || y != 1)
                    grid.Set(x, y, CellMaterial.Rock);

            Tick(colony, 125f);
            Assert.AreEqual(1, CountStored(grid, CellMaterial.TermiteRemains), "Sem lugar, o resto espera.");
            Assert.AreEqual(0, colony.RemainsRotted);

            grid.Set(13, 1, CellMaterial.Empty);
            grid.Set(12, 2, CellMaterial.Empty);
            Tick(colony, 1.5f);
            Assert.AreEqual(0, CountStored(grid, CellMaterial.TermiteRemains), "Com lugar, vira folhas.");
            Assert.AreEqual(3, CountStored(grid, CellMaterial.PlantLeaf));
        }

        [Test]
        public void TermitesThatLandFarAway_StillDestroyTheBedsAndWalls_BeforeLeaving()
        {
            // Mapa largo: o cupim pousa a ~900 colunas da entrada e leva mais que Stall Seconds (30 s) só para chegar.
            var grid = new PixelGrid(1100, 40);
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
            for (int x = 60; x <= 80; x++)
            for (int y = 1; y <= 3; y++)
                grid.SetNutrientRoom(x, y, true);
            grid.Set(62, 1, CellMaterial.Spoil);
            grid.SetBed(62, 1, BedStage.Soil);

            AntColony colony = MakeColony(grid);
            RemoveAllAnts(colony);
            colony.SpawnTermite(950, 21);
            Assert.IsTrue(TickUntil(colony, () => colony.RaidState == RaidState.Calm, 600f));
            Assert.AreEqual(1, colony.BedsDestroyed, "Caminhar até o formigueiro não é ficar parado: o canteiro é destruído.");
            Assert.Greater(colony.WallsDestroyed, 0, "As paredes da sala também.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Termites_TearDownBothWallLayersAroundAClosedRoom_FloorIncluded(bool granular)
        {
            // Sala fechada de terra (x 40..55, y 8..11) no meio do solo, ligada à superfície por um poço em x 47..48.
            var grid = new PixelGrid(120, 40);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, 20);
                for (int y = 1; y <= 20; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int x = 40; x <= 55; x++)
            for (int y = 8; y <= 11; y++)
            {
                grid.Set(x, y, CellMaterial.Empty);
                grid.SetNutrientRoom(x, y, true);
            }
            for (int x = 47; x <= 48; x++)
            for (int y = 12; y <= 20; y++)
                grid.Set(x, y, CellMaterial.Empty);

            AntColony colony = MakeColony(grid);
            RemoveAllAnts(colony);
            colony.SpawnTermite(42, 8);
            colony.SpawnTermite(53, 8);
            colony.SpawnTermite(47, 11);
            Assert.IsTrue(TickUntil(colony, () => colony.RaidObjective == RaidObjective.Walls, 10f));
            Assert.IsTrue(TickUntil(colony, () => colony.WallTargetsLeft > 0, 2f));
            // Com a física granular (como no jogo) o entulho do teto cai na sala; sem ela, tudo fica onde foi derrubado.
            var simulation = new GranularSimulation();
            for (float t = 0f; t < 600f && colony.RaidObjective != RaidObjective.Leave; t += Dt)
            {
                if (granular)
                    simulation.Step(grid);
                colony.Tick(Dt);
            }
            Assert.AreEqual(RaidObjective.Leave, colony.RaidObjective);
            Assert.AreEqual(0, colony.WallTargetsLeft,
                            $"As duas camadas inteiras caem antes de irem embora (derrubadas {colony.WallsDestroyed})\n" + DumpRoom(grid));
        }

        [Test]
        public void WhenTheSectorIsLargerThanTheDugPart_TermitesTearDownItsInsideThenItsOuterWalls()
        {
            // Berçário pintado em x 30..64, y 4..12, mas só x 42..52, y 7..9 escavado (e um poço até a superfície).
            var grid = new PixelGrid(120, 40);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, 20);
                for (int y = 1; y <= 20; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int x = 30; x <= 64; x++)
            for (int y = 4; y <= 12; y++)
                grid.SetNurseryRoom(x, y, true);
            for (int x = 42; x <= 52; x++)
            for (int y = 7; y <= 9; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 47; x <= 48; x++)
            for (int y = 10; y <= 20; y++)
                grid.Set(x, y, CellMaterial.Empty);
            AntColony colony = MakeColony(grid);
            RemoveAllAnts(colony);
            for (int i = 0; i < 6; i++)
                colony.SpawnTermite(43 + 2 * i, 7);
            var simulation = new GranularSimulation();
            Assert.IsTrue(TickUntil(colony, () => colony.RaidObjective == RaidObjective.Sectors, 10f));
            Assert.IsTrue(TickUntil(colony, () => colony.SectorTargetsLeft > 0, 2f));
            int sectorTargets = colony.SectorTargetsLeft;
            int wallTargets = -1;
            bool wallsBeforeSectors = false;
            for (float t = 0f; t < 300f && colony.RaidObjective != RaidObjective.Leave; t += Dt)
            {
                simulation.Step(grid);
                colony.Tick(Dt);
                wallsBeforeSectors |= colony.WallsDestroyed > 0 && colony.SectorTargetsLeft > 0;
                if (wallTargets < 0 && colony.WallTargetsLeft > 0)
                    wallTargets = colony.WallTargetsLeft;
            }
            Assert.AreEqual(RaidObjective.Leave, colony.RaidObjective, "Terminam antes do tempo máximo do ataque.");
            Assert.AreEqual(0, colony.SectorTargetsLeft,
                            $"Todo bloco dentro da área pintada cai ({colony.SectorCellsDestroyed} de {sectorTargets}).");
            Assert.IsFalse(wallsBeforeSectors, "As paredes em volta só começam depois do interior inteiro.");
            Assert.AreEqual(0, colony.WallTargetsLeft,
                            $"As paredes em volta da área pintada inteira caem ({colony.WallsDestroyed} de {wallTargets}).");
            for (int x = 30; x <= 64; x++)
            for (int y = 4; y <= 12; y++)
                Assert.AreNotEqual(CellMaterial.Dirt, grid.Get(x, y), $"Nada de terra firme dentro do setor ({x}, {y}).");
            Assert.AreEqual(CellMaterial.Dirt, grid.Get(30, 15), "Fora das 2 camadas, nada é tocado.");
        }

        static string DumpRoom(PixelGrid grid)
        {
            var sb = new System.Text.StringBuilder();
            for (int y = 15; y >= 4; y--)
            {
                for (int x = 35; x <= 60; x++)
                {
                    CellMaterial m = grid.Get(x, y);
                    sb.Append(m == CellMaterial.Empty ? '.' : m == CellMaterial.Rubble ? 'r' : '#');
                }
                sb.Append(' ').Append(y).Append('\n');
            }
            return sb.ToString();
        }

        [Test]
        public void AnAntCanDieInCombat_EvenWithDeathDisabled()
        {
            AntColony colony = MakeColony(MakeWorld());
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
            colony.QueenStance = QueenStance.Offensive;
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
            Assert.AreEqual(QueenStance.Offensive, loaded.QueenStance);
        }
    }
}
