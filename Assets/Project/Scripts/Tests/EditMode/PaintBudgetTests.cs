using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Upgrades;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Limite de área do pincel por ambiente (KI-043): orçamento de células por ambiente, máximo de salas não conectadas
    /// dividindo esse orçamento e as galerias que somam células.
    /// </summary>
    public sealed class PaintBudgetTests
    {
        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);

        /// <summary>Mundo de 80x80 todo abaixo da superfície (y = 70), onde qualquer sala pode ser pintada.</summary>
        static (PixelGrid grid, AntColony colony) Make()
        {
            var grid = new PixelGrid(80, 80);
            for (int x = 0; x < grid.Width; x++)
                grid.SetSurfaceY(x, 70);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3) { RaidsEnabled = false };
            return (grid, colony);
        }

        static int PaintRect(PixelGrid grid, int x0, int y0, int w, int h)
        {
            int painted = 0;
            for (int x = x0; x < x0 + w; x++)
            for (int y = y0; y < y0 + h; y++)
                if (grid.SetNutrientRoom(x, y, true))
                    painted++;
            return painted;
        }

        [Test]
        public void UsedAndAvailableFollowThePaintedCells()
        {
            var (grid, colony) = Make();
            PaintBudget budget = colony.PaintBudget;
            int limit = budget.Limit(PaintRoom.Nutrient);
            Assert.AreEqual(new ColonySettings().PaintCells(PaintRoom.Nutrient), limit);
            Assert.AreEqual(1f, budget.AvailableFraction(PaintRoom.Nutrient));

            PaintRect(grid, 10, 10, 10, 5); // 50 células
            Assert.AreEqual(50, budget.Used(PaintRoom.Nutrient));
            Assert.AreEqual(limit - 50, budget.Available(PaintRoom.Nutrient));
            Assert.AreEqual(budget.Limit(PaintRoom.Queen), budget.Available(PaintRoom.Queen), "cada ambiente tem o próprio limite");
        }

        [Test]
        public void FullBudgetBlocksPaintingButNotNeighbours()
        {
            var (grid, colony) = Make();
            PaintBudget budget = colony.PaintBudget;
            int limit = budget.Limit(PaintRoom.Nutrient);
            // Uma faixa contínua com exatamente o limite (largura 10).
            PaintRect(grid, 10, 10, 10, limit / 10);
            for (int x = 10; x < 10 + limit % 10; x++)
                grid.SetNutrientRoom(x, 10 + limit / 10, true);
            Assert.AreEqual(0, budget.Available(PaintRoom.Nutrient));
            Assert.AreEqual(PaintBlock.NoCells, budget.Check(PaintRoom.Nutrient, 9, 10));

            grid.SetNutrientRoom(10, 10, false); // apagar libera uma célula
            Assert.AreEqual(PaintBlock.None, budget.Check(PaintRoom.Nutrient, 9, 11));
        }

        [Test]
        public void RoomsAreCountedByFourSidedConnection()
        {
            var (grid, colony) = Make();
            PaintBudget budget = colony.PaintBudget;
            Assert.AreEqual(0, budget.Rooms(PaintRoom.Nutrient));
            PaintRect(grid, 10, 10, 5, 5);
            Assert.AreEqual(1, budget.Rooms(PaintRoom.Nutrient));
            // Só a quina encosta: diagonal não conecta.
            PaintRect(grid, 15, 15, 3, 3);
            Assert.AreEqual(2, budget.Rooms(PaintRoom.Nutrient));
            // Uma célula ligando as duas pelo lado as junta numa sala só.
            grid.SetNutrientRoom(15, 14, true);
            Assert.AreEqual(1, budget.Rooms(PaintRoom.Nutrient));
        }

        [Test]
        public void MaxRoomsBlocksANewRoomButLetsTheExistingOnesGrow()
        {
            var (grid, colony) = Make();
            PaintBudget budget = colony.PaintBudget;
            Assert.AreEqual(2, budget.MaxRooms);
            PaintRect(grid, 10, 10, 4, 4);
            PaintRect(grid, 30, 10, 4, 4);
            int rooms = budget.Rooms(PaintRoom.Nutrient);
            Assert.AreEqual(2, rooms);

            Assert.IsTrue(budget.StartsRoom(PaintRoom.Nutrient, 50, 50));
            Assert.AreEqual(PaintBlock.NoRooms, budget.Check(PaintRoom.Nutrient, 50, 50));
            Assert.AreEqual(PaintBlock.None, budget.Check(PaintRoom.Nutrient, 14, 11), "encostada numa sala existente");
            // Outro ambiente tem as próprias salas.
            Assert.AreEqual(PaintBlock.None, budget.Check(PaintRoom.Queen, 50, 50));
        }

        [Test]
        public void TheWholeBrushDecidesIfItOpensARoom()
        {
            var (grid, colony) = Make();
            PaintBudget budget = colony.PaintBudget;
            PaintRect(grid, 10, 10, 4, 4);
            PaintRect(grid, 30, 10, 4, 4);
            // Raio 4 centrado a 5 células da sala (x 13): a borda do círculo encosta nela, então não abre sala nova.
            Assert.IsFalse(budget.StartsRoom(PaintRoom.Nutrient, 18, 12, 4));
            Assert.AreEqual(PaintBlock.None, budget.Check(PaintRoom.Nutrient, 18, 12, 4));
            // Uma célula mais longe já não encosta (nem pela diagonal da borda).
            Assert.IsTrue(budget.StartsRoom(PaintRoom.Nutrient, 19, 12, 4));
            Assert.AreEqual(PaintBlock.NoRooms, budget.Check(PaintRoom.Nutrient, 19, 12, 4));

            // Com uma sala só, o círculo longe abre a segunda inteiro: nenhuma borda de linha conta como sala à parte.
            grid.SetNutrientRoom(30, 10, false);
            for (int x = 30; x < 34; x++)
            for (int y = 10; y < 14; y++)
                grid.SetNutrientRoom(x, y, false);
            Assert.AreEqual(1, budget.Rooms(PaintRoom.Nutrient));
            Assert.AreEqual(PaintBlock.None, budget.Check(PaintRoom.Nutrient, 50, 50, 4));
        }

        [Test]
        public void ExtraRoomsGalleryRaisesTheMaxRoomsOfEveryRoom()
        {
            var (grid, colony) = Make();
            PaintBudget budget = colony.PaintBudget;
            UpgradeDef def = UpgradeCatalog.Get("T+3");
            Assert.AreEqual(def.Base, budget.MaxRooms, "a base do catálogo é a mesma da cena");
            PaintRect(grid, 10, 10, 4, 4);
            PaintRect(grid, 30, 10, 4, 4);
            Assert.AreEqual(PaintBlock.NoRooms, budget.Check(PaintRoom.Nutrient, 50, 50));

            colony.RestoreNutrients(100000, 100000, 0);
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(def, 1, out int bought));
            Assert.AreEqual(1, bought);
            Assert.AreEqual(def.Base + 1, budget.MaxRooms);
            Assert.AreEqual(PaintBlock.None, budget.Check(PaintRoom.Nutrient, 50, 50), "a 3ª sala cabe");
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(def, 0, out bought));
            Assert.AreEqual(def.Base + def.Cap, budget.MaxRooms, "para no teto");
        }

        [Test]
        public void DepotsHaveSeparateBudgetsAndRooms()
        {
            var (grid, colony) = Make();
            PaintBudget budget = colony.PaintBudget;
            for (int x = 10; x < 15; x++)
                Assert.IsTrue(grid.SetStorageRoom(x, 10, StorageKind.Food, true));
            for (int x = 15; x < 18; x++)
                Assert.IsTrue(grid.SetStorageRoom(x, 10, StorageKind.Water, true));
            Assert.AreEqual(5, budget.Used(PaintRoom.FoodStore));
            Assert.AreEqual(3, budget.Used(PaintRoom.WaterStore));
            Assert.AreEqual(0, budget.Used(PaintRoom.BlockStore));
            Assert.AreEqual(1, budget.Rooms(PaintRoom.FoodStore));
            Assert.AreEqual(1, budget.Rooms(PaintRoom.WaterStore));
            Assert.IsTrue(budget.StartsRoom(PaintRoom.FoodStore, 18, 10), "a água ao lado não conecta o depósito de alimentos");
        }

        [TestCase("C+4", PaintRoom.Nutrient)]
        [TestCase("B+3", PaintRoom.Nursery)]
        [TestCase("D+3", PaintRoom.WaterStore)]
        [TestCase("R+1", PaintRoom.Waste)]
        [TestCase("G+3", PaintRoom.Dew)]
        [TestCase("Q+4", PaintRoom.Queen)]
        public void AreaGalleriesRaiseTheLimit(string id, PaintRoom room)
        {
            var (_, colony) = Make();
            PaintBudget budget = colony.PaintBudget;
            UpgradeDef def = UpgradeCatalog.Get(id);
            int before = budget.Limit(room);
            Assert.AreEqual(def.Base, before, "a base do catálogo é a mesma da cena");
            colony.RestoreNutrients(100000, 100000, 0);
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(def, 2, out int bought));
            Assert.AreEqual(2, bought);
            Assert.AreEqual(before + Mathf.RoundToInt(2 * def.PerLevel), budget.Limit(room));
        }
    }
}
