using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Fundição, forja e equipamento (documento 45, REQ-071). As duas salas são pintadas como depósitos
    /// (<see cref="StorageKind.Smelter"/>, <see cref="StorageKind.Forge"/>) e guardam um item por célula. A transformação corre
    /// sozinha com o tempo, desde que a entrada esteja na sala; as formigas fazem a logística: levam minério e serapilheira à
    /// fundição, as barras à forja e os equipamentos aos armazéns, e cada uma busca no armazém o equipamento do papel dela.
    /// Guia: <c>Colony/WorkshopGuide.md</c>.
    /// </summary>
    public sealed partial class AntColony
    {
        /// <summary>Segundos de jogo para fundir 1 minério com 1 serapilheira numa barra (suposição do agente).</summary>
        public const float SmeltSeconds = 20f;
        /// <summary>Segundos de jogo para forjar 2 barras do mesmo metal num equipamento (suposição do agente).</summary>
        public const float ForgeSeconds = 30f;

        /// <summary>Pincéis do depósito de minério, da fundição, da forja e dos armazéns: só com a câmara comprada (V0, V1, Y0, Y1, Y2).</summary>
        public bool OreStoreUnlocked => Effects.OreStoreUnlocked;
        public bool SmelterUnlocked => Effects.SmelterUnlocked;
        public bool ForgeUnlocked => Effects.ForgeUnlocked;
        public bool ArmoryUnlocked => Effects.ArmoryUnlocked;
        public bool WardrobeUnlocked => Effects.WardrobeUnlocked;

        // Itens nas salas, relidos quando os itens guardados mudam (StoreVersion).
        int _workshopVersion = -1;
        readonly List<int> _smelterOre = new List<int>();
        readonly List<int> _smelterFuel = new List<int>();
        int _smelterCells;
        readonly List<int>[] _forgeBars = { new List<int>(), new List<int>(), new List<int>() };
        // Relógio de cada fusão (pela célula do minério) e de cada forja (pela célula da primeira barra). Não vai para o save.
        readonly Dictionary<int, float> _smeltTimers = new Dictionary<int, float>();
        readonly Dictionary<int, float> _forgeTimers = new Dictionary<int, float>();
        readonly List<int> _timerKeys = new List<int>();
        // A caminho da fundição neste passo (contados em CountWorkshop).
        int _smelterOreIncoming, _smelterFuelIncoming;
        // Equipamentos guardados por tipo, com o campo que leva a eles (o armazém de ferramentas e armas guarda dois tipos).
        readonly FlowField[] _equipFields = new FlowField[4];
        readonly int[] _equipStock = new int[4];
        readonly int[] _equipTakers = new int[4];
        readonly List<int> _equipSources = new List<int>();
        // Equipamentos ainda na forja (relidos com a sala) e na mão de quem os leva ao armazém (contados no passo).
        readonly int[] _forgeItems = new int[4];
        readonly int[] _equipInFlight = new int[4];

        /// <summary>Barras e equipamentos feitos desde o começo do mapa (não vão para o save).</summary>
        public int BarsSmelted { get; private set; }
        public int ItemsForged { get; private set; }

        bool SmelterWantsOre =>
            HasDepot(StorageKind.Smelter) && SmelterRoom &&
            _smelterOre.Count + _smelterOreIncoming < Mathf.Max(1, _smelterCells / 2);

        bool SmelterWantsFuel =>
            HasDepot(StorageKind.Smelter) && SmelterRoom &&
            _smelterFuel.Count + _smelterFuelIncoming < _smelterOre.Count + _smelterOreIncoming + 1;

        bool SmelterRoom => _storeFree[(int)StorageKind.Smelter] - _storeIncoming[(int)StorageKind.Smelter] > 0;

        /// <summary>Minério e serapilheira dentro da fundição são a entrada dela: não são itens soltos.</summary>
        bool IsWorkshopInput(int x, int y, byte item)
        {
            if (_grid.GetStorageRoom(x, y) != StorageKind.Smelter)
                return false;
            CellMaterial m = StoredItems.MaterialOf(item);
            return CellMaterials.IsOre(m) || m == CellMaterial.Litter;
        }

        void ResetWorkshopCounts()
        {
            _smelterOreIncoming = _smelterFuelIncoming = 0;
            System.Array.Clear(_equipTakers, 0, _equipTakers.Length);
            System.Array.Clear(_equipInFlight, 0, _equipInFlight.Length);
        }

        void CountWorkshop(Ant ant)
        {
            if (ant.State == AntState.TakingStored)
            {
                EquipKind kind = EquipKindOf(ant.StoreUse);
                if (kind != EquipKind.None)
                    _equipTakers[(int)kind]++;
                return;
            }
            EquipKind carried = Equipment.KindOf(ant.Carried);
            if (carried != EquipKind.None)
                _equipInFlight[(int)carried] += ant.CarriedCount;
            if (ant.StoreTarget != StorageKind.Smelter)
                return;
            if (CellMaterials.IsOre(ant.Carried))
                _smelterOreIncoming += LoadOf(ant);
            else if (ant.Carried == CellMaterial.Litter)
                _smelterFuelIncoming += LoadOf(ant);
        }

        // ---------------------------------------------------------------- transformação

        void TickWorkshops(float deltaTime)
        {
            if (!HasDepot(StorageKind.Smelter) && !HasDepot(StorageKind.Forge))
                return;
            ScanWorkshops();
            TickSmelter(deltaTime);
            TickForge(deltaTime);
        }

        void ScanWorkshops()
        {
            if (_workshopVersion == _grid.StoreVersion)
                return;
            _workshopVersion = _grid.StoreVersion;
            _smelterOre.Clear();
            _smelterFuel.Clear();
            _smelterCells = 0;
            foreach (List<int> bars in _forgeBars)
                bars.Clear();
            System.Array.Clear(_forgeItems, 0, _forgeItems.Length);
            RectInt area = _grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            for (int cy = cyMin; cy <= cyMax; cy++)
            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                if (_grid.GetChunkStorageRoomCount(cx, cy) == 0)
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    StorageKind room = _grid.GetStorageRoom(x, y);
                    if (room != StorageKind.Smelter && room != StorageKind.Forge)
                        continue;
                    CellMaterial item = StoredItems.MaterialOf(_grid.GetStored(x, y));
                    int index = _grid.ToIndex(x, y);
                    if (room == StorageKind.Smelter)
                    {
                        _smelterCells++;
                        if (CellMaterials.IsOre(item))
                            _smelterOre.Add(index);
                        else if (item == CellMaterial.Litter)
                            _smelterFuel.Add(index);
                    }
                    else if (Equipment.IsBar(item))
                        _forgeBars[Equipment.TierOf(item)].Add(index);
                    else if (Equipment.IsItem(item))
                        _forgeItems[(int)Equipment.KindOf(item)]++;
                }
            }
            DropStaleTimers(_smeltTimers, _smelterOre);
        }

        static void DropStaleTimers(Dictionary<int, float> timers, List<int> valid)
        {
            if (timers.Count == 0)
                return;
            var keep = new HashSet<int>(valid);
            var stale = new List<int>();
            foreach (int key in timers.Keys)
                if (!keep.Contains(key))
                    stale.Add(key);
            foreach (int key in stale)
                timers.Remove(key);
        }

        /// <summary>Cada minério com uma serapilheira sobrando na fundição funde: depois de <see cref="SmeltSeconds"/>, vira barra no lugar.</summary>
        void TickSmelter(float deltaTime)
        {
            int pairs = Mathf.Min(_smelterOre.Count, _smelterFuel.Count);
            int w = _grid.Width;
            for (int i = 0; i < pairs; i++)
            {
                int ore = _smelterOre[i];
                _smeltTimers.TryGetValue(ore, out float t);
                t += deltaTime;
                if (t < SmeltSeconds * Effects.SmeltTime)
                {
                    _smeltTimers[ore] = t;
                    continue;
                }
                _smeltTimers.Remove(ore);
                int fuel = _smelterFuel[i];
                CellMaterial metal = StoredItems.MaterialOf(_grid.GetStored(ore % w, ore / w));
                _grid.SetStored(fuel % w, fuel / w, StoredItems.None);
                _grid.SetStored(ore % w, ore / w, StoredItems.None);
                _grid.SetStored(ore % w, ore / w, StoredItems.Of(Equipment.BarOf(Equipment.TierOf(metal))));
                BarsSmelted++;
            }
        }

        /// <summary>
        /// Duas barras do mesmo metal na forja viram, depois de <see cref="ForgeSeconds"/>, o equipamento que mais falta (a
        /// primeira barra vira o item, a segunda some). O melhor metal primeiro. Sem falta, as barras esperam.
        /// </summary>
        void TickForge(float deltaTime)
        {
            EquipKind wanted = MostWantedEquip();
            if (wanted == EquipKind.None)
                return;
            int w = _grid.Width;
            for (int tier = Equipment.Tiers - 1; tier >= 0; tier--)
            {
                List<int> bars = _forgeBars[tier];
                if (bars.Count < 2)
                    continue;
                int first = bars[0];
                _forgeTimers.TryGetValue(first, out float t);
                t += deltaTime;
                if (t < ForgeSeconds * Effects.ForgeTime)
                {
                    _forgeTimers[first] = t;
                    return;
                }
                _forgeTimers.Clear();
                int second = bars[1];
                _grid.SetStored(second % w, second / w, StoredItems.None);
                _grid.SetStored(first % w, first / w, StoredItems.None);
                _grid.SetStored(first % w, first / w, StoredItems.Of(Equipment.ItemOf(wanted, tier)));
                ItemsForged++;
                return;
            }
        }

        /// <summary>O tipo de equipamento com mais formigas sem ele, descontado o que já está guardado ou feito; None sem falta.</summary>
        EquipKind MostWantedEquip()
        {
            int needTools = 0, needWeapons = 0, needArmor = 0;
            foreach (Ant ant in _ants)
            {
                if (UsesEquip(ant.Role, EquipKind.Tool) && ant.Tool == CellMaterial.Empty) needTools++;
                if (UsesEquip(ant.Role, EquipKind.Weapon) && ant.Weapon == CellMaterial.Empty) needWeapons++;
                if (UsesEquip(ant.Role, EquipKind.Armor) && ant.Armor == CellMaterial.Empty) needArmor++;
            }
            needTools -= EquipAvailable(EquipKind.Tool);
            needWeapons -= EquipAvailable(EquipKind.Weapon);
            needArmor -= EquipAvailable(EquipKind.Armor);
            if (needTools <= 0 && needWeapons <= 0 && needArmor <= 0)
                return EquipKind.None;
            if (needTools >= needWeapons && needTools >= needArmor)
                return EquipKind.Tool;
            return needWeapons >= needArmor ? EquipKind.Weapon : EquipKind.Armor;
        }

        /// <summary>Equipamentos do tipo já feitos: guardados no armazém, ainda na forja ou na mão de quem os leva.</summary>
        int EquipAvailable(EquipKind kind) => _equipStock[(int)kind] + _forgeItems[(int)kind] + _equipInFlight[(int)kind];

        // ---------------------------------------------------------------- logística

        /// <summary>Comum e carregadora ociosas levam minério e serapilheira dos depósitos à fundição, se ela pede.</summary>
        bool TryStartWorkshopSupply(Ant ant)
        {
            if (!HasDepot(StorageKind.Smelter))
                return false;
            ScanWorkshops();
            if (SmelterWantsOre && TryStartTake(ant, StoreUse.SmelterOre))
                return true;
            return SmelterWantsFuel && TryStartTake(ant, StoreUse.SmelterFuel);
        }

        /// <summary>Destino do minério ou da serapilheira na mão: a fundição, se ela pede. Falso deixa o caminho de sempre.</summary>
        bool TryStartSmelterDelivery(Ant ant)
        {
            if (!HasDepot(StorageKind.Smelter))
                return false;
            ScanWorkshops();
            if (CellMaterials.IsOre(ant.Carried) ? SmelterWantsOre : ant.Carried == CellMaterial.Litter && SmelterWantsFuel)
                return TryStartStock(ant, StorageKind.Smelter);
            return false;
        }

        // ---------------------------------------------------------------- equipamento

        /// <summary>Quem usa cada tipo (suposição do agente): ferramenta para escavadora e comum, arma e armadura para as guerreiras.</summary>
        public static bool UsesEquip(AntRole role, EquipKind kind)
        {
            switch (kind)
            {
                case EquipKind.Tool: return role == AntRole.Digger || role == AntRole.Worker;
                case EquipKind.Weapon:
                case EquipKind.Armor: return AntRoles.IsWarrior(role);
                default: return false;
            }
        }

        static EquipKind EquipKindOf(StoreUse use)
        {
            switch (use)
            {
                case StoreUse.EquipTool: return EquipKind.Tool;
                case StoreUse.EquipWeapon: return EquipKind.Weapon;
                case StoreUse.EquipArmor: return EquipKind.Armor;
                default: return EquipKind.None;
            }
        }

        static StoreUse EquipUseOf(EquipKind kind) =>
            kind == EquipKind.Tool ? StoreUse.EquipTool : kind == EquipKind.Weapon ? StoreUse.EquipWeapon : StoreUse.EquipArmor;

        static StorageKind EquipRoomOf(EquipKind kind) => kind == EquipKind.Armor ? StorageKind.Wardrobe : StorageKind.Armory;

        static CellMaterial Equipped(Ant ant, EquipKind kind) =>
            kind == EquipKind.Tool ? ant.Tool : kind == EquipKind.Weapon ? ant.Weapon : ant.Armor;

        /// <summary>Formiga sem o equipamento do papel dela vai buscá-lo no armazém, se há um que ninguém foi buscar.</summary>
        bool TryStartEquip(Ant ant)
        {
            for (EquipKind kind = EquipKind.Tool; kind <= EquipKind.Armor; kind++)
            {
                if (!UsesEquip(ant.Role, kind) || Equipped(ant, kind) != CellMaterial.Empty)
                    continue;
                int k = (int)kind;
                FlowField field = _equipFields[k];
                if (_equipStock[k] - _equipTakers[k] <= 0 || field == null || field.Get(ant.X, ant.Y) == FlowField.Unreachable)
                    continue;
                ReleaseDumpSide(ant);
                ant.State = AntState.TakingStored;
                ant.StoreUse = EquipUseOf(kind);
                ant.StoreTarget = StorageKind.None;
                _equipTakers[k]++;
                UpdateTaking(ant);
                return true;
            }
            return false;
        }

        FlowField EquipField(StoreUse use)
        {
            EquipKind kind = EquipKindOf(use);
            return kind == EquipKind.None ? null : _equipFields[(int)kind];
        }

        /// <summary>Campos até os equipamentos guardados de cada tipo (refeitos junto dos campos dos depósitos).</summary>
        void BuildEquipFields()
        {
            for (EquipKind kind = EquipKind.Tool; kind <= EquipKind.Armor; kind++)
            {
                int k = (int)kind;
                _equipStock[k] = 0;
                StorageKind room = EquipRoomOf(kind);
                if (!HasDepot(room))
                {
                    _equipFields[k]?.Invalidate();
                    continue;
                }
                _equipSources.Clear();
                RectInt area = _grid.Active;
                int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
                int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
                for (int cy = cyMin; cy <= cyMax; cy++)
                for (int cx = cxMin; cx <= cxMax; cx++)
                {
                    if (_grid.GetChunkStorageRoomCount(cx, cy) == 0)
                        continue;
                    int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                    int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                    int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                    int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                    for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                        if (_grid.GetStorageRoom(x, y) == room && _walkability.IsWalkable(x, y) &&
                            Equipment.KindOf(StoredItems.MaterialOf(_grid.GetStored(x, y))) == kind)
                            _equipSources.Add(_grid.ToIndex(x, y));
                }
                _equipStock[k] = _equipSources.Count;
                _equipFields[k] ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
                UpdateField(_equipFields[k], _equipSources);
            }
            _equipSources.Clear();
        }

        /// <summary>Pega o equipamento do tipo ao lado e o veste. Falso se não há um ao alcance.</summary>
        bool TryPickEquip(Ant ant, int x, int y, byte item)
        {
            EquipKind kind = EquipKindOf(ant.StoreUse);
            CellMaterial material = StoredItems.MaterialOf(item);
            if (Equipment.KindOf(material) != kind || _grid.GetStorageRoom(x, y) != EquipRoomOf(kind))
                return false;
            _grid.SetStored(x, y, StoredItems.None);
            int uses = Mathf.Max(1, Mathf.RoundToInt(Equipment.Uses(kind, Equipment.TierOf(material)) * Effects.EquipDurability));
            switch (kind)
            {
                case EquipKind.Tool: ant.Tool = material; ant.ToolUses = uses; break;
                case EquipKind.Weapon: ant.Weapon = material; ant.WeaponUses = uses; break;
                default: ant.Armor = material; ant.ArmorUses = uses; break;
            }
            if (x != ant.X)
                ant.FacingX = x > ant.X ? 1 : -1;
            ant.Cooldown = _settings.DropSeconds;
            ant.StoreUse = StoreUse.None;
            ant.State = AntState.Idle;
            return true;
        }

        float ToolFactor(Ant ant) =>
            ant.Tool == CellMaterial.Empty ? 1f : Equipment.ToolTimeFactor(Equipment.TierOf(ant.Tool)) * Effects.SharpToolTime;

        /// <summary>Um uso da ferramenta (uma célula cavada); gasta, volta como barra.</summary>
        void WearTool(Ant ant)
        {
            if (ant.Tool == CellMaterial.Empty || --ant.ToolUses > 0)
                return;
            ScrapEquip(ant, ant.Tool);
            ant.Tool = CellMaterial.Empty;
        }

        /// <summary>Dano da formiga com a arma; um uso por golpe.</summary>
        float WeaponFactor(Ant ant)
        {
            if (ant.Weapon == CellMaterial.Empty)
                return 1f;
            float factor = Equipment.WeaponDamageFactor(Equipment.TierOf(ant.Weapon)) * Effects.WeaponDamageBoost;
            if (--ant.WeaponUses <= 0)
            {
                ScrapEquip(ant, ant.Weapon);
                ant.Weapon = CellMaterial.Empty;
            }
            return factor;
        }

        /// <summary>Dano que a formiga recebe com a armadura; um uso por golpe recebido.</summary>
        float ArmorFactor(Ant ant)
        {
            if (ant.Armor == CellMaterial.Empty)
                return 1f;
            float factor = Equipment.ArmorTakenFactor(Equipment.TierOf(ant.Armor)) * Effects.ArmorTakenBoost;
            if (--ant.ArmorUses <= 0)
            {
                ScrapEquip(ant, ant.Armor);
                ant.Armor = CellMaterial.Empty;
            }
            return factor;
        }

        /// <summary>Equipamento gasto volta à forja (decisão de 2026-10-23): 1 barra do metal no chão, levada como item solto.</summary>
        void ScrapEquip(Ant ant, CellMaterial item) =>
            DropItemNear(ant.X, ant.Y, StoredItems.Of(Equipment.BarOf(Equipment.TierOf(item))), 128);

        /// <summary>A formiga que morre deixa no chão o que vestia.</summary>
        void DropEquipment(Ant ant)
        {
            foreach (CellMaterial item in new[] { ant.Tool, ant.Weapon, ant.Armor })
                if (item != CellMaterial.Empty)
                    DropItemNear(ant.X, ant.Y, StoredItems.Of(item), 128);
            ant.Tool = ant.Weapon = ant.Armor = CellMaterial.Empty;
        }
    }
}
