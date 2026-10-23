using System.IO;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Preenchimento vertical dos depósitos (REQ-072, etapa 2): a célula guarda uma pilha de itens iguais até a capacidade
    /// do depósito, e cada item ocupa 1/capacidade da altura dela.
    /// </summary>
    public sealed class StorageStackTests
    {
        const float Dt = 0.1f;
        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);
        static readonly byte Soil = StoredItems.Of(CellMaterial.Spoil);
        static readonly byte Leaf = StoredItems.Of(CellMaterial.PlantLeaf);
        static readonly byte Grass = StoredItems.Of(CellMaterial.PlantGrass);

        /// <summary>Solo de terra até y = 20, corredor de x 10 a 100 em y 1..3 e poço em x 50..51 até a superfície.</summary>
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
            return grid;
        }

        static void PaintDepot(PixelGrid grid, StorageKind kind, int minX)
        {
            for (int x = minX; x <= minX + 6; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetStorageRoom(x, y, kind, true));
        }

        [Test]
        public void WithoutTheUpgrade_ACellHoldsOneItem_AsBefore()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Blocks, 30);
            Assert.AreEqual(1, grid.ItemsPerCell(StorageKind.Blocks));
            Assert.IsTrue(grid.SetStored(31, 1, Soil));
            Assert.IsFalse(grid.SetStored(31, 1, Soil), "Sem a melhoria, a célula cheia não aceita outro.");
            Assert.AreEqual(1f, grid.GetStoredFill(31, 1));
        }

        [Test]
        public void WithTheUpgrade_TwoEqualItemsShareTheCell_EachHalfOfIt()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Blocks, 30);
            Assert.IsTrue(grid.SetStored(31, 1, Soil, 90));
            Assert.AreEqual(1f, grid.GetStoredFill(31, 1));

            Assert.IsTrue(grid.SetItemsPerCell(StorageKind.Blocks, 2));
            Assert.AreEqual(0.5f, grid.GetStoredFill(31, 1), "O bloco que enchia a célula passa a ocupar metade dela.");
            Assert.AreEqual(1, grid.StoredRoomFor(31, 1, Soil));
            Assert.IsTrue(grid.SetStored(31, 1, Soil, 200));
            Assert.AreEqual(2, grid.GetStoredAmount(31, 1));
            Assert.AreEqual(1f, grid.GetStoredFill(31, 1));
            Assert.AreEqual(90, grid.GetStoredShade(31, 1), "A pilha guarda o tom do primeiro.");
            Assert.IsFalse(grid.SetStored(31, 1, Soil), "Cheia com dois.");
            Assert.AreEqual(2, grid.StoredCount(StorageKind.Blocks), "A contagem é de itens, não de células.");

            Assert.IsTrue(grid.SetStored(31, 1, StoredItems.None));
            Assert.AreEqual(1, grid.GetStoredAmount(31, 1), "Tirar leva o de cima.");
            Assert.AreEqual(1, grid.StoredCount(StorageKind.Blocks));
            Assert.IsTrue(grid.SetStored(31, 1, StoredItems.None));
            Assert.AreEqual(StoredItems.None, grid.GetStored(31, 1));
            Assert.AreEqual(0, grid.StoredCount(StorageKind.Blocks));
        }

        [Test]
        public void OnlyEqualItemsShareACell()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Food, 20);
            grid.SetItemsPerCell(StorageKind.Food, 2);
            Assert.IsTrue(grid.SetStored(21, 1, Leaf));
            Assert.AreEqual(0, grid.StoredRoomFor(21, 1, Grass));
            Assert.IsFalse(grid.SetStored(21, 1, Grass), "Grama e folha não dividem a célula.");
            Assert.IsTrue(grid.SetStored(21, 1, Leaf));
        }

        [Test]
        public void TheCapacity_IsOnlyForTheDepotsOwnItems_AndOnlyForDepotsThatStack()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Blocks, 30);
            grid.SetItemsPerCell(StorageKind.Blocks, 2);
            Assert.IsTrue(grid.SetStored(32, 2, Leaf));
            Assert.IsFalse(grid.SetStored(32, 2, Leaf), "Folha solta no depósito de construção fica sozinha na célula.");
            Assert.AreEqual(1f, grid.GetStoredFill(32, 2));
            Assert.IsTrue(grid.SetStored(40, 2, Soil), "Fora do depósito: um item por célula.");
            Assert.IsFalse(grid.SetStored(40, 2, Soil));

            Assert.IsFalse(grid.SetItemsPerCell(StorageKind.Smelter, 2), "A fundição transforma o item na célula: não empilha.");
            Assert.AreEqual(1, grid.ItemsPerCell(StorageKind.Smelter));
        }

        [Test]
        public void SolidMaterial_DestroysTheWholeStack()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Blocks, 30);
            grid.SetItemsPerCell(StorageKind.Blocks, 2);
            grid.SetStored(33, 1, Soil);
            grid.SetStored(33, 1, Soil);
            grid.Set(33, 1, CellMaterial.Dirt);
            Assert.AreEqual(StoredItems.None, grid.GetStored(33, 1));
            Assert.AreEqual(0, grid.StoredCount(StorageKind.Blocks));
        }

        [Test]
        public void Stacks_SurviveSaveAndLoad()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Blocks, 30);
            grid.SetItemsPerCell(StorageKind.Blocks, 2);
            grid.SetStored(31, 1, Soil);
            grid.SetStored(31, 1, Soil);
            grid.SetStored(32, 1, Soil);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 5) { RaidsEnabled = false };
            colony.RestoreQueen(95, 2, 1);
            var rain = new RainSimulation(new RainSettings(), 3);
            var water = new WaterSimulation(new WaterSettings(), grid, 3);
            var stream = new MemoryStream();
            GameSnapshot.Capture(grid, colony, rain, water, 0, 3, 10.0).Write(stream);
            stream.Position = 0;
            GameSnapshot read = GameSnapshot.Read(stream);

            var loaded = new PixelGrid(grid.Width, grid.Height);
            for (int x = 0; x < grid.Width; x++)
                loaded.SetSurfaceY(x, grid.GetSurfaceY(x));
            read.ApplyTo(loaded);
            Assert.AreEqual(2, loaded.GetStoredAmount(31, 1));
            Assert.AreEqual(1, loaded.GetStoredAmount(32, 1));
            Assert.AreEqual(3, loaded.StoredCount(StorageKind.Blocks));
            Assert.AreEqual(1f, loaded.GetStoredFill(31, 1), "Sem a melhoria aplicada, a pilha de dois continua inteira (e cheia).");
            Assert.IsFalse(loaded.SetStored(32, 1, Soil));
            loaded.SetItemsPerCell(StorageKind.Blocks, 2);
            Assert.AreEqual(0.5f, loaded.GetStoredFill(32, 1));
        }

        [Test]
        public void Ants_FillTheDepotTwoPerCell_WithTheUpgrade()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Blocks, 30);
            Assert.IsTrue(grid.SetItemsPerCell(StorageKind.Blocks, 2));
            // 30 blocos de terra para cavar: mais que as 21 células do depósito.
            for (int x = 40; x <= 49; x++)
            for (int y = 4; y <= 6; y++)
                Assert.IsTrue(grid.SetMarked(x, y, true));
            RoomLiningTests.LineAllRooms(grid);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 5) { RaidsEnabled = false };
            colony.RestoreQueen(95, 2, 1);

            for (float t = 0f; t < 600f && grid.StoredCount(StorageKind.Blocks) < 30; t += Dt)
                colony.Tick(Dt);

            Assert.AreEqual(30, grid.StoredCount(StorageKind.Blocks), "A terra cavada inteira coube no depósito.");
            Assert.AreEqual(0, colony.TotalDelivered, "Nada foi ao despejo.");
            int doubled = 0;
            for (int x = 30; x <= 36; x++)
            for (int y = 1; y <= 3; y++)
            {
                Assert.LessOrEqual(grid.GetStoredAmount(x, y), 2);
                if (grid.GetStoredAmount(x, y) == 2)
                    doubled++;
            }
            Assert.GreaterOrEqual(doubled, 9, "Pelo menos 9 células com dois blocos (30 blocos em 21 células).");
            colony.Tick(Dt);
            Assert.AreEqual(42 - 30, colony.DepotFree(StorageKind.Blocks), "As vagas contam itens: 21 células × 2 − 30.");
            Assert.AreEqual(30, colony.DepotStock(StorageKind.Blocks));
        }
    }
}
