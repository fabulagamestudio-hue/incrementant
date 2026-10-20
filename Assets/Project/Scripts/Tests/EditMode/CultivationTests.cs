using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Canteiros da sala de nutrientes (pedido do desenvolvedor em 2026-10-18): terra fofa, depois a planta, depois a água;
    /// brotado, cada lado livre ganha um nutriente pronto.
    /// </summary>
    public sealed class CultivationTests
    {
        const float Dt = 1f / 60f;
        const int Surface = 10;

        /// <summary>
        /// Solo de terra até <see cref="Surface"/>, piso de rocha em y = 0, corredor (y 1..3) de x = 2 até a largura − 3,
        /// poço em x = 20 até a superfície e a sala de nutrientes pintada de x 30 a 40 no corredor.
        /// </summary>
        static PixelGrid MakeWorld(int width = 80)
        {
            var grid = new PixelGrid(width, 30);
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
            for (int x = 30; x <= 40; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetNutrientRoom(x, y, true));
            return grid;
        }

        static TerrainLayout Layout => new TerrainLayout(20, new Vector2Int(20, 1), 50, 55);

        static void AddBed(PixelGrid grid, int x, BedStage stage = BedStage.Soil, float grow = 0f)
        {
            grid.Set(x, 1, CellMaterial.Spoil);
            Assert.IsTrue(grid.SetBed(x, 1, stage, grow));
        }

        [Test]
        public void Bed_GrowsAfterWatering_AndPutsANutrientOnEachFreeSide()
        {
            PixelGrid grid = MakeWorld();
            Assert.IsFalse(grid.SetBed(32, 1, BedStage.Soil), "Só terra fofa vira canteiro.");
            grid.Set(32, 1, CellMaterial.Dirt);
            Assert.IsFalse(grid.SetBed(32, 1, BedStage.Soil), "Terra firme não é terra fofa.");

            AddBed(grid, 34);
            grid.SetBed(34, 1, BedStage.Planted);
            Assert.AreEqual(0, grid.AdvanceBedGrowth(100f), "Plantado mas sem água, não brota.");
            grid.SetBed(34, 1, BedStage.Growing, 10f);
            Assert.AreEqual(1, grid.GrowingBedCount);

            Assert.AreEqual(0, grid.AdvanceBedGrowth(9.5f));
            int version = grid.NutrientVersion;
            Assert.AreEqual(3, grid.AdvanceBedGrowth(0.6f), "No chão: esquerda, direita e cima (o lado de baixo é a rocha).");
            Assert.AreNotEqual(version, grid.NutrientVersion, "Brotar refaz os campos de busca.");
            Assert.IsTrue(grid.HasNutrient(33, 1) && grid.HasNutrient(35, 1) && grid.HasNutrient(34, 2));
            Assert.AreEqual(BedStage.Soil, grid.GetBedStage(34, 1), "A terra fica; a planta e a água se gastam.");
            Assert.AreEqual(0, grid.GrowingBedCount);

            // Lado ocupado (material ou nutriente que ninguém levou) não gera.
            grid.Set(33, 1, CellMaterial.Dirt);
            grid.SetBed(34, 1, BedStage.Growing, 1f);
            Assert.AreEqual(0, grid.AdvanceBedGrowth(2f), "Direita e cima ainda têm o nutriente anterior; a esquerda virou terra.");
            Assert.AreEqual(0, grid.FreeBedSides(34, 1));
        }

        [Test]
        public void Bed_StaysWhereItWasPut_AndComesUndoneWhenTheSoilIsDug()
        {
            PixelGrid grid = MakeWorld();
            AddBed(grid, 34, BedStage.Growing, 5f);
            grid.Set(34, 1, CellMaterial.Empty);
            Assert.AreEqual(0, grid.BedCount, "Cavada a terra, o canteiro some.");
            Assert.AreEqual(0, grid.GrowingBedCount);

            // Canteiro sem chão embaixo (como o de parede): a física não o derruba.
            grid.Set(36, 2, CellMaterial.Spoil);
            Assert.IsTrue(grid.SetBed(36, 2, BedStage.Planted));
            // Terra fofa comum ao lado: essa cai.
            grid.Set(38, 3, CellMaterial.Spoil);
            var granular = new GranularSimulation();
            for (int i = 0; i < 10; i++)
                granular.Step(grid);
            Assert.AreEqual(CellMaterial.Spoil, grid.Get(36, 2), "O canteiro fica preso onde foi posto.");
            Assert.AreEqual(BedStage.Planted, grid.GetBedStage(36, 2));
            Assert.AreEqual(CellMaterial.Spoil, grid.Get(38, 1), "A terra fofa solta continua com física.");
        }

        [Test]
        public void Ants_CanBuildBedsOnTheColonyWalls()
        {
            // Poço vertical de 1 coluna de largura e 4 de altura (x 31, y 1..4): sem lugar no chão para canteiro afastado do
            // de baixo, os de cima vão nas paredes.
            PixelGrid grid = MakeWorld();
            for (int x = 30; x <= 40; x++)
            for (int y = 1; y <= 3; y++)
            {
                grid.SetNutrientRoom(x, y, false);
                if (x != 31)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int y = 1; y <= 8; y++)
            {
                grid.Set(31, y, CellMaterial.Empty);
                grid.Set(32, y, CellMaterial.Empty);
                Assert.IsTrue(grid.SetNutrientRoom(31, y, true));
                Assert.IsTrue(grid.SetNutrientRoom(32, y, true));
            }
            // Liga o poço ao corredor da esquerda.
            for (int x = 22; x <= 30; x++)
                grid.Set(x, 1, CellMaterial.Empty);
            // A terra vem da pilha de fora (o corredor do outro lado do poço ficou fechado).
            for (int x = 50; x <= 56; x++)
            for (int y = Surface + 1; y <= Surface + 2; y++)
                grid.Set(x, y, CellMaterial.Spoil);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            var granular = new GranularSimulation();
            for (int i = 0; i < 60 * 90; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
            }

            int onWall = 0;
            for (int k = 0; k < grid.BedCount; k++)
            {
                grid.TryGetBed(k, out int x, out int y);
                Assert.IsTrue(grid.IsNutrientRoom(x, y));
                onWall += !grid.IsSolid(x, y - 1) ? 1 : 0;
            }
            Assert.GreaterOrEqual(grid.BedCount, 2, "O poço de 2 colunas por 8 cabe canteiros no chão e nas paredes.");
            Assert.Greater(onWall, 0, "Pelo menos um canteiro ficou preso na parede, sem chão embaixo.");
        }

        [Test]
        public void Ants_FillTheMiddleOfABigRoom_WithBeds_WithoutClosingThePassage()
        {
            // Salão de 11 x 8 (x 30..40, y 1..8) ligado ao corredor, com a pilha de fora como fonte de terra.
            PixelGrid grid = MakeWorld();
            for (int x = 30; x <= 40; x++)
            for (int y = 1; y <= 8; y++)
            {
                grid.Set(x, y, CellMaterial.Empty);
                grid.SetNutrientRoom(x, y, true);
            }
            for (int x = 50; x <= 56; x++)
            for (int y = Surface + 1; y <= Surface + 2; y++)
                grid.Set(x, y, CellMaterial.Spoil);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            var granular = new GranularSimulation();
            for (int i = 0; i < 60 * 150; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
            }

            int middle = 0;
            for (int k = 0; k < grid.BedCount; k++)
            {
                grid.TryGetBed(k, out int x, out int y);
                bool touchesWall = false;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    touchesWall |= (dx != 0 || dy != 0) && grid.IsSolid(x + dx, y + dy);
                middle += touchesWall ? 0 : 1;
            }
            Assert.GreaterOrEqual(grid.BedCount, 6, "O salão se enche de canteiros espaçados.");
            Assert.Greater(middle, 0, "Há canteiros no meio da sala, longe do chão e das paredes.");
            // A passagem continua: dá para andar do corredor (x 25) até o fundo do salão (x 40) por células caminháveis.
            var field = new FlowField(grid.Width, grid.Height);
            field.Build(grid, new System.Collections.Generic.List<int> { grid.ToIndex(25, 1) });
            for (int x = 30; x <= 40; x++)
            for (int y = 1; y <= 8; y++)
            {
                if (!grid.IsSolid(x, y) && AntNavigation.IsWalkable(grid, x, y))
                    Assert.AreNotEqual(FlowField.Unreachable, field.Get(x, y), $"({x},{y}) ficou isolada.");
            }
        }

        [Test]
        public void WithNothingToDig_AntsTakeLooseSoilFromTheOutsidePile()
        {
            PixelGrid grid = MakeWorld();
            // A pilha de fora que já existe (terra despejada sobre a superfície, perto da faixa de despejo).
            for (int x = 50; x <= 56; x++)
            for (int y = Surface + 1; y <= Surface + 2; y++)
                grid.Set(x, y, CellMaterial.Spoil);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            var granular = new GranularSimulation();
            bool sawFetching = false;
            for (int i = 0; i < 60 * 90; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
                foreach (Ant ant in colony.Ants)
                    sawFetching |= ant.State == AntState.FetchingSoil || ant.State == AntState.GatheringSoil;
            }

            Assert.AreEqual(0, grid.MarkedCount, "Nada foi marcado para cavar.");
            Assert.IsTrue(sawFetching, "As formigas foram à pilha de fora buscar terra fofa.");
            Assert.GreaterOrEqual(grid.BedCount, 3, "A terra da pilha virou canteiros na sala.");
            Assert.GreaterOrEqual(colony.SoilFromPile, grid.BedCount, "Cada canteiro veio de um pixel da pilha.");
        }

        [Test]
        public void Ants_BringDugSoilToTheRoom_AndBuildSpacedBeds()
        {
            PixelGrid grid = MakeWorld();
            // Terra para cavar no teto do corredor, longe da sala.
            for (int x = 44; x <= 50; x++)
            for (int y = 4; y <= 5; y++)
                grid.SetMarked(x, y, true);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 4);
            var granular = new GranularSimulation();
            bool sawBringing = false;
            for (int i = 0; i < 60 * 60; i++)
            {
                colony.Tick(Dt);
                granular.Step(grid);
                foreach (Ant ant in colony.Ants)
                    sawBringing |= ant.State == AntState.BringingSoil;
            }

            Assert.IsTrue(sawBringing, "Quem cava leva a terra à sala de nutrientes.");
            Assert.GreaterOrEqual(grid.BedCount, 3, "A sala de 11 colunas cabe 3 ou 4 canteiros espaçados.");
            for (int a = 0; a < grid.BedCount; a++)
            {
                Assert.IsTrue(grid.TryGetBed(a, out int ax, out int ay));
                Assert.AreEqual(CellMaterial.Spoil, grid.Get(ax, ay));
                Assert.IsTrue(grid.IsNutrientRoom(ax, ay), "Só na sala de nutrientes.");
                for (int b = a + 1; b < grid.BedCount; b++)
                {
                    grid.TryGetBed(b, out int bx, out int by);
                    Assert.GreaterOrEqual(Mathf.Max(Mathf.Abs(ax - bx), Mathf.Abs(ay - by)), 3,
                                          "Os lados de um canteiro nunca são os de outro.");
                }
            }
            Assert.Greater(colony.StoredPixels, 0, "O resto da terra continua indo ao despejo.");
        }

        [Test]
        public void Ants_PlantAndWaterTheBed_AndItGrowsIntoNutrientsForTheQueen()
        {
            PixelGrid grid = MakeWorld();
            AddBed(grid, 34);
            // Poça parada no corredor (sem a simulação da água no teste, ela não escorre).
            for (int x = 60; x <= 62; x++)
                grid.SetWater(x, 1, true);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 6);
            Ant planter = colony.Ants[0];
            planter.PickUp(CellMaterial.PlantGrass, 128);
            planter.State = AntState.Carrying;

            bool sawFetching = false, sawCarrying = false;
            for (int i = 0; i < 60 * 150 && colony.TotalNutrients < 3; i++)
            {
                colony.Tick(Dt);
                foreach (Ant ant in colony.Ants)
                {
                    sawFetching |= ant.State == AntState.FetchingWater;
                    sawCarrying |= ant.State == AntState.CarryingWater;
                }
            }

            Assert.AreEqual(1, colony.HarvestedPixels, "A planta foi posta no canteiro.");
            Assert.IsTrue(sawFetching && sawCarrying, "Uma formiga foi buscar a água e a levou ao canteiro.");
            Assert.AreEqual(1, colony.WaterCollected);
            Assert.AreEqual(2, grid.WaterCount, "O bloco de água saiu do mapa.");
            Assert.AreEqual(3, colony.GrownNutrients, "Brotou um nutriente em cada lado livre.");
            Assert.AreEqual(3, colony.TotalNutrients, "E a rainha consumiu os três.");
            Assert.AreEqual(BedStage.Soil, grid.GetBedStage(34, 1), "O canteiro volta a esperar planta.");
        }

        [Test]
        public void PlantedBed_WaitsForWater_WhenTheMapHasNone()
        {
            PixelGrid grid = MakeWorld();
            AddBed(grid, 34, BedStage.Planted);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 6);
            for (int i = 0; i < 60 * 60; i++)
            {
                colony.Tick(Dt);
                foreach (Ant ant in colony.Ants)
                    Assert.AreNotEqual(AntState.FetchingWater, ant.State, "Sem água no mapa, ninguém sai para buscar.");
            }
            Assert.AreEqual(BedStage.Planted, grid.GetBedStage(34, 1));
            Assert.AreEqual(1, colony.BedsAwaitingWater);
            Assert.AreEqual(0, grid.NutrientCount);
        }

        /// <summary>
        /// Pedido atendido também na água (pedido do desenvolvedor em 2026-10-19): cada formiga reserva o bloco de água que
        /// vai tirar e o canteiro que vai regar; duas nunca vão ao mesmo, e todos os canteiros são regados.
        /// </summary>
        [Test]
        public void WaterCarriers_ReserveDifferentBlocksAndBeds()
        {
            PixelGrid grid = MakeWorld();
            int[] beds = { 31, 34, 37, 40 };
            foreach (int x in beds)
                AddBed(grid, x, BedStage.Planted);
            for (int x = 60; x <= 66; x++)
                grid.SetWater(x, 1, true);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 6);
            for (int i = 0; i < 7; i++)
                colony.Spawn(AntRole.Worker);

            var taken = new System.Collections.Generic.HashSet<int>();
            bool sawWaterReserve = false, sawBedReserve = false;
            int carriersAtOnce = 0;
            for (int i = 0; i < 60 * 120 && colony.WaterCollected < beds.Length; i++)
            {
                colony.Tick(Dt);
                taken.Clear();
                int carriers = 0;
                foreach (Ant ant in colony.Ants)
                {
                    bool water = ant.State == AntState.FetchingWater || ant.State == AntState.CarryingWater;
                    carriers += water ? 1 : 0;
                    if (!water || ant.SeekTarget < 0)
                        continue;
                    sawWaterReserve |= ant.State == AntState.FetchingWater;
                    sawBedReserve |= ant.State == AntState.CarryingWater;
                    Assert.IsTrue(taken.Add(ant.SeekTarget), $"Duas formigas reservaram o mesmo bloco de água ou canteiro no passo {i}.");
                }
                carriersAtOnce = Mathf.Max(carriersAtOnce, carriers);
            }
            for (int i = 0; i < 60 * 10; i++)
                colony.Tick(Dt);

            Assert.IsTrue(sawWaterReserve && sawBedReserve, "As formigas reservam o bloco de água e depois o canteiro.");
            Assert.LessOrEqual(carriersAtOnce, beds.Length, "Uma formiga por canteiro esperando água.");
            Assert.AreEqual(beds.Length, colony.WaterCollected, "Cada canteiro recebeu um bloco, sem bloco a mais.");
            foreach (int x in beds)
                Assert.AreNotEqual(BedStage.Planted, grid.GetBedStage(x, 1), $"O canteiro em x = {x} foi regado.");
        }

        /// <summary>
        /// Relato de 2026-10-19: com vaga para um só pedaço de planta, todas as ociosas desciam o campo de escavação atrás das
        /// folhas caídas marcadas e as que perdiam a disputa ficavam paradas esperando. A reserva do pedaço solto ocupa a
        /// vaga, então só vai quem cabe, e as outras seguem andando.
        /// </summary>
        [Test]
        public void FallenLeaves_OnlyAsManyAsFitAreReserved_AndTheOtherAntsKeepMoving()
        {
            PixelGrid grid = MakeWorld();
            AddBed(grid, 34);
            for (int x = 60; x <= 69; x++)
            {
                grid.Set(x, 1, CellMaterial.FallenLeaf);
                Assert.IsTrue(grid.SetMarked(x, 1, true));
            }
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 6) { RaidsEnabled = false };
            for (int i = 0; i < 12; i++)
                colony.Spawn(AntRole.Worker);

            var lastCell = new System.Collections.Generic.Dictionary<Ant, int>();
            var stillSince = new System.Collections.Generic.Dictionary<Ant, float>();
            float longestStill = 0f;
            float t = 0f;
            for (int i = 0; i < 60 * 60; i++)
            {
                colony.Tick(Dt);
                t += Dt;
                int leafClaims = 0;
                foreach (Ant ant in colony.Ants)
                {
                    bool seeksLeaf = ant.State == AntState.Idle && ant.SeekTarget >= 0 &&
                                     grid.Get(ant.SeekTarget % grid.Width, ant.SeekTarget / grid.Width) == CellMaterial.FallenLeaf;
                    bool digsLeaf = ant.State == AntState.Digging && grid.Get(ant.DigX, ant.DigY) == CellMaterial.FallenLeaf;
                    if (seeksLeaf || digsLeaf)
                        leafClaims++;

                    int cell = grid.ToIndex(ant.X, ant.Y);
                    if (ant.State != AntState.Idle || ant.CarriedCount > 0 || !lastCell.TryGetValue(ant, out int previous) || previous != cell)
                    {
                        lastCell[ant] = cell;
                        stillSince[ant] = t;
                        continue;
                    }
                    if (i > 60 * 5)
                        longestStill = Mathf.Max(longestStill, t - stillSince[ant]);
                }
                Assert.LessOrEqual(leafClaims, 1, $"Só cabe um pedaço de planta, mas {leafClaims} formigas foram atrás de folhas em t = {t:0.00} s.");
            }

            Assert.AreNotEqual(BedStage.Soil, grid.GetBedStage(34, 1), "O canteiro recebeu a folha.");
            Assert.AreEqual(9, CountLeaves(grid), "Só uma folha saiu do chão: não havia lugar para as outras.");
            Assert.Less(longestStill, 1.5f, "Nenhuma ociosa de mão vazia fica parada no lugar esperando a folha de outra.");
        }

        static int CountLeaves(PixelGrid grid)
        {
            int count = 0;
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
                if (grid.Get(x, y) == CellMaterial.FallenLeaf)
                    count++;
            return count;
        }
    }
}
