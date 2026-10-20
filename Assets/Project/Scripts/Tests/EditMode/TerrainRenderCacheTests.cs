using System.Collections.Generic;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// O render do terreno redesenha só os blocos cuja versão visual mudou (<see cref="PixelGrid.GetChunkRenderVersion"/>):
    /// o resultado tem que ser idêntico, pixel a pixel, ao de uma view nova que desenha tudo do zero.
    /// </summary>
    public sealed class TerrainRenderCacheTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        AntFarmView MakeView()
        {
            var go = new GameObject("TerrainRenderCacheTest", typeof(SpriteRenderer)) { hideFlags = HideFlags.HideAndDontSave };
            _objects.Add(go);
            return go.AddComponent<AntFarmView>();
        }

        static Color32[] Draw(AntFarmView view, PixelGrid grid, RectInt window)
        {
            view.Render(grid, new List<Ant>(), new List<Vector2Int>(), Vector2Int.zero, 1, false, false, window);
            Texture2D texture = view.transform.Find("TerrainWindow").GetComponent<SpriteRenderer>().sprite.texture;
            return texture.GetPixels32();
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

        [TestCase(11)]
        [TestCase(12)]
        public void CachedRender_MatchesAFullRedraw_AfterEveryKindOfChange(int seed)
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainGenerator.Generate(grid, settings, seed);
            int x0 = Mathf.Clamp(grid.Width / 2 - 80, 0, grid.Width - 160);
            int y0 = Mathf.Clamp(grid.GetSurfaceY(grid.Width / 2) - 90, 0, grid.Height - 128);
            var window = new RectInt(x0, y0, 160, 128);

            AntFarmView cached = MakeView();
            Draw(cached, grid, window);

            var rng = new System.Random(seed);
            for (int round = 0; round < 12; round++)
            {
                for (int n = 0; n < 40; n++)
                {
                    // Também fora da janela (o bloco fica velho no cache até a janela chegar lá).
                    int x = Mathf.Clamp(window.x - 40 + rng.Next(window.width + 80), 0, grid.Width - 1);
                    int y = Mathf.Clamp(window.y - 40 + rng.Next(window.height + 80), 0, grid.Height - 1);
                    switch (rng.Next(12))
                    {
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

                Color32[] viaCache = Draw(cached, grid, window);
                Color32[] fresh = Draw(MakeView(), grid, window);
                AssertSame(fresh, viaCache, $"rodada {round}");
            }

            // Mudanças fora da janela só valem quando ela chega lá.
            var shifted = new RectInt(Mathf.Clamp(window.x - 40, 0, grid.Width - 160), window.y, 160, 128);
            AssertSame(Draw(MakeView(), grid, shifted), Draw(cached, grid, shifted), "janela deslocada depois das mudanças");
        }

        [Test]
        public void CachedRender_FollowsTheWindow_WhenItMoves()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainGenerator.Generate(grid, settings, 3);
            int y0 = Mathf.Clamp(grid.GetSurfaceY(grid.Width / 2) - 90, 0, grid.Height - 128);

            AntFarmView cached = MakeView();
            Draw(cached, grid, new RectInt(0, y0, 160, 128));
            grid.Set(300, y0 + 40, CellMaterial.Empty, 0);
            var moved = new RectInt(220, y0, 160, 128);
            AssertSame(Draw(MakeView(), grid, moved), Draw(cached, grid, moved), "janela deslocada");
        }
    }
}
