using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// As otimizações por bloco (blocos dormindo na simulação granular, cache de caminhabilidade) não podem mudar o
    /// resultado: comparam com a varredura completa e com a regra exata, célula a célula.
    /// </summary>
    public sealed class ChunkedSimulationTests
    {
        static PixelGrid MakeTerrain(int seed)
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainGenerator.Generate(grid, settings, seed);
            return grid;
        }

        /// <summary>Simula formigas: cava buracos (a areia em volta desaba) e despeja grãos no céu.</summary>
        static void Disturb(PixelGrid grid, System.Random rng)
        {
            int cx = rng.Next(4, grid.Width - 4);
            int cy = rng.Next(4, grid.Height - 4);
            int r = rng.Next(1, 5);
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (grid.Get(cx + dx, cy + dy) != CellMaterial.Bedrock)
                    grid.Set(cx + dx, cy + dy, CellMaterial.Empty, 0);
            }

            int x = rng.Next(2, grid.Width - 2);
            int y = grid.Height - 2;
            if (!grid.IsSolid(x, y))
                grid.Set(x, y, rng.Next(2) == 0 ? CellMaterial.Sand : CellMaterial.Spoil, (byte)rng.Next(256));
            grid.SetMarked(rng.Next(grid.Width), rng.Next(grid.Height), true);
        }

        [Test]
        public void GranularStep_MatchesFullScan()
        {
            PixelGrid fast = MakeTerrain(77);
            PixelGrid reference = MakeTerrain(77);
            var simulation = new GranularSimulation();
            var fullScan = new FullScanReference();
            var rngFast = new System.Random(5);
            var rngReference = new System.Random(5);

            for (int step = 0; step < 400; step++)
            {
                if (step % 3 == 0)
                {
                    Disturb(fast, rngFast);
                    Disturb(reference, rngReference);
                }
                simulation.Step(fast);
                fullScan.Step(reference);
            }

            for (int y = 0; y < fast.Height; y++)
            for (int x = 0; x < fast.Width; x++)
            {
                Assert.AreEqual(reference.Get(x, y), fast.Get(x, y), $"Material diferente em ({x},{y}).");
                Assert.AreEqual(reference.GetShade(x, y), fast.GetShade(x, y), $"Tom diferente em ({x},{y}).");
                Assert.AreEqual(reference.IsMarked(x, y), fast.IsMarked(x, y), $"Marca diferente em ({x},{y}).");
            }
            Assert.AreEqual(reference.MarkedCount, fast.MarkedCount);
        }

        [Test]
        public void WalkabilityMap_MatchesExactRule_AfterChanges()
        {
            PixelGrid grid = MakeTerrain(31);
            var map = new WalkabilityMap(grid);
            var simulation = new GranularSimulation();
            var rng = new System.Random(9);
            map.Refresh();

            for (int round = 0; round < 30; round++)
            {
                Disturb(grid, rng);
                for (int i = 0; i < 5; i++)
                    simulation.Step(grid);
                map.Refresh();
            }

            for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
                Assert.AreEqual(AntNavigation.IsWalkable(grid, x, y), map.IsWalkableAt(grid.ToIndex(x, y)), $"Caminhabilidade em ({x},{y}).");
        }

        [Test]
        public void WalkabilityMap_FollowsActiveAreaExpansion()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.WorldWidth, settings.WorldHeight);
            var generator = new TerrainGenerator(settings, 3, grid.Width, grid.Height, settings.StartArea);
            generator.Begin(grid);
            var map = new WalkabilityMap(grid);
            map.Refresh();

            RectInt start = settings.StartArea;
            generator.Expand(grid, new RectInt(start.xMin - 40, start.yMin - 40, start.width + 80, start.height + 40));
            map.Refresh();

            for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
                Assert.AreEqual(AntNavigation.IsWalkable(grid, x, y), map.IsWalkableAt(grid.ToIndex(x, y)), $"Caminhabilidade em ({x},{y}).");
        }

        [Test]
        public void FlowField_Rebuilt_MatchesFreshField()
        {
            PixelGrid grid = MakeTerrain(8);
            var map = new WalkabilityMap(grid);
            var reused = new FlowField(grid.Width, grid.Height);
            var rng = new System.Random(4);
            var sources = new System.Collections.Generic.List<int>();

            for (int round = 0; round < 10; round++)
            {
                Disturb(grid, rng);
                map.Refresh();
                sources.Clear();
                for (int i = 0; i < grid.Width * grid.Height && sources.Count < 3; i += 997)
                    if (map.IsWalkableAt(i)) sources.Add(i);
                reused.Build(map, sources);
            }

            var fresh = new FlowField(grid.Width, grid.Height);
            fresh.Build(map, sources);
            for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
                Assert.AreEqual(fresh.Get(x, y), reused.Get(x, y), $"Distância em ({x},{y}).");
        }

        [Test]
        public void SettledSand_PutsChunksToSleep()
        {
            PixelGrid grid = MakeTerrain(12);
            var simulation = new GranularSimulation();
            for (int i = 0; i < 600; i++)
                simulation.Step(grid);

            int awake = 0;
            for (int cy = 0; cy < grid.ChunkRows; cy++)
            for (int cx = 0; cx < grid.ChunkColumns; cx++)
                if (grid.IsChunkAwake(cx, cy)) awake++;
            Assert.AreEqual(0, awake, "Sem nada caindo, nenhum bloco deveria continuar acordado.");
        }

        /// <summary>A varredura completa original, só com a API pública, como referência do resultado esperado.</summary>
        sealed class FullScanReference
        {
            int _tick;

            public void Step(PixelGrid grid)
            {
                _tick++;
                RectInt area = grid.Active;
                for (int y = area.yMin + 1; y < area.yMax; y++)
                {
                    bool leftToRight = ((y + _tick) & 1) == 0;
                    for (int k = 0; k < area.width; k++)
                    {
                        int x = leftToRight ? area.xMin + k : area.xMax - 1 - k;
                        if (!CellMaterials.IsGranular(grid.Get(x, y)))
                            continue;
                        if (grid.Get(x, y - 1) == CellMaterial.Empty)
                        {
                            Move(grid, x, y, x, y - 1);
                            continue;
                        }
                        int first = ((x ^ y ^ _tick) & 1) == 0 ? -1 : 1;
                        if (!TryDiagonal(grid, x, y, first))
                            TryDiagonal(grid, x, y, -first);
                    }
                }
            }

            static bool TryDiagonal(PixelGrid grid, int x, int y, int dx)
            {
                int nx = x + dx;
                if (grid.Get(nx, y) != CellMaterial.Empty || grid.Get(nx, y - 1) != CellMaterial.Empty)
                    return false;
                Move(grid, x, y, nx, y - 1);
                return true;
            }

            static void Move(PixelGrid grid, int x, int y, int tx, int ty)
            {
                bool marked = grid.IsMarked(x, y);
                grid.Set(tx, ty, grid.Get(x, y), grid.GetShade(x, y));
                grid.Set(x, y, CellMaterial.Empty, 0);
                if (marked)
                    grid.SetMarked(tx, ty, true);
            }
        }
    }
}
