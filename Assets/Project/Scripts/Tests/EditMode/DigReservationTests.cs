using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Reserva do pixel marcado (pedido do desenvolvedor em 2026-10-18): a formiga que sai para buscar um pixel o reserva,
    /// e as outras vão a outro pixel livre em vez de correrem todas para o mesmo e voltarem quando ele é pego.
    /// </summary>
    public sealed class DigReservationTests
    {
        const float Dt = 1f / 60f;

        /// <summary>Chão de pedra, superfície no chão e <paramref name="count"/> pixels de terra marcados a partir de x = 4, y 1..2.</summary>
        static (PixelGrid grid, TerrainLayout layout) MakeWorld(int columns)
        {
            var grid = new PixelGrid(90, 16);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Bedrock);
                grid.SetSurfaceY(x, 0);
            }
            for (int x = 4; x < 4 + columns; x++)
            for (int y = 1; y <= 2; y++)
            {
                grid.Set(x, y, CellMaterial.Dirt);
                Assert.IsTrue(grid.SetMarked(x, y, true));
            }
            return (grid, new TerrainLayout(45, new Vector2Int(45, 1), 55, 60));
        }

        [Test]
        public void NoTwoAnts_ReserveOrDigTheSamePixel_AndAllIsDelivered()
        {
            var (grid, layout) = MakeWorld(6);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 5);
            for (int i = 0; i < 7; i++)
                colony.Spawn(AntRole.Worker);
            var sim = new GranularSimulation();
            var taken = new HashSet<int>();
            bool sawReservation = false;
            for (int i = 0; i < 60 * 300 && colony.TotalDelivered < 12; i++)
            {
                sim.Step(grid);
                colony.Tick(Dt);
                taken.Clear();
                foreach (Ant ant in colony.Ants)
                {
                    if (ant.SeekTarget >= 0)
                    {
                        sawReservation = true;
                        Assert.IsTrue(taken.Add(ant.SeekTarget), $"Duas formigas reservaram o mesmo pixel no passo {i}.");
                    }
                    else if (ant.State == AntState.Digging && grid.IsMarked(ant.DigX, ant.DigY))
                    {
                        Assert.IsTrue(taken.Add(grid.ToIndex(ant.DigX, ant.DigY)),
                            $"Pixel reservado ou cavado por duas formigas no passo {i}.");
                    }
                }
            }

            Assert.IsTrue(sawReservation, "As formigas devem reservar o pixel ao sair para buscá-lo.");
            Assert.AreEqual(12, colony.TotalDelivered, "Com reservas, todos os pixels marcados ainda são levados.");
        }

        [Test]
        public void SinglePixel_OnlyOneAntGoesForIt()
        {
            var (grid, layout) = MakeWorld(0);
            grid.Set(4, 1, CellMaterial.Dirt);
            Assert.IsTrue(grid.SetMarked(4, 1, true));
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 9);
            for (int i = 0; i < 7; i++)
                colony.Spawn(AntRole.Worker);
            var sim = new GranularSimulation();
            int target = grid.ToIndex(4, 1);
            for (int i = 0; i < 60 * 120 && colony.TotalDelivered < 1; i++)
            {
                sim.Step(grid);
                colony.Tick(Dt);
                int seekers = 0;
                foreach (Ant ant in colony.Ants)
                {
                    if (ant.SeekTarget == target || ant.State == AntState.Digging && grid.ToIndex(ant.DigX, ant.DigY) == target)
                        seekers++;
                }
                Assert.LessOrEqual(seekers, 1, $"Só uma formiga vai atrás do único pixel (passo {i}).");
            }
            Assert.AreEqual(1, colony.TotalDelivered);
        }
    }
}
