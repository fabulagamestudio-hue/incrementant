using System;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Minérios em veios (documento 45, REQ-071): cobre, ferro e ouro em bolsões, cada um na sua faixa de profundidade e alguns
    /// no resto do mapa, abaixo dela; só cortam terra e areia; cavados, vão ao depósito de minério e nunca ao despejo.
    /// </summary>
    public sealed class OreTests
    {
        const float Dt = 0.1f;

        [Test]
        public void Generator_PlacesTheThreeOresInPockets_MostlyInsideTheirDepthBands()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.WorldWidth, settings.WorldHeight);
            var generator = new TerrainGenerator(settings, 7, grid.Width, grid.Height, settings.StartArea, settings.SkyHeadroom);
            generator.Begin(grid);
            generator.Expand(grid, new RectInt(0, 0, grid.Width, grid.Height));

            var counts = new int[3];
            var belowBand = new int[3];
            CellMaterial[] ores = { CellMaterial.CopperOre, CellMaterial.IronOre, CellMaterial.GoldOre };
            OreBand[] bands = { settings.Ores.Copper, settings.Ores.Iron, settings.Ores.Gold };
            int isolated = 0, inner = 0, deepGold = 0;
            int deepest = 0;
            for (int x = 1; x < grid.Width - 1; x++)
                deepest = Math.Max(deepest, grid.GetSurfaceY(x));
            for (int x = 1; x < grid.Width - 1; x++)
            for (int y = 1; y < grid.Height - 1; y++)
            {
                int k = Array.IndexOf(ores, grid.Get(x, y));
                if (k < 0)
                    continue;
                counts[k]++;
                int depth = grid.GetSurfaceY(x) - y;
                Assert.GreaterOrEqual(depth, bands[k].Depth.x, $"{ores[k]} em ({x}, {y}) acima da faixa.");
                if (bands[k].Depth.y > 0 && depth > bands[k].Depth.y + 6)
                    belowBand[k]++;
                if (k == 2 && depth > deepest / 2)
                    deepGold++;
                int same = (grid.Get(x + 1, y) == ores[k] ? 1 : 0) + (grid.Get(x - 1, y) == ores[k] ? 1 : 0) +
                           (grid.Get(x, y + 1) == ores[k] ? 1 : 0) + (grid.Get(x, y - 1) == ores[k] ? 1 : 0);
                if (same == 0)
                    isolated++;
                else if (same == 4)
                    inner++;
            }
            for (int k = 0; k < 3; k++)
                Assert.Greater(counts[k], 100, $"O mundo tem veios de {ores[k]}.");
            Assert.Less(counts[2], counts[0], "O ouro é mais raro que o cobre.");
            int total = counts[0] + counts[1] + counts[2];
            Assert.Less(isolated, total / 50, "Os minérios vêm em bolsões, não em pontos soltos.");
            Assert.Greater(inner, total / 3, "Bolsões grossos: boa parte do minério está cercada de minério, não em trilhas finas.");
            for (int k = 0; k < 2; k++)
            {
                Assert.Greater(belowBand[k], 0, $"Há {ores[k]} no resto do mapa, abaixo da faixa principal.");
                Assert.Less(belowBand[k], counts[k] / 3, $"A maior parte do {ores[k]} fica na faixa principal.");
            }
            Assert.Greater(deepGold, counts[2] / 2, "O ouro se concentra no fundo.");

            var again = new TerrainGenerator(settings, 7, grid.Width, grid.Height, settings.StartArea, settings.SkyHeadroom);
            Assert.AreEqual(generator.Ores.CellCount, again.Ores.CellCount, "A mesma seed dá os mesmos veios.");
        }

        [Test]
        public void DugOre_GoesToTheOreDepot_WithoutMixingMetalsInTheHand()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Ore, 20);
            PaintDepot(grid, StorageKind.Blocks, 30);
            grid.Set(60, 4, CellMaterial.CopperOre);
            grid.Set(61, 4, CellMaterial.IronOre);
            grid.Set(62, 4, CellMaterial.CopperOre);
            grid.Set(63, 4, CellMaterial.GoldOre);
            for (int x = 60; x <= 63; x++)
                Assert.IsTrue(grid.SetMarked(x, 4, true));
            RoomLiningTests.LineAllRooms(grid);
            AntColony colony = MakeColony(grid);

            Assert.IsTrue(TickUntil(colony, () => colony.DepotStock(StorageKind.Ore) == 4, 160f), "O minério cavado vai ao depósito de minério.");
            Assert.AreEqual(0, colony.DepotStock(StorageKind.Blocks), "Minério não é terra.");
            int copper = 0, iron = 0, gold = 0;
            for (int x = 20; x <= 26; x++)
            for (int y = 1; y <= 3; y++)
            {
                byte item = grid.GetStored(x, y);
                if (item == (byte)CellMaterial.CopperOre) copper++;
                else if (item == (byte)CellMaterial.IronOre) iron++;
                else if (item == (byte)CellMaterial.GoldOre) gold++;
            }
            Assert.AreEqual(new[] { 2, 1, 1 }, new[] { copper, iron, gold }, "Cada minério guarda o metal dele.");
        }

        [Test]
        public void WithoutTheOreDepot_TheOreStaysOnTheGround_NeverAtTheDump()
        {
            PixelGrid grid = MakeWorld();
            grid.Set(60, 4, CellMaterial.IronOre);
            Assert.IsTrue(grid.SetMarked(60, 4, true));
            RoomLiningTests.LineAllRooms(grid);
            AntColony colony = MakeColony(grid);

            Assert.IsTrue(TickUntil(colony, () => grid.Get(60, 4) == CellMaterial.Empty, 60f), "O veio é cavado.");
            for (float t = 0f; t < 30f; t += Dt)
                colony.Tick(Dt);
            int onGround = 0;
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
                if (grid.GetStored(x, y) == (byte)CellMaterial.IronOre)
                    onGround++;
            int carried = 0;
            foreach (Ant ant in colony.Ants)
                if (ant.Carried == CellMaterial.IronOre)
                    carried++;
            Assert.AreEqual(1, onGround + carried, "Sem depósito, o minério fica no chão (ou na mão), não some no despejo.");
            Assert.AreEqual(0, colony.TotalDelivered, "Nada foi ao despejo.");
        }

        // ---------------------------------------------------------------- mundo de teste

        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);

        /// <summary>Solo até y = 20, corredor de x 10 a 100 (y 1 a 3), poço em x 50..51 e sala da rainha em x 88..98.</summary>
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

        static void PaintDepot(PixelGrid grid, StorageKind kind, int minX)
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
