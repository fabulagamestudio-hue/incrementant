using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Formiga sem trabalho do próprio papel faz o da comum (pedido do desenvolvedor em 2026-10-19): soldado sem combate,
    /// cortadeira sem corte, cuidadora sem cria... O trabalho do papel continua vindo antes.
    /// </summary>
    public sealed class RoleFallbackTests
    {
        const float Dt = 1f / 60f;
        const int Surface = 10;

        /// <summary>Solo de terra até <see cref="Surface"/>, piso de rocha, corredor (y 1..3) de x = 2 a 77 e poço em x = 20.</summary>
        static PixelGrid MakeWorld()
        {
            var grid = new PixelGrid(80, 30);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, Surface);
                for (int y = 1; y <= Surface; y++)
                    grid.Set(x, y, y == Surface ? CellMaterial.Grass : CellMaterial.Dirt);
            }
            for (int x = 2; x < grid.Width - 2; x++)
            for (int y = 1; y <= 3; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int y = 1; y <= Surface; y++)
                grid.Set(20, y, CellMaterial.Empty);
            return grid;
        }

        static TerrainLayout Layout => new TerrainLayout(20, new Vector2Int(20, 1), 50, 55);

        static bool IsDigWork(AntState state) => state == AntState.Digging || state == AntState.Carrying;

        [Test]
        public void IdleSpecialists_DoTheWorkersJob()
        {
            PixelGrid grid = MakeWorld();
            for (int x = 40; x <= 50; x++)
            for (int y = 4; y <= 5; y++)
                grid.SetMarked(x, y, true);
            int marked = grid.MarkedCount;
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Soldier;
            colony.Spawn(AntRole.Cutter);
            colony.Spawn(AntRole.Nurse);

            bool soldierDug = false, cutterDug = false, nurseDug = false;
            for (int i = 0; i < 60 * 40; i++)
            {
                colony.Tick(Dt);
                foreach (Ant ant in colony.Ants)
                {
                    if (!IsDigWork(ant.State))
                        continue;
                    Assert.IsTrue(ant.ActingAsWorker, $"{ant.Role} só cava fazendo o trabalho da comum.");
                    soldierDug |= ant.Role == AntRole.Soldier;
                    cutterDug |= ant.Role == AntRole.Cutter;
                    nurseDug |= ant.Role == AntRole.Nurse;
                }
            }

            Assert.IsTrue(soldierDug, "Soldado sem combate cava como a comum.");
            Assert.IsTrue(cutterDug, "Cortadeira sem planta-alvo cava como a comum.");
            Assert.IsTrue(nurseDug, "Cuidadora sem cria cava como a comum.");
            Assert.Less(grid.MarkedCount, marked, "A terra marcada foi cavada.");
        }

        [Test]
        public void OwnRoleWork_StaysTheRolesOwn()
        {
            PixelGrid grid = MakeWorld();
            for (int x = 40; x <= 50; x++)
            for (int y = 4; y <= 5; y++)
                grid.SetMarked(x, y, true);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Digger;

            bool dug = false;
            for (int i = 0; i < 60 * 20; i++)
            {
                colony.Tick(Dt);
                foreach (Ant ant in colony.Ants)
                {
                    if (!IsDigWork(ant.State))
                        continue;
                    dug = true;
                    Assert.IsFalse(ant.ActingAsWorker, "Cavar é o trabalho da escavadora: ela não está emprestada à comum.");
                }
            }
            Assert.IsTrue(dug);
        }

        [Test]
        public void WithNothingToDo_SpecialistsJustWander_AndKeepTheirRole()
        {
            PixelGrid grid = MakeWorld();
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 3);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Soldier;
            for (int i = 0; i < 60 * 10; i++)
                colony.Tick(Dt);
            foreach (Ant ant in colony.Ants)
            {
                Assert.AreEqual(AntRole.Soldier, ant.Role, "Fazer o trabalho da comum não muda o papel.");
                Assert.AreEqual(AntState.Idle, ant.State);
                Assert.IsFalse(ant.ActingAsWorker, "Sem trabalho nenhum, não fica emprestada à comum.");
            }
        }
    }
}
