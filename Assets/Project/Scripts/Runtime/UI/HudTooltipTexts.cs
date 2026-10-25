using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Texts;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Textos das dicas do HUD: o que cada botão faz e, nas seções (cartões e salas), como está a produção ou o que falta
    /// para ela começar. Lê o estado da colônia na hora de mostrar; as chaves <c>tip.*</c> estão em <c>LocalizationCatalog</c>.
    /// </summary>
    public static class HudTooltipTexts
    {
        // Índices de AntFarmGame.BrushLabels (os mesmos de AntFarmGame.BrushMode).
        const int BrushDig = 0, BrushBuild = 1, BrushQueenRoom = 2, BrushNutrientRoom = 3, BrushNursery = 4, BrushWaste = 5,
            BrushFinger = 6, BrushDew = 7, BrushFoodStore = 8, BrushBlockStore = 9, BrushWaterStore = 10, BrushGlowMushroom = 11,
            BrushFarm = 12, BrushSandStore = 13, BrushMushroomStore = 14, BrushLitterStore = 15, BrushNectarStore = 16, BrushOreStore = 17, BrushSmelter = 18, BrushForge = 19, BrushArmory = 20, BrushWardrobe = 21,
            BrushWasteLeaves = 22, BrushWasteDirt = 23, BrushWasteNutrient = 24;

        static string Join(string description, string status) =>
            string.IsNullOrEmpty(status) ? description : description + "\n\n" + status;

        /// <summary>Dica do pincel; nos ambientes, com a área usada e as salas (limite do KI-043) no fim.</summary>
        public static string Brush(AntFarmGame game, int index)
        {
            string text = BrushDescription(game, index);
            AntColony colony = game.Colony;
            if (text == null || colony == null || !game.IsBrushUnlocked(index) || !AntFarmGame.TryGetPaintRoom(index, out PaintRoom room))
                return text;
            PaintBudget budget = colony.PaintBudget;
            string area = Loc.T("tip.room.area", budget.Used(room), budget.Limit(room), budget.Rooms(room), budget.MaxRooms);
            if (budget.Available(room) <= 0)
                area += " " + Loc.T("tip.room.area.full");
            return Join(text, area);
        }

        /// <summary>
        /// Dica do ambiente pintado sob o mouse no mapa: o nome e o mesmo status da dica do pincel (produção ou o que falta),
        /// mais a área usada. O status é da colônia inteira para aquele ambiente, não de uma sala em particular.
        /// </summary>
        public static string PaintedRoom(AntFarmGame game, PaintRoom room)
        {
            AntColony colony = game.Colony;
            if (colony == null)
                return null;
            PixelGrid grid = game.Grid;
            PaintBudget budget = colony.PaintBudget;
            string area = Loc.T("tip.room.area", budget.Used(room), budget.Limit(room), budget.Rooms(room), budget.MaxRooms);
            return Join(Loc.T(RoomNameKey(room)), Join(RoomStatus(colony, grid, room), area));
        }

        static string RoomNameKey(PaintRoom room)
        {
            switch (room)
            {
                case PaintRoom.Queen: return "brush.queenroom";
                case PaintRoom.Nutrient: return "brush.nutrientroom";
                case PaintRoom.Nursery: return "brush.nursery";
                case PaintRoom.Waste: return AntFarmHud.WasteModeKey(Colony.WasteMode.RemainsToLeaves);
                case PaintRoom.WasteLeaves: return AntFarmHud.WasteModeKey(Colony.WasteMode.LeavesToWater);
                case PaintRoom.WasteDirt: return AntFarmHud.WasteModeKey(Colony.WasteMode.DirtToWater);
                case PaintRoom.WasteNutrient: return AntFarmHud.WasteModeKey(Colony.WasteMode.LeavesToNutrient);
                case PaintRoom.Dew: return "brush.dew";
                case PaintRoom.FoodStore: return "brush.foodstore";
                case PaintRoom.BlockStore: return "brush.blockstore";
                case PaintRoom.WaterStore: return "brush.waterstore";
                case PaintRoom.Farm: return "brush.farm";
                case PaintRoom.SandStore: return "brush.sandstore";
                case PaintRoom.MushroomStore: return "brush.mushroomstore";
                case PaintRoom.LitterStore: return "brush.litterstore";
                case PaintRoom.NectarStore: return "brush.nectarstore";
                case PaintRoom.OreStore: return "brush.orestore";
                case PaintRoom.Smelter: return "brush.smelter";
                case PaintRoom.Forge: return "brush.forge";
                case PaintRoom.Armory: return "brush.armory";
                default: return "brush.wardrobe";
            }
        }

        static string RoomStatus(AntColony colony, PixelGrid grid, PaintRoom room)
        {
            switch (room)
            {
                case PaintRoom.Queen: return QueenRoomStatus(colony, grid);
                case PaintRoom.Nutrient: return NutrientRoomStatus(colony, grid);
                case PaintRoom.Nursery: return NurseryStatus(colony, grid);
                case PaintRoom.Waste:
                case PaintRoom.WasteLeaves:
                case PaintRoom.WasteDirt: return WasteStatus(colony, grid);
                case PaintRoom.WasteNutrient: return LeafRoomStatus(colony, grid);
                case PaintRoom.Dew: return DewStatus(colony, grid);
                case PaintRoom.Farm: return FarmStatus(colony, grid);
                default: return DepotStatus(colony, PaintBudget.StorageOf(room));
            }
        }

        static string BrushDescription(AntFarmGame game, int index)
        {
            AntColony colony = game.Colony;
            PixelGrid grid = game.Grid;
            switch (index)
            {
                case BrushDig: return Loc.T("tip.brush.dig");
                case BrushBuild: return Loc.T("tip.brush.build");
                case BrushFinger: return Loc.T("tip.brush.finger");
                case BrushGlowMushroom: return Join(Loc.T("tip.brush.glowmushroom"), MushroomStatus(colony));
                case BrushQueenRoom: return Join(Loc.T("tip.brush.queenroom") + " " + Loc.T("tip.room.paint"), QueenRoomStatus(colony, grid));
                case BrushNutrientRoom: return Join(Loc.T("tip.brush.nutrientroom") + " " + Loc.T("tip.room.paint"), NutrientRoomStatus(colony, grid));
                case BrushNursery: return Join(Loc.T("tip.brush.nursery") + " " + Loc.T("tip.room.paint"), NurseryStatus(colony, grid));
                case BrushWaste:
                case BrushWasteLeaves:
                case BrushWasteDirt:
                    return colony != null && !colony.WasteUnlocked
                        ? Loc.T("tip.brush.waste.locked")
                        : Join(WasteMode(index == BrushWasteLeaves ? Colony.WasteMode.LeavesToWater :
                                         index == BrushWasteDirt ? Colony.WasteMode.DirtToWater : Colony.WasteMode.RemainsToLeaves) +
                               " " + Loc.T("tip.room.paint"), WasteStatus(colony, grid));
                case BrushWasteNutrient:
                    return Join(WasteMode(Colony.WasteMode.LeavesToNutrient) + " " + Loc.T("tip.room.paint"), LeafRoomStatus(colony, grid));
                case BrushDew: return Join(Loc.T("tip.brush.dew") + " " + Loc.T("tip.room.paint"), DewStatus(colony, grid));
                case BrushFoodStore: return Join(Loc.T("tip.brush.foodstore") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Food));
                case BrushBlockStore: return Join(Loc.T("tip.brush.blockstore") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Blocks));
                case BrushWaterStore: return Join(Loc.T("tip.brush.waterstore") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Water));
                case BrushSandStore: return Join(Loc.T("tip.brush.sandstore") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Sand));
                case BrushMushroomStore:
                    return Join(Loc.T("tip.brush.mushroomstore") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Mushroom));
                case BrushLitterStore:
                    return Join(Loc.T("tip.brush.litterstore") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Litter));
                case BrushNectarStore:
                    return Join(Loc.T("tip.brush.nectarstore") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Nectar));
                case BrushOreStore:
                    return colony != null && !colony.OreStoreUnlocked
                        ? Loc.T("tip.brush.orestore.locked")
                        : Join(Loc.T("tip.brush.orestore") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Ore));
                case BrushSmelter:
                    return colony != null && !colony.SmelterUnlocked
                        ? Loc.T("tip.brush.smelter.locked")
                        : Join(Loc.T("tip.brush.smelter") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Smelter));
                case BrushForge:
                    return colony != null && !colony.ForgeUnlocked
                        ? Loc.T("tip.brush.forge.locked")
                        : Join(Loc.T("tip.brush.forge") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Forge));
                case BrushArmory:
                    return colony != null && !colony.ArmoryUnlocked
                        ? Loc.T("tip.brush.armory.locked")
                        : Join(Loc.T("tip.brush.armory") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Armory));
                case BrushWardrobe:
                    return colony != null && !colony.WardrobeUnlocked
                        ? Loc.T("tip.brush.wardrobe.locked")
                        : Join(Loc.T("tip.brush.wardrobe") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Wardrobe));
                case BrushFarm:
                    return colony != null && !colony.FarmUnlocked
                        ? Loc.T("tip.brush.farm.locked")
                        : Join(Loc.T("tip.brush.farm") + " " + Loc.T("tip.room.paint"), FarmStatus(colony, grid));
                default: return null;
            }
        }

        public static string NutrientsCard(AntColony colony, PixelGrid grid)
        {
            if (colony == null)
                return null;
            string balance = Loc.T("tip.card.nutrients.balance", colony.Nutrients, colony.MinNutrientReserve);
            return Join(Loc.T("tip.card.nutrients") + " " + balance, NutrientRoomStatus(colony, grid));
        }

        public static string AntsCard(AntColony colony)
        {
            if (colony == null)
                return null;
            string capacity = Loc.T("tip.card.ants.capacity", colony.Ants.Count, colony.AdultCapacity, colony.Brood.Count, colony.NurserySlots);
            return Join(Loc.T("tip.card.ants") + " " + capacity, BreedingStatus(colony));
        }

        public static string QueenCard(AntColony colony, PixelGrid grid)
        {
            if (colony == null)
                return null;
            return Join(Loc.T("tip.card.queen"), QueenRoomStatus(colony, grid) + "\n" + BreedingStatus(colony));
        }

        public static string Role(AntColony colony, AntRole role)
        {
            string text = Loc.T(RoleKey(role));
            if (colony != null && !colony.Tree.IsRoleUnlocked(role))
                return Join(text, Loc.T("tip.role.locked", QueenTree.UnlockNodeOf(role).Value));
            return Join(text, Loc.T("tip.role.choose"));
        }

        public static string Mode(QueenMode mode) =>
            Loc.T(mode == QueenMode.Automatic ? "tip.mode.automatic" : mode == QueenMode.Specific ? "tip.mode.specific" : "tip.mode.paused");

        /// <summary>Dica de um botão do submenu RESÍDUOS (modo de conversão da área).</summary>
        public static string WasteMode(WasteMode mode) => Loc.T(AntFarmHud.WasteModeKey(mode) + ".tip");

        public static string Stance(QueenStance stance) =>
            Loc.T(stance == QueenStance.Offensive ? "tip.stance.offensive" : "tip.stance.defensive");

        static string RoleKey(AntRole role)
        {
            switch (role)
            {
                case AntRole.Carrier: return "tip.role.carrier";
                case AntRole.Cutter: return "tip.role.cutter";
                case AntRole.Digger: return "tip.role.digger";
                case AntRole.Gardener: return "tip.role.gardener";
                case AntRole.Nurse: return "tip.role.nurse";
                case AntRole.Soldier: return "tip.role.soldier";
                case AntRole.Spitter: return "tip.role.spitter";
                case AntRole.Midden: return "tip.role.midden";
                case AntRole.Farmer: return "tip.role.farmer";
                default: return "tip.role.worker";
            }
        }

        // ------------------------------------------------------------------ produção de cada seção

        static string QueenRoomStatus(AntColony colony, PixelGrid grid)
        {
            if (colony == null)
                return null;
            if (colony.QueenRoomSize == 0)
                return Loc.T(grid.QueenRoomCount == 0 ? "tip.st.queen.paint" : "tip.st.queen.dig");
            if (!colony.QueenInRoom && !colony.QueenCanReachRoom)
                return Loc.T("tip.st.queen.path");
            return Loc.T(colony.QueenInRoom ? "tip.st.queen.ok" : "tip.st.queen.going", colony.QueenRoomSize);
        }

        static string NutrientRoomStatus(AntColony colony, PixelGrid grid)
        {
            if (colony == null)
                return null;
            if (grid.NutrientRoomCount == 0 && grid.NutrientCount == 0)
                return Loc.T("tip.st.nutrient.paint");
            if (grid.BedCount == 0)
                return Loc.T(colony.BedSpots > 0 ? "tip.st.nutrient.nobeds" : "tip.st.nutrient.nospot", grid.NutrientCount);
            string text = Loc.T("tip.st.nutrient.ok", grid.BedCount, colony.BedsAwaitingPlant, colony.BedsAwaitingWater,
                grid.GrowingBedCount, grid.NutrientCount);
            return colony.HarvestPaused ? text + " " + Loc.T("tip.st.nutrient.paused") : text;
        }

        static string NurseryStatus(AntColony colony, PixelGrid grid)
        {
            if (colony == null)
                return null;
            if (grid.NurseryRoomCount == 0)
                return Loc.T("tip.st.nursery.paint");
            if (colony.NurserySlots == 0)
                return Loc.T("tip.st.nursery.dig");
            return Loc.T("tip.st.nursery.ok", colony.Brood.Count, colony.NurserySlots);
        }

        /// <summary>Estado de um depósito: sem pintura, sem vaga nem item (falta cavar) ou guardado e vagas.</summary>
        static string DepotStatus(AntColony colony, StorageKind kind)
        {
            if (colony == null)
                return null;
            if (!colony.HasDepot(kind))
                return Loc.T("tip.st.depot.paint");
            if (colony.DepotFree(kind) == 0 && colony.DepotStock(kind) == 0)
                return Loc.T("tip.st.depot.nospot");
            return Loc.T("tip.st.depot.ok", colony.DepotStock(kind), colony.DepotFree(kind));
        }

        /// <summary>Estado da fazenda de pulgões (mesmas regras de <c>AntFarmGame.FarmStatus</c>, no painel F2).</summary>
        static string FarmStatus(AntColony colony, PixelGrid grid)
        {
            if (colony == null)
                return null;
            if (grid.FarmRoomCount == 0)
                return Loc.T("tip.st.farm.paint");
            if (colony.StallCount == 0)
                return Loc.T("tip.st.farm.nospot", AntColony.StallWidth);
            if (colony.Aphids.Count == 0)
                return Loc.T("tip.st.farm.breeding", Mathf.CeilToInt(Mathf.Max(0f, colony.SecondsToNextAphid)));
            string status = Loc.T("tip.st.farm.ok", colony.Aphids.Count, colony.AphidCap, colony.StallCount, colony.TroughsWaiting);
            if (!colony.Tree.IsRoleUnlocked(AntRole.Farmer) || colony.CountOf(AntRole.Farmer) == 0)
                status += " " + Loc.T("tip.st.farm.nofarmer");
            return status;
        }

        static string MushroomStatus(AntColony colony) =>
            colony == null ? null : Loc.T("tip.st.mushroom.stock", colony.MushroomItems);

        static string WasteStatus(AntColony colony, PixelGrid grid)
        {
            if (colony == null)
                return null;
            if (grid.WasteRoomCount == 0)
                return Loc.T("tip.st.waste.paint");
            if (colony.WasteSpots == 0 && colony.WasteCells == 0)
                return Loc.T("tip.st.waste.nospot");
            return Loc.T("tip.st.waste.ok", colony.WasteSpots, colony.WasteCells, colony.WasteDeposited);
        }

        static string LeafRoomStatus(AntColony colony, PixelGrid grid)
        {
            if (colony == null)
                return null;
            if (grid.GetWasteKindCount((byte)Colony.WasteMode.LeavesToNutrient) == 0)
                return Loc.T("tip.st.leafroom.paint");
            return Loc.T("tip.st.leafroom.ok", colony.WasteSpotsOf(Colony.WasteMode.LeavesToNutrient), colony.LeavesToNutrients);
        }

        static string DewStatus(AntColony colony, PixelGrid grid)
        {
            if (colony == null)
                return null;
            if (grid.DewRoomCount == 0)
                return Loc.T("tip.st.dew.paint");
            if (colony.DewSoilSpots > 0)
                return Loc.T("tip.st.dew.soil", colony.DewSoilSpots);
            if (colony.DewLeafSpots > 0)
                return Loc.T("tip.st.dew.leaf", colony.DewLeafSpots);
            return colony.DewActive ? Loc.T("tip.st.dew.ok", colony.DewDrops) : Loc.T("tip.st.dew.nospot");
        }

        /// <summary>Por que a rainha está (ou não) pondo ovos, em frase corrida (a do cartão da rainha usa maiúsculas).</summary>
        static string BreedingStatus(AntColony colony)
        {
            switch (colony.Breeding)
            {
                case BreedingState.Paused: return Loc.T("tip.st.breed.paused");
                case BreedingState.WaitingQueenRoom:
                    return Loc.T(colony.QueenRoomSize == 0 ? "tip.st.breed.noqueenroom" : "tip.st.breed.queenout");
                case BreedingState.WaitingNursery:
                    return Loc.T(colony.NurserySlots == 0 ? "tip.st.breed.nonursery" : "tip.st.breed.nurseryfull");
                case BreedingState.WaitingExpansion: return Loc.T("tip.st.breed.expand");
                case BreedingState.WaitingResources: return Loc.T("tip.st.breed.resources", colony.MinNutrientReserve);
                default: return Loc.T("tip.st.breed.next", UnityEngine.Mathf.CeilToInt(colony.Brood.SecondsToNextLay));
            }
        }
    }
}
