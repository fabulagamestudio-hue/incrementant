using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Carga por viagem (seção 15 do design, 1 pixel = 1 UM), rocha quebrada pela escavadora com E2 e prioridades dos papéis.</summary>
    public sealed class CarryAndRockTests
    {
        const float Dt = 1f / 60f;

        static ulong Mask(params QueenNode[] nodes)
        {
            ulong mask = 0;
            foreach (QueenNode node in nodes)
                mask |= 1UL << (int)node;
            return mask;
        }

        /// <summary>Chão de pedra, superfície no chão e uma faixa de <paramref name="material"/> marcada em x 4..9, y 1..2.</summary>
        static (PixelGrid grid, TerrainLayout layout) MakeWorld(CellMaterial material)
        {
            var grid = new PixelGrid(90, 16);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Bedrock);
                grid.SetSurfaceY(x, 0);
            }
            for (int x = 4; x < 10; x++)
            for (int y = 1; y <= 2; y++)
            {
                grid.Set(x, y, material);
                Assert.IsTrue(grid.SetMarked(x, y, true));
            }
            return (grid, new TerrainLayout(45, new Vector2Int(45, 1), 55, 60));
        }

        [Test]
        public void CapacitiesFollowTheDesign_AndTheUpgrades()
        {
            var tree = new QueenTree();
            Assert.AreEqual(2, tree.MaterialCapacity(AntRole.Worker));
            Assert.AreEqual(4, tree.MaterialCapacity(AntRole.Carrier));
            Assert.AreEqual(2, tree.MaterialCapacity(AntRole.Cutter));
            Assert.AreEqual(4, tree.MaterialCapacity(AntRole.Digger));
            Assert.AreEqual(3, tree.SoilCapacity(AntRole.Worker));
            Assert.AreEqual(6, tree.SoilCapacity(AntRole.Digger));

            tree.Restore(Mask(QueenNode.O1, QueenNode.L1, QueenNode.E1, QueenNode.F1));
            Assert.AreEqual(3, tree.MaterialCapacity(AntRole.Worker), "O1: 2 → 3 UM.");
            Assert.AreEqual(4, tree.SoilCapacity(AntRole.Worker), "O1: +50% de terra.");
            Assert.AreEqual(6, tree.MaterialCapacity(AntRole.Carrier), "L1: 4 → 6 UM.");
            Assert.AreEqual(9, tree.SoilCapacity(AntRole.Digger), "E1: 64 → 96 células, na escala do protótipo.");
            Assert.AreEqual(2, tree.MaterialCapacity(AntRole.Cutter), "F1 muda o corte, não a carga.");
            Assert.AreEqual(0.75f, tree.CutTimeFactor(AntRole.Cutter), "F1: 4 UM em 1,5 s contra 2 UM em 1 s.");
            Assert.AreEqual(1f, tree.CutTimeFactor(AntRole.Worker));
        }

        [Test]
        public void Ants_CarryUpToTheirCapacityPerTrip()
        {
            var (grid, layout) = MakeWorld(CellMaterial.Dirt);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 3);
            var sim = new GranularSimulation();
            int most = 0;
            for (int i = 0; i < 60 * 240 && colony.TotalDelivered < 12; i++)
            {
                sim.Step(grid);
                colony.Tick(Dt);
                foreach (Ant ant in colony.Ants)
                    most = Mathf.Max(most, ant.CarriedCount);
            }

            Assert.AreEqual(12, colony.TotalDelivered, "Cada pixel levado conta uma entrega.");
            Assert.AreEqual(3, most, "A comum leva até 3 pixels de terra por viagem (nunca mais).");
        }

        [Test]
        public void Rock_StaysUntilADiggerWithE2Exists()
        {
            var (grid, layout) = MakeWorld(CellMaterial.Rock);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 3);
            var sim = new GranularSimulation();
            Run(colony, grid, sim, 60f);
            Assert.AreEqual(12, grid.MarkedCount, "Comuns não quebram rocha; a marca espera.");
            Assert.AreEqual(CellMaterial.Rock, grid.Get(4, 1));

            colony.Tree.Restore(Mask(QueenNode.E0, QueenNode.E1, QueenNode.E2));
            Run(colony, grid, sim, 60f);
            Assert.AreEqual(12, grid.MarkedCount, "Com E2 mas sem escavadora, nada muda.");

            colony.Spawn(AntRole.Digger);
            for (int i = 0; i < 60 * 600 && (grid.MarkedCount > 0 || colony.TotalDelivered < 12); i++)
            {
                sim.Step(grid);
                colony.Tick(Dt);
            }
            Assert.AreEqual(0, grid.MarkedCount, "A escavadora com E2 quebra toda a rocha marcada.");
            Assert.AreEqual(12, colony.TotalDelivered, "Os fragmentos seguem para o despejo, um por viagem.");
        }

        [Test]
        public void Rock_WithoutE2_IsNotBrokenEvenWithADigger()
        {
            var (grid, layout) = MakeWorld(CellMaterial.Rock);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 3);
            colony.Spawn(AntRole.Digger);
            Run(colony, grid, new GranularSimulation(), 90f);
            Assert.AreEqual(12, grid.MarkedCount);
        }

        [Test]
        public void Diggers_DoNotCarryEggsOrNutrients_ButCommonsAndCarriersDo()
        {
            Assert.IsFalse(AntRoles.FetchesNutrients(AntRole.Digger));
            Assert.IsFalse(AntRoles.CarriesEggs(AntRole.Digger));
            Assert.IsTrue(AntRoles.FetchesNutrients(AntRole.Worker) && AntRoles.FetchesNutrients(AntRole.Carrier));
            Assert.IsTrue(AntRoles.CarriesEggs(AntRole.Worker) && AntRoles.CarriesEggs(AntRole.Carrier));
        }

        static void Run(AntColony colony, PixelGrid grid, GranularSimulation sim, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                sim.Step(grid);
                colony.Tick(Dt);
            }
        }
    }
}
