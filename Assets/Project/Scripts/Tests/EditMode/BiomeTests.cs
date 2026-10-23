using System;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Primeiro bioma lateral, a Serapilheira (documento 44, REQ-070): faixa à direita do formigueiro coberta de folhas
    /// secas, o insumo que vai ao depósito de serapilheira e volta a cobrir o chão; gravetos e folhas secas do cenário só
    /// nela, gramado central limpo e sem pedras, cogumelos ou grãos.
    /// </summary>
    public sealed class BiomeTests
    {
        const float Dt = 0.1f;

        [Test]
        public void Biomes_AreDeterministic_InTerrariaLikeStrips_WithTheSideDrawnBySeed()
        {
            var settings = new BiomeSettings();
            const int entrance = 800, width = 1920;
            bool sawLeft = false, sawRight = false;
            for (int seed = 40; seed < 52; seed++)
            {
                SurfaceBiomes a = SurfaceBiomes.Generate(width, entrance, settings, seed);
                SurfaceBiomes b = SurfaceBiomes.Generate(width, entrance, settings, seed);
                for (int x = 0; x < width; x++)
                    Assert.AreEqual(a.At(x), b.At(x), $"Seed {seed}, coluna {x}: a mesma seed dá o mesmo bioma.");

                int side = a.LitterSide;
                sawRight |= side > 0;
                sawLeft |= side < 0;
                int half = settings.BlendWidth / 2 + 1;
                // Em coordenadas "para o lado da Serapilheira" (d > 0) e "para o lado do Canteiro" (d < 0).
                for (int x = 0; x < width; x++)
                {
                    int d = (x - entrance) * side;
                    SurfaceBiome expected;
                    if (Mathf.Abs(Mathf.Abs(d) - settings.LitterOffset) <= half ||
                        Mathf.Abs(Mathf.Abs(d) - settings.LitterOffset - settings.LitterWidth) <= half)
                        continue; // faixas de transição
                    if (d > settings.LitterOffset + settings.LitterWidth) expected = SurfaceBiome.FruitGrove;
                    else if (d > settings.LitterOffset) expected = SurfaceBiome.LeafLitter;
                    else if (d < -settings.FlowerOffset - settings.FlowerWidth) expected = SurfaceBiome.Desert;
                    else if (d < -settings.FlowerOffset) expected = SurfaceBiome.FlowerBed;
                    else expected = SurfaceBiome.Meadow;
                    Assert.AreEqual(expected, a.At(x), $"Seed {seed}, coluna {x} (d = {d}).");
                }
                // Entre um bioma lateral e a ponta não sobra gramado.
                for (int x = 0; x < width; x++)
                    if (Mathf.Abs(x - entrance) > Mathf.Max(settings.LitterOffset, settings.FlowerOffset) + half)
                        Assert.AreNotEqual(SurfaceBiome.Meadow, a.At(x), $"Seed {seed}, coluna {x}: só o centro é gramado.");
            }
            Assert.IsTrue(sawLeft && sawRight, "O lado da Serapilheira (e da Fruteira) é sorteado pela seed.");

            SurfaceBiomes mixedMap = SurfaceBiomes.Generate(width, entrance, settings, 42);
            int start = entrance + mixedMap.LitterSide * settings.LitterOffset, mixed = 0;
            for (int x = start - settings.BlendWidth / 2; x < start + settings.BlendWidth / 2; x++)
                if (mixedMap.At(x) != mixedMap.At(x - 1))
                    mixed++;
            Assert.Greater(mixed, 1, "A transição mistura as colunas, sem fronteira reta.");
            Assert.AreEqual(SurfaceBiome.Meadow, SurfaceBiomes.Uniform(10).At(5), "Sem gerador, tudo é gramado.");
        }

        [Test]
        public void Generator_CoversEachBiomeWithItsGround_AndPlantsFollowTheBiome()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.WorldWidth, settings.WorldHeight);
            var generator = new TerrainGenerator(settings, 11, grid.Width, grid.Height, settings.StartArea, settings.SkyHeadroom);
            TerrainLayout layout = generator.Begin(grid);
            generator.Expand(grid, new RectInt(0, settings.StartArea.yMin, grid.Width, grid.Height - settings.StartArea.yMin));
            Assert.AreSame(generator.Biomes, grid.Biomes, "A grade guarda o bioma de cada coluna.");

            int litterColumns = 0;
            for (int x = 1; x < grid.Width - 1; x++)
            {
                if (generator.GetLakeLevel(x) >= 0)
                    continue;
                int top = grid.GetSurfaceY(x);
                SurfaceBiome biome = grid.Biomes.At(x);
                CellMaterial expected = biome == SurfaceBiome.LeafLitter ? CellMaterial.Litter
                    : biome == SurfaceBiome.FlowerBed || biome == SurfaceBiome.FruitGrove ? CellMaterial.Compost
                    : biome == SurfaceBiome.Desert ? CellMaterial.Sand : CellMaterial.Grass;
                if (Mathf.Abs(x - layout.EntranceX) <= 1)
                    continue; // poço do ninho
                if (grid.Get(x, top) == CellMaterial.Spoil || grid.Get(x, top + 1) == CellMaterial.Spoil)
                    continue; // monte de terra da entrada
                Assert.AreEqual(expected, grid.Get(x, top), $"Coluna {x}: a cobertura do chão é a do bioma.");
                Assert.AreEqual(biome == SurfaceBiome.Desert ? CellMaterial.Sand : CellMaterial.Dirt, grid.Get(x, top - 2),
                                $"Coluna {x}: terra logo abaixo da cobertura (areia no Areal).");
                if (expected == CellMaterial.Litter)
                    litterColumns++;
            }
            Assert.Greater(litterColumns, 200, "A Serapilheira tem centenas de colunas.");

            var vegetation = new VegetationSimulation(new VegetationSettings(), 11, layout, grid.Width);
            vegetation.Populate(grid);
            int meadowPlants = 0;
            for (int x = 1; x < grid.Width - 1; x++)
            {
                CellMaterial above = grid.Get(x, grid.GetSurfaceY(x) + 1);
                if (!CellMaterials.IsPlant(above))
                    continue;
                if (grid.Biomes.At(x) == SurfaceBiome.Meadow)
                    meadowPlants++;
            }
            Assert.Greater(meadowPlants, 0, "O gramado continua com plantas colhíveis.");
            int flowers = 0;
            foreach (int root in vegetation.Plants)
            {
                Assert.IsTrue(vegetation.TryGetPlant(root, out PlantKind kind, out _, out _));
                SurfaceBiome biome = grid.Biomes.At(root);
                Assert.AreNotEqual(SurfaceBiome.LeafLitter, biome, $"Planta com raiz em {root}: a Serapilheira não tem plantas.");
                Assert.AreNotEqual(SurfaceBiome.Desert, biome, $"Planta com raiz em {root}: o Areal não tem plantas.");
                Assert.AreEqual(biome == SurfaceBiome.FlowerBed, kind == PlantKind.Flower,
                                $"Planta com raiz em {root}: flor só no Canteiro de flores, e nele só flor.");
                if (biome == SurfaceBiome.FruitGrove)
                    Assert.AreEqual(PlantKind.Leafy, kind, $"Planta com raiz em {root}: a Fruteira só tem mudas folhosas.");
                if (kind == PlantKind.Flower)
                    flowers++;
            }
            Assert.Greater(flowers, 0, "O Canteiro de flores tem flores.");
        }

        [Test]
        public void DugLitter_GoesToTheLitterDepot_NotToTheBuildingDepot()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Blocks, 30);
            PaintDepot(grid, StorageKind.Litter, 20);
            PlaceMarkedLitter(grid);
            // Salas já rebocadas: a construção vem antes das automatizações (decisão de 2026-10-22).
            RoomLiningTests.LineAllRooms(grid);
            AntColony colony = MakeColony(grid);

            Assert.IsTrue(TickUntil(colony, () => colony.DepotStock(StorageKind.Litter) >= 4, 120f),
                          "A serapilheira cavada vai ao depósito de serapilheira.");
            Assert.AreEqual(0, colony.DepotStock(StorageKind.Blocks), "O depósito de construção não recebe serapilheira.");
        }

        [Test]
        public void WithoutTheLitterDepot_LitterIsHandledAsSoil()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Blocks, 30);
            PlaceMarkedLitter(grid);
            // Salas já rebocadas: a construção vem antes das automatizações (decisão de 2026-10-22).
            RoomLiningTests.LineAllRooms(grid);
            AntColony colony = MakeColony(grid);

            Assert.IsTrue(TickUntil(colony, () => colony.DepotStock(StorageKind.Blocks) >= 4, 120f),
                          "Sem o depósito dela, a serapilheira vira terra fofa, como a grama cavada.");
        }

        [Test]
        public void LitterFall_RestoresTheCover_OnlyOverGround()
        {
            var grid = new PixelGrid(40, 40);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.SetSurfaceY(x, 20);
                for (int y = 1; y <= 18; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
                grid.Set(x, 19, CellMaterial.Litter);
                grid.Set(x, 20, CellMaterial.Litter);
            }

            // Coluna 5: as duas células da cobertura cavadas. Volta de baixo para cima.
            grid.Set(5, 19, CellMaterial.Empty);
            grid.Set(5, 20, CellMaterial.Empty);
            Assert.IsTrue(LitterFall.TryRestoreAt(grid, 5, 2));
            Assert.AreEqual(CellMaterial.Litter, grid.Get(5, 19), "A célula de baixo volta primeiro.");
            Assert.AreEqual(CellMaterial.Empty, grid.Get(5, 20));
            Assert.IsTrue(LitterFall.TryRestoreAt(grid, 5, 2));
            Assert.AreEqual(CellMaterial.Litter, grid.Get(5, 20));
            Assert.IsFalse(LitterFall.TryRestoreAt(grid, 5, 2), "Coberta até o solo original: nada a repor.");

            // Coluna 10: poço aberto até o fundo da terra. Não tapa a boca do túnel.
            for (int y = 10; y <= 20; y++)
                grid.Set(10, y, CellMaterial.Empty);
            Assert.IsFalse(LitterFall.TryRestoreAt(grid, 10, 2), "Sem terra embaixo, a folha não cobre o poço.");

            // Coluna 15: formiga na célula que voltaria.
            grid.Set(15, 20, CellMaterial.Empty);
            Assert.IsFalse(LitterFall.TryRestoreAt(grid, 15, 2, (x, y) => x == 15 && y == 20), "Não cai folha em cima de formiga.");
            Assert.IsTrue(LitterFall.TryRestoreAt(grid, 15, 2));

            // Coluna 25: terra despejada por cima segura a cobertura.
            grid.Set(25, 20, CellMaterial.Empty);
            grid.Set(25, 21, CellMaterial.Spoil);
            Assert.IsFalse(LitterFall.TryRestoreAt(grid, 25, 2));
        }

        [Test]
        public void LitterFall_OnlyRestoresInsideTheLitterBiome()
        {
            var terrain = new TerrainSettings();
            var grid = new PixelGrid(terrain.WorldWidth, terrain.WorldHeight);
            var generator = new TerrainGenerator(terrain, 3, grid.Width, grid.Height, terrain.StartArea, terrain.SkyHeadroom);
            generator.Begin(grid);
            generator.Expand(grid, new RectInt(0, terrain.StartArea.yMin, grid.Width, grid.Height - terrain.StartArea.yMin));
            for (int x = 1; x < grid.Width - 1; x++)
            {
                int top = grid.GetSurfaceY(x);
                if (grid.Get(x, top) == CellMaterial.Litter || grid.Get(x, top) == CellMaterial.Grass)
                    grid.Set(x, top, CellMaterial.Empty);
            }

            var fall = new LitterFall(new LitterSettings(), 3);
            fall.Step(grid, 4f * 200f);
            Assert.Greater(fall.Restored, 100, "As folhas voltam a cobrir o chão com o tempo.");
            for (int x = 1; x < grid.Width - 1; x++)
                if (grid.Get(x, grid.GetSurfaceY(x)) == CellMaterial.Litter)
                    Assert.AreEqual(SurfaceBiome.LeafLitter, grid.Biomes.At(x), $"Coluna {x}: folha só volta na Serapilheira.");
        }

        [Test]
        public void Scenery_HasNoPebblesMushroomsOrGrains_AndLitterElementsOnlyInTheLitter()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout = TerrainGenerator.Generate(grid, settings, 21);
            grid.Biomes = SurfaceBiomes.Band(grid.Width, SurfaceBiome.LeafLitter, 380, grid.Width);
            var marker = new Color32(255, 0, 1, 255);
            var forbidden = new Color32(1, 0, 255, 255);
            var art = new SceneryStampLibrary();
            art.Set(SceneryElement.Twig, new[] { Solid(marker) });
            art.Set(SceneryElement.DryLeaf, new[] { Solid(marker) });
            foreach (SceneryElement element in new[] { SceneryElement.Pebble, SceneryElement.Mushroom, SceneryElement.Grain })
                art.Set(element, new[] { Solid(forbidden) });

            SceneryLayer near = SurfaceSceneryPainter.Paint(grid, layout, new SurfaceScenerySettings(), 21, Color.white, Color.white, art: art).Near;
            int ppc = near.PixelsPerCell, painted = 0;
            for (int px = 0; px < near.Width; px++)
            for (int py = 0; py < near.Height; py++)
            {
                Color32 c = near.Get(px, py);
                Assert.IsFalse(c.Equals(forbidden), "Pedras, cogumelos e grãos saíram da geração (documento 44).");
                if (!c.Equals(marker))
                    continue;
                painted++;
                Assert.AreEqual(SurfaceBiome.LeafLitter, grid.Biomes.At(px / ppc), $"Coluna {px / ppc}: graveto e folha seca só na Serapilheira.");
            }
            Assert.Greater(painted, 0, "A Serapilheira tem gravetos e folhas secas.");
        }

        [Test]
        public void WholeFlowers_DropNectarBesideThem_UpToTheLimit()
        {
            var terrain = new TerrainSettings();
            var grid = new PixelGrid(terrain.WorldWidth, terrain.WorldHeight);
            var generator = new TerrainGenerator(terrain, 11, grid.Width, grid.Height, terrain.StartArea, terrain.SkyHeadroom);
            TerrainLayout layout = generator.Begin(grid);
            generator.Expand(grid, new RectInt(0, terrain.StartArea.yMin, grid.Width, grid.Height - terrain.StartArea.yMin));
            var vegetation = new VegetationSimulation(new VegetationSettings(), 11, layout, grid.Width);
            vegetation.Populate(grid);
            var drip = new NectarDrip(new NectarSettings(), 11);

            int flower = -1;
            foreach (int root in vegetation.Plants)
                if (vegetation.TryGetPlant(root, out PlantKind kind, out _, out _) && kind == PlantKind.Flower)
                {
                    flower = root;
                    break;
                }
            Assert.GreaterOrEqual(flower, 0, "O Canteiro de flores tem flores.");
            Assert.IsTrue(drip.TryDrop(grid, vegetation, flower), "A flor inteira solta uma gota.");
            Assert.IsTrue(drip.TryDrop(grid, vegetation, flower));
            Assert.IsFalse(drip.TryDrop(grid, vegetation, flower), "Com 2 gotas em volta, a flor espera alguém levar uma.");
            int drops = 0;
            for (int x = flower - 8; x <= flower + 8; x++)
            for (int y = grid.GetSurfaceY(x) + 1; y <= grid.GetSurfaceY(x) + 4; y++)
                if (grid.GetStored(x, y) == StoredItems.Nectar)
                {
                    drops++;
                    Assert.IsTrue(grid.IsSolid(x, y - 1), "A gota pousa no chão.");
                }
            Assert.AreEqual(2, drops);

            int meadowPlant = -1;
            foreach (int root in vegetation.Plants)
                if (grid.Biomes.At(root) == SurfaceBiome.Meadow)
                    meadowPlant = root;
            Assert.IsFalse(drip.TryDrop(grid, vegetation, meadowPlant), "Grama e trevo não dão néctar.");
        }

        [Test]
        public void Nectar_GoesToTheNectarDepot_AndStaysOnTheGroundWithoutIt()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Blocks, 30);
            Assert.IsTrue(grid.SetStored(70, 1, StoredItems.Nectar), "Gota no chão do corredor.");
            RoomLiningTests.LineAllRooms(grid);
            AntColony colony = MakeColony(grid);

            for (float t = 0f; t < 30f; t += Dt)
                colony.Tick(Dt);
            Assert.AreEqual(StoredItems.Nectar, grid.GetStored(70, 1), "Sem o depósito de néctar, a gota fica onde caiu.");
            Assert.AreEqual(0, colony.DepotStock(StorageKind.Blocks), "O néctar não é terra.");

            PaintDepot(grid, StorageKind.Nectar, 20);
            Assert.IsTrue(TickUntil(colony, () => colony.DepotStock(StorageKind.Nectar) == 1, 60f),
                          "Com o depósito de néctar, as formigas levam a gota para lá.");
            Assert.AreEqual(StoredItems.None, grid.GetStored(70, 1));
        }

        // ---------------------------------------------------------------- mundo de teste

        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);

        /// <summary>Solo até y = 20, corredor de x 10 a 100 (y 1 a 3), poço em x 50..51 e sala da rainha em x 88..98.</summary>
        static PixelGrid MakeWorld()
        {
            var grid = new PixelGrid(120, 40);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, 20);
                for (int y = 1; y <= 20; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int x = 10; x <= 100; x++)
            for (int y = 1; y <= 3; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 50; x <= 51; x++)
            for (int y = 4; y <= 20; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 88; x <= 98; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true));
            return grid;
        }

        /// <summary>Quatro células de serapilheira marcadas no teto do corredor (a terra que a formiga alcança).</summary>
        static void PlaceMarkedLitter(PixelGrid grid)
        {
            for (int x = 60; x <= 63; x++)
            {
                grid.Set(x, 4, CellMaterial.Litter);
                Assert.IsTrue(grid.SetMarked(x, 4, true));
            }
        }

        static void PaintDepot(PixelGrid grid, StorageKind kind, int minX)
        {
            for (int x = minX; x <= minX + 6; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetStorageRoom(x, y, kind, true));
        }

        static AntColony MakeColony(PixelGrid grid)
        {
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 5) { RaidsEnabled = false };
            colony.RestoreQueen(95, 2, 1);
            return colony;
        }

        static bool TickUntil(AntColony colony, Func<bool> done, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                colony.Tick(Dt);
                if (done())
                    return true;
            }
            return false;
        }

        static SceneryStamp Solid(Color32 color)
        {
            var pixels = new Color32[16 * 16];
            Array.Fill(pixels, color);
            return new SceneryStamp(16, 16, pixels, 8, 0);
        }
    }
}
