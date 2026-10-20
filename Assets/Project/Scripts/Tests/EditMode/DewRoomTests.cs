using System;
using System.IO;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Sala Gotas de Orvalho (pedido do desenvolvedor em 2026-10-18): as formigas cobrem de terra fofa os 30% de cima da sala,
    /// depois põem folha, e a sala pronta solta uma gota a cada X segundos, que as paredes dela não absorvem.
    /// </summary>
    public sealed class DewRoomTests
    {
        const float Dt = 1f / 60f;
        const int Surface = 10;
        // Sala de 8 colunas (x 30..37) por 7 linhas (y 1..7): 30% de 7 células = 3 linhas de terra fofa (y 5..7).
        const int RoomX0 = 30, RoomX1 = 37, RoomY1 = 7;

        static TerrainLayout Layout => new TerrainLayout(20, new Vector2Int(20, 1), 50, 55);

        /// <summary>Solo de terra até a superfície, piso de rocha, corredor (y 1..3), poço em x = 20 e a sala escavada e pintada.</summary>
        static PixelGrid MakeWorld(int roomTop = RoomY1, bool withPile = true)
        {
            const int width = 80;
            var grid = new PixelGrid(width, 30);
            for (int x = 0; x < width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, Surface);
                for (int y = 1; y <= Surface; y++)
                    grid.Set(x, y, y == Surface ? CellMaterial.Grass : CellMaterial.Dirt);
            }
            for (int x = 2; x < width - 2; x++)
            for (int y = 1; y <= 3; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int y = 1; y <= Surface; y++)
                grid.Set(20, y, CellMaterial.Empty);
            for (int x = RoomX0; x <= RoomX1; x++)
            for (int y = 1; y <= roomTop; y++)
            {
                grid.Set(x, y, CellMaterial.Empty);
                Assert.IsTrue(grid.SetDewRoom(x, y, true), $"({x},{y}) deveria aceitar a sala.");
            }
            if (withPile)
            {
                // A terra fofa vem da pilha de fora (terra despejada sobre a superfície).
                for (int x = 50; x <= 60; x++)
                for (int y = Surface + 1; y <= Surface + 3; y++)
                    grid.Set(x, y, CellMaterial.Spoil);
            }
            return grid;
        }

        static int BandRows(PixelGrid grid, CellMaterial material, int x)
        {
            int count = 0;
            for (int y = 1; y <= RoomY1; y++)
                count += grid.Get(x, y) == material ? 1 : 0;
            return count;
        }

        static void GiveLeaves(AntColony colony, ref int pending)
        {
            foreach (Ant ant in colony.Ants)
            {
                if (pending == 0)
                    return;
                if (ant.CarriedCount != 0 || ant.State != AntState.Idle)
                    continue;
                ant.PickUp(CellMaterial.PlantLeaf, 128);
                ant.State = AntState.Carrying;
                pending--;
            }
        }

        [Test]
        public void DewRoom_OnlyPaintsOnAnthillCells_AndNeverOverAnotherRoom()
        {
            var grid = new PixelGrid(40, 30);
            for (int x = 0; x < 40; x++)
                grid.SetSurfaceY(x, Surface);
            Assert.IsTrue(grid.SetDewRoom(5, 3, true));
            Assert.IsFalse(grid.SetDewRoom(5, 3, true), "Já pintada.");
            Assert.AreEqual(1, grid.DewRoomCount);
            Assert.IsFalse(grid.SetDewRoom(5, Surface + 2, true), "O céu não é sala.");
            Assert.IsTrue(grid.SetNutrientRoom(6, 3, true));
            Assert.IsFalse(grid.SetDewRoom(6, 3, true), "Não pinta por cima da sala de nutrientes.");
            Assert.IsFalse(grid.SetNurseryRoom(5, 3, true), "Nem o berçário por cima dela.");
            Assert.IsFalse(grid.SetQueenRoom(5, 3, true));
            Assert.IsFalse(grid.SetWasteRoom(5, 3, true));
            Assert.IsFalse(grid.SetNutrientRoom(5, 3, true));
            Assert.IsTrue(grid.SetDewRoom(5, 3, false));
            Assert.AreEqual(0, grid.DewRoomCount);
            Assert.AreEqual(0, grid.GetChunkDewRoomCount(0, 0));
        }

        [Test]
        public void LooseSoilInTheRoom_StaysWhereItIs_ButFallsOutside()
        {
            PixelGrid grid = MakeWorld(withPile: false);
            grid.Set(33, 5, CellMaterial.Spoil);   // dentro da sala, no ar
            grid.Set(45, 3, CellMaterial.Spoil);   // fora dela, no ar do corredor
            var granular = new GranularSimulation();
            for (int i = 0; i < 20; i++)
                granular.Step(grid);
            Assert.AreEqual(CellMaterial.Spoil, grid.Get(33, 5), "Dentro da sala a terra fofa fica presa.");
            Assert.AreEqual(CellMaterial.Spoil, grid.Get(45, 1), "Fora dela a terra cai até o chão.");
        }

        [Test]
        public void Water_InsideTheRoom_IsNotAbsorbedByItsWalls_ButOutsideItIs()
        {
            // Duas cavidades iguais debaixo do solo, cada uma com uma gota parada em cima de terra solta; só a primeira é sala.
            var grid = new PixelGrid(40, 30);
            for (int x = 0; x < 40; x++)
            {
                grid.SetSurfaceY(x, 20);
                for (int y = 0; y <= 20; y++)
                    grid.Set(x, y, y == 0 ? CellMaterial.Rock : CellMaterial.Dirt);
            }
            for (int x = 5; x <= 8; x++)
            for (int y = 1; y <= 2; y++)
            {
                grid.Set(x, y, CellMaterial.Empty);
                grid.Set(x + 20, y, CellMaterial.Empty);
                grid.SetDewRoom(x, y, true);
            }
            grid.SetWater(5, 1, true);   // encostada na parede de terra
            grid.SetWater(25, 1, true);
            var water = new WaterSimulation(new WaterSettings(), grid, 1);
            for (int i = 0; i < 60 * 30; i++)
                water.Step(grid, Dt);

            Assert.IsTrue(grid.HasWater(5, 1), "A gota da sala continua lá: as paredes dela não absorvem.");
            Assert.IsFalse(grid.HasWater(25, 1), "A mesma gota numa cavidade comum é absorvida pela terra.");
            Assert.AreEqual(1, water.ErodedCells + water.AbsorbedCells);
        }

        [Test]
        public void Columns_ShorterThanTheMinimumHeight_ReceiveNothing()
        {
            PixelGrid grid = MakeWorld(roomTop: 3);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            var granular = new GranularSimulation();
            for (int i = 0; i < 60 * 20; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
            }
            Assert.AreEqual(0, colony.DewSoilSpots, "Com 3 de altura a coluna fecharia a passagem.");
            Assert.IsFalse(colony.DewActive, "Sem terra fofa posta a sala não solta gota.");
            for (int x = RoomX0; x <= RoomX1; x++)
                Assert.AreEqual(0, BandRows(grid, CellMaterial.Spoil, x));
        }

        /// <summary>O mundo da sala de orvalho mais uma sala de nutrientes (x 42..52, y 1..3) que disputa a mesma terra da pilha.</summary>
        static PixelGrid MakeSharedWorld()
        {
            PixelGrid grid = MakeWorld();
            for (int x = 42; x <= 52; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetNutrientRoom(x, y, true));
            return grid;
        }

        static int DewSoilPlaced(PixelGrid grid)
        {
            int count = 0;
            for (int x = RoomX0; x <= RoomX1; x++)
                count += BandRows(grid, CellMaterial.Spoil, x);
            return count;
        }

        [Test]
        public void SharedSoil_IsSplitBetweenTheBedsAndTheDewRoom_NotGivenToTheBedsFirst()
        {
            PixelGrid grid = MakeSharedWorld();
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            var granular = new GranularSimulation();
            int dewWhenThirdBed = -1;
            for (int i = 0; i < 60 * 240 && dewWhenThirdBed < 0; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
                if (grid.BedCount >= 3)
                    dewWhenThirdBed = DewSoilPlaced(grid);
            }
            Assert.GreaterOrEqual(grid.BedCount, 3, "Os canteiros recebem terra.");
            Assert.GreaterOrEqual(dewWhenThirdBed, 1, "A sala de orvalho já tinha recebido terra antes do terceiro canteiro.");
        }

        [Test]
        public void SharedPlants_GoToTheRoomWithFewerPlantsPlaced()
        {
            // Dois canteiros já plantados e nenhuma folha: a próxima planta vai para a sala de orvalho, que está pedindo folha.
            PixelGrid grid = MakeSharedWorld();
            for (int x = RoomX0; x <= RoomX1; x++)
            for (int y = RoomY1 - 2; y <= RoomY1; y++)
            {
                grid.Set(x, y, CellMaterial.Spoil);
            }
            foreach (int bx in new[] { 44, 48 })
            {
                grid.Set(bx, 1, CellMaterial.Spoil);
                Assert.IsTrue(grid.SetBed(bx, 1, BedStage.Planted));
            }
            grid.Set(50, 1, CellMaterial.Spoil);
            Assert.IsTrue(grid.SetBed(50, 1, BedStage.Soil));
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            for (int i = 0; i < 60; i++)
                colony.Tick(Dt);
            Assert.AreEqual(1, colony.BedsAwaitingPlant, "O canteiro de terra pede planta.");
            Assert.Greater(colony.DewLeafSpots, 0, "A sala de orvalho pede folha.");

            int leaves = 1;
            GiveLeaves(colony, ref leaves);
            for (int i = 0; i < 60 * 60 && BandLeaves(grid) == 0 && grid.GetBedStage(50, 1) == BedStage.Soil; i++)
                colony.Tick(Dt);
            Assert.AreEqual(BedStage.Soil, grid.GetBedStage(50, 1), "O canteiro (2 plantas) fica esperando.");
            Assert.AreEqual(1, BandLeaves(grid), "A folha foi para a sala de orvalho (0 plantas).");
        }

        static int BandLeaves(PixelGrid grid)
        {
            int count = 0;
            for (int x = RoomX0; x <= RoomX1; x++)
                count += BandRows(grid, CellMaterial.PlantLeaf, x);
            return count;
        }

        [Test]
        public void Ants_CoverTheTopThirdWithSoil_ThenHangLeaves_ThenTheRoomDrips()
        {
            PixelGrid grid = MakeWorld();
            var settings = new ColonySettings();
            var colony = new AntColony(grid, settings, Layout, seed: 4);
            var granular = new GranularSimulation();
            var water = new WaterSimulation(new WaterSettings(), grid, 4);
            int leavesToGive = 0, leavesGivenCount = 0;
            bool leavesGiven = false, sawDewSoil = false;
            int firstActive = -1;
            for (int i = 0; i < 60 * 240 && colony.DewDrops < 2; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
                water.Step(grid, Dt);
                foreach (Ant ant in colony.Ants)
                    sawDewSoil |= ant.DewJob && ant.State == AntState.BringingSoil;
                if (!colony.DewActive)
                    Assert.AreEqual(0, colony.DewDrops, "Nenhuma gota antes de terminar as duas etapas.");
                else if (firstActive < 0)
                    firstActive = i;
                if (colony.DewSoilSpots > 0)
                    Assert.AreEqual(0, colony.DewLeafSpots, "A folha só começa com a terra toda posta.");
                if (!leavesGiven && colony.DewSoilSpots == 0 && colony.DewLeafSpots > 0)
                {
                    leavesToGive = leavesGivenCount = colony.DewLeafSpots;
                    leavesGiven = true;
                }
                if (leavesToGive > 0)
                    GiveLeaves(colony, ref leavesToGive);
            }

            Assert.IsTrue(sawDewSoil, "As formigas levaram terra fofa à sala.");
            Assert.IsTrue(leavesGiven, "Terminada a terra fofa, a sala pediu folhas.");
            for (int x = RoomX0; x <= RoomX1; x++)
            {
                Assert.AreEqual(3, BandRows(grid, CellMaterial.Spoil, x), $"Coluna {x}: 30% de 7 células é 3 linhas de terra fofa.");
                Assert.AreEqual(CellMaterial.Spoil, grid.Get(x, RoomY1), "A terra cobre o teto e desce dele.");
                Assert.AreEqual((x & 1) == 0 ? 1 : 0, BandRows(grid, CellMaterial.PlantLeaf, x), $"Coluna {x}: uma folha a cada duas colunas (soltas: {leavesGivenCount}).");
            }
            Assert.IsTrue(colony.DewActive);
            Assert.GreaterOrEqual(colony.DewDrops, 2, "A sala pronta solta gotas.");
            Assert.Less(firstActive, 60 * 240);
            Assert.AreEqual(0, water.AbsorbedCells + water.ErodedCells, "As paredes da sala não absorvem a gota.");
            Assert.GreaterOrEqual(grid.WaterCount, 2, "A água da gota continua na sala.");
            // A passagem continua aberta: dá para andar do corredor até o fundo da sala.
            var field = new FlowField(grid.Width, grid.Height);
            field.Build(grid, new System.Collections.Generic.List<int> { grid.ToIndex(25, 1) });
            for (int x = RoomX0; x <= RoomX1; x++)
                Assert.AreNotEqual(FlowField.Unreachable, field.Get(x, 1), $"A sala ficou fechada em x = {x}.");
        }

        /// <summary>
        /// Relato de 2026-10-19: a terra fofa do teto ia de uma em uma formiga. Como na escavação, cada formiga com terra reserva
        /// um ponto livre e as outras vão a outro: várias trabalham ao mesmo tempo e nunca duas no mesmo ponto.
        /// </summary>
        [Test]
        public void SoilCarriers_ReserveOneSpotEach_AndWorkTheCeilingInParallel()
        {
            PixelGrid grid = MakeWorld(withPile: false);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4) { RaidsEnabled = false };
            for (int i = 0; i < 6; i++)
                colony.SpawnAt(AntRole.Worker, 24 + i, 1);
            colony.Tick(Dt);
            Assert.AreEqual(RoomX1 - RoomX0 + 1, colony.DewSoilSpots, "Um ponto de terra por coluna da sala.");
            foreach (Ant ant in colony.Ants)
            {
                ant.PickUp(CellMaterial.Dirt, 128);
                ant.DewJob = true;
                ant.State = AntState.BringingSoil;
            }

            int mostReserved = 0;
            for (int i = 0; i < 60 * 30 && DewSoilPlaced(grid) < 6; i++)
            {
                colony.Tick(Dt);
                var reserved = new System.Collections.Generic.HashSet<int>();
                foreach (Ant ant in colony.Ants)
                {
                    if (ant.State != AntState.BringingSoil || !ant.DewJob || ant.SeekTarget < 0)
                        continue;
                    Assert.IsTrue(reserved.Add(ant.SeekTarget), $"Duas formigas reservaram o mesmo ponto no passo {i}.");
                }
                mostReserved = Mathf.Max(mostReserved, reserved.Count);
            }

            Assert.GreaterOrEqual(mostReserved, 4, "Várias formigas levam terra ao teto ao mesmo tempo.");
            Assert.AreEqual(6, DewSoilPlaced(grid), "Cada formiga pôs a terra dela.");
        }

        [Test]
        public void DropInterval_ComesFromTheSettings_AndWaitsWhenTheRoomIsFull()
        {
            PixelGrid grid = MakeWorld(withPile: false);
            // Terra fofa já posta nas 3 linhas de cima; folha nas colunas pares: a sala nasce pronta.
            for (int x = RoomX0; x <= RoomX1; x++)
            {
                for (int y = RoomY1 - 2; y <= RoomY1; y++)
                    grid.Set(x, y, CellMaterial.Spoil);
                if ((x & 1) == 0)
                    grid.Set(x, RoomY1 - 3, CellMaterial.PlantLeaf);
            }
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            var granular = new GranularSimulation();
            var water = new WaterSimulation(new WaterSettings(), grid, 4);
            int elapsed = 0;
            while (!colony.DewActive && elapsed < 60 * 10)
            {
                colony.Tick(Dt);
                elapsed++;
            }
            Assert.IsTrue(colony.DewActive, "Sala com as duas etapas feitas fica pronta.");
            int start = elapsed;
            // Intervalo padrão de 4 s: em 22 s caem 5 gotas.
            for (int i = 0; i < 60 * 22; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
                water.Step(grid, Dt);
            }
            Assert.AreEqual(5, colony.DewDrops, $"Cinco gotas em 22 s com intervalo de 4 s (começou no passo {start}).");
            for (int x = RoomX0; x <= RoomX1; x++)
            for (int y = 1; y <= RoomY1; y++)
            {
                if (grid.HasWater(x, y))
                    Assert.IsFalse(grid.IsSolid(x, y), "A gota nasce numa célula livre da sala.");
            }
        }

        static PixelGrid MakeReadyWorld()
        {
            PixelGrid grid = MakeWorld(withPile: false);
            for (int x = RoomX0; x <= RoomX1; x++)
            {
                for (int y = RoomY1 - 2; y <= RoomY1; y++)
                    grid.Set(x, y, CellMaterial.Spoil);
                if ((x & 1) == 0)
                    grid.Set(x, RoomY1 - 3, CellMaterial.PlantLeaf);
            }
            return grid;
        }

        /// <summary>Gotas em 40 s numa sala pronta; sem água acumulando de mais (a sala não enche). Devolve também se alguma jardineira ficou cuidando.</summary>
        static int DropsIn40Seconds(AntRole role, out bool tended)
        {
            PixelGrid grid = MakeReadyWorld();
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            foreach (Ant ant in colony.Ants)
                ant.Role = role;
            var granular = new GranularSimulation();
            var water = new WaterSimulation(new WaterSettings(), grid, 4);
            for (int i = 0; i < 60 * 5 && !colony.DewActive; i++)
                colony.Tick(Dt);
            Assert.IsTrue(colony.DewActive);
            int before = colony.DewDrops;
            tended = false;
            for (int i = 0; i < 60 * 40; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
                water.Step(grid, Dt);
                foreach (Ant ant in colony.Ants)
                    tended |= ant.State == AntState.TendingDew && ant.Interacting;
            }
            return colony.DewDrops - before;
        }

        [Test]
        public void Gardeners_TendingTheReadyRoom_SpeedUpTheDrops()
        {
            int plain = DropsIn40Seconds(AntRole.Worker, out bool workersTended);
            int tended = DropsIn40Seconds(AntRole.Gardener, out bool gardenersTended);
            Assert.IsFalse(workersTended, "Só a jardineira cuida da sala pronta.");
            Assert.IsTrue(gardenersTended, "A jardineira ociosa fica ao lado da queda das gotas.");
            Assert.Greater(tended, plain, "Com jardineiras cuidando, caem mais gotas no mesmo tempo.");
        }

        [Test]
        public void Gardeners_TendOneBlockEach_NoMoreThanTheRoomHas()
        {
            PixelGrid grid = MakeReadyWorld();
            var settings = new ColonySettings();
            var colony = new AntColony(grid, settings, Layout, seed: 4);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Gardener;
            for (int i = 0; i < 60 * 30; i++)
                colony.Tick(Dt);
            // Sala de 8 colunas, 4 pontos por bloco: 2 blocos, uma jardineira em cada.
            int blocks = (RoomX1 - RoomX0 + 1 + settings.DewPointsPerTender - 1) / settings.DewPointsPerTender;
            var targets = new System.Collections.Generic.HashSet<int>();
            int tenders = 0;
            foreach (Ant ant in colony.Ants)
            {
                if (ant.State != AntState.TendingDew)
                    continue;
                tenders++;
                Assert.IsTrue(targets.Add(ant.EggId), "Duas jardineiras não cuidam do mesmo bloco.");
            }
            Assert.AreEqual(blocks, tenders, "Uma jardineira por bloco da sala.");
        }

        [Test]
        public void Gardeners_AlsoCoverTheTopWithSoil()
        {
            PixelGrid grid = MakeWorld();
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Gardener;
            var granular = new GranularSimulation();
            bool sawGardenerBringing = false;
            for (int i = 0; i < 60 * 240 && colony.DewSoilSpots > 0 || i < 5; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
                foreach (Ant ant in colony.Ants)
                    sawGardenerBringing |= ant.DewJob && ant.Role == AntRole.Gardener && ant.State == AntState.BringingSoil;
            }
            Assert.IsTrue(sawGardenerBringing, "A jardineira levou terra fofa à sala.");
            Assert.AreEqual(0, colony.DewSoilSpots);
            Assert.AreEqual(3, BandRows(grid, CellMaterial.Spoil, 33));
        }

        [Test]
        public void Snapshot_RoundTripsTheDewRoom()
        {
            PixelGrid grid = MakeWorld(withPile: false);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3);
            var rain = new RainSimulation(new RainSettings(), 3);
            var water = new WaterSimulation(new WaterSettings(), grid, 3);
            GameSnapshot saved = GameSnapshot.Capture(grid, colony, rain, water, 0, 3, 1.0);
            var stream = new MemoryStream();
            saved.Write(stream);
            stream.Position = 0;
            GameSnapshot loaded = GameSnapshot.Read(stream);

            PixelGrid grid2 = MakeWorld(withPile: false);
            for (int x = RoomX0; x <= RoomX1; x++)
            for (int y = 1; y <= RoomY1; y++)
                grid2.SetDewRoom(x, y, false);
            loaded.ApplyTo(grid2);
            Assert.AreEqual(grid.DewRoomCount, grid2.DewRoomCount);
            Assert.IsTrue(grid2.IsDewRoom(RoomX0, 1) && grid2.IsDewRoom(RoomX1, RoomY1));
            Assert.IsFalse(grid2.IsNurseryRoom(RoomX0, 1) || grid2.IsWasteRoom(RoomX0, 1), "O bit novo não vaza para as outras salas.");

            // Save de antes da sala: o byte só tem os bits do berçário e do resíduo, e a sala volta vazia.
            for (int i = 0; i < loaded.NurseryRoom.Length; i++)
                loaded.NurseryRoom[i] &= 3;
            PixelGrid grid3 = MakeWorld(withPile: false);
            loaded.ApplyTo(grid3);
            Assert.AreEqual(0, grid3.DewRoomCount, "Um save sem o bit não pinta sala nenhuma.");
        }

        [Test]
        public void GrowingTheRoomDownward_ReleasesTheLeafThatIsNowInsideTheSoilBand_AndStopsTheDrops()
        {
            // Sala de y 3..7 (altura 5): faixa de terra y 6..7, folha em y 5 nas colunas pares.
            PixelGrid grid = MakeWorld(withPile: false);
            for (int x = RoomX0; x <= RoomX1; x++)
            {
                grid.SetDewRoom(x, 1, false);
                grid.SetDewRoom(x, 2, false);
                grid.Set(x, 6, CellMaterial.Spoil);
                grid.Set(x, 7, CellMaterial.Spoil);
                if ((x & 1) == 0)
                    grid.Set(x, 5, CellMaterial.PlantLeaf);
            }
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            for (int i = 0; i < 60 * 5; i++)
                colony.Tick(Dt);
            Assert.IsTrue(colony.DewActive, "A sala pronta solta gotas.");

            // O jogador aumenta a sala para baixo: altura 7, faixa de 3 linhas (y 5..7), e a folha de y 5 fica dentro dela.
            for (int x = RoomX0; x <= RoomX1; x++)
            {
                grid.SetDewRoom(x, 1, true);
                grid.SetDewRoom(x, 2, true);
            }
            for (int i = 0; i < 60 * 2; i++)
                colony.Tick(Dt);
            Assert.IsFalse(colony.DewActive, "Sala fora da forma não solta gota.");
            Assert.AreNotEqual(CellMaterial.PlantLeaf, grid.Get(RoomX0, 5), "A folha velha foi solta para ser reaproveitada.");
            Assert.Greater(grid.PlantDebrisCount, 0);
        }

        [Test]
        public void ShrinkingTheRoom_MarksTheLeftoverSoilForTheAntsToDig()
        {
            // Sala de altura 7 com a faixa de 3 linhas (y 5..7) pronta; o jogador apaga as 2 linhas de baixo: faixa de 2 (y 6..7).
            PixelGrid grid = MakeWorld(withPile: false);
            for (int x = RoomX0; x <= RoomX1; x++)
            for (int y = 5; y <= 7; y++)
                grid.Set(x, y, CellMaterial.Spoil);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            for (int i = 0; i < 60 * 3; i++)
                colony.Tick(Dt);
            for (int x = RoomX0; x <= RoomX1; x++)
            {
                grid.SetDewRoom(x, 1, false);
                grid.SetDewRoom(x, 2, false);
            }
            for (int i = 0; i < 60 * 3; i++)
                colony.Tick(Dt);
            Assert.IsTrue(grid.IsMarked(RoomX0, 5) || grid.Get(RoomX0, 5) != CellMaterial.Spoil,
                "A terra que sobrou abaixo da faixa é marcada para as formigas a recolherem.");
            Assert.IsFalse(colony.DewActive);
        }

        [Test]
        public void ErasingTheNutrientRoom_ResetsTheBedToPlainSoil_AndPlantInHandIsDropped()
        {
            PixelGrid grid = MakeWorld(withPile: false);
            // Sem sala de orvalho: ela também pediria planta (folha) e a formiga esperaria por ela.
            for (int x = RoomX0; x <= RoomX1; x++)
            for (int y = 1; y <= RoomY1; y++)
                grid.SetDewRoom(x, y, false);
            for (int x = 42; x <= 52; x++)
            for (int y = 1; y <= 3; y++)
                grid.SetNutrientRoom(x, y, true);
            grid.Set(45, 1, CellMaterial.Spoil);
            grid.SetBed(45, 1, BedStage.Growing, 30f);
            Assert.AreEqual(1, grid.GrowingBedCount);
            for (int x = 42; x <= 52; x++)
            for (int y = 1; y <= 3; y++)
                grid.SetNutrientRoom(x, y, false);
            Assert.AreEqual(BedStage.Soil, grid.GetBedStage(45, 1), "Fora da sala o canteiro perde planta e água.");
            Assert.AreEqual(0, grid.GrowingBedCount);

            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            int pending = 1;
            GiveLeaves(colony, ref pending);
            for (int i = 0; i < 60 * 10; i++)
                colony.Tick(Dt);
            Assert.IsTrue(grid.IsMarked(45, 1) || grid.Get(45, 1) != CellMaterial.Spoil, "A terra do canteiro solto é recolhida.");
            foreach (Ant ant in colony.Ants)
                Assert.IsFalse(CellMaterials.IsPlant(ant.Carried), "Ninguém fica preso com a planta sem sala para guardá-la.");
        }
    }
}
