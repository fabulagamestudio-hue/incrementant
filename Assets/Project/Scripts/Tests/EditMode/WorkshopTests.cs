using System;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Fundição, forja e equipamento (documento 45, REQ-071): minério + serapilheira viram barra; duas barras viram o
    /// equipamento que falta; a formiga veste o do papel dela, que acelera ou protege e, gasto, volta como barra.
    /// </summary>
    public sealed class WorkshopTests
    {
        const float Dt = 0.1f;

        [Test]
        public void Smelter_TurnsOreAndLitterIntoABar_OfTheSameMetal()
        {
            PixelGrid grid = MakeWorld();
            PaintRoom(grid, StorageKind.Smelter, 20);
            Assert.IsTrue(grid.SetStored(21, 1, StoredItems.Of(CellMaterial.IronOre)));
            Assert.IsTrue(grid.SetStored(23, 1, StoredItems.Litter));
            Assert.IsTrue(grid.SetStored(25, 1, StoredItems.Of(CellMaterial.GoldOre)), "Minério sem combustível espera.");
            RoomLiningTests.LineAllRooms(grid);
            AntColony colony = MakeColony(grid);

            TickFor(colony, AntColony.SmeltSeconds + 2f);
            Assert.AreEqual(1, colony.BarsSmelted);
            Assert.AreEqual(1, Count(grid, CellMaterial.IronBar), "O ferro virou barra de ferro.");
            Assert.AreEqual(0, Count(grid, CellMaterial.Litter), "A serapilheira queimou.");
            Assert.AreEqual(1, Count(grid, CellMaterial.GoldOre), "O ouro espera mais combustível.");
        }

        [Test]
        public void FullChain_OreAndLitterFromTheDepots_BecomeAToolThatAWorkerEquips()
        {
            PixelGrid grid = MakeWorld();
            PaintRoom(grid, StorageKind.Ore, 12);
            PaintRoom(grid, StorageKind.Litter, 20);
            PaintRoom(grid, StorageKind.Smelter, 28);
            PaintRoom(grid, StorageKind.Forge, 36);
            PaintRoom(grid, StorageKind.Armory, 44);
            Assert.IsTrue(grid.SetStored(13, 1, StoredItems.Of(CellMaterial.CopperOre)));
            Assert.IsTrue(grid.SetStored(15, 1, StoredItems.Of(CellMaterial.CopperOre)));
            Assert.IsTrue(grid.SetStored(21, 1, StoredItems.Litter));
            Assert.IsTrue(grid.SetStored(23, 1, StoredItems.Litter));
            RoomLiningTests.LineAllRooms(grid);
            AntColony colony = MakeColony(grid);

            Assert.IsTrue(TickUntil(colony, () => colony.BarsSmelted == 2, 200f), "As formigas levam minério e serapilheira à fundição.");
            Assert.IsTrue(TickUntil(colony, () => colony.ItemsForged == 1, 200f), "As barras vão à forja, que faz o equipamento que falta.");
            Assert.IsTrue(TickUntil(colony, () => Equipped(colony) > 0, 200f), "A ferramenta vai ao armazém e uma operária a veste.");
            foreach (Ant ant in colony.Ants)
                if (ant.Tool != CellMaterial.Empty)
                {
                    Assert.AreEqual(CellMaterial.CopperTool, ant.Tool, "Comum ganha ferramenta (não arma nem armadura).");
                    Assert.AreEqual(Equipment.Uses(EquipKind.Tool, 0), ant.ToolUses);
                }
        }

        [Test]
        public void WornOutTool_ComesBackAsABar()
        {
            PixelGrid grid = MakeWorld();
            grid.Set(60, 4, CellMaterial.Dirt);
            Assert.IsTrue(grid.SetMarked(60, 4, true));
            RoomLiningTests.LineAllRooms(grid);
            AntColony colony = MakeColony(grid);
            foreach (Ant ant in colony.Ants)
            {
                ant.Tool = CellMaterial.IronTool;
                ant.ToolUses = 1;
            }

            Assert.IsTrue(TickUntil(colony, () => grid.Get(60, 4) == CellMaterial.Empty, 60f));
            int worn = 0;
            foreach (Ant ant in colony.Ants)
                if (ant.Tool == CellMaterial.Empty)
                    worn++;
            Assert.AreEqual(1, worn, "Quem cavou gastou a ferramenta.");
            Assert.AreEqual(1, Count(grid, CellMaterial.IronBar) + Carrying(colony, CellMaterial.IronBar), "A ferramenta gasta volta como barra.");
        }

        [Test]
        public void Equipment_TableFollowsTheMetal()
        {
            Assert.AreEqual(0, Equipment.TierOf(CellMaterial.CopperOre));
            Assert.AreEqual(1, Equipment.TierOf(CellMaterial.IronWeapon));
            Assert.AreEqual(2, Equipment.TierOf(CellMaterial.GoldArmor));
            Assert.AreEqual(-1, Equipment.TierOf(CellMaterial.Dirt));
            Assert.AreEqual(CellMaterial.GoldBar, Equipment.BarOf(2));
            Assert.AreEqual(CellMaterial.IronArmor, Equipment.ItemOf(EquipKind.Armor, 1));
            Assert.AreEqual(StorageKind.Forge, StoredItems.KindOf(StoredItems.Of(CellMaterial.CopperBar)));
            Assert.AreEqual(StorageKind.Armory, StoredItems.KindOf(StoredItems.Of(CellMaterial.GoldWeapon)));
            Assert.AreEqual(StorageKind.Wardrobe, StoredItems.KindOf(StoredItems.Of(CellMaterial.CopperArmor)));
            Assert.Less(Equipment.ToolTimeFactor(2), Equipment.ToolTimeFactor(0), "Ouro cava mais rápido que cobre.");
            Assert.Greater(Equipment.WeaponDamageFactor(1), 1f);
            Assert.Less(Equipment.ArmorTakenFactor(0), 1f);
            Assert.IsTrue(AntColony.UsesEquip(AntRole.Digger, EquipKind.Tool));
            Assert.IsTrue(AntColony.UsesEquip(AntRole.Soldier, EquipKind.Armor));
            Assert.IsFalse(AntColony.UsesEquip(AntRole.Nurse, EquipKind.Weapon));
        }

        // ---------------------------------------------------------------- mundo de teste

        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);

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

        static void PaintRoom(PixelGrid grid, StorageKind kind, int minX)
        {
            for (int x = minX; x <= minX + 6; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetStorageRoom(x, y, kind, true));
        }

        static AntColony MakeColony(PixelGrid grid)
        {
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 5) { RaidsEnabled = false };
            colony.RestoreQueen(95, 2, 1);
            return colony;
        }

        static int Count(PixelGrid grid, CellMaterial item)
        {
            int n = 0;
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
                if (grid.GetStored(x, y) == StoredItems.Of(item))
                    n++;
            return n;
        }

        static int Carrying(AntColony colony, CellMaterial item)
        {
            int n = 0;
            foreach (Ant ant in colony.Ants)
                if (ant.Carried == item)
                    n += ant.CarriedCount;
            return n;
        }

        static int Equipped(AntColony colony)
        {
            int n = 0;
            foreach (Ant ant in colony.Ants)
                if (ant.Tool != CellMaterial.Empty)
                    n++;
            return n;
        }

        static void TickFor(AntColony colony, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
                colony.Tick(Dt);
        }

        static bool TickUntil(AntColony colony, Func<bool> done, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                colony.Tick(Dt);
                if (done())
                    return true;
            }
            return false;
        }
    }
}
