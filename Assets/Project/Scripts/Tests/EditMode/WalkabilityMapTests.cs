using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// O cache de caminhabilidade usa uma cópia da regra que lê os arrays da grade direto (KI-004); ela tem de dar o mesmo
    /// que <see cref="AntNavigation.IsWalkable"/> em toda célula.
    /// </summary>
    public sealed class WalkabilityMapTests
    {
        static readonly CellMaterial[] Materials =
        {
            CellMaterial.Empty, CellMaterial.Empty, CellMaterial.Empty, CellMaterial.Dirt, CellMaterial.Sand,
            CellMaterial.Spoil, CellMaterial.PackedSpoil, CellMaterial.PackedSand, CellMaterial.Mud, CellMaterial.Rock,
            CellMaterial.Rubble, CellMaterial.PlantGrass, CellMaterial.PlantLeaf, CellMaterial.FallenLeaf,
            CellMaterial.FallenGrass, CellMaterial.Grass
        };

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void FastRule_MatchesAntNavigation_Everywhere(int seed)
        {
            const int w = 320, h = 200;
            var grid = new PixelGrid(w, h);
            var generator = new TerrainGenerator(new TerrainSettings(), seed, w, h, new RectInt(60, 40, 200, 160));
            generator.Begin(grid);
            var walk = new WalkabilityMap(grid);
            walk.Refresh();
            AssertSame(grid, walk, "depois de gerar a área inicial");

            // Bagunça: materiais, obra, arte do cenário e água em pontos sorteados, dentro e fora da área ativa.
            var rng = new System.Random(seed);
            for (int n = 0; n < 6000; n++)
            {
                int x = rng.Next(w), y = rng.Next(h);
                switch (rng.Next(4))
                {
                    case 0:
                        grid.Set(x, y, Materials[rng.Next(Materials.Length)]);
                        break;
                    case 1:
                        if (grid.IsSolid(x, y))
                            grid.SetBuilt(x, y, rng.Next(2) == 0);
                        break;
                    case 2:
                        grid.SetBackdrop(x, y, rng.Next(2) == 0);
                        break;
                    default:
                        grid.SetWater(x, y, rng.Next(2) == 0);
                        break;
                }
            }
            walk.Refresh();
            AssertSame(grid, walk, "depois das trocas sorteadas");

            generator.Expand(grid, new RectInt(0, 0, w, 1));
            walk.Refresh();
            AssertSame(grid, walk, "depois de expandir o mundo inteiro");
        }

        static void AssertSame(PixelGrid grid, WalkabilityMap walk, string when)
        {
            for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
            {
                if (walk.IsWalkable(x, y) != AntNavigation.IsWalkable(grid, x, y))
                    Assert.Fail($"Cache diverge da regra em ({x},{y}) {when}: material {grid.Get(x, y)}, solo {grid.GetSurfaceY(x)}.");
            }
        }
    }
}
