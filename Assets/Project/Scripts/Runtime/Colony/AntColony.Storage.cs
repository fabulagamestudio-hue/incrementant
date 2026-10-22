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
        /// <summary>Pedaço de planta do depósito de alimentos que quem trabalha na sala de nutrientes leva a um canteiro.</summary>
        BedPlant,
        /// <summary>Areia (do depósito de areia ou solta) que a fazendeira leva ao cocho de um pulgão.</summary>
        FarmSand,
        /// <summary>Água (do depósito de água ou solta) que a fazendeira leva ao cocho de um pulgão.</summary>
        FarmWater,
        /// <summary>Cogumelo luminoso (do depósito de construção ou solto) para uma marca do pincel Cogumelo luminoso.</summary>
        Mushroom,
    }

    /// <summary>
    /// Depósitos de alimentos, de construção e de água (pedido do desenvolvedor em 2026-10-19). O que as formigas trazem de
    /// fora (planta cortada, terra cavada, água do mapa) vai primeiro para o depósito do tipo, onde fica como item guardado
    /// (<see cref="PixelGrid.GetStored"/>: não bloqueia a passagem); quem consome (canteiros, sala Gotas de Orvalho, obra)
    /// pega do depósito. Só sem o depósito do tipo o consumo usa o que está do lado de fora, como antes.
    /// </summary>
    public sealed partial class AntColony
    {
        // Índice = StorageKind (0 sem uso; Waste nunca é pintado, fica parado).
        const int KindSlots = 7;
        // Por tipo (índice = StorageKind; 0 sem uso): campo até as células livres do depósito e até os itens guardados nele.
        readonly FlowField[] _putFields = new FlowField[KindSlots];
        readonly FlowField[] _takeFields = new FlowField[KindSlots];
        readonly bool[] _storeFieldIdle = { true, true, true, true, true, true, true };
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
        // Vagas dos depósitos com dona (Ant.OrderSlot), refeitas a cada passo; o hash pede uma rodada de campos quando mudam.
        readonly System.Collections.Generic.HashSet<int> _slotClaims = new System.Collections.Generic.HashSet<int>();
        long _slotClaimHash, _builtSlotClaimHash;
        // Itens soltos com destino, por tipo (planta, água, o resto), da última rodada: a vaga de quem vai buscá-los é limitada
        // pelo que o destino ainda aceita (LooseRoom), não pelo número de itens (relato de 2026-10-19).
        int _looseFoodItems, _looseWaterItems, _looseOtherItems;
        int _looseTakers;
        int _builtStoreVersion = -1;
        // Planta na mão a caminho dos canteiros vinda do depósito de alimentos (com o depósito, a de fora vai para ele).
        int _plantsToBeds;
        // Parte de _plantsToBeds buscada por quem trabalha na sala de nutrientes (StoreUse.BedPlant): vai só aos canteiros.
        int _gardenPlants;

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

        /// <summary>
        /// Pedaços de planta já fora da planta esperando quem os leve: soltos no chão ao alcance das formigas
        /// (<see cref="_reachableDebris"/>) e soltos fora de depósito. O pixel solto que caiu num bolsão fechado não conta,
        /// senão ocuparia uma vaga para sempre e pararia o corte (KI-011).
        /// </summary>
        int PlantsWaiting => _reachableDebris + _loosePlants;

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
            _gardenPlants = 0;
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
                ant.GardenSupply = false;
            }
            if (ant.State == AntState.TakingStored)
            {
                switch (ant.StoreUse)
                {
                    case StoreUse.Loose: _looseTakers++; break;
                    case StoreUse.Plant: _storeOutgoing[(int)StorageKind.Food]++; _plantsToBeds++; break;
                    case StoreUse.BedPlant: _storeOutgoing[(int)StorageKind.Food]++; _plantsToBeds++; _gardenPlants++; break;
                    case StoreUse.Water: _storeOutgoing[(int)StorageKind.Water]++; _waterCarriers++; break;
                    case StoreUse.Build: _storeOutgoing[(int)StorageKind.Blocks]++; _activeBuilders++; break;
                    case StoreUse.FarmSand:
                    case StoreUse.FarmWater:
                    case StoreUse.Mushroom: break; // a fazenda conta os seus (CountFarm)
                    default: _storeOutgoing[(int)StorageKind.Blocks]++; break; // terra fofa: CountCultivation conta o destino
                }
                return;
            }
            if (ant.StoreTarget != StorageKind.None)
                _storeIncoming[(int)ant.StoreTarget] += LoadOf(ant);
        }

        /// <summary>Itens que a formiga leva agora: os blocos de água na boca ou os pixels na mão (no mínimo 1).</summary>
        static int LoadOf(Ant ant) =>
            ant.State == AntState.CarryingWater ? Mathf.Max(1, ant.WaterLoad) : Mathf.Max(1, ant.CarriedCount);

        // ---------------------------------------------------------------- campos

        /// <summary>
        /// Campos dos depósitos: até as vagas livres (levar) e até os itens guardados (pegar), um par por tipo, e até os itens
        /// soltos que têm para onde ir. Roda antes dos campos do cultivo e da escavação, que usam as vagas e os itens.
        /// </summary>
        void BuildStorageFields()
        {
            _builtStoreVersion = _grid.StoreVersion;
            _builtSlotClaimHash = _slotClaimHash;
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
            int reservedFree = 0;
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
                        // A vaga reservada por uma ordem conta como livre (quem vai a ela está em _storeIncoming), mas sai do
                        // campo: as outras formigas descem para outra.
                        if (_slotClaims.Contains(_grid.ToIndex(x, y)))
                            reservedFree++;
                        else
                            _sources.Add(_grid.ToIndex(x, y));
                    }
                }
            }
            _storeFree[k] = _sources.Count + reservedFree;
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
            if (_grid.StoredCount(StorageKind.Food) + _grid.StoredCount(StorageKind.Blocks) + _grid.StoredCount(StorageKind.Water) +
                _grid.StoredCount(StorageKind.Sand) + _grid.StoredCount(StorageKind.Mushroom) == 0)
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
            byte item = _grid.GetStored(x, y);
            kind = StoredItems.KindOf(item);
            // A folha ou a terra que a área de resíduos está convertendo fica onde está (modo da área, ver WasteMode), e o que
            // a fazenda segura (cochos, areia sem depósito de areia, cogumelo sem depósito de cogumelos) também.
            return kind != StorageKind.None && kind != StorageKind.Waste && _grid.GetStorageRoom(x, y) != kind &&
                   !IsConversionInput(x, y, item) && !IsFarmHeld(x, y, item);
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
            _storeIncoming[k] += LoadOf(ant);
            return true;
        }

        /// <summary>
        /// Leva o item à vaga reservada da ordem (<see cref="Ant.OrderSlot"/>) pelo caminho guardado e o guarda nela. Sem vaga
        /// reservada, reserva a mais perto pelo campo das vagas; se a vaga deixar de servir no caminho, reserva outra na hora,
        /// em vez de seguir o campo com o item na mão. Sem vaga nem caminho, desiste do depósito.
        /// </summary>
        void UpdateStocking(Ant ant)
        {
            StorageKind kind = ant.StoreTarget;
            FlowField field = _putFields[(int)kind];
            int d = field != null && HasDepot(kind) && !_storeFieldIdle[(int)kind] ? field.Get(ant.X, ant.Y) : FlowField.Unreachable;
            int w = _grid.Width;
            if (ant.OrderSlot >= 0 && !IsPutSpot(ant.OrderSlot % w, ant.OrderSlot / w, kind))
                ReleaseSlot(ant);
            if (ant.OrderSlot < 0)
            {
                if (d == FlowField.Unreachable)
                {
                    AbandonStock(ant);
                    return;
                }
                if (!ReserveSlot(ant, kind, field))
                {
                    // Todas as vagas perto já têm dona: espera o campo ser refeito sem elas.
                    Hold(ant);
                    return;
                }
            }
            int sx = ant.OrderSlot % w, sy = ant.OrderSlot / w;
            if (Mathf.Abs(sx - ant.X) <= 1 && Mathf.Abs(sy - ant.Y) <= 1 && TryPutStored(ant, kind))
                return;
            if (StepPath(ant) > 0)
                return;
            // Sem caminho (acabou de pegar a água: a vaga foi travada antes de sair) ou caminho cortado: troca a vaga pela
            // livre mais perto de onde está, com caminho novo. Uma busca longa até a vaga antiga, repetida a cada passo
            // enquanto falhasse, pesava demais com centenas de formigas.
            ReleaseSlot(ant);
            if (d == FlowField.Unreachable || !ReserveSlot(ant, kind, field))
            {
                Hold(ant);
                return;
            }
            StepPath(ant);
        }

        /// <summary>
        /// Reserva a vaga livre mais perto pelo campo das vagas (sem dona) e guarda o caminho até ela. A célula de chegada do
        /// campo pode já ter sido reservada neste mesmo passo (o campo é de antes): aí vale uma vizinha livre dela.
        /// </summary>
        bool ReserveSlot(Ant ant, StorageKind kind, FlowField field)
        {
            if (!TracePath(ant, field, out int x, out int y, out _))
                return false;
            for (int k = -1; k < 8; k++)
            {
                int tx = k < 0 ? x : x + AntNavigation.Dx[k], ty = k < 0 ? y : y + AntNavigation.Dy[k];
                if (!_grid.InBounds(tx, ty) || !IsPutSpot(tx, ty, kind) || _slotClaims.Contains(_grid.ToIndex(tx, ty)))
                    continue;
                ant.OrderSlot = _grid.ToIndex(tx, ty);
                ClaimSlot(ant.OrderSlot);
                return true;
            }
            // Várias chegaram juntas ao mesmo ponto do campo (de antes das reservas): a vaga livre mais perto dali.
            int slot = NearestFreeSlot(x, y, kind);
            int end = ant.SeekLast >= 0 ? ant.SeekLast : _grid.ToIndex(ant.X, ant.Y);
            if (slot < 0 || !AppendPathTo(ant, end, slot, SlotSearchLimit))
            {
                ant.ClearSeek();
                return false;
            }
            ant.OrderSlot = slot;
            ClaimSlot(slot);
            return true;
        }

        /// <summary>Teto de células da busca local de vaga livre (<see cref="NearestFreeSlot"/>).</summary>
        const int SlotSearchLimit = 4000;

        /// <summary>Vaga livre e sem dona mais perto de (<paramref name="x"/>, <paramref name="y"/>) andando pelas células caminháveis; -1 = nenhuma.</summary>
        int NearestFreeSlot(int x, int y, StorageKind kind)
        {
            int w = _grid.Width;
            int start = y * w + x;
            if (!_walkability.IsWalkableAt(start))
                return -1;
            int stamp = ++_floodCounter;
            int head = 0, tail = 0;
            _floodStamp[start] = stamp;
            _floodQueue[tail++] = start;
            while (head < tail)
            {
                int i = _floodQueue[head++];
                int cx = i % w, cy = i / w;
                if (IsPutSpot(cx, cy, kind) && !_slotClaims.Contains(i))
                    return i;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + AntNavigation.Dx[k], ny = cy + AntNavigation.Dy[k];
                    if (!_walkability.IsWalkable(nx, ny))
                        continue;
                    int n = ny * w + nx;
                    if (_floodStamp[n] == stamp || tail >= SlotSearchLimit)
                        continue;
                    _floodStamp[n] = stamp;
                    _floodQueue[tail++] = n;
                }
            }
            return -1;
        }

        /// <summary>
        /// Vaga livre e sem dona mais perto de (<paramref name="x"/>, <paramref name="y"/>) pelo campo das vagas, sem guardar
        /// caminho (a formiga ainda vai buscar o item). -1 = nenhuma.
        /// </summary>
        int FindFreeSlot(StorageKind kind, int x, int y)
        {
            FlowField field = _putFields[(int)kind];
            if (field == null || _storeFieldIdle[(int)kind])
                return -1;
            int d = field.Get(x, y), previous = -1, steps = 0;
            if (d == FlowField.Unreachable)
                return -1;
            while (d > 0 && steps++ < MaxSeekPath)
            {
                int k = DownhillDirection(x, y, field, previous);
                if (k < 0)
                    return -1;
                previous = k;
                x += AntNavigation.Dx[k];
                y += AntNavigation.Dy[k];
                d = field.Get(x, y);
            }
            for (int k = -1; k < 8; k++)
            {
                int tx = k < 0 ? x : x + AntNavigation.Dx[k], ty = k < 0 ? y : y + AntNavigation.Dy[k];
                if (_grid.InBounds(tx, ty) && IsPutSpot(tx, ty, kind) && !_slotClaims.Contains(_grid.ToIndex(tx, ty)))
                    return _grid.ToIndex(tx, ty);
            }
            return NearestFreeSlot(x, y, kind);
        }

        /// <summary>Trava a vaga do depósito para a ordem de uma formiga; o campo das vagas a tira na próxima rodada.</summary>
        void ClaimSlot(int index)
        {
            if (_slotClaims.Add(index))
                _slotClaimHash += index * 2654435761L;
        }

        /// <summary>Larga a vaga reservada (e o caminho até ela).</summary>
        void ReleaseSlot(Ant ant)
        {
            if (ant.OrderSlot >= 0 && _slotClaims.Remove(ant.OrderSlot))
                _slotClaimHash -= ant.OrderSlot * 2654435761L;
            ant.OrderSlot = -1;
            ant.ClearSeek();
        }

        /// <summary>
        /// Refaz a trava da vaga a cada passo, como as outras reservas: vale enquanto a formiga leva (ou vai buscar) algo para
        /// o depósito; fora disso a ordem acabou e a vaga se solta.
        /// </summary>
        void CountSlotOrder(Ant ant)
        {
            if (ant.OrderSlot < 0)
                return;
            bool onOrder = ant.StoreTarget != StorageKind.None &&
                           (ant.State == AntState.Carrying || ant.State == AntState.CarryingWater || ant.State == AntState.FetchingWater);
            if (onOrder)
                ClaimSlot(ant.OrderSlot);
            else
                ant.OrderSlot = -1;
        }

        bool TryPutStored(Ant ant, StorageKind kind)
        {
            for (int k = -1; k < 8; k++)
            {
                int x = k < 0 ? ant.X : ant.X + AntNavigation.Dx[k];
                int y = k < 0 ? ant.Y : ant.Y + AntNavigation.Dy[k];
                if (!_grid.InBounds(x, y) || !IsPutSpot(x, y, kind))
                    continue;
                // Com ordem, só a vaga dela; sem ordem, nunca a vaga de outra.
                int index = _grid.ToIndex(x, y);
                if (ant.OrderSlot >= 0 ? index != ant.OrderSlot : _slotClaims.Contains(index))
                    continue;
                byte item = kind == StorageKind.Water ? StoredItems.Water
                    : kind == StorageKind.Sand ? StoredItems.Sand
                    : StoredItems.Of(kind == StorageKind.Blocks ? CellMaterial.Spoil : ant.Carried);
                _grid.SetStored(x, y, item, kind == StorageKind.Water ? (byte)128 : ant.CarriedShade);
                Emit(SfxId.Drop, x, y);
                StoredItemsTotal++;
                if (x != ant.X)
                    ant.FacingX = x > ant.X ? 1 : -1;
                ant.Cooldown = _settings.DropSeconds;
                ReleaseSlot(ant);
                if (kind == StorageKind.Water)
                {
                    // Com mais água na boca (carregadora), segue para outra vaga: UpdateStocking reserva a próxima.
                    if (ant.WaterLoad > 1)
                    {
                        ant.WaterLoad--;
                        return true;
                    }
                    ant.WaterLoad = 0;
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
            ReleaseSlot(ant);
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
            if (ant.Carried == CellMaterial.GlowMushroom)
            {
                ReturnMushroom(ant); // cogumelo não vai ao despejo: fica no chão como item solto
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
                case StoreUse.BedPlant: _storeOutgoing[(int)StorageKind.Food]++; _plantsToBeds++; _gardenPlants++; break;
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
                case StoreUse.Plant:
                case StoreUse.BedPlant:
                    return StorageKind.Food;
                case StoreUse.Water: return StorageKind.Water;
                default: return StorageKind.Blocks;
            }
        }

        void UpdateTaking(Ant ant)
        {
            FlowField field = IsFarmUse(ant.StoreUse) ? FarmTakeField(ant.StoreUse)
                : ant.StoreUse == StoreUse.Loose ? (_looseFieldIdle ? null : _looseField)
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
                if (IsFarmUse(ant.StoreUse))
                {
                    // Areia e água para o cocho, cogumelo para a marca: de qualquer lugar (depósito ou solto), fora dos cochos.
                    if (item != ItemOf(ant.StoreUse) || !TakesFarmItem(item, x, y))
                        continue;
                    byte farmShade = _grid.GetStoredShade(x, y);
                    _grid.SetStored(x, y, StoredItems.None);
                    Emit(SfxId.Dig, x, y);
                    if (x != ant.X)
                        ant.FacingX = x > ant.X ? 1 : -1;
                    ant.Cooldown = _settings.DropSeconds;
                    ant.StoreUse = StoreUse.None;
                    PickFarmItem(ant, item, farmShade, _grid.ToIndex(x, y));
                    return true;
                }
                StorageKind kind = StoredItems.KindOf(item);
                if (ant.StoreUse == StoreUse.Loose ? !IsLooseItem(x, y, out _) || !LooseHasDestination(kind)
                                                   : kind != KindOf(ant.StoreUse) || _grid.GetStorageRoom(x, y) != kind)
                    continue;
                byte shade = _grid.GetStoredShade(x, y);
                _grid.SetStored(x, y, StoredItems.None);
                Emit(SfxId.Dig, x, y);
                if (x != ant.X)
                    ant.FacingX = x > ant.X ? 1 : -1;
                ant.Cooldown = _settings.DropSeconds;
                StoreUse use = ant.StoreUse;
                ant.StoreUse = StoreUse.None;
                if (kind == StorageKind.Water)
                {
                    ant.State = AntState.CarryingWater;
                    ant.WaterLoad = 1;
                    ant.ClearSeek();
                    if (ant.Role == AntRole.Carrier)
                        TakeMoreStored(ant, use, kind, item);
                    // Solto e com depósito de água com vaga: vai para o depósito; senão rega um canteiro.
                    if (use == StoreUse.Loose && TryStartStock(ant, StorageKind.Water))
                        return true;
                    PlanWaterDelivery(ant);
                    return true;
                }
                ant.PickUp(StoredItems.MaterialOf(item), shade, _grid.ToIndex(x, y));
                if (ant.Role == AntRole.Carrier)
                    TakeMoreStored(ant, use, kind, item);
                switch (use)
                {
                    case StoreUse.Plant:
                    case StoreUse.BedPlant:
                        ant.FromStore = true;
                        ant.GardenSupply = use == StoreUse.BedPlant;
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

        /// <summary>
        /// A carregadora leva mais de cada item (pedido do desenvolvedor em 2026-10-21): depois do primeiro, recolhe os iguais
        /// a até <see cref="TakeMoreReach"/> células dela (o depósito guarda um item por célula, lado a lado; encostada só no
        /// primeiro, os outros ficam a 2) até a carga do papel e só o que o destino ainda pede, para não voltar com sobra. Os
        /// mais perto vêm primeiro; cada item a mais custa o tempo de mais uma pegada.
        /// </summary>
        void TakeMoreStored(Ant ant, StoreUse use, StorageKind kind, byte item)
        {
            bool water = kind == StorageKind.Water;
            int extra = water ? Mathf.Min(WaterCapacity(ant) - 1, ExtraWanted(use, kind))
                              : Mathf.Min(LoadCapacity(ant) - ant.CarriedCount, ExtraWanted(use, kind));
            for (int i = 0; i < TakeMoreOffsets.Length && extra > 0; i++)
            {
                int x = ant.X + TakeMoreOffsets[i].x, y = ant.Y + TakeMoreOffsets[i].y;
                if (!_grid.InBounds(x, y) || _grid.GetStored(x, y) != item)
                    continue;
                if (use == StoreUse.Loose ? !IsLooseItem(x, y, out _) : _grid.GetStorageRoom(x, y) != kind)
                    continue;
                byte shade = _grid.GetStoredShade(x, y);
                _grid.SetStored(x, y, StoredItems.None);
                if (water)
                    ant.WaterLoad++;
                else
                    ant.PickUp(StoredItems.MaterialOf(item), shade, _grid.ToIndex(x, y));
                ant.Cooldown += _settings.DropSeconds;
                extra--;
            }
            // Água para os canteiros: as a mais já contam como levadas, para as outras não buscarem a mesma rega.
            if (water && use == StoreUse.Water)
                _waterCarriers += ant.WaterLoad - 1;
        }

        const int TakeMoreReach = 2;

        /// <summary>Células em volta da formiga até <see cref="TakeMoreReach"/>, das mais perto às mais longe (xadrez, depois reta).</summary>
        static readonly Vector2Int[] TakeMoreOffsets = BuildTakeMoreOffsets();

        static Vector2Int[] BuildTakeMoreOffsets()
        {
            var list = new System.Collections.Generic.List<Vector2Int>();
            for (int dy = -TakeMoreReach; dy <= TakeMoreReach; dy++)
            for (int dx = -TakeMoreReach; dx <= TakeMoreReach; dx++)
                if (dx != 0 || dy != 0)
                    list.Add(new Vector2Int(dx, dy));
            list.Sort((a, b) =>
            {
                int ca = Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.y)), cb = Mathf.Max(Mathf.Abs(b.x), Mathf.Abs(b.y));
                return ca != cb ? ca.CompareTo(cb) : (Mathf.Abs(a.x) + Mathf.Abs(a.y)).CompareTo(Mathf.Abs(b.x) + Mathf.Abs(b.y));
            });
            return list.ToArray();
        }

        /// <summary>Quantos itens além do primeiro o destino de <paramref name="use"/> ainda aceita (o primeiro já foi contado).</summary>
        int ExtraWanted(StoreUse use, StorageKind kind)
        {
            switch (use)
            {
                case StoreUse.BedPlant: return _bedsAwaitingPlant - _gardenPlants;
                case StoreUse.Water: return _bedsAwaitingWater - _waterCarriers;
                case StoreUse.Plant: return _dewLeafSpots - (_plantsToBeds - _gardenPlants);
                case StoreUse.Build: return BuildWorkCount - _activeBuilders;
                case StoreUse.Loose: return kind == StorageKind.Food || kind == StorageKind.Water ? LooseRoom(kind) : int.MaxValue;
                default: return 0; // terra para canteiro e teto da sala de orvalho: um bloco por pedido
            }
        }

        // ---------------------------------------------------------------- pedidos

        /// <summary>
        /// Com depósito de alimentos: folha da sala Gotas de Orvalho esperando planta que ninguém traz. Os canteiros da sala
        /// de nutrientes não entram aqui: quem trabalha na sala busca a planta deles (<see cref="TryStartGardenSupply"/>).
        /// </summary>
        bool TryStartPlantFromDepot(Ant ant) =>
            FoodDepot && (AntRoles.Harvests(ant.TaskRole) || ant.TaskRole == AntRole.Gardener) &&
            SectionAllows(WorkSection.Harvest, ant) && _dewLeafSpots - (_plantsToBeds - _gardenPlants) > 0 &&
            _storeField.Get(ant.X, ant.Y) != FlowField.Unreachable && TryStartTake(ant, StoreUse.Plant);

        /// <summary>
        /// Abastecimento da sala de nutrientes pelos depósitos (pedido do desenvolvedor em 2026-10-21): quem trabalha na sala
        /// (<see cref="AntRoles.Gardens"/>: jardineira e comum) e a carregadora buscam no depósito de água a água dos canteiros com planta e,
        /// no de alimentos, a planta dos canteiros só com terra. Sem o depósito do tipo nada muda: a água vem do mapa
        /// (<see cref="TryStartWaterFetch"/>) e a planta, de quem corta.
        /// </summary>
        bool TryStartGardenSupply(Ant ant)
        {
            // A carregadora também entrega do estoque aos canteiros (pedido do desenvolvedor em 2026-10-21).
            if (!AntRoles.Gardens(ant.TaskRole) && ant.TaskRole != AntRole.Carrier || !SectionAllows(WorkSection.Garden, ant))
                return false;
            if (WaterDepot && _bedsAwaitingWater - _waterCarriers > 0 && _waterBedField.Get(ant.X, ant.Y) != FlowField.Unreachable &&
                TryStartTake(ant, StoreUse.Water))
                return true;
            return FoodDepot && _bedsAwaitingPlant - _gardenPlants > 0 && _bedPlantField.Get(ant.X, ant.Y) != FlowField.Unreachable &&
                   TryStartTake(ant, StoreUse.BedPlant);
        }

        /// <summary>Há canteiro esperando planta ou água que um depósito pode abastecer e que ninguém foi buscar.</summary>
        bool GardenSupplyOpen
        {
            get
            {
                const int food = (int)StorageKind.Food, water = (int)StorageKind.Water;
                return WaterDepot && _bedsAwaitingWater - _waterCarriers > 0 && _storeStock[water] - _storeOutgoing[water] > 0 ||
                       FoodDepot && _bedsAwaitingPlant - _gardenPlants > 0 && _storeStock[food] - _storeOutgoing[food] > 0;
            }
        }

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
            // Água do mapa toda reservada, ou outra formiga já não achou bloco livre neste passo (o campo da água é o mesmo):
            // nem tenta. Sem isso, com pouca água e depósito grande, centenas de formigas saíam e refaziam o caminho a cada
            // passo atrás de um bloco que não havia (relato de 2026-10-20: 605 formigas buscando 454 células de água).
            if (_grid.WaterCount - _waterClaims.Count <= 0 || _waterStockFailedAt == SimulationTime)
                return false;
            // A ordem nasce com as duas pontas travadas: a vaga do depósito e o bloco de água.
            int slot = FindFreeSlot(StorageKind.Water, ant.X, ant.Y);
            if (slot < 0)
            {
                // Sem vaga livre (as do depósito já têm dona): a busca desce o campo e varre as vizinhas, e a das outras
                // formigas neste passo dá no mesmo (medido em 2026-10-21: ~560 µs por ociosa, ~850 ociosas por tick).
                _waterStockFailedAt = SimulationTime;
                return false;
            }
            ant.ClearSeek();
            if (!PlanWaterFetch(ant))
            {
                ant.ClearSeek();
                _waterStockFailedAt = SimulationTime;
                return false;
            }
            ant.State = AntState.FetchingWater;
            ant.StoreTarget = StorageKind.Water;
            _storeIncoming[k]++;
            ant.OrderSlot = slot;
            ClaimSlot(slot);
            UpdateFetchingWater(ant);
            return true;
        }

        // Tempo simulado do passo em que uma formiga não achou bloco de água livre para uma ordem de depósito.
        float _waterStockFailedAt = -1f;

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
            ant.WasteDirtJob = false;
            ant.State = AntState.FetchingSoil;
            ant.StoreTarget = StorageKind.Blocks;
            _storeIncoming[(int)StorageKind.Blocks]++;
            UpdateFetchingSoil(ant);
            return true;
        }

        /// <summary>
        /// Trabalho da carregadora que enche os depósitos, na ordem: itens soltos (ao depósito do tipo ou ao destino de sempre),
        /// água do mapa para o depósito de água e terra da pilha de fora para o depósito de construção.
        /// </summary>
        bool TryStockWork(Ant ant) => TryStartLoose(ant) || TryStartWaterStock(ant) || TryStartRestock(ant);

        /// <summary>
        /// Trabalho da carregadora que esvazia os depósitos para quem consome: planta e água aos canteiros, folha da sala Gotas
        /// de Orvalho e, com depósito de construção, terra para canteiro novo, teto da sala de orvalho e obra.
        /// </summary>
        bool TryDeliverFromStock(Ant ant) =>
            TryStartGardenSupply(ant) || TryStartPlantFromDepot(ant) ||
            BlockDepot && (TryStartSoilFetch(ant) || _hasSite && TryBuildWork(ant));

        /// <summary>Formiga de transporte ociosa (comum, carregadora, lixeira) leva um item solto ao destino dele (depósito do tipo ou o de sempre).</summary>
        bool TryStartLoose(Ant ant) =>
            (ant.TaskRole == AntRole.Worker || ant.TaskRole == AntRole.Carrier || ant.TaskRole == AntRole.Midden) &&
            SectionAllows(WorkSection.Harvest, ant) &&
            TryStartTake(ant, StoreUse.Loose);
    }
}
