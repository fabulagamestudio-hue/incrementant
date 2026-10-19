namespace Fabula.Incrementant.Juice
{
    /// <summary>
    /// Cadência mínima por tipo de efeito (tempo real): a 8x a simulação dispara dezenas de eventos por segundo e
    /// o mundo não pode virar poeira. Lógica pura.
    /// </summary>
    public sealed class FxLimiter
    {
        readonly float[] _interval;
        readonly float[] _last;

        public FxLimiter(float[] minIntervals)
        {
            _interval = minIntervals;
            _last = new float[minIntervals.Length];
            for (int i = 0; i < _last.Length; i++)
                _last[i] = float.NegativeInfinity;
        }

        /// <summary>True (e registra o disparo) se já passou o intervalo mínimo desde o último deste tipo.</summary>
        public bool TryUse(FxId id, float now)
        {
            int i = (int)id;
            if (now - _last[i] < _interval[i])
                return false;
            _last[i] = now;
            return true;
        }
    }
}
