using System.Collections.Generic;
using Fabula.Incrementant.Audio;
using Fabula.Incrementant.Juice;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Fazenda de pulgões (pedido do desenvolvedor em 2026-10-22). O chão da fazenda pintada é dividido em baias de
    /// <see cref="StallWidth"/> células: cocho de areia, cocho de água (lado a lado) e o lugar do pulgão. A fazenda cria um
    /// pulgão por vez até o limite (<see cref="AphidCap"/>) e as baias livres. Sem fazendeira, o pulgão só pinga água; com a
    /// fazendeira ordenhando e os dois cochos cheios, ele come a areia, bebe a água e solta areia, folhas, água e às vezes um
    /// cogumelo luminoso, como itens soltos na fazenda (as formigas os levam aos depósitos). Mordido por um cupim, vira o
    /// pulgão preto e ataca as formigas até morrer. Guia: FarmGuide.md.
    /// </summary>
    public sealed partial class AntColony
    {
        /// <summary>Largura (células) de uma baia: cocho de areia, cocho de água e o pulgão (2 células, o sprite tem 2,5).</summary>
        public const int StallWidth = 4;
        const int WaterTroughOffset = 1;
        const int AphidOffset = 2;
        /// <summary>Teto de células da busca de lugar livre para o que a ordenha solta.</summary>
        const int FarmSpotSearchLimit = 800;
        /// <summary>Até onde (células, em xadrez) o pulgão preto enxerga uma formiga para ir atrás dela.</summary>
        const int BlackAphidSight = 16;

        readonly List<Aphid> _aphids = new List<Aphid>();
        // Baias da última leitura do chão da fazenda: célula do cocho de areia, em ordem (linha, coluna).
        readonly List<int> _stalls = new List<int>();
        readonly HashSet<int> _stallSet = new HashSet<int>();
        // Todas as células das baias (cochos e lugar do pulgão): o que está nelas não é item solto nem recebe o que a ordenha solta.
        readonly HashSet<int> _stallCells = new HashSet<int>();
        // Chão, paredes e teto da fazenda (células caminháveis encostadas em algo sólido): por onde os pulgões passeiam.
        readonly List<int> _farmSurface = new List<int>();
        // Cochos com dona (fazendeira indo buscar ou levando o item), refeitos a cada passo.
        readonly HashSet<int> _troughClaims = new HashSet<int>();
        FlowField _farmField;
        FlowField _farmSandField;
        FlowField _farmWaterField;
        FlowField _mushroomField;
        FlowField _mushroomSiteField;
        bool _farmFieldsIdle = true;
        bool _mushroomFieldIdle = true;
        bool _mushroomSiteIdle = true;
        int _builtStallMarkVersion = -1;
        long _builtStallChunkHash;
        // Itens que a fazendeira pode levar aos cochos e cogumelos para as marcas (contados na última rodada de campos).
        int _farmSandItems, _farmWaterItems, _mushroomItems;
        // Cochos vazios das baias com pulgão (recontados a cada passo) e quem está atendendo cada tipo.
        int _troughsWantSand, _troughsWantWater;
        int _sandFeeders, _waterFeeders;
        int _mushroomTakers, _mushroomHaulers;
        // Marcas de cogumelo que dá para encaixar agora (última rodada de campos).
        int _mushroomSites;
        readonly List<int> _mushroomSiteSources = new List<int>();
        float _breedTimer;
        int _nextAphidId = 1;
        readonly List<int> _farmSpots = new List<int>();
        System.Random _farmRng;

        System.Random FarmRng => _farmRng ??= new System.Random(_seed ^ 0xA9417D);

        /// <summary>Pulgões saudáveis da fazenda (o pulgão preto está em <see cref="Termites"/>).</summary>
        public IReadOnlyList<Aphid> Aphids => _aphids;

        /// <summary>Quantos pulgões a fazenda cria no máximo: a base mais o A+1.</summary>
        public int AphidCap => _settings.AphidBaseCap + Effects.AphidCapBonus;

        /// <summary>Baias no chão da fazenda (cada pulgão precisa de uma).</summary>
        public int StallCount => _stalls.Count;

        /// <summary>A câmara A0 foi comprada: o pincel da fazenda está liberado.</summary>
        public bool FarmUnlocked => Effects.FarmUnlocked;

        /// <summary>Segundos até o próximo pulgão nascer, com vaga e baia livre; negativo = não há vaga.</summary>
        public float SecondsToNextAphid => CanBreedAphid ? Mathf.Max(0f, BreedSeconds - _breedTimer) : -1f;

        public int AphidsBorn { get; private set; }
        public int AphidsCorrupted { get; private set; }
        public int AphidsKilled { get; private set; }
        public int AphidMilkings { get; private set; }
        public int AphidDrips { get; private set; }
        public int MushroomsHarvested { get; private set; }
        public int MushroomsPlanted { get; private set; }

        /// <summary>Cogumelos luminosos guardados no depósito de cogumelos ou soltos, prontos para o pincel Cogumelo luminoso.</summary>
        public int MushroomItems => _grid.StoredCount(StorageKind.Mushroom);

        /// <summary>Cochos vazios esperando areia ou água agora.</summary>
        public int TroughsWaiting => _troughsWantSand + _troughsWantWater;

        float BreedSeconds => _settings.AphidBreedSeconds * Effects.AphidBreedTime;

        bool CanBreedAphid => _aphids.Count < AphidCap && FreeStall() >= 0;

        bool SandDepot => HasDepot(StorageKind.Sand);

        bool MushroomDepot => HasDepot(StorageKind.Mushroom);

        // ---------------------------------------------------------------- baias

        /// <summary>Chão da fazenda: célula pintada, livre (sem material, nutriente nem água), caminhável e com algo sólido embaixo.</summary>
        bool IsFarmFloor(int x, int y) =>
            _grid.IsFarmRoom(x, y) && !_grid.IsSolid(x, y) && _grid.IsSolid(x, y - 1) && !_grid.HasNutrient(x, y) &&
            _walkability.IsWalkable(x, y);

        /// <summary>Destino de passeio: célula da fazenda caminhável encostada em algo sólido (chão, parede ou teto).</summary>
        bool IsFarmSurface(int x, int y) =>
            _grid.IsFarmRoom(x, y) && AntNavigation.IsWalkable(_grid, x, y) && AntNavigation.TouchesSolid(_grid, x, y);

        Vector2Int AphidCell(int stall) =>
            new Vector2Int(stall % _grid.Width + AphidOffset, stall / _grid.Width);

        int WaterTrough(int stall) => stall + WaterTroughOffset;

        /// <summary>
        /// Lê as baias (e o chão onde os pulgões passeiam) de novo quando a fazenda foi pintada ou o terreno dela mudou. As
        /// baias com pulgão ficam onde estão enquanto as 4 células delas continuarem chão (os cochos cheios não mudam de lugar
        /// quando o chão muda ao lado). O resto de cada trecho reto de chão é cortado em baias de <see cref="StallWidth"/>, da
        /// esquerda para a direita, pulando as baias mantidas. Pulgão cuja baia sumiu fica sem baia e procura outra livre.
        /// </summary>
        void CollectStalls()
        {
            long chunkHash = 17;
            if (_grid.FarmRoomCount > 0)
            {
                for (int cy = 0; cy < _grid.ChunkRows; cy++)
                for (int cx = 0; cx < _grid.ChunkColumns; cx++)
                {
                    if (_grid.GetChunkFarmRoomCount(cx, cy) > 0)
                        chunkHash = chunkHash * 31 + (cy * _grid.ChunkColumns + cx) * 7919L + _grid.GetChunkVersion(cx, cy);
                }
            }
            if (_grid.MarkVersion == _builtStallMarkVersion && chunkHash == _builtStallChunkHash)
                return;
            _builtStallMarkVersion = _grid.MarkVersion;
            _builtStallChunkHash = chunkHash;
            _stalls.Clear();
            _stallSet.Clear();
            _stallCells.Clear();
            _farmSurface.Clear();
            foreach (Aphid aphid in _aphids)
                aphid.WanderTarget = -1;
            if (_grid.FarmRoomCount > 0)
            {
                int w = _grid.Width;
                foreach (Aphid aphid in _aphids)
                {
                    if (aphid.Stall >= 0 && IsFreeStallSpot(aphid.Stall))
                        AddStall(aphid.Stall);
                    else
                        aphid.Stall = -1;
                }
                RectInt area = _grid.Active;
                for (int cy = 0; cy < _grid.ChunkRows; cy++)
                for (int cx = 0; cx < _grid.ChunkColumns; cx++)
                {
                    if (_grid.GetChunkFarmRoomCount(cx, cy) == 0)
                        continue;
                    int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift), x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                    int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift), y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                    for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        if (IsFarmSurface(x, y))
                            _farmSurface.Add(y * w + x);
                        // Começo de um trecho de chão: o trecho segue para a direita, mesmo passando para o bloco vizinho.
                        if (!IsFarmFloor(x, y) || IsFarmFloor(x - 1, y))
                            continue;
                        int end = x;
                        while (IsFarmFloor(end + 1, y))
                            end++;
                        int sx = x;
                        while (sx + StallWidth - 1 <= end)
                        {
                            int taken = LastStallCell(y * w + sx);
                            if (taken >= 0)
                            {
                                sx = taken % w + 1; // depois da baia mantida
                                continue;
                            }
                            AddStall(y * w + sx);
                            sx += StallWidth;
                        }
                    }
                }
                _stalls.Sort();
            }
            else
            {
                foreach (Aphid aphid in _aphids)
                    aphid.Stall = -1;
            }
            // O que estava num cocho que deixou de existir vira item solto.
            _grid.TouchStore();
        }

        /// <summary>As <see cref="StallWidth"/> células a partir de <paramref name="stall"/> são chão, na mesma linha, e nenhuma é de outra baia.</summary>
        bool IsFreeStallSpot(int stall)
        {
            int w = _grid.Width;
            int x = stall % w, y = stall / w;
            if (x + StallWidth > w)
                return false;
            for (int k = 0; k < StallWidth; k++)
            {
                if (!IsFarmFloor(x + k, y) || _stallCells.Contains(stall + k))
                    return false;
            }
            return true;
        }

        /// <summary>Célula mais à direita, entre as <see cref="StallWidth"/> a partir de <paramref name="stall"/>, que já é de uma baia; -1 = nenhuma.</summary>
        int LastStallCell(int stall)
        {
            for (int k = StallWidth - 1; k >= 0; k--)
            {
                if (_stallCells.Contains(stall + k))
                    return stall + k;
            }
            return -1;
        }

        void AddStall(int stall)
        {
            _stalls.Add(stall);
            _stallSet.Add(stall);
            for (int k = 0; k < StallWidth; k++)
                _stallCells.Add(stall + k);
        }

        /// <summary>Primeira baia sem pulgão (-1 = nenhuma).</summary>
        int FreeStall()
        {
            foreach (int stall in _stalls)
            {
                if (!StallTaken(stall))
                    return stall;
            }
            return -1;
        }

        bool StallTaken(int stall)
        {
            foreach (Aphid aphid in _aphids)
            {
                if (aphid.Stall == stall)
                    return true;
            }
            return false;
        }

        /// <summary>Baia sem pulgão mais perto da célula (em xadrez); -1 = nenhuma.</summary>
        int NearestFreeStall(int x, int y)
        {
            int best = -1, bestDistance = int.MaxValue;
            foreach (int stall in _stalls)
            {
                if (StallTaken(stall))
                    continue;
                Vector2Int cell = AphidCell(stall);
                int d = Mathf.Max(Mathf.Abs(cell.x - x), Mathf.Abs(cell.y - y));
                if (d < bestDistance)
                {
                    best = stall;
                    bestDistance = d;
                }
            }
            return best;
        }

        /// <summary>O item está preso na fazenda: cocho ou lugar de uma baia, cogumelo sem depósito de cogumelos com vaga, areia da fazenda sem depósito de areia.</summary>
        bool IsFarmHeld(int x, int y, byte item)
        {
            if (_stallCells.Contains(_grid.ToIndex(x, y)))
                return true;
            if (item == StoredItems.Mushroom)
                return !(MushroomDepot && _storeFree[(int)StorageKind.Mushroom] > 0);
            return item == StoredItems.Sand && !SandDepot && _grid.IsFarmRoom(x, y);
        }

        bool IsTroughCell(int index) => _stallCells.Contains(index);

        // ---------------------------------------------------------------- campos

        /// <summary>
        /// Campos da fazenda, na etapa dos depósitos: até as baias com pulgão (a fazendeira entra por ele), até a areia e a água
        /// que ela pode levar aos cochos (depósitos e soltas, fora dos cochos) e até os cogumelos (depósito de cogumelos e
        /// soltos). Parados (sem fazenda nem cogumelo), não custam nada.
        /// </summary>
        void BuildFarmFields()
        {
            _farmDirty = false;
            CollectStalls();
            BuildFarmItemFields();
            if (_aphids.Count == 0 || _stalls.Count == 0)
            {
                if (!_farmFieldsIdle)
                {
                    _farmField.Invalidate();
                    _farmSandField.Invalidate();
                    _farmWaterField.Invalidate();
                    _farmFieldsIdle = true;
                }
                return;
            }
            _farmFieldsIdle = false;
            _farmField ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
            _farmSandField ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
            _farmWaterField ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
            _sources.Clear();
            foreach (Aphid aphid in _aphids)
            {
                if (aphid.Stall < 0)
                    continue;
                for (int k = 0; k < StallWidth; k++)
                {
                    int i = aphid.Stall + k;
                    if (_walkability.IsWalkableAt(i))
                        _sources.Add(i);
                }
            }
            UpdateField(_farmField, _sources);
            CollectFarmItems(StoredItems.Sand, _sources);
            _farmSandItems = _sources.Count;
            UpdateField(_farmSandField, _sources);
            CollectFarmItems(StoredItems.Water, _sources);
            _farmWaterItems = _sources.Count;
            UpdateField(_farmWaterField, _sources);
            _sources.Clear();
        }

        /// <summary>Campo dos cogumelos: os guardados no depósito de cogumelos e os soltos (a fazenda os solta onde ordenha).</summary>
        void BuildFarmItemFields()
        {
            _mushroomItems = 0;
            // Só com cogumelo guardado e marca de cogumelo esperando: sem um dos dois, ninguém busca cogumelo.
            if (_grid.StoredCount(StorageKind.Mushroom) == 0 || _grid.BuildMarkedCount == 0)
            {
                if (!_mushroomFieldIdle)
                {
                    _mushroomField.Invalidate();
                    _mushroomFieldIdle = true;
                }
                return;
            }
            CollectFarmItems(StoredItems.Mushroom, _sources);
            _mushroomItems = _sources.Count;
            if (_mushroomItems == 0 && _mushroomFieldIdle)
                return;
            _mushroomFieldIdle = false;
            _mushroomField ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
            UpdateField(_mushroomField, _sources);
            _sources.Clear();
        }

        /// <summary>Células caminháveis com o item, fora dos cochos e do que a área de resíduos está convertendo.</summary>
        void CollectFarmItems(byte item, List<int> result)
        {
            result.Clear();
            StorageKind kind = StoredItems.KindOf(item);
            if (_grid.StoredCount(kind) == 0)
                return;
            RectInt area = _grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            for (int cy = cyMin; cy <= cyMax; cy++)
            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                if (_grid.GetChunkStoredCount(cx, cy) == 0)
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift), x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift), y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    if (_grid.GetStored(x, y) == item && TakesFarmItem(item, x, y))
                        result.Add(_grid.ToIndex(x, y));
                }
            }
        }

        /// <summary>A fazendeira (ou quem planta o cogumelo) pode pegar este item: fora dos cochos, caminhável e fora da conversão dos resíduos.</summary>
        bool TakesFarmItem(byte item, int x, int y) =>
            !IsTroughCell(_grid.ToIndex(x, y)) && _walkability.IsWalkable(x, y) && !IsConversionInput(x, y, item);

        static bool IsFarmUse(StoreUse use) => use == StoreUse.FarmSand || use == StoreUse.FarmWater || use == StoreUse.Mushroom;

        static byte ItemOf(StoreUse use) =>
            use == StoreUse.FarmSand ? StoredItems.Sand : use == StoreUse.FarmWater ? StoredItems.Water : StoredItems.Mushroom;

        FlowField FarmTakeField(StoreUse use)
        {
            if (use == StoreUse.Mushroom)
                return _mushroomFieldIdle ? null : _mushroomField;
            if (_farmFieldsIdle)
                return null;
            return use == StoreUse.FarmSand ? _farmSandField : _farmWaterField;
        }

        // ---------------------------------------------------------------- contas do passo

        void ResetFarmCounts()
        {
            _sandFeeders = 0;
            _waterFeeders = 0;
            _mushroomTakers = 0;
            _mushroomHaulers = 0;
            _troughClaims.Clear();
            _troughsWantSand = 0;
            _troughsWantWater = 0;
            int w = _grid.Width;
            foreach (Aphid aphid in _aphids)
            {
                aphid.Farmer = null;
                if (aphid.Stall < 0)
                    continue;
                if (_grid.GetStored(aphid.Stall % w, aphid.Stall / w) == StoredItems.None)
                    _troughsWantSand++;
                int water = WaterTrough(aphid.Stall);
                if (_grid.GetStored(water % w, water / w) == StoredItems.None)
                    _troughsWantWater++;
            }
        }

        /// <summary>Quem enche cocho, ordenha ou leva cogumelo (no começo do Tick, junto das outras contagens).</summary>
        void CountFarm(Ant ant)
        {
            switch (ant.State)
            {
                case AntState.TakingStored:
                    if (ant.StoreUse == StoreUse.Mushroom)
                        _mushroomTakers++;
                    else if (ant.StoreUse == StoreUse.FarmSand || ant.StoreUse == StoreUse.FarmWater)
                        CountFeeder(ant, ant.StoreUse == StoreUse.FarmSand);
                    break;
                case AntState.FeedingAphid:
                    CountFeeder(ant, ant.FarmItem == StoredItems.Sand);
                    break;
                case AntState.TendingAphid:
                    Aphid aphid = AphidById(ant.EggId);
                    // Duas no mesmo pulgão (a escolha das duas foi no mesmo passo): a segunda escolhe outro.
                    if (aphid != null && aphid.Farmer == null)
                        aphid.Farmer = ant;
                    else
                        ant.EggId = 0;
                    break;
                case AntState.Hauling:
                case AntState.Placing:
                    if (ant.Carried == CellMaterial.GlowMushroom)
                        _mushroomHaulers++;
                    break;
            }
        }

        void CountFeeder(Ant ant, bool sand)
        {
            if (sand)
                _sandFeeders++;
            else
                _waterFeeders++;
            _troughClaims.Add(_grid.ToIndex(ant.TargetX, ant.TargetY));
        }

        Aphid AphidById(int id)
        {
            if (id <= 0)
                return null;
            foreach (Aphid aphid in _aphids)
            {
                if (aphid.Id == id)
                    return aphid;
            }
            return null;
        }

        // ---------------------------------------------------------------- pulgões

        /// <summary>Nascimento, caminho até a baia, água sem ordenha e a ordenha. Depois das formigas, que acabaram de agir.</summary>
        void TickFarm(float deltaTime)
        {
            TickFleetingMushrooms(deltaTime);
            if (_grid.FarmRoomCount == 0 && _aphids.Count == 0)
            {
                _breedTimer = 0f;
                return;
            }
            if (CanBreedAphid)
            {
                _breedTimer += deltaTime;
                if (_breedTimer >= BreedSeconds)
                {
                    _breedTimer = 0f;
                    BreedAphid(FreeStall());
                }
            }
            else
            {
                _breedTimer = 0f;
            }
            foreach (Aphid aphid in _aphids)
                UpdateAphid(aphid, deltaTime);
        }

        void BreedAphid(int stall)
        {
            Vector2Int cell = AphidCell(stall);
            Aphid aphid = NewAphid(cell.x, cell.y);
            aphid.Stall = stall;
            AphidsBorn++;
            Emit(SfxId.AntBorn, cell.x, cell.y);
            EmitFx(FxId.Hatch, cell.x, cell.y);
        }

        Aphid NewAphid(int x, int y)
        {
            var aphid = new Aphid { Id = _nextAphidId++ };
            Ant body = aphid.Body;
            body.X = x;
            body.Y = y;
            body.FacingX = -1;
            body.SpeedOverride = _settings.AphidSpeed;
            SnapVisual(body);
            body.MoveStart = SimulationTime;
            _aphids.Add(aphid);
            _farmDirty = true;
            return aphid;
        }

        bool _farmDirty;

        void UpdateAphid(Aphid aphid, float deltaTime)
        {
            Ant body = aphid.Body;
            if (UpdateSupport(body, deltaTime))
                return;
            body.Cooldown -= deltaTime;
            if (aphid.Stall < 0)
            {
                aphid.Stall = NearestFreeStall(body.X, body.Y);
                if (aphid.Stall >= 0)
                    _farmDirty = true;
            }
            if (aphid.Stall < 0)
            {
                // Sem baia: não come nem produz; só passeia até abrir uma.
                aphid.EatTimer = 0f;
                if (body.Cooldown <= 0f)
                    StrollAphid(aphid);
                return;
            }

            Ant farmer = aphid.Farmer;
            bool milking = farmer != null && farmer.Interacting && farmer.State == AntState.TendingAphid;
            if (milking && aphid.Fed)
            {
                aphid.MilkTimer += deltaTime * Tree.MilkRate(farmer.Role);
                if (aphid.MilkTimer >= _settings.AphidMilkSeconds * Effects.MilkTime && Milk(aphid))
                    aphid.MilkTimer = 0f;
            }
            else
            {
                aphid.DripTimer += deltaTime;
                if (aphid.DripTimer >= _settings.AphidDripSeconds && DropFarmItems(aphid, 0, 0, 1, false))
                {
                    aphid.DripTimer = 0f;
                    AphidDrips++;
                }
            }

            // Com fome e os dois cochos cheios: vai até eles e come.
            if (!aphid.Fed && TroughsFull(aphid))
            {
                Vector2Int home = AphidCell(aphid.Stall);
                if (body.X == home.x && body.Y == home.y)
                {
                    body.FacingX = -1; // virado para os cochos
                    aphid.EatTimer += deltaTime;
                    if (aphid.EatTimer >= _settings.AphidEatSeconds)
                        Eat(aphid);
                    return;
                }
                aphid.EatTimer = 0f;
                aphid.WanderTarget = -1;
                if (body.Cooldown <= 0f && !StepAphid(body, home.x, home.y))
                    Hold(body);
                return;
            }
            // Alimentado (ou sem comida nos cochos), passeia, mesmo sendo ordenhado: a fazendeira o acompanha.
            aphid.EatTimer = 0f;
            if (body.Cooldown <= 0f)
                StrollAphid(aphid);
        }

        /// <summary>Come a areia e bebe a água dos cochos: fica alimentado (pronto para a ordenha) e os cochos esvaziam.</summary>
        void Eat(Aphid aphid)
        {
            int w = _grid.Width;
            _grid.SetStored(aphid.Stall % w, aphid.Stall / w, StoredItems.None);
            int water = WaterTrough(aphid.Stall);
            _grid.SetStored(water % w, water / w, StoredItems.None);
            aphid.Fed = true;
            aphid.EatTimer = 0f;
            Emit(SfxId.Drop, aphid.X, aphid.Y);
            aphid.Body.Cooldown = RestSeconds();
        }

        /// <summary>Passeio pelo chão da fazenda: anda até uma célula sorteada, para um pouco e sorteia outra.</summary>
        void StrollAphid(Aphid aphid)
        {
            Ant body = aphid.Body;
            int w = _grid.Width;
            if (aphid.WanderTarget >= 0 && body.X == aphid.WanderTarget % w && body.Y == aphid.WanderTarget / w)
            {
                aphid.WanderTarget = -1;
                body.Cooldown = RestSeconds();
                return;
            }
            if (aphid.WanderTarget < 0)
            {
                if (_farmSurface.Count == 0)
                {
                    Hold(body);
                    return;
                }
                aphid.WanderTarget = _farmSurface[FarmRng.Next(_farmSurface.Count)];
            }
            if (!StepAphid(body, aphid.WanderTarget % w, aphid.WanderTarget / w))
            {
                // Sem passo que aproxime (outro trecho de chão, obstáculo): para e sorteia outro destino.
                aphid.WanderTarget = -1;
                body.Cooldown = RestSeconds();
            }
        }

        float RestSeconds()
        {
            float max = _settings.AphidRestSeconds;
            return max <= 0f ? 0.2f : max * (1f / 3f + (float)FarmRng.NextDouble() * 2f / 3f);
        }

        /// <summary>Um passo do pulgão rumo à célula, sem sair da fazenda; entre os que aproximam, prefere os encostados em parede, chão ou teto.</summary>
        bool StepAphid(Ant body, int x, int y)
        {
            int current = Mathf.Max(Mathf.Abs(body.X - x), Mathf.Abs(body.Y - y));
            int best = int.MaxValue;
            int chosen = -1;
            for (int k = 0; k < 8; k++)
            {
                int nx = body.X + AntNavigation.Dx[k], ny = body.Y + AntNavigation.Dy[k];
                if (!_grid.IsFarmRoom(nx, ny) || !AntNavigation.IsWalkable(_grid, nx, ny))
                    continue;
                int distance = Mathf.Max(Mathf.Abs(nx - x), Mathf.Abs(ny - y));
                if (distance >= current)
                    continue;
                int score = distance * 2 + (AntNavigation.TouchesSolid(_grid, nx, ny) ? 0 : 1);
                if (score < best)
                {
                    best = score;
                    chosen = k;
                }
            }
            if (chosen < 0)
                return false;
            MoveTo(body, chosen);
            return true;
        }

        /// <summary>Ordenha pronta: solta a colheita e o pulgão fica com fome de novo. <c>false</c> = sem lugar na fazenda para ela.</summary>
        bool Milk(Aphid aphid)
        {
            bool mushroom = FarmRng.NextDouble() < _settings.MilkMushroomChance;
            if (!DropFarmItems(aphid, _settings.MilkSand, _settings.MilkLeaves, _settings.MilkWater, mushroom))
                return false;
            aphid.Fed = false;
            AphidMilkings++;
            if (mushroom)
                MushroomsHarvested++;
            Emit(SfxId.Plant, aphid.X, aphid.Y);
            return true;
        }

        /// <summary>
        /// Solta os itens nas células livres da fazenda mais perto do pulgão (sem material, item, nutriente nem água, fora das
        /// baias). Sem lugar para todos, não solta nenhum e devolve false (o pulgão espera as formigas levarem o que já está lá).
        /// </summary>
        bool DropFarmItems(Aphid aphid, int sand, int leaves, int water, bool mushroom)
        {
            int count = sand + leaves + water + (mushroom ? 1 : 0);
            if (count == 0)
                return true;
            FindFarmSpots(aphid.X, aphid.Y, count, _farmSpots);
            if (_farmSpots.Count < count)
                return false;
            int w = _grid.Width, next = 0;
            for (int i = 0; i < sand; i++)
                PlaceFarmItem(_farmSpots[next++], StoredItems.Sand, w);
            for (int i = 0; i < leaves; i++)
                PlaceFarmItem(_farmSpots[next++], StoredItems.Of(CellMaterial.PlantLeaf), w);
            for (int i = 0; i < water; i++)
                PlaceFarmItem(_farmSpots[next++], StoredItems.Water, w);
            if (mushroom)
            {
                int cell = _farmSpots[next];
                PlaceFarmItem(cell, StoredItems.Mushroom, w);
                // Poeira na cor do cogumelo (o "+1" do ganho de nutriente diria que entrou nutriente).
                EmitFx(FxId.Build, cell % w, cell / w, CellMaterial.GlowMushroom);
            }
            return true;
        }

        void PlaceFarmItem(int cell, byte item, int width)
        {
            _grid.SetStored(cell % width, cell / width, item, (byte)(96 + FarmRng.Next(64)));
        }

        /// <summary>Lugar para o que a ordenha solta: célula da fazenda livre e caminhável (o item precisa ser alcançado), fora das baias.</summary>
        bool IsFarmSpot(int x, int y) =>
            _grid.IsFarmRoom(x, y) && !_grid.IsSolid(x, y) && _grid.GetStored(x, y) == StoredItems.None && !_grid.HasNutrient(x, y) &&
            !_grid.HasWater(x, y) && !_stallCells.Contains(_grid.ToIndex(x, y)) && _walkability.IsWalkable(x, y);

        /// <summary>Até <paramref name="count"/> células livres da fazenda, das mais perto às mais longe, andando pela fazenda.</summary>
        void FindFarmSpots(int x, int y, int count, List<int> result)
        {
            result.Clear();
            int w = _grid.Width;
            if (!_grid.InBounds(x, y))
                return;
            int start = y * w + x;
            int stamp = ++_floodCounter;
            int head = 0, tail = 0;
            _floodStamp[start] = stamp;
            _floodQueue[tail++] = start;
            while (head < tail && result.Count < count)
            {
                int i = _floodQueue[head++];
                int cx = i % w, cy = i / w;
                if (IsFarmSpot(cx, cy))
                    result.Add(i);
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + AntNavigation.Dx[k], ny = cy + AntNavigation.Dy[k];
                    // Só pela fazenda vazia: o item fica dentro dela, onde a fazendeira o alcança.
                    if (!_grid.InBounds(nx, ny) || !_grid.IsFarmRoom(nx, ny) || _grid.IsSolid(nx, ny))
                        continue;
                    int n = ny * w + nx;
                    if (_floodStamp[n] == stamp || tail >= FarmSpotSearchLimit)
                        continue;
                    _floodStamp[n] = stamp;
                    _floodQueue[tail++] = n;
                }
            }
        }

        // ---------------------------------------------------------------- fazendeira

        /// <summary>
        /// Trabalho da fazendeira, na ordem: encher os cochos vazios (areia do depósito de areia ou solta, água do depósito de
        /// água ou solta) e ordenhar um pulgão sem fazendeira. Sem nada disso, faz o trabalho da comum (segunda função).
        /// </summary>
        bool TryFarmWork(Ant ant) => TryStartTroughFill(ant) || TryStartAphidCare(ant);

        /// <summary>A fazenda tem trabalho para a fazendeira emprestada como comum (ela volta ao papel).</summary>
        bool FarmWorkWaiting => !_farmFieldsIdle && _aphids.Count > 0 &&
                                (TroughFillOpen(true) || TroughFillOpen(false) || UntendedAphids() > 0);

        bool TroughFillOpen(bool sand) =>
            sand ? _troughsWantSand - _sandFeeders > 0 && _farmSandItems - _sandFeeders > 0
                 : _troughsWantWater - _waterFeeders > 0 && _farmWaterItems - _waterFeeders > 0;

        int UntendedAphids()
        {
            int n = 0;
            foreach (Aphid aphid in _aphids)
                n += aphid.Stall >= 0 && aphid.Farmer == null ? 1 : 0;
            return n;
        }

        bool TryStartTroughFill(Ant ant)
        {
            if (_farmFieldsIdle || Recalled || _farmField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            return TryStartTroughFill(ant, true) || TryStartTroughFill(ant, false);
        }

        bool TryStartTroughFill(Ant ant, bool sand)
        {
            if (!TroughFillOpen(sand))
                return false;
            FlowField items = sand ? _farmSandField : _farmWaterField;
            if (items.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            int trough = NearestEmptyTrough(ant.X, ant.Y, sand);
            if (trough < 0)
                return false;
            ReleaseDumpSide(ant);
            ant.TargetX = trough % _grid.Width;
            ant.TargetY = trough / _grid.Width;
            ant.FarmItem = sand ? StoredItems.Sand : StoredItems.Water;
            ant.State = AntState.TakingStored;
            ant.StoreUse = sand ? StoreUse.FarmSand : StoreUse.FarmWater;
            ant.StoreTarget = StorageKind.None;
            // Conta já neste passo, para as formigas que decidem depois não irem ao mesmo cocho.
            CountFeeder(ant, sand);
            UpdateTaking(ant);
            return true;
        }

        /// <summary>Cocho vazio (de areia ou de água) de uma baia com pulgão, sem dona, mais perto da formiga; -1 = nenhum.</summary>
        int NearestEmptyTrough(int x, int y, bool sand)
        {
            int w = _grid.Width;
            int best = -1, bestDistance = int.MaxValue;
            foreach (Aphid aphid in _aphids)
            {
                if (aphid.Stall < 0)
                    continue;
                int trough = sand ? aphid.Stall : WaterTrough(aphid.Stall);
                int tx = trough % w, ty = trough / w;
                if (_grid.GetStored(tx, ty) != StoredItems.None || _troughClaims.Contains(trough))
                    continue;
                int d = Mathf.Max(Mathf.Abs(tx - x), Mathf.Abs(ty - y));
                if (d < bestDistance)
                {
                    best = trough;
                    bestDistance = d;
                }
            }
            return best;
        }

        /// <summary>Pegou o item para o cocho (areia na mão, água na boca) ou o cogumelo para a marca.</summary>
        void PickFarmItem(Ant ant, byte item, byte shade, int from)
        {
            if (item == StoredItems.Mushroom)
            {
                ant.PickUp(CellMaterial.GlowMushroom, shade, from);
                ant.ReturningSoil = false;
                ant.State = AntState.Hauling;
                return;
            }
            ant.FarmItem = item;
            if (item == StoredItems.Sand)
                ant.PickUp(CellMaterial.Sand, shade, from);
            ant.State = AntState.FeedingAphid;
        }

        /// <summary>Leva o item ao cocho reservado e o põe lá; o cocho que encheu por outro caminho troca por outro vazio.</summary>
        void UpdateFeedingAphid(Ant ant)
        {
            bool sand = ant.FarmItem == StoredItems.Sand;
            int trough = _grid.ToIndex(ant.TargetX, ant.TargetY);
            if (!IsEmptyTrough(trough, sand))
            {
                _troughClaims.Remove(trough);
                trough = NearestEmptyTrough(ant.X, ant.Y, sand);
                if (trough < 0)
                {
                    PutBackFarmItem(ant);
                    return;
                }
                ant.TargetX = trough % _grid.Width;
                ant.TargetY = trough / _grid.Width;
                _troughClaims.Add(trough);
            }
            var cell = new Vector2Int(ant.TargetX, ant.TargetY);
            if (NearCell(ant, cell))
            {
                byte shade = sand ? ant.CarriedShade : (byte)128;
                if (_grid.SetStored(cell.x, cell.y, ant.FarmItem, shade))
                {
                    Emit(SfxId.Drop, cell.x, cell.y);
                    if (cell.x != ant.X)
                        ant.FacingX = cell.x > ant.X ? 1 : -1;
                    if (sand)
                        ant.DropTop();
                    ant.FarmItem = 0;
                    ant.State = AntState.Idle;
                    ant.Cooldown = _settings.DropSeconds;
                }
                else
                {
                    Hold(ant);
                }
                return;
            }
            if (_farmFieldsIdle || _farmField.Get(ant.X, ant.Y) == FlowField.Unreachable)
            {
                PutBackFarmItem(ant);
                return;
            }
            WalkToCell(ant, cell, _farmField, _grid.IsFarmRoom(ant.X, ant.Y));
        }

        /// <summary>O cocho (de areia ou de água) é de uma baia com pulgão e está vazio.</summary>
        bool IsEmptyTrough(int trough, bool sand)
        {
            int stall = sand ? trough : trough - WaterTroughOffset;
            return _stallSet.Contains(stall) && StallTaken(stall) &&
                   _grid.GetStored(trough % _grid.Width, trough / _grid.Width) == StoredItems.None;
        }

        /// <summary>Nenhum cocho quer o item: a areia segue o caminho de sempre (depósito de areia ou despejo); a água fica no chão perto dela.</summary>
        void PutBackFarmItem(Ant ant)
        {
            byte item = ant.FarmItem;
            ant.FarmItem = 0;
            if (item == StoredItems.Sand && ant.CarriedCount > 0)
            {
                StartDelivery(ant);
                return;
            }
            ant.State = AntState.Idle;
            DropItemNear(ant.X, ant.Y, item, 128);
            Hold(ant);
        }

        /// <summary>Põe o item como item solto na célula livre mais perto (a formiga não fica com ele). <c>false</c> = sem lugar: o item se perde.</summary>
        bool DropItemNear(int x, int y, byte item, byte shade)
        {
            int w = _grid.Width;
            if (!_grid.InBounds(x, y))
                return false;
            int start = y * w + x;
            int stamp = ++_floodCounter;
            int head = 0, tail = 0;
            _floodStamp[start] = stamp;
            _floodQueue[tail++] = start;
            while (head < tail)
            {
                int i = _floodQueue[head++];
                int cx = i % w, cy = i / w;
                if (!_grid.IsSolid(cx, cy) && !_stallCells.Contains(i) && !_grid.HasWater(cx, cy) && _grid.SetStored(cx, cy, item, shade))
                    return true;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + AntNavigation.Dx[k], ny = cy + AntNavigation.Dy[k];
                    if (!_walkability.IsWalkable(nx, ny))
                        continue;
                    int n = ny * w + nx;
                    if (_floodStamp[n] == stamp || tail >= FarmSpotSearchLimit)
                        continue;
                    _floodStamp[n] = stamp;
                    _floodQueue[tail++] = n;
                }
            }
            return false;
        }

        /// <summary>Vai ordenhar um pulgão sem fazendeira (o mais perto; primeiro os de cocho cheio).</summary>
        bool TryStartAphidCare(Ant ant)
        {
            if (_farmFieldsIdle || Recalled || _farmField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            Aphid target = PickAphid(ant);
            if (target == null)
                return false;
            ReleaseDumpSide(ant);
            ant.State = AntState.TendingAphid;
            ant.EggId = target.Id;
            ant.Interacting = false;
            target.Farmer = ant;
            UpdateTendingAphid(ant);
            return true;
        }

        Aphid PickAphid(Ant ant)
        {
            Aphid best = null;
            int bestScore = int.MaxValue;
            foreach (Aphid aphid in _aphids)
            {
                if (aphid.Stall < 0 || aphid.Farmer != null && aphid.Farmer != ant)
                    continue;
                bool fed = CanMilk(aphid);
                int score = Mathf.Max(Mathf.Abs(aphid.X - ant.X), Mathf.Abs(aphid.Y - ant.Y)) + (fed ? 0 : 100000);
                if (score < bestScore)
                {
                    best = aphid;
                    bestScore = score;
                }
            }
            return best;
        }

        void LeaveAphid(Ant ant)
        {
            ant.State = AntState.Idle;
            ant.EggId = 0;
            ant.Interacting = false;
            Hold(ant);
        }

        /// <summary>
        /// Vai até o pulgão e fica ao lado (<see cref="Ant.Interacting"/>): enquanto ela está ali e os cochos estão cheios, a
        /// ordenha corre. Sai se o pulgão some, se o sino toca ou se há cocho vazio que ninguém está enchendo (encher vem antes).
        /// </summary>
        void UpdateTendingAphid(Ant ant)
        {
            Aphid aphid = AphidById(ant.EggId);
            if (aphid == null && ant.EggId == 0)
            {
                // Outra fazendeira ficou com o pulgão: escolhe outro.
                aphid = PickAphid(ant);
                if (aphid != null)
                {
                    ant.EggId = aphid.Id;
                    aphid.Farmer = ant;
                }
            }
            if (aphid == null || aphid.Stall < 0 || Recalled || _farmFieldsIdle ||
                ant.Role == AntRole.Farmer && (TroughFillOpen(true) || TroughFillOpen(false)) && !CanMilk(aphid))
            {
                LeaveAphid(ant);
                return;
            }
            Vector2Int cell = AphidCell(aphid.Stall);
            ant.TargetX = cell.x;
            ant.TargetY = cell.y;
            if (Mathf.Abs(ant.X - aphid.X) > 2 || Mathf.Abs(ant.Y - aphid.Y) > 1)
            {
                ant.Interacting = false;
                WalkToCell(ant, new Vector2Int(aphid.X, aphid.Y), _farmField, _grid.IsFarmRoom(ant.X, ant.Y));
                return;
            }
            ant.Interacting = true;
            if (aphid.X != ant.X)
                ant.FacingX = aphid.X > ant.X ? 1 : -1;
            ant.Cooldown = _settings.CarePauseSeconds;
        }

        /// <summary>Já comeu, ou vai comer agora (os dois cochos cheios): a ordenha vai correr.</summary>
        bool CanMilk(Aphid aphid) => aphid.Fed || TroughsFull(aphid);

        /// <summary>Cocho de areia com areia e cocho de água com água.</summary>
        bool TroughsFull(Aphid aphid)
        {
            int w = _grid.Width;
            int water = WaterTrough(aphid.Stall);
            return _grid.GetStored(aphid.Stall % w, aphid.Stall / w) == StoredItems.Sand &&
                   _grid.GetStored(water % w, water / w) == StoredItems.Water;
        }

        // ---------------------------------------------------------------- cogumelo luminoso

        // Valor de MushroomFromSoil na última varredura da obra: quando muda, os campos da obra são refeitos.
        bool _mushroomFromSoil;
        readonly List<int> _expiredMushrooms = new List<int>();

        /// <summary>
        /// A colônia ainda não fabrica cogumelo luminoso (nenhum pulgão e nenhum cogumelo guardado ou solto): a marca de
        /// cogumelo usa terra comum, e o cogumelo de terra some depois de <see cref="PixelGrid.FleetingGlowSeconds"/>. Com a
        /// fabricação dos pulgões a marca espera o cogumelo deles, que é permanente.
        /// </summary>
        bool MushroomFromSoil => _aphids.Count == 0 && _grid.StoredCount(StorageKind.Mushroom) == 0;

        /// <summary>A marca de cogumelo da célula espera um cogumelo dos pulgões (não terra): é o caso quando a colônia já os fabrica.</summary>
        bool NeedsMushroomItem(int x, int y) => !_mushroomFromSoil && _grid.IsGlowMushroomMarked(x, y);

        /// <summary>Esvazia os cogumelos de terra vencidos e acompanha se a colônia passou a fabricar (ou deixou de fabricar) cogumelo.</summary>
        void TickFleetingMushrooms(float deltaTime)
        {
            bool fromSoil = MushroomFromSoil;
            if (fromSoil != _mushroomFromSoil)
            {
                _mushroomFromSoil = fromSoil;
                _builtMarkVersion = -1;
            }
            _expiredMushrooms.Clear();
            _grid.AdvanceFleetingGlow(deltaTime, _expiredMushrooms);
            foreach (int cell in _expiredMushrooms)
                EmitFx(FxId.Dig, cell % _grid.Width, cell / _grid.Width, CellMaterial.GlowMushroom);
            _expiredMushrooms.Clear();
        }

        /// <summary>
        /// Marca de cogumelo esperando cogumelo que ninguém foi buscar, e cogumelo guardado no depósito de cogumelos (ou solto) ao alcance: vai pegá-lo
        /// (seção Construção). O cogumelo vem dos pulgões e é permanente; sem a fabricação deles, a marca usa terra
        /// (<see cref="MushroomFromSoil"/>) e este caminho não é usado.
        /// </summary>
        bool TryStartMushroomWork(Ant ant)
        {
            if (_mushroomSites - _mushroomHaulers - _mushroomTakers <= 0 || _mushroomItems - _mushroomTakers <= 0 ||
                _mushroomFieldIdle || _mushroomSiteIdle || !SectionAllows(WorkSection.Build, ant) ||
                _mushroomField.Get(ant.X, ant.Y) == FlowField.Unreachable || _mushroomSiteField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ReleaseDumpSide(ant);
            ant.State = AntState.TakingStored;
            ant.StoreUse = StoreUse.Mushroom;
            ant.StoreTarget = StorageKind.None;
            _mushroomTakers++;
            UpdateTaking(ant);
            return true;
        }

        /// <summary>Lugares de onde encaixar um cogumelo (vizinhos das marcas de cogumelo preenchíveis), montados com a obra.</summary>
        void UpdateMushroomSiteField(int sites)
        {
            _mushroomSites = sites;
            if (_mushroomSiteSources.Count == 0)
            {
                if (!_mushroomSiteIdle)
                {
                    _mushroomSiteField.Invalidate();
                    _mushroomSiteIdle = true;
                }
                return;
            }
            _mushroomSiteIdle = false;
            _mushroomSiteField ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
            UpdateField(_mushroomSiteField, _mushroomSiteSources);
        }

        /// <summary>Com o cogumelo na mão: desce até uma marca de cogumelo e o encaixa; sem marca, devolve o cogumelo.</summary>
        void UpdateHaulingMushroom(Ant ant)
        {
            int d = _mushroomSiteIdle ? FlowField.Unreachable : _mushroomSiteField.Get(ant.X, ant.Y);
            if (d == 0)
            {
                if (!TryStartPlace(ant))
                    Hold(ant);
                return;
            }
            if (d != FlowField.Unreachable && StepDownhill(ant, _mushroomSiteField))
                return;
            ReturnHeldItem(ant);
        }

        /// <summary>Cogumelo sem marca para ele, néctar ou minério: vai ao depósito do tipo (com vaga) ou fica no chão, como item solto.</summary>
        void ReturnHeldItem(Ant ant)
        {
            ant.State = AntState.Carrying;
            ant.StoreTarget = StorageKind.None;
            // O néctar do Canteiro de flores segue a mesma regra do cogumelo (REQ-070): depósito dele, senão o chão.
            byte item = StoredItems.Of(ant.Carried);
            if (TryStartStock(ant, StoredItems.KindOf(item)))
                return;
            if (DropItemNear(ant.X, ant.Y, item, ant.CarriedShade))
            {
                ant.DropTop();
                ant.State = ant.CarriedCount > 0 ? AntState.Carrying : AntState.Idle;
                return;
            }
            Hold(ant);
        }

        // ---------------------------------------------------------------- cupins

        /// <summary>Pulgão saudável a até <paramref name="reach"/> células (xadrez) da célula, o mais perto; null = nenhum.</summary>
        Aphid AphidInReach(int x, int y, int reach)
        {
            Aphid best = null;
            int bestDistance = int.MaxValue;
            foreach (Aphid aphid in _aphids)
            {
                int d = Mathf.Max(Mathf.Abs(aphid.X - x), Mathf.Abs(aphid.Y - y));
                if (d <= reach && d < bestDistance)
                {
                    best = aphid;
                    bestDistance = d;
                }
            }
            return best;
        }

        /// <summary>O cupim mordeu o pulgão: ele fica preto, muda de lado e passa a atacar as formigas (um cupim a mais no combate).</summary>
        void CorruptAphid(Aphid aphid)
        {
            _aphids.Remove(aphid);
            _farmDirty = true;
            Termite t = NewTermite(aphid.X, aphid.Y);
            t.Aphid = true;
            t.Health = _settings.BlackAphidHealth;
            t.Phase = TermitePhase.Raiding;
            t.Body.SpeedOverride = _settings.BlackAphidSpeed;
            t.Body.FacingX = aphid.Body.FacingX;
            t.LastHitTime = SimulationTime;
            _termites.Add(t);
            AphidsCorrupted++;
            _raidDirty = true;
            Emit(SfxId.TermiteHit, aphid.X, aphid.Y);
            EmitFx(FxId.TermiteHit, aphid.X, aphid.Y);
        }

        /// <summary>
        /// Um passo do pulgão preto: morde a formiga (ou a rainha) ao alcance; senão vai atrás da formiga mais perto que ele
        /// enxerga, ou vaga. Não segue o objetivo do ataque nem vai embora: fica até morrer.
        /// </summary>
        void UpdateBlackAphid(Termite t, float deltaTime)
        {
            Ant body = t.Body;
            int x = body.X, y = body.Y;
            if (UpdateSupport(body, deltaTime))
            {
                _raidDirty |= body.X != x || body.Y != y;
                return;
            }
            t.AttackTimer -= deltaTime;
            body.Cooldown -= deltaTime;
            Ant victim = AntInReach(body.X, body.Y);
            if (victim == null && Mathf.Max(Mathf.Abs(Queen.X - body.X), Mathf.Abs(Queen.Y - body.Y)) <= RaidSettings.AttackReach)
                victim = Queen;
            if (victim != null)
            {
                body.State = AntState.Digging;
                body.DigX = victim.X;
                body.DigY = victim.Y;
                if (victim.X != body.X)
                    body.FacingX = victim.X > body.X ? 1 : -1;
                if (t.AttackTimer <= 0f)
                {
                    t.AttackTimer = RaidSettings.AttackSeconds;
                    victim.Wounds += _settings.BlackAphidDamage * ArmorFactor(victim);
                    victim.LastHitTime = SimulationTime;
                    Emit(SfxId.AntHurt, victim.X, victim.Y);
                    EmitFx(FxId.AntHurt, victim.X, victim.Y);
                }
                return;
            }
            if (body.Cooldown > 0f)
                return;
            body.State = AntState.Idle;
            Ant prey = NearestAntInSight(body.X, body.Y);
            if (prey == null || !StepTowardsCell(body, prey.X, prey.Y, keepQueenRoom: false))
                Wander(body);
            _raidDirty = true;
        }

        Ant NearestAntInSight(int x, int y)
        {
            Ant best = null;
            int bestDistance = BlackAphidSight + 1;
            foreach (Ant ant in _ants)
            {
                if (ant.Buried)
                    continue;
                int d = Mathf.Max(Mathf.Abs(ant.X - x), Mathf.Abs(ant.Y - y));
                if (d < bestDistance)
                {
                    best = ant;
                    bestDistance = d;
                }
            }
            return best;
        }

        /// <summary>Origens do campo dos cupins no objetivo dos pulgões: em volta de cada pulgão saudável.</summary>
        bool CollectAphidSources()
        {
            bool any = false;
            foreach (Aphid aphid in _aphids)
            {
                any = true;
                _objectiveCells.Add(_grid.ToIndex(aphid.X, aphid.Y));
                AddRaidSource(aphid.X, aphid.Y);
                AddRaidStand(aphid.X, aphid.Y);
            }
            return any;
        }

        void CountAphidKilled() => AphidsKilled++;

        // ---------------------------------------------------------------- save

        /// <summary>Repõe os pulgões de um save, com a fome (as baias e as fazendeiras voltam na primeira rodada; os relógios recomeçam).</summary>
        public void RestoreFarm(IReadOnlyList<Vector2Int> aphids, float breedTimer, IReadOnlyList<bool> fed = null)
        {
            _aphids.Clear();
            _breedTimer = Mathf.Max(0f, breedTimer);
            if (aphids != null)
            {
                for (int i = 0; i < aphids.Count; i++)
                    NewAphid(aphids[i].x, aphids[i].y).Fed = fed != null && i < fed.Count && fed[i];
            }
            _builtStallMarkVersion = -1;
        }

        /// <summary>Segundos já contados para o próximo pulgão (vai para o save).</summary>
        public float AphidBreedTimer => _breedTimer;

        /// <summary>Pulgão novo na célula (testes e cheats); null fora da grade.</summary>
        public Aphid SpawnAphid(int x, int y) => _grid.InBounds(x, y) ? NewAphid(x, y) : null;

        /// <summary>O cupim morde o pulgão saudável ao alcance (teste da troca de lado sem montar um ataque inteiro).</summary>
        public void CorruptAphidForTest(Aphid aphid)
        {
            if (aphid != null && _aphids.Contains(aphid))
            {
                if (RaidState != RaidState.Active)
                    BeginRaid();
                CorruptAphid(aphid);
            }
        }
    }
}
