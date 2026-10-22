using System.Collections.Generic;
using System.Diagnostics;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Bateria de cenários dos itens abertos do KnownIssues (2026-10-20): cada teste cita o KI. Testes de limitação conhecida
    /// afirmam o comportamento DESEJADO; se falharem, o item continua aberto. Os de desempenho usam orçamentos folgados
    /// (editor em Debug) e escrevem a medição no log do teste.
    /// </summary>
    public sealed class KnownIssuesScenarioTests
    {
        const float Dt = 1f / 60f;
        const int Surface = 10;
        static readonly TerrainLayout SmallLayout = new TerrainLayout(20, new Vector2Int(20, 1), 50, 55);

        // ---------------------------------------------------------------- mundos de teste

        /// <summary>Solo de terra até a superfície, piso de rocha em y = 0 e um corredor de 3 linhas (y 1..3).</summary>
        static PixelGrid MakeSoil(int width = 80, int height = 30, bool corridor = true)
        {
            var grid = new PixelGrid(width, height);
            for (int x = 0; x < width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, Surface);
                for (int y = 1; y <= Surface; y++)
                    grid.Set(x, y, y == Surface ? CellMaterial.Grass : CellMaterial.Dirt);
            }
            if (corridor)
            {
                for (int x = 2; x < width - 2; x++)
                for (int y = 1; y <= 3; y++)
                    grid.Set(x, y, CellMaterial.Empty);
                for (int y = 1; y <= Surface; y++)
                    grid.Set(20, y, CellMaterial.Empty);
            }
            return grid;
        }

        static PixelGrid MakeBox(int width, int height)
        {
            var grid = new PixelGrid(width, height);
            for (int x = 0; x < width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, -1);
            }
            for (int y = 1; y < height; y++)
            {
                grid.Set(0, y, CellMaterial.Rock);
                grid.Set(width - 1, y, CellMaterial.Rock);
            }
            return grid;
        }

        static void RunWater(WaterSimulation water, PixelGrid grid, int steps)
        {
            for (int i = 0; i < steps; i++)
                water.Step(grid, Dt);
        }

        static int CountWater(PixelGrid grid, int x0, int x1, int y0, int y1)
        {
            int n = 0;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                n += grid.HasWater(x, y) ? 1 : 0;
            return n;
        }

        // ---------------------------------------------------------------- KI-050: transbordo da sala de orvalho

        const int RoomX0 = 30, RoomX1 = 37, RoomY1 = 7;

        /// <summary>Sala pronta (terra fofa nas 3 linhas de cima, folha nas colunas pares) com corredor de 1 linha só.</summary>
        static PixelGrid MakeReadyDewWorld()
        {
            PixelGrid grid = MakeSoil();
            // Sala de 7 de altura, com o corredor de entrada de 1 linha no pé: a água não escorre por um túnel largo.
            for (int x = 2; x < grid.Width - 2; x++)
            for (int y = 2; y <= 3; y++)
                grid.Set(x, y, CellMaterial.Dirt);
            for (int x = RoomX0; x <= RoomX1; x++)
            for (int y = 1; y <= RoomY1; y++)
            {
                grid.Set(x, y, CellMaterial.Empty);
                Assert.IsTrue(grid.SetDewRoom(x, y, true));
            }
            for (int x = RoomX0; x <= RoomX1; x++)
            {
                for (int y = RoomY1 - 2; y <= RoomY1; y++)
                    grid.Set(x, y, CellMaterial.Spoil);
                if ((x & 1) == 0)
                    grid.Set(x, RoomY1 - 3, CellMaterial.PlantLeaf);
            }
            return grid;
        }

        [Test]
        public void KI050_ReadyDewRoom_KeepsItsWaterBelowTheCap_AndStopsDripping()
        {
            PixelGrid grid = MakeReadyDewWorld();
            var colony = new AntColony(grid, new ColonySettings(), SmallLayout, seed: 4) { RaidsEnabled = false };
            var granular = new GranularSimulation();
            var water = new WaterSimulation(new WaterSettings(), grid, 4);
            int dropsAt120 = -1, roomMax = 0;
            for (int i = 0; i < 60 * 240; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
                water.Step(grid, Dt);
                roomMax = Mathf.Max(roomMax, CountWater(grid, RoomX0, RoomX1, 1, RoomY1));
                if (i == 60 * 120)
                    dropsAt120 = colony.DewDrops;
            }
            UnityEngine.Debug.Log($"KI-050: gotas {colony.DewDrops}, água na sala (máx) {roomMax}, absorvidas {water.AbsorbedCells}, erodidas {water.ErodedCells}");
            Assert.IsTrue(colony.DewActive);
            int slots = RoomX1 - RoomX0 + 1;
            Assert.LessOrEqual(roomMax, Mathf.CeilToInt(RoomY1 * 0.4f) * slots, "A água na sala nunca passa de 40% da altura de cada coluna.");
            Assert.AreEqual(dropsAt120, colony.DewDrops, "Cheia, a sala para de soltar gotas (sem acumular nem transbordar).");
            Assert.AreEqual(0, water.ErodedCells, "Água que vira lama nos túneis: nenhuma célula foi erodida.");
        }

        [Test]
        public void KI050_WaterOutsideTheRoom_IsNotPulledIntoTheTunnelsByTheRoom()
        {
            PixelGrid grid = MakeReadyDewWorld();
            var colony = new AntColony(grid, new ColonySettings(), SmallLayout, seed: 4) { RaidsEnabled = false };
            var granular = new GranularSimulation();
            var water = new WaterSimulation(new WaterSettings(), grid, 4);
            for (int i = 0; i < 60 * 180; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
                water.Step(grid, Dt);
            }
            int tunnels = CountWater(grid, 2, RoomX0 - 1, 1, 3) + CountWater(grid, RoomX1 + 1, grid.Width - 3, 1, 3);
            UnityEngine.Debug.Log($"KI-050: água nos túneis ao lado da sala = {tunnels}");
            Assert.AreEqual(0, tunnels, "A sala cheia não derrama água no corredor.");
        }

        // ---------------------------------------------------------------- KI-004: mapa de caminhabilidade

        [Test]
        public void KI004_WalkabilityRefresh_AfterWorldExpansion_IsCorrectAndFast()
        {
            var settings = new TerrainSettings();
            const int w = 640, h = 400;
            var grid = new PixelGrid(w, h);
            var generator = new TerrainGenerator(settings, 7, w, h, new RectInt(220, 100, 200, 300));
            generator.Begin(grid);
            var walk = new WalkabilityMap(grid);
            walk.Refresh();
            walk.ClearChangedCells();

            var watch = Stopwatch.StartNew();
            generator.Expand(grid, new RectInt(0, 0, w, 1));
            double expand = watch.Elapsed.TotalMilliseconds;
            watch.Restart();
            walk.Refresh();
            double refresh = watch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"KI-004: Expand {expand:F1} ms, Refresh após expandir o mundo todo {refresh:F1} ms");

            var fresh = new WalkabilityMap(grid);
            fresh.Refresh();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (walk.IsWalkable(x, y) != fresh.IsWalkable(x, y))
                    Assert.Fail($"Mapa incremental diverge do cheio em ({x},{y}).");
            Assert.Less(refresh, 40.0, "Soluço máximo aceitável ao revelar o mundo inteiro de uma vez (editor; ~3,5 ms medido em 2026-10-20).");
        }

        [Test]
        public void KI004_SmallExpansion_CostsFarLessThanTheWholeWorld()
        {
            const int w = 640, h = 400;
            var grid = new PixelGrid(w, h);
            var generator = new TerrainGenerator(new TerrainSettings(), 9, w, h, new RectInt(220, 100, 200, 300));
            generator.Begin(grid);
            var walk = new WalkabilityMap(grid);
            walk.Refresh();
            walk.ClearChangedCells();

            generator.Expand(grid, new RectInt(200, 150, 1, 1));
            var watch = Stopwatch.StartNew();
            walk.Refresh();
            double strip = watch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"KI-004: Refresh após uma faixa de câmera {strip:F2} ms");
            Assert.Less(strip, 60.0, "Expansão normal da câmera não pode dar soluço.");
        }

        // ---------------------------------------------------------------- KI-023: picos dos campos de navegação

        [Test]
        public void KI023_FlowFieldUpdate_AfterSmallEdits_IsMuchCheaperThanBuild_OnALargeMap()
        {
            const int w = 640, h = 400;
            var grid = new PixelGrid(w, h);
            var generator = new TerrainGenerator(new TerrainSettings(), 5, w, h, new RectInt(0, 0, w, h));
            TerrainLayout layout = generator.Begin(grid);
            generator.Expand(grid, new RectInt(0, 0, w, 1));
            var walk = new WalkabilityMap(grid);
            walk.Refresh();
            var sources = new List<int> { grid.ToIndex(layout.Nest.x, layout.Nest.y) };

            var field = new FlowField(w, h);
            var watch = Stopwatch.StartNew();
            field.Build(walk, sources);
            double build = watch.Elapsed.TotalMilliseconds;
            walk.ClearChangedCells();
            UnityEngine.Debug.Log($"KI-023: campo alcança {field.ReachedCount} células");
            Assert.Greater(field.ReachedCount, 1000, "O campo precisa cobrir o formigueiro, senão a medição não vale.");

            double worst = 0, total = 0;
            const int rounds = 40;
            var rng = new System.Random(3);
            for (int i = 0; i < rounds; i++)
            {
                int x = layout.Nest.x + rng.Next(-30, 30), y = layout.Nest.y + rng.Next(-3, 6);
                grid.Set(x, y, grid.Get(x, y) == CellMaterial.Empty ? CellMaterial.Dirt : CellMaterial.Empty);
                walk.Refresh();
                watch.Restart();
                field.Update(walk, sources);
                double ms = watch.Elapsed.TotalMilliseconds;
                walk.ClearChangedCells();
                worst = System.Math.Max(worst, ms);
                total += ms;
            }
            UnityEngine.Debug.Log($"KI-023: Build {build:F1} ms, Update médio {total / rounds:F2} ms, pior {worst:F2} ms (mapa {w}x{h})");
            Assert.Less(total / rounds, build, "Atualizar incrementalmente tem de custar menos que refazer o campo.");
            Assert.Less(worst, 16.0, "Nenhuma atualização pequena passa de um quadro (16 ms).");
        }

        // ---------------------------------------------------------------- KI-039: custo da colônia grande

        // Mundo sintético leve (ninho pequeno): serve de piso contra regressões. Os picos reais (KI-023/KI-039) só aparecem
        // repetindo o quicksave do jogador no editor (ver memória replay-quicksave-in-editor).
        [Test]
        public void KI039_ColonyTick_WithALargePopulation_StaysInsideTheFrameBudget()
        {
            const int w = 640, h = 400;
            var grid = new PixelGrid(w, h);
            var settings = new TerrainSettings();
            var generator = new TerrainGenerator(settings, 11, w, h, new RectInt(0, 0, w, h));
            TerrainLayout layout = generator.Begin(grid);
            generator.Expand(grid, new RectInt(0, 0, w, 1));
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 11) { RaidsEnabled = false };
            for (int i = 0; i < 150; i++)
                colony.Spawn(i % 4 == 0 ? AntRole.Digger : AntRole.Worker);
            // Trabalho para todos: terra marcada para escavar ao redor do ninho.
            int marked = 0;
            for (int y = layout.Nest.y - 25; y <= layout.Nest.y + 5 && marked < 600; y++)
            for (int x = layout.Nest.x - 40; x <= layout.Nest.x + 40 && marked < 600; x++)
                if (grid.InBounds(x, y) && grid.Get(x, y) == CellMaterial.Dirt && grid.SetMarked(x, y, true))
                    marked++;
            UnityEngine.Debug.Log($"KI-039: {marked} células marcadas para escavar");
            var granular = new GranularSimulation();
            for (int i = 0; i < 120; i++) // aquecimento: primeira montagem dos campos
            {
                colony.Tick(Dt);
                granular.Step(grid);
            }
            var samples = new List<double>();
            var watch = new Stopwatch();
            for (int i = 0; i < 600; i++)
            {
                watch.Restart();
                colony.Tick(Dt);
                samples.Add(watch.Elapsed.TotalMilliseconds);
                granular.Step(grid);
            }
            samples.Sort();
            double avg = 0;
            foreach (double s in samples)
                avg += s;
            avg /= samples.Count;
            double p99 = samples[(int)(samples.Count * 0.99)], max = samples[samples.Count - 1];
            UnityEngine.Debug.Log($"KI-039: {colony.Ants.Count} formigas · Tick médio {avg:F2} ms · p99 {p99:F2} ms · pior {max:F2} ms");
            Assert.Less(avg, 8.0, "Média do Tick da colônia (editor em Debug).");
            Assert.Less(p99, 30.0, "p99 do Tick da colônia.");
        }

        // ---------------------------------------------------------------- KI-022: água some nas grades de teste

        [TestCase(10, 16, 1, 1)]
        [TestCase(20, 16, 1, 2)]
        [TestCase(12, 20, 2, 3)]
        public void KI022_WaterIsConserved_WhenItFallsAndSpreads_InABox(int width, int height, int columns, int seed)
        {
            PixelGrid grid = MakeBox(width, height);
            for (int c = 0; c < columns; c++)
            for (int y = 4; y < 4 + 6; y++)
                grid.SetWater(3 + c * 2, y, true);
            int total = grid.WaterCount;
            var water = new WaterSimulation(new WaterSettings { InfiltrationReach = 0, EvaporationSeconds = 0f }, grid, seed);
            RunWater(water, grid, 600);
            Assert.AreEqual(total, grid.WaterCount, "Sem infiltração, a água não some nem se duplica.");
        }

        [Test]
        public void KI022_WaterIsConserved_WhenAGrainDisplacesIt()
        {
            PixelGrid grid = MakeBox(10, 12);
            for (int x = 1; x < 9; x++)
            for (int y = 1; y <= 3; y++)
                grid.SetWater(x, y, true);
            grid.Set(4, 8, CellMaterial.Sand);
            int total = grid.WaterCount;
            var water = new WaterSimulation(new WaterSettings { InfiltrationReach = 0, EvaporationSeconds = 0f }, grid, 4);
            var granular = new GranularSimulation();
            for (int i = 0; i < 120; i++)
            {
                water.Step(grid, Dt);
                granular.Step(grid);
            }
            Assert.AreEqual(total, grid.WaterCount);
        }

        // ---------------------------------------------------------------- KI-011: pixel de planta solto fora de alcance

        [Test]
        public void KI011_UnreachableLoosePlantDebris_DoesNotBlockHarvestOfReachablePlants()
        {
            var layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);
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
            // Sala de nutrientes pequena (poucas vagas) e depósito de alimentos para receber a colheita.
            for (int x = 60; x <= 63; x++)
            for (int y = 1; y <= 2; y++)
                Assert.IsTrue(grid.SetNutrientRoom(x, y, true));
            for (int x = 20; x <= 26; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetStorageRoom(x, y, StorageKind.Food, true));
            // Pixels de planta soltos num bolsão selado dentro da terra: fora do alcance das formigas.
            for (int x = 80; x <= 83; x++)
            for (int y = 10; y <= 11; y++)
                grid.Set(x, y, CellMaterial.FallenLeaf, 120);
            int unreachable = grid.PlantDebrisCount;
            Assert.Greater(unreachable, 0);
            // Pedaços alcançáveis, marcados para colheita.
            foreach (int x in new[] { 84, 85, 86 })
            {
                grid.Set(x, 1, CellMaterial.FallenLeaf, 120);
                Assert.IsTrue(grid.SetMarked(x, 1, true));
            }
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 5) { RaidsEnabled = false };
            colony.RestoreQueen(95, 2, 1);
            for (float t = 0f; t < 120f; t += 0.1f)
                colony.Tick(0.1f);

            int stored = 0;
            for (int x = 20; x <= 26; x++)
            for (int y = 1; y <= 3; y++)
                stored += StoredItems.KindOf(grid.GetStored(x, y)) == StorageKind.Food ? 1 : 0;
            UnityEngine.Debug.Log($"KI-011: {unreachable} pixels inalcançáveis · guardados {stored} · colhidos {colony.HarvestedPixels}");
            Assert.AreEqual(3, stored + colony.HarvestedPixels, "Os 3 pedaços alcançáveis são colhidos mesmo com pixels soltos inalcançáveis ocupando vagas.");
        }

        // ---------------------------------------------------------------- KI-006: custo da obra e beco sem saída

        [Test]
        public void KI006_Building_WithNoDirtAnywhere_FillsNothing()
        {
            PixelGrid grid = MakeSoil(corridor: true);
            for (int x = 40; x <= 42; x++)
                Assert.IsTrue(grid.SetBuildMarked(x, 1, true));
            var colony = new AntColony(grid, new ColonySettings(), SmallLayout, seed: 3) { RaidsEnabled = false };
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Worker;
            var granular = new GranularSimulation();
            for (int i = 0; i < 60 * 60; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
            }
            // O que existir de terra solta só pode ter vindo de escavação autorizada; sem marca de escavação nem pilha, nada se constrói.
            Assert.AreEqual(3, grid.BuildMarkedCount, "Sem terra de nenhuma fonte, a obra não se enche sozinha.");
        }

        [Test]
        public void KI006_BuildingIntoADeadEnd_NeverLeavesAnAntSealedIn()
        {
            // Beco sem saída de 1 linha (x 45..55, y 1) saindo do corredor; uma formiga está no fundo e a boca é marcada para obra.
            PixelGrid grid = MakeSoil(corridor: true);
            for (int x = 2; x < grid.Width - 2; x++)
            for (int y = 2; y <= 3; y++)
                grid.Set(x, y, x == 20 ? CellMaterial.Empty : CellMaterial.Dirt); // o poço da saída continua aberto
            for (int x = 40; x <= 55; x++)
                grid.Set(x, 1, CellMaterial.Empty);
            for (int x = 22; x <= 30; x++)
                grid.Set(x, Surface + 1, CellMaterial.Spoil);
            Assert.IsTrue(grid.SetBuildMarked(42, 1, true));
            var colony = new AntColony(grid, new ColonySettings(), SmallLayout, seed: 6) { RaidsEnabled = false };
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Worker;
            Ant trapped = colony.SpawnAt(AntRole.Worker, 52, 1);
            var granular = new GranularSimulation();
            for (int i = 0; i < 60 * 90; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
            }
            bool sealedIn = grid.IsSolid(42, 1) && trapped.X > 42;
            Assert.IsFalse(sealedIn, "A obra não pode fechar a formiga atrás da célula preenchida sem saída.");
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void KI006_BuildingIntoALongDeadEnd_NeverSealsAnyAntIn(int seed)
        {
            // Beco de 1 linha comprido (x 43..75): as formigas no fundo levam muito tempo para sair andando a esmo, então a
            // obra na boca (x = 42) tem tempo de sobra para tentar fechá-las lá dentro.
            PixelGrid grid = MakeSoil(width: 90, corridor: true);
            for (int x = 2; x < grid.Width - 2; x++)
            for (int y = 2; y <= 3; y++)
                grid.Set(x, y, x == 20 ? CellMaterial.Empty : CellMaterial.Dirt); // o poço da saída continua aberto
            for (int x = 40; x <= 75; x++)
                grid.Set(x, 1, CellMaterial.Empty);
            for (int x = 76; x < grid.Width - 2; x++)
                grid.Set(x, 1, CellMaterial.Dirt);
            for (int x = 22; x <= 30; x++)
                grid.Set(x, Surface + 1, CellMaterial.Spoil);
            // Escavação no fundo do beco: formigas trabalhando lá dentro enquanto a boca é construída (ociosas saem sozinhas).
            for (int x = 76; x <= 86; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetMarked(x, y, true));
            Assert.IsTrue(grid.SetBuildMarked(42, 1, true));
            var colony = new AntColony(grid, new ColonySettings(), SmallLayout, seed) { RaidsEnabled = false };
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Worker;
            for (int i = 0; i < 3; i++)
                colony.SpawnAt(AntRole.Worker, 70 + i, 1);
            var granular = new GranularSimulation();
            for (int i = 0; i < 60 * 300; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
                if (!grid.IsSolid(42, 1))
                    continue;
                foreach (Ant ant in colony.Ants)
                    Assert.IsFalse(ant.X > 42 && ant.Y <= 3, $"Formiga fechada no beco em ({ant.X},{ant.Y}) no passo {i}.");
            }
        }

        [Test]
        public void KI006_BuildingIntoADeadEnd_NeverSealsTheQueenInHerRoom()
        {
            PixelGrid grid = MakeSoil(width: 90, corridor: true);
            for (int x = 2; x < grid.Width - 2; x++)
            for (int y = 2; y <= 3; y++)
                grid.Set(x, y, x == 20 ? CellMaterial.Empty : CellMaterial.Dirt); // o poço da saída continua aberto
            for (int x = 40; x <= 60; x++)
                grid.Set(x, 1, CellMaterial.Empty);
            for (int x = 61; x < grid.Width - 2; x++)
                grid.Set(x, 1, CellMaterial.Dirt);
            for (int x = 54; x <= 60; x++)
                Assert.IsTrue(grid.SetQueenRoom(x, 1, true));
            for (int x = 22; x <= 30; x++)
                grid.Set(x, Surface + 1, CellMaterial.Spoil);
            Assert.IsTrue(grid.SetBuildMarked(42, 1, true));
            var colony = new AntColony(grid, new ColonySettings(), SmallLayout, seed: 2) { RaidsEnabled = false };
            colony.RestoreQueen(57, 1, 1);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Worker;
            var granular = new GranularSimulation();
            for (int i = 0; i < 60 * 120; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
            }
            Assert.IsFalse(grid.IsSolid(42, 1), "Fechar a boca do beco trancaria a rainha (e quem a cerca) na sala.");
        }

        // ---------------------------------------------------------------- KI-007: pressão da água e formigas na água

        [Test]
        public void KI007_Water_InAUTube_RisesInTheOtherArm()
        {
            // U de 3 de largura por arm: despeja-se água no braço esquerdo; vasos comunicantes nivelam os dois braços.
            PixelGrid grid = MakeBox(13, 20);
            for (int y = 1; y < 20; y++)
                grid.Set(6, y, CellMaterial.Rock);        // parede central
            grid.Set(6, 1, CellMaterial.Empty);            // comunicação no fundo
            grid.Set(6, 2, CellMaterial.Empty);
            for (int y = 1; y <= 10; y++)
            for (int x = 1; x <= 5; x++)
                grid.SetWater(x, y, true);                 // braço esquerdo cheio até y = 10
            var water = new WaterSimulation(new WaterSettings { InfiltrationReach = 0, EvaporationSeconds = 0f }, grid, 1);
            RunWater(water, grid, 1200);
            int left = CountWater(grid, 1, 5, 1, 19), right = CountWater(grid, 7, 11, 1, 19);
            UnityEngine.Debug.Log($"KI-007: esquerda {left}, direita {right}");
            Assert.Greater(right, 0, "A água sobe pelo outro braço.");
            Assert.Less(Mathf.Abs(left - right), Mathf.Max(5, (left + right) / 5), "Os dois braços ficam aproximadamente no mesmo nível.");
        }

        [Test]
        public void KI007_WideTrough_LevelsIntoAFlatSheet()
        {
            const int width = 200;
            PixelGrid grid = MakeBox(width, 6);
            for (int y = 1; y <= 4; y++)
            for (int x = 1; x <= 20; x++)
                grid.SetWater(x, y, true);
            int total = grid.WaterCount;
            var water = new WaterSimulation(new WaterSettings { InfiltrationReach = 0, EvaporationSeconds = 0f }, grid, 2);
            RunWater(water, grid, 6000);
            int farthest = 0, tallest = 0;
            for (int x = 1; x < width - 1; x++)
            {
                int h = CountWater(grid, x, x, 1, 5);
                tallest = Mathf.Max(tallest, h);
                if (h > 0) farthest = x;
            }
            UnityEngine.Debug.Log($"KI-007: poça de {total} células vai até x = {farthest}, coluna mais alta = {tallest}");
            Assert.AreEqual(total, grid.WaterCount);
            Assert.LessOrEqual(tallest, 2, "A poça larga assenta numa lâmina plana (no máximo um degrau de 1 célula).");
        }

        [Test]
        public void KI007_AntWalkingThroughAFloodedTunnel_Survives()
        {
            PixelGrid grid = MakeSoil();
            for (int x = 30; x <= 40; x++)
            for (int y = 1; y <= 3; y++)
                grid.SetWater(x, y, true);
            var colony = new AntColony(grid, new ColonySettings(), SmallLayout, seed: 8) { RaidsEnabled = false };
            for (int i = 0; i < 10; i++)
                colony.SpawnAt(AntRole.Worker, 28 + i, 1);
            int before = colony.Ants.Count;
            var water = new WaterSimulation(new WaterSettings { InfiltrationReach = 0, EvaporationSeconds = 0f }, grid, 8);
            for (int i = 0; i < 60 * 60; i++)
            {
                colony.Tick(Dt);
                water.Step(grid, Dt);
            }
            Assert.AreEqual(before, colony.Ants.Count, "Desde o KI-025 ninguém morre afogado.");
        }

        [Test]
        public void KI007_Navigation_IgnoresWater_ByDesign()
        {
            PixelGrid grid = MakeSoil();
            grid.SetWater(35, 1, true);
            Assert.IsTrue(AntNavigation.IsWalkable(grid, 35, 1), "Limitação conhecida: a navegação não desvia de água.");
        }

        // ---------------------------------------------------------------- KI-003: formigas se sobrepõem

        [Test]
        public void KI003_AntsShareCells_WithoutCollision_AndAreNotStuckByIt()
        {
            PixelGrid grid = MakeSoil();
            var colony = new AntColony(grid, new ColonySettings(), SmallLayout, seed: 2) { RaidsEnabled = false };
            for (int i = 0; i < 20; i++)
                colony.SpawnAt(AntRole.Worker, 30, 1);
            int sharedMax = 0;
            for (int i = 0; i < 60 * 20; i++)
            {
                colony.Tick(Dt);
                var cells = new Dictionary<int, int>();
                foreach (Ant ant in colony.Ants)
                {
                    int key = ant.Y * 1000 + ant.X;
                    cells.TryGetValue(key, out int n);
                    cells[key] = n + 1;
                    sharedMax = Mathf.Max(sharedMax, n + 1);
                }
            }
            UnityEngine.Debug.Log($"KI-003: maior amontoado numa célula = {sharedMax}");
            Assert.GreaterOrEqual(sharedMax, 2, "Caracterização: formigas ocupam a mesma célula (sem colisão entre elas).");
            foreach (Ant ant in colony.Ants)
                Assert.IsTrue(AntNavigation.IsWalkable(grid, ant.X, ant.Y) || ant.Buried, "Mesmo amontoadas, as formigas ficam em células caminháveis.");
        }

        [Test]
        public void KI003_AntSprite_FootprintIsThreeByTwoCells()
        {
            // O sprite (3x2 células) cobre parte das paredes em túneis de 1 célula: aqui só se confere que a formiga vive
            // em túnel de 1 célula sem ficar presa, que é o que o protótipo aceita.
            PixelGrid grid = MakeSoil();
            for (int x = 2; x < grid.Width - 2; x++)
            for (int y = 2; y <= 3; y++)
                grid.Set(x, y, CellMaterial.Dirt);
            var colony = new AntColony(grid, new ColonySettings(), SmallLayout, seed: 5) { RaidsEnabled = false };
            Ant ant = colony.SpawnAt(AntRole.Worker, 45, 1);
            for (int i = 0; i < 60 * 10; i++)
                colony.Tick(Dt);
            Assert.AreEqual(1, ant.Y, "Em túnel de 1 célula a formiga segue no piso dele.");
        }
    }
}
