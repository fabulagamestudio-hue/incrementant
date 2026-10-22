using System.Collections.Generic;
using System.IO;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Cogumelo luminoso (pedido de 2026-10-21): a marca do pincel, as formigas encaixando o cogumelo pela obra, cavar o
    /// cogumelo, o save da marca e o cogumelo como fonte de luz; e a penumbra do formigueiro (túnel sem preto absoluto, terra
    /// longe da luz preta).
    /// </summary>
    public sealed class GlowMushroomTests
    {
        const int Surface = 20;
        readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _objects)
                Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        /// <summary>
        /// Solo de terra com piso de rocha, uma entrada (x 39..41) e um túnel em y 5..7 até x = 70; lá fora, sobre o solo, uma
        /// pilha de terra solta (o substrato).
        /// </summary>
        static PixelGrid MakeTunnelWorld()
        {
            var grid = new PixelGrid(80, 30);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, Surface);
                for (int y = 1; y <= Surface; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int y = 5; y <= Surface; y++)
            for (int x = 39; x <= 41; x++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 39; x <= 70; x++)
            for (int y = 5; y <= 7; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 48; x <= 57; x++)
                grid.Set(x, Surface + 1, CellMaterial.Spoil);
            return grid;
        }

        static AntColony MakeColony(PixelGrid grid)
        {
            var layout = new TerrainLayout(40, new Vector2Int(40, 6), 60, 70);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 11);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Worker;
            return colony;
        }

        static void Run(PixelGrid grid, AntColony colony, System.Func<bool> done, int seconds = 180)
        {
            var sim = new GranularSimulation();
            for (int tick = 0; tick < 60 * seconds && !done(); tick++)
            {
                sim.Step(grid);
                colony.Tick(1f / 60f);
            }
        }

        [Test]
        public void Mark_OnlyInsideTheAnthill_OnEmptyCellsTouchingTheSoil()
        {
            PixelGrid grid = MakeTunnelWorld();
            Assert.IsFalse(grid.SetGlowMushroomMarked(60, 6, true), "No meio do túnel, sem terra encostada, o cogumelo ficaria solto.");
            Assert.IsFalse(grid.SetGlowMushroomMarked(60, 4, true), "Célula de terra não recebe a marca.");
            Assert.IsFalse(grid.SetGlowMushroomMarked(30, Surface + 3, true), "Acima do solo original (céu) não vale.");
            Assert.IsTrue(grid.SetGlowMushroomMarked(60, 5, true), "No chão do túnel vale.");
            Assert.IsTrue(grid.SetGlowMushroomMarked(62, 7, true), "No teto do túnel também.");
            Assert.IsTrue(grid.IsBuildMarked(60, 5) && grid.IsGlowMushroomMarked(60, 5), "A marca é uma obra do tipo cogumelo.");
            Assert.AreEqual(2, grid.BuildMarkedCount);

            Assert.IsTrue(grid.SetBuildMarked(64, 5, true));
            Assert.IsFalse(grid.SetGlowMushroomMarked(64, 5, true), "Obra comum já marcada não vira cogumelo.");
            Assert.IsFalse(grid.SetGlowMushroomMarked(64, 5, false), "Desmarcar cogumelo não apaga a obra comum.");
            Assert.IsTrue(grid.IsBuildMarked(64, 5));

            Assert.IsTrue(grid.SetGlowMushroomMarked(62, 7, false));
            Assert.IsFalse(grid.IsBuildMarked(62, 7));
            // O pincel Construir apagando a marca leva o tipo junto: marcar obra comum ali de novo não vira cogumelo.
            grid.SetBuildMarked(60, 5, false);
            grid.SetBuildMarked(60, 5, true);
            Assert.IsFalse(grid.IsGlowMushroomMarked(60, 5));
        }

        [Test]
        public void Colony_PlantsAGlowMushroomOnTheMark_OnlyWithAMushroomFromTheAphids()
        {
            PixelGrid grid = MakeTunnelWorld();
            Assert.IsTrue(grid.SetGlowMushroomMarked(65, 5, true));
            AntColony colony = MakeColony(grid);

            // Pedido do desenvolvedor em 2026-10-22: o cogumelo vem dos pulgões. Só com terra (a pilha de fora), a marca espera.
            Run(grid, colony, () => grid.BuildMarkedCount == 0, 40);
            Assert.IsTrue(grid.IsGlowMushroomMarked(65, 5), "Sem cogumelo colhido, a marca espera.");
            Assert.AreEqual(CellMaterial.Empty, grid.Get(65, 5), "A terra não serve mais de substrato.");

            Assert.IsTrue(grid.SetStored(45, 5, StoredItems.Mushroom), "Um cogumelo solto (como os que a ordenha deixa na fazenda).");
            Run(grid, colony, () => grid.BuildMarkedCount == 0);

            Assert.AreEqual(CellMaterial.GlowMushroom, grid.Get(65, 5), "A formiga deveria levar o cogumelo à marca.");
            Assert.IsFalse(grid.IsGlowMushroomMarked(65, 5), "A marca some quando o cogumelo entra.");
            Assert.AreEqual(0, grid.StoredCount(StorageKind.Mushroom), "O cogumelo guardado foi gasto.");
            Assert.AreEqual(1, colony.MushroomsPlanted);
            Assert.AreEqual(0, colony.TotalDelivered, "Plantar o cogumelo não conta como entrega de terra.");
        }

        [Test]
        public void DiggingAGlowMushroom_GivesLooseSoilBack()
        {
            Assert.AreEqual(CellMaterial.Spoil, CellMaterials.ToCarried(CellMaterial.GlowMushroom));
            Assert.AreEqual(CellMaterial.PackedSpoil, CellMaterials.ToPacked(CellMaterial.GlowMushroom),
                            "Se a formiga ainda tiver o cogumelo na mão, ele não pode virar outro cogumelo numa obra comum.");

            PixelGrid grid = MakeTunnelWorld();
            grid.Set(65, 5, CellMaterial.GlowMushroom);
            Assert.IsTrue(grid.SetMarked(65, 5, true), "O cogumelo aceita a marca de cavar, para o jogador poder tirá-lo.");
            AntColony colony = MakeColony(grid);

            Run(grid, colony, () => grid.MarkedCount == 0 && grid.Get(65, 5) == CellMaterial.Empty, 120);

            Assert.AreEqual(CellMaterial.Empty, grid.Get(65, 5), "As escavadoras deveriam tirar o cogumelo.");
            for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
                Assert.AreNotEqual(CellMaterial.GlowMushroom, grid.Get(x, y), $"Nenhum cogumelo deveria reaparecer ({x}, {y}).");
        }

        [Test]
        public void Save_KeepsTheGlowMushroomMarkAndTheMushroom()
        {
            PixelGrid grid = MakeTunnelWorld();
            grid.Set(50, 5, CellMaterial.GlowMushroom);
            Assert.IsTrue(grid.SetGlowMushroomMarked(60, 5, true));
            Assert.IsTrue(grid.SetBuildMarked(64, 5, true));
            AntColony colony = MakeColony(grid);
            var rain = new RainSimulation(new RainSettings(), 3);
            var water = new WaterSimulation(new WaterSettings(), grid, 3);

            var stream = new MemoryStream();
            GameSnapshot.Capture(grid, colony, rain, water, 0, 3, 1.0).Write(stream);
            stream.Position = 0;
            GameSnapshot loaded = GameSnapshot.Read(stream);

            PixelGrid grid2 = MakeTunnelWorld();
            loaded.ApplyTo(grid2);
            Assert.AreEqual(CellMaterial.GlowMushroom, grid2.Get(50, 5));
            Assert.IsTrue(grid2.IsGlowMushroomMarked(60, 5), "A marca de cogumelo volta como cogumelo.");
            Assert.IsTrue(grid2.IsBuildMarked(64, 5) && !grid2.IsGlowMushroomMarked(64, 5), "A obra comum volta comum.");
        }

        // ---------------------------------------------------------------- luz e penumbra

        AntFarmView MakeView(PixelGrid grid, bool tiles)
        {
            var go = new GameObject("GlowMushroomTest", typeof(SpriteRenderer)) { hideFlags = HideFlags.HideAndDontSave };
            _objects.Add(go);
            var view = go.AddComponent<AntFarmView>();
            if (tiles)
            {
                var material = new Material(Shader.Find("Incrementant/TerrainTiles")) { hideFlags = HideFlags.HideAndDontSave };
                _objects.Add(material);
                var serialized = new SerializedObject(view);
                serialized.FindProperty("_tileMaterial").objectReferenceValue = material;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            view.Initialize(grid);
            return view;
        }

        static void Render(AntFarmView view, PixelGrid grid, RectInt window) =>
            view.Render(grid, new List<Ant>(), new List<Vector2Int>(), Vector2Int.zero, 1, false, false, window);

        static SpriteRenderer WindowRenderer(AntFarmView view) => view.transform.Find("TerrainWindow").GetComponent<SpriteRenderer>();

        /// <summary>Galeria funda (60 células abaixo do solo), fechada, longe de qualquer luz de fora.</summary>
        static PixelGrid MakeDeepGallery(out RectInt window, out int floorY)
        {
            var grid = new PixelGrid(96, 96);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.SetSurfaceY(x, 80);
                for (int y = 0; y <= 80; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            floorY = 20;
            for (int x = 10; x <= 85; x++)
            for (int y = floorY; y <= floorY + 2; y++)
                grid.Set(x, y, CellMaterial.Empty);
            window = new RectInt(0, 0, 96, 48);
            return grid;
        }

        [Test]
        public void GlowMushroom_LightsTheGalleryAround_WithinTheLightReach_AndGoesOutWhenRemoved()
        {
            PixelGrid grid = MakeDeepGallery(out RectInt window, out int floorY);
            AntFarmView view = MakeView(grid, true);
            int Light(int x, int y)
            {
                Render(view, grid, window);
                var texture = (Texture2D)WindowRenderer(view).sharedMaterial.GetTexture("_CellMaterials");
                byte[] data = texture.GetRawTextureData<byte>().ToArray();
                return data[AntFarmView.TileBytes * ((y - window.y) * window.width + x - window.x) + 2];
            }

            Assert.AreEqual(0, Light(40, floorY + 1), "Sem cogumelo, a galeria funda não tem luz do mapa.");

            grid.Set(40, floorY, CellMaterial.GlowMushroom);
            int glow = new SerializedObject(view).FindProperty("_glowMushroomLight").intValue;
            Assert.Greater(glow, 1);
            Assert.Greater(Light(40, floorY + 1), 0, "Ao lado do cogumelo deveria haver luz.");
            Assert.Greater(Light(40 + glow - 2, floorY + 1), 0, "A luz segue pelo túnel perdendo 1 nível por célula.");
            Assert.AreEqual(0, Light(40 + glow + 2, floorY + 1), "Além do alcance (nível do cogumelo), volta à penumbra sem luz do mapa.");
            Assert.Less(Light(40, floorY - 3), Light(40, floorY + 3),
                        "Na terra a luz perde mais por célula que no túnel (Solid Light Cost).");

            int Flags(int x, int y)
            {
                Render(view, grid, window);
                var texture = (Texture2D)WindowRenderer(view).sharedMaterial.GetTexture("_CellMaterials");
                return texture.GetRawTextureData<byte>()[AntFarmView.TileBytes * ((y - window.y) * window.width + x - window.x) + 3];
            }
            Assert.AreEqual(128, Flags(42, floorY + 1), "A luz que veio do cogumelo é marcada para o shader usar o tom amarelado dele.");
            Assert.AreEqual(255, Flags(40, floorY), "O próprio cogumelo sai da CPU já com a cor dele (não recebe luz).");

            grid.Set(40, floorY, CellMaterial.Empty);
            Assert.AreEqual(0, Light(40, floorY + 1), "Tirado o cogumelo, a luz some.");
            Assert.AreEqual(0, Flags(42, floorY + 1), "Sem cogumelo, a célula perde a marca da luz dele.");
        }

        [Test]
        public void DeepTunnel_IsAStrongPenumbra_ButSoilAwayFromTheLightStaysBlack()
        {
            PixelGrid grid = MakeDeepGallery(out RectInt window, out int floorY);
            // Sem tiles a luz é aplicada por célula na CPU (mesmo piso que o shader usa por texel).
            AntFarmView view = MakeView(grid, false);
            Render(view, grid, window);
            Color32[] pixels = WindowRenderer(view).sprite.texture.GetPixels32();
            Color32 tunnel = pixels[(floorY + 1 - window.y) * window.width + 40 - window.x];
            Color32 soil = pixels[(floorY - 6 - window.y) * window.width + 40 - window.x];
            Assert.Greater(tunnel.r + tunnel.g + tunnel.b, 0, "O túnel longe da luz não pode ser preto absoluto.");
            Assert.AreEqual(0, soil.r + soil.g + soil.b, "A terra longe da luz (fora do caminho das formigas) fica preta.");
            Assert.Less(Mathf.Max(tunnel.r, tunnel.g, tunnel.b), 64, "Mas continua uma penumbra forte.");
        }
    }
}
