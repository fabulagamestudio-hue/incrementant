using System;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Seção de trabalho da colônia que o jogador liga, desliga e limita no painel da colônia (pedido do desenvolvedor em
    /// 2026-10-19). Valores gravados no save, na ordem: não renumerar; seção nova entra no fim.
    /// </summary>
    public enum WorkSection : byte
    {
        /// <summary>Cavar e retirar a terra marcada (inclui quebrar rocha).</summary>
        Dig = 0,
        /// <summary>Buscar terra na pilha de fora e encaixá-la nas obras.</summary>
        Build = 1,
        /// <summary>Cortar plantas da superfície e levar os pedaços aos canteiros.</summary>
        Harvest = 2,
        /// <summary>Buscar nutriente na sala de nutrientes e levá-lo à rainha.</summary>
        QueenFeed = 3,
        /// <summary>Levar os ovos ao berçário e cuidar das crias.</summary>
        Nursery = 4,
        /// <summary>Sala de nutrientes: montar canteiros, regar e cuidar do que brota.</summary>
        Garden = 5,
        /// <summary>Sala Gotas de Orvalho: terra fofa no teto e cuidado das gotas.</summary>
        Dew = 6,
        /// <summary>Desmembrar cupins mortos e levar entulho e restos à área de resíduos.</summary>
        Waste = 7,
    }

    /// <summary>
    /// Comportamento da rainha (pedido do desenvolvedor em 2026-10-21): fica no ninho pondo ovos ou trabalha como uma das
    /// classes que ela cria (a mesma IA da formiga daquele papel). Gravado no save como 1 + <see cref="AntRole"/>: os
    /// valores antigos 1 (COLETAR RECURSOS) e 2 (COLETAR NUTRIENTE) viram operária e jardineira. Não renumerar.
    /// </summary>
    public enum QueenJob : byte
    {
        /// <summary>Fica na sala dela pondo ovos (o comportamento de sempre).</summary>
        Nest = 0,
        // 1 + AntRole.
        Worker = 1,
        Gardener = 2,
        Carrier = 3,
        Cutter = 4,
        Digger = 5,
        Nurse = 6,
        Soldier = 7,
        Spitter = 8,
        Midden = 9,
        Farmer = 10,
    }

    public static class QueenJobs
    {
        /// <summary>Opções do comportamento da rainha, na ordem do painel: ninho e depois as classes de <see cref="AntRoles.All"/>.</summary>
        public static readonly QueenJob[] All = BuildAll();

        static QueenJob[] BuildAll()
        {
            var all = new QueenJob[1 + AntRoles.All.Length];
            all[0] = QueenJob.Nest;
            for (int i = 0; i < AntRoles.All.Length; i++)
                all[i + 1] = Of(AntRoles.All[i]);
            return all;
        }

        public static QueenJob Of(AntRole role) => (QueenJob)(1 + (int)role);

        /// <summary>Papel que a rainha imita; <c>false</c> no ninho.</summary>
        public static bool TryGetRole(QueenJob job, out AntRole role)
        {
            role = job == QueenJob.Nest ? AntRole.Worker : (AntRole)((int)job - 1);
            return job != QueenJob.Nest;
        }
    }

    /// <summary>
    /// Configuração das seções de trabalho: se cada uma está ativa e quantas formigas pode ter ao mesmo tempo. Seção
    /// desligada não recebe formiga nova; quem já está nela termina o que tem nas mãos (quem só cuida, sai na hora).
    /// </summary>
    public sealed class WorkPolicy
    {
        /// <summary>Limite que significa "sem limite".</summary>
        public const int Unlimited = -1;

        public static readonly WorkSection[] All =
        {
            WorkSection.Dig, WorkSection.Build, WorkSection.Harvest, WorkSection.QueenFeed,
            WorkSection.Nursery, WorkSection.Garden, WorkSection.Dew, WorkSection.Waste,
        };

        public static int Count => All.Length;

        readonly bool[] _enabled = new bool[All.Length];
        readonly int[] _limits = new int[All.Length];

        /// <summary>Sobe a cada mudança (o painel só redesenha quando muda).</summary>
        public int Version { get; private set; }

        public WorkPolicy() => Reset();

        /// <summary>Tudo ativo e sem limite (o padrão de uma colônia nova).</summary>
        public void Reset()
        {
            for (int i = 0; i < _enabled.Length; i++)
            {
                _enabled[i] = true;
                _limits[i] = Unlimited;
            }
            Version++;
        }

        public bool IsEnabled(WorkSection section) => _enabled[(int)section];

        /// <summary>Máximo de formigas na seção; <see cref="Unlimited"/> = sem limite.</summary>
        public int LimitOf(WorkSection section) => _limits[(int)section];

        public void SetEnabled(WorkSection section, bool enabled)
        {
            if (_enabled[(int)section] == enabled)
                return;
            _enabled[(int)section] = enabled;
            Version++;
        }

        /// <param name="limit">Formigas (0 ou mais) ou <see cref="Unlimited"/>.</param>
        public void SetLimit(WorkSection section, int limit)
        {
            limit = limit < 0 ? Unlimited : limit;
            if (_limits[(int)section] == limit)
                return;
            _limits[(int)section] = limit;
            Version++;
        }

        /// <summary>A seção aceita mais uma formiga, com <paramref name="current"/> já nela.</summary>
        public bool HasRoom(WorkSection section, int current)
        {
            int limit = _limits[(int)section];
            return limit == Unlimited || current < limit;
        }

        /// <summary>Estado para o save: um byte de ativo e um int de limite por seção, na ordem de <see cref="All"/>.</summary>
        public void CopyTo(bool[] enabled, int[] limits)
        {
            Array.Copy(_enabled, enabled, _enabled.Length);
            Array.Copy(_limits, limits, _limits.Length);
        }

        /// <summary>Restaura do save; seções que o save não tem (versões antigas) ficam no padrão.</summary>
        public void Restore(bool[] enabled, int[] limits)
        {
            Reset();
            int n = Math.Min(_enabled.Length, Math.Min(enabled?.Length ?? 0, limits?.Length ?? 0));
            for (int i = 0; i < n; i++)
            {
                _enabled[i] = enabled[i];
                _limits[i] = limits[i] < 0 ? Unlimited : limits[i];
            }
            Version++;
        }
    }

    /// <summary>
    /// Ritmo de produção da colônia em unidades por segundo de jogo, medido numa janela deslizante de
    /// <see cref="WindowSeconds"/> (amostras a cada segundo simulado; não depende da velocidade nem do FPS).
    /// </summary>
    public sealed class ColonyRates
    {
        public const int WindowSeconds = 10;

        /// <summary>Contadores acumulados que viram ritmo.</summary>
        public enum Counter
        {
            /// <summary>Nutrientes que chegaram à rainha (entregues ou comidos por ela).</summary>
            NutrientsToQueen,
            /// <summary>Nutrientes que brotaram dos canteiros.</summary>
            NutrientsGrown,
            /// <summary>Pedaços de planta guardados nos canteiros.</summary>
            PlantsHarvested,
            /// <summary>Terra cavada e despejada fora.</summary>
            SoilDumped,
        }

        static readonly int CounterCount = Enum.GetValues(typeof(Counter)).Length;

        // Anel de amostras: [amostra, contador]. A amostra i foi tirada no segundo _times[i].
        readonly float[,] _samples = new float[WindowSeconds + 1, CounterCount];
        readonly float[] _times = new float[WindowSeconds + 1];
        readonly float[] _current = new float[CounterCount];
        int _head = -1;
        int _filled;
        float _nextSample;

        /// <summary>Recomeça a medição (mapa novo ou save carregado).</summary>
        public void Reset(float time)
        {
            _head = -1;
            _filled = 0;
            _nextSample = time;
        }

        /// <summary>Guarda os contadores de agora; uma amostra por segundo simulado.</summary>
        public void Sample(float time, float toQueen, float grown, float harvested, float dumped)
        {
            _current[(int)Counter.NutrientsToQueen] = toQueen;
            _current[(int)Counter.NutrientsGrown] = grown;
            _current[(int)Counter.PlantsHarvested] = harvested;
            _current[(int)Counter.SoilDumped] = dumped;
            if (time < _nextSample)
                return;
            _nextSample = time + 1f;
            _head = (_head + 1) % _times.Length;
            _times[_head] = time;
            for (int c = 0; c < CounterCount; c++)
                _samples[_head, c] = _current[c];
            if (_filled < _times.Length)
                _filled++;
        }

        /// <summary>Unidades por segundo na janela (0 até haver duas amostras).</summary>
        public float PerSecond(Counter counter)
        {
            if (_filled < 2)
                return 0f;
            int oldest = (_head - _filled + 1 + _times.Length) % _times.Length;
            float span = _times[_head] - _times[oldest];
            if (span <= 0f)
                return 0f;
            return (_samples[_head, (int)counter] - _samples[oldest, (int)counter]) / span;
        }
    }
}
