using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// <see cref="FlowField.Update"/> tem de dar exatamente o campo que <see cref="FlowField.Build"/> daria sobre o mesmo
    /// estado, qualquer que seja a sequência de mudanças de terreno e de origens.
    /// </summary>
    public sealed class FlowFieldIncrementalTests
    {
        const int Width = 70;
        const int Height = 40;
        const int Surface = 22;

        static PixelGrid MakeGrid(System.Random rng, float fill)
        {
            var grid = new PixelGrid(Width, Height);
            for (int x = 0; x < Width; x++)
            {
                grid.SetSurfaceY(x, Surface);
                grid.Set(x, 0, CellMaterial.Bedrock);
            }
            for (int y = 1; y <= Surface; y++)
            for (int x = 0; x < Width; x++)
            {
                if (rng.NextDouble() < fill)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            return grid;
        }

        static void Compare(FlowField incremental, FlowField fresh, string context)
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (incremental.Get(x, y) != fresh.Get(x, y))
                    Assert.Fail($"{context}: ({x},{y}) incremental={incremental.Get(x, y)} cheio={fresh.Get(x, y)}");
            }
            Assert.AreEqual(fresh.ReachedCount, incremental.ReachedCount, $"{context}: células alcançadas");
        }

        [TestCase(1, 0.55f)]
        [TestCase(2, 0.30f)]
        [TestCase(3, 0.75f)]
        [TestCase(4, 0.45f)]
        [TestCase(5, 0.60f)]
        public void Update_MatchesBuild_AfterRandomTerrainAndSourceChanges(int seed, float fill)
        {
            var rng = new System.Random(seed);
            PixelGrid grid = MakeGrid(rng, fill);
            var walk = new WalkabilityMap(grid);
            walk.Refresh();
            var sources = new List<int>();
            for (int i = 0; i < 6; i++)
                sources.Add(rng.Next(1, Surface) * Width + rng.Next(Width));

            var field = new FlowField(Width, Height);
            field.Update(walk, sources); // primeira vez: cheio
            walk.ClearChangedCells();
            var fresh = new FlowField(Width, Height);
            fresh.Build(walk, sources);
            Compare(field, fresh, "início");

            int incremental = 0;
            for (int step = 0; step < 250; step++)
            {
                int edits = 1 + rng.Next(6);
                for (int e = 0; e < edits; e++)
                {
                    int x = rng.Next(Width);
                    // Também acima do solo (grão no céu) e a parede da base fica de fora.
                    int y = 1 + rng.Next(Surface + 8);
                    CellMaterial now = grid.Get(x, y);
                    grid.Set(x, y, now == CellMaterial.Empty ? CellMaterial.Dirt : CellMaterial.Empty);
                }
                walk.Refresh();
                if (rng.Next(4) == 0)
                {
                    // Origens: tira uma ou põe outra (inclusive em célula sólida, como a rocha marcada).
                    if (sources.Count > 0 && rng.Next(2) == 0)
                        sources.RemoveAt(rng.Next(sources.Count));
                    else
                        sources.Add(rng.Next(1, Surface + 8) * Width + rng.Next(Width));
                }
                if (rng.Next(30) == 0)
                    sources.Clear(); // sem origem nenhuma: o campo esvazia (caminho direto do Update)
                field.Update(walk, sources);
                incremental += field.LastUpdateWasIncremental ? 1 : 0;
                walk.ClearChangedCells();

                fresh.Build(walk, sources);
                Compare(field, fresh, $"seed {seed}, passo {step}");
            }
            Assert.Greater(incremental, 200, "O teste precisa exercitar o caminho incremental, não o Build de reserva.");
        }

        [Test]
        public void Update_WithNoChanges_KeepsTheField()
        {
            var rng = new System.Random(9);
            PixelGrid grid = MakeGrid(rng, 0.5f);
            var walk = new WalkabilityMap(grid);
            walk.Refresh();
            var sources = new List<int> { 5 * Width + 5, 10 * Width + 40 };
            var field = new FlowField(Width, Height);
            field.Build(walk, sources);
            walk.ClearChangedCells();
            var before = new int[Width * Height];
            for (int i = 0; i < before.Length; i++)
                before[i] = field.Get(i % Width, i / Width);

            field.Update(walk, sources);
            for (int i = 0; i < before.Length; i++)
                Assert.AreEqual(before[i], field.Get(i % Width, i / Width));
        }
    }
}
