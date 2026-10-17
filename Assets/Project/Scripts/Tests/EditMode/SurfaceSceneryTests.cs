using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    public sealed class SurfaceSceneryTests
    {
        static readonly Color32 Sky = new Color32(120, 170, 215, 255);

        static (PixelGrid grid, TerrainLayout layout) MakeMap(int seed)
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout = TerrainGenerator.Generate(grid, settings, seed);
            return (grid, layout);
        }

        [Test]
        public void Layers_CoverTheSkyAtAntResolution()
        {
            var (grid, layout) = MakeMap(7);
            SceneryLayers layers = SurfaceSceneryPainter.Paint(grid, layout, new SurfaceScenerySettings(), 7, Sky, Sky);

            int ppc = SurfaceSceneryPainter.PixelsPerCell;
            Assert.AreEqual(grid.Width * ppc, layers.Near.Width);
            Assert.AreEqual((grid.Height - layers.Near.OriginY) * ppc, layers.Near.Height);
            for (int x = 0; x < grid.Width; x++)
                Assert.LessOrEqual(layers.Near.OriginY, grid.GetSurfaceY(x), "A camada precisa começar abaixo do solo de todas as colunas.");

            foreach (Color32 c in layers.Sky.Pixels)
                Assert.AreEqual(255, c.a, "O céu precisa ser opaco: o céu do terreno fica transparente.");
            Assert.LessOrEqual(layers.Sky.OriginY, layers.Near.OriginY, "O céu precisa descer até a base do cenário.");
        }

        [Test]
        public void DistantBands_AreSeamlessTilesWithOpaqueField()
        {
            var (grid, layout) = MakeMap(9);
            SceneryLayers layers = SurfaceSceneryPainter.Paint(grid, layout, new SurfaceScenerySettings(), 9, Sky, Sky);

            foreach (SceneryLayer band in new[] { layers.Far, layers.Mid })
            {
                Assert.IsTrue(band.WrapX, "As faixas distantes são ladrilhos repetidos na horizontal.");
                Assert.AreEqual(SurfaceSceneryPainter.BandTileCells * band.PixelsPerCell, band.Width);
                // O campo liso embaixo cobre o vão quando a câmera sobe (paralaxe): a base inteira é opaca.
                for (int px = 0; px < band.Width; px++)
                    Assert.AreEqual(255, band.Get(px, 0).a, $"Furo no campo da faixa distante em px = {px}.");
                // A base fica BandFill células sob o horizonte (solo da entrada).
                Assert.AreEqual(grid.GetSurfaceY(layout.EntranceX) + 1 - SurfaceSceneryPainter.BandFill, band.OriginY);
            }
        }

        [Test]
        public void Near_HasGrassTallerThanAnAnt()
        {
            var (grid, layout) = MakeMap(11);
            SceneryLayers layers = SurfaceSceneryPainter.Paint(grid, layout, new SurfaceScenerySettings(), 11, Sky, Sky);
            SceneryLayer near = layers.Near;

            // Algum pixel pintado a mais de 20 células acima do solo da coluna (a formiga tem 2 de altura).
            bool tall = false;
            for (int py = 0; py < near.Height && !tall; py++)
            for (int px = 0; px < near.Width; px++)
            {
                if (near.Get(px, py).a == 0)
                    continue;
                float y = near.OriginY + py / (float)near.PixelsPerCell;
                if (y > grid.GetSurfaceY(px / near.PixelsPerCell) + 20f)
                {
                    tall = true;
                    break;
                }
            }
            Assert.IsTrue(tall);
        }

        [Test]
        public void Front_LeavesEntranceAndDumpClear()
        {
            var (grid, layout) = MakeMap(3);
            var settings = new SurfaceScenerySettings();
            SceneryLayer front = SurfaceSceneryPainter.Paint(grid, layout, settings, 3, Sky, Sky).Front;

            // Tufos têm até ~3 células de espalhamento e ~6 de inclinação; a folga padrão (12) cobre isso.
            int margin = settings.ForegroundClearance - 10;
            for (int py = 0; py < front.Height; py++)
            for (int px = 0; px < front.Width; px++)
            {
                if (front.Get(px, py).a == 0)
                    continue;
                int x = px / front.PixelsPerCell;
                Assert.IsFalse(Mathf.Abs(x - layout.EntranceX) < margin, $"Tufo da frente sobre a entrada em x = {x}.");
                Assert.IsFalse(x > layout.DumpMinX - margin && x < layout.DumpMaxX + margin, $"Tufo da frente sobre o despejo em x = {x}.");
            }
        }

        [Test]
        public void Elements_AreIsolatedStampsWithPivotOnTheGround()
        {
            int ppc = SurfaceSceneryPainter.PixelsPerCell;
            foreach (SceneryElement element in System.Enum.GetValues(typeof(SceneryElement)))
            {
                SceneryStamp stamp = SurfaceSceneryPainter.PaintElement(element, 4);
                Assert.Greater(stamp.Width * stamp.Height, 0, element.ToString());
                Assert.That(stamp.PivotX, Is.InRange(0, stamp.Width), $"{element}: pivô fora da largura.");
                // Recorte justo: no máximo ~1 célula enterrada e o resto acima do chão.
                Assert.That(stamp.PivotY, Is.InRange(0, ppc + 1), $"{element}: pivô precisa ficar na base do recorte.");
                Assert.Less(stamp.PivotY, stamp.Height, $"{element}: o elemento precisa aparecer acima do chão.");
            }
        }

        [Test]
        public void Paint_PlacesStampsFromTheGivenLibrary()
        {
            var (grid, layout) = MakeMap(13);
            var red = new Color32(255, 0, 0, 255);
            var pixels = new Color32[4 * 4];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = red;
            var art = new SceneryStampLibrary();
            art.Set(SceneryElement.Grass, new[] { new SceneryStamp(4, 4, pixels, 2, 0) });

            SceneryLayer near = SurfaceSceneryPainter.Paint(grid, layout, new SurfaceScenerySettings(), 13, Sky, Sky, art: art).Near;
            int redCount = 0;
            foreach (Color32 c in near.Pixels)
                if (c.Equals(red))
                    redCount++;
            Assert.Greater(redCount, 0, "A grama precisa vir da biblioteca dada.");

            // Os outros elementos, sem carimbos na biblioteca, caem na arte procedural.
            SceneryLayer fallback = SurfaceSceneryPainter.Paint(grid, layout, new SurfaceScenerySettings(), 13, Sky, Sky).Near;
            Assert.IsTrue(System.Array.Exists(fallback.Pixels, c => c.a > 0));
        }

        [Test]
        public void SameSeed_PaintsTheSameScenery()
        {
            var (grid, layout) = MakeMap(5);
            var settings = new SurfaceScenerySettings();
            Color32[] a = SurfaceSceneryPainter.Paint(grid, layout, settings, 99, Sky, Sky).Near.Pixels;
            Color32[] b = SurfaceSceneryPainter.Paint(grid, layout, settings, 99, Sky, Sky).Near.Pixels;
            CollectionAssert.AreEqual(a, b);
        }
    }
}
