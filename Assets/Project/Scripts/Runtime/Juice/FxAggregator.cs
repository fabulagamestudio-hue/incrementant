namespace Fabula.Incrementant.Juice
{
    /// <summary>
    /// Soma eventos repetidos numa janela de tempo e entrega o total de uma vez ("+12" em vez de doze "+1").
    /// A janela abre no primeiro <see cref="Add"/> e fecha em <see cref="TryFlush"/>. Lógica pura.
    /// </summary>
    public sealed class FxAggregator
    {
        readonly float _window;
        int _total;
        float _start;
        bool _open;

        public FxAggregator(float windowSeconds) => _window = windowSeconds;

        /// <summary>Há uma janela aberta (algo somado e ainda não entregue).</summary>
        public bool Open => _open;

        public void Add(int amount, float now)
        {
            if (!_open)
            {
                _open = true;
                _start = now;
            }
            _total += amount;
        }

        /// <summary>True (com o total acumulado) quando a janela venceu; reinicia o acúmulo.</summary>
        public bool TryFlush(float now, out int total)
        {
            if (_open && now - _start >= _window)
            {
                total = _total;
                _total = 0;
                _open = false;
                return true;
            }
            total = 0;
            return false;
        }
    }
}
