using System;
using Fabula.Incrementant.World;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Cache de <see cref="AntNavigation.IsWalkable"/> para a grade inteira, usado pelo BFS dos campos de navegação.
    /// <see cref="Refresh"/> recalcula só os blocos cuja versão mudou (<see cref="PixelGrid.GetChunkVersion"/>); depois
    /// dele o cache é igual à regra exata, célula a célula.
    /// </summary>
    public sealed class WalkabilityMap
    {
        readonly bool[] _walkable;
        readonly int[] _builtVersion;

        public PixelGrid Grid { get; }

        public WalkabilityMap(PixelGrid grid)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _walkable = new bool[grid.Width * grid.Height];
            _builtVersion = new int[grid.ChunkColumns * grid.ChunkRows];
            Array.Fill(_builtVersion, -1);
        }

        public bool IsWalkableAt(int index) => _walkable[index];

        public bool IsWalkable(int x, int y) =>
            (uint)x < (uint)Grid.Width && (uint)y < (uint)Grid.Height && _walkable[y * Grid.Width + x];

        public void Refresh()
        {
            PixelGrid grid = Grid;
            int w = grid.Width;
            for (int cy = 0; cy < grid.ChunkRows; cy++)
            for (int cx = 0; cx < grid.ChunkColumns; cx++)
            {
                int version = grid.GetChunkVersion(cx, cy);
                int c = cy * grid.ChunkColumns + cx;
                if (_builtVersion[c] == version)
                    continue;
                _builtVersion[c] = version;

                int x0 = cx << PixelGrid.ChunkShift;
                int y0 = cy << PixelGrid.ChunkShift;
                int x1 = Math.Min(w, x0 + PixelGrid.ChunkSize);
                int y1 = Math.Min(grid.Height, y0 + PixelGrid.ChunkSize);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    _walkable[y * w + x] = AntNavigation.IsWalkable(grid, x, y);
            }
        }
    }
}
