using Fabula.Incrementant.World;
using NUnit.Framework;

namespace Fabula.Incrementant.Tests
{
    public sealed class RainSimulationTests
    {
        const float Tick = 1f / 60f;

        /// <summary>Grade com piso de rocha em y = 0 e uma coluna de material em x = 2, de y = 1 até <paramref name="top"/>.</summary>
        static PixelGrid MakeColumnGrid(CellMaterial material, int top)
        {
            var grid = new PixelGrid(5, 30);
            for (int x = 0; x < grid.Width; x++)
                grid.Set(x, 0, CellMaterial.Rock);
            for (int y = 1; y <= top; y++)
                grid.Set(2, y, material);
            return grid;
        }

        /// <summary>As gotas caem em colunas sorteadas: os testes usam taxas altas e rodam até a coluna 2 receber o bastante.</summary>
        static RainSimulation Rain(Weather weather, RainSettings settings, int seed = 7) =>
            new RainSimulation(settings, seed) { Weather = weather };

        [Test]
        public void LightRain_WetsAndThenDissolvesSpoil()
        {
            PixelGrid grid = MakeColumnGrid(CellMaterial.Spoil, 3);
            var settings = new RainSettings { LightDropsPer100Columns = 2000f, LightHitsToDissolve = 3 };
            RainSimulation rain = Rain(Weather.LightRain, settings);

            bool sawWet = false;
            for (int i = 0; i < 600 && grid.Get(2, 1) != CellMaterial.Empty; i++)
            {
                rain.Step(grid, Tick);
                sawWet |= grid.GetWetness(2, 3) > 0 || grid.GetWetness(2, 2) > 0 || grid.GetWetness(2, 1) > 0;
            }

            Assert.IsTrue(sawWet, "A terra deveria ficar molhada antes de sumir.");
            Assert.AreEqual(CellMaterial.Empty, grid.Get(2, 1), "A coluna de terra despejada deveria se dissolver inteira.");
            Assert.AreEqual(3, rain.DissolvedCells);
            Assert.AreEqual(CellMaterial.Rock, grid.Get(2, 0), "A rocha não é afetada pela chuva leve.");
        }

        [Test]
        public void LightRain_DoesNotAffectNaturalDirtOrSand()
        {
            PixelGrid grid = MakeColumnGrid(CellMaterial.Dirt, 2);
            grid.Set(1, 1, CellMaterial.Sand);
            RainSimulation rain = Rain(Weather.LightRain, new RainSettings { LightDropsPer100Columns = 2000f });

            for (int i = 0; i < 300; i++)
                rain.Step(grid, Tick);

            Assert.AreEqual(CellMaterial.Dirt, grid.Get(2, 2));
            Assert.AreEqual(CellMaterial.Sand, grid.Get(1, 1));
            Assert.AreEqual(0, grid.WetCount);
        }

        [Test]
        public void HeavyRain_PacksGranularCellsSoTheyStopFalling()
        {
            PixelGrid grid = MakeColumnGrid(CellMaterial.Spoil, 4);
            RainSimulation rain = Rain(Weather.HeavyRain, new RainSettings { HeavyDropsPer100Columns = 2000f, HeavySoakDepth = 2 });

            for (int i = 0; i < 300 && grid.Get(2, 4) == CellMaterial.Spoil; i++)
                rain.Step(grid, Tick);

            Assert.AreEqual(CellMaterial.PackedSpoil, grid.Get(2, 4));
            Assert.AreEqual(CellMaterial.PackedSpoil, grid.Get(2, 3), "A gota encharca até HeavySoakDepth células.");
            Assert.AreEqual(CellMaterial.Spoil, grid.Get(2, 2), "Abaixo da crosta continua solto.");

            // A crosta compactada não cai: abre um vão embaixo dela e roda a física granular.
            grid.Set(2, 1, CellMaterial.Empty);
            grid.Set(2, 2, CellMaterial.Empty);
            var sim = new GranularSimulation();
            for (int i = 0; i < 10; i++)
                sim.Step(grid);
            Assert.AreEqual(CellMaterial.PackedSpoil, grid.Get(2, 4));
            Assert.AreEqual(CellMaterial.PackedSpoil, grid.Get(2, 3));
        }

        [Test]
        public void PackedMaterials_AreDiggableAndLoosenWhenDumped()
        {
            Assert.IsTrue(CellMaterials.IsDiggable(CellMaterial.PackedSpoil));
            Assert.IsTrue(CellMaterials.IsDiggable(CellMaterial.PackedSand));
            Assert.IsFalse(CellMaterials.IsGranular(CellMaterial.PackedSpoil));
            Assert.IsFalse(CellMaterials.IsGranular(CellMaterial.PackedSand));
            Assert.AreEqual(CellMaterial.Spoil, CellMaterials.ToDumped(CellMaterial.PackedSpoil));
            Assert.AreEqual(CellMaterial.Sand, CellMaterials.ToDumped(CellMaterial.PackedSand));
        }

