using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Desempate da descida pelo campo (<see cref="AntNavigation.Downhill"/>): o campo dá custo 1 a todo vizinho e sortear
    /// os empates a cada passo fazia a formiga andar em zigue-zague. O caminho sai reto e só vira diagonal quando precisa.
    /// </summary>
    public sealed class DownhillTieBreakTests
    {
        const int Seeds = 20;

        /// <summary>Terra até y = 25 com uma câmara aberta de x 5 a 55, y 5 a 20.</summary>
        static PixelGrid MakeChamber()
        {
            var grid = new PixelGrid(60, 30);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.SetSurfaceY(x, 25);
                for (int y = 0; y <= 25; y++)
                    grid.Set(x, y, x >= 5 && x <= 55 && y >= 5 && y <= 20 ? CellMaterial.Empty : CellMaterial.Dirt);
            }
            return grid;
        }

        /// <summary>Desce do início até a origem; devolve as direções de cada passo.</summary>
        static List<int> Walk(PixelGrid grid, FlowField field, int seed, int x, int y, int previous)
        {
            var rng = new System.Random(seed);
            var moves = new List<int>();
            while (field.Get(x, y) > 0)
            {
                int k = AntNavigation.Downhill(grid, field, rng, x, y, previous);
                Assert.GreaterOrEqual(k, 0, "Câmara aberta: sempre há um vizinho que desce.");
                x += AntNavigation.Dx[k];
                y += AntNavigation.Dy[k];
                moves.Add(k);
                previous = k;
                Assert.Less(moves.Count, 200, "A descida não pode andar em círculo.");
            }
            return moves;
        }

        static int Turns(List<int> moves)
        {
            int turns = 0;
            for (int i = 1; i < moves.Count; i++)
                turns += moves[i] != moves[i - 1] ? 1 : 0;
            return turns;
        }

        [Test]
        public void TargetStraightAhead_WalksInAStraightLine()
        {
            PixelGrid grid = MakeChamber();
            var field = new FlowField(grid.Width, grid.Height);
            field.Build(grid, new List<int> { grid.ToIndex(45, 12) });
            for (int seed = 0; seed < Seeds; seed++)
            {
                List<int> moves = Walk(grid, field, seed, 10, 12, -1);
                Assert.AreEqual(35, moves.Count, $"seed {seed}");
                Assert.AreEqual(0, Turns(moves), $"seed {seed}: alvo à frente, nenhum desvio para cima ou para baixo.");
            }
        }

        [Test]
        public void ComingInDiagonally_StraightensOut_InsteadOfAV()
        {
            // Vinha na diagonal (nordeste) e o alvo está à frente: segue reto, não sobe e volta.
            PixelGrid grid = MakeChamber();
            var field = new FlowField(grid.Width, grid.Height);
            field.Build(grid, new List<int> { grid.ToIndex(45, 12) });
            for (int seed = 0; seed < Seeds; seed++)
            {
                List<int> moves = Walk(grid, field, seed, 10, 12, 4);
                Assert.AreEqual(0, Turns(moves), $"seed {seed}");
                Assert.AreEqual(0, moves[0], $"seed {seed}: o primeiro passo já é para a direita.");
            }
        }

        [Test]
        public void TargetOffAxis_GoesStraightThenDiagonal_TurningOnce()
        {
            PixelGrid grid = MakeChamber();
            var field = new FlowField(grid.Width, grid.Height);
            field.Build(grid, new List<int> { grid.ToIndex(45, 12) });
            for (int seed = 0; seed < Seeds; seed++)
            {
                List<int> moves = Walk(grid, field, seed, 10, 5, -1);
                Assert.AreEqual(35, moves.Count, $"seed {seed}: caminho mínimo.");
                Assert.AreEqual(1, Turns(moves), $"seed {seed}: um trecho reto e um diagonal.");
                Assert.AreEqual(7, moves.FindAll(k => k >= 4).Count, $"seed {seed}: só as diagonais que precisa.");
            }
        }
    }
}
