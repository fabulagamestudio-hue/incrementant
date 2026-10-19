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

        /// <summary>Folha de grama em <see cref="BladeX"/>: raiz logo acima do solo e <paramref name="height"/> pixels ao todo.</summary>
        static void PlantBlade(PixelGrid grid, int height)
        {
            for (int i = 0; i < height; i++)
                grid.Set(BladeX, Surface + 1 + i, CellMaterial.PlantGrass);
        }

        /// <summary>
        /// Mundo largo com uma única planta nascida da seed (longe da entrada e do despejo), completa, e a sala de nutrientes
        /// pintada. As outras plantas da seed são apagadas para o alvo de corte ser sempre esta.
        /// </summary>
        static (PixelGrid grid, VegetationSimulation vegetation, int plantX, PlantShape shape) MakeGarden(PlantShape[] shapes = null)
        {
            PixelGrid grid = MakeWorld(400);
            var vegetation = shapes == null
                ? new VegetationSimulation(new VegetationSettings(), seed: 11, Layout, grid.Width)
                : new VegetationSimulation(new VegetationSettings(), seed: 11, Layout, grid.Width, shapes, shapes);
            vegetation.Populate(grid);
            int plantX = -1;
            PlantShape shape = null;
            foreach (int x in vegetation.Plants)
            {
                if (plantX < 0)
                {
                    plantX = x;
                    vegetation.TryGetPlant(x, out _, out shape, out _);
                    continue;
                }
                vegetation.TryGetPlant(x, out _, out PlantShape other, out _);
                foreach (Vector2Int c in other.Cells)
                    grid.Set(x + c.x, Surface + 1 + c.y, CellMaterial.Empty);
            }
            Assert.GreaterOrEqual(plantX, 0, "A seed deveria ter alguma planta longe da entrada.");
            PaintNutrientRoom(grid, 24, 47, 1, 3);
            return (grid, vegetation, plantX, shape);
        }

        /// <summary>Canteiros só com terra no chão do corredor (y = 1), nas colunas dadas.</summary>
        static void AddBeds(PixelGrid grid, params int[] xs)
        {
            foreach (int x in xs)
            {
                grid.Set(x, 1, CellMaterial.Spoil);
                Assert.IsTrue(grid.SetBed(x, 1, BedStage.Soil), $"({x},1) deveria virar canteiro.");
            }
        }

        /// <summary>
        /// Poça parada no fim do corredor (y 1 e 2, de x = 300 em diante): a água que as formigas tiram para regar. Sem a
        /// simulação da água no teste, ela não escorre.
        /// </summary>
        static void AddPuddle(PixelGrid grid, int width = 90)
        {
            for (int x = 300; x < 300 + width; x++)
            for (int y = 1; y <= 2; y++)
                grid.SetWater(x, y, true);
        }

        static int BedsIn(PixelGrid grid, BedStage stage)
        {
            int count = 0;
            for (int k = 0; k < grid.BedCount; k++)
                count += grid.TryGetBed(k, out int x, out int y) && grid.GetBedStage(x, y) == stage ? 1 : 0;
            return count;
        }

        /// <summary>Sala de nutrientes pronta para o ciclo: quatro canteiros com terra e água para regar.</summary>
        static void PrepareCultivation(PixelGrid grid)
        {
            AddBeds(grid, 24, 27, 30, 33);
            AddPuddle(grid);
        }

        static int PlantCells(PixelGrid grid, int plantX, PlantShape shape)
        {
            int cells = 0;
            for (int y = Surface + 1; y < grid.Height; y++)
            for (int x = plantX + shape.MinDx; x <= plantX + shape.MaxDx; x++)
                cells += CellMaterials.IsPlant(grid.Get(x, y)) ? 1 : 0;
            return cells;
        }

        static int PlantCellsInRow(PixelGrid grid, int plantX, PlantShape shape, int y)
        {
            int cells = 0;
            for (int x = plantX + shape.MinDx; x <= plantX + shape.MaxDx; x++)
                cells += CellMaterials.IsPlant(grid.Get(x, y)) ? 1 : 0;
            return cells;
        }

        static int TopRow(PixelGrid grid, int plantX, PlantShape shape)
        {
            for (int y = grid.Height - 1; y > Surface; y--)
            {
                if (PlantCellsInRow(grid, plantX, shape, y) > 0)
                    return y;
            }
            return Surface;
        }

        /// <summary>Pixels que podem ser cortados na forma (tudo menos a linha do chão, que é a raiz).</summary>
        static int HarvestableCells(PlantShape shape)
        {
            int cells = 0;
            foreach (Vector2Int c in shape.Cells)
                cells += c.y > 0 ? 1 : 0;
            return cells;
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
        public void Plants_CanBeCutAnywhere_ButNeverAtTheRoot()
        {
            PixelGrid grid = MakeWorld();
            PlantBlade(grid, 3);
            Assert.IsFalse(VegetationSimulation.CanHarvest(grid, BladeX, Surface + 1), "Raiz.");
            Assert.IsTrue(VegetationSimulation.CanHarvest(grid, BladeX, Surface + 2), "Qualquer pixel acima da raiz pode ser cortado, não só a ponta.");
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

            // Colhe tudo menos a linha do chão (que nunca é colhida).
            for (int y = Surface + 2; y < grid.Height; y++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (CellMaterials.IsPlant(grid.Get(plantX + dx, y)))
                    grid.Set(plantX + dx, y, CellMaterial.Empty);
            }

            // Sem nada para colher, reinicia: volta inteira para debaixo da terra, sem nada para fora.
            RunGrowStep(vegetation, grid, settings);
            Assert.AreEqual(shape.Height, vegetation.GetSunkRows(plantX), "Reinicia toda enterrada.");
            Assert.AreEqual(0, CountPlant(grid, plantX), "Nada da planta aparece acima do chão ao reiniciar.");
            Assert.AreEqual(0, grid.MarkedCount, "Planta não tem marca de colheita.");

            // Cresce pela base: a ponta sai primeiro; depois a planta inteira sobe 1 linha por passo e o que nasce embaixo
            // é a linha seguinte da forma.
            for (int i = 1; i <= shape.Height; i++)
            {
                RunGrowStep(vegetation, grid, settings);
                Assert.AreEqual(shape.Height - i, vegetation.GetSunkRows(plantX));
                Assert.AreEqual(Surface + i, TopOfPlant(grid, plantX), "A planta inteira sobe 1 linha por passo.");
                Assert.AreEqual(0, grid.MarkedCount);
            }
            Assert.AreEqual(shape.Cells.Length, CountPlant(grid, plantX), "Termina com a forma completa.");

            RunGrowStep(vegetation, grid, settings);
            Assert.AreEqual(shape.Cells.Length, CountPlant(grid, plantX), "Completa, para de crescer.");
        }

        [Test]
        public void Vegetation_WaitsForTheGeneratedArea_InsteadOfSproutingClippedAtItsEdge()
        {
            PixelGrid grid = MakeWorld(400);
            grid.SetActive(new RectInt(0, 0, 230, grid.Height));
            var vegetation = new VegetationSimulation(new VegetationSettings(), seed: 11, Layout, grid.Width);
            vegetation.Populate(grid);
            foreach (int x in vegetation.Plants)
            {
                vegetation.TryGetPlant(x, out _, out PlantShape shape, out _);
                Assert.Less(x + shape.MaxDx, 229, $"A planta em {x} passa da borda da área gerada.");
            }
            int before = vegetation.PlantCount;

            grid.ExpandActive(new RectInt(0, 0, 400, grid.Height));
            vegetation.Populate(grid);
            Assert.Greater(vegetation.PlantCount, before, "As plantas que não cabiam nascem quando a área cresce.");
            foreach (int x in vegetation.Plants)
            {
                vegetation.TryGetPlant(x, out _, out PlantShape shape, out _);
                Assert.AreEqual(shape.Cells.Length, PlantCells(grid, x, shape), $"A planta em {x} nasce inteira, sem corte.");
            }
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

        [Test]
        public void Vegetation_KeepsGrowing_BehindGrainAndBuildings()
        {
            PixelGrid grid = MakeWorld(400);
            var settings = new VegetationSettings();
            var vegetation = new VegetationSimulation(settings, seed: 11, Layout, grid.Width);
            vegetation.Populate(grid);
            int plantX = vegetation.Plants[0];
            vegetation.TryGetPlant(plantX, out _, out PlantShape shape, out _);
            Assert.GreaterOrEqual(shape.Height, 4, "O teste precisa de uma planta com algumas linhas.");

            // Colhe tudo menos a raiz: no passo seguinte ela reinicia enterrada.
            for (int y = Surface + 2; y < grid.Height; y++)
            for (int cx = plantX + shape.MinDx; cx <= plantX + shape.MaxDx; cx++)
            {
                if (CellMaterials.IsPlant(grid.Get(cx, y)))
                    grid.Set(cx, y, CellMaterial.Empty);
            }
            RunGrowStep(vegetation, grid, settings);
            Assert.AreEqual(shape.Height, vegetation.GetSunkRows(plantX));

            // Grão parado na raiz e uma construção 2 linhas acima, na coluna da raiz.
            int rootY = Surface + 1;
            grid.Set(plantX, rootY, CellMaterial.Spoil, 50);
            grid.Set(plantX, rootY + 2, CellMaterial.PackedSpoil, 60);

            for (int i = 0; i < shape.Height; i++)
                RunGrowStep(vegetation, grid, settings);

            Assert.AreEqual(0, vegetation.GetSunkRows(plantX), "Grão e construção não seguram o crescimento.");
            Assert.AreEqual(CellMaterial.Spoil, grid.Get(plantX, rootY), "O grão continua no lugar.");
            Assert.AreEqual(CellMaterial.PackedSpoil, grid.Get(plantX, rootY + 2), "A construção continua no lugar.");
            foreach (Vector2Int c in shape.Cells)
            {
                int cx = plantX + c.x, cy = rootY + c.y;
                bool covered = cx == plantX && (cy == rootY || cy == rootY + 2);
                CellMaterial seen = covered ? grid.GetBehind(cx, cy) : grid.Get(cx, cy);
                Assert.IsTrue(CellMaterials.IsPlant(seen), $"Célula ({c.x},{c.y}) deveria ter crescido{(covered ? " atrás do material" : "")}.");
            }

            // Tirado o material, a planta aparece no lugar.
            grid.Set(plantX, rootY, CellMaterial.Empty, 0);
            grid.Set(plantX, rootY + 2, CellMaterial.Empty, 0);
            Assert.AreEqual(shape.Cells.Length, PlantCells(grid, plantX, shape));
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
        public void Colony_HarvestsThePlantOnItsOwn_TopDown_AndTheQueenConsumesIt()
        {
            var (grid, vegetation, plantX, shape) = MakeGarden();
            PrepareCultivation(grid);
            // A colheita passa por vários ciclos dos canteiros: o teste é dela, não do envelhecimento nem do afogamento.
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 7) { DeathEnabled = false };
            colony.AttachVegetation(vegetation);
            int expected = HarvestableCells(shape);
            Assert.AreEqual(0, grid.MarkedCount, "O jogador não marca nada: a planta nasce sem marca.");

            var granular = new GranularSimulation();
            int lastCells = PlantCells(grid, plantX, shape);
            bool Done() => colony.HarvestedPixels == expected && BedsIn(grid, BedStage.Soil) == grid.BedCount &&
                           colony.GrownNutrients > 0 && colony.TotalNutrients == colony.GrownNutrients;
            for (int i = 0; i < 60 * 1200 && !Done(); i++)
            {
                int topRow = TopRow(grid, plantX, shape);
                int topCells = PlantCellsInRow(grid, plantX, shape, topRow);
                colony.Tick(Dt);
                granular.Step(grid); // o pixel solto pela formiga cai antes de ser colhido
                int cells = PlantCells(grid, plantX, shape);
                Assert.LessOrEqual(cells, lastCells, "A planta só diminui.");
                if (cells < lastCells)
                    Assert.Less(PlantCellsInRow(grid, plantX, shape, topRow), topCells, "Corta sempre pela linha de cima.");
                lastCells = cells;
            }

            Assert.AreEqual(shape.Cells.Length - expected, PlantCells(grid, plantX, shape), "Colhe tudo menos a raiz.");
            Assert.AreEqual(expected, colony.HarvestedPixels, "Cada pixel colhido é plantado num canteiro.");
            Assert.AreEqual(expected, colony.WaterCollected, "Cada canteiro plantado é regado com um bloco de água do mapa.");
            Assert.GreaterOrEqual(colony.GrownNutrients, expected, "Cada canteiro brota pelo menos um nutriente (até 3 no chão).");
            Assert.AreEqual(colony.GrownNutrients, colony.TotalNutrients, "A rainha consome todos os que brotaram.");
            Assert.AreEqual(0, grid.NutrientCount, "A sala esvazia quando a rainha consome tudo.");
            Assert.AreEqual(4, grid.BedCount, "A terra fofa fica: os canteiros voltam a esperar planta.");
        }

        [Test]
        public void Gardeners_AndDiggers_NeverTouchPlants()
        {
            var (grid, vegetation, plantX, shape) = MakeGarden();
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 7);
            colony.AttachVegetation(vegetation);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Gardener;
            colony.Ants[0].Role = AntRole.Digger;
            int before = PlantCells(grid, plantX, shape);

            for (int i = 0; i < 60 * 40; i++)
                colony.Tick(Dt);

            Assert.AreEqual(before, PlantCells(grid, plantX, shape), "Jardineira (cultivo de fungo) e escavadora não cortam planta.");
            Assert.AreEqual(0, colony.HarvestedPixels);
            Assert.AreEqual(-1, colony.GardenTarget);
        }

        [Test]
        public void Colony_PicksTheBiggestPlant_ThenTheNearestToTheEntrance()
        {
            PixelGrid grid = MakeWorld(400);
            var vegetation = new VegetationSimulation(new VegetationSettings(), seed: 11, Layout, grid.Width);
            vegetation.Populate(grid);
            PaintNutrientRoom(grid, 30, 35, 1, 3);
            AddBeds(grid, 31); // a planta-alvo precisa de um canteiro alcançável esperando planta

            int expected = -1, bestRows = 0, bestDistance = int.MaxValue, candidates = 0;
            foreach (int x in vegetation.Plants)
            {
                if (!vegetation.IsHarvestable(grid, x, out int rows))
                    continue;
                candidates++;
                int distance = Mathf.Abs(x - Layout.EntranceX);
                if (rows > bestRows || rows == bestRows && distance < bestDistance)
                {
                    expected = x;
                    bestRows = rows;
                    bestDistance = distance;
                }
            }
            Assert.GreaterOrEqual(candidates, 2, "A seed deveria ter mais de uma planta para comparar.");

            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 7);
            colony.AttachVegetation(vegetation);
            colony.Tick(Dt);
            Assert.AreEqual(expected, colony.GardenTarget, "Maior primeiro; no empate, a mais perto da entrada.");
        }

        [Test]
        public void Plant_OnlyBecomesATarget_AfterGrowingTheMinimum()
        {
            var (grid, vegetation, plantX, shape) = MakeGarden();
            Assert.IsTrue(vegetation.IsHarvestable(grid, plantX, out int rows));
            Assert.AreEqual(shape.Height, rows);

            // Recém-brotada (só a ponta para fora): ainda não vale, mesmo com pixels para cortar.
            vegetation.SetSunkRows(plantX, shape.Height - 1);
            Assert.IsFalse(vegetation.IsHarvestable(grid, plantX, out rows));
            Assert.AreEqual(1, rows);

            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 7);
            colony.AttachVegetation(vegetation);
            colony.Tick(Dt);
            Assert.AreEqual(-1, colony.GardenTarget, "Planta que não cresceu o mínimo não é escolhida.");

            vegetation.SetSunkRows(plantX, 0);
            Assert.IsTrue(vegetation.IsHarvestable(grid, plantX, out _), "Crescida, vale.");
        }

        [Test]
        public void Ants_CanClimbAndEnterPlants_LikeTunnels()
        {
            PixelGrid grid = MakeWorld();
            PlantBlade(grid, 6);
            int y = Surface + 4;
            Assert.IsTrue(AntNavigation.IsWalkable(grid, BladeX + 1, y), "Planta serve de apoio.");
            Assert.IsTrue(AntNavigation.IsWalkable(grid, BladeX, y), "Dentro da planta a formiga passa.");
            Assert.IsFalse(AntNavigation.IsWalkable(grid, BladeX + 1, y + 10), "Longe da planta é céu.");
        }

        [Test]
        public void DisconnectedPlantPieces_BecomeFallingPixels()
        {
            PixelGrid grid = MakeWorld();
            PlantBlade(grid, 4);
            for (int i = 0; i < 3; i++)
                grid.Set(BladeX + 6 + i, Surface + 9, CellMaterial.PlantGrass); // pedaço solto no ar
            int dropped = VegetationSimulation.DropDisconnected(grid, BladeX + 5, Surface + 8);
            Assert.AreEqual(3, dropped);
            Assert.AreEqual(4, BladeCells(grid), "A planta ligada à raiz não é tocada.");
            var granular = new GranularSimulation();
            for (int i = 0; i < 40; i++)
                granular.Step(grid);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(CellMaterial.Empty, grid.Get(BladeX + 6 + i, Surface + 9), "O pixel saiu do ar.");
                Assert.IsTrue(CellMaterials.IsPlantDebris(grid.Get(BladeX + 6 + i, Surface + 1)), "E pousou no chão.");
                Assert.IsTrue(grid.IsMarked(BladeX + 6 + i, Surface + 1), "Marcado para a colheita.");
            }
        }

        [Test]
        public void CuttingTheBase_DropsTheWholePlantAbove()
        {
            PixelGrid grid = MakeWorld();
            PlantBlade(grid, 6);
            int y = Surface + 2; // primeiro pixel acima da raiz
            grid.Set(BladeX, y, CellMaterial.Empty, 0);
            Assert.AreEqual(4, VegetationSimulation.DropDisconnected(grid, BladeX, y), "Os 4 pixels acima do corte ficam sem raiz.");
            Assert.AreEqual(1, BladeCells(grid), "Fica só a raiz.");
        }

        [Test]
        public void Colony_HarvestsAWidePlant()
        {
            // Touceira larga: 13 colunas, mais alta no meio (silhueta de sprite), com furos.
            var cells = new System.Collections.Generic.List<Vector2Int>();
            for (int dx = -6; dx <= 6; dx++)
            {
                int h = 18 - Mathf.Abs(dx) * 2;
                for (int i = 0; i < h; i++)
                {
                    if (i > 2 && dx % 2 != 0 && i % 4 == 0)
                        continue;
                    cells.Add(new Vector2Int(dx, i));
                }
            }
            var wide = new PlantShape(cells);
            var (grid, vegetation, plantX, shape) = MakeGarden(new[] { wide });
            AddBeds(grid, 24, 27, 30, 33, 36, 39, 42, 45);
            AddPuddle(grid);
            // A colheita leva até 30 min de jogo, mais que a vida das formigas (~10 min), e aqui não há berçário para repor
            // quem morre: o teste é da colheita, não do envelhecimento (ver AntColony.Age).
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 5) { DeathEnabled = false };
            colony.AttachVegetation(vegetation);
            int expected = HarvestableCells(shape);

            var granular = new GranularSimulation();
            for (int i = 0; i < 60 * 1800 && colony.HarvestedPixels < expected; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
            }
            // A touceira fica longe da sala de nutrientes: as comuns levam um pixel por vez, então a colheita é lenta.
            Assert.AreEqual(expected, colony.HarvestedPixels,
                            $"colhidos; plantas restantes {PlantCells(grid, plantX, shape)} (raiz: {shape.Cells.Length - expected})");
        }

        [Test]
        public void Colony_ReleasesThePixelFirst_AndOnlyThenCollectsItFromTheGround()
        {
            var (grid, vegetation, plantX, shape) = MakeGarden();
            PrepareCultivation(grid);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 7) { DeathEnabled = false };
            colony.AttachVegetation(vegetation);
            var granular = new GranularSimulation();
            int expected = HarvestableCells(shape);

            bool sawFallen = false;
            for (int i = 0; i < 60 * 1200 && colony.HarvestedPixels < expected; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
                for (int y = Surface + 1; y < grid.Height; y++)
                {
                    for (int x = plantX - 8; x <= plantX + 8; x++)
                    {
                        if (!CellMaterials.IsPlantDebris(grid.Get(x, y)))
                            continue;
                        sawFallen = true;
                        Assert.IsTrue(grid.IsMarked(x, y), "O pixel solto mantém a marca de colheita.");
                    }
                }
            }

            Assert.IsTrue(sawFallen, "A formiga solta o pixel antes de colhê-lo.");
            Assert.AreEqual(expected, colony.HarvestedPixels, "Todos os pixels soltos acabam colhidos.");
            Assert.AreEqual(shape.Cells.Length - expected, PlantCells(grid, plantX, shape), "Sobra só a raiz.");
        }

        [Test]
        public void Colony_StopsHarvesting_WhenNoBedWaitsForAPlant()
        {
            PixelGrid grid = MakeWorld(400);
            var vegetation = new VegetationSimulation(new VegetationSettings(), seed: 11, Layout, grid.Width);
            vegetation.Populate(grid);
            PaintNutrientRoom(grid, 30, 35, 1, 3);
            AddBeds(grid, 31); // um canteiro e nenhuma água: plantado, ele fica esperando
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3);
            colony.AttachVegetation(vegetation);

            var granularSim = new GranularSimulation();
            for (int i = 0; i < 60 * 90; i++)
            {
                colony.Tick(Dt);
                granularSim.Step(grid);
            }

            Assert.AreEqual(BedStage.Planted, grid.GetBedStage(31, 1), "O canteiro recebeu a planta e espera água.");
            Assert.AreEqual(1, colony.HarvestedPixels, "Sem canteiro esperando planta, ninguém colhe mais.");
            Assert.IsFalse(colony.CanHarvest);
            Assert.IsTrue(colony.HarvestPaused);
            Assert.AreEqual(0, colony.TotalNutrients, "Sem água, nada brota.");
        }

        [Test]
        public void Harvesters_GoHomeWhenNoBedWaits_AndOnlyReturnWhenMoreThanHalfWait()
        {
            var (grid, vegetation, plantX, _) = MakeGarden();
            int[] beds = { 24, 27, 30, 33 };
            AddBeds(grid, beds);
            // Todos já plantados e sem água no mapa: nenhum brota nem volta a esperar planta sozinho.
            foreach (int x in beds)
                grid.SetBed(x, 1, BedStage.Planted);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 5);
            colony.AttachVegetation(vegetation);
            // A cortadeira começa no pé da planta, onde esperaria se houvesse canteiro.
            Ant gardener = colony.Ants[0];
            gardener.Role = AntRole.Cutter;
            gardener.X = plantX;
            gardener.Y = Surface + 1;
            gardener.FromX = gardener.X;
            gardener.FromY = gardener.Y;

            var granular = new GranularSimulation();
            void Run(float seconds)
            {
                for (float t = 0f; t < seconds; t += Dt)
                {
                    colony.Tick(Dt);
                    granular.Step(grid);
                }
            }

            Run(90f);
            Assert.IsTrue(colony.HarvestPaused, "Nenhum canteiro espera planta: a colheita pausa.");
            Assert.LessOrEqual(gardener.Y, Surface, "A cortadora volta ao formigueiro em vez de esperar no pé da planta.");

            // Metade dos canteiros esperando ainda não basta (2 de 4): é preciso mais da metade.
            grid.SetBed(beds[0], 1, BedStage.Soil);
            grid.SetBed(beds[1], 1, BedStage.Soil);
            Run(3f);
            Assert.IsTrue(colony.HarvestPaused, "Com exatamente metade esperando ainda não volta a cortar.");

            grid.SetBed(beds[2], 1, BedStage.Soil);
            Run(1f);
            Assert.IsFalse(colony.HarvestPaused, "Mais da metade esperando planta: a colheita reabre.");

            int harvested = colony.HarvestedPixels;
            Run(240f);
            Assert.Greater(colony.HarvestedPixels, harvested, "Reaberta, a cortadora volta ao corte e planta pixels novos.");
        }

        /// <summary>
        /// Pedido atendido (pedido do desenvolvedor em 2026-10-19): um canteiro esperando planta chama uma formiga para
        /// cortar, não a colônia inteira em fila até a planta.
        /// </summary>
        [Test]
        public void OneBedWaiting_OnlyOneAntGoesToCut()
        {
            var (grid, vegetation, _, _) = MakeGarden();
            AddBeds(grid, 24);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 5) { DeathEnabled = false };
            colony.AttachVegetation(vegetation);
            for (int i = 0; i < 7; i++)
                colony.Spawn(AntRole.Worker);

            var granular = new GranularSimulation();
            int most = 0;
            for (int i = 0; i < 60 * 240 && grid.GetBedStage(24, 1) == BedStage.Soil; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
                int cutting = 0;
                foreach (Ant ant in colony.Ants)
                {
                    bool onPlant = ant.State == AntState.Digging && CellMaterials.IsPlant(grid.Get(ant.DigX, ant.DigY));
                    cutting += ant.Errand == AntErrand.Cut || onPlant ? 1 : 0;
                }
                most = Mathf.Max(most, cutting);
            }

            Assert.AreEqual(1, most, "Um canteiro esperando: uma formiga atende o pedido de planta.");
            Assert.AreNotEqual(BedStage.Soil, grid.GetBedStage(24, 1), "O canteiro ainda recebe a planta.");
        }

        [Test]
        public void Colony_StartsWithOnlyWorkers()
        {
            var colony = new AntColony(MakeWorld(), new ColonySettings(), Layout, seed: 1);
            Assert.AreEqual(colony.Ants.Count, colony.CountOf(AntRole.Worker), "Classes só existem depois de compradas.");
        }

        [Test]
        public void AntRoles_CostsAndWorkFollowTheDesign()
        {
            var colony = new AntColony(MakeWorld(), new ColonySettings(), Layout, seed: 1);
            Assert.AreEqual(18, colony.CostOf(AntRole.Worker), "Comum: 18 N, do design.");
            Assert.Greater(colony.CostOf(AntRole.Digger), colony.CostOf(AntRole.Carrier));
            Assert.Greater(colony.CostOf(AntRole.Carrier), colony.CostOf(AntRole.Worker));
            Assert.IsTrue(AntRoles.Harvests(AntRole.Worker) && AntRoles.Harvests(AntRole.Carrier) && AntRoles.Harvests(AntRole.Cutter));
            Assert.IsFalse(AntRoles.Harvests(AntRole.Gardener) || AntRoles.Harvests(AntRole.Digger), "Jardineira cuida do fungo; escavadora, das obras.");
            Assert.IsTrue(AntRoles.CutsFirst(AntRole.Cutter) && !AntRoles.CutsFirst(AntRole.Worker));
            Assert.AreEqual(0.5f, AntRoles.DigTimeFactor(AntRole.Digger), "Escavadora: 32 contra 16 células/s.");
            Assert.AreEqual(0.5f, AntRoles.CutTimeFactor(AntRole.Cutter), "Cortadeira: 1 s contra 2 s.");
            Assert.Greater(AntRoles.SpeedFactor(AntRole.Carrier), 1f);
            Assert.Less(AntRoles.SpeedFactor(AntRole.Gardener), 1f);
        }

        [Test]
        public void Snapshot_RoundTripsNutrientRoomPlantsAndEconomy()
        {
            PixelGrid grid = MakeWorld();
            PlantBlade(grid, 4);
            PaintNutrientRoom(grid, 30, 33, 1, 2);
            grid.SetNutrient(30, 2, true);
            AddBeds(grid, 32);
            grid.SetBed(32, 1, BedStage.Growing, 7.3f);
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
            Assert.IsTrue(grid2.HasNutrient(30, 2));
            Assert.AreEqual(1, grid2.BedCount);
            Assert.AreEqual(CellMaterial.Spoil, grid2.Get(32, 1));
            Assert.AreEqual(BedStage.Growing, grid2.GetBedStage(32, 1), "O canteiro volta na mesma etapa.");
            Assert.AreEqual(7.3f, grid2.GetBedGrow(32, 1), 1e-4f, "E com o tempo que faltava para brotar.");
            Assert.AreEqual(4, BladeCells(grid2));
            Assert.IsFalse(grid2.IsMarked(BladeX, Surface + 4), "Planta em pé não tem marca de colheita.");
            Assert.AreEqual(colony.Ants.Count, colony2.Ants.Count);
            for (int i = 0; i < colony.Ants.Count; i++)
                Assert.AreEqual(colony.Ants[i].Role, colony2.Ants[i].Role, "O papel de cada formiga volta com o save.");
            Assert.AreEqual(5, colony2.Nutrients);
            Assert.AreEqual(12, colony2.TotalNutrients);
            Assert.AreEqual(13, colony2.HarvestedPixels);
        }
    }
}
