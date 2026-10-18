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
            colony.Tree.Restore(QueenTree.LegacyMask | 1UL << (int)QueenNode.N0); // só as classes liberadas; estes testes são do ciclo, não da árvore (ver QueenTreeTests)
            colony.RestoreNutrients(nutrients, 0, 0);
            Tick(colony, 1f); // monta os campos e conta o espaço
            return colony;
        }

        static void Tick(AntColony colony, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
                colony.Tick(Dt);
        }

        /// <summary>Avança até a condição valer (no máximo <paramref name="maxSeconds"/>); devolve se valeu.</summary>
        static bool TickUntil(AntColony colony, System.Func<bool> condition, float maxSeconds)
        {
            for (float t = 0f; t < maxSeconds; t += Dt)
            {
                if (condition())
                    return true;
                colony.Tick(Dt);
            }
            return condition();
        }

        static bool AllPlaced(AntColony colony)
        {
            foreach (BroodRecord b in colony.Brood.Brood)
            {
                if (b.Phase != EggPhase.Placed)
                    return false;
            }
            return colony.Brood.Count > 0;
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
            // A postura sai aos 18 s; o ovo só se desenvolve depois de levado ao berçário, então a pupa vem uns segundos depois.
            Assert.IsTrue(TickUntil(colony, () => colony.Brood.Pupae == 1, 60f), "Ovo → larva → pupa.");
            Assert.AreEqual(adults, colony.Ants.Count, "Ainda é pupa.");

            Assert.IsTrue(TickUntil(colony, () => colony.Ants.Count == adults + 1, 30f), "Pupa → adulta.");
            Assert.AreEqual(carriers + 1, colony.CountOf(AntRole.Carrier));
            // Um ovo por postura (24 N cada): o que nasceu mais os que ainda estão na lista.
            Assert.AreEqual(1000 - 24 * (colony.Brood.Count + 1), colony.Nutrients);
        }

        [Test]
        public void TheEgg_IsLaidWhereTheQueenIs_AndOnlyDevelopsAfterBeingCarriedToTheNursery()
        {
            AntColony colony = MakeColony(21, 1000);
            Assert.IsTrue(TickUntil(colony, () => colony.Brood.Count == 1, 25f));
            BroodRecord laid = colony.Brood.Brood[0];
            // Uma formiga ociosa o reivindica já no passo da postura (as formigas andam depois da rainha).
            Assert.IsTrue(laid.Phase == EggPhase.Laid || laid.Phase == EggPhase.Claimed, $"Fase {laid.Phase}.");
            Assert.AreEqual(colony.Queen.X, laid.LaidX);
            Assert.AreEqual(colony.Queen.Y, laid.LaidY);
            Assert.AreEqual(-1, laid.Slot, "Ainda não está em nenhum ponto do berçário.");

            bool wasCarried = false;
            float eggSeconds = new ColonySettings().EggSeconds;
            while (!AllPlaced(colony))
            {
                BroodRecord b = colony.Brood.Brood[0];
                wasCarried |= b.Phase == EggPhase.Carried;
                Assert.AreEqual(eggSeconds, b.Remaining, "Ovo fora do berçário não se desenvolve.");
                Assert.Less(colony.SimulationTime, 60f, "Nenhuma formiga levou o ovo.");
                colony.Tick(Dt);
            }
            Assert.IsTrue(wasCarried, "O ovo passou pela boca de uma formiga.");
            BroodRecord placed = colony.Brood.Brood[0];
            Vector2Int spot = colony.NurserySpots[placed.Slot];
            Assert.IsTrue(colony.Brood.TryGet(placed.Id, out _));
            Assert.AreEqual(BroodStage.Egg, placed.Stage);
            Assert.AreEqual(1, colony.Brood.PlacedAt(placed.Slot));
            Assert.IsTrue(MakeWorld(21).IsNurseryRoom(spot.x, spot.y), "O ponto fica no berçário.");
        }

        [Test]
        public void OnlyCommonAntsAndCarriersCarryTheEgg()
        {
            AntColony colony = MakeColony(21, 1000);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Cutter;
            Assert.IsTrue(TickUntil(colony, () => colony.Brood.Count == 1, 25f));
            Tick(colony, 30f);
            Assert.AreEqual(EggPhase.Laid, colony.Brood.Brood[0].Phase, "Cortadeiras não carregam ovo.");

            colony.Ants[0].Role = AntRole.Worker;
            Assert.IsTrue(TickUntil(colony, () => AllPlaced(colony), 40f), "Uma operária comum leva o ovo.");
        }

        [Test]
        public void TwoEggs_GoToDifferentSpotsOfTheNursery()
        {
            AntColony colony = MakeColony(21, 1000);
            Assert.IsTrue(TickUntil(colony, () => colony.Brood.Count == 2 && AllPlaced(colony), 60f));
            Assert.AreNotEqual(colony.Brood.Brood[0].Slot, colony.Brood.Brood[1].Slot);
        }

        [Test]
        public void ABroodCycle_ReleasesTheEgg_WhenTheCarrierIsLost()
        {
            var cycle = new BroodCycle(new ColonySettings());
            var context = new BroodContext
            {
                Mode = QueenMode.Specific, Role = AntRole.Worker, Cost = 18, Nutrients = 1000, Adults = 0, AdultCapacity = 100,
                NurserySlots = 5, QueenX = 7, QueenY = 3,
            };
            var born = new System.Collections.Generic.List<AntRole>();
            for (float t = 0f; t < 19f && cycle.Count == 0; t += Dt)
                cycle.Tick(Dt, context, born);
            Assert.AreEqual(1, cycle.Count);
            Assert.IsTrue(cycle.TryClaimEgg(out int id));
            Assert.IsFalse(cycle.TryClaimEgg(out _), "Um ovo, uma formiga.");
            cycle.PickUpEgg(id);
            cycle.ReleaseEgg(id, 12, 5);
            cycle.TryGet(id, out BroodRecord egg);
            Assert.AreEqual(EggPhase.Laid, egg.Phase);
            Assert.AreEqual(12, egg.LaidX, "Volta ao chão onde a formiga estava.");
            Assert.AreEqual(5, egg.LaidY);
            Assert.IsTrue(cycle.TryClaimEgg(out _), "Outra formiga pode pegar.");
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
        public void AdultsPlusBrood_NeverExceedTheCapacity_EvenOnTheTickWhenAnAntIsBorn()
        {
            // Regressão: no passo do nascimento a vaga era liberada duas vezes (a cria saía da lista antes de a adulta existir)
            // e a rainha punha um ovo a mais, deixando adultas + crias em capacidade + 1.
            AntColony colony = MakeColony(21, 100000, corridorHeight: 3); // 7 vagas
            Assert.AreEqual(7, colony.AdultCapacity);
            int births = 0;
            for (float t = 0f; t < 200f; t += Dt)
            {
                int before = colony.Ants.Count;
                colony.Tick(Dt);
                births += colony.Ants.Count - before;
                Assert.LessOrEqual(colony.Ants.Count + colony.Brood.Count, colony.AdultCapacity, $"Estourou em t = {t:F1} s.");
            }
            Assert.GreaterOrEqual(births, 1, "O teste precisa cobrir ao menos um nascimento.");
        }

        [Test]
        public void EachBrood_KeepsItsSlot_WhileAnotherIsBorn()
        {
            AntColony colony = MakeColony(21, 100000);
            colony.SetQueenPolicy(QueenMode.Specific, AntRole.Worker);
            int adults = colony.Ants.Count;
            Assert.IsTrue(TickUntil(colony, () => colony.Brood.Count == 2 && AllPlaced(colony), 60f));
            BroodRecord second = colony.Brood.Brood[1];

            Assert.IsTrue(TickUntil(colony, () => colony.Ants.Count == adults + 1, 60f), "A primeira nasce.");
            Assert.IsTrue(colony.Brood.TryGet(second.Id, out BroodRecord after), "A segunda segue na lista.");
            // Quem cuida do berçário muda as crias de ponto; o que não pode acontecer é a cria sair da sala quando outra nasce.
            Assert.IsTrue(after.Phase == EggPhase.Placed || after.Phase == EggPhase.Moving, "A segunda segue no berçário.");
            Assert.Less(after.Slot, colony.NurserySpots.Count);
        }

        [Test]
        public void Restore_AssignsDistinctSlots()
        {
            AntColony colony = MakeColony(21, 0);
            colony.RestoreBrood(new[]
            {
                new BroodRecord { Role = AntRole.Worker, Stage = BroodStage.Egg, Remaining = 3f },
                new BroodRecord { Role = AntRole.Digger, Stage = BroodStage.Larva, Remaining = 5f },
                new BroodRecord { Role = AntRole.Cutter, Stage = BroodStage.Pupa, Remaining = 7f },
            }, 4f);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(i, colony.Brood.Brood[i].Slot);
                Assert.AreEqual(EggPhase.Placed, colony.Brood.Brood[i].Phase, "O save não guarda o transporte: volta no berçário.");
            }
        }

        [Test]
        public void NurserySpots_CoverTheWholeNursery_AWholeSpriteApart()
        {
            AntColony colony = MakeColony(21, 0);
            // Berçário x 10..30, y 1..3: o chão (y = 1) e o alto (y = 3) recebem pontos, um a cada 3 células por linha e
            // 2 de altura; a linha y = 2 fica coberta pelos sprites de baixo.
            Assert.AreEqual(14, colony.NurserySpots.Count);
            for (int i = 0; i < colony.NurserySpots.Count; i++)
            {
                Assert.AreEqual(i < 7 ? 1 : 3, colony.NurserySpots[i].y, "Chão primeiro, depois acima.");
                if (i > 0 && colony.NurserySpots[i].y == colony.NurserySpots[i - 1].y)
                    Assert.GreaterOrEqual(colony.NurserySpots[i].x - colony.NurserySpots[i - 1].x, AntColony.NurserySpotSpacing);
            }
            Assert.AreEqual(0, MakeColony(0, 0).NurserySpots.Count, "Sem berçário não há onde desenhar.");
        }

        [Test]
        public void Automatic_SpreadsTheBroodAcrossTheRoles()
        {
            AntColony colony = MakeColony(21, 100000);
            for (int i = 0; i < 30; i++)
            {
                colony.Brood.Restore(new BroodRecord[0], 0f);
                var role = colony.NextAntRole;
                Assert.IsTrue(role.HasValue);
                colony.Spawn(role.Value);
            }
            foreach (AntRole role in AntRoles.All)
            {
                // O soldado só existe depois do primeiro ataque de cupins (não liberado aqui): o automático não o cria.
                if (!colony.Tree.IsRoleUnlocked(role))
                {
                    Assert.AreEqual(0, colony.CountOf(role), $"{role} bloqueado não nasce.");
                    continue;
                }
                Assert.GreaterOrEqual(colony.CountOf(role), 2, $"O automático também cria {role}.");
            }
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
