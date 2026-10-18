using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Cultivo da sala de nutrientes (pedido do desenvolvedor em 2026-10-18): a formiga monta um canteiro com terra fofa
    /// (o <see cref="CellMaterial.Spoil"/> que ela tirou ao cavar), outra põe o pedaço de planta nele e outra o rega com um
    /// bloco de água tirado do mapa. Regado, o canteiro brota no tempo de <see cref="ColonySettings.NutrientCureSeconds"/>
    /// (com formiga cuidando da sala) e põe um nutriente pronto em cada lado livre; a terra fica para o próximo ciclo.
    /// </summary>
    public sealed partial class AntColony
    {
        // Campos do cultivo: até os pontos onde cabe um canteiro novo, até os canteiros com planta esperando água e até
        // as células ao lado de água. Os canteiros com terra esperando planta usam _storeField; os que brotam, _curingField.
        readonly FlowField _bedSpotField;
        readonly FlowField _waterBedField;
        readonly FlowField _waterField;
        // Até a terra fofa solta de fora (a pilha do despejo), para quando não há terra cavada a caminho dos canteiros.
        readonly FlowField _soilPileField;
        int _builtSoilPileVersion = -1;
        readonly List<int> _bedSpotCells = new List<int>();
        readonly List<int> _waterBedSources = new List<int>();
        readonly List<int> _waterSources = new List<int>();
        readonly HashSet<int> _waterSeen = new HashSet<int>();
        int _bedsAwaitingPlant;
        int _bedsAwaitingWater;
        int _soilCarriers;
        int _waterCarriers;
        int _builtWaterVersion = -1;

        /// <summary>Canteiros só com terra, com lado livre e alcançáveis: cada um recebe um pedaço de planta.</summary>
        public int BedsAwaitingPlant => _bedsAwaitingPlant;

        /// <summary>Canteiros com planta, alcançáveis, esperando um bloco de água.</summary>
        public int BedsAwaitingWater => _bedsAwaitingWater;

        /// <summary>Células da sala de nutrientes onde ainda cabe um canteiro novo (vizinhas entre si contam todas).</summary>
        public int BedSpots => _bedSpotCells.Count;

        /// <summary>Nutrientes que já brotaram dos canteiros desde o começo do mapa (não vai para o save).</summary>
        public int GrownNutrients { get; private set; }

        /// <summary>Pixels de terra fofa que as formigas tiraram da pilha de fora para os canteiros (não vai para o save).</summary>
        public int SoilFromPile { get; private set; }

        /// <summary>Blocos de água que as formigas tiraram do mapa para regar (não vai para o save).</summary>
        public int WaterCollected { get; private set; }

        /// <summary>Distância mínima (Chebyshev) entre dois canteiros: assim os lados de um nunca são os de outro.</summary>
        const int BedSpacing = 3;

        /// <summary>Formigas levando terra ou água, recontadas a cada <see cref="Tick"/>.</summary>
        void CountCultivation(Ant ant)
        {
            if (ant.State == AntState.BringingSoil || ant.State == AntState.FetchingSoil || ant.State == AntState.GatheringSoil)
                _soilCarriers++;
            else if (ant.State == AntState.FetchingWater || ant.State == AntState.CarryingWater)
                _waterCarriers++;
        }

        void AdvanceBeds(float deltaTime) => GrownNutrients += _grid.AdvanceBedGrowth(deltaTime, GardenCureRate());

        // ---------------------------------------------------------------- campos

        /// <summary>
        /// Campos do cultivo: nutrientes prontos (busca para a rainha), canteiros por etapa (planta, água, cuidado) e pontos
        /// livres para canteiros novos. Conta os canteiros que esperam planta ou água e vê se algum nutriente chega à rainha.
        /// </summary>
        void BuildNutrientFields()
        {
            _sources.Clear();
            _buildSources.Clear();
            _curingSources.Clear();
            _waterBedSources.Clear();
            _bedSpotCells.Clear();
            _bedsAwaitingPlant = 0;
            _bedsAwaitingWater = 0;
            int w = _grid.Width;
            if (_grid.NutrientRoomCount > 0 || _grid.NutrientCount > 0)
            {
                RectInt area = _grid.Active;
                int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
                int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
                for (int cy = cyMin; cy <= cyMax; cy++)
                for (int cx = cxMin; cx <= cxMax; cx++)
                {
                    if (_grid.GetChunkNutrientRoomCount(cx, cy) == 0 && _grid.GetChunkNutrientCount(cx, cy) == 0)
                        continue;
                    int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                    int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                    int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                    int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                    for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        int i = y * w + x;
                        if (_grid.HasNutrient(x, y))
                        {
                            if (_walkability.IsWalkableAt(i))
                                _buildSources.Add(i);
                        }
                        else if (IsBedSpot(x, y))
                        {
                            _bedSpotCells.Add(i);
                        }
                    }
                }
            }
            // De baixo para cima e da esquerda para a direita: quem chega escolhe sempre na mesma ordem.
            _bedSpotCells.Sort();

            for (int k = 0; k < _grid.BedCount; k++)
            {
                if (!_grid.TryGetBed(k, out int bx, out int by))
                    break;
                switch (_grid.GetBedStage(bx, by))
                {
                    case BedStage.Soil:
                        // Sem lado livre o canteiro não teria onde pôr o nutriente: espera alguém levar os que já brotaram.
                        if (_grid.FreeBedSides(bx, by) > 0 && AddStandCells(bx, by, _sources))
                            _bedsAwaitingPlant++;
                        break;
                    case BedStage.Planted:
                        if (AddStandCells(bx, by, _waterBedSources))
                            _bedsAwaitingWater++;
                        break;
                    case BedStage.Growing:
                        AddStandCells(bx, by, _curingSources);
                        break;
                }
            }

            UpdateField(_storeField, _sources);
            UpdateField(_nutrientField, _buildSources);
            UpdateField(_curingField, _curingSources);
            UpdateField(_waterBedField, _waterBedSources);
            UpdateField(_bedSpotField, _bedSpotCells);
            BuildSoilPileField();
            BuildWaterField();

            _nutrientsReachQueen = false;
            foreach (int i in _buildSources)
                _nutrientsReachQueen |= _homeField.Get(i % w, i / w) != FlowField.Unreachable;
            // _buildSources é da construção: devolve vazio (BuildConstructionFields o refaz quando precisa).
            _buildSources.Clear();
        }

        /// <summary>Células caminháveis encostadas no canteiro (de onde a formiga o alcança); falso se não há nenhuma.</summary>
        bool AddStandCells(int bx, int by, List<int> into)
        {
            bool any = false;
            int w = _grid.Width;
            for (int k = 0; k < 8; k++)
            {
                int nx = bx + AntNavigation.Dx[k], ny = by + AntNavigation.Dy[k];
                if (!_grid.InBounds(nx, ny) || !_walkability.IsWalkableAt(ny * w + nx))
                    continue;
                into.Add(ny * w + nx);
                any = true;
            }
            return any;
        }

        /// <summary>
        /// Campo até a água do mapa (laguinhos, poças da chuva, água nos túneis): as células caminháveis e secas ao lado
        /// dela. Só é montado com canteiro esperando água; a água que escorre o refaz a cada rodada.
        /// </summary>
        void BuildWaterField()
        {
            _builtWaterVersion = _grid.WaterVersion;
            _waterSources.Clear();
            _waterSeen.Clear();
            if (_bedsAwaitingWater == 0 || _grid.WaterCount == 0)
            {
                UpdateField(_waterField, _waterSources);
                return;
            }
            int w = _grid.Width;
            RectInt area = _grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            for (int cy = cyMin; cy <= cyMax; cy++)
            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                if (_grid.GetChunkWaterCount(cx, cy) == 0)
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    if (!_grid.HasWater(x, y))
                        continue;
                    for (int k = 0; k < 8; k++)
                    {
                        int nx = x + AntNavigation.Dx[k], ny = y + AntNavigation.Dy[k];
                        if (!_grid.InBounds(nx, ny) || _grid.HasWater(nx, ny) || !_walkability.IsWalkableAt(ny * w + nx))
                            continue;
                        if (_waterSeen.Add(ny * w + nx))
                            _waterSources.Add(ny * w + nx);
                    }
                }
            }
            UpdateField(_waterField, _waterSources);
        }

        /// <summary>
        /// Campo até a terra fofa solta de fora: células caminháveis encostadas em terra despejada ou lama acima do solo
        /// original (a pilha de fora, a mesma da obra, sem a areia). Só é montado com ponto para canteiro; muda com a estrutura.
        /// </summary>
        void BuildSoilPileField()
        {
            if (_bedSpotCells.Count == 0)
            {
                // Parado, o campo deixa de acompanhar as mudanças de caminhabilidade: refaz do zero quando voltar.
                _soilPileField.Invalidate();
                _builtSoilPileVersion = -1;
                return;
            }
            if (_builtSoilPileVersion == _grid.StructureVersion)
                return;
            _builtSoilPileVersion = _grid.StructureVersion;
            _waterSources.Clear();
            RectInt area = _grid.Active;
            int w = _grid.Width;
            for (int x = area.xMin; x < area.xMax; x++)
            {
                for (int y = Mathf.Max(area.yMin, _grid.GetSurfaceY(x) + 1); y < area.yMax && IsPileCell(x, y); y++)
                {
                    if (!IsSoilPileCell(x, y))
                        continue;
                    for (int k = 0; k < 8; k++)
                    {
                        int nx = x + AntNavigation.Dx[k], ny = y + AntNavigation.Dy[k];
                        if (_walkability.IsWalkable(nx, ny))
                            _waterSources.Add(ny * w + nx);
                    }
                }
            }
            UpdateField(_soilPileField, _waterSources);
            _waterSources.Clear();
        }

        /// <summary>Terra fofa solta de fora que dá para levar a um canteiro: terra despejada ou lama (a areia não vira canteiro).</summary>
        bool IsSoilPileCell(int x, int y)
        {
            if (!IsOutdoor(x, y))
                return false;
            CellMaterial material = _grid.Get(x, y);
            return material == CellMaterial.Spoil || material == CellMaterial.Mud;
        }

        /// <summary>A água mudou e há canteiro esperando por ela: o campo da água precisa de outra rodada.</summary>
        bool WaterFieldStale => _bedsAwaitingWater > 0 && _grid.WaterVersion != _builtWaterVersion;

        /// <summary>
        /// Cabe um canteiro novo na célula: na sala de nutrientes, livre (sem material, nutriente nem água), caminhável e
        /// sem canteiro a menos de <see cref="BedSpacing"/> células. Pode ser no chão, na parede ou no meio da sala (a formiga
        /// anda pelo fundo do formigueiro e põe o bloco como na obra): montado, o canteiro fica preso ali, porque a física
        /// granular não o move. Para não fechar a passagem, as células livres em volta precisam continuar ligadas entre si
        /// (<see cref="KeepsPassage"/>).
        /// </summary>
        bool IsBedSpot(int x, int y)
        {
            if (!_grid.IsNutrientRoom(x, y) || _grid.IsSolid(x, y) || _grid.HasNutrient(x, y) || _grid.HasWater(x, y) ||
                !_walkability.IsWalkable(x, y))
                return false;
            for (int dy = -(BedSpacing - 1); dy < BedSpacing; dy++)
            for (int dx = -(BedSpacing - 1); dx < BedSpacing; dx++)
            {
                if (_grid.GetBedStage(x + dx, y + dy) != BedStage.None)
                    return false;
            }
            return KeepsPassage(x, y);
        }

        // As 8 vizinhas em volta da célula, em sequência (cada uma encosta na seguinte).
        static readonly int[] RingDx = { -1, 0, 1, 1, 1, 0, -1, -1 };
        static readonly int[] RingDy = { -1, -1, -1, 0, 1, 1, 1, 0 };

        /// <summary>
        /// Encher a célula não separa caminhos: as vizinhas livres formam um só trecho seguido na volta dela (no chão de
        /// um túnel de 1 de altura, ou no meio de um poço de 1 de largura, ficariam dois trechos e a passagem fecharia).
        /// </summary>
        bool KeepsPassage(int x, int y)
        {
            int runs = 0, free = 0;
            for (int k = 0; k < 8; k++)
            {
                bool open = !_grid.IsSolid(x + RingDx[k], y + RingDy[k]);
                int prev = (k + 7) % 8;
                bool prevOpen = !_grid.IsSolid(x + RingDx[prev], y + RingDy[prev]);
                if (open)
                {
                    free++;
                    if (!prevOpen)
                        runs++;
                }
            }
            return free > 0 && runs <= 1;
        }

        /// <summary>
        /// Quantas formigas levando terra a sala chama: um canteiro tira os pontos da volta dele (até 5x5), então uma a cada
        /// <see cref="BedSpacing"/>² pontos, pelo menos uma.
        /// </summary>
        int WantedSoilCarriers => Mathf.Max(1, (_bedSpotCells.Count + BedSpacing * BedSpacing - 1) / (BedSpacing * BedSpacing));

        // ---------------------------------------------------------------- terra fofa

        /// <summary>
        /// Ao sair para entregar terra: se a sala de nutrientes tem ponto para canteiro novo, alcançável, e ainda falta
        /// quem leve terra, a formiga leva a terra para lá em vez do despejo. Só a terra que vira terra fofa serve (areia não).
        /// </summary>
        bool TryStartBedSoil(Ant ant)
        {
            // Restos de cupim não viram canteiro (sem área de resíduos, vão ao despejo).
            if (ant.Carried == CellMaterial.Refuse || CellMaterials.ToDumped(ant.Carried) != CellMaterial.Spoil || _bedSpotCells.Count == 0)
                return false;
            if (_soilCarriers >= WantedSoilCarriers || _bedSpotField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ReleaseDumpSide(ant);
            ant.State = AntState.BringingSoil;
            _soilCarriers++;
            return true;
        }

        /// <summary>
        /// Levando terra para a sala de nutrientes: desce o campo dos pontos livres e, ao lado de um que ainda vale (e sem
        /// formiga em cima), larga a terra ali; ela vira canteiro. Sem ponto alcançável, leva a terra ao despejo.
        /// </summary>
        void UpdateBringingSoil(Ant ant)
        {
            int d = _bedSpotField.Get(ant.X, ant.Y);
            if (d == FlowField.Unreachable)
            {
                SendSoilToDump(ant);
                return;
            }
            if (d <= 1 && TryPlaceBed(ant))
                return;
            if (d > 0 && StepDownhill(ant, _bedSpotField))
                return;
            if (d == 0)
            {
                // Em cima do ponto (a terra não cai na própria formiga): sai para o lado e larga dali.
                Wander(ant);
                return;
            }
            SendSoilToDump(ant);
        }

        bool TryPlaceBed(Ant ant)
        {
            for (int k = 0; k < 8; k++)
            {
                int x = ant.X + AntNavigation.Dx[k], y = ant.Y + AntNavigation.Dy[k];
                if (!IsBedSpot(x, y) || AntAt(x, y))
                    continue;
                _grid.Set(x, y, CellMaterial.Spoil, ant.CarriedShade);
                _grid.SetBed(x, y, BedStage.Soil);
                ant.DropTop();
                ant.Cooldown = _settings.DropSeconds;
                if (x != ant.X)
                    ant.FacingX = x > ant.X ? 1 : -1;
                // Sobrou terra na mão: entrega o resto (outro canteiro ou o despejo).
                if (ant.CarriedCount > 0)
                    StartDelivery(ant);
                else
                    ant.State = AntState.Idle;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Ociosa e de mão vazia (comum, carregadora, escavadora): se a sala tem ponto para canteiro e ainda falta quem leve
        /// terra, vai buscar terra fofa na pilha de fora, que já existe desde o primeiro despejo.
        /// </summary>
        bool TryStartSoilFetch(Ant ant)
        {
            if (_bedSpotCells.Count == 0 || _builtSoilPileVersion < 0)
                return false;
            if (_soilCarriers >= WantedSoilCarriers || _soilPileField.Get(ant.X, ant.Y) == FlowField.Unreachable ||
                _bedSpotField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ant.State = AntState.FetchingSoil;
            _soilCarriers++;
            UpdateFetchingSoil(ant);
            return true;
        }

        /// <summary>Desce o campo da pilha de fora e, encostada nela, começa a pegar um pixel de terra fofa.</summary>
        void UpdateFetchingSoil(Ant ant)
        {
            int d = _bedSpotCells.Count > 0 && _builtSoilPileVersion >= 0 ? _soilPileField.Get(ant.X, ant.Y) : FlowField.Unreachable;
            if (d == 0)
            {
                int count = 0;
                for (int k = 0; k < 8; k++)
                {
                    // Tirar o pixel de baixo desmonta o próprio apoio: só se não houver outro.
                    if (k != 3 && IsSoilPileCell(ant.X + AntNavigation.Dx[k], ant.Y + AntNavigation.Dy[k]))
                        _candidates[count++] = k;
                }
                if (count == 0 && IsSoilPileCell(ant.X, ant.Y - 1))
                    _candidates[count++] = 3;
                if (count > 0)
                {
                    int pick = _candidates[_rng.Next(count)];
                    ant.DigX = ant.X + AntNavigation.Dx[pick];
                    ant.DigY = ant.Y + AntNavigation.Dy[pick];
                    if (ant.DigX != ant.X)
                        ant.FacingX = ant.DigX > ant.X ? 1 : -1;
                    ant.State = AntState.GatheringSoil;
                    ant.Cooldown = CellMaterials.DigSeconds(_grid.Get(ant.DigX, ant.DigY)) * _settings.DigTimeMultiplier * WorkTimeFactor(ant);
                    return;
                }
                // Outra formiga levou a terra daqui: espera o campo ser refeito.
                ant.State = AntState.Idle;
                Hold(ant);
                return;
            }
            if (d != FlowField.Unreachable && StepDownhill(ant, _soilPileField))
                return;
            ant.State = AntState.Idle;
            Hold(ant);
        }

        /// <summary>Pega o pixel de terra fofa da pilha e o leva a um canteiro (ou ao despejo, se não houver mais ponto).</summary>
        void FinishSoilGather(Ant ant)
        {
            bool adjacent = Mathf.Abs(ant.DigX - ant.X) <= 1 && Mathf.Abs(ant.DigY - ant.Y) <= 1;
            if (!adjacent || !IsSoilPileCell(ant.DigX, ant.DigY))
            {
                ant.State = AntState.Idle;
                return;
            }
            ant.PickUp(_grid.Get(ant.DigX, ant.DigY), _grid.GetShade(ant.DigX, ant.DigY), _grid.ToIndex(ant.DigX, ant.DigY));
            _grid.Set(ant.DigX, ant.DigY, CellMaterial.Empty, 0);
            SoilFromPile++;
            ant.State = AntState.BringingSoil;
        }

        void SendSoilToDump(Ant ant)
        {
            ant.State = AntState.Carrying;
            AssignDumpSide(ant);
        }

        // ---------------------------------------------------------------- planta

        /// <summary>
        /// Carregando um pixel de planta: vai até um canteiro que só tem terra e o planta. Sem canteiro alcançável, espera
        /// com a planta na mão perto de casa até algum voltar a só ter terra.
        /// </summary>
        void UpdateStoring(Ant ant)
        {
            int d = _storeField.Get(ant.X, ant.Y);
            if (d == 0)
            {
                if (!TryStore(ant))
                    Hold(ant); // o canteiro foi plantado por outra formiga: o próximo recálculo manda esta para outro
                return;
            }
            if (d != FlowField.Unreachable && StepDownhill(ant, _storeField))
                return;
            WanderHome(ant);
        }

        bool TryStore(Ant ant)
        {
            for (int k = 0; k < 8; k++)
            {
                int x = ant.X + AntNavigation.Dx[k], y = ant.Y + AntNavigation.Dy[k];
                if (_grid.GetBedStage(x, y) != BedStage.Soil || _grid.FreeBedSides(x, y) == 0)
                    continue;
                _grid.SetBed(x, y, BedStage.Planted);
                _bedsAwaitingPlant = Mathf.Max(0, _bedsAwaitingPlant - 1);
                _plantsInFlight = Mathf.Max(0, _plantsInFlight - 1);
                HarvestedPixels++;
                ant.DropTop();
                if (x != ant.X)
                    ant.FacingX = x > ant.X ? 1 : -1;
                // Ainda tem planta na mão: segue plantando (um pixel por canteiro) antes de voltar ao trabalho.
                ant.State = ant.CarriedCount > 0 ? AntState.Carrying : AntState.Idle;
                ant.Cooldown = _settings.DropSeconds;
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- água

        /// <summary>
        /// Ociosa: se há canteiro com planta esperando água sem ninguém levando, e água e canteiro são alcançáveis, vai
        /// buscar um bloco de água.
        /// </summary>
        bool TryStartWaterFetch(Ant ant)
        {
            if (_bedsAwaitingWater - _waterCarriers <= 0)
                return false;
            if (_waterField.Get(ant.X, ant.Y) == FlowField.Unreachable || _waterBedField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ant.State = AntState.FetchingWater;
            _waterCarriers++;
            UpdateFetchingWater(ant);
            return true;
        }

        /// <summary>Desce o campo da água e, ao lado dela, tira um bloco (a célula de água some do mapa).</summary>
        void UpdateFetchingWater(Ant ant)
        {
            if (_bedsAwaitingWater == 0)
            {
                ant.State = AntState.Idle;
                return;
            }
            int d = _waterField.Get(ant.X, ant.Y);
            if (d == 0)
            {
                for (int k = 0; k < 8; k++)
                {
                    int x = ant.X + AntNavigation.Dx[k], y = ant.Y + AntNavigation.Dy[k];
                    if (!_grid.HasWater(x, y))
                        continue;
                    _grid.SetWater(x, y, false);
                    WaterCollected++;
                    if (x != ant.X)
                        ant.FacingX = x > ant.X ? 1 : -1;
                    ant.State = AntState.CarryingWater;
                    ant.Cooldown = _settings.DropSeconds;
                    return;
                }
                // A água escorreu dali: espera o campo ser refeito.
                ant.State = AntState.Idle;
                Hold(ant);
                return;
            }
            if (d != FlowField.Unreachable && StepDownhill(ant, _waterField))
                return;
            ant.State = AntState.Idle;
            Hold(ant);
        }

        /// <summary>Leva o bloco de água ao canteiro com planta e o rega: ele começa a brotar.</summary>
        void UpdateCarryingWater(Ant ant)
        {
            int d = _waterBedField.Get(ant.X, ant.Y);
            if (d == 0)
            {
                if (!TryWater(ant))
                    Hold(ant); // outra formiga regou este: o próximo recálculo manda esta para outro
                return;
            }
            if (d != FlowField.Unreachable && StepDownhill(ant, _waterBedField))
                return;
            // Nenhum canteiro esperando água agora: espera com a água perto de casa.
            WanderHome(ant);
        }

        bool TryWater(Ant ant)
        {
            for (int k = 0; k < 8; k++)
            {
                int x = ant.X + AntNavigation.Dx[k], y = ant.Y + AntNavigation.Dy[k];
                if (_grid.GetBedStage(x, y) != BedStage.Planted)
                    continue;
                _grid.SetBed(x, y, BedStage.Growing, _settings.NutrientCureSeconds);
                _bedsAwaitingWater = Mathf.Max(0, _bedsAwaitingWater - 1);
                if (x != ant.X)
                    ant.FacingX = x > ant.X ? 1 : -1;
                ant.State = AntState.Idle;
                ant.Cooldown = _settings.DropSeconds;
                return true;
            }
            return false;
        }
    }
}
