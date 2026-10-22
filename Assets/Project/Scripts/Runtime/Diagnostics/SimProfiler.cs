using System.Diagnostics;

namespace Fabula.Incrementant.Diagnostics
{
    /// <summary>Etapas medidas por frame. A ordem é a de exibição no painel (F11); as filhas ficam depois da mãe.</summary>
    public enum ProfileStage
    {
        Rain,
        Water,
        Vegetation,
        Granular,
        ColonyTick,
        TickPrep,
        Fields,
        FieldWalkability,
        FieldQueenRoom,
        FieldDump,
        FieldHome,
        FieldNursery,
        FieldNutrients,
        FieldDig,
        FieldGarden,
        FieldConstruction,
        FieldRaid,
        Queen,
        Brood,
        Ants,
        Termites,
        RenderTerrain,
        RenderAnts,
        RenderBrood,
        RenderScenery,
        Count,
    }

    /// <summary>Contadores por segundo (eventos, não tempo).</summary>
    public enum ProfileCounter
    {
        Ticks,
        FieldRebuilds,
        /// <summary>Campos refeitos do zero (e não corrigidos aos poucos) por segundo.</summary>
        FieldFullBuilds,
        // Por que as rodadas de campos aconteceram (uma rodada pode ter várias causas).
        CauseUnderground,
        CauseSurface,
        CauseNearMarks,
        CauseBuild,
        CauseMarks,
        CauseQueen,
        CauseNutrients,
        CauseHarvest,
        CauseClaims,
        CauseGarden,
        CauseNursery,
        CauseRaid,
        Count,
    }

    /// <summary>Um passo de formiga que passou de <see cref="SlowStepLog.ThresholdMs"/>: quem era e o que fazia.</summary>
    public struct SlowStep
    {
        public float Ms;
        public int Before, After, Role, Task, Second, Mark, X, Y;
        public float SimTime;
    }

    /// <summary>Últimos passos de formiga lentos (anel, sem alocação). <c>Mark</c> diz até onde da decisão o passo chegou.</summary>
    public sealed class SlowStepLog
    {
        public const float ThresholdMs = 3f;
        public const int Size = 8;
        readonly SlowStep[] _ring = new SlowStep[Size];
        public int Total { get; private set; }

        public void Add(in SlowStep step) => _ring[Total++ % Size] = step;

        /// <summary>0 = o mais recente.</summary>
        public bool TryGet(int back, out SlowStep step)
        {
            step = default;
            if (back >= Size || back >= Total)
                return false;
            step = _ring[(Total - 1 - back) % Size];
            return true;
        }
    }

    /// <summary>
    /// Cronômetro leve por etapa: soma o tempo de cada etapa dentro do frame (vários ticks somam), e a cada
    /// <see cref="EndFrame"/> atualiza a média suavizada e o pico da janela de <see cref="PeakWindowSeconds"/>.
    /// Sem alocação; um <see cref="Stopwatch.GetTimestamp"/> por medida.
    /// </summary>
    public sealed class SimProfiler
    {
        public const float PeakWindowSeconds = 2f;
        const int StageCount = (int)ProfileStage.Count;
        const int CounterCount = (int)ProfileCounter.Count;
        static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;

        readonly long[] _frame = new long[StageCount];
        readonly float[] _average = new float[StageCount];
        readonly float[] _windowPeak = new float[StageCount];
        readonly float[] _peak = new float[StageCount];
        readonly int[] _counterAccum = new int[CounterCount];
        readonly float[] _counterRate = new float[CounterCount];
        float _windowTime;

        /// <summary>Quantos estados de formiga a medição por estado comporta (o enum <c>AntState</c> cabe com folga).</summary>
        public const int StateSlots = 32;
        // Tempo gasto em UpdateAnt, por estado da formiga ANTES do passo (quem decide o que fazer conta em Idle).
        readonly long[] _stateFrame = new long[StateSlots];
        readonly float[] _stateAverage = new float[StateSlots];
        readonly float[] _stateWindowPeak = new float[StateSlots];
        readonly float[] _statePeak = new float[StateSlots];

        public static long Now() => Stopwatch.GetTimestamp();

        public static float ToMs(long ticks) => (float)(ticks * TicksToMs);

        /// <summary>Soma ao frame o tempo desde <paramref name="start"/> (de <see cref="Now"/>) na etapa.</summary>
        public void Add(ProfileStage stage, long start) => _frame[(int)stage] += Stopwatch.GetTimestamp() - start;

        public void Count(ProfileCounter counter) => _counterAccum[(int)counter]++;

        /// <summary>Soma ao frame o tempo desde <paramref name="start"/> no estado de formiga <paramref name="state"/>.</summary>
        public void AddState(int state, long start) => _stateFrame[state] += Stopwatch.GetTimestamp() - start;

        /// <summary>Tempo médio (ms por frame) gasto em passos de formigas que estavam no estado.</summary>
        public float StateAverageMs(int state) => _stateAverage[state];

        /// <summary>Maior tempo (ms num frame) do estado na última janela de <see cref="PeakWindowSeconds"/>.</summary>
        public float StatePeakMs(int state) => _statePeak[state];

        /// <summary>Fecha o frame: <paramref name="deltaSeconds"/> é o tempo real (sem escala) que ele durou.</summary>
        public void EndFrame(float deltaSeconds)
        {
            _windowTime += deltaSeconds;
            bool rollWindow = _windowTime >= PeakWindowSeconds;
            for (int i = 0; i < StageCount; i++)
            {
                float ms = (float)(_frame[i] * TicksToMs);
                _frame[i] = 0;
                _average[i] += (ms - _average[i]) * 0.1f;
                if (ms > _windowPeak[i])
                    _windowPeak[i] = ms;
                if (!rollWindow)
                    continue;
                _peak[i] = _windowPeak[i];
                _windowPeak[i] = 0f;
            }
            for (int i = 0; i < StateSlots; i++)
            {
                float ms = (float)(_stateFrame[i] * TicksToMs);
                _stateFrame[i] = 0;
                _stateAverage[i] += (ms - _stateAverage[i]) * 0.1f;
                if (ms > _stateWindowPeak[i])
                    _stateWindowPeak[i] = ms;
                if (!rollWindow)
                    continue;
                _statePeak[i] = _stateWindowPeak[i];
                _stateWindowPeak[i] = 0f;
            }
            if (!rollWindow)
                return;
            for (int i = 0; i < CounterCount; i++)
            {
                _counterRate[i] = _counterAccum[i] / _windowTime;
                _counterAccum[i] = 0;
            }
            _windowTime = 0f;
        }

        /// <summary>Tempo médio (ms por frame) da etapa.</summary>
        public float AverageMs(ProfileStage stage) => _average[(int)stage];

        /// <summary>Maior tempo (ms num frame) da etapa na última janela de <see cref="PeakWindowSeconds"/>.</summary>
        public float PeakMs(ProfileStage stage) => _peak[(int)stage];

        /// <summary>Eventos por segundo na última janela.</summary>
        public float RatePerSecond(ProfileCounter counter) => _counterRate[(int)counter];
    }
}
