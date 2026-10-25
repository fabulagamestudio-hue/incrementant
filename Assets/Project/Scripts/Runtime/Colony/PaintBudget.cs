using System.Collections.Generic;
using Fabula.Incrementant.Upgrades;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Ambiente que o pincel pinta e que tem limite de área (KI-043). Os depósitos têm limites separados, e cada modo da
    /// área de resíduos também: <see cref="Waste"/> é o modo restos em folhas, <see cref="WasteLeaves"/> e <see cref="WasteDirt"/>
    /// vêm no fim para não renumerar os ambientes (o reboco do save guarda o índice).
    /// </summary>
    public enum PaintRoom { Queen, Nutrient, Nursery, Waste, Dew, FoodStore, BlockStore, WaterStore, Farm, SandStore, MushroomStore, LitterStore, NectarStore, OreStore, Smelter, Forge, Armory, Wardrobe, WasteLeaves, WasteDirt, WasteNutrient }

    /// <summary>Por que o pincel não pode pintar uma célula do ambiente.</summary>
    public enum PaintBlock
    {
        None,
        /// <summary>O ambiente já usa todas as células do limite.</summary>
        NoCells,
        /// <summary>A célula começaria uma sala nova (sem vizinha do mesmo ambiente) e o ambiente já tem o máximo de salas.</summary>
        NoRooms,
    }

    /// <summary>
    /// Limite do pincel por ambiente (KI-043): cada ambiente tem um orçamento de células (base de <see cref="ColonySettings"/>
    /// mais as galerias de área, ver <see cref="UpgradeEffects.PaintCellsBonus"/>) dividido entre no máximo
    /// <see cref="ColonySettings.MaxPaintRooms"/> salas não conectadas (vizinhança de 4 lados). Só o pincel do jogador
    /// consulta isto: save, vitrine e testes pintam direto na grade, e um save com mais área que o limite continua como está
    /// (o pincel só volta a pintar depois de apagar o excesso). Apagar é sempre livre.
    /// </summary>
    public sealed class PaintBudget
    {
        /// <summary>Ambientes com limite de área: todos os valores de <see cref="PaintRoom"/> (antes fixo em 11, o que deixava os depósitos novos de fora).</summary>
        public static readonly int RoomCount = System.Enum.GetValues(typeof(PaintRoom)).Length;

        readonly PixelGrid _grid;
        readonly ColonySettings _settings;
        readonly UpgradeEffects _effects;

        // Salas contadas por ambiente, válidas enquanto as versões da grade não mudam.
        readonly int[] _rooms = new int[RoomCount];
        readonly int[] _roomsMarkVersion = new int[RoomCount];
        readonly int[] _roomsNutrientVersion = new int[RoomCount];
        readonly bool[] _roomsValid = new bool[RoomCount];
        readonly HashSet<int> _visited = new HashSet<int>();
        readonly Stack<int> _stack = new Stack<int>();

        public PaintBudget(PixelGrid grid, ColonySettings settings, UpgradeEffects effects)
        {
            _grid = grid;
            _settings = settings;
            _effects = effects;
        }

        public int MaxRooms => _settings.MaxPaintRooms + _effects.PaintRoomsBonus;

        public int Limit(PaintRoom room) => _settings.PaintCells(room) + _effects.PaintCellsBonus(room);

        public int Used(PaintRoom room)
        {
            switch (room)
            {
                case PaintRoom.Queen: return _grid.QueenRoomCount;
                case PaintRoom.Nutrient: return _grid.NutrientRoomCount;
                case PaintRoom.Nursery: return _grid.NurseryRoomCount;
                case PaintRoom.Waste:
                case PaintRoom.WasteLeaves:
                case PaintRoom.WasteDirt:
                case PaintRoom.WasteNutrient: return _grid.GetWasteKindCount(WasteKindOf(room));
                case PaintRoom.Dew: return _grid.DewRoomCount;
                case PaintRoom.Farm: return _grid.FarmRoomCount;
                default: return _grid.StorageRoomCount(StorageOf(room));
            }
        }

        public int Available(PaintRoom room) => Mathf.Max(0, Limit(room) - Used(room));

        /// <summary>Parte do limite ainda livre (0 a 1): o preenchimento da barra no botão do ambiente.</summary>
        public float AvailableFraction(PaintRoom room)
        {
            int limit = Limit(room);
            return limit <= 0 ? 0f : Mathf.Clamp01(Available(room) / (float)limit);
        }

        /// <summary>Salas não conectadas do ambiente agora (componentes de 4 lados). Recontada só quando a grade muda.</summary>
        public int Rooms(PaintRoom room)
        {
            int k = (int)room;
            if (_roomsValid[k] && _roomsMarkVersion[k] == _grid.MarkVersion && _roomsNutrientVersion[k] == _grid.NutrientVersion)
                return _rooms[k];
            _rooms[k] = CountRooms(room);
            _roomsMarkVersion[k] = _grid.MarkVersion;
            _roomsNutrientVersion[k] = _grid.NutrientVersion;
            _roomsValid[k] = true;
            return _rooms[k];
        }

        /// <summary>
        /// O pincel (círculo de raio <paramref name="radius"/> em torno da célula) pode pintar o ambiente? Sem área livre,
        /// não; e um traço que não encosta em nenhuma sala do ambiente abre uma sala nova, que só cabe abaixo do máximo. O
        /// círculo é decidido inteiro (e não célula a célula, que veria a borda de cada linha como uma sala sozinha); quem
        /// pinta ainda confere <see cref="Available"/> a cada célula, que o traço pode esgotar no meio.
        /// </summary>
        public PaintBlock Check(PaintRoom room, int x, int y, int radius = 0)
        {
            if (Available(room) <= 0)
                return PaintBlock.NoCells;
            if (Rooms(room) >= MaxRooms && StartsRoom(room, x, y, radius))
                return PaintBlock.NoRooms;
            return PaintBlock.None;
        }

        /// <summary>Nenhuma célula do círculo, nem vizinha (4 lados) dele, é do ambiente: pintado, ele abre uma sala nova.</summary>
        public bool StartsRoom(PaintRoom room, int x, int y, int radius = 0)
        {
            int r2 = radius * radius;
            for (int dy = -radius - 1; dy <= radius + 1; dy++)
            for (int dx = -radius - 1; dx <= radius + 1; dx++)
            {
                // O círculo crescido de um passo: cada célula dele ou é do círculo ou tem lado com uma que é.
                if (!InDiskOrBeside(dx, dy, r2))
                    continue;
                if (IsRoom(_grid, room, x + dx, y + dy))
                    return false;
            }
            return true;
        }

        static bool InDiskOrBeside(int dx, int dy, int r2) =>
            dx * dx + dy * dy <= r2 || (dx - 1) * (dx - 1) + dy * dy <= r2 || (dx + 1) * (dx + 1) + dy * dy <= r2 ||
            dx * dx + (dy - 1) * (dy - 1) <= r2 || dx * dx + (dy + 1) * (dy + 1) <= r2;

        public static bool IsRoom(PixelGrid grid, PaintRoom room, int x, int y)
        {
            switch (room)
            {
                case PaintRoom.Queen: return grid.IsQueenRoom(x, y);
                case PaintRoom.Nutrient: return grid.IsNutrientRoom(x, y);
                case PaintRoom.Nursery: return grid.IsNurseryRoom(x, y);
                case PaintRoom.Waste:
                case PaintRoom.WasteLeaves:
                case PaintRoom.WasteDirt:
                case PaintRoom.WasteNutrient: return grid.IsWasteRoom(x, y) && grid.GetWasteKind(x, y) == WasteKindOf(room);
                case PaintRoom.Dew: return grid.IsDewRoom(x, y);
                case PaintRoom.Farm: return grid.IsFarmRoom(x, y);
                default: return grid.GetStorageRoom(x, y) == StorageOf(room);
            }
        }

        /// <summary>Modo de conversão (<see cref="WasteMode"/> como byte) da área de resíduos de um ambiente <c>Waste*</c>.</summary>
        public static byte WasteKindOf(PaintRoom room) =>
            (byte)(room == PaintRoom.WasteLeaves ? WasteMode.LeavesToWater : room == PaintRoom.WasteDirt ? WasteMode.DirtToWater
                : room == PaintRoom.WasteNutrient ? WasteMode.LeavesToNutrient : WasteMode.RemainsToLeaves);

        /// <summary>Ambiente da área de resíduos de um modo (<see cref="WasteKindOf"/> ao contrário).</summary>
        public static PaintRoom WasteRoomOf(byte kind) =>
            kind == (byte)WasteMode.LeavesToWater ? PaintRoom.WasteLeaves : kind == (byte)WasteMode.DirtToWater ? PaintRoom.WasteDirt
                : kind == (byte)WasteMode.LeavesToNutrient ? PaintRoom.WasteNutrient : PaintRoom.Waste;

        /// <summary>Ambiente pintado na célula, se houver (as salas nunca se sobrepõem).</summary>
        public static bool TryGetRoomAt(PixelGrid grid, int x, int y, out PaintRoom room)
        {
            room = default;
            if (grid.IsQueenRoom(x, y)) room = PaintRoom.Queen;
            else if (grid.IsNutrientRoom(x, y)) room = PaintRoom.Nutrient;
            else if (grid.IsNurseryRoom(x, y)) room = PaintRoom.Nursery;
            else if (grid.IsWasteRoom(x, y)) room = WasteRoomOf(grid.GetWasteKind(x, y));
            else if (grid.IsDewRoom(x, y)) room = PaintRoom.Dew;
            else if (grid.IsFarmRoom(x, y)) room = PaintRoom.Farm;
            else
            {
                switch (grid.GetStorageRoom(x, y))
                {
                    case StorageKind.Food: room = PaintRoom.FoodStore; break;
                    case StorageKind.Blocks: room = PaintRoom.BlockStore; break;
                    case StorageKind.Water: room = PaintRoom.WaterStore; break;
                    case StorageKind.Sand: room = PaintRoom.SandStore; break;
                    case StorageKind.Mushroom: room = PaintRoom.MushroomStore; break;
                    case StorageKind.Litter: room = PaintRoom.LitterStore; break;
                    case StorageKind.Nectar: room = PaintRoom.NectarStore; break;
                    case StorageKind.Ore: room = PaintRoom.OreStore; break;
                    case StorageKind.Smelter: room = PaintRoom.Smelter; break;
                    case StorageKind.Forge: room = PaintRoom.Forge; break;
                    case StorageKind.Armory: room = PaintRoom.Armory; break;
                    case StorageKind.Wardrobe: room = PaintRoom.Wardrobe; break;
                    default: return false;
                }
            }
            return true;
        }

        public static StorageKind StorageOf(PaintRoom room)
        {
            switch (room)
            {
                case PaintRoom.BlockStore: return StorageKind.Blocks;
                case PaintRoom.WaterStore: return StorageKind.Water;
                case PaintRoom.SandStore: return StorageKind.Sand;
                case PaintRoom.MushroomStore: return StorageKind.Mushroom;
                case PaintRoom.LitterStore: return StorageKind.Litter;
                case PaintRoom.NectarStore: return StorageKind.Nectar;
                case PaintRoom.OreStore: return StorageKind.Ore;
                case PaintRoom.Smelter: return StorageKind.Smelter;
                case PaintRoom.Forge: return StorageKind.Forge;
                case PaintRoom.Armory: return StorageKind.Armory;
                case PaintRoom.Wardrobe: return StorageKind.Wardrobe;
                default: return StorageKind.Food;
            }
        }

        int ChunkCount(PaintRoom room, int cx, int cy)
        {
            switch (room)
            {
                case PaintRoom.Queen: return _grid.GetChunkQueenRoomCount(cx, cy);
                case PaintRoom.Nutrient: return _grid.GetChunkNutrientRoomCount(cx, cy);
                case PaintRoom.Nursery: return _grid.GetChunkNurseryRoomCount(cx, cy);
                case PaintRoom.Waste:
                case PaintRoom.WasteLeaves:
                case PaintRoom.WasteDirt:
                case PaintRoom.WasteNutrient: return _grid.GetChunkWasteRoomCount(cx, cy); // os modos juntos: IsRoom separa
                case PaintRoom.Dew: return _grid.GetChunkDewRoomCount(cx, cy);
                case PaintRoom.Farm: return _grid.GetChunkFarmRoomCount(cx, cy);
                default: return _grid.GetChunkStorageRoomCount(cx, cy); // os depósitos juntos: IsRoom separa
            }
        }

        /// <summary>Preenchimento a partir de cada célula ainda não visitada, só nos blocos que têm o ambiente (salas são pequenas).</summary>
        int CountRooms(PaintRoom room)
        {
            if (Used(room) == 0)
                return 0;
            int width = _grid.Width;
            int rooms = 0;
            _visited.Clear();
            for (int cy = 0; cy < _grid.ChunkRows; cy++)
            for (int cx = 0; cx < _grid.ChunkColumns; cx++)
            {
                if (ChunkCount(room, cx, cy) == 0)
                    continue;
                int x0 = cx * PixelGrid.ChunkSize, y0 = cy * PixelGrid.ChunkSize;
                for (int y = y0; y < y0 + PixelGrid.ChunkSize; y++)
                for (int x = x0; x < x0 + PixelGrid.ChunkSize; x++)
                {
                    if (!IsRoom(_grid, room, x, y) || !_visited.Add(y * width + x))
                        continue;
                    rooms++;
                    _stack.Push(y * width + x);
                    while (_stack.Count > 0)
                    {
                        int i = _stack.Pop();
                        int px = i % width, py = i / width;
                        Visit(room, px - 1, py);
                        Visit(room, px + 1, py);
                        Visit(room, px, py - 1);
                        Visit(room, px, py + 1);
                    }
                }
            }
            return rooms;
        }

        void Visit(PaintRoom room, int x, int y)
        {
            if (IsRoom(_grid, room, x, y) && _visited.Add(y * _grid.Width + x))
                _stack.Push(y * _grid.Width + x);
        }
    }
}