        [Test]
        public void Wetness_FollowsFallingGrainAndDriesWhenRainStops()
        {
            var grid = new PixelGrid(5, 10);
            for (int x = 0; x < grid.Width; x++)
                grid.Set(x, 0, CellMaterial.Rock);
            grid.Set(2, 6, CellMaterial.Spoil);
            grid.SetWetness(2, 6, 2);

            var sim = new GranularSimulation();
            for (int i = 0; i < 10; i++)
                sim.Step(grid);
            Assert.AreEqual(CellMaterial.Spoil, grid.Get(2, 1));
            Assert.AreEqual(2, grid.GetWetness(2, 1), "A umidade cai junto com o grão.");
            Assert.AreEqual(1, grid.WetCount);

            RainSimulation rain = Rain(Weather.Clear, new RainSettings { DrySeconds = 0.5f });
            for (int i = 0; i < 70; i++)
                rain.Step(grid, Tick);
            Assert.AreEqual(0, grid.GetWetness(2, 1));
            Assert.AreEqual(0, grid.WetCount);
        }

        [Test]
        public void DropsStopAtTheFirstSolidCellAndDoNotPassThroughIt()
        {
            PixelGrid grid = MakeColumnGrid(CellMaterial.Rock, 1);
            for (int x = 0; x < grid.Width; x++)
                grid.Set(x, 10, CellMaterial.Dirt); // teto
            grid.Set(2, 5, CellMaterial.Spoil);    // embaixo do teto: não pode ser atingido
            RainSimulation rain = Rain(Weather.LightRain, new RainSettings { LightDropsPer100Columns = 2000f, LightHitsToDissolve = 1 });

            for (int i = 0; i < 300; i++)
                rain.Step(grid, Tick);

            Assert.AreEqual(CellMaterial.Spoil, grid.Get(2, 5));
            foreach (RainDrop drop in rain.Drops)
                Assert.Greater(drop.Y, 10);
        }

        static RainSettings StormSettings(float chance) => new RainSettings
        {
            StormChance = chance, StormMinIntervalMinutes = 1f, StormMaxIntervalMinutes = 10f, StormDurationMinutes = 1f,
            HeavyDropsPer100Columns = 0f,
        };

        [Test]
        public void Storm_StartsAfterOneToTenMinutes_LastsOneMinute_ThenClears()
        {
            var grid = new PixelGrid(5, 30);
            RainSimulation rain = Rain(Weather.Clear, StormSettings(1f));
            rain.Step(grid, Tick);
            Assert.That(rain.SecondsToStormRoll, Is.InRange(59f, 600f), "O primeiro período sorteado fica entre 1 e 10 minutos.");

            float elapsed = 0f;
            while (rain.Weather == Weather.Clear && elapsed < 601f)
            {
                rain.Step(grid, 1f);
                elapsed += 1f;
            }
            Assert.AreEqual(Weather.HeavyRain, rain.Weather);
            Assert.That(elapsed, Is.InRange(59f, 601f));
            Assert.IsTrue(rain.StormActive);

            float duration = 0f;
            while (rain.Weather == Weather.HeavyRain && duration < 120f)
            {
                rain.Step(grid, 1f);
                duration += 1f;
            }
            Assert.AreEqual(Weather.Clear, rain.Weather);
            Assert.That(duration, Is.InRange(59f, 62f), "A tempestade dura 1 minuto de jogo.");
            Assert.IsFalse(rain.StormActive);
        }

        [Test]
        public void Storm_ChanceZeroNeverRains_AndSpeedDoesNotShortenIt()
        {
            var grid = new PixelGrid(5, 30);
            RainSimulation never = Rain(Weather.Clear, StormSettings(0f));
            for (int i = 0; i < 3000; i++)
                never.Step(grid, 1f);
            Assert.AreEqual(Weather.Clear, never.Weather);

            // A 8x cada passo vale 8 segundos simulados, mas só 1 de jogo: o período não encurta.
            RainSimulation fast = Rain(Weather.Clear, StormSettings(1f));
            fast.StartStorm();
            for (int i = 0; i < 30; i++)
                fast.Step(grid, 1f, 8f);
            Assert.AreEqual(Weather.HeavyRain, fast.Weather, "30 s simulados a 8x são menos de 4 s de jogo.");
        }

        [Test]
        public void Storm_ManualWeatherCancelsIt_AndRestoreKeepsIt()
        {
            var grid = new PixelGrid(5, 30);
            RainSimulation rain = Rain(Weather.Clear, StormSettings(1f));
            rain.StartStorm();
            rain.Weather = Weather.Clear;
            Assert.IsFalse(rain.StormActive);

            var restored = Rain(Weather.Clear, StormSettings(1f));
            restored.Weather = Weather.HeavyRain;
            restored.RestoreStorm(300f, 20f);
            Assert.IsTrue(restored.StormActive);
            for (int i = 0; i < 21; i++)
                restored.Step(grid, 1f);
            Assert.AreEqual(Weather.Clear, restored.Weather);
        }
    }
}
