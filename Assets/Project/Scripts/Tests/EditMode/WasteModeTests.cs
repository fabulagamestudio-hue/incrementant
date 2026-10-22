using System.IO;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Modos das áreas de resíduos (KI-042: folhas → água; KI-045: terra → areia + água; 2026-10-20). Desde 2026-10-21 cada
    /// área tem o próprio modo, posto pelo pincel, e recebe só o conteúdo dele.
    /// </summary>
    public sealed class WasteModeTests
    {
        const float Dt = 0.1f;
        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);

        /// <summary>
        /// Solo de terra até y = 20, corredor de x 10 a 100 com 3 linhas (y 1 a 3), poço em x 50..51 até a superfície, área
        /// de resíduos em x 10..16 com o modo dado e sala da rainha em x 88..98. Sem sala de nutrientes nem depósitos.
        /// </summary>
        static PixelGrid MakeWorld(WasteMode mode)
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
            PaintArea(grid, 10, 16, mode);
            for (int x = 88; x <= 98; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true));
            return grid;
        }

        static void PaintArea(PixelGrid grid, int x0, int x1, WasteMode mode)
        {
            for (int x = x0; x <= x1; x++)
            for (int y = 1; y <= 3; y++)
                grid.SetWasteRoom(x, y, true, (byte)mode);
        }

        /// <summary>Colônia sem formigas (só a rainha, longe), com os itens dados guardados e a lista dos resíduos refeita.</summary>
        static AntColony MakeColony(PixelGrid grid, params (int x, int y, CellMaterial material)[] items)
        {
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3) { RaidsEnabled = false };
            colony.RestoreQueen(95, 2, 1);
            foreach (var (x, y, material) in items)
                Assert.IsTrue(grid.SetStored(x, y, StoredItems.Of(material)));
            colony.RestoreState(0, 0, 0f, new Ant[0]);
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

        static int CountStored(PixelGrid grid, byte item, bool onlyWasteRoom = false)
        {
            int n = 0;
            RectInt area = grid.Active;
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
                n += grid.GetStored(x, y) == item && (!onlyWasteRoom || grid.IsWasteRoom(x, y)) ? 1 : 0;
            return n;
        }

        [Test]
        public void LeavesToWater_TheLeavesFromTermiteRemains_TurnIntoWaterInsideTheArea()
        {
            PixelGrid grid = MakeWorld(WasteMode.LeavesToWater);
            AntColony colony = MakeColony(grid, (12, 1, CellMaterial.TermiteRemains));
            byte leaf = StoredItems.Of(CellMaterial.PlantLeaf);

            Tick(colony, 121f);
            Assert.AreEqual(1, colony.RemainsRotted);
            Assert.AreEqual(3, CountStored(grid, leaf, onlyWasteRoom: true));
            Assert.AreEqual(3, colony.WasteCells, "As folhas passam a ser acompanhadas para virar água.");

            Tick(colony, colony.RaidSettings.LeafWaterSeconds + 1f);
            Assert.AreEqual(0, CountStored(grid, leaf), "Nenhuma folha sobra.");
            Assert.AreEqual(3, CountStored(grid, StoredItems.Water, onlyWasteRoom: true), "1 folha, 1 água, no mesmo lugar.");
            Assert.AreEqual(3, colony.LeavesWatered);
            Assert.AreEqual(0, colony.WasteCells);
        }

        [Test]
        public void RemainsToLeaves_TheLeavesStayLeaves()
        {
            PixelGrid grid = MakeWorld(WasteMode.RemainsToLeaves);
            AntColony colony = MakeColony(grid, (12, 1, CellMaterial.TermiteRemains));

            Tick(colony, 121f + colony.RaidSettings.LeafWaterSeconds + 1f);
            Assert.AreEqual(3, CountStored(grid, StoredItems.Of(CellMaterial.PlantLeaf)), "Na área de restos em folhas a folha não vira água.");
            Assert.AreEqual(0, CountStored(grid, StoredItems.Water));
            Assert.AreEqual(0, colony.LeavesWatered);
        }

        [Test]
        public void DirtToWater_EachSoilBecomesOneSandAndOneWater_AndR2HalvesTheTime()
        {
            PixelGrid grid = MakeWorld(WasteMode.DirtToWater);
            AntColony colony = MakeColony(grid, (12, 1, CellMaterial.Spoil), (14, 2, CellMaterial.Spoil));
            float seconds = colony.RaidSettings.DirtWaterSeconds;

            Tick(colony, seconds - 1f);
            Assert.AreEqual(2, CountStored(grid, StoredItems.Of(CellMaterial.Spoil)), "Antes do tempo, nada muda.");

            Tick(colony, 2f);
            Assert.AreEqual(0, CountStored(grid, StoredItems.Of(CellMaterial.Spoil)));
            Assert.AreEqual(StoredItems.Water, grid.GetStored(12, 1), "A água fica no lugar da terra.");
            Assert.AreEqual(StoredItems.Water, grid.GetStored(14, 2));
            Assert.AreEqual(2, CountStored(grid, StoredItems.Of(CellMaterial.Sand), onlyWasteRoom: true), "E a areia numa vaga perto.");
            Assert.AreEqual(2, colony.DirtConverted);

            PixelGrid grid2 = MakeWorld(WasteMode.DirtToWater);
            AntColony fast = MakeColony(grid2);
            fast.RestoreGalleries(new[] { "R2" }, new[] { 1 });
            Assert.IsTrue(grid2.SetStored(12, 1, StoredItems.Of(CellMaterial.Spoil)));
            fast.RestoreState(0, 0, 0f, new Ant[0]);
            Tick(fast, seconds * 0.5f + 1f);
            Assert.AreEqual(1, fast.DirtConverted, "Com o R2, a terra vira areia e água na metade do tempo.");
        }

        [Test]
        public void DirtToWater_WaitsForAFreeCellForTheSand()
        {
            PixelGrid grid = MakeWorld(WasteMode.DirtToWater);
            AntColony colony = MakeColony(grid, (12, 1, CellMaterial.Spoil));
            for (int x = 10; x <= 16; x++)
            for (int y = 1; y <= 3; y++)
                if (x != 12 || y != 1)
                    grid.Set(x, y, CellMaterial.Rock);

            Tick(colony, colony.RaidSettings.DirtWaterSeconds + 5f);
            Assert.AreEqual(0, colony.DirtConverted, "Sem vaga para a areia, a terra espera.");
            Assert.AreEqual(StoredItems.Of(CellMaterial.Spoil), grid.GetStored(12, 1));

            grid.Set(13, 1, CellMaterial.Empty);
            Tick(colony, 1.5f);
            Assert.AreEqual(1, colony.DirtConverted);
            Assert.AreEqual(StoredItems.Of(CellMaterial.Sand), grid.GetStored(13, 1));
        }

        [Test]
        public void RepaintingAnArea_WithAnotherMode_ReleasesTheOldInputs_AndPicksUpTheNewOnes()
        {
            PixelGrid grid = MakeWorld(WasteMode.DirtToWater);
            AntColony colony = MakeColony(grid, (12, 1, CellMaterial.Spoil), (13, 1, CellMaterial.PlantLeaf), (15, 1, CellMaterial.Refuse));
            Assert.AreEqual(2, colony.WasteCells, "Terra (área de terra) e entulho.");

            Tick(colony, 30f);
            PaintArea(grid, 10, 16, WasteMode.LeavesToWater);
            Assert.AreEqual(WasteMode.LeavesToWater, colony.WasteModeAt(12, 1), "Pintar por cima troca o modo da célula.");
            colony.Tick(Dt);
            Assert.AreEqual(2, colony.WasteCells, "Agora a folha e o entulho; a terra fica solta.");
            Tick(colony, colony.RaidSettings.LeafWaterSeconds + 1f);
            Assert.AreEqual(StoredItems.Water, grid.GetStored(13, 1), "A folha vira água.");
            Assert.AreEqual(StoredItems.Of(CellMaterial.Spoil), grid.GetStored(12, 1), "A terra não se converte fora da área de terra.");
            Assert.AreEqual(0, colony.DirtConverted);
            // O entulho chegou no começo (120 s): trocar o modo não recomeça o tempo dele.
            Assert.AreEqual(StoredItems.Of(CellMaterial.Refuse), grid.GetStored(15, 1));
            Tick(colony, 120f - 30f - colony.RaidSettings.LeafWaterSeconds);
            Assert.AreEqual(StoredItems.None, grid.GetStored(15, 1), "Some aos 120 s contados desde que chegou.");
        }

        [Test]
        public void DirtToWater_TheDiggersBringTheSoilToTheArea_InsteadOfTheDump()
        {
            PixelGrid grid = MakeWorld(WasteMode.DirtToWater);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3) { RaidsEnabled = false };
            colony.RestoreQueen(95, 2, 1);
            for (int i = 0; i < 4; i++)
                colony.Spawn(AntRole.Digger);
            for (int x = 30; x <= 34; x++)
                grid.SetMarked(x, 4, true);

            Assert.IsTrue(TickUntil(colony, () => colony.DirtConverted >= 2, 300f),
                          $"A terra cavada vai à área e vira areia e água (depositados {colony.WasteDeposited}, convertidos {colony.DirtConverted}).");
            Assert.Greater(CountStored(grid, StoredItems.Water), 0);
        }

        /// <summary>
        /// Relato de 2026-10-21 (save no modo terra → água, 1290 formigas, ~270 ociosas): a área ficava vazia porque só a
        /// terra cavada que iria ao despejo ia para lá. Sem nada para cavar, as ociosas levam a pilha de fora.
        /// </summary>
        [Test]
        public void DirtToWater_IdleAnts_BringTheOutsidePileToTheArea_WithNothingToDig()
        {
            PixelGrid grid = MakeWorld(WasteMode.DirtToWater);
            for (int x = 56; x <= 62; x++)
            for (int y = 21; y <= 23; y++)
                grid.Set(x, y, CellMaterial.Spoil);
            // Salas já rebocadas: a construção vem antes das automatizações (decisão de 2026-10-22); o teste mede o que vem depois dela.
            RoomLiningTests.LineAllRooms(grid);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3) { RaidsEnabled = false };
            colony.RestoreQueen(95, 2, 1);
            for (int i = 0; i < 4; i++)
                colony.Spawn(AntRole.Worker);

            Assert.IsTrue(TickUntil(colony, () => colony.WasteDeposited >= 3, 300f),
                          $"As ociosas levam a terra da pilha para a área (depositados {colony.WasteDeposited}).");
            Assert.IsTrue(TickUntil(colony, () => colony.DirtConverted >= 1, colony.RaidSettings.DirtWaterSeconds + 30f));

            PaintArea(grid, 10, 16, WasteMode.RemainsToLeaves);
            Tick(colony, 5f);
            int deposited = colony.WasteDeposited;
            Tick(colony, 60f);
            Assert.AreEqual(deposited, colony.WasteDeposited, "Numa área de restos, ninguém traz a pilha.");
        }

        /// <summary>
        /// Relato de 2026-10-21 (print com duas áreas misturando terra, areia, água e folhas): cada área recebe só o conteúdo
        /// do modo dela, e o que nasce das conversões fica na mesma área.
        /// </summary>
        [Test]
        public void TwoAreas_EachReceivesOnlyItsOwnContent()
        {
            PixelGrid grid = MakeWorld(WasteMode.RemainsToLeaves);
            PaintArea(grid, 30, 36, WasteMode.DirtToWater);
            for (int x = 56; x <= 62; x++)
            for (int y = 21; y <= 23; y++)
                grid.Set(x, y, CellMaterial.Spoil);
            AntColony colony = MakeColony(grid, (12, 1, CellMaterial.TermiteRemains), (13, 2, CellMaterial.Refuse));
            for (int i = 0; i < 4; i++)
                colony.Spawn(AntRole.Worker);
            for (int i = 0; i < 2; i++)
                colony.Spawn(AntRole.Digger);
            for (int x = 70; x <= 74; x++)
                grid.SetMarked(x, 4, true);

            Assert.IsTrue(TickUntil(colony, () => colony.DirtConverted >= 3 && colony.RemainsRotted >= 1, 300f),
                          $"Convertidas {colony.DirtConverted}, restos {colony.RemainsRotted}.");
            byte spoil = StoredItems.Of(CellMaterial.Spoil), sand = StoredItems.Of(CellMaterial.Sand), leaf = StoredItems.Of(CellMaterial.PlantLeaf);
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                if (!grid.IsWasteRoom(x, y))
                    continue;
                byte item = grid.GetStored(x, y);
                WasteMode mode = colony.WasteModeAt(x, y);
                if (item == spoil || item == sand)
                    Assert.AreEqual(WasteMode.DirtToWater, mode, $"Terra ou areia em ({x}, {y}) fora da área de terra.");
                if (item == leaf || item == StoredItems.Of(CellMaterial.TermiteRemains) || item == StoredItems.Of(CellMaterial.Refuse))
                    Assert.AreEqual(WasteMode.RemainsToLeaves, mode, $"Folha ou resíduo em ({x}, {y}) fora da área de restos.");
            }
        }

        [Test]
        public void TheModeOfEachArea_GoesToTheSave()
        {
            PixelGrid grid = MakeWorld(WasteMode.DirtToWater);
            PaintArea(grid, 30, 36, WasteMode.LeavesToWater);
            AntColony colony = MakeColony(grid, (12, 1, CellMaterial.Spoil));
            var rain = new RainSimulation(new RainSettings(), 3);
            var water = new WaterSimulation(new WaterSettings(), grid, 3);
            var stream = new MemoryStream();
            GameSnapshot.Capture(grid, colony, rain, water, 0, 3, 10.0).Write(stream);
            stream.Position = 0;
            GameSnapshot read = GameSnapshot.Read(stream);

            var loadedGrid = new PixelGrid(grid.Width, grid.Height);
            for (int x = 0; x < grid.Width; x++)
                loadedGrid.SetSurfaceY(x, grid.GetSurfaceY(x));
            read.ApplyTo(loadedGrid);
            var loaded = new AntColony(loadedGrid, new ColonySettings(), Layout, seed: 3) { RaidsEnabled = false };
            read.ApplyTo(loaded);

            Assert.AreEqual(WasteMode.DirtToWater, loaded.WasteModeAt(12, 1));
            Assert.AreEqual(WasteMode.LeavesToWater, loaded.WasteModeAt(33, 2));
            Assert.AreEqual(1, loaded.WasteCells, "A terra guardada volta a se converter.");
        }
    }
}
