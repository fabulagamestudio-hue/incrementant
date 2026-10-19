using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Cuidado de região (design 14/18/19/26): o berçário (crias) e a sala de nutrientes (canteiros que brotam, o cultivo)
    /// só avançam com formigas trabalhando dentro deles. No berçário, trabalhar é mover as crias de um ponto a outro; na
    /// sala, é ir de canteiro em canteiro mexendo em cada um. A formiga escolhe blocos variados, e só o bloco com que ela interage recebe o ritmo da especialista (cuidadora 4/3,
    /// jardineira com J1 ×1,3); os demais avançam no ritmo normal enquanto houver cobertura. Quanto mais blocos a
    /// região tem, mais formigas ela chama (<see cref="Desired"/>), escolhendo a região mais carente.
    /// </summary>
    public sealed partial class AntColony
    {
        /// <summary>Quem trabalha numa região neste passo (recontado a cada <see cref="Tick"/>).</summary>
        struct RegionLoad
        {
            /// <summary>Blocos que pedem cuidado: crias no berçário, canteiros que brotam na sala de nutrientes.</summary>
            public int Blocks;
            public int Specialists;
            public int Commons;
            /// <summary>Soma da capacidade (blocos cobertos) de quem já está dentro da sala.</summary>
            public int CapacityInside;
        }

        RegionLoad _nursery;
        RegionLoad _garden;
        int _workerCount;
        int _commonTenders;
        readonly List<BlockBoost> _boosts = new List<BlockBoost>();
        // Blocos que alguma formiga já escolheu neste passo: as outras escolhem blocos diferentes.
        readonly HashSet<int> _nurseryTargets = new HashSet<int>();
        readonly HashSet<int> _gardenTargets = new HashSet<int>();

        // Sala de nutrientes: campo até os canteiros que brotam (de onde se cuida deles).
        readonly List<int> _curingSources = new List<int>();
        readonly FlowField _curingField;

        void ResetCare()
        {
            _nursery = new RegionLoad { Blocks = _brood.CaredCount };
            _garden = new RegionLoad { Blocks = _grid.GrowingBedCount };
            _workerCount = 0;
            _commonTenders = 0;
            _boosts.Clear();
            _nurseryTargets.Clear();
            _gardenTargets.Clear();
            _dewTendedPoints = 0;
            _dewTendCount = 0;
            _dewTendTargets.Clear();
        }

        void CountCare(Ant ant)
        {
            if (ant.State == AntState.TendingDew)
            {
                _dewTendCount++;
                if (ant.EggId > 0)
                {
                    _dewTendTargets.Add(ant.EggId);
                    if (ant.Interacting)
                        _dewTendedPoints += DewBlockSize(ant.EggId - 1);
                }
            }
            if (ant.Role == AntRole.Worker)
                _workerCount++;
            bool nursery = ant.State == AntState.Tending || ant.State == AntState.MovingBrood;
            bool garden = ant.State == AntState.Gardening;
            if (!nursery && !garden)
                return;

            ref RegionLoad region = ref (nursery ? ref _nursery : ref _garden);
            if (ant.Role == AntRole.Worker)
            {
                region.Commons++;
                _commonTenders++;
            }
            else
            {
                region.Specialists++;
            }
            // Só quem está dentro da sala faz os blocos avançarem: ir até lá ainda não é cuidar.
            if (nursery ? _grid.IsNurseryRoom(ant.X, ant.Y) : _grid.IsNutrientRoom(ant.X, ant.Y))
                region.CapacityInside += nursery ? Tree.CareCapacity(ant.Role) : GardenCapacity(ant);

            if (nursery && ant.EggId != 0)
            {
                _nurseryTargets.Add(ant.EggId);
                if (ant.Interacting)
                    _boosts.Add(new BlockBoost { Id = ant.EggId, Rate = Tree.CareRate(ant.Role) });
            }
            if (garden && ant.EggId != 0)
                _gardenTargets.Add(ant.EggId);
        }

        // ---------------------------------------------------------------- prioridade das regiões

        int Desired(int blocks) => blocks <= 0 ? 0 : (blocks + _settings.BlocksPerTender - 1) / _settings.BlocksPerTender;

        /// <summary>Quanto a região precisa de gente: blocos por formiga que já está lá. Quanto mais blocos, mais alto.</summary>
        static float Score(in RegionLoad region) => region.Blocks / (float)(region.Specialists + region.Commons + 1);

        /// <summary>
        /// A formiga entra na região se ela ainda quer gente: a especialista vai até a região ter uma por bloco desejado; a
        /// comum ocupa o que as especialistas não cobrem, sem passar de <see cref="ColonySettings.CommonTenderShare"/> das comuns.
        /// </summary>
        bool WantsTender(Ant ant, in RegionLoad region)
        {
            if (region.Blocks == 0)
                return false;
            int desired = Desired(region.Blocks);
            if (ant.Role != AntRole.Worker)
                return region.Specialists < desired;
            int cap = Mathf.Max(1, Mathf.RoundToInt(_workerCount * _settings.CommonTenderShare));
            return region.Commons < desired - region.Specialists && _commonTenders < cap;
        }

        bool IsSurplus(Ant ant, in RegionLoad region)
        {
            int desired = Desired(region.Blocks);
            return ant.Role != AntRole.Worker ? region.Specialists > desired : region.Commons > Mathf.Max(0, desired - region.Specialists);
        }

        /// <summary>Ociosa: vai à região (berçário ou sala de nutrientes) mais carente entre as que o papel dela cuida e ainda querem gente.</summary>
        bool TryStartCare(Ant ant)
        {
            bool nursery = AntRoles.Tends(ant.Role) && _nurserySpots.Count > 0 && WantsTender(ant, _nursery) &&
                           _nurseryField.Get(ant.X, ant.Y) != FlowField.Unreachable;
            bool garden = AntRoles.Gardens(ant.Role) && WantsTender(ant, _garden) &&
                          _curingField.Get(ant.X, ant.Y) != FlowField.Unreachable;
            if (!nursery && !garden)
                return false;
            bool chooseNursery = nursery && (!garden || Score(_nursery) >= Score(_garden));

            ant.EggId = 0;
            ant.Interacting = false;
            if (chooseNursery)
            {
                ant.State = AntState.Tending;
                Join(ant, ref _nursery);
                UpdateTending(ant);
            }
            else
            {
                ant.State = AntState.Gardening;
                Join(ant, ref _garden);
                UpdateGardening(ant);
            }
            return true;
        }

        void Join(Ant ant, ref RegionLoad region)
        {
            if (ant.Role == AntRole.Worker)
            {
                region.Commons++;
                _commonTenders++;
            }
            else
            {
                region.Specialists++;
            }
        }

        void Leave(Ant ant, ref RegionLoad region)
        {
            if (ant.Role == AntRole.Worker)
            {
                region.Commons--;
                _commonTenders--;
            }
            else
            {
                region.Specialists--;
            }
            ant.State = AntState.Idle;
            ant.EggId = 0;
            ant.Interacting = false;
            Hold(ant);
        }

        float CarePause(Ant ant) => _settings.CarePauseSeconds / Tree.CareRate(ant.Role);

        bool NearCell(Ant ant, Vector2Int cell) => Mathf.Abs(ant.X - cell.x) <= 1 && Mathf.Abs(ant.Y - cell.y) <= 1;

        /// <summary>Um passo rumo à célula dentro da sala: entra nela pelo campo dado e, dentro, aproxima-se da célula.</summary>
        void WalkToCell(Ant ant, Vector2Int cell, FlowField entry, bool insideRoom)
        {
            int d = entry.Get(ant.X, ant.Y);
            if (!insideRoom && d != FlowField.Unreachable && d > 0 && StepDownhill(ant, entry))
                return;
            if (StepTowardsCell(ant, cell.x, cell.y, keepQueenRoom: false))
                return;
            Wander(ant);
        }

        // ---------------------------------------------------------------- berçário

        Vector2Int SpotCell(int slot) => _nurserySpots[slot % _nurserySpots.Count];

        /// <summary>Cria do berçário que outra formiga ainda não escolheu e que não foi a última que esta mexeu (blocos variados).</summary>
        bool PickNurseryTarget(Ant ant, out BroodRecord target)
        {
            int placed = _brood.PlacedCount;
            target = default;
            for (int pass = 0; pass < 3; pass++)
            {
                int seen = 0;
                for (int k = 0; k < placed; k++)
                {
                    if (!_brood.TryGetPlaced(k, out BroodRecord b))
                        break;
                    if (pass < 2 && _nurseryTargets.Contains(b.Id) || pass < 1 && b.Id == ant.LastBlock)
                        continue;
                    if (_rng.Next(++seen) == 0)
                        target = b;
                }
                if (seen > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Cuidando do berçário: escolhe uma cria (variando), vai até o ponto dela, pega-a e a leva para o ponto menos
        /// ocupado. Sem cria, ou com formigas demais para a região, sai e volta a procurar outra tarefa.
        /// </summary>
        void UpdateTending(Ant ant)
        {
            if (_nursery.Blocks == 0 || _nurserySpots.Count == 0 || IsSurplus(ant, _nursery) ||
                _nurseryField.Get(ant.X, ant.Y) == FlowField.Unreachable)
            {
                Leave(ant, ref _nursery);
                return;
            }

            BroodRecord target = default;
            if (ant.EggId == 0 || !_brood.TryGet(ant.EggId, out target) || target.Phase != EggPhase.Placed)
            {
                ant.Interacting = false;
                if (!PickNurseryTarget(ant, out target))
                {
                    Leave(ant, ref _nursery);
                    return;
                }
                ant.EggId = target.Id;
                _nurseryTargets.Add(target.Id);
            }

            Vector2Int spot = SpotCell(target.Slot);
            if (!NearCell(ant, spot))
            {
                ant.Interacting = false;
                WalkToCell(ant, spot, _nurseryField, _grid.IsNurseryRoom(ant.X, ant.Y));
                return;
            }

            ant.Interacting = true;
            int destination = PickMoveDestination(target.Slot);
            if (destination < 0)
            {
                // Um ponto só: atende no lugar e passa para a próxima.
                ant.LastBlock = target.Id;
                ant.EggId = 0;
                ant.Cooldown = CarePause(ant);
                return;
            }
            _brood.BeginMove(target.Id);
            ant.State = AntState.MovingBrood;
            ant.DigX = destination;
            ant.Cooldown = CarePause(ant);
            _nurseryDirty = true;
        }

        /// <summary>Ponto de destino da cria: entre os outros pontos, o de menor ocupação (sorteado no empate); -1 se não há outro.</summary>
        int PickMoveDestination(int currentSlot)
        {
            int current = currentSlot % _nurserySpots.Count;
            int best = -1, ties = 0, bestLoad = int.MaxValue;
            for (int i = 0; i < _nurserySpots.Count; i++)
            {
                if (i == current)
                    continue;
                int load = _brood.PlacedAt(i);
                if (load < bestLoad)
                {
                    best = i;
                    bestLoad = load;
                    ties = 1;
                }
                else if (load == bestLoad && _rng.Next(++ties) == 0)
                {
                    best = i;
                }
            }
            return best;
        }

        void UpdateMovingBrood(Ant ant)
        {
            if (!_brood.TryGet(ant.EggId, out BroodRecord cria) || cria.Phase != EggPhase.Moving)
            {
                ant.State = AntState.Idle;
                ant.EggId = 0;
                ant.Interacting = false;
                Hold(ant);
                return;
            }
            // Berçário sumiu ou ficou inalcançável: a cria volta ao ponto de onde saiu.
            if (ant.DigX >= _nurserySpots.Count || _nurseryField.Get(ant.X, ant.Y) == FlowField.Unreachable)
            {
                _brood.EndMove(ant.EggId, cria.Slot);
                _nurseryDirty = true;
                ant.State = AntState.Idle;
                ant.EggId = 0;
                ant.Interacting = false;
                Hold(ant);
                return;
            }
            Vector2Int destination = _nurserySpots[ant.DigX];
            ant.Interacting = true; // carregando a cria, é ela o bloco em interação
            if (!NearCell(ant, destination))
            {
                WalkToCell(ant, destination, _nurseryField, _grid.IsNurseryRoom(ant.X, ant.Y));
                return;
            }
            // O destino foi escolhido vazio, mas no caminho outra cria pode ter sido largada nele (um ovo recém-trazido,
            // por exemplo): com um ponto menos cheio, vai para ele em vez de empilhar as duas.
            int better = PickMoveDestination(cria.Slot);
            if (better >= 0 && better != ant.DigX && _brood.PlacedAt(better) < _brood.PlacedAt(ant.DigX))
            {
                ant.DigX = better;
                WalkToCell(ant, _nurserySpots[better], _nurseryField, _grid.IsNurseryRoom(ant.X, ant.Y));
                return;
            }
            _brood.EndMove(ant.EggId, ant.DigX);
            _nurseryDirty = true;
            ant.LastBlock = ant.EggId;
            ant.State = AntState.Tending;
            ant.EggId = 0;
            ant.Interacting = false;
            ant.Cooldown = CarePause(ant);
        }

        // ---------------------------------------------------------------- sala de nutrientes (cultivo)

        /// <summary>Canteiros que o papel cobre: blocos (B) do design vezes as células de um B no protótipo.</summary>
        int GardenCapacity(Ant ant) => Tree.GardenBlocks(ant.Role) * Mathf.Max(1, _settings.CellsPerAdultSlot);

        /// <summary>
        /// Ritmo dos canteiros que brotam sem ninguém em interação: a parte que cabe na cobertura de quem está na sala
        /// avança no ritmo normal e o resto fica parado; sem ninguém na sala, nada brota.
        /// </summary>
        float GardenCureRate()
        {
            int total = _garden.Blocks;
            if (total == 0 || _garden.CapacityInside <= 0)
                return 0f;
            return Mathf.Min(1f, _garden.CapacityInside / (float)total);
        }

        /// <summary>O ritmo da jardineira vale só para o canteiro que ela toca.</summary>
        void ApplyGardenBoosts(float deltaTime)
        {
            foreach (Ant ant in _ants)
            {
                if (ant.State != AntState.Gardening || !ant.Interacting || ant.EggId == 0)
                    continue;
                float rate = Tree.GardenRate(ant.Role);
                if (rate > 1f)
                    GrownNutrients += _grid.ShaveBedGrowth(ant.TargetX, ant.TargetY, deltaTime * (rate - 1f));
            }
        }

        /// <summary>Canteiro brotando que outra formiga ainda não escolheu (e não foi o último cuidado por esta): canteiros variados.</summary>
        bool PickGardenTarget(Ant ant, out int x, out int y)
        {
            int count = _grid.BedCount;
            x = y = 0;
            for (int pass = 0; pass < 3; pass++)
            {
                int seen = 0;
                for (int k = 0; k < count; k++)
                {
                    if (!_grid.TryGetBed(k, out int bx, out int by))
                        break;
                    if (_grid.GetBedStage(bx, by) != BedStage.Growing)
                        continue;
                    int key = _grid.ToIndex(bx, by) + 1;
                    if (pass < 2 && _gardenTargets.Contains(key) || pass < 1 && key == ant.LastBlock)
                        continue;
                    if (_rng.Next(++seen) == 0)
                    {
                        x = bx;
                        y = by;
                    }
                }
                if (seen > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Cuidando da sala: escolhe um canteiro que brota (variando), vai até ele e mexe nele por um instante (o trabalho
        /// visível), depois passa a outro. Sem canteiro brotando, ou com formigas demais para a região, sai e volta a
        /// procurar outra tarefa.
        /// </summary>
        void UpdateGardening(Ant ant)
        {
            if (_garden.Blocks == 0 || IsSurplus(ant, _garden) || _curingField.Get(ant.X, ant.Y) == FlowField.Unreachable)
            {
                Leave(ant, ref _garden);
                return;
            }

            // Terminou de mexer no canteiro (ou ele brotou, ou se desfez): passa a outro.
            if (ant.EggId != 0 && (ant.Interacting || _grid.GetBedStage(ant.TargetX, ant.TargetY) != BedStage.Growing))
            {
                if (ant.Interacting)
                    ant.LastBlock = ant.EggId;
                ant.EggId = 0;
                ant.Interacting = false;
            }
            if (ant.EggId == 0)
            {
                if (!PickGardenTarget(ant, out int px, out int py))
                {
                    Leave(ant, ref _garden);
                    return;
                }
                ant.TargetX = px;
                ant.TargetY = py;
                ant.EggId = _grid.ToIndex(px, py) + 1;
                _gardenTargets.Add(ant.EggId);
            }

            var cell = new Vector2Int(ant.TargetX, ant.TargetY);
            if (!NearCell(ant, cell))
            {
                WalkToCell(ant, cell, _curingField, _grid.IsNutrientRoom(ant.X, ant.Y));
                return;
            }

            // Ao lado do canteiro: cuida dele por um instante (só ele recebe o ritmo da especialista).
            ant.Interacting = true;
            if (cell.x != ant.X)
                ant.FacingX = cell.x > ant.X ? 1 : -1;
            ant.Cooldown = CarePause(ant);
        }
    }
}
