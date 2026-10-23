using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Rachadura dos blocos golpeados (REQ-072): dano na célula, estágios e golpes das formigas.</summary>
    public sealed class BlockCrackTests
    {
        const float Dt = 1f / 60f;

        [Test]
        public void Damage_GrowsInStages_NeverShrinks_AndOnlyOnSolidCells()
        {
            var grid = new PixelGrid(8, 8);
            grid.Set(3, 3, CellMaterial.Dirt);

            Assert.AreEqual(0, grid.GetCrackStage(3, 3));
            Assert.IsTrue(grid.SetDamage(3, 3, 0.1f));
            Assert.AreEqual(0, grid.GetCrackStage(3, 3), "Abaixo de 20% ainda não aparece rachadura.");
            grid.SetDamage(3, 3, 0.25f);
            Assert.AreEqual(1, grid.GetCrackStage(3, 3));
            grid.SetDamage(3, 3, 0.85f);
            Assert.AreEqual(PixelGrid.CrackStages, grid.GetCrackStage(3, 3));
            Assert.IsFalse(grid.SetDamage(3, 3, 0.3f), "O dano não diminui.");
            Assert.AreEqual(0.85f, grid.GetDamage(3, 3), 0.01f);

            Assert.IsFalse(grid.SetDamage(4, 4, 0.5f), "Célula vazia não tem dano.");
            Assert.AreEqual(1, grid.DamagedCount);
        }

        [Test]
        public void Damage_GoesAway_WhenTheMaterialChanges_OrTheGrainFalls()
        {
            var grid = new PixelGrid(8, 8);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Bedrock);
                grid.SetSurfaceY(x, 7);
            }
            grid.Set(2, 1, CellMaterial.Dirt);
            grid.SetDamage(2, 1, 0.6f);
            grid.Set(2, 1, CellMaterial.Mud);
            Assert.AreEqual(0f, grid.GetDamage(2, 1), "Trocar o material tira a rachadura.");

            grid.Set(5, 4, CellMaterial.Sand);
            grid.SetDamage(5, 4, 0.6f);
            var sim = new GranularSimulation();
            for (int i = 0; i < 20; i++)
                sim.Step(grid);
            Assert.AreEqual(CellMaterial.Empty, grid.Get(5, 4), "A areia caiu.");
            Assert.AreEqual(0, grid.DamagedCount, "O grão que cai não leva a rachadura.");
        }

        [Test]
        public void CrackArt_EachStageContainsThePreviousOne()
        {
            Assert.AreEqual(PixelGrid.CrackStages, CrackArt.StageCount);
            int size = CellTileArt.TexelsPerCell;
            int previous = 0;
            for (int stage = 1; stage <= CrackArt.StageCount; stage++)
            {
                int count = 0;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    if (CrackArt.IsCracked(stage, x, y))
                        count++;
                    else if (stage > 1)
                        Assert.IsFalse(CrackArt.IsCracked(stage - 1, x, y), $"Estágio {stage} perde o texel ({x}, {y}) do anterior.");
                }
                Assert.Greater(count, previous, $"O estágio {stage} racha mais que o anterior.");
                previous = count;
            }
        }

        /// <summary>Chão de pedra e uma faixa de <paramref name="material"/> marcada em x 4..9, y 1..2.</summary>
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

        static int CellsLeft(PixelGrid grid, CellMaterial material)
        {
            int count = 0;
            for (int x = 4; x < 10; x++)
            for (int y = 1; y <= 2; y++)
                if (grid.Get(x, y) == material)
                    count++;
            return count;
        }

        [Test]
        public void DiggingOre_CracksTheBlockStageByStage_BeforeItComesOut()
        {
            var (grid, layout) = MakeWorld(CellMaterial.IronOre);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 3);
            var sim = new GranularSimulation();
            int highest = 0;
            bool decreasedWhileStanding = false;
            var last = new int[grid.Width * grid.Height];
            for (int i = 0; i < 60 * 120 && CellsLeft(grid, CellMaterial.IronOre) > 0; i++)
            {
                sim.Step(grid);
                colony.Tick(Dt);
                for (int x = 4; x < 10; x++)
                for (int y = 1; y <= 2; y++)
                {
                    int stage = grid.GetCrackStage(x, y);
                    int index = y * grid.Width + x;
                    if (grid.Get(x, y) == CellMaterial.IronOre && stage < last[index])
                        decreasedWhileStanding = true;
                    last[index] = stage;
                    highest = Mathf.Max(highest, stage);
                }
            }

            Assert.AreEqual(0, CellsLeft(grid, CellMaterial.IronOre), "O veio foi todo cavado.");
            Assert.AreEqual(PixelGrid.CrackStages, highest, "O minério (0,8 s por bloco) chega à última rachadura antes de sair.");
            Assert.IsFalse(decreasedWhileStanding, "A rachadura de um bloco em pé nunca diminui.");
            Assert.AreEqual(0, grid.DamagedCount, "Bloco cavado não deixa rachadura no vão.");
        }

        [Test]
        public void ADamagedBlock_ComesOutSooner_ThanAnIntactOne()
        {
            float intact = FirstBlockOut(0f);
            float damaged = FirstBlockOut(0.75f);
            Assert.Less(damaged, intact, "O dano deixado por outra formiga desconta o tempo de quem continua.");
        }

        static float FirstBlockOut(float damage)
        {
            var (grid, layout) = MakeWorld(CellMaterial.IronOre);
            for (int x = 4; x < 10; x++)
            for (int y = 1; y <= 2; y++)
                grid.SetDamage(x, y, damage);
            var colony = new AntColony(grid, new ColonySettings(), layout, seed: 3);
            var sim = new GranularSimulation();
            for (int i = 0; i < 60 * 60; i++)
            {
                sim.Step(grid);
                colony.Tick(Dt);
                if (CellsLeft(grid, CellMaterial.IronOre) < 12)
                    return i * Dt;
            }
            Assert.Fail("Nenhum bloco saiu em 60 s.");
            return 0f;
        }
    }
}
