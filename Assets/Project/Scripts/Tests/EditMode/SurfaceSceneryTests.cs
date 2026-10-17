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
        public void Scenery_PaintsNoPlants_TheyAreAllHarvestable()
        {
            var (grid, layout) = MakeMap(3);
            var red = new Color32(255, 0, 0, 255);
            var pixels = new Color32[4 * 4];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = red;
            var art = new SceneryStampLibrary();
            foreach (SceneryElement plant in new[] { SceneryElement.Grass, SceneryElement.Tuft, SceneryElement.Clover, SceneryElement.Flower })
                art.Set(plant, new[] { new SceneryStamp(4, 4, pixels, 2, 0) });

            SceneryLayers layers = SurfaceSceneryPainter.Paint(grid, layout, new SurfaceScenerySettings(), 3, Sky, Sky, art: art);
            Assert.IsFalse(System.Array.Exists(layers.Near.Pixels, c => c.Equals(red)),
                           "Grama, tufo, trevo e flor são plantas colhíveis (PlantSprites), não cenário.");
            Assert.IsFalse(System.Array.Exists(layers.Front.Pixels, c => c.a > 0), "A camada da frente fica vazia.");
            Assert.IsTrue(System.Array.Exists(layers.Near.Pixels, c => c.a > 0), "O resto do cenário perto continua.");
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
            art.Set(SceneryElement.Pebble, new[] { new SceneryStamp(4, 4, pixels, 2, 0) });

            SceneryLayer near = SurfaceSceneryPainter.Paint(grid, layout, new SurfaceScenerySettings(), 13, Sky, Sky, art: art).Near;
            int redCount = 0;
            foreach (Color32 c in near.Pixels)
                if (c.Equals(red))
                    redCount++;
            Assert.Greater(redCount, 0, "As pedrinhas precisam vir da biblioteca dada.");

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
