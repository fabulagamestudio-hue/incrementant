using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Ciclo de criação (seções 11, 12 e 19 do design): berçário, vagas, reserva, ovo → larva → pupa → adulta.</summary>
    public sealed class BroodTests
    {
        const float Dt = 0.1f;
        static readonly TerrainLayout Layout = new TerrainLayout(20, new Vector2Int(20, 2), 30, 35);

        /// <summary>
        /// Solo de terra até y = 20, corredor de x = 2 a 57 com <paramref name="corridorHeight"/> linhas (y 1 em diante) e,
        /// se <paramref name="nurseryWidth"/> &gt; 0, berçário em x 10.. (3 linhas): 21 colunas dão 63 células.
        /// </summary>
        static PixelGrid MakeWorld(int nurseryWidth, int corridorHeight = 8)
        {
            var grid = new PixelGrid(60, 24);
            for (int x = 0; x < 60; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, 20);
                for (int y = 1; y <= 20; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int x = 2; x < 58; x++)
            for (int y = 1; y <= corridorHeight; y++)
                grid.Set(x, y, CellMaterial.Empty);
            if (nurseryWidth > 0)
            {
                for (int x = 10; x < 10 + nurseryWidth; x++)
                for (int y = 1; y <= 3; y++)
                    Assert.IsTrue(grid.SetNurseryRoom(x, y, true));
            }
            return grid;
        }

        static AntColony MakeColony(int nurseryWidth, int nutrients, int corridorHeight = 8)
        {
            var colony = new AntColony(MakeWorld(nurseryWidth, corridorHeight), new ColonySettings(), Layout, seed: 1);
            colony.RestoreNutrients(nutrients, 0, 0);
            Tick(colony, 1f); // monta os campos e conta o espaço
            return colony;
        }

        static void Tick(AntColony colony, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
                colony.Tick(Dt);
        }

        [Test]
        public void WithoutNursery_TheQueenWaitsForOne_AndLaysNothing()
        {
            AntColony colony = MakeColony(0, 1000);
            Tick(colony, 40f);
            Assert.AreEqual(BreedingState.WaitingNursery, colony.Breeding);
            Assert.AreEqual(0, colony.Brood.Count);
            Assert.AreEqual(1000, colony.Nutrients, "Sem berçário não gasta alimento.");
        }

        [Test]
        public void NurserySlots_CountOnlyReachableEmptyCells()
        {
            AntColony colony = MakeColony(21, 0);
            // 21 x 3 = 63 células / 16 = 3 vagas.
            Assert.AreEqual(3, colony.NurserySlots);
            Assert.AreEqual(18, colony.AdultCapacity, "448 células úteis / 24 por vaga.");
        }

        [Test]
        public void TheQueenLaysAnEgg_AfterTheInterval_AndPaysTheFoodOnce()
        {
            AntColony colony = MakeColony(21, 1000);
            colony.SetQueenPolicy(QueenMode.Specific, AntRole.Digger);
            Tick(colony, 20f);
            Assert.AreEqual(1, colony.Brood.Count);
            Assert.AreEqual(BroodStage.Egg, colony.Brood.Brood[0].Stage);
            Assert.AreEqual(AntRole.Digger, colony.Brood.Brood[0].Role);
            Assert.AreEqual(1000 - 28, colony.Nutrients, "Escavadora: 28 N, debitados uma vez.");
        }

        [Test]
        public void TheEgg_BecomesAnAdultOfTheChosenRole_AfterEggLarvaAndPupa()
        {
            AntColony colony = MakeColony(21, 1000);
            colony.SetQueenPolicy(QueenMode.Specific, AntRole.Carrier);
            int adults = colony.Ants.Count;
            int carriers = colony.CountOf(AntRole.Carrier);
            Tick(colony, 18f + 8f + 14f + 12f - 2f);
            Assert.AreEqual(adults, colony.Ants.Count, "Ainda é pupa.");
            Assert.AreEqual(1, colony.Brood.Pupae);

            Tick(colony, 2f);
            Assert.AreEqual(adults + 1, colony.Ants.Count);
            Assert.AreEqual(carriers + 1, colony.CountOf(AntRole.Carrier));
            Assert.AreEqual(1000 - 24 * 2, colony.Nutrients, "Dois ovos postos até agora (a segunda postura vem 18 s depois da primeira).");
        }

        [Test]
        public void TheReserve_IsKept_BeforeLaying()
        {
            AntColony colony = MakeColony(21, 18 + 29);
            colony.SetQueenPolicy(QueenMode.Specific, AntRole.Worker);
            Tick(colony, 25f);
            Assert.AreEqual(BreedingState.WaitingResources, colony.Breeding);
            Assert.AreEqual(0, colony.Brood.Count);

            colony.RestoreNutrients(18 + 30, 0, 0);
            Tick(colony, 1f);
            Assert.AreEqual(1, colony.Brood.Count, "Com a reserva de 30 N preservada, o ovo sai.");
            Assert.AreEqual(30, colony.Nutrients);
        }

        [Test]
        public void Paused_StopsNewEggs_ButKeepsTheOnesAlreadyLaid()
        {
            AntColony colony = MakeColony(21, 1000);
            Tick(colony, 20f);
            Assert.AreEqual(1, colony.Brood.Count);

            colony.SetQueenPolicy(QueenMode.Paused, AntRole.Worker);
            int adults = colony.Ants.Count;
            Tick(colony, 80f);
            Assert.AreEqual(BreedingState.Paused, colony.Breeding);
            Assert.AreEqual(0, colony.Brood.Count, "A cria já iniciada chega ao fim.");
            Assert.AreEqual(adults + 1, colony.Ants.Count);
        }

        [Test]
        public void TheBrood_FillsTheNurseryAndThenWaitsForASlot()
        {
            AntColony colony = MakeColony(7, 100000); // 21 células de berçário = 1 vaga
            Assert.AreEqual(1, colony.NurserySlots);
            Tick(colony, 30f);
            Assert.AreEqual(1, colony.Brood.Count);
            Assert.AreEqual(BreedingState.WaitingNursery, colony.Breeding, "Com a vaga ocupada, a rainha espera.");
            Tick(colony, 30f);
            Assert.AreEqual(1, colony.Brood.Count, "Nasceu a primeira e a vaga liberada já foi usada por um novo ovo.");
        }

        [Test]
        public void AdultCapacity_LimitsTheEggs_ByFutureAdults()
        {
            // 6 iniciais + 1 cria já ocupam as 7 vagas.
            AntColony colony = MakeColony(21, 100000, corridorHeight: 3); // 168 células = 7 vagas
            Assert.AreEqual(7, colony.AdultCapacity);
            Assert.AreEqual(6, colony.Ants.Count);
            Tick(colony, 40f);
            Assert.AreEqual(1, colony.Brood.Count, "Só cabe uma cria: adultas + crias = 7.");
            Assert.AreEqual(BreedingState.WaitingExpansion, colony.Breeding);
        }

        [Test]
        public void Automatic_SpreadsTheBroodAcrossTheRoles()
        {
            AntColony colony = MakeColony(21, 100000);
            for (int i = 0; i < 12; i++)
            {
                colony.Brood.Restore(new BroodRecord[0], 0f);
                var role = colony.NextAntRole;
                Assert.IsTrue(role.HasValue);
                colony.Spawn(role.Value);
            }
            foreach (AntRole role in AntRoles.All)
                Assert.GreaterOrEqual(colony.CountOf(role), 2, $"O automático também cria {role}.");
            Assert.Greater(colony.CountOf(AntRole.Carrier), colony.CountOf(AntRole.Cutter), "Carregadoras têm parte maior que cortadeiras.");
        }

        [Test]
        public void TheNursery_DoesNotOverlapOtherRooms()
        {
            PixelGrid grid = MakeWorld(21);
            Assert.IsFalse(grid.SetQueenRoom(15, 2, true), "Sala da rainha não pinta sobre o berçário.");
            Assert.IsFalse(grid.SetNutrientRoom(15, 2, true), "Sala de nutrientes não pinta sobre o berçário.");
            Assert.IsTrue(grid.SetQueenRoom(40, 2, true));
            Assert.IsFalse(grid.SetNurseryRoom(40, 2, true), "Berçário não pinta sobre a sala da rainha.");
            Assert.IsFalse(grid.SetNurseryRoom(15, 22, true), "Só o formigueiro (abaixo do solo) aceita berçário.");
        }
    }
}
