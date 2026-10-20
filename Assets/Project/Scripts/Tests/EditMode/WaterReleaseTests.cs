using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Liberação de blocos de água por vizinhança (pedido do desenvolvedor em 2026-10-19): o bloco reservado por uma formiga
    /// já conta como fora do caminho, então os vizinhos dele, ainda obstruídos, podem ser reservados por outras; se a
    /// formiga do bloco obstruído chega antes, ela pega o bloco do meio e passa o dela para a outra formiga.
    /// </summary>
    public sealed class WaterReleaseTests
    {
        const float Dt = 0.1f;
        const int Floor = 6;
        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, Floor + 1), 60, 70);

        /// <summary>
        /// Solo de terra até y = 20, piso de rocha em y = 0, corredor de x 10 a 100 (y 6 a 8), poço da entrada em x 50..51,
        /// depósito de água em x 20..26, sala da rainha em x 88..98 e um buraco cheio de água de x
        /// <paramref name="wellMinX"/> a <paramref name="wellMaxX"/> e de y <paramref name="wellBottom"/> a 5, embaixo do
        /// corredor: só a linha de cima tem célula seca ao lado.
        /// </summary>
        static PixelGrid MakeWorld(int wellMinX, int wellMaxX, int wellBottom)
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
            for (int y = Floor; y <= Floor + 2; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 50; x <= 51; x++)
            for (int y = Floor + 3; y <= 20; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 20; x <= 26; x++)
            for (int y = Floor; y <= Floor + 2; y++)
                Assert.IsTrue(grid.SetStorageRoom(x, y, StorageKind.Water, true));
            for (int x = 88; x <= 98; x++)
            for (int y = Floor; y <= Floor + 2; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true));
            for (int x = wellMinX; x <= wellMaxX; x++)
            for (int y = wellBottom; y < Floor; y++)
            {
                grid.Set(x, y, CellMaterial.Empty);
                Assert.IsTrue(grid.SetWater(x, y, true));
            }
            return grid;
        }

        static AntColony MakeColony(PixelGrid grid)
        {
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 5) { RaidsEnabled = false };
            colony.RestoreQueen(95, Floor + 1, 1);
            return colony;
        }

        /// <summary>
        /// Poço de 3 × 5 blocos com só 3 expostos: com a liberação, mais de 3 formigas saem ao mesmo tempo, cada uma com o
        /// seu bloco (nunca duas no mesmo), e o poço todo vai para o depósito.
        /// </summary>
        [Test]
        public void DeepPool_ReleasesTheBlocksUnderTheReservedOnes()
        {
            PixelGrid grid = MakeWorld(60, 62, 1);
            Assert.AreEqual(15, grid.WaterCount);
            AntColony colony = MakeColony(grid);
            for (int i = 0; i < 8; i++)
                colony.Spawn(AntRole.Worker);

            var taken = new HashSet<int>();
            int maxReserved = 0;
            bool sawRelease = false;
            for (float t = 0f; t < 300f && grid.StoredCount(StorageKind.Water) < 15; t += Dt)
            {
                colony.Tick(Dt);
                taken.Clear();
                int reserved = 0;
                foreach (Ant ant in colony.Ants)
                {
                    if (ant.State != AntState.FetchingWater || ant.SeekTarget < 0)
                        continue;
                    reserved++;
                    sawRelease |= ant.WaterVia >= 0;
                    Assert.IsTrue(taken.Add(ant.SeekTarget), $"Duas formigas reservaram o mesmo bloco de água em t = {t:0.0} s.");
                }
                maxReserved = Mathf.Max(maxReserved, reserved);
            }

            Assert.IsTrue(sawRelease, "Alguma formiga reservou um bloco ainda obstruído, chegando pelo bloco reservado do lado.");
            Assert.Greater(maxReserved, 3, "Mais formigas que blocos expostos saíram para buscar água ao mesmo tempo.");
            Assert.AreEqual(15, grid.StoredCount(StorageKind.Water), "O poço todo foi para o depósito de água.");
            Assert.AreEqual(0, grid.WaterCount);
        }

        /// <summary>
        /// A formiga do bloco obstruído chega primeiro: pega o bloco do meio, ainda reservado pela outra, e a outra fica com
        /// o bloco de baixo, chegando pelo lugar do bloco do meio.
        /// </summary>
        [Test]
        public void AntOfTheBlockedBlock_ArrivingFirst_TakesTheFreeOne_AndHandsOverItsTarget()
        {
            PixelGrid grid = MakeWorld(60, 60, 4);
            int middle = grid.ToIndex(60, 5), below = grid.ToIndex(60, 4);
            AntColony colony = MakeColony(grid);

            // A formiga do bloco do meio ainda está longe, a caminho; a do bloco de baixo já está ao lado do bloco do meio.
            Ant far = colony.SpawnAt(AntRole.Worker, 52, Floor);
            far.State = AntState.FetchingWater;
            far.StoreTarget = StorageKind.Water;
            far.BeginSeekPath();
            for (int x = 53; x <= 59; x++)
                far.AddSeekStep(grid.ToIndex(x, Floor));
            far.SeekTarget = middle;

            Ant near = colony.SpawnAt(AntRole.Worker, 60, Floor);
            near.State = AntState.FetchingWater;
            near.StoreTarget = StorageKind.Water;
            near.BeginSeekPath();
            near.SeekTarget = below;
            near.WaterVia = middle;

            for (int i = 0; i < 50 && near.State != AntState.CarryingWater; i++)
                colony.Tick(Dt);

            Assert.AreEqual(AntState.CarryingWater, near.State, "A formiga que chegou primeiro pegou o bloco do meio.");
            Assert.IsFalse(grid.HasWater(60, 5));
            Assert.AreEqual(AntState.FetchingWater, far.State);
            Assert.AreEqual(below, far.SeekTarget, "A outra formiga ficou com o bloco de baixo.");
            Assert.AreEqual(middle, far.WaterVia, "E chega a ele pelo lugar do bloco do meio.");

            for (float t = 0f; t < 120f && grid.StoredCount(StorageKind.Water) < 2; t += Dt)
                colony.Tick(Dt);
            Assert.AreEqual(0, grid.WaterCount, "A outra formiga entrou no lugar do bloco do meio e tirou o de baixo.");
            Assert.AreEqual(2, grid.StoredCount(StorageKind.Water), "Os dois blocos foram para o depósito.");
        }
    }
}
