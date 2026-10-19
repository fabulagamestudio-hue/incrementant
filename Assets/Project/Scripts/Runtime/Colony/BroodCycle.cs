using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>Etapa de uma cria. Ovo na postura; larva e pupa no berçário.</summary>
    public enum BroodStage : byte
    {
        Egg = 0,
        Larva = 1,
        Pupa = 2,
    }

    /// <summary>Por que a rainha está (ou não) iniciando um novo ovo, na ordem em que é conferido (seção 11 do design).</summary>
    public enum BreedingState : byte
    {
        /// <summary>O próximo ovo tem tipo, alimento e espaço possíveis.</summary>
        Available = 0,
        /// <summary>O saldo que passa da reserva mínima ainda não cobre a cria.</summary>
        WaitingResources = 1,
        /// <summary>Falta berçário funcional ou vaga nele.</summary>
        WaitingNursery = 2,
        /// <summary>Falta capacidade adulta futura (área escavada acessível).</summary>
        WaitingExpansion = 3,
        /// <summary>Escolha explícita do jogador: sem novas posturas.</summary>
        Paused = 4,
        /// <summary>A rainha só põe ovos na sala dela e ainda não está lá (sem sala ou a caminho).</summary>
        WaitingQueenRoom = 5,
    }

    /// <summary>
    /// Onde o ovo está até começar a se desenvolver: a rainha o põe onde está, uma operária o busca e o leva ao berçário.
    /// <see cref="Placed"/> é o zero de propósito: registro novo ou vindo do save já está no berçário.
    /// </summary>
    public enum EggPhase : byte
    {
        Placed = 0,
        /// <summary>No chão, onde a rainha o pôs, esperando uma formiga.</summary>
        Laid = 1,
        /// <summary>Uma formiga já foi buscá-lo.</summary>
        Claimed = 2,
        /// <summary>Na boca da formiga, a caminho do berçário.</summary>
        Carried = 3,
        /// <summary>Já no berçário, na boca de uma cuidadora que a muda de ponto: não se desenvolve nesse intervalo.</summary>
        Moving = 4,
    }

    /// <summary>
    /// Uma cria com que uma formiga interage agora (carrega ou toca): só ela avança no ritmo <see cref="Rate"/> da
    /// formiga (cuidadora 4 crias em 3 s = 4/3); as demais cobertas avançam no ritmo normal.
    /// </summary>
    public struct BlockBoost
    {
        public int Id;
        public float Rate;
    }

    /// <summary>Uma cria reservada: o papel que a adulta terá, a etapa e quanto falta dela.</summary>
    public struct BroodRecord
    {
        public AntRole Role;
        public BroodStage Stage;
        public float Remaining;

        // Só de execução (não vão para o save; <see cref="BroodCycle.Restore"/> as reatribui, tudo já no berçário).
        /// <summary>Identifica a cria para a formiga que a transporta (a lista muda de índice quando alguém nasce).</summary>
        public int Id;
        public EggPhase Phase;
        /// <summary>Onde o ovo foi posto (ou largado) enquanto não está no berçário.</summary>
        public int LaidX, LaidY;
        /// <summary>Ponto de <see cref="AntColony.NurserySpots"/> onde a cria está (índice); -1 antes de chegar ao berçário.</summary>
        public int Slot;
    }

    /// <summary>O que o ciclo precisa saber da colônia a cada passo.</summary>
    public struct BroodContext
    {
        public QueenMode Mode;
        /// <summary>Papel do próximo ovo (já resolvido pela política) e seu custo em nutrientes.</summary>
        public AntRole Role;
        public int Cost;
        public int Nutrients;
        public int Adults;
        public int AdultCapacity;
        public int NurserySlots;
        /// <summary>Célula da rainha: onde o ovo novo é posto.</summary>
        public int QueenX, QueenY;
        /// <summary>A rainha está fora da sala dela: não pode pôr ovos (o padrão, false, não impede).</summary>
        public bool QueenOutsideRoom;
        /// <summary>
        /// Resolvem o papel e o custo dos ovos seguintes de um lote (a partir do segundo): a colônia recalcula depois de cada
        /// ovo posto. Nulos, só o primeiro ovo (<see cref="Role"/> e <see cref="Cost"/>) é posto.
        /// </summary>
        public Func<AntRole> RoleProvider;
        public Func<AntRole, int> CostProvider;
        /// <summary>
        /// Quantas crias as formigas que trabalham dentro do berçário cobrem. A cria só avança se coberta; nulo = sem
        /// exigência de cuidado (só os testes do ciclo).
        /// </summary>
        public int? CareCapacity;
        /// <summary>Crias com que uma formiga interage agora; elas são cobertas primeiro e avançam no ritmo da formiga.</summary>
        public IReadOnlyList<BlockBoost> Boosts;
    }

    /// <summary>
    /// Ciclo de criação (seções 11, 12 e 19 do design): a rainha reserva vaga e alimento, põe um ovo a cada intervalo de
    /// postura, e a cria passa por ovo, larva (no berçário) e pupa até nascer adulta do papel escolhido. Não conhece o
    /// mapa: a colônia informa vagas e capacidade; aqui só há as regras e o relógio.
    /// </summary>
    public sealed class BroodCycle
    {
        readonly ColonySettings _settings;
        readonly List<BroodRecord> _brood = new List<BroodRecord>();
        float _layTimer;
        int _lastId;

        /// <summary>Multiplica o intervalo de postura (a árvore da rainha, Q1).</summary>
        public float LayIntervalFactor { get; set; } = 1f;

        /// <summary>Ovos por ciclo (a árvore da rainha, Q2): lote parcial se faltar alimento, vaga ou capacidade.</summary>
        public int EggsPerLay { get; set; } = 1;

        float LayInterval => _settings.LayIntervalSeconds * LayIntervalFactor;

        public BroodCycle(ColonySettings settings)
        {
            _settings = settings;
            _layTimer = settings.LayIntervalSeconds;
        }

        public IReadOnlyList<BroodRecord> Brood => _brood;
        public BreedingState State { get; private set; }

        /// <summary>Segundos até a próxima postura (0 = pronta, esperando condições).</summary>
        public float SecondsToNextLay => _layTimer;

        /// <summary>Quanto do intervalo até o próximo ovo já passou (0 a 1); 1 = pronto, esperando condições.</summary>
        public float LayProgress => Mathf.Clamp01(1f - _layTimer / Mathf.Max(0.01f, LayInterval));

        public int Count => _brood.Count;
        public int Eggs => CountOf(BroodStage.Egg);
        public int Larvae => CountOf(BroodStage.Larva);
        public int Pupae => CountOf(BroodStage.Pupa);

        int CountOf(BroodStage stage)
        {
            int n = 0;
            foreach (BroodRecord b in _brood)
                n += b.Stage == stage ? 1 : 0;
            return n;
        }

        /// <summary>Crias reservadas que vão virar adultas do papel dado.</summary>
        public int CountOf(AntRole role)
        {
            int n = 0;
            foreach (BroodRecord b in _brood)
                n += b.Role == role ? 1 : 0;
            return n;
        }

        /// <summary>Estado de reprodução para o contexto dado, sem avançar o tempo.</summary>
        public BreedingState Evaluate(in BroodContext c)
        {
            if (c.Mode == QueenMode.Paused)
                return BreedingState.Paused;
            if (c.QueenOutsideRoom)
                return BreedingState.WaitingQueenRoom;
            if (c.NurserySlots <= 0 || _brood.Count >= c.NurserySlots)
                return BreedingState.WaitingNursery;
            if (c.Adults + _brood.Count >= c.AdultCapacity)
                return BreedingState.WaitingExpansion;
            // Rainha sozinha (sem adultas nem crias): bota o ovo que recomeça a colônia sem respeitar a reserva mínima,
            // só precisa pagar o custo (pedido do desenvolvedor em 2026-10-18).
            bool loneQueen = c.Adults == 0 && _brood.Count == 0;
            int reserve = loneQueen ? 0 : _settings.MinNutrientReserve;
            if (c.Nutrients - c.Cost < reserve)
                return BreedingState.WaitingResources;
            return BreedingState.Available;
        }

        /// <summary>
        /// Avança a cria e a postura. Papéis que nasceram neste passo vão para <paramref name="born"/>; devolve os nutrientes
        /// gastos com um ovo novo (0 se não pôs).
        /// </summary>
        public int Tick(float deltaTime, in BroodContext c, List<AntRole> born)
        {
            int bornBefore = born.Count;
            AdvanceBrood(deltaTime, c.NurserySlots > 0, born, c.CareCapacity, c.Boosts);

            // Quem nasceu neste passo já saiu da lista de crias, mas a colônia só o cria como adulto depois deste Tick:
            // conta como adulto aqui, senão a vaga seria liberada duas vezes e a capacidade estouraria em 1.
            BroodContext afterBirths = c;
            afterBirths.Adults += born.Count - bornBefore;

            _layTimer = Mathf.Max(0f, _layTimer - deltaTime);
            State = Evaluate(afterBirths);
            if (State != BreedingState.Available || _layTimer > 0f)
                return 0;

            // Cada ovo do lote paga seu custo e reserva sua vaga; o lote sai parcial se algum ovo deixar de caber.
            // O ovo já reserva a vaga do berçário, mas só começa a contar depois de levado até lá.
            int spent = 0;
            AntRole role = c.Role;
            int cost = c.Cost;
            for (int n = 0; n < Mathf.Max(1, EggsPerLay); n++)
            {
                if (n > 0)
                {
                    if (c.RoleProvider == null || c.CostProvider == null)
                        break;
                    role = c.RoleProvider();
                    cost = c.CostProvider(role);
                    BroodContext next = afterBirths;
                    next.Role = role;
                    next.Cost = cost;
                    next.Nutrients = c.Nutrients - spent;
                    if (Evaluate(next) != BreedingState.Available)
                        break;
                }
                _brood.Add(new BroodRecord
                {
                    Role = role, Stage = BroodStage.Egg, Remaining = _settings.EggSeconds,
                    Id = ++_lastId, Phase = EggPhase.Laid, LaidX = c.QueenX, LaidY = c.QueenY, Slot = -1,
                });
                spent += cost;
            }
            _layTimer = LayInterval;
            return spent;
        }

        /// <summary>
        /// Põe um ovo agora (clique na rainha): ignora o relógio de postura e a reserva mínima de nutrientes, mas respeita
        /// pausa, vaga no berçário, capacidade adulta e o custo da cria. Não mexe no relógio. Devolve o custo gasto (0 = não pôs).
        /// </summary>
        public int ForceLay(in BroodContext c)
        {
            if (c.Mode == QueenMode.Paused || c.QueenOutsideRoom || c.NurserySlots <= 0 || _brood.Count >= c.NurserySlots)
                return 0;
            if (c.Adults + _brood.Count >= c.AdultCapacity || c.Nutrients < c.Cost)
                return 0;
            _brood.Add(new BroodRecord
            {
                Role = c.Role, Stage = BroodStage.Egg, Remaining = _settings.EggSeconds,
                Id = ++_lastId, Phase = EggPhase.Laid, LaidX = c.QueenX, LaidY = c.QueenY, Slot = -1,
            });
            return c.Cost;
        }

        // ---------------------------------------------------------------- transporte do ovo

        /// <summary>Há ovo no chão sem ninguém a caminho.</summary>
        public bool HasEggToCarry
        {
            get
            {
                foreach (BroodRecord b in _brood)
                {
                    if (b.Phase == EggPhase.Laid)
                        return true;
                }
                return false;
            }
        }

        /// <summary>Reserva o ovo mais antigo que está no chão para uma formiga.</summary>
        public bool TryClaimEgg(out int id)
        {
            for (int i = 0; i < _brood.Count; i++)
            {
                if (_brood[i].Phase != EggPhase.Laid)
                    continue;
                SetPhase(i, EggPhase.Claimed);
                id = _brood[i].Id;
                return true;
            }
            id = 0;
            return false;
        }

        public bool TryGet(int id, out BroodRecord record)
        {
            int i = IndexOf(id);
            record = i >= 0 ? _brood[i] : default;
            return i >= 0;
        }

        public void PickUpEgg(int id) => SetPhase(IndexOf(id), EggPhase.Carried);

        /// <summary>A cria foi destruída (cupins): sai da lista e libera a vaga do berçário e a adulta reservada. O alimento não volta.</summary>
        public bool Remove(int id)
        {
            int i = IndexOf(id);
            if (i < 0)
                return false;
            _brood.RemoveAt(i);
            return true;
        }

        /// <summary>O ovo chegou ao ponto <paramref name="slot"/> do berçário e passa a se desenvolver.</summary>
        public void PlaceEgg(int id, int slot)
        {
            int i = IndexOf(id);
            if (i < 0)
                return;
            BroodRecord b = _brood[i];
            b.Phase = EggPhase.Placed;
            b.Slot = slot;
            _brood[i] = b;
        }

        /// <summary>A formiga não vai levar o ovo (morreu): ele volta ao chão, onde ela estava, para outra pegar.</summary>
        public void ReleaseEgg(int id, int x, int y)
        {
            int i = IndexOf(id);
            if (i < 0 || _brood[i].Phase == EggPhase.Placed)
                return;
            BroodRecord b = _brood[i];
            if (b.Phase == EggPhase.Moving)
            {
                // Cria que a cuidadora carregava: volta ao ponto onde estava, já no berçário.
                b.Phase = EggPhase.Placed;
                _brood[i] = b;
                return;
            }
            if (b.Phase == EggPhase.Carried)
            {
                b.LaidX = x;
                b.LaidY = y;
            }
            b.Phase = EggPhase.Laid;
            _brood[i] = b;
        }

        float[] _rates = new float[16];

        /// <summary>Crias já no berçário (as que a cuidadora carrega não contam), que pedem cuidado.</summary>
        public int PlacedCount
        {
            get
            {
                int n = 0;
                foreach (BroodRecord b in _brood)
                    n += b.Phase == EggPhase.Placed ? 1 : 0;
                return n;
            }
        }

        /// <summary>Crias que pedem cuidado: no berçário ou nas mãos de quem as muda de ponto.</summary>
        public int CaredCount
        {
            get
            {
                int n = 0;
                foreach (BroodRecord b in _brood)
                    n += b.Phase == EggPhase.Placed || b.Phase == EggPhase.Moving ? 1 : 0;
                return n;
            }
        }

        /// <summary>Uma cria do berçário sorteada por <paramref name="pick"/> (0 a <see cref="PlacedCount"/>-1); falso se não há.</summary>
        public bool TryGetPlaced(int pick, out BroodRecord record)
        {
            foreach (BroodRecord b in _brood)
            {
                if (b.Phase != EggPhase.Placed)
                    continue;
                if (pick-- == 0)
                {
                    record = b;
                    return true;
                }
            }
            record = default;
            return false;
        }

        /// <summary>A cuidadora pegou a cria para mudá-la de ponto.</summary>
        public void BeginMove(int id) => SetPhase(IndexOf(id), EggPhase.Moving);

        /// <summary>A cuidadora largou a cria no ponto <paramref name="slot"/>.</summary>
        public void EndMove(int id, int slot)
        {
            int i = IndexOf(id);
            if (i < 0)
                return;
            BroodRecord b = _brood[i];
            b.Phase = EggPhase.Placed;
            b.Slot = slot;
            _brood[i] = b;
        }

        /// <summary>Crias já no berçário que ocupam o ponto <paramref name="slot"/>.</summary>
        public int PlacedAt(int slot)
        {
            int n = 0;
            foreach (BroodRecord b in _brood)
                n += b.Phase == EggPhase.Placed && b.Slot == slot ? 1 : 0;
            return n;
        }

        int IndexOf(int id)
        {
            for (int i = 0; i < _brood.Count; i++)
            {
                if (_brood[i].Id == id)
                    return i;
            }
            return -1;
        }

        void SetPhase(int index, EggPhase phase)
        {
            if (index < 0)
                return;
            BroodRecord b = _brood[index];
            b.Phase = phase;
            _brood[index] = b;
        }

        /// <summary>
        /// Larva e pupa só se desenvolvem com berçário acessível (a transferência espera; a cria não some nem atravessa o
        /// solo). Um ovo pronto espera a vaga para virar larva.
        /// </summary>
        void AdvanceBrood(float deltaTime, bool nurseryOpen, List<AntRole> born, int? careCapacity, IReadOnlyList<BlockBoost> boosts)
        {
            // Quem avança e a que ritmo: as crias em interação primeiro (no ritmo da formiga), depois as demais já no
            // berçário, na ordem da lista, até a cobertura acabar. O percurso é do fim para o começo (a lista perde as
            // que nascem), então os ritmos são decididos antes.
            if (_rates.Length < _brood.Count)
                _rates = new float[Mathf.Max(_brood.Count, _rates.Length * 2)];
            for (int i = 0; i < _brood.Count; i++)
                _rates[i] = -1f;
            int left = careCapacity ?? int.MaxValue;
            if (boosts != null)
            {
                foreach (BlockBoost boost in boosts)
                {
                    int index = IndexOf(boost.Id);
                    if (index < 0 || _brood[index].Phase != EggPhase.Placed && _brood[index].Phase != EggPhase.Moving)
                        continue;
                    _rates[index] = boost.Rate;
                    left--;
                }
            }
            for (int i = 0; i < _brood.Count; i++)
            {
                if (_rates[i] >= 0f)
                    continue;
                EggPhase phase = _brood[i].Phase;
                if (phase == EggPhase.Moving)
                    _rates[i] = 1f; // a que uma formiga carrega avança no ritmo normal, sem ocupar a cobertura
                else if (phase == EggPhase.Placed && left > 0)
                {
                    _rates[i] = 1f;
                    left--;
                }
                else
                    _rates[i] = 0f;
            }

            for (int i = _brood.Count - 1; i >= 0; i--)
            {
                BroodRecord b = _brood[i];
                // Ovo no chão ou no caminho ainda não se desenvolve: só conta depois de chegar ao berçário.
                if (b.Phase != EggPhase.Placed && b.Phase != EggPhase.Moving || b.Stage != BroodStage.Egg && !nurseryOpen)
                    continue;
                if (_rates[i] <= 0f)
                    continue; // ninguém atende esta cria agora
                b.Remaining -= deltaTime * _rates[i];
                if (b.Remaining <= 0f)
                {
                    if (b.Stage == BroodStage.Egg && !nurseryOpen)
                    {
                        b.Remaining = 0f;
                    }
                    else if (b.Stage == BroodStage.Pupa)
                    {
                        born.Add(b.Role);
                        _brood.RemoveAt(i);
                        continue;
                    }
                    else
                    {
                        b.Stage = b.Stage == BroodStage.Egg ? BroodStage.Larva : BroodStage.Pupa;
                        b.Remaining = b.Stage == BroodStage.Larva ? _settings.LarvaSeconds : _settings.PupaSeconds;
                    }
                }
                _brood[i] = b;
            }
        }

        /// <summary>Substitui a cria e a postura por as de um save.</summary>
        public void Restore(IEnumerable<BroodRecord> brood, float layTimer)
        {
            _brood.Clear();
            _brood.AddRange(brood);
            // O save não guarda o transporte nem o ponto no berçário: tudo volta já no berçário, repartido na ordem da lista.
            for (int i = 0; i < _brood.Count; i++)
            {
                BroodRecord b = _brood[i];
                b.Id = ++_lastId;
                b.Phase = EggPhase.Placed;
                b.Slot = i;
                _brood[i] = b;
            }
            _layTimer = Mathf.Max(0f, layTimer);
        }
    }
}
