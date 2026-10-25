using System.IO;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Depósitos de alimentos, construção e água (pedido do desenvolvedor em 2026-10-19): o que vem de fora vai primeiro ao
    /// depósito, como item guardado que não bloqueia a passagem, e quem consome pega de lá.
    /// </summary>
    public sealed class StorageTests
    {
        const float Dt = 0.1f;
        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);

        /// <summary>
        /// Solo de terra até y = 20 (céu acima), corredor de x 10 a 100 com 3 linhas (y 1 a 3), poço em x 50..51 até a
        /// superfície, sala de nutrientes em x 60..80 e sala da rainha em x 88..98. Os depósitos são pintados por teste:
        /// alimentos em x 20..26, construção em x 30..36, água em x 40..46.
        /// </summary>
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
            for (int x = 50; x <= 51; x++)
            for (int y = 4; y <= 20; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 60; x <= 80; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetNutrientRoom(x, y, true));
            for (int x = 88; x <= 98; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true));
            return grid;
        }

        static void PaintDepot(PixelGrid grid, StorageKind kind, int minX, bool paint = true)
        {
            for (int x = minX; x <= minX + 6; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetStorageRoom(x, y, kind, paint));
        }

        static AntColony MakeColony(PixelGrid grid)
        {
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 5) { RaidsEnabled = false };
            colony.RestoreQueen(95, 2, 1);
            return colony;
        }

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

        static int CountStoredIn(PixelGrid grid, StorageKind kind, int minX)
        {
            int n = 0;
            for (int x = minX; x <= minX + 6; x++)
            for (int y = 1; y <= 3; y++)
                n += StoredItems.KindOf(grid.GetStored(x, y)) == kind ? 1 : 0;
            return n;
        }

        static int CountMaterial(PixelGrid grid, CellMaterial material)
        {
            int n = 0;
            RectInt area = grid.Active;
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
                n += grid.Get(x, y) == material ? 1 : 0;
            return n;
        }

        [Test]
        public void StoredItems_DoNotBlockThePassage_AndAreLostUnderSolidMaterial()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Food, 20);
            int structure = grid.StructureVersion;
            Assert.IsTrue(grid.SetStored(22, 2, StoredItems.Of(CellMaterial.PlantLeaf)));
            Assert.AreEqual(structure, grid.StructureVersion, "Guardar não muda a estrutura.");
            Assert.AreEqual(CellMaterial.Empty, grid.Get(22, 2));
            Assert.IsTrue(AntNavigation.IsWalkable(grid, 22, 2), "A formiga passa pela célula com item.");
            Assert.IsFalse(grid.SetStored(22, 2, StoredItems.Water), "Um item por célula.");
            Assert.IsFalse(grid.SetStored(22, 4, StoredItems.Water), "Célula com material não guarda.");
            Assert.AreEqual(1, grid.StoredCount(StorageKind.Food));

            grid.Set(22, 2, CellMaterial.Sand);
            Assert.AreEqual(StoredItems.None, grid.GetStored(22, 2), "Material sólido por cima destrói o item.");
            Assert.AreEqual(0, grid.StoredCount(StorageKind.Food));
        }

        [Test]
        public void Depots_AreRooms_ThatNeverOverlapOtherRooms()
        {
            PixelGrid grid = MakeWorld();
            Assert.IsFalse(grid.SetStorageRoom(65, 2, StorageKind.Food, true), "Não pinta por cima da sala de nutrientes.");
            Assert.IsFalse(grid.SetStorageRoom(30, 25, StorageKind.Food, true), "Só no formigueiro (no solo original ou abaixo).");
            Assert.IsTrue(grid.SetStorageRoom(30, 2, StorageKind.Blocks, true));
            Assert.IsFalse(grid.SetStorageRoom(30, 2, StorageKind.Water, true), "Um depósito por célula.");
            Assert.IsFalse(grid.SetNurseryRoom(30, 2, true), "As outras salas não pintam por cima do depósito.");
            Assert.IsFalse(grid.SetStorageRoom(30, 2, StorageKind.Water, false), "Apagar com o pincel de outro tipo não apaga.");
            Assert.IsTrue(grid.SetStorageRoom(30, 2, StorageKind.Blocks, false));
            Assert.AreEqual(0, grid.StorageRoomCount(StorageKind.Blocks));
        }

        [Test]
        public void CutPlants_GoToTheFoodDepot_AndTheBedsTakeThemFromThere()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Food, 20);
            foreach (int x in new[] { 84, 85, 86 })
            {
                grid.Set(x, 1, CellMaterial.FallenLeaf, 120);
                Assert.IsTrue(grid.SetMarked(x, 1, true));
            }
            AntColony colony = MakeColony(grid);

            Assert.IsTrue(TickUntil(colony, () => CountStoredIn(grid, StorageKind.Food, 20) == 3, 120f),
                          "Sem canteiro, os pedaços soltos vão ao depósito de alimentos.");
            Assert.AreEqual(0, CountMaterial(grid, CellMaterial.FallenLeaf));
            Assert.AreEqual(0, colony.HarvestedPixels);

            grid.Set(62, 1, CellMaterial.Spoil);
            Assert.IsTrue(grid.SetBed(62, 1, BedStage.Soil));
            Assert.IsTrue(TickUntil(colony, () => grid.GetBedStage(62, 1) == BedStage.Planted, 120f),
                          "O canteiro recebe a planta do depósito.");
            Assert.AreEqual(2, CountStoredIn(grid, StorageKind.Food, 20), "Saiu um pedaço do depósito.");
            Assert.AreEqual(1, colony.HarvestedPixels);
        }

        [Test]
        public void DugSoil_GoesToTheBlockDepot_AndTheBuildUsesIt()
        {
            PixelGrid grid = MakeWorld();
            // Sem sala de nutrientes: os canteiros também puxariam a terra do depósito.
            for (int x = 60; x <= 80; x++)
            for (int y = 1; y <= 3; y++)
                grid.SetNutrientRoom(x, y, false);
            PaintDepot(grid, StorageKind.Blocks, 30);
            foreach (int x in new[] { 40, 41, 42 })
                Assert.IsTrue(grid.SetMarked(x, 4, true));
            // Salas já rebocadas: a construção vem antes das automatizações (decisão de 2026-10-22); o teste mede o que vem depois dela.
            RoomLiningTests.LineAllRooms(grid);
            AntColony colony = MakeColony(grid);

            Assert.IsTrue(TickUntil(colony, () => CountStoredIn(grid, StorageKind.Blocks, 30) == 3, 120f),
                          "A terra cavada vai ao depósito de construção.");
            Assert.AreEqual(0, colony.TotalDelivered, "Nada foi ao despejo.");
            for (int x = 30; x <= 36; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsFalse(grid.IsSolid(x, y), "O depósito continua aberto: os blocos guardados não são parede.");

            Assert.IsTrue(grid.SetBuildMarked(85, 3, true));
            Assert.IsTrue(TickUntil(colony, () => grid.IsSolid(85, 3), 120f), "A obra é feita.");
            Assert.AreEqual(2, CountStoredIn(grid, StorageKind.Blocks, 30), "Com o bloco do depósito.");
        }

        [Test]
        public void PileOutside_DumpedBeforeTheDepot_IsCarriedIntoIt()
        {
            PixelGrid grid = MakeWorld();
            for (int x = 60; x <= 80; x++)
            for (int y = 1; y <= 3; y++)
                grid.SetNutrientRoom(x, y, false);
            // Terra despejada antes de o depósito existir, sem obra nem canteiro pedindo terra.
            foreach (int x in new[] { 70, 71, 72 })
                grid.Set(x, 21, CellMaterial.Spoil);
            PaintDepot(grid, StorageKind.Blocks, 30);
            // Salas já rebocadas: a construção vem antes das automatizações (decisão de 2026-10-22); o teste mede o que vem depois dela.
            RoomLiningTests.LineAllRooms(grid);
            AntColony colony = MakeColony(grid);

            Assert.IsTrue(TickUntil(colony, () => CountStoredIn(grid, StorageKind.Blocks, 30) == 3, 120f),
                          "A pilha de fora vai ao depósito de construção.");
            Assert.AreEqual(0, CountMaterial(grid, CellMaterial.Spoil), "Não sobrou terra solta do lado de fora.");
        }

        [Test]
        public void MapWater_FillsTheWaterDepot_AndTheBedsAreWateredFromIt()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Water, 40);
            for (int x = 82; x <= 85; x++)
                Assert.IsTrue(grid.SetWater(x, 1, true));
            AntColony colony = MakeColony(grid);

            Assert.IsTrue(TickUntil(colony, () => CountStoredIn(grid, StorageKind.Water, 40) == 4, 120f),
                          "A água do mapa vai ao depósito de água.");
            Assert.AreEqual(0, grid.WaterCount, "Toda a água foi tirada do mapa.");

            grid.Set(62, 1, CellMaterial.Spoil);
            Assert.IsTrue(grid.SetBed(62, 1, BedStage.Planted));
            Assert.IsTrue(TickUntil(colony, () => grid.GetBedStage(62, 1) == BedStage.Growing, 120f),
                          "O canteiro é regado com a água guardada.");
            Assert.AreEqual(3, CountStoredIn(grid, StorageKind.Water, 40));
        }

        /// <summary>
        /// Abastecimento da sala de nutrientes (pedido do desenvolvedor em 2026-10-21): com depósito, quem busca a planta e a
        /// água dos canteiros é quem trabalha na sala (jardineira e comum) e a carregadora, que entrega do estoque (pedido do
        /// mesmo dia); cortadeira, escavadora e as outras classes não.
        /// </summary>
        [Test]
        public void NutrientRoomWorkersAndCarriers_FetchPlantAndWaterFromTheDepots()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Food, 20);
            PaintDepot(grid, StorageKind.Water, 40);
            foreach (int x in new[] { 21, 22, 23 })
            {
                Assert.IsTrue(grid.SetStored(x, 1, StoredItems.Of(CellMaterial.PlantLeaf)));
                Assert.IsTrue(grid.SetStored(x + 20, 1, StoredItems.Water));
            }
            grid.Set(62, 1, CellMaterial.Spoil);
            Assert.IsTrue(grid.SetBed(62, 1, BedStage.Soil));
            AntColony colony = MakeColony(grid);
            for (int i = 0; i < colony.Ants.Count; i += 2)
                colony.Ants[i].Role = AntRole.Carrier;
            colony.SpawnAt(AntRole.Gardener, 70, 1);

            // A carregadora sem trabalho do papel pode virar comum (segunda função, KI-033): aí ela trabalha na sala.
            bool onlyRoomWorkers = true;
            Assert.IsTrue(TickUntil(colony, () =>
            {
                foreach (Ant ant in colony.Ants)
                {
                    bool supplying = ant.State == AntState.TakingStored && (ant.StoreUse == StoreUse.BedPlant || ant.StoreUse == StoreUse.Water) ||
                                     ant.State == AntState.Carrying && ant.FromStore;
                    onlyRoomWorkers &= !supplying ||
                                       (AntRoles.Gardens(ant.TaskRole) || ant.TaskRole == AntRole.Carrier) && (ant.State != AntState.Carrying || ant.GardenSupply);
                }
                return grid.GetBedStage(62, 1) == BedStage.Growing;
            }, 120f), "Quem trabalha na sala busca a planta e a água nos depósitos.");
            Assert.IsTrue(onlyRoomWorkers, "Só jardineira, comum e carregadora levam o que sai do depósito aos canteiros.");
            Assert.AreEqual(2, CountStoredIn(grid, StorageKind.Food, 20));
            Assert.AreEqual(2, CountStoredIn(grid, StorageKind.Water, 40));
        }

        /// <summary>Tira as formigas iniciais (como se tivessem morrido) e deixa só uma carregadora em (<paramref name="x"/>, 1).</summary>
        static Ant OnlyACarrier(AntColony colony, int x)
        {
            foreach (Ant ant in colony.Ants)
                ant.Wounds = 1000f;
            colony.Tick(Dt);
            Assert.AreEqual(0, colony.Ants.Count);
            return colony.SpawnAt(AntRole.Carrier, x, 1);
        }

        /// <summary>
        /// A carregadora leva mais de cada item (pedido do desenvolvedor em 2026-10-21): o dobro da comum em terra e, no
        /// depósito, recolhe de uma vez os itens iguais encostados nela, até o que os canteiros pedem.
        /// </summary>
        [Test]
        public void Carrier_CarriesMoreSoil_AndTakesSeveralItemsFromTheDepotInOneTrip()
        {
            var tree = new QueenTree();
            Assert.AreEqual(6, tree.SoilCapacity(AntRole.Carrier), "O dobro das 3 da comum.");

            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Food, 20);
            foreach (int x in new[] { 22, 23, 24 })
                Assert.IsTrue(grid.SetStored(x, 1, StoredItems.Of(CellMaterial.PlantLeaf)));
            foreach (int x in new[] { 62, 64, 66 })
            {
                grid.Set(x, 1, CellMaterial.Spoil);
                Assert.IsTrue(grid.SetBed(x, 1, BedStage.Soil));
            }
            AntColony colony = MakeColony(grid);
            Ant carrier = OnlyACarrier(colony, 40);

            int most = 0;
            Assert.IsTrue(TickUntil(colony, () =>
            {
                if (carrier.FromStore)
                    most = Mathf.Max(most, carrier.CarriedCount);
                return grid.GetBedStage(62, 1) != BedStage.Soil && grid.GetBedStage(64, 1) != BedStage.Soil &&
                       grid.GetBedStage(66, 1) != BedStage.Soil;
            }, 120f), "Os três canteiros recebem planta do depósito.");
            Assert.GreaterOrEqual(most, 2, "Ela sai do depósito com mais de uma planta.");
            Assert.LessOrEqual(most, 3, "Não pega mais do que os canteiros pedem.");
        }

        /// <summary>
        /// A carregadora leva várias águas por viagem (pedido do desenvolvedor em 2026-10-21): do mapa ao depósito, guardando
        /// uma em cada vaga, e do depósito aos canteiros, regando um por um.
        /// </summary>
        [Test]
        public void Carrier_CarriesSeveralWaterBlocks_FromTheMapToTheDepot_AndFromTheDepotToTheBeds()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Water, 40);
            for (int x = 30; x <= 33; x++)
                Assert.IsTrue(grid.SetWater(x, 1, true));
            AntColony colony = MakeColony(grid);
            Ant carrier = OnlyACarrier(colony, 36);

            int most = 0;
            Assert.IsTrue(TickUntil(colony, () =>
            {
                if (carrier.State == AntState.CarryingWater)
                    most = Mathf.Max(most, carrier.WaterLoad);
                return CountStoredIn(grid, StorageKind.Water, 40) == 4;
            }, 120f), "As quatro águas do mapa vão ao depósito.");
            Assert.AreEqual(0, grid.WaterCount);
            Assert.GreaterOrEqual(most, 2, "Ela sai da água com mais de um bloco.");

            foreach (int x in new[] { 62, 64, 66 })
            {
                grid.Set(x, 1, CellMaterial.Spoil);
                Assert.IsTrue(grid.SetBed(x, 1, BedStage.Planted));
            }
            most = 0;
            Assert.IsTrue(TickUntil(colony, () =>
            {
                if (carrier.State == AntState.CarryingWater && carrier.StoreTarget == StorageKind.None)
                    most = Mathf.Max(most, carrier.WaterLoad);
                return grid.GetBedStage(62, 1) == BedStage.Growing && grid.GetBedStage(64, 1) == BedStage.Growing &&
                       grid.GetBedStage(66, 1) == BedStage.Growing;
            }, 120f), "Os três canteiros são regados com a água do depósito.");
            Assert.GreaterOrEqual(most, 2, "Ela sai do depósito com mais de uma água.");
            Assert.LessOrEqual(most, 3, "Não pega mais do que os canteiros pedem.");
            Assert.AreEqual(1, CountStoredIn(grid, StorageKind.Water, 40), "Sobra a quarta água no depósito.");
        }

        /// <summary>
        /// Prioridade da carregadora (pedido do desenvolvedor em 2026-10-21): primeiro enche o depósito (a água do mapa),
        /// depois entrega do depósito (a planta dos canteiros) e, sem nada disso, trabalha como comum (cava).
        /// </summary>
        [Test]
        public void Carrier_StocksFirst_ThenDeliversFromStock_ThenWorksAsACommonWorker()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Food, 20);
            PaintDepot(grid, StorageKind.Water, 40);
            Assert.IsTrue(grid.SetStored(22, 1, StoredItems.Of(CellMaterial.PlantLeaf)));
            Assert.IsTrue(grid.SetWater(30, 1, true));
            grid.Set(62, 1, CellMaterial.Spoil);
            Assert.IsTrue(grid.SetBed(62, 1, BedStage.Soil));
            for (int x = 12; x <= 14; x++)
                grid.SetMarked(x, 4, true);
            AntColony colony = MakeColony(grid);
            Ant carrier = OnlyACarrier(colony, 35);

            var order = new System.Collections.Generic.List<string>();
            Assert.IsTrue(TickUntil(colony, () =>
            {
                string step = carrier.State == AntState.FetchingWater && carrier.StoreTarget == StorageKind.Water ? "estoque"
                    : carrier.State == AntState.TakingStored && carrier.StoreUse == StoreUse.BedPlant ? "entrega"
                    : carrier.ActingAsWorker && carrier.State != AntState.Idle ? "comum" : null;
                if (step != null && (order.Count == 0 || order[order.Count - 1] != step))
                    order.Add(step);
                return order.Contains("comum");
            }, 120f), "Sem estoque a mexer, ela trabalha como comum.");
            CollectionAssert.AreEqual(new[] { "estoque", "entrega", "comum" }, order);
        }

        /// <summary>
        /// Ordens de entrega (pedido do desenvolvedor em 2026-10-20): cada formiga que vai encher o depósito sai com a vaga
        /// travada; duas nunca disputam a mesma vaga e ninguém fica andando com água na boca quando as vagas acabam.
        /// </summary>
        [Test]
        public void WaterOrders_EachAntHasItsOwnSlot_AndNobodyIsLeftHoldingWater()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Water, 40);
            // Só 3 vagas livres: o resto do depósito já está cheio.
            int free = 0;
            for (int x = 40; x <= 46; x++)
            for (int y = 1; y <= 3; y++)
            {
                if (free < 3 && y == 1)
                {
                    free++;
                    continue;
                }
                Assert.IsTrue(grid.SetStored(x, y, StoredItems.Water));
            }
            for (int x = 70; x <= 85; x++)
                Assert.IsTrue(grid.SetWater(x, 1, true));
            AntColony colony = MakeColony(grid);
            for (int i = 0; i < 10; i++)
                colony.SpawnAt(AntRole.Worker, 75, 2);

            var owners = new System.Collections.Generic.HashSet<int>();
            for (float t = 0f; t < 90f; t += Dt)
            {
                colony.Tick(Dt);
                owners.Clear();
                foreach (Ant ant in colony.Ants)
                {
                    if (ant.OrderSlot < 0)
                        continue;
                    Assert.IsTrue(owners.Add(ant.OrderSlot), $"Duas formigas com a mesma vaga ({ant.OrderSlot}) em t = {t:F1} s.");
                }
            }
            Assert.AreEqual(21, CountStoredIn(grid, StorageKind.Water, 40), "As 3 vagas livres foram ocupadas.");
            foreach (Ant ant in colony.Ants)
                Assert.AreNotEqual(AntState.CarryingWater, ant.State, "Sem vaga nem canteiro, ninguém fica com água na boca.");
            Assert.AreEqual(16 - 3, grid.WaterCount, "Só saiu do mapa a água que tinha vaga.");
        }

        /// <summary>
        /// Troca de função no obstáculo (REQ-043, 2026-10-20): a formiga com ordem de água acha o caminho fechado por um bloco
        /// que outra ia cavar; quem chegou cava e a dona do bloco assume a ordem, com a mesma vaga travada.
        /// </summary>
        [Test]
        public void WaterOrder_BlockedByAMarkedBlock_SwapsWithTheAntThatWasGoingToDigIt()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Water, 20);
            for (int x = 82; x <= 85; x++)
                Assert.IsTrue(grid.SetWater(x, 1, true));
            AntColony colony = MakeColony(grid);
            Ant carrier = null;
            int carrierIndex = -1;
            for (float t = 0f; t < 30f && carrier == null; t += Dt)
            {
                colony.Tick(Dt);
                for (int i = 0; i < colony.Ants.Count - 1 && carrier == null; i++)
                {
                    Ant a = colony.Ants[i];
                    if (a.State == AntState.FetchingWater && a.OrderSlot >= 0 && a.SeekPathLength > 3 && !a.SeekArrived)
                    {
                        carrier = a;
                        carrierIndex = i;
                    }
                }
            }
            Assert.IsNotNull(carrier, "Alguma formiga sai com uma ordem de água.");
            int cell = carrier.SeekNext;
            int cx = cell % grid.Width, cy = cell / grid.Width;
            grid.Set(cx, cy, CellMaterial.Dirt);
            Assert.IsTrue(grid.SetMarked(cx, cy, true));
            // A dona do bloco: uma operária ociosa depois dela na lista, que ia cavá-lo.
            Ant owner = colony.Ants[colony.Ants.Count - 1];
            Assert.Greater(colony.Ants.Count - 1, carrierIndex);
            owner.Role = AntRole.Worker;
            owner.State = AntState.Idle;
            owner.ClearSeek();
            owner.SeekTarget = cell;
            int slot = carrier.OrderSlot;

            colony.Tick(Dt);

            Assert.AreEqual(1, colony.ObstacleSwaps, "As duas trocaram de função.");
            Assert.AreEqual(AntState.Digging, carrier.State, "Quem chegou cava o bloco.");
            Assert.AreEqual(cell, carrier.DigY * grid.Width + carrier.DigX);
            Assert.AreEqual(AntState.FetchingWater, owner.State, "A dona do bloco assume a ordem da água.");
            Assert.AreEqual(slot, owner.OrderSlot, "Com a mesma vaga travada.");
        }

        [Test]
        public void WaterOrder_BlockedByABlockNobodyOwns_DoesNotDigIt()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Water, 20);
            for (int x = 82; x <= 85; x++)
                Assert.IsTrue(grid.SetWater(x, 1, true));
            AntColony colony = MakeColony(grid);
            Ant carrier = null;
            for (float t = 0f; t < 30f && carrier == null; t += Dt)
            {
                colony.Tick(Dt);
                foreach (Ant a in colony.Ants)
                    if (carrier == null && a.State == AntState.FetchingWater && a.OrderSlot >= 0 && a.SeekPathLength > 3 && !a.SeekArrived)
                        carrier = a;
            }
            Assert.IsNotNull(carrier);
            int cell = carrier.SeekNext;
            grid.Set(cell % grid.Width, cell / grid.Width, CellMaterial.Dirt);
            Assert.IsTrue(grid.SetMarked(cell % grid.Width, cell / grid.Width, true));
            foreach (Ant a in colony.Ants)
                if (a.SeekTarget == cell)
                    a.ClearSeek();

            colony.Tick(Dt);

            Assert.AreEqual(0, colony.ObstacleSwaps, "Sem dona, não há troca.");
            Assert.AreNotEqual(AntState.Digging, carrier.State, "E ela não cava o bloco de ninguém.");
        }

        /// <summary>
        /// Segunda função (KI-033, 2026-10-20): sem trabalho do papel, a formiga vai à seção mais desatendida que consegue
        /// atender. Um nutriente pronto (rainha) e muita terra marcada: uma só vai ao nutriente (a falta dele acaba) e as
        /// outras ficam com a escavação.
        /// </summary>
        [Test]
        public void AntsWithoutRoleWork_TakeTheMostNeededSection()
        {
            PixelGrid grid = MakeWorld();
            AntColony colony = MakeColony(grid);
            foreach (Ant a in colony.Ants)
                a.Role = AntRole.Cutter; // sem planta no mapa: a cortadeira não tem trabalho do papel
            colony.Tick(Dt); // campos montados, nada a fazer
            for (int x = 30; x <= 44; x++)
            for (int y = 4; y <= 8; y++)
                Assert.IsTrue(grid.SetMarked(x, y, true));
            Assert.IsTrue(grid.SetNutrient(65, 1, true));
            // 1º passo: a demanda da escavação ainda é a da rodada anterior (zero), então a primeira vai ao nutriente. Depois,
            // cada uma que vai cavar reserva um bloco e a seguinte espera a rodada de campos sem ele: uma por passo.
            int mostFeeding = 0, dig = 0;
            for (int i = 0; i < 20; i++)
            {
                colony.Tick(Dt);
                int feed = 0;
                dig = 0;
                foreach (Ant a in colony.Ants)
                {
                    if (a.SecondFunction == (int)WorkSection.Dig)
                        dig++;
                    else if (a.SecondFunction == (int)WorkSection.QueenFeed)
                        feed++;
                }
                mostFeeding = Mathf.Max(mostFeeding, feed);
            }
            Assert.Greater(colony.SectionWanted(WorkSection.Dig), 1);
            Assert.AreEqual(1, mostFeeding, "Só uma vai ao único nutriente.");
            Assert.GreaterOrEqual(dig, colony.Ants.Count - 1, "As outras ficam com a escavação.");
        }

        /// <summary>
        /// Pedido do desenvolvedor em 2026-10-19: encher o depósito de água não conta no limite da seção Jardim. Com o Jardim
        /// limitado a 0, as formigas enchem o depósito mesmo assim, e o painel as mostra na seção do depósito de água
        /// (2026-10-24), não como sem trabalho.
        /// </summary>
        [Test]
        public void MapWater_FillsTheWaterDepot_EvenWithTheGardenSectionAtItsLimit()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Water, 40);
            for (int x = 82; x <= 85; x++)
                Assert.IsTrue(grid.SetWater(x, 1, true));
            AntColony colony = MakeColony(grid);
            colony.Work.SetLimit(WorkSection.Garden, 0);

            var census = new ColonyCensus();
            int maxStocking = 0;
            Assert.IsTrue(TickUntil(colony, () =>
            {
                colony.TakeCensus(census);
                maxStocking = Mathf.Max(maxStocking, census.Sections[(int)WorkSection.WaterDepot]);
                Assert.AreEqual(0, census.Sections[(int)WorkSection.Garden], "Encher o depósito não ocupa a seção Jardim.");
                return CountStoredIn(grid, StorageKind.Water, 40) == 4;
            }, 120f), "Com o Jardim no limite, a água do mapa ainda vai ao depósito de água.");
            Assert.Greater(maxStocking, 0, "O painel conta quem enche o depósito na seção dele.");
        }

        /// <summary>
        /// Pedido do desenvolvedor em 2026-10-24: cada depósito tem a sua seção no painel. Parada, ninguém o enche; com limite,
        /// no máximo esse tanto de formigas leva itens a ele ao mesmo tempo.
        /// </summary>
        [Test]
        public void TheWaterDepotSection_StopsAndCapsWhoFillsIt()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Water, 40);
            for (int x = 82; x <= 85; x++)
                Assert.IsTrue(grid.SetWater(x, 1, true));
            AntColony colony = MakeColony(grid);
            colony.Work.SetEnabled(WorkSection.WaterDepot, false);
            for (float t = 0f; t < 40f; t += 0.1f)
            {
                colony.Tick(0.1f);
                Assert.AreEqual(0, colony.WorkersIn(WorkSection.WaterDepot), "Parada, ninguém vai encher o depósito.");
            }
            Assert.AreEqual(0, CountStoredIn(grid, StorageKind.Water, 40));

            colony.Work.SetEnabled(WorkSection.WaterDepot, true);
            colony.Work.SetLimit(WorkSection.WaterDepot, 1);
            Assert.IsTrue(TickUntil(colony, () =>
            {
                Assert.LessOrEqual(colony.WorkersIn(WorkSection.WaterDepot), 1, "Com limite 1, uma formiga por vez.");
                return CountStoredIn(grid, StorageKind.Water, 40) >= 2;
            }, 120f), "Religada com limite, a água continua chegando ao depósito.");
        }

        [Test]
        public void ItemsOfAnErasedDepot_AreCarriedToTheNewOne()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Food, 20);
            Assert.IsTrue(grid.SetStored(21, 1, StoredItems.Of(CellMaterial.PlantLeaf)));
            Assert.IsTrue(grid.SetStored(23, 2, StoredItems.Of(CellMaterial.PlantGrass)));
            PaintDepot(grid, StorageKind.Food, 20, paint: false);
            PaintDepot(grid, StorageKind.Food, 30);
            AntColony colony = MakeColony(grid);

            Assert.IsTrue(TickUntil(colony, () => CountStoredIn(grid, StorageKind.Food, 30) == 2, 120f),
                          "Os itens soltos vão ao depósito do tipo deles.");
            Assert.AreEqual(0, colony.LooseItems);
        }

        /// <summary>
        /// Relato de 2026-10-19: com centenas de folhas soltas fora do depósito e uma vaga só, cada folha chamava uma formiga;
        /// as que chegavam depois ficavam com a folha na mão para sempre. Só sai quem cabe no destino.
        /// </summary>
        [Test]
        public void LoosePlants_OnlyAsManyAntsGoAsTheDepotHasRoom_AndNobodyKeepsALeafForever()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Food, 20);
            for (int x = 20; x <= 26; x++)
            for (int y = 1; y <= 3; y++)
            {
                if (x != 26 || y != 1)
                    Assert.IsTrue(grid.SetStored(x, y, StoredItems.Of(CellMaterial.PlantLeaf)));
            }
            for (int x = 30; x <= 39; x++)
                Assert.IsTrue(grid.SetStored(x, 1, StoredItems.Of(CellMaterial.PlantLeaf)));
            AntColony colony = MakeColony(grid);
            for (int i = 0; i < 12; i++)
                colony.Spawn(AntRole.Worker);

            int mostAtOnce = 0;
            var since = new System.Collections.Generic.Dictionary<Ant, float>();
            float longestHold = 0f;
            for (float t = 0f; t < 90f; t += Dt)
            {
                colony.Tick(Dt);
                int going = 0;
                foreach (Ant ant in colony.Ants)
                {
                    bool loose = ant.State == AntState.TakingStored && ant.StoreUse == StoreUse.Loose;
                    bool plant = CellMaterials.IsPlant(ant.Carried);
                    going += loose || plant ? 1 : 0;
                    if (!plant)
                    {
                        since.Remove(ant);
                        continue;
                    }
                    if (!since.ContainsKey(ant))
                        since[ant] = t;
                    longestHold = Mathf.Max(longestHold, t - since[ant]);
                }
                mostAtOnce = Mathf.Max(mostAtOnce, going);
            }

            Assert.AreEqual(21, CountStoredIn(grid, StorageKind.Food, 20), "A vaga do depósito foi preenchida.");
            Assert.LessOrEqual(mostAtOnce, 1, "Uma vaga chama uma formiga, não uma por folha solta.");
            Assert.Less(longestHold, 30f, "Ninguém fica com a folha na mão sem destino.");
        }

        /// <summary>Planta na mão sem destino (canteiros sem pedido, sem depósito): a formiga a larga no chão em vez de segurá-la.</summary>
        [Test]
        public void PlantInHand_WithNowhereToGo_IsDroppedAsLooseDebris()
        {
            PixelGrid grid = MakeWorld();
            grid.Set(70, 1, CellMaterial.Spoil);
            Assert.IsTrue(grid.SetBed(70, 1, BedStage.Planted));
            AntColony colony = MakeColony(grid);
            Ant ant = colony.Spawn(AntRole.Worker);
            ant.PickUp(CellMaterial.PlantLeaf, 128);
            ant.State = AntState.Carrying;

            Assert.IsTrue(TickUntil(colony, () => ant.CarriedCount == 0, 15f), "A formiga largou a folha.");
            Assert.AreEqual(1, grid.PlantDebrisCount, "A folha ficou no chão, para quando algum canteiro pedir.");
        }

        [Test]
        public void DepotsAndStoredItems_SurviveSaveAndLoad()
        {
            PixelGrid grid = MakeWorld();
            PaintDepot(grid, StorageKind.Food, 20);
            PaintDepot(grid, StorageKind.Water, 40);
            Assert.IsTrue(grid.SetStored(21, 1, StoredItems.Of(CellMaterial.PlantLeaf), 77));
            Assert.IsTrue(grid.SetStored(41, 2, StoredItems.Water));
            AntColony colony = MakeColony(grid);
            var rain = new RainSimulation(new RainSettings(), 3);
            var water = new WaterSimulation(new WaterSettings(), grid, 3);
            GameSnapshot snapshot = GameSnapshot.Capture(grid, colony, rain, water, 0, 3, 10.0);
            var stream = new MemoryStream();
            snapshot.Write(stream);
            stream.Position = 0;
            GameSnapshot read = GameSnapshot.Read(stream);

            var loadedGrid = new PixelGrid(grid.Width, grid.Height);
            for (int x = 0; x < grid.Width; x++)
                loadedGrid.SetSurfaceY(x, grid.GetSurfaceY(x));
            read.ApplyTo(loadedGrid);

            Assert.AreEqual(StorageKind.Food, loadedGrid.GetStorageRoom(22, 2));
            Assert.AreEqual(StorageKind.Water, loadedGrid.GetStorageRoom(42, 2));
            Assert.AreEqual(grid.StorageRoomCount(StorageKind.Food), loadedGrid.StorageRoomCount(StorageKind.Food));
            Assert.AreEqual(StoredItems.Of(CellMaterial.PlantLeaf), loadedGrid.GetStored(21, 1));
            Assert.AreEqual(77, loadedGrid.GetStoredShade(21, 1));
            Assert.AreEqual(StoredItems.Water, loadedGrid.GetStored(41, 2));
        }
    }
}
