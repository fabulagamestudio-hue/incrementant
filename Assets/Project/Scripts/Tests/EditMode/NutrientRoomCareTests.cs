using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Cuidado da sala de nutrientes, o cultivo (design 14/18/26): o canteiro regado só brota com formiga trabalhando nela.</summary>
    public sealed class NutrientRoomCareTests
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
                    grid.Set(x, y, CellMaterial.Dirt); // teto do corredor (y 1 a 3), para a sala caber abaixo da superfície
            }
            for (int x = 30; x <= 41; x++)
            for (int y = 1; y <= 2; y++)
                Assert.IsTrue(grid.SetNutrientRoom(x, y, true));
            for (int i = 0; i < growing; i++)
            {
                grid.Set(BedX(i), 1, CellMaterial.Spoil);
                Assert.IsTrue(grid.SetBed(BedX(i), 1, BedStage.Growing, growSeconds));
            }
            return (grid, new TerrainLayout(45, new Vector2Int(45, 1), 55, 60));
        }

        static int BedX(int i) => 30 + 3 * i;

        static void Run(AntColony colony, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
                colony.Tick(Dt);
        }

        static int Gardening(AntColony colony, AntRole? role = null)
        {
            int count = 0;
            foreach (Ant ant in colony.Ants)
                count += ant.State == AntState.Gardening && (role == null || ant.Role == role) ? 1 : 0;
            return count;
        }

        [Test]
        public void TheBed_DoesNotGrow_WithoutAnAntInTheRoom()
        {
            var (grid, layout) = MakeWorld(3, 20f);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 2);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Digger; // escavadoras não cuidam do cultivo
            Run(colony, 60f);
            Assert.AreEqual(3, grid.GrowingBedCount, "Sem formiga na sala, nada brota.");
            Assert.AreEqual(20f, grid.GetBedGrow(30, 1), 1e-3f);
        }

        [Test]
        public void ACommonAnt_TendsTheBeds_AndTheyBecomeNutrients()
        {
            var (grid, layout) = MakeWorld(3, 20f);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 2);
            bool sawGardening = false;
            for (float t = 0f; t < 90f && grid.GrowingBedCount > 0; t += Dt)
            {
                colony.Tick(Dt);
                sawGardening |= Gardening(colony) > 0;
            }

            Assert.IsTrue(sawGardening, "Cuidar é ir de canteiro em canteiro dentro da sala.");
            Assert.AreEqual(0, grid.GrowingBedCount, "Cuidados, os três canteiros brotam.");
            Assert.AreEqual(9, colony.GrownNutrients, "No chão, cada canteiro brota 3 nutrientes (esquerda, direita e cima).");
            Assert.AreEqual(3, grid.BedCount, "A terra fica para o próximo ciclo.");
            Assert.AreEqual(BedStage.Soil, grid.GetBedStage(30, 1));
        }

        [Test]
        public void TheGardener_TakesPartOfTheRoom_AndTheCommonsLeaveRoomForHer()
        {
            var (grid, layout) = MakeWorld(4, 400f);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 2);
            colony.Tree.Restore(1UL << (int)QueenNode.J0);
            Ant gardener = colony.Spawn(AntRole.Gardener);
            bool gardened = false;

            Run(colony, 30f);
            for (float t = 0f; t < 30f; t += Dt)
            {
                colony.Tick(Dt);
                gardened |= gardener.State == AntState.Gardening;
            }

            Assert.IsTrue(gardened, "A jardineira cuida da sala.");
            Assert.LessOrEqual(Gardening(colony, AntRole.Worker), 1,
                               "4 canteiros chamam 2 formigas: a jardineira ocupa uma vaga e sobra uma para as comuns.");
        }

        [Test]
        public void TheMoreGrowingBeds_TheMoreAntsTheRoomCalls()
        {
            int Most(int beds)
            {
                var (grid, layout) = MakeWorld(beds, 400f);
                var colony = new AntColony(grid, new ColonySettings(), layout, seed: 2);
                int most = 0;
                for (float t = 0f; t < 30f; t += Dt)
                {
                    colony.Tick(Dt);
                    most = Mathf.Max(most, Gardening(colony));
                }
                return most;
            }

            int few = Most(1);
            int many = Most(4);
            Assert.LessOrEqual(few, 1, "1 canteiro chama 1 formiga.");
            Assert.Greater(many, few, "Quanto mais canteiros brotando, mais formigas vão trabalhar na sala.");
        }

        [Test]
        public void OnlyTheBedInInteraction_GetsTheSpecialistsBonus()
        {
            var (grid, _) = MakeWorld(3, 100f);
            Assert.AreEqual(0, grid.ShaveBedGrowth(30, 1, 10f));
            Assert.AreEqual(90f, grid.GetBedGrow(30, 1), 1e-3f, "O bônus abate só o canteiro em interação.");
            Assert.AreEqual(100f, grid.GetBedGrow(33, 1), 1e-3f);
            Assert.AreEqual(3, grid.GrowingBedCount);
            Assert.AreEqual(3, grid.ShaveBedGrowth(33, 1, 500f), "Abatido por inteiro, o canteiro brota na hora.");
            Assert.AreEqual(2, grid.GrowingBedCount);
            Assert.IsTrue(grid.HasNutrient(32, 1) && grid.HasNutrient(34, 1) && grid.HasNutrient(33, 2));
        }

        [Test]
        public void TheGardenerCovers6B_TheCommon4B_AndJ1Speeds_TheCure()
        {
            var tree = new QueenTree();
            Assert.AreEqual(6, tree.GardenBlocks(AntRole.Gardener));
            Assert.AreEqual(4, tree.GardenBlocks(AntRole.Worker));
            Assert.AreEqual(0, tree.GardenBlocks(AntRole.Cutter));
            Assert.AreEqual(1f, tree.GardenRate(AntRole.Gardener));
            tree.Restore(1UL << (int)QueenNode.J1);
            Assert.AreEqual(1.3f, tree.GardenRate(AntRole.Gardener), 1e-5f, "J1: ×1,3 na área atendida.");
            Assert.AreEqual(1f, tree.GardenRate(AntRole.Worker));
        }
    }
}
