using System;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Índice espacial de pontos (formigas ou cupins) por célula, refeito a cada uso: as chaves (célula, índice) são
    /// ordenadas, então os pontos de uma célula ficam juntos e em ordem crescente de índice. Troca as buscas "quem está
    /// ao alcance" de O(formigas x cupins) por buscas binárias nas linhas em volta. Como o primeiro ponto de cada célula é
    /// o de menor índice, quem só quer o mais perto (desempate pelo índice, como na varredura da lista) olha um ponto por
    /// célula, e o custo não cresce com a multidão parada em cima do cupim (centenas de formigas na mesma célula).
    /// Pontos fora da grade ficam à parte (<see cref="Outside"/>) e quem consulta os confere um a um.
    /// </summary>
    sealed class PointIndex
    {
        const long OutsideCell = -1;

        int _width, _height, _count;
        long[] _keys = Array.Empty<long>();

        /// <summary>Há um índice montado e ainda válido (quem consulta cai na busca linear quando não há).</summary>
        public bool Valid { get; private set; }

        /// <summary>Começa um índice de <paramref name="count"/> pontos numa grade <paramref name="width"/> x <paramref name="height"/>.</summary>
        public void Begin(int width, int height, int count)
        {
            _width = width;
            _height = height;
            _count = count;
            if (_keys.Length < count)
                _keys = new long[Math.Max(count, _keys.Length * 2)];
            Valid = false;
        }

        /// <summary>Posição do ponto <paramref name="index"/> (todos de 0 a count-1 precisam ser informados antes de <see cref="Finish"/>).</summary>
        public void Set(int index, int x, int y)
        {
            long cell = x >= 0 && x < _width && y >= 0 && y < _height ? (long)y * _width + x : OutsideCell;
            _keys[index] = (cell << 32) | (uint)index;
        }

        public void Finish()
        {
            Array.Sort(_keys, 0, _count);
            Valid = true;
        }

        public void Invalidate() => Valid = false;

        /// <summary>Faixa dos pontos fora da grade (normalmente vazia).</summary>
        public void Outside(out int from, out int to)
        {
            from = 0;
            to = Lower(0L, 0, _count);
        }

        /// <summary>
        /// Faixa dos pontos da linha <paramref name="y"/> entre as colunas <paramref name="x0"/> e <paramref name="x1"/>
        /// (cortadas na grade), agrupados por célula da esquerda para a direita.
        /// </summary>
        public void Row(int y, int x0, int x1, out int from, out int to)
        {
            x0 = Math.Max(0, x0);
            x1 = Math.Min(_width - 1, x1);
            if (y < 0 || y >= _height || x0 > x1)
            {
                from = to = 0;
                return;
            }
            long row = (long)y * _width;
            from = Lower((row + x0) << 32, 0, _count);
            to = Lower((row + x1 + 1) << 32, from, _count);
        }

        /// <summary>Primeira posição depois de todos os pontos da célula de <paramref name="slot"/> (até <paramref name="to"/>).</summary>
        public int NextCell(int slot, int to) => Lower(((_keys[slot] >> 32) + 1) << 32, slot + 1, to);

        public int Item(int slot) => (int)(_keys[slot] & 0xFFFFFFFFL);

        /// <summary>Primeira posição em [lo, hi) com chave >= <paramref name="key"/>.</summary>
        int Lower(long key, int lo, int hi)
        {
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (_keys[mid] < key)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }
    }
}
