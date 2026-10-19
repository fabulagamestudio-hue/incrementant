using Fabula.Incrementant.Audio;
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
        // Canteiros dentro da sala de nutrientes (os de fora, depois de apagada a pintura, não produzem).
        int _activeBeds;
        // Terra fofa que a sala já não quer (canteiro fora da sala, terra do teto que sobrou ao mudar a sala): as formigas a
        // cavam como qualquer marca e a terra segue para um canteiro, para o teto da sala Gotas de Orvalho ou para o despejo.
        readonly HashSet<int> _reclaimSoil = new HashSet<int>();
        // Marcas de escavação postas pela colônia em _reclaimSoil, para tirar as que deixarem de valer.
        readonly HashSet<int> _reclaimMarked = new HashSet<int>();
        readonly List<int> _reclaimDrop = new List<int>();
        int _soilCarriers;
        int _waterCarriers;
        int _builtWaterVersion = -1;
        // Origens do campo da água da última varredura e o tempo (de jogo) desde ela: a água que escorre só pede outra
        // varredura depois de ColonySettings.WaterFieldRefreshSeconds (ver WaterFieldStale).
        readonly List<int> _waterFieldSources = new List<int>();
        float _waterFieldAge;
        // Blocos de água e canteiros com planta que uma formiga reservou (Ant.SeekTarget) ao sair para buscá-los ou regá-los:
        // saem dos campos da água para as outras irem a outro. Recontados a cada Tick.
        readonly HashSet<int> _waterClaims = new HashSet<int>();
        long _waterClaimHash;
        long _builtWaterClaimHash;

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
            {
                if (ant.DewJob)
                    _dewCarriers++;
                else
                    _soilCarriers++;
            }
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
            _dewStoreSources.Clear();
            _bedsAwaitingPlant = 0;
            _bedsAwaitingWater = 0;
            _activeBeds = 0;
            bool waterClaimsChanged = _waterClaimHash != _builtWaterClaimHash;
            _builtWaterClaimHash = _waterClaimHash;
            _reclaimSoil.Clear();
            int bedPlants = 0;
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
                // Apagaram a sala por baixo do canteiro: ele não planta, não rega nem brota; só a terra sobra para reaproveitar.
                if (!_grid.IsNutrientRoom(bx, by))
                {
                    _reclaimSoil.Add(_grid.ToIndex(bx, by));
                    continue;
                }
                _activeBeds++;
                switch (_grid.GetBedStage(bx, by))
                {
                    case BedStage.Soil:
                        // Sem lado livre o canteiro não teria onde pôr o nutriente: espera alguém levar os que já brotaram.
                        if (_grid.FreeBedSides(bx, by) > 0 && AddStandCells(bx, by, _sources))
                            _bedsAwaitingPlant++;
                        break;
                    case BedStage.Planted:
                        bedPlants++;
                        // Já tem formiga indo regá-lo: continua esperando água, mas sai do campo para as outras irem a outro.
                        if (_waterClaims.Contains(_grid.ToIndex(bx, by)))
                            _bedsAwaitingWater++;
                        else if (AddStandCells(bx, by, _waterBedSources))
                            _bedsAwaitingWater++;
                        break;
                    case BedStage.Growing:
                        bedPlants++;
                        AddStandCells(bx, by, _curingSources);
                        break;
                }
            }

            // Os pontos de folha da sala Gotas de Orvalho disputam a planta com os canteiros (mesmo campo): a sala com
            // menos plantas postas leva a próxima (empate: os canteiros).
            BuildDewSpots(_dewStoreSources);
            if (_dewStoreSources.Count > 0 && (_sources.Count == 0 || _dewLeavesPlaced < bedPlants))
            {
                _sources.Clear();
                _sources.AddRange(_dewStoreSources);
            }
            UpdateField(_storeField, _sources);
            UpdateField(_nutrientField, _buildSources);
            UpdateField(_curingField, _curingSources);
            UpdateField(_waterBedField, _waterBedSources);
            UpdateField(_bedSpotField, _bedSpotCells);
            SyncReclaimMarks();
            BuildSoilPileField();
            BuildWaterField(waterClaimsChanged);

            _nutrientsReachQueen = false;
            foreach (int i in _buildSources)
                _nutrientsReachQueen |= _homeField.Get(i % w, i / w) != FlowField.Unreachable;
            // _buildSources é da construção: devolve vazio (BuildConstructionFields o refaz quando precisa).
            _buildSources.Clear();
        }

        /// <summary>
        /// Marca para escavar a terra que sobrou (<see cref="_reclaimSoil"/>) e tira as marcas que a colônia pôs e já não valem
        /// (a sala voltou a querer a terra, ou ela foi levada). A terra cavada segue o caminho normal de entrega
        /// (<see cref="StartDelivery"/>): canteiro novo ou teto da sala Gotas de Orvalho, se pedirem, senão o despejo.
        /// </summary>
        void SyncReclaimMarks()
        {
            foreach (int i in _reclaimSoil)
            {
                if (_reclaimMarked.Contains(i))
                    continue;
                int x = i % _grid.Width, y = i / _grid.Width;
                if (_grid.IsMarked(x, y))
                    continue; // já era uma marca do jogador: não é nossa para tirar depois
                if (_grid.SetMarked(x, y, true))
                    _reclaimMarked.Add(i);
            }
            if (_reclaimMarked.Count == 0)
                return;
            _reclaimDrop.Clear();
            foreach (int i in _reclaimMarked)
            {
                if (!_reclaimSoil.Contains(i))
                    _reclaimDrop.Add(i);
            }
            foreach (int i in _reclaimDrop)
            {
                _reclaimMarked.Remove(i);
                _grid.SetMarked(i % _grid.Width, i / _grid.Width, false);
            }
        }

        /// <summary>Algo ainda quer planta: canteiro esperando, folha da sala Gotas de Orvalho, sala em obra ou canteiro na sala (voltará a pedir).</summary>
        bool PlantStillWanted => PlantDemand > 0 || _dewSoilSpots > 0 || _activeBeds > 0;

        /// <summary>
        /// Sem destino para o que leva (a sala foi apagada, mudou de forma): solta o pixel de planta numa célula livre vizinha,
        /// como pixel solto marcado. Ele fica no chão até alguma sala pedir planta de novo (<see cref="IsWorkTarget"/>).
        /// </summary>
        void DropCarriedPlant(Ant ant)
        {
            for (int k = 0; k < 8; k++)
            {
                int x = ant.X + AntNavigation.Dx[k], y = ant.Y + AntNavigation.Dy[k];
                if (!_grid.InBounds(x, y) || _grid.Get(x, y) != CellMaterial.Empty || _grid.HasWater(x, y) || AntAt(x, y))
                    continue;
                _grid.Set(x, y, CellMaterials.ToDebris(ant.Carried), ant.CarriedShade);
                _grid.SetMarked(x, y, true);
                ant.DropTop();
                if (x != ant.X)
                    ant.FacingX = x > ant.X ? 1 : -1;
                if (ant.CarriedCount == 0)
                    ant.State = AntState.Idle;
                ant.Cooldown = _settings.DropSeconds;
                return;
            }
            Hold(ant); // sem célula livre ao lado: tenta de novo no próximo passo
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
        /// dela. Só é montado com canteiro esperando água. A água que escorre só pede outra varredura depois de
        /// <see cref="ColonySettings.WaterFieldRefreshSeconds"/>: na chuva forte ela muda a cada passo, e refazer as origens
        /// em toda rodada custava a maior parte da rodada. Entre varreduras o campo segue as mudanças de caminhabilidade com
        /// as origens de antes (sem as que viraram parede). Reserva nova também não varre o mapa: só saem as origens que ficaram
        /// sem água livre ao lado (a reserva solta volta na próxima varredura). A formiga que chega e não acha mais água
        /// espera a próxima.
        /// </summary>
        void BuildWaterField(bool claimsChanged)
        {
            bool waterMoved = _grid.WaterVersion != _builtWaterVersion &&
                              _waterFieldAge >= _settings.WaterFieldRefreshSeconds;
            if (!waterMoved && _bedsAwaitingWater > 0 && _waterFieldSources.Count > 0)
            {
                int kept = 0;
                for (int k = 0; k < _waterFieldSources.Count; k++)
                {
                    int i = _waterFieldSources[k];
                    if (_walkability.IsWalkableAt(i) && (!claimsChanged || HasFreeWaterAround(i)))
                        _waterFieldSources[kept++] = i;
                }
                _waterFieldSources.RemoveRange(kept, _waterFieldSources.Count - kept);
                UpdateField(_waterField, _waterFieldSources);
                return;
            }
            _waterFieldAge = 0f;
            _builtWaterVersion = _grid.WaterVersion;
            List<int> sources = _waterFieldSources;
            sources.Clear();
            _waterSeen.Clear();
            if (_bedsAwaitingWater == 0 || _grid.WaterCount == 0)
            {
                UpdateField(_waterField, sources);
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
                    // Bloco reservado por outra formiga não chama mais ninguém.
                    if (!_grid.HasWater(x, y) || _waterClaims.Contains(y * w + x))
                        continue;
                    for (int k = 0; k < 8; k++)
                    {
                        int nx = x + AntNavigation.Dx[k], ny = y + AntNavigation.Dy[k];
                        if (!_grid.InBounds(nx, ny) || _grid.HasWater(nx, ny) || !_walkability.IsWalkableAt(ny * w + nx))
                            continue;
                        if (_waterSeen.Add(ny * w + nx))
                            sources.Add(ny * w + nx);
                    }
                }
            }
            UpdateField(_waterField, sources);
        }

        /// <summary>Alguma vizinha da célula tem água que nenhuma formiga reservou (a célula ainda serve de origem do campo da água).</summary>
        bool HasFreeWaterAround(int index)
        {
            int w = _grid.Width;
            int x = index % w, y = index / w;
            for (int k = 0; k < 8; k++)
            {
                int nx = x + AntNavigation.Dx[k], ny = y + AntNavigation.Dy[k];
                if (_grid.HasWater(nx, ny) && !_waterClaims.Contains(ny * w + nx))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Campo até a terra fofa solta de fora: células caminháveis encostadas em terra despejada ou lama acima do solo
        /// original (a pilha de fora, a mesma da obra, sem a areia). Só é montado com ponto para canteiro; muda com a estrutura.
        /// </summary>
        void BuildSoilPileField()
        {
            if (_bedSpotCells.Count == 0 && _dewSoilSpots == 0)
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

        /// <summary>
        /// As reservas mudaram, ou a água mudou há mais de <see cref="ColonySettings.WaterFieldRefreshSeconds"/>, e há canteiro
        /// esperando água: os campos da água precisam de outra rodada.
        /// </summary>
        bool WaterFieldStale =>
            _bedsAwaitingWater > 0 && (_waterClaimHash != _builtWaterClaimHash ||
                                       _grid.WaterVersion != _builtWaterVersion && _waterFieldAge >= _settings.WaterFieldRefreshSeconds);

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
            if (ant.Carried == CellMaterial.TermiteRemains || ant.Carried == CellMaterial.Refuse || CellMaterials.ToDumped(ant.Carried) != CellMaterial.Spoil)
                return false;
            ChooseSoilRoom(ant, false, out bool toBed, out bool toDew);
            if (!toBed && !toDew)
                return false;
            ReleaseDumpSide(ant);
            ant.DewJob = toDew;
            ant.State = AntState.BringingSoil;
            if (toDew)
                _dewCarriers++;
            else
                _soilCarriers++;
            return true;
        }

        /// <summary>
        /// Terra fofa tem dois destinos: o canteiro novo e o teto da sala Gotas de Orvalho. Quando os dois pedem, vai para o que
        /// tem menos terra posta ou a caminho (empate: o canteiro), para as duas salas andarem juntas.
        /// </summary>
        void ChooseSoilRoom(Ant ant, bool dewOnly, out bool toBed, out bool toDew)
        {
            bool bedWants = !dewOnly && WantsBedSoil(ant);
            bool dewWants = WantsDewSoil(ant);
            toDew = dewWants && (!bedWants || _dewSoilPlaced + _dewCarriers < _activeBeds + _soilCarriers);
            toBed = bedWants && !toDew;
        }

        /// <summary>Ainda falta quem leve terra a um canteiro novo, e o ponto é alcançável de onde a formiga está.</summary>
        bool WantsBedSoil(Ant ant) =>
            _bedSpotCells.Count > 0 && _soilCarriers < WantedSoilCarriers &&
            _bedSpotField.Get(ant.X, ant.Y) != FlowField.Unreachable;

        /// <summary>Ainda falta quem leve terra fofa ao teto da sala Gotas de Orvalho, e o ponto é alcançável de onde a formiga está.</summary>
        bool WantsDewSoil(Ant ant) =>
            _dewSoilSpots > 0 && _dewCarriers < WantedDewCarriers && _dewSoilField.Get(ant.X, ant.Y) != FlowField.Unreachable;

        /// <summary>
        /// Levando terra para a sala de nutrientes: desce o campo dos pontos livres e, ao lado de um que ainda vale (e sem
        /// formiga em cima), larga a terra ali; ela vira canteiro. Sem ponto alcançável, leva a terra ao despejo.
        /// </summary>
        void UpdateBringingSoil(Ant ant)
        {
            // A terra vai ao canteiro novo ou ao teto da sala Gotas de Orvalho, conforme o destino escolhido ao pegá-la.
            FlowField field = ant.DewJob ? _dewSoilField : _bedSpotField;
            int d = field == null || ant.DewJob && _dewSoilSpots == 0 ? FlowField.Unreachable : field.Get(ant.X, ant.Y);
            if (d == FlowField.Unreachable)
            {
                SendSoilToDump(ant);
                return;
            }
            if (d <= 1 && (ant.DewJob ? TryPlaceDewSoil(ant) : TryPlaceBed(ant)))
                return;
            if (d > 0 && StepDownhill(ant, field))
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
                Emit(SfxId.Drop);
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
        bool TryStartSoilFetch(Ant ant, bool dewOnly = false)
        {
            if (_builtSoilPileVersion < 0 || _soilPileField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ChooseSoilRoom(ant, dewOnly, out bool toBed, out bool toDew);
            if (!toBed && !toDew)
                return false;
            ant.DewJob = toDew;
            ant.State = AntState.FetchingSoil;
            if (toDew)
                _dewCarriers++;
            else
                _soilCarriers++;
            UpdateFetchingSoil(ant);
            return true;
        }

        /// <summary>Desce o campo da pilha de fora e, encostada nela, começa a pegar um pixel de terra fofa.</summary>
        void UpdateFetchingSoil(Ant ant)
        {
            bool wanted = ant.DewJob ? _dewSoilSpots > 0 : _bedSpotCells.Count > 0;
            int d = wanted && _builtSoilPileVersion >= 0 ? _soilPileField.Get(ant.X, ant.Y) : FlowField.Unreachable;
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
            ant.DewJob = false;
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
            if (!PlantStillWanted)
            {
                DropCarriedPlant(ant);
                return;
            }
            int d = _storeField.Get(ant.X, ant.Y);
            if (d == 0)
            {
                if (!TryStore(ant) && !TryStoreDewLeaf(ant))
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
                Emit(SfxId.Plant);
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
        /// buscar um bloco de água. Ao sair, reserva o bloco (<see cref="PlanWaterFetch"/>), para as outras irem a outro.
        /// </summary>
        bool TryStartWaterFetch(Ant ant)
        {
            if (_bedsAwaitingWater - _waterCarriers <= 0)
                return false;
            if (_waterField.Get(ant.X, ant.Y) == FlowField.Unreachable || _waterBedField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ant.State = AntState.FetchingWater;
            _waterCarriers++;
            ant.ClearSeek();
            UpdateFetchingWater(ant);
            return true;
        }

        /// <summary>Trava o bloco de água ou o canteiro para as outras formigas; os campos da água o tiram na próxima rodada.</summary>
        void ClaimWater(int index)
        {
            if (_waterClaims.Add(index))
                _waterClaimHash += index * 2654435761L;
        }

        /// <summary>A célula está reservada por outra formiga (a reserva da própria não conta).</summary>
        bool WaterClaimedByOther(Ant ant, int index) => index != ant.SeekTarget && _waterClaims.Contains(index);

        /// <summary>
        /// Traça o caminho pelo campo da água até a célula ao lado dela e reserva ali um bloco de água livre
        /// (<see cref="Ant.SeekTarget"/>). <c>false</c> sem reserva: a formiga segue o campo como antes.
        /// </summary>
        bool PlanWaterFetch(Ant ant)
        {
            if (!TracePath(ant, _waterField, out int x, out int y, out _))
                return false;
            int count = 0;
            for (int k = 0; k < 8; k++)
            {
                int tx = x + AntNavigation.Dx[k], ty = y + AntNavigation.Dy[k];
                if (_grid.HasWater(tx, ty) && !_waterClaims.Contains(_grid.ToIndex(tx, ty)))
                    _candidates[count++] = k;
            }
            return ReserveWaterTarget(ant, x, y, count);
        }

        /// <summary>
        /// Traça o caminho pelo campo dos canteiros com planta e reserva um deles, livre, ao lado da chegada.
        /// <c>false</c> sem reserva: a formiga segue o campo como antes.
        /// </summary>
        bool PlanWaterDelivery(Ant ant)
        {
            if (!TracePath(ant, _waterBedField, out int x, out int y, out _))
                return false;
            int count = 0;
            for (int k = 0; k < 8; k++)
            {
                int tx = x + AntNavigation.Dx[k], ty = y + AntNavigation.Dy[k];
                if (_grid.GetBedStage(tx, ty) == BedStage.Planted && !_waterClaims.Contains(_grid.ToIndex(tx, ty)))
                    _candidates[count++] = k;
            }
            return ReserveWaterTarget(ant, x, y, count);
        }

        /// <summary>Reserva um dos <paramref name="count"/> vizinhos candidatos de (<paramref name="x"/>, <paramref name="y"/>).</summary>
        bool ReserveWaterTarget(Ant ant, int x, int y, int count)
        {
            if (count == 0)
            {
                ant.ClearSeek();
                return false;
            }
            int pick = _candidates[_rng.Next(count)];
            ant.SeekTarget = _grid.ToIndex(x + AntNavigation.Dx[pick], y + AntNavigation.Dy[pick]);
            ClaimWater(ant.SeekTarget);
            return true;
        }

        /// <summary>
        /// Segue o caminho até o alvo reservado (bloco de água ou canteiro). <c>true</c> = andou ou está ao lado dele;
        /// <c>false</c> = sem reserva, ou caminho cortado (a reserva cai e ela segue o campo).
        /// </summary>
        bool FollowWaterPath(Ant ant)
        {
            if (ant.SeekTarget < 0)
                return false;
            int step = StepPath(ant);
            if (step > 0)
                return true;
            int w = _grid.Width;
            int tx = ant.SeekTarget % w, ty = ant.SeekTarget / w;
            if (step == 0 && Mathf.Abs(tx - ant.X) <= 1 && Mathf.Abs(ty - ant.Y) <= 1)
                return true;
            ant.ClearSeek();
            return false;
        }

        /// <summary>Volta a ficar ociosa largando a reserva da água.</summary>
        void EndWaterTrip(Ant ant)
        {
            ant.ClearSeek();
            ant.State = AntState.Idle;
        }

        /// <summary>
        /// Vai ao bloco de água reservado (ou, sem reserva, desce o campo da água) e, ao lado dele, o tira: a célula de
        /// água some do mapa e a formiga já reserva o canteiro que vai regar.
        /// </summary>
        void UpdateFetchingWater(Ant ant)
        {
            if (_bedsAwaitingWater == 0)
            {
                EndWaterTrip(ant);
                return;
            }
            if (ant.SeekTarget < 0)
                PlanWaterFetch(ant);
            if (FollowWaterPath(ant))
            {
                if (!ant.SeekArrived)
                    return;
                // Chegou: pega o bloco reservado; se ele escorreu, qualquer outro livre ao lado.
                int w = _grid.Width;
                if (TakeWater(ant, ant.SeekTarget % w, ant.SeekTarget / w) || TakeAdjacentWater(ant))
                    return;
                ant.ClearSeek();
            }
            int d = _waterField.Get(ant.X, ant.Y);
            if (d == 0)
            {
                if (TakeAdjacentWater(ant))
                    return;
                // A água escorreu dali: espera o campo ser refeito.
                EndWaterTrip(ant);
                Hold(ant);
                return;
            }
            if (d != FlowField.Unreachable && StepDownhill(ant, _waterField))
                return;
            EndWaterTrip(ant);
            Hold(ant);
        }

        bool TakeAdjacentWater(Ant ant)
        {
            for (int k = 0; k < 8; k++)
            {
                int x = ant.X + AntNavigation.Dx[k], y = ant.Y + AntNavigation.Dy[k];
                if (_grid.InBounds(x, y) && !WaterClaimedByOther(ant, _grid.ToIndex(x, y)) && TakeWater(ant, x, y))
                    return true;
            }
            return false;
        }

        /// <summary>Tira o bloco de água vizinho em (<paramref name="x"/>, <paramref name="y"/>), se ainda houver água ali.</summary>
        bool TakeWater(Ant ant, int x, int y)
        {
            if (Mathf.Abs(x - ant.X) > 1 || Mathf.Abs(y - ant.Y) > 1 || !_grid.HasWater(x, y))
                return false;
            _grid.SetWater(x, y, false);
            WaterCollected++;
            if (x != ant.X)
                ant.FacingX = x > ant.X ? 1 : -1;
            ant.State = AntState.CarryingWater;
            ant.Cooldown = _settings.DropSeconds;
            ant.ClearSeek();
            PlanWaterDelivery(ant);
            return true;
        }

        /// <summary>
        /// Leva o bloco de água ao canteiro com planta reservado (ou, sem reserva, desce o campo dos canteiros) e o rega:
        /// ele começa a brotar.
        /// </summary>
        void UpdateCarryingWater(Ant ant)
        {
            // Nenhum canteiro na sala para regar (a sala foi apagada): a água volta ao mapa em vez de esperar para sempre.
            if (_activeBeds == 0)
            {
                PourOutWater(ant);
                return;
            }
            if (ant.SeekTarget < 0)
                PlanWaterDelivery(ant);
            if (FollowWaterPath(ant))
            {
                if (!ant.SeekArrived || TryWater(ant))
                    return;
                // O canteiro deixou de esperar água (foi apagado ou regado): procura outro.
                ant.ClearSeek();
            }
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

        /// <summary>Despeja o bloco de água numa célula livre vizinha; sem nenhuma, a água se perde.</summary>
        void PourOutWater(Ant ant)
        {
            for (int k = 0; k < 8; k++)
            {
                int x = ant.X + AntNavigation.Dx[k], y = ant.Y + AntNavigation.Dy[k];
                if (!_grid.IsOpen(x, y))
                    continue;
                _grid.SetWater(x, y, true);
                break;
            }
            EndWaterTrip(ant);
            ant.Cooldown = _settings.DropSeconds;
        }

        /// <summary>Rega um canteiro com planta vizinho: o reservado por ela primeiro; nunca o reservado por outra.</summary>
        bool TryWater(Ant ant)
        {
            int w = _grid.Width;
            if (ant.SeekTarget >= 0 && WaterBed(ant, ant.SeekTarget % w, ant.SeekTarget / w))
                return true;
            for (int k = 0; k < 8; k++)
            {
                int x = ant.X + AntNavigation.Dx[k], y = ant.Y + AntNavigation.Dy[k];
                if (_grid.InBounds(x, y) && !WaterClaimedByOther(ant, _grid.ToIndex(x, y)) && WaterBed(ant, x, y))
                    return true;
            }
            return false;
        }

        bool WaterBed(Ant ant, int x, int y)
        {
            if (Mathf.Abs(x - ant.X) > 1 || Mathf.Abs(y - ant.Y) > 1 || _grid.GetBedStage(x, y) != BedStage.Planted)
                return false;
            _grid.SetBed(x, y, BedStage.Growing, _settings.NutrientCureSeconds);
            _bedsAwaitingWater = Mathf.Max(0, _bedsAwaitingWater - 1);
            if (x != ant.X)
                ant.FacingX = x > ant.X ? 1 : -1;
            EndWaterTrip(ant);
            ant.Cooldown = _settings.DropSeconds;
            return true;
        }
    }
}
