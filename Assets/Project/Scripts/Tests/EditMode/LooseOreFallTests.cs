using Fabula.Incrementant.World;
using NUnit.Framework;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Queda do minério solto (documento 45, pedido de 2026-10-23): fora do depósito de minério e da fundição o minério cai até
    /// apoiar e escorrega de cima de outro item; dentro deles fica onde foi posto, inclusive na parede de fundo.
    /// </summary>
    public sealed class LooseOreFallTests
    {
        const float Seconds = 3f;
        const float Dt = 0.05f;

        /// <summary>Terra até y = 20, com um vão de x 5 a 25 e y 1 a 12 (chão em y = 0, de pedra).</summary>
        static PixelGrid MakeHall()
        {
            var grid = new PixelGrid(40, 30);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.SetSurfaceY(x, 20);
                grid.Set(x, 0, CellMaterial.Rock);
                for (int y = 1; y <= 20; y++)
                    grid.Set(x, y, x >= 5 && x <= 25 && y <= 12 ? CellMaterial.Empty : CellMaterial.Dirt);
            }
            return grid;
        }

        static void Run(PixelGrid grid, LooseOreFall fall)
        {
            for (float t = 0f; t < Seconds; t += Dt)
                fall.Step(grid, Dt);
        }

        [Test]
        public void LooseOre_FallsToTheFloor_AndPilesUp()
        {
            PixelGrid grid = MakeHall();
            var fall = new LooseOreFall(new OreFallSettings());
            Assert.IsTrue(grid.SetStored(10, 9, (byte)CellMaterial.CopperOre, 90));
            Assert.IsTrue(grid.SetStored(10, 10, (byte)CellMaterial.IronOre));
            Assert.IsTrue(grid.SetStored(10, 11, (byte)CellMaterial.GoldOre));
            Assert.IsTrue(grid.SetStored(10, 12, (byte)CellMaterial.CopperOre));

            Run(grid, fall);

            Assert.AreEqual((byte)CellMaterial.CopperOre, grid.GetStored(10, 1), "O primeiro minério chega ao chão.");
            Assert.AreEqual(90, grid.GetStoredShade(10, 1), "O tom do item vai junto.");
            // Como a areia: o que cai em cima de outro item escorrega para a diagonal livre; sem ela, fica no topo do monte.
            Assert.AreNotEqual(StoredItems.None, grid.GetStored(9, 1), "Escorregou do monte para a esquerda.");
            Assert.AreNotEqual(StoredItems.None, grid.GetStored(11, 1), "Escorregou do monte para a direita.");
            Assert.AreNotEqual(StoredItems.None, grid.GetStored(10, 2), "Com as duas diagonais tomadas, o último fica no topo.");
            Assert.AreEqual(4, grid.StoredCount(StorageKind.Ore), "Nenhum minério se perde na queda.");
        }

        [Test]
        public void OreInItsDepotOrInTheSmelter_StaysOnTheBackWall()
        {
            PixelGrid grid = MakeHall();
            for (int x = 6; x <= 8; x++)
            for (int y = 1; y <= 12; y++)
                Assert.IsTrue(grid.SetStorageRoom(x, y, StorageKind.Ore, true));
            for (int x = 12; x <= 14; x++)
            for (int y = 1; y <= 12; y++)
                Assert.IsTrue(grid.SetStorageRoom(x, y, StorageKind.Smelter, true));
            Assert.IsTrue(grid.SetStored(7, 9, (byte)CellMaterial.CopperOre));
            Assert.IsTrue(grid.SetStored(13, 9, (byte)CellMaterial.IronOre));
            Assert.IsTrue(grid.SetStored(20, 9, (byte)CellMaterial.Litter));

            Run(grid, new LooseOreFall(new OreFallSettings()));

            Assert.AreEqual((byte)CellMaterial.CopperOre, grid.GetStored(7, 9), "No depósito de minério, fica na parede de fundo.");
            Assert.AreEqual((byte)CellMaterial.IronOre, grid.GetStored(13, 9), "Na fundição (em uso), fica onde está.");
            Assert.AreEqual((byte)CellMaterial.Litter, grid.GetStored(20, 9), "Só o minério cai; os outros itens seguem como antes.");
        }

        [Test]
        public void OreLeavingItsDepot_StartsFalling()
        {
            PixelGrid grid = MakeHall();
            for (int y = 1; y <= 12; y++)
                Assert.IsTrue(grid.SetStorageRoom(10, y, StorageKind.Ore, true));
            Assert.IsTrue(grid.SetStored(10, 8, (byte)CellMaterial.GoldOre));
            var fall = new LooseOreFall(new OreFallSettings());
            Run(grid, fall);
            Assert.AreEqual((byte)CellMaterial.GoldOre, grid.GetStored(10, 8));

            for (int y = 1; y <= 12; y++)
                Assert.IsTrue(grid.SetStorageRoom(10, y, StorageKind.Ore, false));
            Run(grid, fall);

            Assert.AreEqual((byte)CellMaterial.GoldOre, grid.GetStored(10, 1), "Depósito apagado: o minério fica solto e cai.");
        }
    }
}
