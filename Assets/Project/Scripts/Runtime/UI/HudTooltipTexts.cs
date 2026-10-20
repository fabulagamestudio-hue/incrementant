using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Texts;
using Fabula.Incrementant.World;

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
            BrushFinger = 6, BrushDew = 7, BrushFoodStore = 8, BrushBlockStore = 9, BrushWaterStore = 10;

        static string Join(string description, string status) =>
            string.IsNullOrEmpty(status) ? description : description + "\n\n" + status;

        public static string Brush(AntFarmGame game, int index)
        {
            AntColony colony = game.Colony;
            PixelGrid grid = game.Grid;
            switch (index)
            {
                case BrushDig: return Loc.T("tip.brush.dig");
                case BrushBuild: return Loc.T("tip.brush.build");
                case BrushFinger: return Loc.T("tip.brush.finger");
                case BrushQueenRoom: return Join(Loc.T("tip.brush.queenroom") + " " + Loc.T("tip.room.paint"), QueenRoomStatus(colony, grid));
                case BrushNutrientRoom: return Join(Loc.T("tip.brush.nutrientroom") + " " + Loc.T("tip.room.paint"), NutrientRoomStatus(colony, grid));
                case BrushNursery: return Join(Loc.T("tip.brush.nursery") + " " + Loc.T("tip.room.paint"), NurseryStatus(colony, grid));
                case BrushWaste:
                    return colony != null && !colony.WasteUnlocked
                        ? Loc.T("tip.brush.waste.locked")
                        : Join(Loc.T("tip.brush.waste") + " " + Loc.T("tip.room.paint"), WasteStatus(colony, grid));
                case BrushDew: return Join(Loc.T("tip.brush.dew") + " " + Loc.T("tip.room.paint"), DewStatus(colony, grid));
                case BrushFoodStore: return Join(Loc.T("tip.brush.foodstore") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Food));
                case BrushBlockStore: return Join(Loc.T("tip.brush.blockstore") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Blocks));
                case BrushWaterStore: return Join(Loc.T("tip.brush.waterstore") + " " + Loc.T("tip.room.paint"), DepotStatus(colony, StorageKind.Water));
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
