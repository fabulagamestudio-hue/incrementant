using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Cuidado do berçário (design 14/19/26): a cria só avança com formiga trabalhando na sala; a cuidadora é mais rápida.</summary>
    public sealed class NurseryCareTests
    {
        const float Dt = 0.1f;
        static readonly TerrainLayout Layout = new TerrainLayout(20, new Vector2Int(20, 2), 30, 35);

        static PixelGrid MakeWorld()
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
            for (int y = 1; y <= 8; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 10; x < 31; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetNurseryRoom(x, y, true));
            return grid;
        }

        static AntColony MakeColony()
        {
            var colony = new AntColony(MakeWorld(), new ColonySettings(), Layout, seed: 1);
            colony.Tree.Restore(QueenTree.LegacyMask | 1UL << (int)QueenNode.N0);
            colony.SetQueenPolicy(QueenMode.Paused, AntRole.Worker);
            Tick(colony, 1f);
            return colony;
        }

        static void Tick(AntColony colony, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
                colony.Tick(Dt);
        }

        static BroodRecord Larva(float remaining) =>
            new BroodRecord { Role = AntRole.Worker, Stage = BroodStage.Larva, Remaining = remaining };

        [Test]
        public void TheBrood_DoesNotAdvance_WithoutAnAntTendingTheNursery()
        {
            AntColony colony = MakeColony();
            colony.Work.SetEnabled(WorkSection.Nursery, false); // berçário parado: ninguém vai cuidar
            colony.RestoreBrood(new[] { Larva(10f) }, 0f);

            Tick(colony, 40f);
            Assert.AreEqual(10f, colony.Brood.Brood[0].Remaining, 1e-3f, "Sem ninguém cuidando, a larva fica parada.");
        }

        [Test]
        public void ACommonAnt_GoesToTheNursery_AndMovesTheBrood_WhileItDevelops()
        {
            AntColony colony = MakeColony();
            colony.RestoreBrood(new[] { Larva(200f), Larva(200f) }, 0f);
            var slots = new HashSet<int>();
            bool sawMoving = false, sawTender = false;

            for (float t = 0f; t < 60f; t += Dt)
            {
                colony.Tick(Dt);
                foreach (BroodRecord b in colony.Brood.Brood)
                {
                    slots.Add(b.Slot * 100 + b.Id);
                    sawMoving |= b.Phase == EggPhase.Moving;
                }
                foreach (Ant ant in colony.Ants)
                    sawTender |= ant.State == AntState.Tending || ant.State == AntState.MovingBrood;
            }

            Assert.IsTrue(sawTender, "Uma comum ociosa vai cuidar do berçário.");
            Assert.IsTrue(sawMoving, "O cuidado é visível: a formiga carrega a cria para outro ponto.");
            Assert.Greater(slots.Count, 2, "As crias mudam de ponto.");
            Assert.Less(colony.Brood.Brood[0].Remaining, 200f, "Cuidada, a larva avança.");
        }

        [Test]
        public void TheNurse_TakesPartOfTheNursery_AndTheCommonsLeaveRoomForHer()
        {
            AntColony colony = MakeColony();
            Ant nurse = colony.Spawn(AntRole.Nurse);
            colony.RestoreBrood(new[] { Larva(300f), Larva(300f), Larva(300f) }, 0f);
            bool nurseTended = false;

            Tick(colony, 40f);
            for (float t = 0f; t < 40f; t += Dt)
            {
                colony.Tick(Dt);
                nurseTended |= nurse.State == AntState.Tending || nurse.State == AntState.MovingBrood;
            }

            Assert.IsTrue(nurseTended, "A cuidadora cuida mesmo com comuns já cuidando.");
            int commons = 0;
            foreach (Ant ant in colony.Ants)
                commons += ant.Role == AntRole.Worker && (ant.State == AntState.Tending || ant.State == AntState.MovingBrood) ? 1 : 0;
            Assert.LessOrEqual(commons, 1, "3 crias chamam 2 formigas: a cuidadora ocupa uma vaga e sobra uma para as comuns.");
        }

        static int MostTenders(AntColony colony, float seconds)
        {
            int most = 0;
            for (float t = 0f; t < seconds; t += Dt)
            {
                colony.Tick(Dt);
                int now = 0;
                foreach (Ant ant in colony.Ants)
                    now += ant.State == AntState.Tending || ant.State == AntState.MovingBrood ? 1 : 0;
                most = Mathf.Max(most, now);
            }
            return most;
        }

        [Test]
        public void TheMoreBrood_TheMoreAntsTheNurseryCalls()
        {
            AntColony few = MakeColony();
            few.RestoreBrood(new[] { Larva(300f), Larva(300f) }, 0f);
            AntColony many = MakeColony();
            var records = new List<BroodRecord>();
            for (int i = 0; i < 8; i++)
                records.Add(Larva(300f));
            many.RestoreBrood(records, 0f);

            int fewest = MostTenders(few, 30f);
            int most = MostTenders(many, 30f);
            Assert.LessOrEqual(fewest, 1, "2 crias chamam 1 formiga.");
            Assert.Greater(most, fewest, "Quanto mais crias, mais formigas vão trabalhar na sala (até metade das comuns).");
        }

        [Test]
        public void ATender_WorksOnVariedBrood_NotAlwaysTheSame()
        {
            AntColony colony = MakeColony();
            colony.RestoreBrood(new[] { Larva(300f), Larva(300f), Larva(300f), Larva(300f) }, 0f);
            var seen = new HashSet<int>();
            for (float t = 0f; t < 90f; t += Dt)
            {
                colony.Tick(Dt);
                foreach (Ant ant in colony.Ants)
                {
                    if (ant.State == AntState.MovingBrood)
                        seen.Add(ant.EggId);
                }
            }
            Assert.GreaterOrEqual(seen.Count, 3, "As formigas mexem em crias variadas.");
        }

        [Test]
        public void NurserySpots_SkipHolesInTheWall_EvenWhenTheWallIsPainted()
        {
            PixelGrid grid = MakeWorld();
            // Chão do berçário mais alto, com buracos de um pixel nele; a pintura cobre a parede e os buracos.
            for (int x = 10; x < 31; x++)
                grid.Set(x, 1, CellMaterial.Dirt);
            grid.Set(15, 1, CellMaterial.Empty);
            grid.Set(24, 1, CellMaterial.Empty);
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 1);
            Tick(colony, 1f);

            Assert.Greater(colony.NurserySpots.Count, 0, "O vão aberto do berçário continua com pontos.");
            foreach (Vector2Int spot in colony.NurserySpots)
            {
                for (int dy = 0; dy < AntColony.NurserySpotHeight; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    Assert.IsFalse(grid.IsSolid(spot.x + dx, spot.y + dy),
                        $"O ponto {spot} encosta o sprite da cria na parede em ({spot.x + dx}, {spot.y + dy}).");
            }
        }

        [Test]
        public void ATender_LeavesWhenThereIsNothingToTend()
        {
            AntColony colony = MakeColony();
            colony.RestoreBrood(new BroodRecord[0], 0f);
            Tick(colony, 10f);
            foreach (Ant ant in colony.Ants)
                Assert.AreNotEqual(AntState.Tending, ant.State, "Sem cria, ninguém fica cuidando.");
        }

        [Test]
        public void Care_CoversFourBroodPerAnt_AndTheNurseIsFaster()
        {
            var cycle = new BroodCycle(new ColonySettings());
            var records = new List<BroodRecord>();
            for (int i = 0; i < 5; i++)
                records.Add(Larva(10f));
            cycle.Restore(records, 100f);
            // Uma cuidadora cobre 4 crias; ela carrega a última da lista (a quinta), que avança no ritmo dela (4/3).
            var boosts = new List<BlockBoost> { new BlockBoost { Id = cycle.Brood[4].Id, Rate = 4f / 3f } };
            var context = new BroodContext { Mode = QueenMode.Paused, CareCapacity = 4, Boosts = boosts, NurserySlots = 10 };
            var born = new List<AntRole>();

            cycle.Tick(3f, context, born);
            Assert.AreEqual(10f - 4f, cycle.Brood[4].Remaining, 1e-3f, "Só a cria em interação recebe o ritmo da cuidadora: 3 s × 4/3.");
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(10f - 3f, cycle.Brood[i].Remaining, 1e-3f, "As outras cobertas avançam no ritmo normal.");
            Assert.AreEqual(10f, cycle.Brood[3].Remaining, 1e-3f, "A quarta ficou sem cobertura (a capacidade 4 foi usada pela interação e por três).");
        }

        [Test]
        public void TheNurse_IsUnlockedByN0_AndN1RaisesHerCapacity()
        {
            var tree = new QueenTree();
            Assert.IsFalse(tree.IsRoleUnlocked(AntRole.Nurse));
            tree.Restore(1UL << (int)QueenNode.N0);
            Assert.IsTrue(tree.IsRoleUnlocked(AntRole.Nurse));
            Assert.AreEqual(4, tree.CareCapacity(AntRole.Nurse));
            Assert.AreEqual(4f / 3f, tree.CareRate(AntRole.Nurse), 1e-5f);
            Assert.AreEqual(1f, tree.CareRate(AntRole.Worker));
            tree.Restore(1UL << (int)QueenNode.N0 | 1UL << (int)QueenNode.N1);
            Assert.AreEqual(8, tree.CareCapacity(AntRole.Nurse), "N1: 8 crias por viagem.");
            Assert.AreEqual(4, tree.CareCapacity(AntRole.Worker));
        }
    }
}
