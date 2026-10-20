using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Soterramento e resgate (KI-025, 2026-10-20): a formiga presa fica parada sem colisão em vez de subir.</summary>
    public sealed class BurialTests
    {
        const float Dt = 0.1f;
        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);

        /// <summary>Solo de terra até y = 20 com um corredor de x 10 a 100 (y 1 a 3) e a sala da rainha em x 88..98.</summary>
        static PixelGrid MakeWorld()
        {
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
            for (int x = 88; x <= 98; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true));
            return grid;
        }

        static AntColony MakeColony(PixelGrid grid)
        {
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3) { RaidsEnabled = false };
            colony.RestoreQueen(95, 2, 1);
            Tick(colony, 1f);
            return colony;
        }

        static void Tick(AntColony colony, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
                colony.Tick(Dt);
        }

        static Ant PlaceAnt(AntColony colony, int index, int x, int y)
        {
            Ant ant = colony.Ants[index];
            ant.X = x;
            ant.Y = y;
            ant.FromX = x;
            ant.FromY = y;
            ant.State = AntState.Idle;
            ant.Cooldown = 10f; // não anda sozinha
            return ant;
        }

        [Test]
        public void DirtOnAnAnt_LeavesItStuckInPlaceInsteadOfPushingItUp()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            Ant ant = PlaceAnt(colony, 0, 30, 2);

            grid.Set(30, 2, CellMaterial.Dirt);
            Tick(colony, 1f);

            Assert.IsTrue(ant.Buried);
            Assert.AreEqual(30, ant.X);
            Assert.AreEqual(2, ant.Y, "Sem colisão: não sobe pela terra.");
            Assert.AreEqual(AntState.Idle, ant.State);
            Assert.AreEqual(1, colony.BuriedCount);
        }

        [Test]
        public void ABuriedAnt_DropsItsLoad_AndFreesItselfWhenTheCellOpens()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            Ant ant = PlaceAnt(colony, 0, 30, 2);
            ant.PickUp(CellMaterial.Dirt, 0);

            grid.Set(30, 2, CellMaterial.Dirt);
            Tick(colony, 0.5f);
            Assert.IsTrue(ant.Buried);
            Assert.AreEqual(0, ant.CarriedCount, "A carga se perde.");

            grid.Set(30, 2, CellMaterial.Empty);
            Tick(colony, 0.5f);
            Assert.IsFalse(ant.Buried);
            Assert.AreEqual(0, colony.BuriedCount);
        }

        [Test]
        public void WithoutO3_NobodyRescuesTheBuriedAnt()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            Ant buried = PlaceAnt(colony, 0, 30, 2);
            grid.Set(30, 2, CellMaterial.Dirt);

            Tick(colony, 15f);

            Assert.IsTrue(buried.Buried);
        }

        [Test]
        public void WithO3_AWorkerDigsTheBuriedAntOut()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            colony.Tree.Grant(QueenNode.O3);
            Ant buried = PlaceAnt(colony, 0, 30, 2);
            grid.Set(30, 2, CellMaterial.Dirt);

            bool freed = false;
            for (float t = 0f; t < 60f && !freed; t += Dt)
            {
                colony.Tick(Dt);
                freed = !buried.Buried;
            }

            Assert.IsTrue(freed, "Uma operária ociosa tira a soterrada.");
            Assert.IsTrue(AntNavigation.IsWalkable(grid, buried.X, buried.Y), "Ela volta numa célula caminhável.");
            Assert.AreEqual(0, colony.BuriedCount);
        }
    }
}
