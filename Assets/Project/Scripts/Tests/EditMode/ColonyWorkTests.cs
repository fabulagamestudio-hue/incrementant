using System.IO;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Painel da colônia: seções de trabalho ligadas, paradas e limitadas, e o que vai para o save.</summary>
    public sealed class ColonyWorkTests
    {
        const float Dt = 0.1f;

        /// <summary>Sala de nutrientes de x 30 a 41 (y 1 e 2) com <paramref name="growing"/> canteiros regados, a cada 3 colunas.</summary>
        static (PixelGrid grid, TerrainLayout layout) MakeWorld(int growing, float growSeconds)
        {
            var grid = new PixelGrid(90, 16);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Bedrock);
                grid.SetSurfaceY(x, 6);
                for (int y = 4; y <= 6; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int x = 30; x <= 41; x++)
            for (int y = 1; y <= 2; y++)
                Assert.IsTrue(grid.SetNutrientRoom(x, y, true));
            for (int i = 0; i < growing; i++)
            {
                grid.Set(30 + 3 * i, 1, CellMaterial.Spoil);
                Assert.IsTrue(grid.SetBed(30 + 3 * i, 1, BedStage.Growing, growSeconds));
            }
            return (grid, new TerrainLayout(45, new Vector2Int(45, 1), 55, 60));
        }

        static int Gardening(AntColony colony)
        {
            int count = 0;
            foreach (Ant ant in colony.Ants)
                count += ant.State == AntState.Gardening ? 1 : 0;
            return count;
        }

        [Test]
        public void StoppingTheGarden_SendsTheTendersAway_AndNobodyComesBack()
        {
            var (grid, layout) = MakeWorld(4, 400f);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 2);
            for (float t = 0f; t < 30f; t += Dt)
                colony.Tick(Dt);
            Assert.Greater(Gardening(colony), 0, "Ligada, a sala chama formigas.");

            colony.Work.SetEnabled(WorkSection.Garden, false);
            for (float t = 0f; t < 5f; t += Dt)
                colony.Tick(Dt);
            for (float t = 0f; t < 30f; t += Dt)
            {
                colony.Tick(Dt);
                Assert.AreEqual(0, Gardening(colony), "Parada, quem cuidava sai e ninguém volta.");
                Assert.AreEqual(0, colony.WorkersIn(WorkSection.Garden));
            }
            float grow = grid.GetBedGrow(30, 1);
            for (float t = 0f; t < 10f; t += Dt)
                colony.Tick(Dt);
            Assert.AreEqual(grow, grid.GetBedGrow(30, 1), 1e-4f, "Sem ninguém na sala, os canteiros não avançam.");

            colony.Work.SetEnabled(WorkSection.Garden, true);
            for (float t = 0f; t < 30f; t += Dt)
                colony.Tick(Dt);
            Assert.Greater(Gardening(colony), 0, "Religada, a sala volta a chamar formigas.");
        }

        [Test]
        public void TheLimit_CapsHowManyAntsWorkInTheSection()
        {
            var (grid, layout) = MakeWorld(4, 400f);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 2);
            for (float t = 0f; t < 30f; t += Dt)
                colony.Tick(Dt);
            Assert.Greater(colony.WorkersIn(WorkSection.Garden), 1, "Sem limite, 4 canteiros chamam mais de uma formiga.");

            colony.Work.SetLimit(WorkSection.Garden, 1);
            for (float t = 0f; t < 5f; t += Dt)
                colony.Tick(Dt);
            bool someoneTended = false;
            for (float t = 0f; t < 30f; t += Dt)
            {
                colony.Tick(Dt);
                Assert.LessOrEqual(colony.WorkersIn(WorkSection.Garden), 1, "Com limite 1, no máximo uma formiga na seção.");
                Assert.LessOrEqual(Gardening(colony), 1);
                someoneTended |= Gardening(colony) == 1;
            }
            Assert.IsTrue(someoneTended, "O limite não para a seção: uma formiga continua cuidando.");
        }

        [Test]
        public void TheCensus_CountsEveryAdultOnce()
        {
            var (grid, layout) = MakeWorld(4, 400f);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 2);
            for (float t = 0f; t < 20f; t += Dt)
                colony.Tick(Dt);
            var census = new ColonyCensus();
            colony.TakeCensus(census);

            int working = 0, places = 0, roles = 0;
            foreach (int n in census.Sections) working += n;
            foreach (int n in census.Places) places += n;
            foreach (int n in census.Roles) roles += n;
            Assert.AreEqual(colony.Ants.Count, working + census.Idle + census.Fighting, "Cada adulta trabalha, está sem trabalho ou luta.");
            Assert.AreEqual(colony.Ants.Count, places);
            Assert.AreEqual(colony.Ants.Count, roles);
            Assert.Greater(census.Places[(int)ColonyPlace.NutrientRoom], 0, "Quem cuida está na sala de nutrientes.");
            for (int i = 0; i < WorkPolicy.Count; i++)
                Assert.AreEqual(colony.WorkersIn(WorkPolicy.All[i]), census.Sections[i], $"A contagem do passo bate com o retrato ({WorkPolicy.All[i]}).");
        }

        [Test]
        public void GardenerQueen_FetchesTheReadyNutrient_AndBackInTheNestGoesHome()
        {
            // Sem canteiro brotando: a jardineira cuidaria deles antes de buscar nutriente.
            var (grid, layout) = MakeWorld(0, 400f);
            for (int x = 50; x <= 53; x++)
            for (int y = 1; y <= 2; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true));
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 2);
            // Limite 0 em alimentar a rainha: nenhuma formiga leva o nutriente; só ela (que não segue limites) o busca.
            colony.Work.SetLimit(WorkSection.QueenFeed, 0);
            colony.QueenJob = QueenJob.Gardener;

            int before = colony.TotalNutrients;
            grid.SetNutrient(38, 1, true);
            bool visited = false;
            for (float t = 0f; t < 60f && colony.TotalNutrients == before; t += Dt)
            {
                colony.Tick(Dt);
                visited |= colony.QueenPlace == ColonyPlace.NutrientRoom;
            }
            Assert.AreEqual(before + 1, colony.TotalNutrients, "A rainha jardineira busca o nutriente pronto e o come.");
            Assert.IsTrue(visited, "Ela foi à sala de nutrientes.");

            colony.QueenJob = QueenJob.Nest;
            for (float t = 0f; t < 30f && !colony.QueenInRoom; t += Dt)
                colony.Tick(Dt);
            Assert.IsTrue(colony.QueenInRoom, "De volta ao ninho, ela vai para a sala dela.");
        }

        [Test]
        public void GardenerQueen_RespectsAStoppedFeedingSection()
        {
            var (grid, layout) = MakeWorld(0, 400f);
            for (int x = 50; x <= 53; x++)
            for (int y = 1; y <= 2; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true));
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 2);
            colony.Work.SetEnabled(WorkSection.QueenFeed, false);
            colony.QueenJob = QueenJob.Gardener;

            int before = colony.TotalNutrients;
            grid.SetNutrient(38, 1, true);
            for (float t = 0f; t < 60f; t += Dt)
            {
                colony.Tick(Dt);
                Assert.AreNotEqual(AntState.Fetching, colony.Queen.State, "Alimentar a rainha parada: nem ela busca o nutriente.");
            }
            Assert.AreEqual(before, colony.TotalNutrients);
        }

        [Test]
        public void QueenJobs_ListTheNestAndEveryRole_AndOldSaveValuesBecomeWorkerAndGardener()
        {
            Assert.AreEqual(QueenJob.Nest, QueenJobs.All[0]);
            Assert.AreEqual(1 + AntRoles.All.Length, QueenJobs.All.Length);
            for (int i = 0; i < AntRoles.All.Length; i++)
            {
                Assert.IsTrue(QueenJobs.TryGetRole(QueenJobs.All[i + 1], out AntRole role));
                Assert.AreEqual(AntRoles.All[i], role);
            }
            Assert.IsFalse(QueenJobs.TryGetRole(QueenJob.Nest, out _));
            // Saves de antes do seletor: 1 = COLETAR RECURSOS, 2 = COLETAR NUTRIENTE.
            Assert.IsTrue(QueenJobs.TryGetRole((QueenJob)1, out AntRole forage));
            Assert.AreEqual(AntRole.Worker, forage);
            Assert.IsTrue(QueenJobs.TryGetRole((QueenJob)2, out AntRole nutrients));
            Assert.AreEqual(AntRole.Gardener, nutrients);
        }

        [Test]
        public void Rates_MeasureUnitsPerSimulatedSecond()
        {
            var rates = new ColonyRates();
            rates.Reset(0f);
            Assert.AreEqual(0f, rates.PerSecond(ColonyRates.Counter.NutrientsToQueen), "Sem duas amostras, sem ritmo.");
            for (int s = 0; s <= 20; s++)
                rates.Sample(s, toQueen: 2 * s, grown: 0f, harvested: s, dumped: 0f);
            Assert.AreEqual(2f, rates.PerSecond(ColonyRates.Counter.NutrientsToQueen), 1e-4f);
            Assert.AreEqual(1f, rates.PerSecond(ColonyRates.Counter.PlantsHarvested), 1e-4f);
            Assert.AreEqual(0f, rates.PerSecond(ColonyRates.Counter.SoilDumped), 1e-4f);
        }

        [Test]
        public void PolicyAndQueenJob_RoundTripThroughTheSave()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout = TerrainGenerator.Generate(grid, settings, 99);
            var colony = new AntColony(grid, new ColonySettings(), layout, 99);
            colony.Work.SetEnabled(WorkSection.Build, false);
            colony.Work.SetLimit(WorkSection.Nursery, 3);
            colony.Work.SetLimit(WorkSection.Waste, 0);
            colony.QueenJob = QueenJob.Cutter;

            var rain = new RainSimulation(new RainSettings(), 99);
            var water = new WaterSimulation(new WaterSettings(), grid, 99);
            var stream = new MemoryStream();
            GameSnapshot.Capture(grid, colony, rain, water, 0, 99, 1.0).Write(stream);
            stream.Position = 0;
            GameSnapshot loaded = GameSnapshot.Read(stream);

            var grid2 = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout2 = TerrainGenerator.Generate(grid2, settings, 99);
            loaded.ApplyTo(grid2);
            var colony2 = new AntColony(grid2, new ColonySettings(), layout2, 99);
            loaded.ApplyTo(colony2);

            Assert.AreEqual(QueenJob.Cutter, colony2.QueenJob);
            Assert.IsFalse(colony2.Work.IsEnabled(WorkSection.Build));
            Assert.IsTrue(colony2.Work.IsEnabled(WorkSection.Dig));
            Assert.AreEqual(3, colony2.Work.LimitOf(WorkSection.Nursery));
            Assert.AreEqual(0, colony2.Work.LimitOf(WorkSection.Waste));
            Assert.AreEqual(WorkPolicy.Unlimited, colony2.Work.LimitOf(WorkSection.Harvest));
        }

        [Test]
        public void OldSaves_ComeBackWithEverythingOn_AndTheQueenInTheNest()
        {
            var policy = new WorkPolicy();
            policy.SetEnabled(WorkSection.Dig, false);
            policy.Restore(new bool[0], new int[0]);
            foreach (WorkSection section in WorkPolicy.All)
            {
                Assert.IsTrue(policy.IsEnabled(section));
                Assert.AreEqual(WorkPolicy.Unlimited, policy.LimitOf(section));
            }
            Assert.AreEqual((byte)QueenJob.Nest, new GameSnapshot().QueenJob, "Save sem o campo: rainha no ninho.");
        }
    }
}
