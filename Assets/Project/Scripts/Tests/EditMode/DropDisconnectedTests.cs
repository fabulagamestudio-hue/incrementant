using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// <see cref="VegetationSimulation.DropDisconnected"/> (busca que para ao achar a raiz) tem de soltar exatamente o que a
    /// versão que varria o pedaço inteiro soltava.
    /// </summary>
    public sealed class DropDisconnectedTests
    {
        const int Width = 60;
        const int Height = 50;
        const int Surface = 8;

        static PixelGrid MakeGrid(System.Random rng, double density)
        {
            var grid = new PixelGrid(Width, Height);
            for (int x = 0; x < Width; x++)
            {
                grid.SetSurfaceY(x, Surface);
                grid.Set(x, 0, CellMaterial.Bedrock);
                for (int y = 1; y <= Surface; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int y = Surface + 1; y < Height - 2; y++)
            for (int x = 1; x < Width - 1; x++)
            {
                if (rng.NextDouble() < density)
                    grid.Set(x, y, rng.Next(2) == 0 ? CellMaterial.PlantGrass : CellMaterial.PlantLeaf);
            }
            return grid;
        }

        // Versão de referência: varre o pedaço inteiro antes de decidir se tem raiz.
        static int ReferenceDrop(PixelGrid grid, int x, int y)
        {
            var visited = new HashSet<int>();
            var cells = new List<int>();
            var pending = new Stack<int>();
            int dropped = 0;
            for (int k = 0; k < 8; k++)
            {
                int nx = x + AntNavigation.Dx[k], ny = y + AntNavigation.Dy[k];
                if (!CellMaterials.IsPlant(grid.Get(nx, ny)) || visited.Contains(ny * grid.Width + nx))
                    continue;
                cells.Clear();
                bool rooted = false;
                pending.Push(ny * grid.Width + nx);
                visited.Add(ny * grid.Width + nx);
                while (pending.Count > 0)
                {
                    int i = pending.Pop();
                    int cx = i % grid.Width, cy = i / grid.Width;
                    cells.Add(i);
                    rooted |= cy <= grid.GetSurfaceY(cx) + 1;
                    for (int d = 0; d < 8; d++)
                    {
                        int mx = cx + AntNavigation.Dx[d], my = cy + AntNavigation.Dy[d];
                        int j = my * grid.Width + mx;
                        if (CellMaterials.IsPlant(grid.Get(mx, my)) && visited.Add(j))
                            pending.Push(j);
                    }
                }
                if (rooted)
                    continue;
                foreach (int i in cells)
                {
                    int cx = i % grid.Width, cy = i / grid.Width;
                    CellMaterial debris = CellMaterials.ToDebris(grid.Get(cx, cy));
                    byte shade = grid.GetShade(cx, cy);
                    grid.Set(cx, cy, CellMaterial.Empty, 0);
                    grid.Set(cx, cy, debris, shade);
                    grid.SetMarked(cx, cy, true);
                    dropped++;
                }
            }
            return dropped;
        }

        [TestCase(1, 0.45)]
        [TestCase(2, 0.60)]
        [TestCase(3, 0.35)]
        [TestCase(4, 0.75)]
        public void Drops_ExactlyWhatTheFullScanDropped(int seed, double density)
        {
            var rngA = new System.Random(seed);
            var rngB = new System.Random(seed);
            PixelGrid fast = MakeGrid(rngA, density);
            PixelGrid reference = MakeGrid(rngB, density);
            var pick = new System.Random(seed + 100);
            int totalDropped = 0;

            for (int step = 0; step < 300; step++)
            {
                int x = pick.Next(1, Width - 1);
                int y = pick.Next(Surface + 2, Height - 2);
                if (!CellMaterials.IsPlant(fast.Get(x, y)))
                    continue;
                fast.Set(x, y, CellMaterial.Empty, 0);
                reference.Set(x, y, CellMaterial.Empty, 0);
                int a = VegetationSimulation.DropDisconnected(fast, x, y);
                int b = ReferenceDrop(reference, x, y);
                totalDropped += a;
                Assert.AreEqual(b, a, $"passo {step}: pixels soltos em ({x},{y})");
                for (int yy = 0; yy < Height; yy++)
                for (int xx = 0; xx < Width; xx++)
                {
                    if (fast.Get(xx, yy) != reference.Get(xx, yy) || fast.IsMarked(xx, yy) != reference.IsMarked(xx, yy))
                        Assert.Fail($"passo {step}: ({xx},{yy}) difere depois do corte em ({x},{y})");
                }
            }
            Assert.Greater(totalDropped, 0, "O teste precisa soltar pedaços para valer.");
        }
    }
}
