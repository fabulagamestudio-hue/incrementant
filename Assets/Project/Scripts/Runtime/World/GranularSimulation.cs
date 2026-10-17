namespace Fabula.Incrementant.World
{
    /// <summary>
    /// Queda de materiais granulares no estilo "falling sand": cai reto, senão escorrega em diagonal.
    /// Varre de baixo para cima; como só há movimento para a linha de baixo (já processada), nenhuma célula
    /// anda duas vezes no mesmo passo. A direção horizontal alterna por linha e por passo para não enviesar pilhas.
    /// Pula os blocos dormindo da grade (<see cref="PixelGrid.IsChunkAwake"/>): neles todo grão já está parado, então
    /// o resultado é idêntico ao da varredura completa e o custo acompanha só o que está caindo.
    /// </summary>
    public sealed class GranularSimulation
    {
        int _tick;

        public void Step(PixelGrid grid)
        {
            _tick++;
            // Só a área ativa: fora dela é rocha-mãe. A primeira linha não cai (embaixo dela também é limite).
            UnityEngine.RectInt area = grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift;
            int cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;

            for (int y = area.yMin + 1; y < area.yMax; y++)
            {
                int cy = y >> PixelGrid.ChunkShift;
                if (!grid.IsChunkRowAwake(cy))
                {
                    // Nada acordado nesta faixa de blocos: pula para a primeira linha da faixa de cima.
                    y |= PixelGrid.ChunkSize - 1;
                    continue;
                }
                // O estado acordado é lido ao entrar em cada bloco: um grão que sai da borda acorda o vizinho
                // ainda nesta linha, como na varredura completa.
                if (((y + _tick) & 1) == 0)
                {
                    for (int cx = cxMin; cx <= cxMax; cx++)
                    {
                        if (!grid.IsChunkAwake(cx, cy))
                            continue;
                        int x1 = System.Math.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                        for (int x = System.Math.Max(area.xMin, cx << PixelGrid.ChunkShift); x < x1; x++)
                            StepCell(grid, x, y);
                    }
                }
                else
                {
                    for (int cx = cxMax; cx >= cxMin; cx--)
                    {
                        if (!grid.IsChunkAwake(cx, cy))
                            continue;
                        int x0 = System.Math.Max(area.xMin, cx << PixelGrid.ChunkShift);
                        for (int x = System.Math.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift) - 1; x >= x0; x--)
                            StepCell(grid, x, y);
                    }
                }
            }
            grid.AdvanceChunkWake();
        }

        void StepCell(PixelGrid grid, int x, int y)
        {
            int w = grid.Width;
            int i = y * w + x;
            if (!CellMaterials.IsGranular(grid.GetAt(i)))
                return;

            int below = i - w;
            if (grid.GetAt(below) == CellMaterial.Empty)
            {
                grid.MoveCell(i, below);
                return;
            }

            int first = ((x ^ y ^ _tick) & 1) == 0 ? -1 : 1;
            if (!TryDiagonal(grid, x, y, first))
                TryDiagonal(grid, x, y, -first);
        }

        static bool TryDiagonal(PixelGrid grid, int x, int y, int dx)
        {
            int nx = x + dx;
            // Exige a lateral livre também, para o grão não atravessar quinas.
            if (grid.Get(nx, y) != CellMaterial.Empty || grid.Get(nx, y - 1) != CellMaterial.Empty)
                return false;

            grid.MoveCell(grid.ToIndex(x, y), grid.ToIndex(nx, y - 1));
            return true;
        }
    }
}
