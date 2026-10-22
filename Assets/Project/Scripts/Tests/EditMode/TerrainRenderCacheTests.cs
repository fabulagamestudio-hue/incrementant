using System.Collections.Generic;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// O render do terreno redesenha só os blocos cuja versão visual mudou (<see cref="PixelGrid.GetChunkRenderVersion"/>):
    /// o resultado tem que ser idêntico, pixel a pixel, ao de uma view nova que desenha tudo do zero. Com tiles, vale também
    /// para a textura de materiais (material, distância com sinal até a borda terra/vazio, luz e "já iluminada"); a distância
    /// e a luz também são conferidas contra um cálculo direto na grade.
    /// </summary>
    public sealed class TerrainRenderCacheTests
    {
        readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _objects)
                Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        /// <param name="tiles">Liga o Tile Material (shader Incrementant/TerrainTiles), que grava a textura de materiais.</param>
        AntFarmView MakeView(PixelGrid grid, bool tiles = false, bool plantsAsSprites = false)
        {
            var go = new GameObject("TerrainRenderCacheTest", typeof(SpriteRenderer)) { hideFlags = HideFlags.HideAndDontSave };
            _objects.Add(go);
            var view = go.AddComponent<AntFarmView>();
            if (tiles)
            {
                Shader shader = Shader.Find("Incrementant/TerrainTiles");
                Assert.IsNotNull(shader, "Shader Incrementant/TerrainTiles não encontrado (Assets/Project/Materials/TerrainTiles.shader).");
                var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                _objects.Add(material);
                var serialized = new SerializedObject(view);
                serialized.FindProperty("_tileMaterial").objectReferenceValue = material;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            view.Initialize(grid, plantsAsSprites: plantsAsSprites);
            return view;
        }

        static void Render(AntFarmView view, PixelGrid grid, RectInt window) =>
            view.Render(grid, new List<Ant>(), new List<Vector2Int>(), Vector2Int.zero, 1, false, false, window);

        static SpriteRenderer WindowRenderer(AntFarmView view) =>
            view.transform.Find("TerrainWindow").GetComponent<SpriteRenderer>();

        static Color32[] Draw(AntFarmView view, PixelGrid grid, RectInt window)
        {
            Render(view, grid, window);
            return WindowRenderer(view).sprite.texture.GetPixels32();
        }

        /// <summary>Textura de materiais da janela: <see cref="AntFarmView.TileBytes"/> por célula (material, 128 + distância com sinal, luz, já iluminada).</summary>
        static byte[] DrawMaterials(AntFarmView view, PixelGrid grid, RectInt window)
        {
            Render(view, grid, window);
            var texture = (Texture2D)WindowRenderer(view).sharedMaterial.GetTexture("_CellMaterials");
            return texture.GetRawTextureData<byte>().ToArray();
        }

        static void AssertSame(Color32[] expected, Color32[] actual, string step)
        {
            Assert.AreEqual(expected.Length, actual.Length, step);
            for (int i = 0; i < expected.Length; i++)
            {
                if (!expected[i].Equals(actual[i]))
                    Assert.Fail($"{step}: pixel {i} é {actual[i]}, o desenho do zero dá {expected[i]}.");
            }
        }

        static void AssertSame(byte[] expected, byte[] actual, RectInt window, string step)
        {
            Assert.AreEqual(expected.Length, actual.Length, step);
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] != actual[i])
                {
                    int cell = i / AntFarmView.TileBytes;
                    string[] channels = { "material", "distância", "luz", "já iluminada" };
                    Assert.Fail($"{step}: célula ({window.x + cell % window.width}, {window.y + cell / window.width}), canal {channels[i % AntFarmView.TileBytes]}, " +
                                $"é {actual[i]}, o desenho do zero dá {expected[i]}.");
                }
            }
        }

        /// <summary>A distância gravada confere com uma varredura direta da grade (sem tabela de área somada nem cache).</summary>
        static void AssertSignedDistances(byte[] materials, PixelGrid grid, RectInt window, bool plantsAsSprites, string step)
        {
            int range = AntFarmView.SignedDistanceRange;
            for (int wy = 0; wy < window.height; wy++)
            for (int wx = 0; wx < window.width; wx++)
            {
                int x = window.x + wx, y = window.y + wy;
                bool open = IsOpen(grid, x, y, plantsAsSprites);
                int expected = open ? -range : range;
                for (int k = 1; k < range && Mathf.Abs(expected) == range; k++)
                {
                    for (int dy = -k; dy <= k; dy++)
                    for (int dx = -k; dx <= k; dx++)
                    {
                        int nx = Mathf.Clamp(x + dx, 0, grid.Width - 1), ny = Mathf.Clamp(y + dy, 0, grid.Height - 1);
                        if (IsOpen(grid, nx, ny, plantsAsSprites) != open)
                            expected = open ? -k : k;
                    }
                }
                int actual = AntFarmView.SignedDistanceOf(materials[AntFarmView.TileBytes * (wy * window.width + wx) + 1]);
                if (actual != expected)
                    Assert.Fail($"{step}: distância com sinal em ({x}, {y}) é {actual}, a varredura da grade dá {expected}.");
            }
        }

        /// <summary>
        /// A luz gravada confere com uma propagação na grade inteira (fila por nível, sem blocos nem folga), com os valores do
        /// Inspector da view.
        /// </summary>
        static void AssertLight(byte[] materials, AntFarmView view, PixelGrid grid, RectInt window, bool plantsAsSprites, string step)
        {
            var serialized = new SerializedObject(view);
            int outside = serialized.FindProperty("_outsideLight").intValue;
            int solidCost = serialized.FindProperty("_solidLightCost").intValue;
            int glow = serialized.FindProperty("_glowMushroomLight").intValue;
            int glowSolidCost = serialized.FindProperty("_glowSolidLightCost").intValue;

            int width = grid.Width, height = grid.Height;
            var level = new int[width * height];
            var queues = new List<int>[AntFarmView.LightLevels + 1];
            for (int i = 0; i < queues.Length; i++)
                queues[i] = new List<int>();
            // Fontes: o lado de fora (acima do solo original, vazio ou não) e o feixe que desce reto dele pelas células livres.
            for (int x = 0; x < width; x++)
            {
                int beam = grid.GetSurfaceY(x);
                while (beam >= 0 && IsOpen(grid, x, beam, plantsAsSprites))
                    beam--;
                for (int y = beam + 1; y < height && outside > 0; y++)
                {
                    level[y * width + x] = outside;
                    queues[outside].Add(y * width + x);
                }
            }
            Spread(level, solidCost);
            // Cogumelos luminosos: propagados à parte, com o custo próprio na terra; fica a maior das duas luzes.
            var glowLevel = new int[width * height];
            for (int i = 0; i < width * height; i++)
            {
                if (grid.Get(i % width, i / width) == CellMaterial.GlowMushroom && glow > 0)
                {
                    glowLevel[i] = glow;
                    queues[glow].Add(i);
                }
            }
            Spread(glowLevel, glowSolidCost);
            for (int i = 0; i < width * height; i++)
                level[i] = Mathf.Max(level[i], glowLevel[i]);

            void Spread(int[] levels, int cost)
            {
                for (int l = AntFarmView.LightLevels; l > 1; l--)
                {
                    foreach (int i in queues[l])
                    {
                        if (levels[i] != l)
                            continue;
                        int x = i % width, y = i / width;
                        for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if ((dx == 0 && dy == 0) || !grid.InBounds(nx, ny))
                                continue;
                            int n = ny * width + nx;
                            int next = l - (IsOpen(grid, nx, ny, plantsAsSprites) ? 1 : cost);
                            if (next > levels[n])
                            {
                                levels[n] = next;
                                queues[next].Add(n);
                            }
                        }
                    }
                    queues[l].Clear();
                }
            }

            for (int wy = 0; wy < window.height; wy++)
            for (int wx = 0; wx < window.width; wx++)
            {
                int x = window.x + wx, y = window.y + wy;
                int expected = (level[y * width + x] * 255 + AntFarmView.LightLevels / 2) / AntFarmView.LightLevels;
                int actual = materials[AntFarmView.TileBytes * (wy * window.width + wx) + 2];
                if (actual != expected)
                    Assert.Fail($"{step}: luz em ({x}, {y}) é {actual}, a propagação na grade dá {expected} (nível {level[y * width + x]}).");
            }
        }

        static bool IsOpen(PixelGrid grid, int x, int y, bool plantsAsSprites)
        {
            CellMaterial material = grid.Get(x, y);
            return material == CellMaterial.Empty || (plantsAsSprites && CellMaterials.IsPlant(material));
        }

        static PixelGrid MakeGrid(int seed, out RectInt window)
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainGenerator.Generate(grid, settings, seed);
            int x0 = Mathf.Clamp(grid.Width / 2 - 80, 0, grid.Width - 160);
            int y0 = Mathf.Clamp(grid.GetSurfaceY(grid.Width / 2) - 90, 0, grid.Height - 128);
            window = new RectInt(x0, y0, 160, 128);
            return grid;
        }

        static void ApplyRandomChanges(PixelGrid grid, RectInt window, System.Random rng, bool withPlants)
        {
            for (int n = 0; n < 40; n++)
            {
                // Também fora da janela (o bloco fica velho no cache até a janela chegar lá).
                int x = Mathf.Clamp(window.x - 40 + rng.Next(window.width + 80), 0, grid.Width - 1);
                int y = Mathf.Clamp(window.y - 40 + rng.Next(window.height + 80), 0, grid.Height - 1);
                switch (rng.Next(withPlants ? 16 : 12))
                {
                    // Cogumelo luminoso: fonte de luz que entra e sai com o material.
                    case 15: grid.Set(x, y, rng.Next(2) == 0 ? CellMaterial.GlowMushroom : CellMaterial.Empty, 128); break;
                    // Sala da rainha: pintura acima da luz.
                    case 14: grid.SetQueenRoom(x, y, rng.Next(2) == 0); break;
                    // Planta trocada por sólido e de volta: não sobe a versão do bloco, mas muda o lado da borda.
                    case 12: grid.Set(x, y, CellMaterial.Sand, (byte)rng.Next(256)); grid.Set(x, y, CellMaterial.PlantGrass, 128); break;
                    case 13: grid.Set(x, y, CellMaterial.PlantLeaf, 128); grid.Set(x, y, CellMaterial.Dirt, (byte)rng.Next(256)); break;
                    case 10: grid.Set(x, y, CellMaterial.Spoil, (byte)rng.Next(256)); grid.Set(x, y, CellMaterial.Sand, (byte)rng.Next(256)); break;
                    case 11: grid.DirtyRender(x, y); grid.SetNutrient(x, y, true); break;
                    case 0: grid.Set(x, y, CellMaterial.Empty, 0); break;
                    case 1: grid.Set(x, y, CellMaterial.Sand, (byte)rng.Next(256)); break;
                    case 2: grid.SetWetness(x, y, (byte)rng.Next(4)); break;
                    case 3: grid.SetMarked(x, y, rng.Next(2) == 0); break;
                    case 4: grid.SetBuildMarked(x, y, rng.Next(2) == 0); break;
                    case 5: grid.SetWater(x, y, rng.Next(2) == 0); break;
                    case 6: grid.SetNutrientRoom(x, y, rng.Next(2) == 0); break;
                    case 7: grid.SetStorageRoom(x, y, StorageKind.Blocks, rng.Next(2) == 0); break;
                    case 8: grid.SetStored(x, y, StoredItems.Water); break;
                    default: grid.SetNutrient(x, y, rng.Next(2) == 0); break;
                }
            }
        }

        [TestCase(11)]
        [TestCase(12)]
        public void CachedRender_MatchesAFullRedraw_AfterEveryKindOfChange(int seed)
        {
            PixelGrid grid = MakeGrid(seed, out RectInt window);
            AntFarmView cached = MakeView(grid);
            Draw(cached, grid, window);

            var rng = new System.Random(seed);
            for (int round = 0; round < 12; round++)
            {
                ApplyRandomChanges(grid, window, rng, false);
                Color32[] viaCache = Draw(cached, grid, window);
                Color32[] fresh = Draw(MakeView(grid), grid, window);
                AssertSame(fresh, viaCache, $"rodada {round}");
            }

            // Mudanças fora da janela só valem quando ela chega lá.
            var shifted = new RectInt(Mathf.Clamp(window.x - 40, 0, grid.Width - 160), window.y, 160, 128);
            AssertSame(Draw(MakeView(grid), grid, shifted), Draw(cached, grid, shifted), "janela deslocada depois das mudanças");
        }

        [TestCase(11, false)]
        [TestCase(12, true)]
        public void CachedTileData_MatchesAFullRedraw_AfterEveryKindOfChange(int seed, bool plantsAsSprites)
        {
            PixelGrid grid = MakeGrid(seed, out RectInt window);
            AntFarmView cached = MakeView(grid, true, plantsAsSprites);
            byte[] first = DrawMaterials(cached, grid, window);
            AssertSignedDistances(first, grid, window, plantsAsSprites, "primeiro desenho");
            AssertLight(first, cached, grid, window, plantsAsSprites, "primeiro desenho");

            var rng = new System.Random(seed);
            for (int round = 0; round < 12; round++)
            {
                ApplyRandomChanges(grid, window, rng, true);
                byte[] viaCache = DrawMaterials(cached, grid, window);
                byte[] fresh = DrawMaterials(MakeView(grid, true, plantsAsSprites), grid, window);
                AssertSame(fresh, viaCache, window, $"rodada {round}");
                AssertSignedDistances(viaCache, grid, window, plantsAsSprites, $"rodada {round}");
                AssertLight(viaCache, cached, grid, window, plantsAsSprites, $"rodada {round}");
            }

            var shifted = new RectInt(Mathf.Clamp(window.x - 40, 0, grid.Width - 160), window.y, 160, 128);
            AssertSame(DrawMaterials(MakeView(grid, true, plantsAsSprites), grid, shifted), DrawMaterials(cached, grid, shifted),
                       shifted, "janela deslocada depois das mudanças");
        }

        [Test]
        public void Beam_LightsADeepShaftToTheBottom_AndGoesOutWhenPlugged()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainGenerator.Generate(grid, settings, 5);
            // Longe do ninho inicial (no centro), para nenhum outro túnel iluminar a galeria.
            int x = grid.Width / 4;
            int surface = grid.GetSurfaceY(x);
            // Poço de 40 células (bem mais fundo que o espalhamento) com uma galeria no fundo.
            int bottom = surface - 40;
            for (int y = bottom; y <= surface; y++)
                grid.Set(x, y, CellMaterial.Empty, 0);
            for (int gx = x - 20; gx <= x; gx++)
                grid.Set(gx, bottom, CellMaterial.Empty, 0);
            var window = new RectInt(x - 40, bottom - 8, 64, 24);

            AntFarmView cached = MakeView(grid, true);
            byte[] lit = DrawMaterials(cached, grid, window);
            int Light(byte[] materials, int cx, int cy) => materials[AntFarmView.TileBytes * ((cy - window.y) * window.width + cx - window.x) + 2];
            Assert.AreEqual(255, Light(lit, x, bottom), "o fundo do poço deveria ter luz cheia");
            Assert.Greater(Light(lit, x - 6, bottom), 0, "a luz deveria se espalhar pela galeria a partir do feixe");
            Assert.AreEqual(0, Light(lit, x - 20, bottom), "a ponta da galeria, a 20 células do feixe, fica no breu");

            // Tapar o poço perto da boca, fora da janela: o fundo, a 30 células da célula mexida, apaga.
            grid.Set(x, surface - 10, CellMaterial.Dirt, 128);
            byte[] plugged = DrawMaterials(cached, grid, window);
            Assert.AreEqual(0, Light(plugged, x, bottom), "com o poço tapado, o fundo deveria ficar no breu");
            AssertSame(DrawMaterials(MakeView(grid, true), grid, window), plugged, window, "poço tapado");
        }

        [Test]
        public void Brush_IsDrawnAboveTheLight()
        {
            PixelGrid grid = MakeGrid(11, out RectInt window);
            AntFarmView view = MakeView(grid, true);
            // Fundo da janela: longe da superfície, no breu.
            var brush = new Vector2Int(window.x + window.width / 2, window.y + 8);
            view.Render(grid, new List<Ant>(), new List<Vector2Int>(), brush, 3, true, false, window);
            var renderer = WindowRenderer(view);
            byte[] materials = ((Texture2D)renderer.sharedMaterial.GetTexture("_CellMaterials")).GetRawTextureData<byte>().ToArray();
            Color32[] pixels = renderer.sprite.texture.GetPixels32();

            int Cell(int x, int y) => (y - window.y) * window.width + x - window.x;
            // Borda do pincel (raio 3): marcada como já iluminada, com a cor do pincel por cima do fundo escuro.
            int edge = Cell(brush.x + 3, brush.y);
            Assert.AreEqual(255, materials[AntFarmView.TileBytes * edge + 3], "a borda do pincel deveria estar acima da luz");
            Assert.Greater(pixels[edge].r, 100, "a cor do pincel (branco) deveria aparecer por cima do breu");
            // Centro do pincel (só a borda é desenhada) segue com a luz do shader, a menos que tenha marcação.
            int center = Cell(brush.x, brush.y);
            if (!grid.IsMarked(brush.x, brush.y) && !grid.IsQueenRoom(brush.x, brush.y))
                Assert.AreEqual(0, materials[AntFarmView.TileBytes * center + 3], "fora do pincel a luz é do shader");
        }

        [Test]
        public void CachedRender_FollowsTheWindow_WhenItMoves()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainGenerator.Generate(grid, settings, 3);
            int y0 = Mathf.Clamp(grid.GetSurfaceY(grid.Width / 2) - 90, 0, grid.Height - 128);

            AntFarmView cached = MakeView(grid);
            Draw(cached, grid, new RectInt(0, y0, 160, 128));
            grid.Set(300, y0 + 40, CellMaterial.Empty, 0);
            var moved = new RectInt(220, y0, 160, 128);
            AssertSame(Draw(MakeView(grid), grid, moved), Draw(cached, grid, moved), "janela deslocada");
        }
    }
}
