using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Cuidado da sala de nutrientes, o cultivo (design 14/18/26): o nutriente só cura com formiga trabalhando nela.</summary>
    public sealed class NutrientRoomCareTests
    {
        const float Dt = 0.1f;

        static (PixelGrid grid, TerrainLayout layout) MakeWorld(int curing, float cureSeconds)
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
            for (int i = 0; i < curing; i++)
                Assert.IsTrue(grid.SetNutrient(30 + i, 1, true, cureSeconds));
            return (grid, new TerrainLayout(45, new Vector2Int(45, 1), 55, 60));
        }

        static void Run(AntColony colony, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
                colony.Tick(Dt);
        }

        [Test]
        public void TheNutrient_DoesNotCure_WithoutAnAntInTheRoom()
        {
            var (grid, layout) = MakeWorld(3, 20f);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 2);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Digger; // escavadoras não cuidam do cultivo
            Run(colony, 60f);
            Assert.AreEqual(3, grid.CuringNutrientCount, "Sem formiga na sala, nada cura.");
            Assert.AreEqual(20f, grid.GetNutrientCure(30, 1), 1e-3f);
        }

        [Test]
        public void ACommonAnt_MovesTheCuringPixels_AndTheyBecomeNutrients()
        {
            var (grid, layout) = MakeWorld(3, 20f);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 2);
            bool sawMoving = false;
            for (float t = 0f; t < 90f && grid.ReadyNutrientCount + colony.TotalNutrients < 3; t += Dt)
            {
                colony.Tick(Dt);
                foreach (Ant ant in colony.Ants)
                    sawMoving |= ant.State == AntState.MovingNutrient;
            }

            Assert.IsTrue(sawMoving, "Cuidar é mover os pixels entre os slots da sala.");
            Assert.AreEqual(3, grid.ReadyNutrientCount + colony.TotalNutrients, "Cuidados, os três pixels curam.");
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
                gardened |= gardener.State == AntState.Gardening || gardener.State == AntState.MovingNutrient;
            }

            Assert.IsTrue(gardened, "A jardineira cuida da sala.");
            int commons = 0;
            foreach (Ant ant in colony.Ants)
                commons += ant.Role == AntRole.Worker && (ant.State == AntState.Gardening || ant.State == AntState.MovingNutrient) ? 1 : 0;
            Assert.LessOrEqual(commons, 1, "4 pixels chamam 2 formigas: a jardineira ocupa uma vaga e sobra uma para as comuns.");
        }

        [Test]
        public void TheMoreCuringPixels_TheMoreAntsTheRoomCalls()
        {
            int Most(int pixels)
            {
                var (grid, layout) = MakeWorld(pixels, 400f);
                var colony = new AntColony(grid, new ColonySettings(), layout, seed: 2);
                int most = 0;
                for (float t = 0f; t < 30f; t += Dt)
                {
                    colony.Tick(Dt);
                    int now = 0;
                    foreach (Ant ant in colony.Ants)
                        now += ant.State == AntState.Gardening || ant.State == AntState.MovingNutrient ? 1 : 0;
                    most = Mathf.Max(most, now);
                }
                return most;
            }

            int few = Most(2);
            int many = Most(12);
            Assert.LessOrEqual(few, 1, "2 pixels chamam 1 formiga.");
            Assert.Greater(many, few, "Quanto mais pixels curando, mais formigas vão trabalhar na sala.");
        }

        [Test]
        public void OnlyTheBlockInInteraction_GetsTheSpecialistsBonus()
        {
            var (grid, _) = MakeWorld(3, 100f);
            grid.ShaveNutrientCure(30, 1, 10f);
            Assert.AreEqual(90f, grid.GetNutrientCure(30, 1), 1e-3f, "O bônus abate só o pixel em interação.");
            Assert.AreEqual(100f, grid.GetNutrientCure(31, 1), 1e-3f);
            Assert.IsTrue(grid.TryGetCuringCell(0, out int x, out int y));
            Assert.AreEqual(1, y);
            Assert.AreEqual(3, grid.CuringNutrientCount);
            grid.ShaveNutrientCure(31, 1, 500f);
            Assert.AreEqual(2, grid.CuringNutrientCount, "Abatido por inteiro, o pixel termina de curar.");
            Assert.IsTrue(grid.HasReadyNutrient(31, 1));
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
