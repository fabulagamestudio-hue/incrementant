using System.IO;
using System.Linq;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    public sealed class SaveLoadTests
    {
        const int Seed = 4321;

        static (PixelGrid grid, AntColony colony, RainSimulation rain) MakeWorld()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout = TerrainGenerator.Generate(grid, settings, Seed);
            var colony = new AntColony(grid, new ColonySettings(), layout, Seed);
            var rain = new RainSimulation(new RainSettings(), Seed);
            return (grid, colony, rain);
        }

        static WaterSimulation Water(PixelGrid grid) => new WaterSimulation(new WaterSettings(), grid, Seed);

        [Test]
        public void Snapshot_RoundTripsGridColonyAndRain()
        {
            var (grid, colony, rain) = MakeWorld();
            Vector2Int nest = colony.Layout.Nest;
            for (int dx = -3; dx <= 3; dx++)
                grid.SetMarked(nest.x + 6 + dx, nest.y - 4, true);
            grid.Set(nest.x + 10, nest.y + 1, CellMaterial.Spoil);
            grid.SetWetness(nest.x + 10, nest.y + 1, 2);
            grid.SetWater(nest.x, nest.y + 2, true);
            WaterSimulation water = Water(grid);
            rain.Weather = Weather.LightRain;
            for (int i = 0; i < 120; i++)
            {
                rain.Step(grid, 1f / 60f);
                water.Step(grid, 1f / 60f);
                colony.Tick(1f / 60f);
            }
            colony.TryBuyAnt();

            GameSnapshot saved = GameSnapshot.Capture(grid, colony, rain, water, 0, Seed, 125.5);
            var stream = new MemoryStream();
            saved.Write(stream);
            stream.Position = 0;
            GameSnapshot loaded = GameSnapshot.Read(stream);

            var (grid2, colony2, rain2) = MakeWorld();
            loaded.ApplyTo(grid2);
            loaded.ApplyTo(rain2);
            WaterSimulation water2 = Water(grid2);
            loaded.ApplyTo(water2);
            colony2 = new AntColony(grid2, new ColonySettings(), colony.Layout, Seed);
            loaded.ApplyTo(colony2);

            Assert.AreEqual(125.5, loaded.MatchSeconds);
            Assert.AreEqual(grid.MarkedCount, grid2.MarkedCount);
            Assert.AreEqual(grid.WetCount, grid2.WetCount);
            Assert.Greater(grid.WaterCount, 0, "O teste deveria ter água para salvar.");
            Assert.AreEqual(grid.WaterCount, grid2.WaterCount);
            Assert.AreEqual(rain.WaterCells, rain2.WaterCells);
            Assert.AreEqual(water.ErodedCells, water2.ErodedCells);
            RectInt area = grid.Active;
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
            {
                Assert.AreEqual(grid.Get(x, y), grid2.Get(x, y), $"material em ({x},{y})");
                Assert.AreEqual(grid.GetShade(x, y), grid2.GetShade(x, y));
                Assert.AreEqual(grid.IsMarked(x, y), grid2.IsMarked(x, y));
                Assert.AreEqual(grid.GetWetness(x, y), grid2.GetWetness(x, y));
                Assert.AreEqual(grid.HasWater(x, y), grid2.HasWater(x, y), $"água em ({x},{y})");
            }
            Assert.AreEqual(Weather.LightRain, rain2.Weather);
            Assert.AreEqual(colony.Ants.Count, colony2.Ants.Count);
            Assert.AreEqual(colony.StoredPixels, colony2.StoredPixels);
            Assert.AreEqual(colony.TotalDelivered, colony2.TotalDelivered);
            Assert.AreEqual(colony.SimulationTime, colony2.SimulationTime);
            Assert.IsTrue(colony.Ants.Zip(colony2.Ants, (a, b) => a.X == b.X && a.Y == b.Y && a.State == b.State).All(same => same));
        }

        [Test]
        public void Snapshot_RestoredColonyKeepsRunning()
        {
            var (grid, colony, rain) = MakeWorld();
            GameSnapshot snapshot = GameSnapshot.Capture(grid, colony, rain, Water(grid), 0, Seed, 0.0);
            var (grid2, _, _) = MakeWorld();
            snapshot.ApplyTo(grid2);
            var colony2 = new AntColony(grid2, new ColonySettings(), colony.Layout, Seed);
            snapshot.ApplyTo(colony2);

            for (int i = 0; i < 300; i++)
                colony2.Tick(1f / 60f);

            Assert.AreEqual(colony.Ants.Count, colony2.Ants.Count);
        }

        [Test]
        public void Read_RejectsGarbage()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            Assert.Throws<InvalidDataException>(() => GameSnapshot.Read(stream));
        }

        [Test]
        public void Read_RejectsTruncatedFile()
        {
            var (grid, colony, rain) = MakeWorld();
            var stream = new MemoryStream();
            GameSnapshot.Capture(grid, colony, rain, Water(grid), 0, Seed, 1.0).Write(stream);
            byte[] bytes = stream.ToArray();
            var truncated = new MemoryStream(bytes, 0, bytes.Length / 2);
            Assert.That(() => GameSnapshot.Read(truncated), Throws.Exception);
        }

        [Test]
        public void MatchTimer_AccumulatesAndFormats()
        {
            var timer = new MatchTimer();
            timer.Advance(61.5f);
            timer.Advance(-3f);
            Assert.AreEqual(61.5, timer.Seconds, 1e-6);
            Assert.AreEqual("01:01", MatchTimer.Format(timer.Seconds));
            Assert.AreEqual("1:01:05", MatchTimer.Format(3665));
            timer.Reset();
            Assert.AreEqual(0.0, timer.Seconds);
        }
    }
}
