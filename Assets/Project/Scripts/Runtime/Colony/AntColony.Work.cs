using System;
using Fabula.Incrementant.World;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Painel da colônia (pedido do desenvolvedor em 2026-10-19): seções de trabalho que o jogador liga, desliga e limita,
    /// o que a rainha faz (ninho ou coleta), o retrato das formigas (por seção, papel e lugar) e o ritmo de produção.
    /// </summary>
    public sealed partial class AntColony
    {
        /// <summary>Seções ativas e limites de formigas por seção. Vai para o save (versão 16).</summary>
        public WorkPolicy Work { get; } = new WorkPolicy();

        /// <summary>Ninho (põe ovos), coleta (come o nutriente guardado, corta e carrega plantas) ou só nutriente (fica na sala de nutrientes). Vai para o save (versão 16).</summary>
        public QueenJob QueenJob { get; set; }

        /// <summary>Ritmo de produção (nutrientes, plantas, terra) por segundo de jogo.</summary>
        public ColonyRates Rates { get; } = new ColonyRates();

        // Formigas em cada seção, recontadas no começo do Tick e acertadas a cada formiga que muda de seção no passo.
        readonly int[] _sectionCounts = new int[WorkPolicy.Count];

        /// <summary>Até onde (passos do campo de escavação) a rainha coletora vai buscar um pedaço de planta solto.</summary>
        const int QueenPickupRange = 24;

        /// <summary>Formigas trabalhando na seção agora (a rainha coletora não conta).</summary>
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
            switch (ant.State)
            {
                case AntState.Dismembering:
                case AntState.CarryingWaste:
                    return (int)WorkSection.Waste;
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
                    // Encher o depósito de água não é da seção Jardim (pedido do desenvolvedor em 2026-10-19): com o Jardim
                    // no limite, ninguém enchia o depósito. Só regar os canteiros conta lá.
                    return ant.StoreTarget == StorageKind.Water ? -1 : (int)WorkSection.Garden;
                case AntState.Gardening:
                    return (int)WorkSection.Garden;
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
                        return (int)WorkSection.Harvest;
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

        bool IsPlantCell(int x, int y)
        {
            CellMaterial material = _grid.Get(x, y);
            return CellMaterials.IsPlant(material) || CellMaterials.IsPlantDebris(material);
        }

        /// <summary>
        /// A formiga pode começar (ou seguir em) um trabalho da seção: ela está ativa e ainda tem vaga, ou a formiga já é
        /// dela. A rainha não segue os limites (ela só trabalha sozinha ou coletando, por escolha do jogador).
        /// </summary>
        bool SectionAllows(WorkSection section, Ant ant)
        {
            if (ant == Queen)
                return true;
            if (!Work.IsEnabled(section))
                return false;
            return SectionIndex(ant) == (int)section || Work.HasRoom(section, _sectionCounts[(int)section]);
        }

        void ClearSectionCounts() => Array.Clear(_sectionCounts, 0, _sectionCounts.Length);

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
            StepAnt(ant, deltaTime);
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
        /// acima do limite, sai na hora. As demais terminam a viagem e só depois deixam de voltar à seção.
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
            if (Work.IsEnabled(section) && Work.HasRoom(section, _sectionCounts[(int)section] - 1))
                return false;
            if (section == WorkSection.Nursery)
                Leave(ant, ref _nursery);
            else if (section == WorkSection.Garden)
                Leave(ant, ref _garden);
            else
                LeaveDewCare(ant);
            return true;
        }

        // ---------------------------------------------------------------- rainha coletora

        /// <summary>
        /// Rainha fora do ninho. Coletora: come o nutriente guardado, corta plantas e leva os pedaços aos canteiros, como uma
        /// operária. Só nutriente: vai à sala de nutrientes, fica lá e come cada nutriente pronto. Ao voltar ao ninho (ou com
        /// o sino), termina o que tem nas mãos antes. Devolve true se o passo foi gasto nisso.
        /// </summary>
        bool TickQueenWork(Ant queen)
        {
            bool busy = queen.State != AntState.Idle || queen.CarriedCount > 0;
            if (!busy && (QueenJob == QueenJob.Nest || Recalled))
            {
                queen.Errand = AntErrand.None;
                queen.ClearSeek();
                return false;
            }
            if (queen.State != AntState.Idle)
            {
                RunState(queen);
                return true;
            }
            _seekKept = false;
            _errandKept = false;
            if (queen.CarriedCount > 0)
                UpdateLoadedIdle(queen);
            else if (QueenJob == QueenJob.Nutrients)
                StayAtNutrients(queen);
            else if (!TryQueenEatNutrient(queen) &&
                     !(IsOutdoor(queen.X, queen.Y) && SeekWork(queen, _digField, rock: false, QueenPickupRange)) &&
                     !TryCutWork(queen))
                WanderHome(queen);
            if (!_seekKept)
                queen.ClearSeek();
            if (!_errandKept)
                queen.Errand = AntErrand.None;
            return true;
        }

        /// <summary>
        /// Rainha só de nutriente: come o nutriente pronto mais perto; sem nenhum, vai à sala de nutrientes pelo campo de
        /// algum canteiro (crescendo, à espera de planta ou de água, ou ponto livre) e espera lá dentro o próximo brotar.
        /// </summary>
        void StayAtNutrients(Ant queen)
        {
            if (TryQueenEatNutrient(queen))
                return;
            if (_grid.IsNutrientRoom(queen.X, queen.Y))
            {
                queen.Cooldown = QueenNutrientWaitSeconds;
                return;
            }
            FlowField[] fields = { _curingField, _storeField, _waterBedField, _bedSpotField };
            foreach (FlowField field in fields)
            {
                int d = field.Get(queen.X, queen.Y);
                if (d == FlowField.Unreachable)
                    continue;
                if (d == 0)
                    queen.Cooldown = QueenNutrientWaitSeconds;
                else if (!StepDownhill(queen, field))
                    continue;
                return;
            }
            WanderHome(queen);
        }

        /// <summary>Espera da rainha só de nutriente parada na sala, entre uma olhada e outra.</summary>
        const float QueenNutrientWaitSeconds = 0.5f;

        /// <summary>Reservas e pedidos da rainha coletora entram nas contas do passo, como os das formigas.</summary>
        void CountQueenWork()
        {
            Ant queen = Queen;
            if (queen.State == AntState.Digging)
            {
                ClaimTarget(queen);
                if (CellMaterials.IsPlant(_grid.Get(queen.DigX, queen.DigY)))
                    _cutters++;
            }
            if (queen.SeekTarget >= 0)
            {
                if (queen.State == AntState.Idle)
                    Claim(queen.SeekTarget);
                else
                    queen.ClearSeek();
            }
            if (queen.State != AntState.Idle)
                queen.Errand = AntErrand.None;
            else if (queen.Errand == AntErrand.Cut)
                _cutters++;
            if (IsHarvesting(queen))
                _plantsInFlight += queen.CarriedCount;
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
                else if (ant.StoreTarget == StorageKind.Water && (ant.State == AntState.FetchingWater || ant.State == AntState.CarryingWater))
                    census.WaterStocking++;
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
        /// <summary>Levando água do mapa ao depósito de água (fora das seções de trabalho).</summary>
        public int WaterStocking;

        public void Clear()
        {
            Array.Clear(Sections, 0, Sections.Length);
            Array.Clear(Roles, 0, Roles.Length);
            Array.Clear(Places, 0, Places.Length);
            Idle = 0;
            Fighting = 0;
            WaterStocking = 0;
        }
    }
}
