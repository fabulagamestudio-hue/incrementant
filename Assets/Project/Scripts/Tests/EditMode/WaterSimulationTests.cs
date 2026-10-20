using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    public sealed class WaterSimulationTests
    {
        const float Tick = 1f / 60f;

        /// <summary>Caixa de rocha: piso em y = 0 e paredes em x = 0 e x = largura − 1. O solo original fica em <paramref name="surfaceY"/>.</summary>
        static PixelGrid MakeBox(int width, int height, int surfaceY = -1)
        {
            var grid = new PixelGrid(width, height);
            for (int x = 0; x < width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, surfaceY);
            }
            for (int y = 1; y < height; y++)
            {
                grid.Set(0, y, CellMaterial.Rock);
                grid.Set(width - 1, y, CellMaterial.Rock);
            }
            return grid;
        }

        static void Run(WaterSimulation water, PixelGrid grid, int steps, GranularSimulation granular = null)
        {
            for (int i = 0; i < steps; i++)
            {
                water.Step(grid, Tick);
                granular?.Step(grid);
            }
        }

        static HashSet<int> WaterCells(PixelGrid grid)
        {
            var cells = new HashSet<int>();
            for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
            {
                if (grid.HasWater(x, y))
                    cells.Add(grid.ToIndex(x, y));
            }
            return cells;
        }

        [Test]
        public void Water_FallsAndSpreadsIntoHorizontalLines()
        {
            PixelGrid grid = MakeBox(20, 16);
            for (int y = 4; y < 14; y++)
                grid.SetWater(10, y, true); // 10 células numa coluna, no ar
            grid.SetWater(9, 15, true);
            grid.SetWater(11, 15, true);
            grid.SetWater(10, 15, true);
            grid.SetWater(8, 3, true);
            grid.SetWater(12, 3, true);
            grid.SetWater(10, 2, true);
            // 16 células numa caixa de 18 de largura: uma linha só, quase cheia.
            Assert.AreEqual(16, grid.WaterCount);

            Run(new WaterSimulation(new WaterSettings(), grid, 1), grid, 600);

            Assert.AreEqual(16, grid.WaterCount, "A água não some nem se duplica ao escorrer.");
            for (int y = 2; y < grid.Height; y++)
            for (int x = 1; x < grid.Width - 1; x++)
                Assert.IsFalse(grid.HasWater(x, y), $"Sobrou água empilhada em ({x},{y}).");
        }

        [Test]
        public void Water_FillsTheBoxLayerByLayer()
        {
            PixelGrid grid = MakeBox(12, 20);
            for (int y = 1; y < 19; y++)
            {
                grid.SetWater(5, y, true);
                if (y < 15)
                    grid.SetWater(6, y, true);
            }
            int total = grid.WaterCount; // 32 = 3 linhas cheias de 10 + 2

            Run(new WaterSimulation(new WaterSettings(), grid, 2), grid, 900);

            Assert.AreEqual(total, grid.WaterCount);
            for (int y = 1; y <= 3; y++)
            for (int x = 1; x <= 10; x++)
                Assert.IsTrue(grid.HasWater(x, y), $"A linha {y} deveria estar cheia (faltou {x}).");
            int fourth = 0;
            for (int x = 1; x <= 10; x++)
                fourth += grid.HasWater(x, 4) ? 1 : 0;
            Assert.AreEqual(2, fourth, "O que sobra fica na linha de cima.");
        }

        [Test]
        public void Water_StopsMovingOnceSettled()
        {
            PixelGrid grid = MakeBox(40, 12);
            for (int x = 3; x < 30; x += 2)
                grid.SetWater(x, 6, true);
            var water = new WaterSimulation(new WaterSettings(), grid, 3);
            Run(water, grid, 400);

            HashSet<int> settled = WaterCells(grid);
            Run(water, grid, 200);
            CollectionAssert.AreEquivalent(settled, WaterCells(grid), "Poça assentada não deveria ficar se mexendo.");
        }

        [Test]
        public void Grains_SinkThroughWater()
        {
            PixelGrid grid = MakeBox(10, 12);
            for (int x = 1; x < 9; x++)
            for (int y = 1; y <= 3; y++)
                grid.SetWater(x, y, true);
            grid.Set(4, 8, CellMaterial.Sand);
            int total = grid.WaterCount;

            Run(new WaterSimulation(new WaterSettings(), grid, 4), grid, 120, new GranularSimulation());

            Assert.AreEqual(CellMaterial.Sand, grid.Get(4, 1), "O grão afunda até o fundo.");
            Assert.AreEqual(total, grid.WaterCount, "A água deslocada pelo grão sobe, não some.");
        }

        [Test]
        public void SolidPlacedOnWater_PushesItUp()
        {
            PixelGrid grid = MakeBox(6, 6);
            grid.SetWater(2, 1, true);
            grid.Set(2, 1, CellMaterial.PackedSpoil);
            Assert.IsTrue(grid.HasWater(2, 2));
            Assert.AreEqual(1, grid.WaterCount);
        }

        [Test]
        public void Water_ErodesFixedDirtInsideTheAnthill()
        {
            // Solo original no alto: tudo é "dentro do formigueiro".
            PixelGrid grid = MakeBox(8, 8, surfaceY: 7);
            grid.Set(2, 1, CellMaterial.Rock);
            grid.SetWater(3, 1, true);
            grid.Set(4, 1, CellMaterial.Dirt);
            grid.Set(4, 2, CellMaterial.PackedSpoil);
            var settings = new WaterSettings { ErosionSeconds = 1f, CheckInterval = 0.25f };
            var water = new WaterSimulation(settings, grid, 5);

            Run(water, grid, 30); // 0,5 s
            Assert.AreEqual(CellMaterial.Dirt, grid.Get(4, 1), "Ainda não deu o tempo de contato.");

            Run(water, grid, 40); // ~1,2 s
            Assert.AreEqual(CellMaterial.Mud, grid.Get(4, 1), "Terra fixa encostada na água vira terra mole.");
            Assert.AreEqual(1, water.ErodedCells, "Só a célula que encosta na água amolece.");
            Assert.AreEqual(0, grid.WaterCount, "A água que amoleceu a terra é consumida (não forma lago no formigueiro).");
            Assert.IsTrue(CellMaterials.IsGranular(CellMaterial.Mud));
            Assert.IsTrue(CellMaterials.IsDiggable(CellMaterial.Mud));
            Assert.AreEqual(CellMaterial.Rock, grid.Get(2, 1), "Pedra não amolece.");
        }

        [Test]
        public void Water_DoesNotErodeOutsideTheAnthill()
        {
            PixelGrid grid = MakeBox(8, 8, surfaceY: 0); // tudo acima do solo original: é céu
            grid.Set(2, 1, CellMaterial.Rock);
            grid.SetWater(3, 1, true);
            grid.Set(4, 1, CellMaterial.Dirt);
            var water = new WaterSimulation(new WaterSettings { ErosionSeconds = 0.5f }, grid, 6);

            Run(water, grid, 300);

            Assert.AreEqual(CellMaterial.Dirt, grid.Get(4, 1));
            Assert.AreEqual(0, water.ErodedCells);
        }

        [Test]
        public void SurfaceWater_IsNeverConsumedOutsideTheAnthill()
        {
            // Decisão do desenvolvedor (2026-10-17): a água só é consumida dentro do formigueiro. No céu ela não
            // infiltra (nem em grama/areia/terra solta) nem evapora, e os laguinhos não secam com o tempo.
            PixelGrid grid = MakeBox(12, 6); // tudo é céu (solo original abaixo da grade)
            CellMaterial[] floor =
            {
                CellMaterial.Grass, CellMaterial.Dirt, CellMaterial.Sand, CellMaterial.Spoil, CellMaterial.Mud,
                CellMaterial.PackedSand, CellMaterial.PackedSpoil, CellMaterial.Rock, CellMaterial.Grass, CellMaterial.Dirt,
            };
            for (int x = 1; x < 11; x++)
            {
                grid.Set(x, 1, floor[x - 1]);
                grid.SetWater(x, 2, true);
                grid.SetWater(x, 3, true);
            }
            int total = grid.WaterCount;
            var water = new WaterSimulation(new WaterSettings { ErosionSeconds = 0.5f }, grid, 8);

            Run(water, grid, 6000); // 100 s

            Assert.AreEqual(total, grid.WaterCount);
            Assert.AreEqual(0, water.AbsorbedCells);
            Assert.AreEqual(0, water.ErodedCells);
        }

        [Test]
        public void LightRain_DoesNotCreateWater()
        {
            PixelGrid grid = MakeBox(6, 30);
            var rain = new RainSimulation(new RainSettings { LightDropsPer100Columns = 2000f }, 3) { Weather = Weather.LightRain };

            for (int i = 0; i < 120; i++)
                rain.Step(grid, Tick);

            Assert.AreEqual(0, grid.WaterCount);
            Assert.AreEqual(0, rain.WaterCells);
        }

        [Test]
        public void Water_IsUsedUpWhileSofteningTheTunnelWalls()
        {
            // Poço de 1 célula de largura cavado em terra, com 5 células de água: cada célula amolecida gasta uma.
            PixelGrid grid = MakeBox(7, 12, surfaceY: 11);
            for (int y = 1; y < 11; y++)
            for (int x = 1; x < 6; x++)
                grid.Set(x, y, x == 3 ? CellMaterial.Empty : CellMaterial.Dirt);
            for (int y = 1; y <= 5; y++)
                grid.SetWater(3, y, true);
            var water = new WaterSimulation(new WaterSettings { ErosionSeconds = 0.5f }, grid, 9);

            Run(water, grid, 1200, new GranularSimulation());

            Assert.AreEqual(0, grid.WaterCount, "A água acaba molhando a terra; não sobra lago no túnel.");
            Assert.Greater(water.ErodedCells, 0);
            Assert.AreEqual(5, water.ErodedCells + water.AbsorbedCells, "Cada célula de água molha o solo uma vez e some.");
        }

        [Test]
        public void Water_PooledOnMudInsideTheAnthillDrainsAway()
        {
            // Poça sobre um piso de terra mole entre paredes de pedra: sem terra fixa para amolecer, o solo mole absorve.
            PixelGrid grid = MakeBox(8, 8, surfaceY: 7);
            for (int x = 1; x < 7; x++)
                grid.Set(x, 1, CellMaterial.Mud);
            for (int x = 1; x < 7; x++)
            for (int y = 2; y <= 3; y++)
                grid.SetWater(x, y, true);
            var water = new WaterSimulation(new WaterSettings { ErosionSeconds = 0.5f }, grid, 10);

            Run(water, grid, 600);

            Assert.AreEqual(0, grid.WaterCount);
            Assert.AreEqual(12, water.AbsorbedCells);
            Assert.AreEqual(0, water.ErodedCells);
            Assert.AreEqual(CellMaterial.Rock, grid.Get(0, 2), "Pedra não absorve nem amolece.");
        }

        [Test]
        public void Rain_LeavesWaterWhereTheDropStops()
        {
            PixelGrid grid = MakeBox(6, 30);
            var settings = new RainSettings { HeavyDropsPer100Columns = 2000f, HeavyWaterChance = 1f };
            var rain = new RainSimulation(settings, 3) { Weather = Weather.HeavyRain };

            for (int i = 0; i < 60; i++)
                rain.Step(grid, Tick);

            Assert.Greater(grid.WaterCount, 0);
            Assert.AreEqual(rain.WaterCells, grid.WaterCount);
            for (int x = 1; x < 5; x++)
            {
                // Sem simulação da água, as gotas empilham: a coluna é água contínua a partir do piso.
                int top = 0;
                while (grid.HasWater(x, top + 1))
                    top++;
                for (int y = top + 1; y < grid.Height; y++)
                    Assert.IsFalse(grid.HasWater(x, y), $"Água solta no ar em ({x},{y}).");
            }
        }

        [Test]
        public void Generator_FillsSurfaceLakesThatStayPut()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout = TerrainGenerator.Generate(grid, settings, 99);

            Assert.Greater(grid.WaterCount, 0, "O mapa padrão deveria ter ao menos um laguinho.");
            HashSet<int> all = WaterCells(grid);
            var lake = new HashSet<int>();
            int deepest = 0;
            foreach (int i in all)
            {
                int x = i % grid.Width, y = i / grid.Width;
                if (y <= grid.GetSurfaceY(x))
                    continue; // bolsão subterrâneo, testado à parte
                lake.Add(i);
                deepest = Mathf.Max(deepest, y - grid.GetSurfaceY(x));
                Assert.Greater(Mathf.Abs(x - layout.EntranceX), settings.DumpOffset.y, "Laguinho longe da entrada e do despejo.");
            }
            Assert.Greater(lake.Count, 0);
            Assert.GreaterOrEqual(deepest, settings.LakeDepth.x - 1, "Laguinhos fundos (KI-028).");

            var water = new WaterSimulation(new WaterSettings(), grid, 99);
            Run(water, grid, 3600, new GranularSimulation()); // 60 s: o laguinho não seca com o tempo
            foreach (int i in lake)
                Assert.IsTrue(grid.HasWater(i % grid.Width, i / grid.Width), "O laguinho gerado já nasce assentado e contido.");
        }

        [Test]
        public void Generator_CreatesSealedUndergroundWaterPocketsThatStayPut()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainGenerator.Generate(grid, settings, 99);

            var pockets = new HashSet<int>();
            foreach (int i in WaterCells(grid))
            {
                int x = i % grid.Width, y = i / grid.Width;
                if (y > grid.GetSurfaceY(x))
                    continue;
                pockets.Add(i);
                Assert.LessOrEqual(y, grid.GetSurfaceY(x) - settings.WaterPocketMinDepth, "Bolsão fundo, longe do ninho inicial.");
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    bool wet = grid.HasWater(nx, ny);
                    Assert.IsTrue(wet || grid.Get(nx, ny) == CellMaterial.Rock, $"Bolsão de ({x},{y}) vaza para ({nx},{ny}).");
                }
            }
            Assert.Greater(pockets.Count, 0, "O mapa padrão deveria ter bolsões de água subterrâneos.");

            var water = new WaterSimulation(new WaterSettings(), grid, 99);
            Run(water, grid, 1800, new GranularSimulation());
            foreach (int i in pockets)
                Assert.IsTrue(grid.HasWater(i % grid.Width, i / grid.Width), "A água do bolsão fechado não some nem escorre.");
            Assert.AreEqual(0, water.ErodedCells);
        }
    }
}
