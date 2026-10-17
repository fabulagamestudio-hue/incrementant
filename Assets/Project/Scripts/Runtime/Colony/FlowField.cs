using System;
using System.Collections.Generic;
using Fabula.Incrementant.World;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Regras de onde uma formiga pode ficar. Abaixo do solo original, todo vazio é caminhável: é túnel e tem
    /// o fundo do formigueiro (a parede de trás) como apoio. No céu, só células encostadas (8 vizinhos) em algo sólido.
    /// </summary>
    public static class AntNavigation
    {
        public static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        public static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

        public static bool IsWalkable(PixelGrid grid, int x, int y)
        {
            if (!grid.InBounds(x, y) || grid.IsSolid(x, y))
                return false;
            if (IsTunnelBackground(grid, x, y))
                return true;
            return TouchesSolid(grid, x, y);
        }

        /// <summary>Vazio abaixo do solo original: a formiga se apoia na parede de trás do formigueiro.</summary>
        public static bool IsTunnelBackground(PixelGrid grid, int x, int y) => y <= grid.GetSurfaceY(x);

        public static bool TouchesSolid(PixelGrid grid, int x, int y)
        {
            for (int k = 0; k < 8; k++)
            {
                if (grid.IsSolid(x + Dx[k], y + Dy[k]))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Campo de distância (BFS em 8 direções sobre células caminháveis) a partir de um conjunto de origens.
    /// Formigas descem o gradiente para chegar à origem mais próxima.
    /// </summary>
    public sealed class FlowField
    {
        public const int Unreachable = int.MaxValue;

        readonly int[] _distance;
        readonly int[] _queue;
        readonly int _width;
        readonly int _height;
        int _reached;

        public FlowField(int width, int height)
        {
            _width = width;
            _height = height;
            _distance = new int[width * height];
            _queue = new int[width * height];
            Array.Fill(_distance, Unreachable);
        }

        public int Get(int x, int y) =>
            (uint)x < (uint)_width && (uint)y < (uint)_height ? _distance[y * _width + x] : Unreachable;

        /// <summary>Monta o campo consultando a grade célula a célula. Conveniência para testes; o jogo usa o cache.</summary>
        public void Build(PixelGrid grid, List<int> sourceIndices)
        {
            var walkability = new WalkabilityMap(grid);
            walkability.Refresh();
            Build(walkability, sourceIndices);
        }

        /// <summary>Monta o campo sobre <paramref name="walkability"/>, que precisa estar atualizado (<see cref="WalkabilityMap.Refresh"/>).</summary>
        public void Build(WalkabilityMap walkability, List<int> sourceIndices)
        {
            // Toda célula com distância passou pela fila do build anterior: limpar só ela custa o alcançado, não a grade.
            for (int n = 0; n < _reached; n++)
                _distance[_queue[n]] = Unreachable;
            int head = 0;
            int tail = 0;

            foreach (int source in sourceIndices)
            {
                if (_distance[source] != Unreachable)
                    continue;
                _distance[source] = 0;
                _queue[tail++] = source;
            }

            while (head < tail)
            {
                int i = _queue[head++];
                int x = i % _width;
                int y = i / _width;
                int next = _distance[i] + 1;

                for (int k = 0; k < 8; k++)
                {
                    int nx = x + AntNavigation.Dx[k];
                    int ny = y + AntNavigation.Dy[k];
                    if ((uint)nx >= (uint)_width || (uint)ny >= (uint)_height)
                        continue;
                    int ni = ny * _width + nx;
                    if (_distance[ni] != Unreachable || !walkability.IsWalkableAt(ni))
                        continue;
                    _distance[ni] = next;
                    _queue[tail++] = ni;
                }
            }
            _reached = tail;
        }
    }
}
