using System.Collections.Generic;
using System.Linq;
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

        [Test]
        public void Colony_SplitsDumpedDirtEvenlyBetweenBothSidesOfTheEntrance()
        {
            // Entrada no meio, faixa configurada à direita; o espelho fica à esquerda. A terra vem toda do lado esquerdo,
            // então sem o equilíbrio o lado mais perto (ou o configurado) ficaria com tudo.
            PixelGrid grid = MakeFloorGrid(90, 16);
            for (int x = 0; x < grid.Width; x++)
                grid.SetSurfaceY(x, 0);
            const int pixels = 12;
            for (int x = 4; x < 10; x++)
            for (int y = 1; y <= 2; y++)
            {
                grid.Set(x, y, CellMaterial.Dirt);
                grid.SetMarked(x, y, true);
            }

            var layout = new TerrainLayout(45, new Vector2Int(45, 1), 55, 60);
            Assert.IsTrue(layout.TryGetMirrorDump(grid.Width, out int mirrorMin, out int mirrorMax));
            Assert.AreEqual(30, mirrorMin);
            Assert.AreEqual(35, mirrorMax);

            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 3);
            // Chão de pedra sem buraco: nenhuma abertura, então valem a faixa configurada e o espelho.
            Assert.AreEqual(0, colony.OpeningCount);
            Assert.AreEqual(2, colony.DumpStrips.Count);
            var sim = new GranularSimulation();
            const float dt = 1f / 60f;
            for (int i = 0; i < 60 * 240 && colony.TotalDelivered < pixels; i++)
            {
                sim.Step(grid);
                colony.Tick(dt);
            }
            Assert.AreEqual(pixels, colony.TotalDelivered, "Todos os pixels marcados deveriam ter sido despejados.");

            int left = 0, right = 0;
            for (int x = 0; x < grid.Width; x++)
            for (int y = 1; y < grid.Height; y++)
            {
                if (grid.Get(x, y) != CellMaterial.Spoil)
                    continue;
                if (x < layout.EntranceX) left++;
                else right++;
            }
            Assert.AreEqual(pixels, left + right);
            Assert.LessOrEqual(Mathf.Abs(left - right), 2, $"Monte desequilibrado: {left} à esquerda, {right} à direita.");
        }

        [Test]
        public void Colony_PilesDirtOnBothSidesOfTheNearestSurfaceOpening()
        {
            // Solo maciço com duas aberturas ligadas por um túnel: a entrada (x 39..41) e outra cavada depois (x 100..102).
            // A terra marcada fica ao lado da segunda, então ela deve ganhar o monte, dividido entre os dois lados.
            const int surface = 20;
            PixelGrid grid = MakeFloorGrid(140, 40);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.SetSurfaceY(x, surface);
                for (int y = 1; y <= surface; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int y = 5; y <= surface; y++)
            for (int i = -1; i <= 1; i++)
            {
                grid.Set(40 + i, y, CellMaterial.Empty);
                grid.Set(101 + i, y, CellMaterial.Empty);
            }
            for (int x = 39; x <= 102; x++)
            for (int y = 5; y <= 7; y++)
                grid.Set(x, y, CellMaterial.Empty);

            const int pixels = 8;
            for (int x = 103; x < 107; x++)
            for (int y = 5; y <= 6; y++)
                grid.SetMarked(x, y, true);

            // Faixa configurada: 16..30 células à direita da entrada.
            var layout = new TerrainLayout(40, new Vector2Int(40, 5), 56, 70);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 5);
            Assert.AreEqual(2, colony.OpeningCount);
            CollectionAssert.AreEqual(
                new[] { new Vector2Int(10, 24), new Vector2Int(56, 70), new Vector2Int(71, 85), new Vector2Int(117, 131) },
                colony.DumpStrips.OrderBy(s => s.x).ToArray(),
                "Cada abertura ganha uma faixa de cada lado, com a distância da faixa configurada.");

            var sim = new GranularSimulation();
            const float dt = 1f / 60f;
            for (int i = 0; i < 60 * 240 && colony.TotalDelivered < pixels; i++)
            {
                sim.Step(grid);
                colony.Tick(dt);
            }
            Assert.AreEqual(pixels, colony.TotalDelivered);

            int left = 0, right = 0, elsewhere = 0;
            for (int x = 0; x < grid.Width; x++)
            for (int y = surface + 1; y < grid.Height; y++)
            {
                if (grid.Get(x, y) != CellMaterial.Spoil)
                    continue;
                if (x >= 68 && x < 100) left++;
                else if (x > 102) right++;
                else elsewhere++;
            }
            Assert.AreEqual(0, elsewhere, "A terra deveria ir para a abertura mais perto, não para a entrada.");
            Assert.AreEqual(pixels, left + right);
            Assert.LessOrEqual(Mathf.Abs(left - right), 2, $"Monte desequilibrado: {left} à esquerda, {right} à direita.");
        }

        [Test]
        public void Layout_HasNoMirrorDump_WhenTheStripCoversTheEntranceOrLeavesTheMap()
        {
            Assert.IsFalse(new TerrainLayout(20, new Vector2Int(20, 1), 15, 25).TryGetMirrorDump(100, out _, out _));
            Assert.IsFalse(new TerrainLayout(2, new Vector2Int(2, 1), 30, 35).TryGetMirrorDump(100, out _, out _));
            Assert.IsTrue(new TerrainLayout(10, new Vector2Int(10, 1), 15, 30).TryGetMirrorDump(100, out int min, out int max));
            Assert.AreEqual(0, min, "O espelho é cortado na borda do mapa.");
            Assert.AreEqual(5, max);
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
        public void TopOfWorld_IsNotACeiling()
        {
            // Fora da grade conta como rocha; acima do topo, porém, é céu aberto: a última linha não vira teto.
            PixelGrid grid = MakeFloorGrid(12, 14);
            for (int x = 0; x < grid.Width; x++)
                grid.SetSurfaceY(x, 8);

            Assert.IsFalse(AntNavigation.IsWalkable(grid, 6, grid.Height - 1), "Topo do mundo sem nada encostado: não anda.");
            Assert.IsTrue(AntNavigation.IsWalkable(grid, 0, grid.Height - 2), "A borda lateral do mundo continua sendo parede.");
        }

        [Test]
        public void BuildMarks_OnlyApplyToEmptyUndergroundCells_AndClearWhenFilled()
        {
            PixelGrid grid = MakeFloorGrid(10, 10);
            for (int x = 0; x < grid.Width; x++)
                grid.SetSurfaceY(x, 5);
            grid.Set(3, 2, CellMaterial.Dirt);

            Assert.IsFalse(grid.SetBuildMarked(3, 2, true), "Célula sólida não recebe marca de construção.");
            Assert.IsFalse(grid.SetBuildMarked(4, 7, true), "O céu acima do solo original não se constrói.");
            Assert.IsTrue(grid.SetBuildMarked(4, 2, true));
            Assert.AreEqual(1, grid.BuildMarkedCount);
            Assert.IsTrue(grid.IsBuildMarked(4, 2));

            int version = grid.MarkVersion;
            grid.Set(4, 2, CellMaterial.PackedSpoil);
            Assert.AreEqual(0, grid.BuildMarkedCount, "Preencher a célula apaga a marca.");
            Assert.AreNotEqual(version, grid.MarkVersion);
        }

        [Test]
        public void Colony_FillsBuildMarksFromTheDeepestPointOutward_UsingDirtFromOutside()
        {
            // Solo maciço de terra com uma entrada (x 39..41) e um túnel horizontal até x = 70. Lá fora, sobre o solo,
            // há uma pilha de terra solta. As marcas de construção ocupam o fundo do túnel (x 60..65).
            const int surface = 20;
            PixelGrid grid = MakeFloorGrid(80, 30);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.SetSurfaceY(x, surface);
                for (int y = 1; y <= surface; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int y = 5; y <= surface; y++)
            for (int x = 39; x <= 41; x++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 39; x <= 70; x++)
            for (int y = 5; y <= 7; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 48; x <= 57; x++)
                grid.Set(x, surface + 1, CellMaterial.Spoil);
            for (int x = 50; x <= 55; x++)
                grid.Set(x, surface + 2, CellMaterial.Spoil);

            for (int x = 60; x <= 65; x++)
            for (int y = 5; y <= 6; y++)
                Assert.IsTrue(grid.SetBuildMarked(x, y, true));
            int toFill = grid.BuildMarkedCount;

            var layout = new TerrainLayout(40, new Vector2Int(40, 6), 60, 70);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 11);
            var sim = new GranularSimulation();
            const float dt = 1f / 60f;

            // Tick em que cada coluna recebeu o primeiro e o último pixel.
            var first = new Dictionary<int, int>();
            var last = new Dictionary<int, int>();
            var seen = new HashSet<Vector2Int>();
            int peakBuilders = 0;
            for (int tick = 0; tick < 60 * 180 && grid.BuildMarkedCount > 0; tick++)
            {
                sim.Step(grid);
                colony.Tick(dt);
                int builders = colony.Ants.Count(a => a.State == AntState.Gathering || a.State == AntState.Hauling || a.State == AntState.Placing);
                peakBuilders = Mathf.Max(peakBuilders, builders);
                for (int x = 60; x <= 65; x++)
                for (int y = 5; y <= 6; y++)
                {
                    if (grid.IsSolid(x, y) && seen.Add(new Vector2Int(x, y)))
                    {
                        if (!first.ContainsKey(x)) first[x] = tick;
                        last[x] = tick;
                    }
                }
            }

            Assert.AreEqual(0, grid.BuildMarkedCount, "Todas as marcas deveriam ter sido preenchidas.");
            Assert.AreEqual(toFill, seen.Count);
            // Pontos até 1 de distância da saída em diferença podem andar juntos; a partir de 2 colunas, o mais fundo vem antes.
            for (int x = 60; x < 64; x++)
                Assert.LessOrEqual(last[x + 2], first[x], $"A coluna {x + 2} (mais fundo) deveria ficar pronta antes de a {x} começar.");
            Assert.GreaterOrEqual(peakBuilders, 3, "Várias formigas devem construir ao mesmo tempo, sem voltar ociosas.");
            Assert.AreEqual(CellMaterial.PackedSpoil, grid.Get(65, 5), "A terra posta fica encaixada, sem escorrer.");
            Assert.AreEqual(0, colony.TotalDelivered, "Construir não conta como entrega de terra.");
        }

        [Test]
        public void Colony_NeverFillsACellThatWouldSealOffAnInnerBuildMark()
        {
            // Túnel de 1 célula de altura: preencher qualquer ponto fecharia a passagem até as marcas mais fundas.
            const int surface = 20;
            PixelGrid grid = MakeFloorGrid(80, 30);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.SetSurfaceY(x, surface);
                for (int y = 1; y <= surface; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int y = 5; y <= surface; y++)
            for (int x = 39; x <= 41; x++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 39; x <= 70; x++)
                grid.Set(x, 5, CellMaterial.Empty);
            for (int x = 48; x <= 57; x++)
                grid.Set(x, surface + 1, CellMaterial.Spoil);
            for (int x = 50; x <= 55; x++)
                grid.Set(x, surface + 2, CellMaterial.Spoil);
            for (int x = 50; x <= 55; x++)
                Assert.IsTrue(grid.SetBuildMarked(x, 5, true));

            var colony = new AntColony(grid, new ColonySettings(), new TerrainLayout(40, new Vector2Int(40, 6), 60, 70), seed: 4);
            var sim = new GranularSimulation();
            for (int tick = 0; tick < 60 * 120 && grid.BuildMarkedCount > 0; tick++)
            {
                sim.Step(grid);
                colony.Tick(1f / 60f);
                // Sempre do fundo para a saída: nenhuma célula é preenchida com uma mais funda ainda vazia.
                for (int x = 50; x < 55; x++)
                {
                    if (grid.IsSolid(x, 5))
                        Assert.IsTrue(grid.IsSolid(x + 1, 5), $"Preencheu a célula {x} antes da {x + 1}, selando a passagem.");
                }
            }
            Assert.AreEqual(0, grid.BuildMarkedCount, "O túnel inteiro deveria ter sido preenchido, de dentro para fora.");
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
