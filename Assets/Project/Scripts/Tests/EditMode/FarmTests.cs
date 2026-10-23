using System.IO;
using System.Linq;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.Upgrades;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Fazenda de pulgões (pedido do desenvolvedor em 2026-10-22): baias e limite de pulgões, água sem fazendeira, ordenha
    /// com os cochos cheios, depósito de areia, cupim deixando o pulgão preto, fazendeira na árvore da rainha e save.
    /// </summary>
    public sealed class FarmTests
    {
        const float Dt = 0.1f;
        const int FarmMinX = 12, FarmMaxX = 31;
        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);

        /// <summary>
        /// Solo de terra até y = 20, corredor de x 10 a 100 com 3 linhas (y 1 a 3), poço em x 50..51 até a superfície,
        /// fazenda em x 12..31 (chão de 20 células: 5 baias) e sala da rainha em x 88..98.
        /// </summary>
        static PixelGrid MakeWorld(bool farm = true)
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
            if (farm)
            {
                for (int x = FarmMinX; x <= FarmMaxX; x++)
                for (int y = 1; y <= 3; y++)
                    Assert.IsTrue(grid.SetFarmRoom(x, y, true));
            }
            return grid;
        }

        static AntColony MakeColony(PixelGrid grid)
        {
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 5) { RaidsEnabled = false };
            colony.RestoreQueen(95, 2, 1);
            return colony;
        }

        static void PaintDepot(PixelGrid grid, StorageKind kind, int minX)
        {
            for (int x = minX; x <= minX + 6; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetStorageRoom(x, y, kind, true));
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

        static void TickFor(AntColony colony, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
                colony.Tick(Dt);
        }

        static int CountItems(PixelGrid grid, byte item)
        {
            int n = 0;
            RectInt area = grid.Active;
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
                n += grid.GetStored(x, y) == item ? 1 : 0;
            return n;
        }

        [Test]
        public void FarmRoom_IsARoomOfItsOwn_ThatNeverOverlapsOtherRooms()
        {
            PixelGrid grid = MakeWorld();
            Assert.AreEqual(60, grid.FarmRoomCount);
            Assert.IsFalse(grid.SetStorageRoom(15, 2, StorageKind.Sand, true), "O depósito não pinta por cima da fazenda.");
            Assert.IsFalse(grid.SetNurseryRoom(15, 2, true), "As outras salas também não.");
            Assert.IsFalse(grid.SetFarmRoom(90, 2, true), "A fazenda não pinta por cima da sala da rainha.");
            Assert.IsFalse(grid.SetFarmRoom(40, 25, true), "Só no formigueiro (no solo original ou abaixo).");
            Assert.IsTrue(grid.IsAnyRoom(15, 2));
            Assert.AreEqual(StorageKind.Sand, StoredItems.KindOf(StoredItems.Sand), "A areia tem depósito próprio.");
            Assert.AreEqual(StorageKind.Mushroom, StoredItems.KindOf(StoredItems.Mushroom), "O cogumelo tem depósito próprio.");
            Assert.IsTrue(grid.SetStorageRoom(36, 2, StorageKind.Mushroom, true), "O depósito de cogumelos é pintável.");
        }

        [Test]
        public void Farm_SplitsTheFloorIntoStalls_AndBreedsAphidsUpToTheCap()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            colony.Tick(Dt);
            Assert.AreEqual((FarmMaxX - FarmMinX + 1) / AntColony.StallWidth, colony.StallCount, "Uma baia a cada 4 células do chão.");
            Assert.AreEqual(2, colony.AphidCap, "Sem upgrades, a fazenda cria 2 pulgões.");

            Assert.IsTrue(TickUntil(colony, () => colony.Aphids.Count == 2, 200f), "Os pulgões nascem um por vez.");
            TickFor(colony, 150f);
            Assert.AreEqual(2, colony.Aphids.Count, "No limite, não nasce mais nenhum.");

            colony.RestoreGalleries(new[] { "A+1" }, new[] { 1 });
            Assert.AreEqual(3, colony.AphidCap, "A+1 soma um pulgão por nível.");
            Assert.IsTrue(TickUntil(colony, () => colony.Aphids.Count == 3, 100f));
            TickFor(colony, 10f);
            var stalls = new System.Collections.Generic.HashSet<int>();
            foreach (Aphid aphid in colony.Aphids)
            {
                Assert.GreaterOrEqual(aphid.Stall, 0, "Cada pulgão tem baia.");
                Assert.IsTrue(stalls.Add(aphid.Stall), "Um pulgão por baia.");
                Assert.IsTrue(grid.IsFarmRoom(aphid.X, aphid.Y), "O pulgão fica dentro da fazenda.");
            }
        }

        [Test]
        public void Aphids_StrollAroundTheFarm_AndGoToTheirTroughsOnlyToEat()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            colony.Tick(Dt);
            Aphid aphid = colony.SpawnAphid(FarmMinX + 2, 1);
            var visited = new System.Collections.Generic.HashSet<int>();
            bool offTheFloor = false;
            for (float t = 0f; t < 60f; t += Dt)
            {
                colony.Tick(Dt);
                Assert.IsTrue(grid.IsFarmRoom(aphid.X, aphid.Y), "O passeio não sai da fazenda.");
                visited.Add(aphid.X);
                offTheFloor |= aphid.Y > 1;
            }
            Assert.GreaterOrEqual(visited.Count, 6, "Sem comida nos cochos, o pulgão passeia pela fazenda.");
            Assert.IsTrue(offTheFloor, "Ele sobe pelas paredes e anda pelo teto também.");
            Assert.IsFalse(aphid.Fed);

            int sandX = aphid.Stall % grid.Width, y = aphid.Stall / grid.Width;
            Assert.IsTrue(grid.SetStored(sandX, y, StoredItems.Sand));
            Assert.IsTrue(grid.SetStored(sandX + 1, y, StoredItems.Water));
            Assert.IsTrue(TickUntil(colony, () => aphid.Fed, 30f), "Com os dois cochos cheios, ele vai até eles e come.");
            Assert.AreEqual(StoredItems.None, grid.GetStored(sandX, y), "Comeu a areia.");
            Assert.AreEqual(StoredItems.None, grid.GetStored(sandX + 1, y), "Bebeu a água.");
            Assert.AreEqual(sandX + 2, aphid.X, "Comeu ao lado dos cochos.");

            visited.Clear();
            for (float t = 0f; t < 30f; t += Dt)
            {
                colony.Tick(Dt);
                visited.Add(aphid.X);
            }
            Assert.Greater(visited.Count, 1, "Depois de comer, volta a passear.");
        }

        [Test]
        public void StallsWithAnAphid_KeepTheirPlace_WhenTheFloorBesideThemChanges()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            colony.Tick(Dt);
            Aphid first = colony.SpawnAphid(FarmMinX + 6, 1);
            Aphid second = colony.SpawnAphid(FarmMinX + 10, 1);
            TickFor(colony, 2f);
            int firstStall = first.Stall, secondStall = second.Stall;
            Assert.AreEqual(grid.ToIndex(FarmMinX + 4, 1), firstStall, "O pulgão fica com a baia mais perto de onde nasceu.");
            Assert.AreEqual(grid.ToIndex(FarmMinX + 8, 1), secondStall);

            // O começo do chão muda (antes, todas as baias do trecho se deslocavam, com os cochos).
            grid.Set(FarmMinX, 1, CellMaterial.Dirt);
            TickFor(colony, 10f);
            Assert.AreEqual(firstStall, first.Stall, "A baia com pulgão não muda de lugar.");
            Assert.AreEqual(secondStall, second.Stall);
            Assert.AreEqual(4, colony.StallCount, "O resto do chão vira baias em volta das que têm pulgão.");

            grid.Set(FarmMinX, 1, CellMaterial.Empty);
            TickFor(colony, 10f);
            Assert.AreEqual(firstStall, first.Stall, "Nem quando o chão volta.");
            Assert.AreEqual(secondStall, second.Stall);
        }

        [Test]
        public void WithoutAFarmer_TheAphidOnlyDripsWater()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            colony.Tick(Dt);
            colony.SpawnAphid(FarmMinX + 2, 1);

            TickFor(colony, 100f);

            Assert.GreaterOrEqual(colony.AphidDrips, 2, "Sem fazendeira, o pulgão pinga água sozinho.");
            Assert.GreaterOrEqual(CountItems(grid, StoredItems.Water), 2, "A água fica na fazenda (sem depósito de água nem canteiro).");
            Assert.AreEqual(0, colony.AphidMilkings);
            Assert.AreEqual(0, CountItems(grid, StoredItems.Sand), "Sem ordenha, não sai areia.");
            Assert.AreEqual(0, CountItems(grid, StoredItems.Of(CellMaterial.PlantLeaf)), "Sem ordenha, não sai folha.");
        }

        [Test]
        public void Farmer_FillsTheTroughsFromTheDepots_AndMilkingYieldsSandLeavesAndWater()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Sand, 34);
            PaintDepot(grid, StorageKind.Water, 42);
            for (int x = 34; x <= 38; x++)
                Assert.IsTrue(grid.SetStored(x, 1, StoredItems.Sand));
            for (int x = 42; x <= 46; x++)
                Assert.IsTrue(grid.SetStored(x, 1, StoredItems.Water));
            AntColony colony = MakeColony(grid);
            colony.Tick(Dt);
            Aphid aphid = colony.SpawnAphid(FarmMinX + 2, 1);
            Ant farmer = colony.SpawnAt(AntRole.Farmer, 40, 1);

            Assert.IsTrue(TickUntil(colony, () => colony.AphidMilkings >= 1, 120f), "A fazendeira enche os cochos e ordenha.");

            Assert.AreEqual(AntRole.Farmer, farmer.Role);
            Assert.GreaterOrEqual(CountItems(grid, StoredItems.Of(CellMaterial.PlantLeaf)), 1, "A ordenha solta folha.");
            Assert.Greater(CountItems(grid, StoredItems.Sand), 0, "A ordenha solta areia (o depósito a recebe de volta).");
            Assert.AreEqual(aphid, colony.Aphids[0]);
        }

        [Test]
        public void SandDepot_ReceivesTheDugSand()
        {
            PixelGrid grid = MakeWorld(farm: false);
            PaintDepot(grid, StorageKind.Sand, 34);
            for (int x = 60; x <= 63; x++)
            {
                grid.Set(x, 4, CellMaterial.Sand);
                Assert.IsTrue(grid.SetMarked(x, 4, true));
            }
            AntColony colony = MakeColony(grid);

            Assert.IsTrue(TickUntil(colony, () => colony.DepotStock(StorageKind.Sand) >= 4, 120f),
                          "A areia cavada vai ao depósito de areia, não ao despejo.");
        }

        [Test]
        public void Mushrooms_GoToTheMushroomDepot_NeverToTheBuildingDepot()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Blocks, 34);
            PaintDepot(grid, StorageKind.Mushroom, 42);
            Assert.IsTrue(grid.SetStored(FarmMinX + 8, 3, StoredItems.Mushroom), "Cogumelo solto na fazenda, como a ordenha o deixa.");
            Assert.IsTrue(grid.SetStored(FarmMinX + 12, 3, StoredItems.Mushroom));
            AntColony colony = MakeColony(grid);

            Assert.IsTrue(TickUntil(colony, () => colony.DepotStock(StorageKind.Mushroom) == 2, 60f),
                          "As formigas levam os cogumelos soltos ao depósito de cogumelos.");
            Assert.AreEqual(0, colony.DepotStock(StorageKind.Blocks), "O depósito de construção não recebe cogumelo.");
            Assert.AreEqual(2, colony.MushroomItems);
        }

        [Test]
        public void WithoutTheMushroomDepot_TheMushroomStaysWhereTheMilkingLeftIt()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Blocks, 34);
            Assert.IsTrue(grid.SetStored(FarmMinX + 8, 3, StoredItems.Mushroom));
            AntColony colony = MakeColony(grid);

            TickFor(colony, 30f);

            Assert.AreEqual(StoredItems.Mushroom, grid.GetStored(FarmMinX + 8, 3), "Sem depósito de cogumelos, ele fica na fazenda.");
            Assert.AreEqual(0, colony.DepotStock(StorageKind.Blocks), "Nem vai ao depósito de construção.");
        }

        [Test]
        public void TermiteBite_TurnsTheAphidBlack_AndItFightsForTheTermites()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            colony.Tick(Dt);
            Aphid aphid = colony.SpawnAphid(FarmMinX + 2, 1);
            colony.SpawnTermite(FarmMinX + 8, 1);

            Assert.IsTrue(TickUntil(colony, () => colony.AphidsCorrupted == 1, 30f), "O cupim vai atrás do pulgão e o morde.");
            Assert.AreEqual(0, colony.Aphids.Count, "O pulgão mordido deixa a fazenda.");
            Termite black = null;
            foreach (Termite t in colony.Termites)
            {
                if (t.Aphid)
                    black = t;
            }
            Assert.IsNotNull(black, "Ele vira o pulgão preto, do lado dos cupins.");
            Assert.IsTrue(black.Alive && black.OnGround);

            Assert.IsTrue(TickUntil(colony, () => !black.Alive, 240f), "As formigas caçam o pulgão preto dentro do formigueiro.");
            Assert.AreEqual(1, colony.AphidsKilled, "O pulgão preto não vai embora: fica até morrer.");
        }

        [Test]
        public void BlackAphid_BitesTheAntsNextToIt()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            colony.Tick(Dt);
            Aphid aphid = colony.SpawnAphid(FarmMinX + 2, 1);
            Ant ant = colony.SpawnAt(AntRole.Worker, FarmMinX + 3, 1);

            colony.CorruptAphidForTest(aphid);
            float wounds = 0f;
            for (float t = 0f; t < 3f && colony.Ants.Contains(ant); t += Dt)
            {
                colony.Tick(Dt);
                wounds = Mathf.Max(wounds, ant.Wounds);
            }

            Assert.Greater(wounds, 0f, "O pulgão preto morde a formiga ao alcance.");
        }

        [Test]
        public void Farmer_NeedsTheFarmChamber_AndBothAreBoughtWithNutrients()
        {
            AntColony colony = MakeColony(MakeWorld());
            colony.RestoreNutrients(5000, 5000, 0);
            Assert.IsFalse(colony.Tree.IsRoleUnlocked(AntRole.Farmer));
            Assert.AreEqual(BuyBlock.MissingRequirement, colony.TryBuy(QueenNode.Z0), "A fazendeira espera a fazenda.");
            Assert.IsFalse(colony.FarmUnlocked);

            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(UpgradeCatalog.Get("A0"), 1, out int bought));
            Assert.AreEqual(1, bought);
            Assert.IsTrue(colony.FarmUnlocked, "A câmara A0 libera o pincel da fazenda.");
            Assert.AreEqual(BuyBlock.None, colony.TryBuy(QueenNode.Z0));
            Assert.IsTrue(colony.Tree.IsRoleUnlocked(AntRole.Farmer));
            Assert.AreEqual(BuyBlock.NeedsDepth, colony.TryBuy(QueenNode.Z1), "Z1 fica no nível 2 do formigueiro.");
            QueenTreeTests.OpenTierTwo(colony);
            Assert.AreEqual(BuyBlock.None, colony.TryBuy(QueenNode.Z1));
            Assert.AreEqual(1.5f, colony.Tree.MilkRate(AntRole.Farmer), 1e-5f, "Z1: ordenha 50% mais rápida.");
        }

        [Test]
        public void FarmAndAphids_SurviveSaveAndLoad()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            colony.Tick(Dt);
            colony.SpawnAphid(FarmMinX + 2, 1);
            colony.SpawnAphid(FarmMinX + 6, 1).Fed = true;
            Assert.IsTrue(grid.SetStored(FarmMinX, 1, StoredItems.Sand));
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
            var loaded = new AntColony(loadedGrid, new ColonySettings(), Layout, seed: 5);
            read.ApplyTo(loaded);

            Assert.AreEqual(grid.FarmRoomCount, loadedGrid.FarmRoomCount, "A fazenda pintada volta.");
            Assert.AreEqual(StoredItems.Sand, loadedGrid.GetStored(FarmMinX, 1), "O cocho cheio volta cheio.");
            Assert.AreEqual(2, loaded.Aphids.Count, "Os pulgões voltam.");
            Assert.AreEqual(FarmMinX + 6, loaded.Aphids[1].X);
            Assert.IsFalse(loaded.Aphids[0].Fed, "O pulgão com fome volta com fome.");
            Assert.IsTrue(loaded.Aphids[1].Fed, "O que já comeu volta alimentado.");
        }
    }
}
