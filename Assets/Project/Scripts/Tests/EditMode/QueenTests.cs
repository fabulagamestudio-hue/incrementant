using System.IO;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    public sealed class QueenTests
    {
        const float Dt = 1f / 60f;

        /// <summary>
        /// Solo maciço de terra até <paramref name="surface"/> (inclusive), piso de rocha em y = 0 e um corredor
        /// subterrâneo (y 1..3) de x = 2 até a largura − 3.
        /// </summary>
        static PixelGrid MakeCorridor(int width, int height, int surface)
        {
            var grid = new PixelGrid(width, height);
            for (int x = 0; x < width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, surface);
                for (int y = 1; y <= surface; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int x = 2; x < width - 2; x++)
            for (int y = 1; y <= 3; y++)
                grid.Set(x, y, CellMaterial.Empty);
            return grid;
        }

        static void PaintRoom(PixelGrid grid, int xMin, int xMax, int yMin, int yMax)
        {
            for (int x = xMin; x <= xMax; x++)
            for (int y = yMin; y <= yMax; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true), $"({x},{y}) deveria aceitar a sala da rainha.");
        }

        [Test]
        public void QueenRoom_OnlyAcceptsAnthillCells_SolidOrNot()
        {
            PixelGrid grid = MakeCorridor(20, 12, 6);
            int version = grid.MarkVersion;

            Assert.IsTrue(grid.SetQueenRoom(5, 2, true), "Túnel aceita.");
            Assert.IsTrue(grid.SetQueenRoom(5, 5, true), "Terra ainda maciça aceita (pinta antes de cavar).");
            Assert.IsFalse(grid.SetQueenRoom(5, 9, true), "O céu acima do solo original não é sala.");
            Assert.AreEqual(2, grid.QueenRoomCount);
            Assert.AreNotEqual(version, grid.MarkVersion, "Pintar a sala muda a versão das marcas (recalcula os campos).");

            grid.Set(5, 5, CellMaterial.Empty);
            Assert.IsTrue(grid.IsQueenRoom(5, 5), "A sala vale o lugar: cavar não apaga a pintura.");
            Assert.IsTrue(grid.SetQueenRoom(5, 2, false));
            Assert.AreEqual(1, grid.QueenRoomCount);
        }

        [Test]
        public void Queen_WalksToTheLargestRoom_AndNeverLeavesIt()
        {
            PixelGrid grid = MakeCorridor(60, 12, 11);
            PaintRoom(grid, 5, 6, 1, 3);    // 6 células
            PaintRoom(grid, 40, 46, 1, 3);  // 21 células
            PaintRoom(grid, 40, 46, 5, 6);  // ainda maciça: só conta depois de cavada

            var layout = new TerrainLayout(20, new Vector2Int(20, 1), 30, 35);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 3);
            Assert.AreEqual(20, colony.Queen.X);

            for (int i = 0; i < 60 * 20 && !colony.QueenInRoom; i++)
                colony.Tick(Dt);

            Assert.IsTrue(colony.QueenInRoom, "A rainha deveria ter chegado à sala.");
            Assert.AreEqual(21, colony.QueenRoomSize, "Fica na maior sala; a parte maciça não conta.");
            int minX = int.MaxValue, maxX = int.MinValue;
            for (int i = 0; i < 60 * 30; i++)
            {
                colony.Tick(Dt);
                Ant queen = colony.Queen;
                Assert.IsTrue(colony.IsQueenArea(queen.X, queen.Y), $"A rainha saiu da sala: ({queen.X},{queen.Y}).");
                minX = Mathf.Min(minX, queen.X);
                maxX = Mathf.Max(maxX, queen.X);
            }
            Assert.GreaterOrEqual(minX, 40);
            Assert.LessOrEqual(maxX, 46);
            Assert.Greater(maxX - minX, 1, "Dentro da sala ela anda, não fica parada.");
        }

        [Test]
        public void Queen_WalksAtWorkerSpeed()
        {
            // 20 células até a sala: na velocidade antiga da rainha (4 células/s) eram 5 s; como operária (20/s), cerca de 1 s.
            PixelGrid grid = MakeCorridor(60, 12, 11);
            PaintRoom(grid, 40, 46, 1, 3);
            var layout = new TerrainLayout(20, new Vector2Int(20, 1), 30, 35);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 3);

            float t = 0f;
            for (; t < 10f && !colony.QueenInRoom; t += Dt)
                colony.Tick(Dt);
            Assert.IsTrue(colony.QueenInRoom);
            Assert.Less(t, 2.5f, "A rainha anda na velocidade da operária.");
        }

        [Test]
        public void QueenWithoutARoom_WandersTheAnthill_WithoutGoingOutside()
        {
            // Sem sala da rainha, ela não fica parada: anda pelo formigueiro como uma formiga ociosa, sem sair pelo poço.
            const int surface = 10;
            PixelGrid grid = MakeCorridor(80, 20, surface);
            for (int y = 1; y <= surface; y++)
                grid.Set(40, y, CellMaterial.Empty);
            var layout = new TerrainLayout(40, new Vector2Int(40, 1), 50, 60);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 9);

            int farthest = 0;
            for (int i = 0; i < 60 * 30; i++)
            {
                colony.Tick(Dt);
                Ant queen = colony.Queen;
                Assert.LessOrEqual(queen.Y, surface, $"A rainha saiu do formigueiro: ({queen.X},{queen.Y}).");
                farthest = Mathf.Max(farthest, Mathf.Abs(queen.X - 40));
            }
            Assert.Greater(farthest, 10, "Sem sala, a rainha anda pelo formigueiro.");
        }

        [Test]
        public void Queen_MovesToANewLargerRoom_WhenItIsDug()
        {
            PixelGrid grid = MakeCorridor(60, 12, 11);
            PaintRoom(grid, 10, 14, 1, 3);  // 15 células
            var layout = new TerrainLayout(12, new Vector2Int(12, 2), 30, 35);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 5);
            for (int i = 0; i < 60 * 2; i++)
                colony.Tick(Dt);
            Assert.IsTrue(colony.QueenInRoom);

            // Sala maior, mais longe, pintada sobre o corredor: a rainha troca de sala.
            PaintRoom(grid, 40, 50, 1, 3);
            for (int i = 0; i < 60 * 20 && colony.Queen.X < 40; i++)
                colony.Tick(Dt);
            for (int i = 0; i < 60 * 2; i++)
                colony.Tick(Dt);

            Assert.AreEqual(33, colony.QueenRoomSize);
            Assert.IsTrue(colony.QueenInRoom);
            Assert.GreaterOrEqual(colony.Queen.X, 40);
        }

        [Test]
        public void IdleAnts_WanderTheWholeAnthillConnectedToTheQueen_WithoutGoingOutside()
        {
            // Corredor longo com um poço até a superfície no meio (x = 40). Sem trabalho, as formigas se espalham pelo
            // corredor inteiro, mas não saem para o céu pelo poço.
            const int surface = 10;
            PixelGrid grid = MakeCorridor(80, 20, surface);
            for (int y = 1; y <= surface; y++)
                grid.Set(40, y, CellMaterial.Empty);

            var layout = new TerrainLayout(40, new Vector2Int(40, 1), 50, 60);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 9);
            int farthest = 0;
            for (int i = 0; i < 60 * 30; i++)
            {
                colony.Tick(Dt);
                foreach (Ant ant in colony.Ants)
                {
                    Assert.LessOrEqual(ant.Y, surface, $"Formiga ociosa saiu do formigueiro: ({ant.X},{ant.Y}).");
                    farthest = Mathf.Max(farthest, Mathf.Abs(ant.X - 40));
                }
            }
            Assert.Greater(farthest, 25, "As ociosas deveriam ir longe da rainha, pelo formigueiro todo.");
        }

        [Test]
        public void IdleAnts_OutsideTheAnthill_ComeBackInside()
        {
            const int surface = 10;
            PixelGrid grid = MakeCorridor(80, 20, surface);
            for (int y = 1; y <= surface; y++)
                grid.Set(40, y, CellMaterial.Empty);
            var layout = new TerrainLayout(40, new Vector2Int(40, 1), 50, 60);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 4);
            // Uma formiga ociosa no chão lá fora, longe da abertura.
            Ant outside = colony.Ants[0];
            outside.X = 60;
            outside.Y = surface + 1;
            outside.FromX = outside.X;
            outside.FromY = outside.Y;

            for (int i = 0; i < 60 * 10 && outside.Y > surface; i++)
                colony.Tick(Dt);
            Assert.LessOrEqual(outside.Y, surface, "A formiga de fora deveria voltar para o formigueiro.");
        }

        [Test]
        public void Snapshot_RoundTripsQueenAndRoom()
        {
            PixelGrid grid = MakeCorridor(60, 12, 11);
            PaintRoom(grid, 40, 46, 1, 3);
            var layout = new TerrainLayout(20, new Vector2Int(20, 1), 30, 35);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 3);
            for (int i = 0; i < 60 * 20 && !colony.QueenInRoom; i++)
                colony.Tick(Dt);
            Assert.IsTrue(colony.QueenInRoom);

            var rain = new RainSimulation(new RainSettings(), 3);
            var water = new WaterSimulation(new WaterSettings(), grid, 3);
            GameSnapshot saved = GameSnapshot.Capture(grid, colony, rain, water, 0, 3, 10.0);
            var stream = new MemoryStream();
            saved.Write(stream);
            stream.Position = 0;
            GameSnapshot loaded = GameSnapshot.Read(stream);

            PixelGrid grid2 = MakeCorridor(60, 12, 11);
            loaded.ApplyTo(grid2);
            var colony2 = new AntColony(grid2, new ColonySettings(), layout, seed: 3);
            loaded.ApplyTo(colony2);

            Assert.AreEqual(grid.QueenRoomCount, grid2.QueenRoomCount);
            Assert.AreEqual(colony.Queen.X, colony2.Queen.X);
            Assert.AreEqual(colony.Queen.Y, colony2.Queen.Y);
            colony2.Tick(Dt);
            Assert.IsTrue(colony2.QueenInRoom, "Depois de carregar, a rainha continua na sala.");
        }
    }
}
