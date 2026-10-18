using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary><see cref="AntColony.DeferFieldRefresh"/>: a rodada de campos espera o último passo do frame.</summary>
    public sealed class FieldRefreshDeferralTests
    {
        const float Dt = 1f / 60f;

        static (PixelGrid grid, AntColony colony) Make()
        {
            var grid = new PixelGrid(90, 16);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Bedrock);
                grid.SetSurfaceY(x, 0);
            }
            var layout = new TerrainLayout(45, new Vector2Int(45, 1), 55, 60);
            return (grid, new AntColony(grid, new ColonySettings(), layout, seed: 3));
        }

        [Test]
        public void DeferredTicks_DoNotRebuildFields_UntilTheLastTickOfTheFrame()
        {
            var (grid, colony) = Make();
            colony.Tick(Dt); // a primeira rodada nunca é adiada
            int rounds = colony.FieldRounds;
            Assert.GreaterOrEqual(rounds, 1);

            colony.DeferFieldRefresh = true;
            for (int x = 20; x < 30; x++)
                grid.Set(x, 1, CellMaterial.Dirt); // muda o terreno: pede rodada
            for (int i = 0; i < 120; i++)
                colony.Tick(Dt);
            Assert.AreEqual(rounds, colony.FieldRounds, "Adiado, nenhuma rodada acontece, por mais passos que rodem.");

            colony.DeferFieldRefresh = false;
            colony.Tick(Dt);
            Assert.AreEqual(rounds + 1, colony.FieldRounds, "No último passo do frame a rodada acontece, uma só.");
        }

        [Test]
        public void WithoutDeferral_RoundsFollowTheInterval()
        {
            var (grid, colony) = Make();
            colony.Tick(Dt);
            int rounds = colony.FieldRounds;
            for (int i = 0; i < 120; i++)
            {
                grid.Set(20 + i % 30, 1, i % 2 == 0 ? CellMaterial.Dirt : CellMaterial.Empty);
                colony.Tick(Dt);
            }
            Assert.Greater(colony.FieldRounds, rounds + 2, "Sem adiar, o terreno mudando refaz os campos a cada intervalo.");
        }
    }
}
