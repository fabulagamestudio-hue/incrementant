using System;
using Fabula.Incrementant.Diagnostics;
using Fabula.Incrementant.World;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Painel da colônia (pedido do desenvolvedor em 2026-10-19): seções de trabalho que o jogador liga, desliga e limita,
    /// o que a rainha faz (ninho ou o trabalho de uma classe), o retrato das formigas (por seção, papel e lugar) e o ritmo de produção.
    /// </summary>
    public sealed partial class AntColony
    {
        /// <summary>Seções ativas e limites de formigas por seção. Vai para o save (versão 16).</summary>
        public WorkPolicy Work { get; } = new WorkPolicy();

        /// <summary>Ninho (põe ovos) ou o trabalho de uma das classes (<see cref="QueenJobs"/>). Vai para o save (versão 16).</summary>
        public QueenJob QueenJob { get; set; }

        /// <summary>Ritmo de produção (nutrientes, plantas, terra) por segundo de jogo.</summary>
        public ColonyRates Rates { get; } = new ColonyRates();

        // Formigas em cada seção, recontadas no começo do Tick e acertadas a cada formiga que muda de seção no passo.
        readonly int[] _sectionCounts = new int[WorkPolicy.Count];

        /// <summary>Formigas trabalhando na seção agora (a rainha trabalhando não conta).</summary>
        public int WorkersIn(WorkSection section) => _sectionCounts[(int)section];

        /// <summary>Seção do que a formiga está fazendo; <c>false</c> se ela está ociosa, vagando ou lutando.</summary>
        public bool TryGetSection(Ant ant, out WorkSection section)
        {
            int index = SectionIndex(ant);
            section = index >= 0 ? (WorkSection)index : default;
            return index >= 0;
        }

        /// <summary>Índice da seção do trabalho atual (-1 = nenhuma). Barato: é chamado duas vezes por formiga a cada passo.</summary>
        int SectionIndex(Ant ant)
        {
            // Levando algo a um depósito ou à área de resíduos, a seção é o destino (pedido do desenvolvedor em 2026-10-24),
            // venha de onde vier: terra cavada, planta cortada, item solto, água do mapa ou terra da pilha de fora.
            if (ant.StoreTarget != StorageKind.None && IsStockTrip(ant.State) &&
                WorkPolicy.TryGetDepotSection(ant.StoreTarget, out WorkSection depot))
                return (int)depot;
            if (ant.WasteDirtJob)
                return (int)WorkSection.WasteRoom;
            switch (ant.State)
            {
                case AntState.Dismembering:
                    return (int)WorkSection.Waste;
                case AntState.CarryingWaste:
                    return (int)WorkSection.WasteRoom;
                case AntState.Fetching:
                case AntState.Delivering:
                    return (int)WorkSection.QueenFeed;
                case AntState.FetchingEgg:
                case AntState.CarryingEgg:
                case AntState.Tending:
                case AntState.MovingBrood:
                    return (int)WorkSection.Nursery;
                case AntState.FetchingWater:
                case AntState.CarryingWater:
                    // Encher o depósito de água é da seção do depósito (acima), não do Jardim: com o Jardim no limite, ninguém
                    // enchia o depósito (2026-10-19). Só regar os canteiros conta aqui.
                    return (int)WorkSection.Garden;
                case AntState.Gardening:
                    return (int)WorkSection.Garden;
                case AntState.TakingStored:
                    // Planta e água do depósito para os canteiros: trabalho de quem cuida da sala de nutrientes.
                    return ant.StoreUse == StoreUse.BedPlant || ant.StoreUse == StoreUse.Water ? (int)WorkSection.Garden : -1;
                case AntState.TendingDew:
                    return (int)WorkSection.Dew;
                case AntState.BringingSoil:
                case AntState.FetchingSoil:
                case AntState.GatheringSoil:
                    return (int)(ant.DewJob ? WorkSection.Dew : WorkSection.Garden);
                case AntState.Gathering:
                case AntState.Hauling:
                case AntState.Placing:
                    return (int)WorkSection.Build;
                case AntState.Carrying:
                    if (CellMaterials.IsPlant(ant.Carried))
                        return (int)(ant.GardenSupply ? WorkSection.Garden : WorkSection.Harvest);
                    return (int)(ant.ReturningSoil ? WorkSection.Build : WorkSection.Dig);
                case AntState.Digging:
                    return (int)(IsPlantCell(ant.DigX, ant.DigY) ? WorkSection.Harvest : WorkSection.Dig);
                case AntState.Idle:
                    if (ant.Errand == AntErrand.Cut)
                        return (int)WorkSection.Harvest;
                    if (ant.Errand == AntErrand.Build)
                        return (int)WorkSection.Build;
                    if (ant.SeekTarget < 0)
                        return -1;
                    // Indo pegar um pixel reservado: pedaço de planta cortada é coleta; terra e rocha, escavação.
                    return (int)(!ant.SeekRock && IsPlantCell(ant.SeekTarget % _grid.Width, ant.SeekTarget / _grid.Width)
                        ? WorkSection.Harvest : WorkSection.Dig);
                default:
                    return -1;
            }
        }

        /// <summary>Estados em que <see cref="Ant.StoreTarget"/> é uma viagem para encher o depósito (ida à água ou à pilha, ou a volta carregada).</summary>
        static bool IsStockTrip(AntState state) =>
            state == AntState.Carrying || state == AntState.CarryingWater || state == AntState.FetchingWater ||
            state == AntState.FetchingSoil || state == AntState.GatheringSoil;

        /// <summary>
        /// Formigas que a seção ainda aceita além das que já estão nela (0 parada; <see cref="int.MaxValue"/> sem limite).
        /// Os itens soltos usam isso para não sair buscando o que o depósito no limite não vai receber.
        /// </summary>
        int SectionRoom(WorkSection section)
        {
            if (!Work.IsEnabled(section))
                return 0;
            int limit = Work.LimitOf(section);
            return limit == WorkPolicy.Unlimited ? int.MaxValue : UnityEngine.Mathf.Max(0, limit - _sectionCounts[(int)section]);
        }

        bool IsPlantCell(int x, int y)
        {
            CellMaterial material = _grid.Get(x, y);
            return CellMaterials.IsPlant(material) || CellMaterials.IsPlantDebris(material);
        }

        /// <summary>
        /// A formiga pode começar (ou seguir em) um trabalho da seção: ela está ativa e ainda tem vaga, ou a formiga já é
        /// dela. A rainha respeita a seção parada, mas não os limites (ela não entra na contagem por seção; pedido do
        /// desenvolvedor em 2026-10-21).
        /// </summary>
        bool SectionAllows(WorkSection section, Ant ant)
        {
            if (!Work.IsEnabled(section))
                return false;
            if (ant == Queen)
                return true;
            return SectionIndex(ant) == (int)section || Work.HasRoom(section, _sectionCounts[(int)section]);
        }

        /// <summary>
        /// A seção tem onde trabalhar: o depósito dela pintado ou alguma área de resíduos (as seções de trabalho, sempre). O
        /// painel apaga a linha de quem ainda não tem sala.
        /// </summary>
        public bool HasSectionRoom(WorkSection section)
        {
            if (section == WorkSection.WasteRoom)
                return _grid.WasteRoomCount > 0;
            StorageKind kind = WorkPolicy.DepotOf(section);
            return kind == StorageKind.None || HasDepot(kind);
        }

        void ClearSectionCounts() => Array.Clear(_sectionCounts, 0, _sectionCounts.Length);

        // ---------------------------------------------------------------- segunda função (KI-033)

        // Formigas que cada seção poderia ocupar agora (a demanda dela), recontadas no começo do Tick.
        readonly int[] _sectionWanted = new int[WorkPolicy.Count];
        readonly bool[] _sectionTried = new bool[WorkPolicy.Count];
        // Células de onde se cava alguma marca (origens do campo de escavação), da última rodada de campos.
        int _digSpots;

        /// <summary>Quantas formigas a seção poderia ocupar agora (para a necessidade da segunda função).</summary>
        public int SectionWanted(WorkSection section) => _sectionWanted[(int)section];

        /// <summary>
        /// Demanda de cada seção em formigas, com o que a colônia já conta: berçário (ovo esperando e blocos de cria),
        /// sala de nutrientes (canteiros esperando água e blocos que brotam), rainha (nutrientes prontos), escavação
        /// (pontos de onde cavar), obra (marcas), coleta (planta que os canteiros ou o depósito ainda pedem), gotas de orvalho
        /// (pontos de terra e de folha e, pronta e ligada, os blocos de cuidado) e resíduos (partes de cupim no chão).
        /// </summary>
        void UpdateSectionWanted()
        {
            _sectionWanted[(int)WorkSection.Nursery] = (_brood.HasEggToCarry ? 1 : 0) + Desired(_nursery.Blocks);
            _sectionWanted[(int)WorkSection.Garden] = _bedsAwaitingWater + (FoodDepot ? _bedsAwaitingPlant : 0) + Desired(_garden.Blocks);
            _sectionWanted[(int)WorkSection.QueenFeed] = UnityEngine.Mathf.Min(_grid.NutrientCount, MaxFetchers);
            _sectionWanted[(int)WorkSection.Dig] = _grid.MarkedCount > 0 ? _digSpots : 0;
            _sectionWanted[(int)WorkSection.Build] = BuildWorkCount;
            _sectionWanted[(int)WorkSection.Harvest] = _harvestPaused ? 0 : UnityEngine.Mathf.Max(0, PlantSupplyDemand - _plantsInFlight - PlantsWaiting);
            _sectionWanted[(int)WorkSection.Dew] = _dewSoilSpots + _dewLeafSpots + (DewDripping ? DewBlockCount : 0);
            _sectionWanted[(int)WorkSection.Waste] = _corpsePieces;
        }

        /// <summary>
        /// Segunda função: com o papel sem trabalho, a formiga vai à seção mais desatendida (maior fração da demanda sem
        /// formiga; empate, a de maior demanda) que ela consegue atender como operária. Quem aceita entra na contagem da
        /// seção no mesmo passo (<see cref="UpdateAnt"/>), então as que decidem depois veem a falta menor. <c>false</c> = nenhuma
        /// seção com falta tinha trabalho para ela; segue a ordem fixa da operária.
        /// </summary>
        bool TrySecondFunction(Ant ant)
        {
            Array.Clear(_sectionTried, 0, _sectionTried.Length);
            for (int round = 0; round < WorkPolicy.Count; round++)
            {
                int best = -1;
                float bestShare = 0f;
                for (int s = 0; s < WorkPolicy.Count; s++)
                {
                    int wanted = _sectionWanted[s];
                    if (_sectionTried[s] || wanted <= 0 || !Work.IsEnabled((WorkSection)s))
                        continue;
                    int missing = wanted - _sectionCounts[s];
                    if (missing <= 0)
                        continue;
                    float share = missing / (float)wanted;
                    if (best < 0 || share > bestShare || share == bestShare && wanted > _sectionWanted[best])
                    {
                        best = s;
                        bestShare = share;
                    }
                }
                if (best < 0)
                    return false;
                _sectionTried[best] = true;
                _stepMark = 30 + best;
                if (TrySectionWork(ant, (WorkSection)best))
                {
                    ant.SecondFunction = best;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Tenta o trabalho de uma seção só (cada TryStart* confere papel, limite da seção e alcance).</summary>
        bool TrySectionWork(Ant ant, WorkSection section)
        {
            switch (section)
            {
                case WorkSection.Nursery:
                    return AntRoles.CarriesEggs(ant.TaskRole) && TryStartEggFetch(ant) || TryStartCare(ant);
                case WorkSection.Garden:
                    return TryStartGardenSupply(ant) || AntRoles.CarriesWater(ant.TaskRole) && TryStartWaterFetch(ant) ||
                           TryStartSoilFetch(ant) || TryStartCare(ant);
                case WorkSection.QueenFeed:
                    return AntRoles.FetchesNutrients(ant.TaskRole) && _grid.NutrientCount > 0 && TryStartFetch(ant);
                case WorkSection.Dig:
                    return _grid.MarkedCount > 0 && SectionAllows(WorkSection.Dig, ant) &&
                           SeekWork(ant, _digField, rock: false, FlowField.Unreachable);
                case WorkSection.Build:
                    return _hasSite && TryBuildWork(ant);
                case WorkSection.Harvest:
                    return TryStartLoose(ant) || AntRoles.Harvests(ant.TaskRole) && TryCutWork(ant);
                case WorkSection.Dew:
                    return TryStartSoilFetch(ant, dewOnly: true) || TryStartDewCare(ant);
                case WorkSection.Waste:
                    return TryStartDismember(ant);
                default:
                    return false;
            }
        }

        void CountSection(Ant ant)
        {
            int index = SectionIndex(ant);
            if (index >= 0)
                _sectionCounts[index]++;
        }

        /// <summary>Passo de uma formiga, mantendo a contagem por seção certa para as que decidem depois dela no mesmo passo.</summary>
        void UpdateAnt(Ant ant, float deltaTime)
        {
            int before = SectionIndex(ant);
            int state = (int)ant.State;
            long start = SimProfiler.Now();
            _stepMark = 0;
            StepAnt(ant, deltaTime);
            long elapsed = SimProfiler.Now() - start;
            Profiler.AddState(state, start);
            float stepMs = SimProfiler.ToMs(elapsed);
            if (stepMs >= SlowStepLog.ThresholdMs)
                SlowSteps.Add(new SlowStep
                {
                    Ms = stepMs, Before = state, After = (int)ant.State, Role = (int)ant.Role, Task = (int)ant.TaskRole,
                    Second = ant.SecondFunction, Mark = _stepMark, X = ant.X, Y = ant.Y, SimTime = SimulationTime,
                });
            int after = SectionIndex(ant);
            if (before == after)
                return;
            if (before >= 0)
                _sectionCounts[before]--;
            if (after >= 0)
                _sectionCounts[after]++;
        }

        /// <summary>
        /// Quem só cuida (berçário, sala de nutrientes, gotas de orvalho) não tem o que terminar: com a seção desligada ou
        /// acima do limite, sai na hora. As demais terminam a viagem e só depois deixam de voltar à seção. A rainha sai só
        /// com a seção desligada (ela não segue os limites).
        /// </summary>
        bool QuitStoppedSection(Ant ant)
        {
            WorkSection section;
            switch (ant.State)
            {
                case AntState.Tending: section = WorkSection.Nursery; break;
                case AntState.Gardening: section = WorkSection.Garden; break;
                case AntState.TendingDew: section = WorkSection.Dew; break;
                default: return false;
            }
            // Ela própria já está na contagem: cabe se as outras (contagem - 1) ainda estão abaixo do limite.
            if (Work.IsEnabled(section) && (ant == Queen || Work.HasRoom(section, _sectionCounts[(int)section] - 1)))
                return false;
            if (section == WorkSection.Nursery)
                Leave(ant, ref _nursery);
            else if (section == WorkSection.Garden)
                Leave(ant, ref _garden);
            else
                LeaveDewCare(ant);
            return true;
        }

        // ---------------------------------------------------------------- rainha trabalhando

        /// <summary>A rainha trabalha neste passo (ou termina o que tem nas mãos) e entra nas contas do passo como uma formiga.</summary>
        bool QueenWorks => QueenJob != QueenJob.Nest || Queen.State != AntState.Idle || Queen.CarriedCount > 0;

        /// <summary>
        /// Rainha fora do ninho: faz o trabalho da classe escolhida com a mesma IA da formiga daquele papel (sem trabalho do
        /// papel, o da comum), respeitando as seções paradas do painel, mas não os limites. Ao voltar ao ninho (ou com o
        /// sino), termina o que tem nas mãos antes. Devolve true se o passo foi gasto nisso.
        /// </summary>
        bool TickQueenWork(Ant queen)
        {
            bool home = QueenJob == QueenJob.Nest || Recalled;
            // Cuidado não tem o que terminar: de volta ao ninho, ela sai na hora.
            if (home)
                LeaveQueenCare(queen);
            else if (QuitStoppedSection(queen))
                return true;
            bool busy = queen.State != AntState.Idle || queen.CarriedCount > 0;
            if (!busy && home)
            {
                queen.ActingAsWorker = false;
                queen.Errand = AntErrand.None;
                queen.ClearSeek();
                return false;
            }
            RunState(queen);
            return true;
        }

        void LeaveQueenCare(Ant queen)
        {
            switch (queen.State)
            {
                case AntState.Gardening: Leave(queen, ref _garden); break;
                case AntState.TendingDew: LeaveDewCare(queen); break;
                case AntState.TendingAphid: LeaveAphid(queen); break;
                // O berçário da rainha sozinha (sem adultas) segue no TickLoneQueenBrood.
                case AntState.Tending:
                    if (_ants.Count > 0)
                        Leave(queen, ref _nursery);
                    break;
            }
        }

        // ---------------------------------------------------------------- retrato

        /// <summary>Preenche <paramref name="census"/> com o retrato de agora (o painel chama algumas vezes por segundo).</summary>
        public void TakeCensus(ColonyCensus census)
        {
            census.Clear();
            foreach (Ant ant in _ants)
            {
                int section = SectionIndex(ant);
                if (section >= 0)
                    census.Sections[section]++;
                else if (ant.State == AntState.Fighting)
                    census.Fighting++;
                else
                    census.Idle++;
                census.Roles[(int)ant.Role]++;
                census.Places[(int)PlaceOf(ant.X, ant.Y)]++;
            }
        }

        ColonyPlace PlaceOf(int x, int y)
        {
            if (IsOutdoor(x, y))
                return ColonyPlace.Outdoor;
            if (IsQueenArea(x, y) || _grid.IsQueenRoom(x, y))
                return ColonyPlace.QueenRoom;
            if (_grid.IsNurseryRoom(x, y))
                return ColonyPlace.Nursery;
            if (_grid.IsNutrientRoom(x, y))
                return ColonyPlace.NutrientRoom;
            if (_grid.IsDewRoom(x, y))
                return ColonyPlace.DewRoom;
            if (_grid.IsWasteRoom(x, y))
                return ColonyPlace.WasteRoom;
            if (_grid.IsFarmRoom(x, y))
                return ColonyPlace.FarmRoom;
            return ColonyPlace.Tunnels;
        }

        /// <summary>Onde a rainha está agora (para o painel).</summary>
        public ColonyPlace QueenPlace => PlaceOf(Queen.X, Queen.Y);

        void SampleRates() => Rates.Sample(SimulationTime, TotalNutrients, GrownNutrients, HarvestedPixels, TotalDelivered);
    }

    /// <summary>Lugar do mapa onde uma formiga está, para o retrato do painel.</summary>
    public enum ColonyPlace
    {
        Outdoor,
        Tunnels,
        QueenRoom,
        Nursery,
        NutrientRoom,
        DewRoom,
        WasteRoom,
        FarmRoom,
    }

    /// <summary>Retrato das formigas adultas: por seção de trabalho, sem trabalho, lutando, por papel e por lugar.</summary>
    public sealed class ColonyCensus
    {
        public readonly int[] Sections = new int[WorkPolicy.Count];
        public readonly int[] Roles = new int[Enum.GetValues(typeof(AntRole)).Length];
        public readonly int[] Places = new int[Enum.GetValues(typeof(ColonyPlace)).Length];
        /// <summary>Sem trabalho: vagando, esperando ou recolhidas pelo sino.</summary>
        public int Idle;
        public int Fighting;

        public void Clear()
        {
            Array.Clear(Sections, 0, Sections.Length);
            Array.Clear(Roles, 0, Roles.Length);
            Array.Clear(Places, 0, Places.Length);
            Idle = 0;
            Fighting = 0;
        }
    }
}
