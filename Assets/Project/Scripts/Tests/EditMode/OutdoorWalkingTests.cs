using System.Collections.Generic;
using System.IO;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Andar livre do lado de fora (pedido do desenvolvedor em 2026-10-18): a formiga anda sobre toda a arte do cenário
    /// de perto (fundo, <see cref="PixelGrid.IsBackdrop"/>) e por dentro da obra de fora (<see cref="PixelGrid.IsBuilt"/>),
    /// não só pelo contorno.
    /// </summary>
    public sealed class OutdoorWalkingTests
    {
        const int Surface = 8;

        /// <summary>Piso de rocha em y = 0, terra até <see cref="Surface"/> e céu acima.</summary>
        static PixelGrid MakeGround(int width, int height)
        {
            var grid = new PixelGrid(width, height);
            for (int x = 0; x < width; x++)
            {
                grid.SetSurfaceY(x, Surface);
                grid.Set(x, 0, CellMaterial.Rock);
                for (int y = 1; y <= Surface; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            return grid;
        }

        [Test]
        public void SceneryBackdrop_MakesOpenSkyWalkable()
        {
            PixelGrid grid = MakeGround(20, 30);
            Assert.IsFalse(AntNavigation.IsWalkable(grid, 10, 20), "Céu aberto, sem arte atrás: não anda.");

            grid.SetBackdrop(10, 20, true);
            Assert.IsTrue(AntNavigation.IsWalkable(grid, 10, 20), "Sobre a arte do cenário anda, mesmo sem nada encostado.");

            grid.ClearBackdrop();
            Assert.IsFalse(grid.IsBackdrop(10, 20));
            Assert.IsFalse(AntNavigation.IsWalkable(grid, 10, 20));
        }

        [Test]
        public void SceneryBackdrop_ChangesStructureVersion_SoTheFieldsAreRebuilt()
        {
            PixelGrid grid = MakeGround(20, 30);
            int before = grid.StructureVersion;
            grid.SetBackdrop(10, 20, true);
            Assert.AreNotEqual(before, grid.StructureVersion);
        }

        [Test]
        public void FlowField_CrossesThroughTheMiddleOfTheSceneryArt()
        {
            // Um "cogumelo" de arte cobrindo x 5..15, y 9..18: o caminho do topo até o chão desce pelo meio dele.
            PixelGrid grid = MakeGround(21, 30);
            for (int x = 5; x <= 15; x++)
            for (int y = Surface + 1; y <= 18; y++)
                grid.SetBackdrop(x, y, true);

            var field = new FlowField(grid.Width, grid.Height);
            field.Build(grid, new List<int> { grid.ToIndex(10, Surface + 1) });
            Assert.AreEqual(9, field.Get(10, 18), "Do topo da arte ao chão em linha reta, pelo meio.");
        }

        [Test]
        public void OutdoorBuilt_IsWalkableInside_ButOtherSolidsAreNot()
        {
            PixelGrid grid = MakeGround(20, 30);
            // Bloco 3x3 de obra fora do formigueiro e um de terra compactada (crosta de chuva) do lado.
            for (int x = 4; x <= 6; x++)
            for (int y = Surface + 1; y <= Surface + 3; y++)
            {
                grid.Set(x, y, CellMaterial.PackedSpoil);
                Assert.IsTrue(grid.SetBuilt(x, y, true));
            }
            grid.Set(12, Surface + 1, CellMaterial.PackedSpoil);

            Assert.IsTrue(AntNavigation.IsWalkable(grid, 5, Surface + 2), "Meio da obra de fora: anda por dentro.");
            Assert.IsFalse(AntNavigation.IsWalkable(grid, 12, Surface + 1), "Terra compactada que não é obra continua parede.");

            // Dentro do formigueiro a obra continua parede (só a de fora é caminhável).
            grid.Set(10, 4, CellMaterial.PackedSpoil);
            grid.SetBuilt(10, 4, true);
            Assert.IsFalse(AntNavigation.IsWalkable(grid, 10, 4));

            Assert.IsFalse(grid.SetBuilt(15, 20, true), "Célula vazia não vira obra.");
            grid.Set(5, Surface + 2, CellMaterial.Mud);
            Assert.IsFalse(grid.IsBuilt(5, Surface + 2), "Trocar o material desfaz a obra.");
        }

        [Test]
        public void Colony_PlacedSoilIsMarkedAsBuilt_AndStillGrowsFromTheGroundOverSceneryArt()
        {
            // Arte do cenário cobrindo todo o céu em volta da obra: a célula marcada é caminhável sem apoio, mas a obra
            // ainda precisa crescer a partir do que já existe (nada flutuando na frente do cenário).
            const int surface = 20;
            PixelGrid grid = new PixelGrid(80, 40);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.SetSurfaceY(x, surface);
                grid.Set(x, 0, CellMaterial.Rock);
                for (int y = 1; y <= surface; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int y = 5; y <= surface; y++)
            for (int x = 39; x <= 41; x++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 39; x <= 50; x++)
            for (int y = 5; y <= 7; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 48; x <= 57; x++)
            for (int y = surface + 1; y <= surface + 2; y++)
                grid.Set(x, y, CellMaterial.Spoil);
            for (int x = 58; x <= 68; x++)
            for (int y = surface + 1; y <= surface + 12; y++)
                grid.SetBackdrop(x, y, true);

            for (int x = 62; x <= 63; x++)
            for (int y = surface + 1; y <= surface + 4; y++)
                Assert.IsTrue(grid.SetBuildMarked(x, y, true));

            var colony = new AntColony(grid, new ColonySettings(), new TerrainLayout(40, new Vector2Int(40, 6), 60, 70), seed: 5);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Worker;
            var sim = new GranularSimulation();
            for (int tick = 0; tick < 60 * 240 && grid.BuildMarkedCount > 0; tick++)
            {
                sim.Step(grid);
                colony.Tick(1f / 60f);
                for (int x = 62; x <= 63; x++)
                for (int y = surface + 2; y <= surface + 4; y++)
                {
                    if (grid.IsSolid(x, y))
                        Assert.IsTrue(AntNavigation.TouchesSolid(grid, x, y), $"({x},{y}) foi posta flutuando, sem encostar em nada.");
                }
            }

            Assert.AreEqual(0, grid.BuildMarkedCount, "A obra de fora deveria ter sido toda construída.");
            for (int x = 62; x <= 63; x++)
            for (int y = surface + 1; y <= surface + 4; y++)
            {
                Assert.IsTrue(grid.IsBuilt(x, y), $"({x},{y}) foi posta pela formiga e deveria ser obra.");
                Assert.IsTrue(AntNavigation.IsWalkable(grid, x, y), $"({x},{y}): obra de fora é caminhável por dentro.");
            }
        }

        [Test]
        public void SceneryPainter_BackdropFollowsTheNearArt_AndGoesToTheGrid()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout = TerrainGenerator.Generate(grid, settings, 21);
            var scenery = new SurfaceScenerySettings();
            Color32 sky = new Color32(120, 170, 215, 255);
            SceneryLayers layers = SurfaceSceneryPainter.Paint(grid, layout, scenery, 21, sky, sky);

            int ppc = SurfaceSceneryPainter.PixelsPerCell;
            SceneryLayer near = layers.Near;
            int covered = 0;
            for (int i = 0; i < layers.Backdrop.Length; i++)
            {
                if (!layers.Backdrop[i])
                    continue;
                covered++;
                int cx = i % grid.Width, cy = i / grid.Width;
                bool opaque = false;
                for (int py = cy * ppc; py < (cy + 1) * ppc && !opaque; py++)
                for (int px = cx * ppc; px < (cx + 1) * ppc && !opaque; px++)
                    opaque = near.Get(px, py).a != 0;
                Assert.IsTrue(opaque, $"Célula ({cx},{near.OriginY + cy}) no fundo sem arte nenhuma.");
            }
            Assert.Greater(covered, 0, "O cenário de perto deveria cobrir alguma célula.");

            SurfaceSceneryPainter.ApplyBackdrop(grid, layers);
            int onGrid = 0;
            for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
                onGrid += grid.IsBackdrop(x, y) ? 1 : 0;
            Assert.AreEqual(covered, onGrid);
        }

        [Test]
        public void Snapshot_RoundTripsTheBuiltMark()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout = TerrainGenerator.Generate(grid, settings, 77);
            var colony = new AntColony(grid, new ColonySettings(), layout, 77);
            var rain = new RainSimulation(new RainSettings(), 77);
            int x = layout.EntranceX + 12;
            int y = grid.GetSurfaceY(x) + 1;
            grid.Set(x, y, CellMaterial.PackedSpoil);
            grid.SetBuilt(x, y, true);
            grid.Set(x + 1, y, CellMaterial.PackedSpoil);
            grid.SetMarked(x + 1, y, true);

            GameSnapshot saved = GameSnapshot.Capture(grid, colony, rain, new WaterSimulation(new WaterSettings(), grid, 77), 0, 77, 0);
            var stream = new MemoryStream();
            saved.Write(stream);
            stream.Position = 0;
            GameSnapshot loaded = GameSnapshot.Read(stream);

            var grid2 = new PixelGrid(settings.Width, settings.Height);
            TerrainGenerator.Generate(grid2, settings, 77);
            loaded.ApplyTo(grid2);
            Assert.IsTrue(grid2.IsBuilt(x, y));
            Assert.IsFalse(grid2.IsBuilt(x + 1, y));
            Assert.IsTrue(grid2.IsMarked(x + 1, y), "A marca de escavar continua lida com a obra no mesmo byte.");
        }
    }
}
