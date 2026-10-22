using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Salas construídas (REQ-058): os slots de reboco em contato com o vão (piso, teto, lados, quinas e a parede dentro da área
    /// pintada), as formigas rebocando com terra de fora sem tirar células da sala, a sala pronta, o reboco desfeito, o save e
    /// o desenho (linhas do bloco e do fundo no atlas).
    /// </summary>
    public sealed class RoomLiningTests
    {
        const int Surface = 20;
        // Berçário cavado: vão de x 50..57, y 5..8. O túnel (y 5..7) chega pela esquerda, até x = 49.
        const int RoomLeft = 50, RoomRight = 57, RoomBottom = 5, RoomTop = 8;
        // Dentro da área pintada: um torrão de terra que não foi cavado (parede interna) e terra fofa no chão (fica como está).
        static readonly Vector2Int InnerWall = new Vector2Int(54, 5);
        static readonly Vector2Int LooseSoil = new Vector2Int(52, 5);
        readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _objects)
                Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        /// <summary>
        /// Solo de terra com piso de rocha, entrada em x 39..41, túnel em y 5..7 até x = 49, o berçário pintado e cavado (menos
        /// <see cref="InnerWall"/>, com <see cref="LooseSoil"/> no chão) e uma pedra na parede direita (58, 6). Lá fora, sobre o
        /// solo, terra solta para a obra.
        /// </summary>
        static PixelGrid MakeRoomWorld()
        {
            var grid = new PixelGrid(80, 30);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, Surface);
                for (int y = 1; y <= Surface; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int y = 5; y <= Surface; y++)
            for (int x = 39; x <= 41; x++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 39; x <= 49; x++)
            for (int y = 5; y <= 7; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = RoomLeft; x <= RoomRight; x++)
            for (int y = RoomBottom; y <= RoomTop; y++)
            {
                grid.Set(x, y, CellMaterial.Empty);
                Assert.IsTrue(grid.SetNurseryRoom(x, y, true));
            }
            grid.Set(InnerWall.x, InnerWall.y, CellMaterial.Dirt);
            grid.Set(LooseSoil.x, LooseSoil.y, CellMaterial.Spoil);
            grid.Set(58, 6, CellMaterial.Rock);
            for (int x = 46; x <= 59; x++)
            for (int y = Surface + 1; y <= Surface + 2; y++)
                grid.Set(x, y, CellMaterial.Spoil);
            return grid;
        }

        static AntColony MakeColony(PixelGrid grid)
        {
            var colony = new AntColony(grid, new ColonySettings(), new TerrainLayout(40, new Vector2Int(40, 6), 60, 70), seed: 7);
            foreach (Ant ant in colony.Ants)
                ant.Role = AntRole.Worker;
            return colony;
        }

        static void Run(PixelGrid grid, AntColony colony, System.Func<bool> done, int seconds)
        {
            var sim = new GranularSimulation();
            for (int tick = 0; tick < 60 * seconds && !done(); tick++)
            {
                sim.Step(grid);
                colony.Tick(1f / 60f);
            }
        }

        static IEnumerable<Vector2Int> ExpectedSlots()
        {
            for (int x = RoomLeft - 1; x <= RoomRight + 1; x++)
            {
                yield return new Vector2Int(x, RoomBottom - 1); // piso e quinas de baixo
                yield return new Vector2Int(x, RoomTop + 1); // teto e quinas de cima
            }
            yield return new Vector2Int(RoomLeft - 1, RoomTop); // parede esquerda acima do túnel
            for (int y = RoomBottom; y <= RoomTop; y++)
            {
                if (y != 6)
                    yield return new Vector2Int(RoomRight + 1, y); // parede direita, menos a pedra
            }
            yield return InnerWall; // parede dentro da área pintada
        }

        static bool IsInsideSolid(int x, int y) => x == InnerWall.x && y == InnerWall.y || x == LooseSoil.x && y == LooseSoil.y;

        [Test]
        public void Slots_AreTheEarthTouchingTheRoom_InsideAndAround_NotTheTunnelTheRockOrTheLooseSoil()
        {
            PixelGrid grid = MakeRoomWorld();
            AntColony colony = MakeColony(grid);
            colony.Tick(1f / 60f);

            var expected = new HashSet<Vector2Int>(ExpectedSlots());
            foreach (Vector2Int cell in expected)
                Assert.IsTrue(colony.HasLiningSlot(cell.x, cell.y), $"({cell.x}, {cell.y}) deveria ser slot.");
            Assert.AreEqual(expected.Count, colony.LiningPending);
            Assert.IsFalse(colony.HasLiningSlot(58, 6), "Pedra não é rebocada.");
            Assert.IsFalse(colony.HasLiningSlot(LooseSoil.x, LooseSoil.y), "Terra fofa dentro da sala (canteiro, teto do orvalho) fica como está.");
            Assert.IsFalse(colony.HasLiningSlot(RoomLeft - 2, RoomTop + 1), "Terra que não encosta no vão não é slot.");
            Assert.IsFalse(grid.IsRoomFinished(55, 6), "Sem reboco, a sala não está pronta.");
        }

        [Test]
        public void Ants_LineTheRoomWithOutsideSoil_KeepingTheRoomCells_AndTheRoomIsFinished()
        {
            PixelGrid grid = MakeRoomWorld();
            AntColony colony = MakeColony(grid);
            colony.Tick(1f / 60f);
            int nurserySlots = colony.NurserySlots;
            Assert.Greater(colony.LiningPending, 0);

            Run(grid, colony, () => colony.LiningPending == 0 && grid.IsRoomFinished(54, 6), 240);

            byte kind = RoomLining.KindOf(PaintRoom.Nursery);
            foreach (Vector2Int cell in ExpectedSlots())
            {
                Assert.AreEqual(kind, grid.GetLining(cell.x, cell.y), $"({cell.x}, {cell.y}) deveria estar rebocada.");
                Assert.IsTrue(grid.IsSolid(cell.x, cell.y));
            }
            Assert.AreEqual(0, grid.GetLining(LooseSoil.x, LooseSoil.y));
            Assert.AreEqual(CellMaterial.Spoil, grid.Get(LooseSoil.x, LooseSoil.y));
            for (int x = RoomLeft; x <= RoomRight; x++)
            for (int y = RoomBottom; y <= RoomTop; y++)
            {
                if (!IsInsideSolid(x, y))
                    Assert.IsFalse(grid.IsSolid(x, y), $"O vão da sala não perde células ({x}, {y}).");
                Assert.IsTrue(grid.IsRoomFinished(x, y), $"Com o reboco completo a sala fica pronta ({x}, {y}).");
            }
            Assert.AreEqual(nurserySlots, colony.NurserySlots, "Só visual: as vagas do berçário não mudam.");
            Assert.AreEqual(ExpectedSlots().Count(), colony.CellsLined);
        }

        /// <summary>
        /// Construção antes das automatizações (decisão de 2026-10-22): com uma sala de nutrientes pedindo terra para canteiros,
        /// as salas são rebocadas mesmo assim.
        /// </summary>
        [Test]
        public void Lining_DoesNotWaitForTheBeds()
        {
            PixelGrid grid = MakeRoomWorld();
            for (int x = 43; x <= 48; x++)
            for (int y = 5; y <= 7; y++)
                Assert.IsTrue(grid.SetNutrientRoom(x, y, true));
            for (int x = 40; x <= 66; x++)
                grid.Set(x, Surface + 3, CellMaterial.Spoil);
            AntColony colony = MakeColony(grid);

            Run(grid, colony, () => colony.CellsLined > 0 && colony.LiningPending == 0, 300);
            Assert.AreEqual(0, colony.LiningPending, "Todos os slots, do berçário e da sala de nutrientes, são rebocados.");
            Assert.IsTrue(grid.IsRoomFinished(55, 6) && grid.IsRoomFinished(45, 6));
        }

        /// <summary>Com depósito de construção, o reboco usa a terra guardada sem esperar o depósito encher.</summary>
        [Test]
        public void Lining_UsesTheBlockDepot_WithoutWaitingForItToFill()
        {
            PixelGrid grid = MakeRoomWorld();
            for (int x = 43; x <= 48; x++)
            for (int y = 5; y <= 7; y++)
                Assert.IsTrue(grid.SetStorageRoom(x, y, StorageKind.Blocks, true));
            for (int x = 43; x <= 48; x++)
            for (int y = 5; y <= 6; y++)
                Assert.IsTrue(grid.SetStored(x, y, StoredItems.Of(CellMaterial.Dirt), 128));
            AntColony colony = MakeColony(grid);
            colony.Tick(1f / 60f);
            Assert.Greater(colony.DepotFree(StorageKind.Blocks), 0, "O depósito ainda tem vaga.");

            Run(grid, colony, () => colony.CellsLined >= 5, 120);
            Assert.GreaterOrEqual(colony.CellsLined, 5, "O reboco anda com o depósito pela metade.");
        }

        /// <summary>
        /// Reboca na hora todos os slots das salas da grade (sala já construída), pela mesma regra de
        /// <c>AntColony.IsLiningSlot</c>. Para os testes das automatizações (canteiros, orvalho, depósitos, resíduos): a
        /// construção vem antes delas (decisão de 2026-10-22), então elas só recebem terra com as salas prontas.
        /// </summary>
        public static void LineAllRooms(PixelGrid grid)
        {
            RectInt area = grid.Active;
            var slots = new List<(int x, int y, byte kind)>();
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
            {
                if (grid.IsSolid(x, y) || !PaintBudget.TryGetRoomAt(grid, x, y, out PaintRoom room))
                    continue;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (!grid.InBounds(nx, ny) || ny > grid.GetSurfaceY(nx) || grid.IsMarked(nx, ny) || grid.GetLining(nx, ny) != 0)
                        continue;
                    CellMaterial material = grid.Get(nx, ny);
                    bool inside = PaintBudget.TryGetRoomAt(grid, nx, ny, out PaintRoom own);
                    if (!RoomLining.CanLine(material) || inside && (material == CellMaterial.Spoil || grid.GetBedStage(nx, ny) != BedStage.None))
                        continue;
                    slots.Add((nx, ny, RoomLining.KindOf(inside ? own : room)));
                }
            }
            foreach ((int x, int y, byte kind) in slots)
            {
                grid.Set(x, y, CellMaterial.PackedSpoil);
                grid.SetLining(x, y, kind);
            }
        }

        [Test]
        public void LiningLost_ReopensTheSlot_AndTheRoomIsNoLongerFinished()
        {
            PixelGrid grid = MakeRoomWorld();
            foreach (Vector2Int cell in ExpectedSlots())
            {
                grid.Set(cell.x, cell.y, CellMaterial.PackedSpoil);
                Assert.IsTrue(grid.SetLining(cell.x, cell.y, RoomLining.KindOf(PaintRoom.Nursery)));
            }
            AntColony colony = MakeColony(grid);
            colony.Tick(1f / 60f);
            Assert.AreEqual(0, colony.LiningPending);
            Assert.IsTrue(grid.IsRoomFinished(54, 6));

            // A água amolece a terra (vira lama): a peça rebocada se desfaz e o slot volta a pedir terra.
            grid.Set(54, RoomBottom - 1, CellMaterial.Mud);
            Assert.AreEqual(0, grid.GetLining(54, RoomBottom - 1));
            Run(grid, colony, () => colony.HasLiningSlot(54, RoomBottom - 1), 5);
            Assert.IsTrue(colony.HasLiningSlot(54, RoomBottom - 1));
            Assert.IsFalse(grid.IsRoomFinished(54, 6));

            // Marca de escavação dentro da sala também segura a sala pronta (pedra: sem o upgrade, ninguém a tira).
            grid.Set(54, RoomBottom - 1, CellMaterial.PackedSpoil);
            grid.SetLining(54, RoomBottom - 1, RoomLining.KindOf(PaintRoom.Nursery));
            grid.Set(55, RoomTop, CellMaterial.Rock);
            Assert.IsTrue(grid.SetMarked(55, RoomTop, true));
            Run(grid, colony, () => false, 2);
            Assert.IsFalse(grid.IsRoomFinished(54, 6), "Com escavação pendente na sala, ela não está pronta.");
        }

        [Test]
        public void Save_KeepsTheLining()
        {
            PixelGrid grid = MakeRoomWorld();
            grid.Set(54, RoomBottom - 1, CellMaterial.PackedSpoil);
            grid.SetLining(54, RoomBottom - 1, RoomLining.KindOf(PaintRoom.Nursery));
            grid.Set(58, 5, CellMaterial.PackedSand);
            grid.SetLining(58, 5, RoomLining.KindOf(PaintRoom.MushroomStore));
            AntColony colony = MakeColony(grid);
            var rain = new RainSimulation(new RainSettings(), 3);
            var water = new WaterSimulation(new WaterSettings(), grid, 3);

            var stream = new MemoryStream();
            GameSnapshot.Capture(grid, colony, rain, water, 0, 3, 1.0).Write(stream);
            stream.Position = 0;
            GameSnapshot loaded = GameSnapshot.Read(stream);

            PixelGrid grid2 = MakeRoomWorld();
            loaded.ApplyTo(grid2);
            Assert.AreEqual(RoomLining.KindOf(PaintRoom.Nursery), grid2.GetLining(54, RoomBottom - 1));
            Assert.AreEqual(RoomLining.KindOf(PaintRoom.MushroomStore), grid2.GetLining(58, 5));
            Assert.AreEqual(0, grid2.GetLining(55, RoomBottom - 1));
            Assert.AreEqual(CellMaterial.PackedSand, grid2.Get(58, 5));
        }

        [Test]
        public void View_DrawsTheRoomBlockOnTheLining_AndTheRoomBackInAFinishedRoom()
        {
            PixelGrid grid = MakeRoomWorld();
            grid.Set(54, RoomBottom - 1, CellMaterial.PackedSpoil);
            grid.SetLining(54, RoomBottom - 1, RoomLining.KindOf(PaintRoom.Nursery));
            AntFarmView view = MakeView(grid);
            var window = new RectInt(32, 0, 48, 30);

            int nurseryBlock = CellTileArt.RoomBlockRow(RoomStyle.Nursery);
            int nurseryBack = CellTileArt.RoomBackRow(RoomStyle.Nursery);
            Assert.AreEqual(nurseryBlock, TileId(view, grid, window, 54, RoomBottom - 1), "Reboco desenha o bloco do ambiente.");
            Assert.AreEqual((int)CellMaterial.Empty, TileId(view, grid, window, 54, 6), "Sala sem reboco completo: fundo do túnel.");

            grid.SetRoomFinished(54, 6, true);
            Assert.AreEqual(nurseryBack, TileId(view, grid, window, 54, 6), "Sala pronta: fundo do ambiente.");

            grid.Set(54, RoomBottom - 1, CellMaterial.Dirt);
            Assert.AreEqual((int)CellMaterial.Dirt, TileId(view, grid, window, 54, RoomBottom - 1), "Sem reboco, volta à terra.");
        }

        AntFarmView MakeView(PixelGrid grid)
        {
            var go = new GameObject("RoomLiningTest", typeof(SpriteRenderer)) { hideFlags = HideFlags.HideAndDontSave };
            _objects.Add(go);
            var view = go.AddComponent<AntFarmView>();
            var material = new Material(Shader.Find("Incrementant/TerrainTiles")) { hideFlags = HideFlags.HideAndDontSave };
            _objects.Add(material);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_tileMaterial").objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.Initialize(grid);
            return view;
        }

        static int TileId(AntFarmView view, PixelGrid grid, RectInt window, int x, int y)
        {
            view.Render(grid, new List<Ant>(), new List<Vector2Int>(), Vector2Int.zero, 1, false, false, window);
            var texture = (Texture2D)view.transform.Find("TerrainWindow").GetComponent<SpriteRenderer>().sharedMaterial.GetTexture("_CellMaterials");
            byte[] data = texture.GetRawTextureData<byte>().ToArray();
            return data[AntFarmView.TileBytes * ((y - window.y) * window.width + x - window.x)];
        }
    }
}
