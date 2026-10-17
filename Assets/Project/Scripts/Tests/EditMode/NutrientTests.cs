using System.IO;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    public sealed class NutrientTests
    {
        const float Dt = 1f / 60f;
        const int Surface = 10;
        const int BladeX = 40;

        /// <summary>
        /// Solo de terra até <see cref="Surface"/>, piso de rocha em y = 0, corredor (y 1..3) de x = 2 até a largura − 3 e
        /// um poço em x = 20 do corredor até a superfície.
        /// </summary>
        static PixelGrid MakeWorld(int width = 60, int height = 40)
        {
            var grid = new PixelGrid(width, height);
            for (int x = 0; x < width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, Surface);
                for (int y = 1; y <= Surface; y++)
                    grid.Set(x, y, y == Surface ? CellMaterial.Grass : CellMaterial.Dirt);
            }
            for (int x = 2; x < width - 2; x++)
            for (int y = 1; y <= 3; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int y = 1; y <= Surface; y++)
                grid.Set(20, y, CellMaterial.Empty);
            return grid;
        }

        /// <summary>Folha de grama em <see cref="BladeX"/>: raiz logo acima do solo e <paramref name="height"/> pixels ao todo, marcada.</summary>
        static void PlantBlade(PixelGrid grid, int height)
        {
            for (int i = 0; i < height; i++)
            {
                grid.Set(BladeX, Surface + 1 + i, CellMaterial.PlantGrass);
                Assert.IsTrue(grid.SetMarked(BladeX, Surface + 1 + i, true), "Planta aceita marca de colheita.");
            }
        }

        static int BladeHeight(PixelGrid grid)
        {
            int height = 0;
            while (grid.Get(BladeX, Surface + 1 + height) == CellMaterial.PlantGrass)
                height++;
            return height;
        }

        static int BladeCells(PixelGrid grid)
        {
            int cells = 0;
            for (int y = Surface + 1; y < grid.Height; y++)
                cells += grid.Get(BladeX, y) == CellMaterial.PlantGrass ? 1 : 0;
            return cells;
        }

        static void PaintNutrientRoom(PixelGrid grid, int xMin, int xMax, int yMin, int yMax)
        {
            for (int x = xMin; x <= xMax; x++)
            for (int y = yMin; y <= yMax; y++)
                Assert.IsTrue(grid.SetNutrientRoom(x, y, true), $"({x},{y}) deveria aceitar a sala de nutrientes.");
        }

        static TerrainLayout Layout => new TerrainLayout(20, new Vector2Int(20, 1), 50, 55);

        [Test]
        public void NutrientRoom_OnlyAcceptsAnthillCells_AndNutrientsDoNotBlockAnts()
        {
            PixelGrid grid = MakeWorld();
            Assert.IsTrue(grid.SetNutrientRoom(30, 1, true));
            Assert.IsFalse(grid.SetNutrientRoom(30, Surface + 2, true), "O céu não é sala.");
            Assert.IsTrue(grid.SetQueenRoom(31, 1, true));
            Assert.IsFalse(grid.SetNutrientRoom(31, 1, true), "Não pinta por cima da sala da rainha.");

            int structure = grid.StructureVersion;
            Assert.IsTrue(grid.SetNutrient(30, 1, true));
            Assert.AreEqual(1, grid.NutrientCount);
            Assert.AreEqual(structure, grid.StructureVersion, "Nutriente não é material: a estrutura não muda.");
            Assert.IsTrue(AntNavigation.IsWalkable(grid, 30, 1), "Slot ocupado continua caminhável.");
            Assert.IsFalse(grid.SetNutrient(25, 5, true), "Célula com material não guarda nutriente.");

            grid.Set(30, 1, CellMaterial.Spoil);
            Assert.AreEqual(0, grid.NutrientCount, "Material por cima destrói o nutriente.");
        }

        [Test]
        public void StoredPixel_CuresBeforeBecomingAReadyNutrient()
        {
            PixelGrid grid = MakeWorld();
            grid.SetNutrientRoom(30, 1, true);
            Assert.IsTrue(grid.SetNutrient(30, 1, true, 10f));
            Assert.IsTrue(grid.HasNutrient(30, 1), "Ocupa o slot enquanto cura.");
            Assert.IsFalse(grid.HasReadyNutrient(30, 1));
            Assert.AreEqual(0, grid.ReadyNutrientCount);

            grid.AdvanceNutrientCure(9.5f);
            Assert.IsFalse(grid.HasReadyNutrient(30, 1));
            int version = grid.NutrientVersion;
            grid.AdvanceNutrientCure(0.6f);
            Assert.IsTrue(grid.HasReadyNutrient(30, 1));
            Assert.AreEqual(1, grid.ReadyNutrientCount);
            Assert.AreNotEqual(version, grid.NutrientVersion, "Curar refaz o campo de busca.");

            // Retirado ainda curando, o slot volta limpo: o próximo pixel começa a cura do zero.
            grid.SetNutrient(30, 1, false);
            grid.SetNutrient(30, 1, true, 10f);
            grid.SetNutrient(30, 1, false);
            Assert.AreEqual(0, grid.GetNutrientCure(30, 1));
            Assert.AreEqual(0, grid.NutrientCount - grid.ReadyNutrientCount);
        }

        [Test]
        public void Colony_WaitsForTheStoredPixelToCure_BeforeTakingItToTheQueen()
        {
            PixelGrid grid = MakeWorld();
            PaintNutrientRoom(grid, 30, 33, 1, 2);
            grid.SetNutrient(31, 1, true, 5f);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3);

            for (int i = 0; i < 60 * 4; i++)
            {
                colony.Tick(Dt);
                foreach (Ant ant in colony.Ants)
                    Assert.AreNotEqual(AntState.Fetching, ant.State, "Ninguém busca o pixel ainda curando.");
            }
            Assert.IsTrue(grid.HasNutrient(31, 1));
            Assert.AreEqual(0, colony.TotalNutrients);

            for (int i = 0; i < 60 * 60 && colony.TotalNutrients == 0; i++)
                colony.Tick(Dt);
            Assert.AreEqual(1, colony.TotalNutrients, "Curado, vai para a rainha.");
        }

        [Test]
        public void Plants_AreHarvestedFromTheTipDown_AndNeverAtTheRoot()
        {
            PixelGrid grid = MakeWorld();
            PlantBlade(grid, 3);
            Assert.IsFalse(VegetationSimulation.CanHarvest(grid, BladeX, Surface + 1), "Raiz.");
            Assert.IsFalse(VegetationSimulation.CanHarvest(grid, BladeX, Surface + 2), "Tem planta em cima.");
            Assert.IsTrue(VegetationSimulation.CanHarvest(grid, BladeX, Surface + 3), "Ponta.");
            Assert.IsFalse(CellMaterials.IsDiggable(CellMaterial.PlantGrass), "O pincel de cavar não pega plantas.");
        }

        [Test]
        public void FallingSoil_PassesBehindPlants_AndThePlantComesBackWhenTheSoilLeaves()
        {
            PixelGrid grid = MakeWorld();
            PlantBlade(grid, 5);
            int top = Surface + 5;
            grid.Set(BladeX, top + 3, CellMaterial.Spoil, 77);
            var granular = new GranularSimulation();
            for (int i = 0; i < 20; i++)
                granular.Step(grid);

            Assert.AreEqual(CellMaterial.Spoil, grid.Get(BladeX, Surface + 1), "O grão atravessa a planta até o chão.");
            Assert.AreEqual(77, grid.GetShade(BladeX, Surface + 1));
            Assert.AreEqual(CellMaterial.PlantGrass, grid.GetBehind(BladeX, Surface + 1), "A raiz fica atrás do grão.");
            Assert.IsFalse(grid.IsMarked(BladeX, Surface + 1), "A marca de colheita do pedaço coberto sai.");
            for (int y = Surface + 2; y <= top; y++)
            {
                Assert.AreEqual(CellMaterial.PlantGrass, grid.Get(BladeX, y), $"A planta continua inteira em y = {y}.");
                Assert.AreEqual(CellMaterial.Empty, grid.GetBehind(BladeX, y));
            }

            // Save com a planta coberta: volta igual.
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3);
            var snapshot = GameSnapshot.Capture(grid, colony, new RainSimulation(new RainSettings(), 1),
                                                new WaterSimulation(new WaterSettings(), grid, 1), 0, 1, 0);
            using var stream = new MemoryStream();
            snapshot.Write(stream);
            stream.Position = 0;
            PixelGrid loaded = MakeWorld();
            GameSnapshot.Read(stream).ApplyTo(loaded);
            Assert.AreEqual(CellMaterial.Spoil, loaded.Get(BladeX, Surface + 1));
            Assert.AreEqual(CellMaterial.PlantGrass, loaded.GetBehind(BladeX, Surface + 1), "O save guarda a planta coberta.");

            // Cavado o grão, a planta volta para a célula.
            grid.Set(BladeX, Surface + 1, CellMaterial.Empty, 0);
            Assert.AreEqual(CellMaterial.PlantGrass, grid.Get(BladeX, Surface + 1));
            Assert.AreEqual(CellMaterial.Empty, grid.GetBehind(BladeX, Surface + 1));
        }

        [Test]
        public void Vegetation_RegrowsFromTheBase_PushingTheWholePlantUp()
        {
            PixelGrid grid = MakeWorld(400);
            var settings = new VegetationSettings();
            var vegetation = new VegetationSimulation(settings, seed: 11, Layout, grid.Width);
            int plantX = -1;
            PlantShape shape = null;
            for (int x = 60; x < 390 && plantX < 0; x++)
            {
                if (vegetation.TryGetPlant(x, out _, out shape, out _))
                    plantX = x;
            }
            Assert.GreaterOrEqual(plantX, 0, "A seed deveria ter alguma planta longe da entrada.");

            vegetation.Populate(grid);
            Assert.AreEqual(shape.Cells.Length, CountPlant(grid, plantX), "A planta nasce completa.");
            Assert.AreEqual(0, vegetation.GetSunkRows(plantX));

            // Completa e com a ponta colhida: ainda tem pixel para colher, então não cresce.
            int top = TopOfPlant(grid, plantX);
            for (int dx = -1; dx <= 1; dx++)
                grid.Set(plantX + dx, top, CellMaterial.Empty);
            int remaining = CountPlant(grid, plantX);
            Assert.Greater(TopOfPlant(grid, plantX), Surface + 1, "O teste precisa de pixels colhíveis acima do chão.");
            RunGrowStep(vegetation, grid, settings);
            Assert.AreEqual(0, vegetation.GetSunkRows(plantX), "Com o que colher, a planta completa espera.");
            Assert.AreEqual(remaining, CountPlant(grid, plantX), "Não repõe nada enquanto há o que colher.");

            // Colhe tudo menos a linha do chão (que nunca é colhida) e marca a raiz.
            for (int y = Surface + 2; y < grid.Height; y++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (CellMaterials.IsPlant(grid.Get(plantX + dx, y)))
                    grid.Set(plantX + dx, y, CellMaterial.Empty);
            }
            Assert.IsTrue(grid.SetMarked(plantX, Surface + 1, true));

            // Sem nada para colher, reinicia: volta inteira para debaixo da terra com só a ponta para fora, sem marca.
            RunGrowStep(vegetation, grid, settings);
            Assert.AreEqual(shape.Height - 1, vegetation.GetSunkRows(plantX), "Reinicia com só a ponta para fora.");
            Assert.AreEqual(Surface + 1, TopOfPlant(grid, plantX));
            Assert.AreEqual(0, grid.MarkedCount, "A planta reiniciada nasce sem marca.");

            // O jogador marca a ponta: a marca sobe junto com o pixel, e o que cresce embaixo nasce sem marca.
            Assert.IsTrue(grid.SetMarked(plantX, Surface + 1, true));
            for (int i = 1; i < shape.Height; i++)
            {
                RunGrowStep(vegetation, grid, settings);
                Assert.AreEqual(shape.Height - 1 - i, vegetation.GetSunkRows(plantX));
                Assert.AreEqual(Surface + 1 + i, TopOfPlant(grid, plantX), "A planta inteira sobe 1 linha por passo.");
                Assert.IsTrue(grid.IsMarked(plantX, Surface + 1 + i), "A marca de colheita sobe junto com o pixel.");
                Assert.AreEqual(1, grid.MarkedCount, "O que cresce não nasce marcado.");
            }
            Assert.AreEqual(shape.Cells.Length, CountPlant(grid, plantX), "Termina com a forma completa.");

            RunGrowStep(vegetation, grid, settings);
            Assert.AreEqual(shape.Cells.Length, CountPlant(grid, plantX), "Completa, para de crescer.");
        }

        [Test]
        public void Vegetation_RegrowsOverUnevenGround_EveryColumnEmergesAboveItsOwnSoil()
        {
            PixelGrid grid = MakeWorld(400);
            var settings = new VegetationSettings();
            var vegetation = new VegetationSimulation(settings, seed: 11, Layout, grid.Width);
            int plantX = -1;
            PlantShape shape = null;
            for (int x = 60; x < 390 && plantX < 0; x++)
            {
                if (vegetation.TryGetPlant(x, out PlantKind kind, out shape, out _) && kind == PlantKind.Leafy)
                    plantX = x;
            }
            Assert.GreaterOrEqual(plantX, 0, "A seed deveria ter alguma muda folhosa (com folhas fora da coluna da raiz).");

            // O chão das colunas vizinhas fica 3 linhas acima da raiz: ali a forma sai da terra mais tarde.
            foreach (int cx in new[] { plantX - 1, plantX + 1 })
            {
                for (int y = Surface + 1; y <= Surface + 3; y++)
                    grid.Set(cx, y, CellMaterial.Dirt);
                grid.SetSurfaceY(cx, Surface + 3);
            }
            vegetation.Populate(grid);
            for (int y = Surface + 2; y < grid.Height; y++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (CellMaterials.IsPlant(grid.Get(plantX + dx, y)))
                    grid.Set(plantX + dx, y, CellMaterial.Empty);
            }

            for (int i = 0; i <= shape.Height; i++)
                RunGrowStep(vegetation, grid, settings);

            Assert.AreEqual(0, vegetation.GetSunkRows(plantX));
            int rootY = Surface + 1;
            foreach (Vector2Int c in shape.Cells)
            {
                int cx = plantX + c.x, cy = rootY + c.y;
                if (cy > grid.GetSurfaceY(cx))
                    Assert.IsTrue(CellMaterials.IsPlant(grid.Get(cx, cy)), $"Célula ({c.x},{c.y}) acima do chão deveria ter crescido.");
            }
        }

        static int TopOfPlant(PixelGrid grid, int x)
        {
            int top = 0;
            for (int y = Surface + 1; y < grid.Height; y++)
            for (int dx = -1; dx <= 1; dx++)
                if (CellMaterials.IsPlant(grid.Get(x + dx, y)))
                    top = y;
            return top;
        }

        /// <summary>Avança exatamente um período de crescimento (um passo para cada planta).</summary>
        static void RunGrowStep(VegetationSimulation vegetation, PixelGrid grid, VegetationSettings settings)
        {
            const float tick = 0.25f;
            for (int i = 0, n = Mathf.RoundToInt(settings.GrowSeconds / tick); i < n; i++)
                vegetation.Step(grid, tick);
        }

        [Test]
        public void PlantShape_IsTheSpriteSilhouette_InCells()
        {
            // Desenho de 3x4 células (6 px por célula), pivô no centro da coluna do meio, 1 célula enterrada:
            // um caule no meio até o topo e uma folha à direita na 2ª linha acima do chão.
            const int ppc = 6;
            int width = 3 * ppc, height = 4 * ppc;
            var pixels = new Color32[width * height];
            var green = new Color32(60, 140, 60, 255);
            void Fill(int cx, int cy)
            {
                for (int y = cy * ppc; y < (cy + 1) * ppc; y++)
                for (int x = cx * ppc; x < (cx + 1) * ppc; x++)
                    pixels[y * width + x] = green;
            }
            for (int cy = 0; cy < 4; cy++)
                Fill(1, cy);
            Fill(2, 2);
            pixels[3 * ppc * width + 0] = green; // 1 pixel solto: não vira célula

            PlantShape shape = PlantShape.FromPixels(pixels, width, height, pivotX: ppc + ppc / 2, pivotY: ppc, ppc);
            CollectionAssert.AreEqual(
                new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(0, 2) },
                shape.Cells, "Silhueta acima do chão, em ordem de crescimento (de baixo para cima, do centro para fora).");

            var vegetation = new VegetationSimulation(new VegetationSettings(), seed: 11, Layout, 400,
                                                      grassShapes: new[] { shape }, leafyShapes: new[] { shape });
            int found = 0;
            for (int x = 60; x < 400; x++)
            {
                if (!vegetation.TryGetPlant(x, out _, out PlantShape plant, out _))
                    continue;
                found++;
                Assert.AreSame(shape, plant, "Com arte, a planta tem a forma do sprite.");
            }
            Assert.Greater(found, 0);
        }

        static int CountPlant(PixelGrid grid, int x)
        {
            int count = 0;
            for (int y = Surface + 1; y < grid.Height; y++)
            for (int dx = -1; dx <= 1; dx++)
                count += CellMaterials.IsPlant(grid.Get(x + dx, y)) ? 1 : 0;
            return count;
        }

        [Test]
        public void Colony_HarvestsMarkedPlant_StoresNutrients_AndTheQueenConsumesThem()
        {
            PixelGrid grid = MakeWorld();
            PlantBlade(grid, 9);
            PaintNutrientRoom(grid, 30, 35, 1, 3);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 7);
            Assert.AreEqual(0, colony.Nutrients);

            int lastHeight = BladeHeight(grid);
            for (int i = 0; i < 60 * 120 && colony.TotalNutrients < 8; i++)
            {
                colony.Tick(Dt);
                int height = BladeHeight(grid);
                Assert.AreEqual(height, BladeCells(grid), "Colhe da ponta para a base: não sobra buraco no meio.");
                Assert.LessOrEqual(height, lastHeight);
                lastHeight = height;
            }

            Assert.AreEqual(1, BladeHeight(grid), "Colhe tudo menos a raiz.");
            Assert.AreEqual(8, colony.HarvestedPixels);
            Assert.AreEqual(8, colony.TotalNutrients, "Cada pixel colhido vira um nutriente consumido pela rainha.");
            Assert.AreEqual(8, colony.Nutrients);
            Assert.AreEqual(0, grid.NutrientCount, "A sala esvazia quando a rainha consome tudo.");
        }

        [Test]
        public void Colony_StopsHarvesting_WhenTheNutrientRoomIsFull()
        {
            PixelGrid grid = MakeWorld();
            PlantBlade(grid, 10);
            PaintNutrientRoom(grid, 30, 32, 1, 1); // 3 slots
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3);
            // Tranca a rainha no ninho: os nutrientes ficam na sala e ela enche.
            for (int y = 1; y <= 3; y++)
                grid.Set(23, y, CellMaterial.Rock);
            for (int y = 4; y <= Surface; y++)
                grid.Set(20, y, CellMaterial.Rock);
            foreach (Ant ant in colony.Ants)
            {
                ant.X = 26;
                ant.Y = 1;
                ant.FromX = ant.X;
                ant.FromY = ant.Y;
            }
            // Sem a rainha, as ociosas não teriam para onde voltar: a saída fica do lado delas.
            for (int y = 4; y <= Surface; y++)
                grid.Set(26, y, CellMaterial.Empty);

            for (int i = 0; i < 60 * 90; i++)
                colony.Tick(Dt);

            Assert.AreEqual(3, grid.NutrientCount, "Enche os 3 slots.");
            Assert.AreEqual(3, colony.HarvestedPixels, "Sem slot livre, ninguém colhe mais.");
            Assert.AreEqual(7, BladeCells(grid));
            Assert.IsFalse(colony.CanHarvest);
            Assert.AreEqual(0, colony.TotalNutrients, "A rainha trancada não recebe nada.");
        }

        [Test]
        public void BuyingAnAnt_SpendsNutrients()
        {
            PixelGrid grid = MakeWorld();
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 1);
            int before = colony.Ants.Count;
            Assert.IsFalse(colony.TryBuyAnt(), "Sem nutrientes não compra.");
            colony.RestoreNutrients(colony.NextAntCost, 0, 0);
            Assert.IsTrue(colony.TryBuyAnt());
            Assert.AreEqual(before + 1, colony.Ants.Count);
            Assert.AreEqual(0, colony.Nutrients);
        }

        [Test]
        public void Snapshot_RoundTripsNutrientRoomPlantsAndEconomy()
        {
            PixelGrid grid = MakeWorld();
            PlantBlade(grid, 4);
            PaintNutrientRoom(grid, 30, 33, 1, 2);
            grid.SetNutrient(31, 1, true);
            grid.SetNutrient(32, 1, true, 7.3f);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3);
            colony.RestoreNutrients(5, 12, 13);

            var rain = new RainSimulation(new RainSettings(), 3);
            var water = new WaterSimulation(new WaterSettings(), grid, 3);
            var vegetation = new VegetationSimulation(new VegetationSettings(), 3, Layout, grid.Width);
            vegetation.SetSunkRows(BladeX, 5);
            GameSnapshot saved = GameSnapshot.Capture(grid, colony, rain, water, 0, 3, 1.0, vegetation);
            var stream = new MemoryStream();
            saved.Write(stream);
            stream.Position = 0;
            GameSnapshot loaded = GameSnapshot.Read(stream);

            PixelGrid grid2 = MakeWorld();
            loaded.ApplyTo(grid2);
            var colony2 = new AntColony(grid2, new ColonySettings(), Layout, seed: 3);
            loaded.ApplyTo(colony2);
            var vegetation2 = new VegetationSimulation(new VegetationSettings(), 3, Layout, grid2.Width);
            loaded.ApplyTo(vegetation2);
            Assert.AreEqual(5, vegetation2.GetSunkRows(BladeX), "As linhas enterradas da planta voltam com o save.");

            Assert.AreEqual(8, grid2.NutrientRoomCount);
            Assert.IsTrue(grid2.HasReadyNutrient(31, 1));
            Assert.IsTrue(grid2.HasNutrient(32, 1));
            Assert.AreEqual(8f, grid2.GetNutrientCure(32, 1), "A cura que falta volta arredondada para cima.");
            Assert.AreEqual(4, BladeCells(grid2));
            Assert.IsTrue(grid2.IsMarked(BladeX, Surface + 4), "A marca de colheita volta com a planta.");
            Assert.AreEqual(5, colony2.Nutrients);
            Assert.AreEqual(12, colony2.TotalNutrients);
            Assert.AreEqual(13, colony2.HarvestedPixels);
        }
    }
}
