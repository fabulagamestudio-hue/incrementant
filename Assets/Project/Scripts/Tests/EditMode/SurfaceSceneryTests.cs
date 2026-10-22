using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    public sealed class SurfaceSceneryTests
    {
        static readonly Color32 Sky = new Color32(120, 170, 215, 255);

        [Test]
        public void PlantMask_AlignsFractionalPivotToArtPixelWithoutChangingSprite()
        {
            var texture = new Texture2D(64, 32, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0,0,64,32), new Vector2(0.123f,0.234f),16f);
            try
            {
                Vector2 original = sprite.pivot;
                var mask = new PlantCellMask(sprite);
                Vector2 corner = (mask.VisualOffset - sprite.pivot / 16f) * 16f;
                Assert.AreEqual(Mathf.Round(corner.x),corner.x,0.0001f);
                Assert.AreEqual(Mathf.Round(corner.y),corner.y,0.0001f);
                Assert.AreEqual(original,sprite.pivot);
                Assert.AreEqual(-mask.Mapping.z * 16f,corner.x,0.0001f);
                Assert.AreEqual(-mask.Mapping.w * 16f,corner.y,0.0001f);
            }
            finally { Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

        [Test]
        public void DetailedStamp_PreservesOnePixelPlantTips()
        {
            var texture = new Texture2D(32, 64, TextureFormat.RGBA32, false);
            texture.SetPixels32(new Color32[32 * 64]);
            for (int y = 0; y < 64; y++) texture.SetPixel(0, y, Color.green);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0,0,32,64), Vector2.zero, 16f);
            try
            {
                SceneryStamp stamp = SceneryStamp.FromSprite(sprite);
                PlantShape shape = PlantShape.FromPixels(stamp.Pixels, stamp.Width, stamp.Height, stamp.PivotX, stamp.PivotY, 6, 1);
                Assert.AreEqual(4, shape.Cells.Length, "O caule fino deve continuar presente nas quatro células de altura.");
            }
            finally { Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

        [Test]
        public void PlantMask_UsesOneTexelPerCellAndKeepsBuriedBase()
        {
            var texture = new Texture2D(128, 80, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 128, 80), new Vector2(0.5f, 0.2f), 16f);
            try
            {
                var mask = new PlantCellMask(sprite);
                Assert.AreEqual(9, mask.Width);
                Assert.AreEqual(5, mask.Height);
                Assert.AreEqual(-1, mask.MinY);
                Assert.Less(mask.Pixels.Length, texture.width * texture.height / 100);
                for (int x = 0; x < mask.Width; x++)
                    Assert.AreEqual(255, mask.Pixels[x].a, "A base enterrada continua visível.");
                mask.SetCell(Vector2Int.zero, 1);
                int index = -mask.MinY * mask.Width - mask.MinX;
                Assert.AreEqual(255, mask.Pixels[index].a);
                Assert.AreEqual(0, mask.Pixels[index].r);
                mask.SetCell(Vector2Int.zero, 2);
                Assert.AreEqual(255, mask.Pixels[index].r);
                mask.SetCell(Vector2Int.zero, 0);
                Assert.AreEqual(0, mask.Pixels[index].a);
            }
            finally { Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

        [Test]
        public void PlantMask_ShaderCutsHarvestedCellsAndTintsMarkedCells()
        {
            Shader shader = Resources.Load<Shader>("Scenery/DetailedPlants");
            Assert.IsNotNull(shader);
            Assert.IsTrue(shader.isSupported);
            var source = new Texture2D(64, 32, TextureFormat.RGBA32, false, true);
            var colors = new Color32[64 * 32];
            System.Array.Fill(colors, new Color32(0, 180, 0, 255));
            source.SetPixels32(colors); source.Apply();
            Sprite sprite = Sprite.Create(source, new Rect(0, 0, 64, 32), new Vector2(0.5f, 0), 16f);
            var mask = new PlantCellMask(sprite);
            mask.SetCell(new Vector2Int(-1, 0), 1);
            mask.SetCell(new Vector2Int(0, 0), 2);
            var maskTexture = new Texture2D(mask.Width, mask.Height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            maskTexture.SetPixels32(mask.Pixels); maskTexture.Apply();
            var material = new Material(shader);
            material.SetTexture("_MainTex", source); material.SetTexture("_CellMask", maskTexture);
            material.SetVector("_MaskMapping", mask.Mapping); material.SetVector("_MaskBounds", mask.Bounds);
            material.SetColor("_MarkColor", Color.red); material.SetFloat("_MarkStrength", 1f);
            var target = new RenderTexture(64, 32, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(64, 32, TextureFormat.RGBA32, false, true);
            var mesh = new Mesh
            {
                vertices = new[] { new Vector3(0,0), new Vector3(1,0), new Vector3(1,1), new Vector3(0,1) },
                uv = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) },
                colors = new[] { Color.white, Color.white, Color.white, Color.white },
                triangles = new[] { 0,1,2,0,2,3 },
            };
            RenderTexture previous = RenderTexture.active;
            try
            {
                target.Create(); RenderTexture.active = target;
                GL.Clear(true, true, Color.clear); GL.PushMatrix();
                try
                {
                    GL.LoadOrtho(); material.SetPass(0); Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                }
                finally { GL.PopMatrix(); }
                readback.ReadPixels(new Rect(0,0,64,32),0,0); readback.Apply();
                Assert.Greater(readback.GetPixel(16,8).g, 0.5f, "Célula presente conserva a arte verde.");
                Assert.Greater(readback.GetPixel(32,8).r, 0.9f, "Célula marcada recebe a tinta.");
                Assert.Less(readback.GetPixel(56,8).a, 0.01f, "Célula colhida fica transparente.");
            }
            finally
            {
                RenderTexture.active = previous; target.Release();
                Object.DestroyImmediate(target); Object.DestroyImmediate(readback); Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(material); Object.DestroyImmediate(maskTexture); Object.DestroyImmediate(sprite); Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void DetailedStamp_KeepsWorldSizeAndCustomPivot()
        {
            var texture = new Texture2D(128, 80, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 128, 80), new Vector2(0.25f, 0.2f), 16f);
            try
            {
                SceneryStamp stamp = SceneryStamp.FromSprite(sprite);
                Assert.AreSame(sprite, stamp.VisualSprite);
                Assert.AreEqual(8f, stamp.Width / (float)SurfaceSceneryPainter.PixelsPerCell);
                Assert.AreEqual(5f, stamp.Height / (float)SurfaceSceneryPainter.PixelsPerCell);
                Assert.AreEqual(12, stamp.PivotX);
                Assert.AreEqual(6, stamp.PivotY);
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void DetailedPebbles_PreserveBackdropWithoutPaintingCoarsePixels()
        {
            var texture = new Texture2D(128, 80, TextureFormat.RGBA32, false);
            var marker = new Color32(255, 0, 1, 255);
            var pixels = new Color32[128 * 80];
            System.Array.Fill(pixels, marker);
            texture.SetPixels32(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 128, 80), new Vector2(0.5f, 0f), 16f);
            try
            {
                var (grid, layout) = MakeMap(13);
                var art = new SceneryStampLibrary();
                art.Set(SceneryElement.Pebble, new[] { SceneryStamp.FromSprite(sprite) });
                SceneryLayers layers = SurfaceSceneryPainter.Paint(grid, layout, new SurfaceScenerySettings(), 13, Sky, Sky, art: art);
                Assert.Greater(layers.Near.SpriteStamps.Count, 0);
                Assert.IsFalse(System.Array.Exists(layers.Near.Pixels, c => c.Equals(marker)), "A pedra detalhada não deve deixar um carimbo grosseiro sob o sprite.");
                foreach (var placed in layers.Near.SpriteStamps)
                {
                    int x = placed.Left + placed.Stamp.Width / 2;
                    int y = placed.Bottom + placed.Stamp.Height / 2;
                    int ppc = layers.Near.PixelsPerCell;
                    Assert.IsTrue(layers.Backdrop[(y / ppc) * grid.Width + x / ppc], "A área caminhável da pedra precisa continuar presente.");
                }
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

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
