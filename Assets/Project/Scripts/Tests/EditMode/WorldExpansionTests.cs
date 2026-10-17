using System.Collections.Generic;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    public sealed class WorldExpansionTests
    {
        const int WorldWidth = 200;
        const int WorldHeight = 160;
        static readonly RectInt StartArea = new RectInt(60, 60, 80, 100);

        static (PixelGrid grid, TerrainGenerator generator) Begin(int seed)
        {
            var grid = new PixelGrid(WorldWidth, WorldHeight);
            var generator = new TerrainGenerator(new TerrainSettings(), seed, WorldWidth, WorldHeight, StartArea);
            generator.Begin(grid);
            return (grid, generator);
        }

        [Test]
        public void Begin_ActivatesOnlyTheStartArea()
        {
            var (grid, _) = Begin(1);

            Assert.AreEqual(StartArea, grid.Active);
            Assert.IsFalse(grid.InBounds(StartArea.xMin - 1, StartArea.yMin + 10));
            Assert.AreEqual(CellMaterial.Bedrock, grid.Get(StartArea.xMin - 1, StartArea.yMin + 10), "Fora da área gerada vale como rocha-mãe.");
            Assert.AreEqual(CellMaterial.Bedrock, grid.Get(StartArea.xMin + 10, StartArea.yMin - 1));
            for (int x = 0; x < WorldWidth; x++)
                Assert.Greater(grid.GetSurfaceY(x), 0, "O relevo vale para o mundo inteiro desde o começo.");
        }

        [Test]
        public void Expand_InAnyOrder_MatchesExpandingAllAtOnce()
        {
            var (stepwise, stepGen) = Begin(42);
            stepGen.Expand(stepwise, new RectInt(StartArea.xMin - 32, StartArea.yMin, 32, 1));
            stepGen.Expand(stepwise, new RectInt(StartArea.xMin, StartArea.yMin - 32, 1, 1));
            stepGen.Expand(stepwise, new RectInt(StartArea.xMax, 0, 40, 1));
            stepGen.Expand(stepwise, new RectInt(0, 0, WorldWidth, 1));

            var (once, onceGen) = Begin(42);
            onceGen.Expand(once, new RectInt(0, 0, WorldWidth, 1));

            Assert.AreEqual(new RectInt(0, 0, WorldWidth, WorldHeight), stepwise.Active);
            for (int y = 0; y < WorldHeight; y++)
            for (int x = 0; x < WorldWidth; x++)
            {
                Assert.AreEqual(once.Get(x, y), stepwise.Get(x, y), $"Material diferente em ({x}, {y}).");
                Assert.AreEqual(once.GetShade(x, y), stepwise.GetShade(x, y), $"Tom diferente em ({x}, {y}).");
            }
        }

        [Test]
        public void Expand_PutsBedrockOnTheWorldLimits()
        {
            var (grid, generator) = Begin(3);
            generator.Expand(grid, new RectInt(0, 0, WorldWidth, 1));

            for (int y = 0; y < WorldHeight; y++)
            {
                Assert.AreEqual(CellMaterial.Bedrock, grid.Get(0, y));
                Assert.AreEqual(CellMaterial.Bedrock, grid.Get(WorldWidth - 1, y));
            }
            for (int x = 0; x < WorldWidth; x++)
                Assert.AreEqual(CellMaterial.Bedrock, grid.Get(x, 0));
        }

        [Test]
        public void Expand_OnlyGrowsAndChangesStructureVersion()
        {
            var (grid, generator) = Begin(5);
            int version = grid.StructureVersion;

            Assert.IsFalse(generator.Expand(grid, new RectInt(StartArea.xMin + 5, StartArea.yMin + 5, 4, 4)), "Área já gerada não muda nada.");
            Assert.AreEqual(version, grid.StructureVersion);

            Assert.IsTrue(generator.Expand(grid, new RectInt(StartArea.xMin - 10, StartArea.yMin, 1, 1)));
            Assert.AreNotEqual(version, grid.StructureVersion, "Área nova precisa invalidar a navegação.");
            Assert.AreEqual(StartArea.xMin - 10, grid.Active.xMin);
            Assert.AreEqual(WorldHeight, grid.Active.yMax);
        }

        [Test]
        public void FlowField_DoesNotLeaveTheActiveArea()
        {
            // Sem rocha-mãe nas bordas da área ativa: só o limite dela segura o BFS.
            var grid = new PixelGrid(30, 10);
            grid.SetActive(new RectInt(10, 0, 10, 10));
            for (int x = 0; x < 30; x++)
                grid.Set(x, 0, CellMaterial.Rock);

            var field = new FlowField(grid.Width, grid.Height);
            field.Build(grid, new List<int> { grid.ToIndex(15, 1) });

            Assert.AreNotEqual(FlowField.Unreachable, field.Get(19, 1));
            Assert.AreEqual(FlowField.Unreachable, field.Get(20, 1));
            Assert.AreEqual(FlowField.Unreachable, field.Get(9, 1));
        }

        [Test]
        public void Sand_DoesNotFallOutOfTheActiveArea()
        {
            var grid = new PixelGrid(10, 20);
            grid.SetActive(new RectInt(0, 10, 10, 10));
            grid.Set(5, 12, CellMaterial.Sand);
            var sim = new GranularSimulation();
            for (int i = 0; i < 10; i++)
                sim.Step(grid);
            Assert.AreEqual(CellMaterial.Sand, grid.Get(5, 10), "O grão para na primeira linha da área ativa.");
        }

        [Test]
        public void ClampCenter_KeepsTheViewInsideTheWorld()
        {
            var bounds = new Rect(0f, 0f, 400f, 300f);

            Vector2 c = MapCamera.ClampCenter(new Vector2(-50f, 500f), 40f, 30f, bounds);
            Assert.AreEqual(new Vector2(40f, 270f), c);

            c = MapCamera.ClampCenter(new Vector2(390f, 10f), 40f, 30f, bounds);
            Assert.AreEqual(new Vector2(360f, 30f), c);

            // Vista maior que o mundo num eixo: centraliza nesse eixo.
            c = MapCamera.ClampCenter(new Vector2(10f, 10f), 250f, 30f, bounds);
            Assert.AreEqual(200f, c.x);
        }

        [Test]
        public void TerrainSettings_StartAreaIsTopCenterOfTheWorld()
        {
            var settings = new TerrainSettings();
            RectInt start = settings.StartArea;
            Assert.AreEqual(settings.WorldHeight, start.yMax, "A área inicial encosta no topo (céu).");
            Assert.AreEqual(settings.WorldWidth - start.xMax, start.xMin, "A área inicial fica centralizada.");
            Assert.GreaterOrEqual(settings.WorldWidth, settings.Width);
            Assert.GreaterOrEqual(settings.WorldHeight, settings.Height);
        }
    }
}
