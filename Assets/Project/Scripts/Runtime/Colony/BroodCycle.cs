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
    }

    /// <summary>Uma cria reservada: o papel que a adulta terá, a etapa e quanto falta dela.</summary>
    public struct BroodRecord
    {
        public AntRole Role;
        public BroodStage Stage;
        public float Remaining;
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

        public BroodCycle(ColonySettings settings)
        {
            _settings = settings;
            _layTimer = settings.LayIntervalSeconds;
        }

        public IReadOnlyList<BroodRecord> Brood => _brood;
        public BreedingState State { get; private set; }

        /// <summary>Segundos até a próxima postura (0 = pronta, esperando condições).</summary>
        public float SecondsToNextLay => _layTimer;

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
            if (c.NurserySlots <= 0 || _brood.Count >= c.NurserySlots)
                return BreedingState.WaitingNursery;
            if (c.Adults + _brood.Count >= c.AdultCapacity)
                return BreedingState.WaitingExpansion;
            if (c.Nutrients - c.Cost < _settings.MinNutrientReserve)
                return BreedingState.WaitingResources;
            return BreedingState.Available;
        }

        /// <summary>
        /// Avança a cria e a postura. Papéis que nasceram neste passo vão para <paramref name="born"/>; devolve os nutrientes
        /// gastos com um ovo novo (0 se não pôs).
        /// </summary>
        public int Tick(float deltaTime, in BroodContext c, List<AntRole> born)
        {
            AdvanceBrood(deltaTime, c.NurserySlots > 0, born);

            _layTimer = Mathf.Max(0f, _layTimer - deltaTime);
            State = Evaluate(c);
            if (State != BreedingState.Available || _layTimer > 0f)
                return 0;

            _brood.Add(new BroodRecord { Role = c.Role, Stage = BroodStage.Egg, Remaining = _settings.EggSeconds });
            _layTimer = _settings.LayIntervalSeconds;
            return c.Cost;
        }

        /// <summary>
        /// Larva e pupa só se desenvolvem com berçário acessível (a transferência espera; a cria não some nem atravessa o
        /// solo). Um ovo pronto espera a vaga para virar larva.
        /// </summary>
        void AdvanceBrood(float deltaTime, bool nurseryOpen, List<AntRole> born)
        {
            for (int i = _brood.Count - 1; i >= 0; i--)
            {
                BroodRecord b = _brood[i];
                if (b.Stage != BroodStage.Egg && !nurseryOpen)
                    continue;
                b.Remaining -= deltaTime;
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
            _layTimer = Mathf.Max(0f, layTimer);
        }
    }
}
