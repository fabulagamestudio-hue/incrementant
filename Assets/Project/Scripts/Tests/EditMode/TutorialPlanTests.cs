using Fabula.Incrementant.Tutorial;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    public sealed class TutorialPlanTests
    {
        static PixelGrid NewWorld(int seed, out TerrainLayout layout)
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.WorldWidth, settings.WorldHeight);
            var generator = new TerrainGenerator(settings, seed, grid.Width, grid.Height, settings.StartArea, settings.SkyHeadroom);
            layout = generator.Begin(grid);
            return grid;
        }

        [Test]
        public void ShelterBounds_CoversTheCarvedChamberAndNotTheWholeShaft()
        {
            PixelGrid grid = NewWorld(7, out TerrainLayout layout);
            RectInt shelter = TutorialPlanner.ShelterBounds(grid, layout.Nest);
            Assert.GreaterOrEqual(shelter.width, 15);
            Assert.LessOrEqual(shelter.width, 25);
            Assert.LessOrEqual(shelter.height, 16);
            Assert.IsTrue(shelter.Contains(layout.Nest));
        }

        [Test]
        public void Plans_AreConnectedToTheShelterAndDoNotOverlap()
        {
            for (int seed = 1; seed <= 12; seed++)
            {
                PixelGrid grid = NewWorld(seed, out TerrainLayout layout);
                RectInt shelter = TutorialPlanner.ShelterBounds(grid, layout.Nest);
                DigArea a = TutorialPlanner.Choose(grid, layout.Nest, shelter);
                Assert.NotNull(a, $"seed {seed}");
                DigArea b = TutorialPlanner.Choose(grid, layout.Nest, shelter, new[] { a.Bounds });
                Assert.NotNull(b, $"seed {seed}");
                Assert.AreNotEqual(a.Side, b.Side, $"seed {seed}: as câmaras ficam em lados opostos");
                Assert.IsFalse(a.Bounds.Overlaps(b.Bounds), $"seed {seed}");
                Assert.Less(a.Penalty, 100, $"seed {seed}: a primeira câmara não deve romper a superfície");
                Assert.Greater(a.DigCells.Length, 30, $"seed {seed}");
            }
        }

        [Test]
        public void DiggingEveryPlannedCell_CompletesTheStep_AndNotBefore()
        {
            PixelGrid grid = NewWorld(3, out TerrainLayout layout);
            RectInt shelter = TutorialPlanner.ShelterBounds(grid, layout.Nest);
            DigArea area = TutorialPlanner.Choose(grid, layout.Nest, shelter);

            Assert.IsFalse(TutorialPlanner.IsDugAndReachable(grid, layout.Nest, shelter, area));
            Assert.AreEqual(0, TutorialPlanner.MarkedCount(grid, area));

            // Marcar as células conta para a marcação, mas só escavar conclui.
            foreach (Vector2Int c in area.DigCells)
                grid.SetMarked(c.x, c.y, true);
            Assert.AreEqual(1f, TutorialPlanner.MarkedOrOpenFraction(grid, area), 1e-4f);
            Assert.IsFalse(TutorialPlanner.IsDugAndReachable(grid, layout.Nest, shelter, area));

            foreach (Vector2Int c in area.DigCells)
                grid.Set(c.x, c.y, CellMaterial.Empty, 0);
            Assert.IsTrue(TutorialPlanner.IsDugAndReachable(grid, layout.Nest, shelter, area));
        }

        [Test]
        public void ADugChamberCutOffFromTheShelter_DoesNotCount()
        {
            PixelGrid grid = NewWorld(3, out TerrainLayout layout);
            RectInt shelter = TutorialPlanner.ShelterBounds(grid, layout.Nest);
            DigArea area = TutorialPlanner.Choose(grid, layout.Nest, shelter);
            foreach (Vector2Int c in area.ChamberCells)
                grid.Set(c.x, c.y, CellMaterial.Empty, 0);
            // Só a câmara foi cavada, sem o túnel: ainda não está concluída.
            Assert.IsFalse(TutorialPlanner.IsDugAndReachable(grid, layout.Nest, shelter, area));
            foreach (Vector2Int c in area.DigCells)
                grid.Set(c.x, c.y, CellMaterial.Empty, 0);
            Assert.IsTrue(TutorialPlanner.IsDugAndReachable(grid, layout.Nest, shelter, area));
        }

        [Test]
        public void PaintedCount_OnlyCountsTheChamberInterior()
        {
            PixelGrid grid = NewWorld(3, out TerrainLayout layout);
            RectInt shelter = TutorialPlanner.ShelterBounds(grid, layout.Nest);
            DigArea area = TutorialPlanner.Choose(grid, layout.Nest, shelter);
            foreach (Vector2Int c in area.ChamberCells)
                grid.Set(c.x, c.y, CellMaterial.Empty, 0);
            for (int i = 0; i < 10; i++)
                grid.SetNutrientRoom(area.ChamberCells[i].x, area.ChamberCells[i].y, true);
            grid.SetNutrientRoom(layout.Nest.x, layout.Nest.y, true); // no abrigo: não conta
            Assert.AreEqual(10, TutorialPlanner.PaintedCount(grid, area, (g, x, y) => g.IsNutrientRoom(x, y)));
        }
    }
}
