using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    public sealed class AntFarmSimulationTests
    {
        /// <summary>Grade com piso de rocha em y = 0 e o resto vazio.</summary>
        static PixelGrid MakeFloorGrid(int width, int height)
        {
            var grid = new PixelGrid(width, height);
            for (int x = 0; x < width; x++)
                grid.Set(x, 0, CellMaterial.Rock);
            return grid;
        }

        [Test]
        public void Sand_FallsUntilItHitsTheFloor()
        {
            PixelGrid grid = MakeFloorGrid(5, 10);
            grid.Set(2, 8, CellMaterial.Sand);
            var sim = new GranularSimulation();

            for (int i = 0; i < 20; i++)
                sim.Step(grid);

            Assert.AreEqual(CellMaterial.Sand, grid.Get(2, 1));
            Assert.AreEqual(CellMaterial.Empty, grid.Get(2, 8));
        }

        [Test]
        public void Sand_SlidesDiagonallyOffAColumn()
        {
            PixelGrid grid = MakeFloorGrid(7, 6);
            grid.Set(3, 1, CellMaterial.Sand);
            grid.Set(3, 2, CellMaterial.Sand);
            var sim = new GranularSimulation();

            for (int i = 0; i < 5; i++)
                sim.Step(grid);

            int atFloor = 0;
            for (int x = 0; x < 7; x++)
                if (grid.Get(x, 1) == CellMaterial.Sand) atFloor++;
            Assert.AreEqual(2, atFloor, "A pilha de 2 grãos deve se espalhar em 2 grãos no chão.");
        }

        [Test]
        public void Dirt_DoesNotFall()
        {
            PixelGrid grid = MakeFloorGrid(5, 6);
            grid.Set(2, 4, CellMaterial.Dirt);
            new GranularSimulation().Step(grid);
            Assert.AreEqual(CellMaterial.Dirt, grid.Get(2, 4));
        }

        [Test]
        public void Mark_OnlyAcceptsDiggableCells_AndClearsWhenDug()
        {
            PixelGrid grid = MakeFloorGrid(5, 5);
            grid.Set(1, 1, CellMaterial.Dirt);
            Assert.IsFalse(grid.SetMarked(2, 0, true), "Rocha não pode ser marcada.");
            Assert.IsTrue(grid.SetMarked(1, 1, true));
            Assert.AreEqual(1, grid.MarkedCount);

            grid.Set(1, 1, CellMaterial.Empty);
            Assert.AreEqual(0, grid.MarkedCount);
        }

        [Test]
        public void FlowField_CountsStepsAlongTheFloor()
        {
            // Altura 6: a linha de cima encosta no "teto" fora da grade, mas y = 3 fica solta no ar.
            PixelGrid grid = MakeFloorGrid(10, 6);
            var field = new FlowField(grid.Width, grid.Height);
            field.Build(grid, new List<int> { grid.ToIndex(1, 1) });

            Assert.AreEqual(0, field.Get(1, 1));
            Assert.AreEqual(5, field.Get(6, 1));
            Assert.AreEqual(FlowField.Unreachable, field.Get(5, 3), "Célula solta no ar não é caminhável.");
        }

        [Test]
        public void Colony_DigsMarkedPixel_AndDeliversItToTheDump()
        {
            // Corredor plano: ninho à esquerda, um bloco de terra no meio, despejo à direita.
            PixelGrid grid = MakeFloorGrid(40, 8);
            for (int x = 0; x < grid.Width; x++)
                grid.SetSurfaceY(x, 0);
            grid.Set(15, 1, CellMaterial.Dirt);
            grid.SetMarked(15, 1, true);

            var layout = new TerrainLayout(2, new Vector2Int(2, 1), 30, 35);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 7);
            Assert.Greater(colony.Ants.Count, 0);

            var sim = new GranularSimulation();
            const float dt = 1f / 60f;
            for (int i = 0; i < 60 * 30 && colony.TotalDelivered == 0; i++)
            {
                sim.Step(grid);
                colony.Tick(dt);
            }

            Assert.AreEqual(1, colony.TotalDelivered, "O pixel marcado deveria ter sido levado ao despejo.");
            Assert.AreEqual(0, grid.MarkedCount);
            Assert.AreNotEqual(CellMaterial.Dirt, grid.Get(15, 1));

            int dumped = 0;
            for (int x = 30; x <= 36; x++)
            for (int y = 1; y < grid.Height; y++)
                if (grid.Get(x, y) == CellMaterial.Spoil) dumped++;
            Assert.AreEqual(1, dumped, "O pixel despejado vira Spoil na faixa de despejo.");
        }

        [TestCase(1)]
        [TestCase(42)]
        [TestCase(777)]
        [TestCase(-31337)]
        public void Generator_PilesStableSpoilMoundsOnBothSidesOfTheEntrance(int seed)
        {
            var grid = new PixelGrid(160, 120);
            TerrainLayout layout = TerrainGenerator.Generate(grid, new TerrainSettings(), seed);
            int entrance = layout.EntranceX;
            int surface = grid.GetSurfaceY(entrance);

            int left = 0, right = 0;
            for (int x = entrance - 20; x <= entrance + 20; x++)
            for (int y = grid.GetSurfaceY(x) + 1; y < grid.Height; y++)
            {
                if (grid.Get(x, y) != CellMaterial.Spoil)
                    continue;
                if (x < entrance) left++;
                else right++;
            }
            Assert.Greater(left, 0, "Falta a pilha de terra à esquerda da entrada.");
            Assert.Greater(right, 0, "Falta a pilha de terra à direita da entrada.");
            for (int x = entrance - 1; x <= entrance + 1; x++)
                Assert.AreEqual(CellMaterial.Empty, grid.Get(x, surface + 1), "A boca do poço precisa ficar livre.");

            // As pilhas são granulares: precisam nascer em repouso e não escorrer para dentro do poço.
            var before = new List<CellMaterial>();
            for (int x = entrance - 20; x <= entrance + 20; x++)
            for (int y = layout.Nest.y - 6; y <= surface + 10; y++)
                before.Add(grid.Get(x, y));
            var sim = new GranularSimulation();
            for (int i = 0; i < 300; i++)
                sim.Step(grid);
            int index = 0; // mesma ordem de varredura do instantâneo
            for (int x = entrance - 20; x <= entrance + 20; x++)
            for (int y = layout.Nest.y - 6; y <= surface + 10; y++)
                Assert.AreEqual(before[index++], grid.Get(x, y), $"A célula ({x}, {y}) mudou: a pilha não está estável.");
        }

        [Test]
        public void MapCodec_RoundTripsMaterialsAndSurface()
        {
            var original = new PixelGrid(64, 48);
            TerrainLayout layout = TerrainGenerator.Generate(original, new TerrainSettings(), 42);

            Color32[] pixels = TerrainMapCodec.Encode(original);
            PixelGrid decoded = TerrainMapCodec.Decode(pixels, original.Width, original.Height);

            for (int y = 0; y < original.Height; y++)
            for (int x = 0; x < original.Width; x++)
                Assert.AreEqual(original.Get(x, y), decoded.Get(x, y), $"Material diferente em ({x},{y}).");
            // O poço de entrada é túnel (não céu), então o solo da coluna da entrada se mantém.
            Assert.AreEqual(original.GetSurfaceY(layout.EntranceX), decoded.GetSurfaceY(layout.EntranceX));
        }

        [Test]
        public void MapCodec_AcceptsSlightlyOffPaletteColors()
        {
            Color32 dirt = CellMaterials.BaseColor(CellMaterial.Dirt);
            var pixels = new Color32[9];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32((byte)(dirt.r + 3), (byte)(dirt.g - 2), dirt.b, 255);
            pixels[8] = new Color32(10, 10, 10, 0); // transparente = céu

            PixelGrid grid = TerrainMapCodec.Decode(pixels, 3, 3);
            Assert.AreEqual(CellMaterial.Dirt, grid.Get(0, 0));
            Assert.AreEqual(CellMaterial.Empty, grid.Get(2, 2));
            Assert.AreEqual(2, grid.GetSurfaceY(0));
            Assert.AreEqual(1, grid.GetSurfaceY(2));
        }

        [Test]
        public void Ant_WithoutSupport_ClingsToNearbyWallInsteadOfFalling()
        {
            // Parede vertical em x = 5; as formigas nascem soltas no ar em (3,6), a duas células dela e longe do chão.
            PixelGrid grid = MakeFloorGrid(12, 12);
            for (int y = 1; y < 11; y++)
                grid.Set(5, y, CellMaterial.Rock);
            var layout = new TerrainLayout(3, new Vector2Int(3, 6), 8, 9);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 3);

            // 1º passo: (3,6) não tem apoio e (4,x) é caminhável → agarra na parede em vez de cair.
            colony.Tick(1f / 60f);
            foreach (Ant ant in colony.Ants)
            {
                Assert.AreEqual(4, ant.X, "A formiga deveria ter ido para a coluna encostada na parede.");
                Assert.Greater(ant.Y, 4, "A formiga não deveria ter despencado.");
                Assert.IsTrue(AntNavigation.IsWalkable(grid, ant.X, ant.Y));
            }
        }

        [Test]
        public void Ant_WithNothingInReach_Falls()
        {
            // Mapa largo e poucos passos: a queda (7 células) termina antes de qualquer formiga alcançar as bordas.
            PixelGrid grid = MakeFloorGrid(24, 12);
            var layout = new TerrainLayout(12, new Vector2Int(12, 8), 20, 21);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 3);

            for (int i = 0; i < 10; i++)
                colony.Tick(1f / 60f);
            foreach (Ant ant in colony.Ants)
                Assert.AreEqual(1, ant.Y, "Sem nada ao alcance, a formiga cai até o chão.");
        }

        [Test]
        public void TunnelBackground_IsWalkable_ButOpenSkyIsNot()
        {
            // Solo original na linha 8: abaixo dela o vazio é túnel (tem a parede de trás); acima é céu.
            PixelGrid grid = MakeFloorGrid(12, 14);
            for (int x = 0; x < grid.Width; x++)
                grid.SetSurfaceY(x, 8);

            Assert.IsTrue(AntNavigation.IsWalkable(grid, 6, 5), "Meio do túnel, sem nada encostado: anda no fundo.");
            Assert.IsFalse(AntNavigation.IsWalkable(grid, 6, 11), "Céu aberto, sem nada encostado: não anda.");

            // O caminho mais curto passa pelo meio do túnel, não contornando pelo chão.
            var field = new FlowField(grid.Width, grid.Height);
            field.Build(grid, new List<int> { grid.ToIndex(6, 1) });
            Assert.AreEqual(5, field.Get(6, 6));
        }

        [Test]
        public void Colony_BuyingAnAntSpendsStoredPixels()
        {
            PixelGrid grid = MakeFloorGrid(10, 4);
            var colony = new AntColony(grid, new ColonySettings(), new TerrainLayout(2, new Vector2Int(2, 1), 6, 8), 1);
            int before = colony.Ants.Count;
            Assert.IsFalse(colony.TryBuyAnt(), "Sem terra em estoque não compra.");
            Assert.AreEqual(before, colony.Ants.Count);
        }
    }
}
