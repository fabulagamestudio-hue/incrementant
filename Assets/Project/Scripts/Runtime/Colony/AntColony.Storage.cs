using Fabula.Incrementant.Audio;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>Para que a formiga foi pegar um item guardado (<see cref="AntState.TakingStored"/>). Não vai para o save.</summary>
    public enum StoreUse : byte
    {
        None,
        /// <summary>Pedaço de planta do depósito de alimentos para um canteiro ou para a folha da sala Gotas de Orvalho.</summary>
        Plant,
        /// <summary>Bloco do depósito de construção para um canteiro novo ou para o teto da sala Gotas de Orvalho (<see cref="Ant.DewJob"/>).</summary>
        Soil,
        /// <summary>Bloco do depósito de construção para a obra.</summary>
        Build,
        /// <summary>Bloco de água do depósito de água para um canteiro com planta.</summary>
        Water,
        /// <summary>Item solto fora do depósito dele (depósito apagado, folha do resto de cupim): vai ao destino de sempre.</summary>
        Loose,
    }

    /// <summary>
    /// Depósitos de alimentos, de construção e de água (pedido do desenvolvedor em 2026-10-19). O que as formigas trazem de
    /// fora (planta cortada, terra cavada, água do mapa) vai primeiro para o depósito do tipo, onde fica como item guardado
    /// (<see cref="PixelGrid.GetStored"/>: não bloqueia a passagem); quem consome (canteiros, sala Gotas de Orvalho, obra)
    /// pega do depósito. Só sem o depósito do tipo o consumo usa o que está do lado de fora, como antes.
    /// </summary>
    public sealed partial class AntColony
    {
        const int KindSlots = 4;
        // Por tipo (índice = StorageKind; 0 sem uso): campo até as células livres do depósito e até os itens guardados nele.
        readonly FlowField[] _putFields = new FlowField[KindSlots];
        readonly FlowField[] _takeFields = new FlowField[KindSlots];
        readonly bool[] _storeFieldIdle = { true, true, true, true };
        // Vagas livres e itens guardados (da última rodada de campos), e formigas levando ou indo buscar (recontadas a cada Tick).
        readonly int[] _storeFree = new int[KindSlots];
        readonly int[] _storeStock = new int[KindSlots];
        readonly int[] _storeIncoming = new int[KindSlots];
        readonly int[] _storeOutgoing = new int[KindSlots];
        // Itens soltos (fora do depósito do tipo deles) que têm para onde ir, e quem está indo buscá-los.
        FlowField _looseField;
        bool _looseFieldIdle = true;
        int _looseCount;
        int _loosePlants;
        // Itens soltos com destino, por tipo (planta, água, o resto), da última rodada: a vaga de quem vai buscá-los é limitada
        // pelo que o destino ainda aceita (LooseRoom), não pelo número de itens (relato de 2026-10-19).
        int _looseFoodItems, _looseWaterItems, _looseOtherItems;
        int _looseTakers;
        int _builtStoreVersion = -1;
        // Planta na mão a caminho dos canteiros vinda do depósito de alimentos (com o depósito, a de fora vai para ele).
        int _plantsToBeds;

        /// <summary>Existe depósito pintado do tipo.</summary>
        public bool HasDepot(StorageKind kind) => _grid.StorageRoomCount(kind) > 0;

        /// <summary>Vagas livres no depósito do tipo (células vazias e alcançáveis, contadas na última rodada de campos).</summary>
        public int DepotFree(StorageKind kind) => _storeFree[(int)kind];

        /// <summary>Itens guardados dentro do depósito do tipo (contados na última rodada de campos).</summary>
        public int DepotStock(StorageKind kind) => _storeStock[(int)kind];

        /// <summary>Itens soltos fora do depósito deles, esperando uma formiga levá-los.</summary>
        public int LooseItems => _looseCount;

        bool FoodDepot => HasDepot(StorageKind.Food);
        bool BlockDepot => HasDepot(StorageKind.Blocks);
        bool WaterDepot => HasDepot(StorageKind.Water);

        /// <summary>Quanto de planta a colheita ainda pode trazer: vagas do depósito de alimentos ou, sem ele, o que os canteiros pedem.</summary>
        int PlantSupplyDemand => FoodDepot ? _storeFree[(int)StorageKind.Food] : PlantDemand;

        /// <summary>
        /// A planta cortada tem para onde ir a partir da célula: o depósito de alimentos (com vaga) ou, sem ele, os canteiros
        /// e a folha da sala Gotas de Orvalho. Quem corta e quem recolhe o pedaço solto só saem com isso.
        /// </summary>
        bool PlantDestinationReachable(int x, int y)
        {
            const int k = (int)StorageKind.Food;
            if (!FoodDepot)
                return _storeField.Get(x, y) != FlowField.Unreachable;
            return !_storeFieldIdle[k] && _putFields[k].Get(x, y) != FlowField.Unreachable;
        }

        /// <summary>Pedaços de planta já fora da planta esperando quem os leve: soltos no chão e soltos fora de depósito.</summary>
        int PlantsWaiting => _grid.PlantDebrisCount + _loosePlants;

        /// <summary>Capacidade de referência da histerese da colheita: o depósito de alimentos inteiro, ou os canteiros.</summary>
        int HarvestCapacity => FoodDepot ? _storeFree[(int)StorageKind.Food] + _storeStock[(int)StorageKind.Food] : _activeBeds;

        /// <summary>Bloco que o depósito de construção aceita: terra que vira terra fofa (não areia, rocha, planta nem resíduo).</summary>
        static bool IsBlockItem(CellMaterial material) =>
            material != CellMaterial.Empty && material != CellMaterial.Rock && !CellMaterials.IsPlant(material) &&
            material != CellMaterial.TermiteRemains && material != CellMaterial.Refuse &&
            CellMaterials.ToDumped(material) == CellMaterial.Spoil;

        void ResetStorageCounts()
        {
            System.Array.Clear(_storeIncoming, 0, KindSlots);
            System.Array.Clear(_storeOutgoing, 0, KindSlots);
            _looseTakers = 0;
            _plantsToBeds = 0;
        }

        /// <summary>Conta quem leva ou busca itens dos depósitos (no começo do Tick, junto das outras contagens).</summary>
        void CountStorage(Ant ant)
        {
            // Mão vazia fora de uma viagem de água ou de terra da pilha: nada a levar; limpa o destino velho.
            if (ant.CarriedCount == 0 && ant.State != AntState.FetchingWater && ant.State != AntState.CarryingWater &&
                ant.State != AntState.FetchingSoil && ant.State != AntState.GatheringSoil && ant.State != AntState.TakingStored)
            {
                ant.StoreTarget = StorageKind.None;
                ant.FromStore = false;
            }
            if (ant.State == AntState.TakingStored)
            {
                switch (ant.StoreUse)
                {
                    case StoreUse.Loose: _looseTakers++; break;
                    case StoreUse.Plant: _storeOutgoing[(int)StorageKind.Food]++; _plantsToBeds++; break;
                    case StoreUse.Water: _storeOutgoing[(int)StorageKind.Water]++; _waterCarriers++; break;
                    case StoreUse.Build: _storeOutgoing[(int)StorageKind.Blocks]++; _activeBuilders++; break;
                    default: _storeOutgoing[(int)StorageKind.Blocks]++; break; // terra fofa: CountCultivation conta o destino
                }
                return;
            }
            if (ant.StoreTarget != StorageKind.None)
                _storeIncoming[(int)ant.StoreTarget] += Mathf.Max(1, ant.CarriedCount);
        }

        // ---------------------------------------------------------------- campos

        /// <summary>
        /// Campos dos depósitos: até as vagas livres (levar) e até os itens guardados (pegar), um par por tipo, e até os itens
        /// soltos que têm para onde ir. Roda antes dos campos do cultivo e da escavação, que usam as vagas e os itens.
        /// </summary>
        void BuildStorageFields()
        {
            _builtStoreVersion = _grid.StoreVersion;
            for (int kind = 1; kind < KindSlots; kind++)
                BuildDepotFields((StorageKind)kind);
            BuildLooseField();
        }

        void BuildDepotFields(StorageKind kind)
        {
            int k = (int)kind;
            _storeFree[k] = 0;
            _storeStock[k] = 0;
            if (_grid.StorageRoomCount(kind) == 0)
            {
                if (!_storeFieldIdle[k])
                {
                    _putFields[k].Invalidate();
                    _takeFields[k].Invalidate();
                    _storeFieldIdle[k] = true;
                }
                return;
            }
            _storeFieldIdle[k] = false;
            _putFields[k] ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
            _takeFields[k] ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
            _sources.Clear();
            _buildSources.Clear();
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
                    if (_grid.GetStorageRoom(x, y) != kind || !_walkability.IsWalkable(x, y))
                        continue;
                    byte item = _grid.GetStored(x, y);
                    if (item != StoredItems.None)
                    {
                        if (StoredItems.KindOf(item) == kind)
                            _buildSources.Add(_grid.ToIndex(x, y));
                    }
                    else if (IsPutSpot(x, y, kind))
                    {
                        _sources.Add(_grid.ToIndex(x, y));
                    }
                }
            }
            _storeFree[k] = _sources.Count;
            _storeStock[k] = _buildSources.Count;
            UpdateField(_putFields[k], _sources);
            UpdateField(_takeFields[k], _buildSources);
            _sources.Clear();
            _buildSources.Clear();
        }

        /// <summary>Vaga livre do depósito: célula dele sem material, item, nutriente nem água, caminhável.</summary>
        bool IsPutSpot(int x, int y, StorageKind kind) =>
            _grid.GetStorageRoom(x, y) == kind && !_grid.IsSolid(x, y) && _grid.GetStored(x, y) == StoredItems.None &&
            !_grid.HasNutrient(x, y) && !_grid.HasWater(x, y) && _walkability.IsWalkable(x, y);

        /// <summary>
        /// Item solto: fora do depósito do tipo dele (e fora da área de resíduos, se for resíduo, que se decompõe onde está).
        /// Só entra no campo se tem destino agora: planta com depósito de alimentos com vaga ou canteiro esperando; terra
        /// sempre (depósito ou despejo); água com depósito de água com vaga ou canteiro esperando.
        /// </summary>
        void BuildLooseField()
        {
            _looseCount = 0;
            _loosePlants = 0;
            _looseFoodItems = _looseWaterItems = _looseOtherItems = 0;
            if (_grid.StoredCount(StorageKind.Food) + _grid.StoredCount(StorageKind.Blocks) + _grid.StoredCount(StorageKind.Water) == 0)
            {
                if (!_looseFieldIdle)
                {
                    _looseField.Invalidate();
                    _looseFieldIdle = true;
                }
                return;
            }
            _sources.Clear();
            RectInt area = _grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            for (int cy = cyMin; cy <= cyMax; cy++)
            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                if (_grid.GetChunkStoredCount(cx, cy) == 0)
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    if (!IsLooseItem(x, y, out StorageKind kind))
                        continue;
                    if (kind == StorageKind.Food)
                        _loosePlants++;
                    if (!LooseHasDestination(kind) || !_walkability.IsWalkable(x, y))
                        continue;
                    _sources.Add(_grid.ToIndex(x, y));
                    if (kind == StorageKind.Food)
                        _looseFoodItems++;
                    else if (kind == StorageKind.Water)
                        _looseWaterItems++;
                    else
                        _looseOtherItems++;
                }
            }
            _looseCount = _sources.Count;
            if (_looseCount == 0 && _looseFieldIdle)
                return;
            _looseFieldIdle = false;
            _looseField ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
            UpdateField(_looseField, _sources);
            _sources.Clear();
        }

        bool IsLooseItem(int x, int y, out StorageKind kind)
        {
            kind = StoredItems.KindOf(_grid.GetStored(x, y));
            return kind != StorageKind.None && kind != StorageKind.Waste && _grid.GetStorageRoom(x, y) != kind;
        }

        bool LooseHasDestination(StorageKind kind)
        {
            switch (kind)
            {
                case StorageKind.Food: return FoodDepot ? _storeFree[(int)StorageKind.Food] > 0 : PlantDemand > 0;
                case StorageKind.Water: return WaterDepot ? _storeFree[(int)StorageKind.Water] > 0 : _bedsAwaitingWater > 0;
                default: return true;
            }
        }

        /// <summary>
        /// Quantos itens soltos do tipo o destino ainda aceita, descontado o que já está a caminho: vagas do depósito menos as
        /// formigas indo para ele, ou o pedido dos canteiros (e das folhas da sala Gotas de Orvalho) menos o que já está na mão
        /// ou reservado no chão. Terra não tem limite (o despejo aceita tudo).
        /// </summary>
        int LooseRoom(StorageKind kind)
        {
            const int food = (int)StorageKind.Food, water = (int)StorageKind.Water;
            switch (kind)
            {
                case StorageKind.Food:
                    return FoodDepot ? _storeFree[food] - _storeIncoming[food] : PlantDemand - _plantsInFlight - _plantsToBeds - _debrisClaims;
                case StorageKind.Water:
                    return WaterDepot ? _storeFree[water] - _storeIncoming[water] : _bedsAwaitingWater - _waterCarriers;
                default:
                    return int.MaxValue;
            }
        }

        /// <summary>Itens soltos que ainda cabem no destino deles, menos as formigas que já saíram para buscar algum.</summary>
        int LooseWanted =>
            Mathf.Clamp(LooseRoom(StorageKind.Food), 0, _looseFoodItems) + Mathf.Clamp(LooseRoom(StorageKind.Water), 0, _looseWaterItems) +
            _looseOtherItems - _looseTakers;

        // ---------------------------------------------------------------- levar ao depósito

        /// <summary>
        /// Com o que tem na mão (ou a água na boca), vai guardar no depósito do tipo, se ele tem vaga sem ninguém a caminho e
        /// é alcançável de onde a formiga está. O estado não muda (<see cref="AntState.Carrying"/> ou
        /// <see cref="AntState.CarryingWater"/>): <see cref="Ant.StoreTarget"/> desvia a entrega para <see cref="UpdateStocking"/>.
        /// </summary>
        bool TryStartStock(Ant ant, StorageKind kind)
        {
            int k = (int)kind;
            FlowField field = _putFields[k];
            if (!HasDepot(kind) || field == null || _storeFieldIdle[k] || _storeFree[k] - _storeIncoming[k] <= 0 ||
                field.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ReleaseDumpSide(ant);
            ant.StoreTarget = kind;
            _storeIncoming[k] += Mathf.Max(1, ant.CarriedCount);
            return true;
        }

        /// <summary>Desce o campo das vagas do depósito e guarda o item na célula dela ou numa vizinha. Sem vaga ou caminho, desiste.</summary>
        void UpdateStocking(Ant ant)
        {
            StorageKind kind = ant.StoreTarget;
            FlowField field = _putFields[(int)kind];
            int d = field != null && HasDepot(kind) && !_storeFieldIdle[(int)kind] ? field.Get(ant.X, ant.Y) : FlowField.Unreachable;
            if (d == FlowField.Unreachable)
            {
                AbandonStock(ant);
                return;
            }
            if (d <= 1 && TryPutStored(ant, kind))
                return;
            if (d > 0 && StepDownhill(ant, field))
                return;
            // Na vaga, mas ela foi tomada: espera o campo ser refeito.
            Hold(ant);
        }

        bool TryPutStored(Ant ant, StorageKind kind)
        {
            for (int k = -1; k < 8; k++)
            {
                int x = k < 0 ? ant.X : ant.X + AntNavigation.Dx[k];
                int y = k < 0 ? ant.Y : ant.Y + AntNavigation.Dy[k];
                if (!IsPutSpot(x, y, kind))
                    continue;
                byte item = kind == StorageKind.Water ? StoredItems.Water
                    : StoredItems.Of(kind == StorageKind.Blocks ? CellMaterial.Spoil : ant.Carried);
                _grid.SetStored(x, y, item, kind == StorageKind.Water ? (byte)128 : ant.CarriedShade);
                Emit(SfxId.Drop);
                StoredItemsTotal++;
                if (x != ant.X)
                    ant.FacingX = x > ant.X ? 1 : -1;
                ant.Cooldown = _settings.DropSeconds;
                if (kind == StorageKind.Water)
                {
                    ant.StoreTarget = StorageKind.None;
                    EndWaterTrip(ant);
                    return true;
                }
                ant.DropTop();
                if (ant.CarriedCount == 0)
                {
                    ant.StoreTarget = StorageKind.None;
                    ant.State = AntState.Idle;
                }
                return true;
            }
            return false;
        }

        /// <summary>O depósito deixou de ter vaga ou caminho: entrega o que leva pelo caminho de sempre.</summary>
        void AbandonStock(Ant ant)
        {
            StorageKind kind = ant.StoreTarget;
            ant.StoreTarget = StorageKind.None;
            if (kind == StorageKind.Water)
                return; // segue em CarryingWater: canteiro esperando, senão a água volta ao mapa
            if (kind == StorageKind.Food)
            {
                ant.FromStore = true; // não tenta o depósito de novo: vai aos canteiros
                ant.State = AntState.Carrying;
                return;
            }
            ant.State = AntState.Carrying;
            AssignDumpSide(ant);
        }

        /// <summary>Itens que as formigas guardaram nos depósitos desde o começo do mapa (não vai para o save).</summary>
        public int StoredItemsTotal { get; private set; }

        // ---------------------------------------------------------------- pegar do depósito

        /// <summary>
        /// Vai pegar um item guardado para <paramref name="use"/>: do depósito do tipo (ou um item solto, em
        /// <see cref="StoreUse.Loose"/>), se há item que ninguém foi buscar e caminho até ele.
        /// </summary>
        bool TryStartTake(Ant ant, StoreUse use)
        {
            FlowField field;
            if (use == StoreUse.Loose)
            {
                field = _looseFieldIdle ? null : _looseField;
                if (LooseWanted <= 0)
                    return false;
            }
            else
            {
                int k = (int)KindOf(use);
                field = _storeFieldIdle[k] ? null : _takeFields[k];
                if (_storeStock[k] - _storeOutgoing[k] <= 0)
                    return false;
            }
            if (field == null || field.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ReleaseDumpSide(ant);
            ant.State = AntState.TakingStored;
            ant.StoreUse = use;
            ant.StoreTarget = StorageKind.None;
            // Conta já neste passo, para as formigas que decidem depois verem o item tomado.
            switch (use)
            {
                case StoreUse.Loose: _looseTakers++; break;
                case StoreUse.Plant: _storeOutgoing[(int)StorageKind.Food]++; _plantsToBeds++; break;
                case StoreUse.Water: _storeOutgoing[(int)StorageKind.Water]++; _waterCarriers++; break;
                case StoreUse.Build: _storeOutgoing[(int)StorageKind.Blocks]++; _activeBuilders++; break;
                default:
                    _storeOutgoing[(int)StorageKind.Blocks]++;
                    if (ant.DewJob)
                        _dewCarriers++;
                    else
                        _soilCarriers++;
                    break;
            }
            UpdateTaking(ant);
            return true;
        }

        static StorageKind KindOf(StoreUse use)
        {
            switch (use)
            {
                case StoreUse.Plant: return StorageKind.Food;
                case StoreUse.Water: return StorageKind.Water;
                default: return StorageKind.Blocks;
            }
        }

        void UpdateTaking(Ant ant)
        {
            FlowField field = ant.StoreUse == StoreUse.Loose
                ? (_looseFieldIdle ? null : _looseField)
                : _storeFieldIdle[(int)KindOf(ant.StoreUse)] ? null : _takeFields[(int)KindOf(ant.StoreUse)];
            int d = field != null ? field.Get(ant.X, ant.Y) : FlowField.Unreachable;
            if (d != FlowField.Unreachable && d <= 1 && TryPickStored(ant))
                return;
            if (d != FlowField.Unreachable && d > 0 && StepDownhill(ant, field))
                return;
            // Sem caminho, ou o item foi levado por outra: espera o campo ser refeito.
            ant.State = AntState.Idle;
            ant.StoreUse = StoreUse.None;
            Hold(ant);
        }

        bool TryPickStored(Ant ant)
        {
            for (int k = -1; k < 8; k++)
            {
                int x = k < 0 ? ant.X : ant.X + AntNavigation.Dx[k];
                int y = k < 0 ? ant.Y : ant.Y + AntNavigation.Dy[k];
                byte item = _grid.GetStored(x, y);
                if (item == StoredItems.None)
                    continue;
                StorageKind kind = StoredItems.KindOf(item);
                if (ant.StoreUse == StoreUse.Loose ? !IsLooseItem(x, y, out _) || !LooseHasDestination(kind)
                                                   : kind != KindOf(ant.StoreUse) || _grid.GetStorageRoom(x, y) != kind)
                    continue;
                byte shade = _grid.GetStoredShade(x, y);
                _grid.SetStored(x, y, StoredItems.None);
                Emit(SfxId.Dig);
                if (x != ant.X)
                    ant.FacingX = x > ant.X ? 1 : -1;
                ant.Cooldown = _settings.DropSeconds;
                StoreUse use = ant.StoreUse;
                ant.StoreUse = StoreUse.None;
                if (kind == StorageKind.Water)
                {
                    ant.State = AntState.CarryingWater;
                    ant.ClearSeek();
                    // Solto e com depósito de água com vaga: vai para o depósito; senão rega um canteiro.
                    if (use == StoreUse.Loose && TryStartStock(ant, StorageKind.Water))
                        return true;
                    PlanWaterDelivery(ant);
                    return true;
                }
                ant.PickUp(StoredItems.MaterialOf(item), shade, _grid.ToIndex(x, y));
                switch (use)
                {
                    case StoreUse.Plant:
                        ant.FromStore = true;
                        ant.State = AntState.Carrying;
                        break;
                    case StoreUse.Soil:
                        ant.State = AntState.BringingSoil;
                        break;
                    case StoreUse.Build:
                        ant.ReturningSoil = false;
                        ant.State = AntState.Hauling;
                        break;
                    default:
                        StartDelivery(ant);
                        break;
                }
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- pedidos

        /// <summary>Com depósito de alimentos: canteiro (ou folha da sala Gotas de Orvalho) esperando planta que ninguém traz.</summary>
        bool TryStartPlantFromDepot(Ant ant) =>
            FoodDepot && (AntRoles.Harvests(ant.TaskRole) || ant.TaskRole == AntRole.Gardener) &&
            SectionAllows(WorkSection.Harvest, ant) && PlantDemand - _plantsToBeds > 0 &&
            _storeField.Get(ant.X, ant.Y) != FlowField.Unreachable && TryStartTake(ant, StoreUse.Plant);

        /// <summary>Com depósito de água: canteiro com planta esperando água que ninguém traz.</summary>
        bool TryStartWaterFromDepot(Ant ant) =>
            _bedsAwaitingWater - _waterCarriers > 0 && _waterBedField.Get(ant.X, ant.Y) != FlowField.Unreachable &&
            TryStartTake(ant, StoreUse.Water);

        /// <summary>
        /// Enche o depósito de água com a água do mapa (laguinho, poças, sala Gotas de Orvalho): vai buscar um bloco se o
        /// depósito tem vaga sem ninguém a caminho.
        /// </summary>
        bool TryStartWaterStock(Ant ant)
        {
            const int k = (int)StorageKind.Water;
            // Fora das seções de trabalho: o limite do Jardim não segura o depósito (ver SectionIndex).
            if (!WaterDepot || !AntRoles.CarriesWater(ant.TaskRole) ||
                _storeFree[k] - _storeIncoming[k] <= 0 || _waterField.Get(ant.X, ant.Y) == FlowField.Unreachable ||
                _putFields[k] == null || _putFields[k].Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ant.State = AntState.FetchingWater;
            ant.StoreTarget = StorageKind.Water;
            _storeIncoming[k]++;
            ant.ClearSeek();
            UpdateFetchingWater(ant);
            return true;
        }

        /// <summary>Depósito de construção com vaga livre que ninguém está indo encher.</summary>
        bool BlockRestockWanted
        {
            get
            {
                const int k = (int)StorageKind.Blocks;
                return BlockDepot && _storeFree[k] - _storeIncoming[k] > 0;
            }
        }

        /// <summary>
        /// Depósito de construção com vaga: a ociosa busca terra solta da pilha de fora (inclusive a despejada antes de o
        /// depósito existir) e a guarda no depósito, de onde os consumidores pegam.
        /// </summary>
        bool TryStartRestock(Ant ant)
        {
            if (!BlockRestockWanted || !SectionAllows(WorkSection.Dig, ant) || _builtSoilPileVersion < 0 ||
                _soilPileField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ant.DewJob = false;
            ant.State = AntState.FetchingSoil;
            ant.StoreTarget = StorageKind.Blocks;
            _storeIncoming[(int)StorageKind.Blocks]++;
            UpdateFetchingSoil(ant);
            return true;
        }

        /// <summary>Formiga de transporte ociosa leva um item solto ao destino dele (depósito do tipo ou o de sempre).</summary>
        bool TryStartLoose(Ant ant) =>
            (ant.TaskRole == AntRole.Worker || ant.TaskRole == AntRole.Carrier) && SectionAllows(WorkSection.Harvest, ant) &&
            TryStartTake(ant, StoreUse.Loose);
    }
}
