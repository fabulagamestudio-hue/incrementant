using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Mapa de luz do terreno (etapa 6 do documento 40; substitui a névoa). A única fonte é o lado de fora: tudo acima do
    /// solo original (céu, montes, construções) tem luz cheia, <see cref="_outsideLight"/>, e fica totalmente visível. A luz
    /// cai reta pelas bocas do formigueiro, sem perder força, por todas as células livres da coluna até bater num bloco (o
    /// feixe, <see cref="_beamBottom"/>); do feixe ela se espalha pelas 8 vizinhas perdendo 1 nível por célula vazia e
    /// <see cref="_solidLightCost"/> por célula de terra. O interior longe dos feixes fica no breu (túnel no piso
    /// <see cref="_interiorAmbient"/>, terra preta). O espalhamento não passa de um bloco, então a versão do bloco e dos 8
    /// vizinhos basta para saber quando refazer; o feixe, que pode descer qualquer distância, é conferido a cada frame
    /// (<see cref="RefreshBeams"/>) e invalida os blocos em volta do trecho que mudou.
    /// </summary>
    public sealed partial class AntFarmView
    {
        /// <summary>Nível máximo de luz; é também o alcance, em células, do espalhamento a partir do feixe. Não passa de <see cref="PixelGrid.ChunkSize"/>.</summary>
        public const int LightLevels = 16;

        // Região do bloco com folga de LightLevels: terra e nível de cada célula, e as filas por nível da propagação.
        bool[] _lightSolidRegion;
        byte[] _lightRegion;
        readonly List<int>[] _lightQueues = CreateLightQueues();
        // Por coluna, a célula mais baixa do feixe de luz que desce do lado de fora (GetSurfaceY + 1 = a boca está fechada).
        int[] _beamBottom;

        static List<int>[] CreateLightQueues()
        {
            var queues = new List<int>[LightLevels + 1];
            for (int i = 0; i < queues.Length; i++)
                queues[i] = new List<int>();
            return queues;
        }

        /// <summary>Célula mais baixa do feixe da coluna: desce do solo original enquanto a célula estiver livre.</summary>
        int BeamBottom(PixelGrid grid, int x)
        {
            int y = grid.GetSurfaceY(x);
            while (y >= 0 && IsEdgeOpen(grid, x, y))
                y--;
            return y + 1;
        }

        /// <summary>
        /// Confere o fundo do feixe de cada coluna. Onde ele mudou (cavaram a ponta, taparam o poço, mesmo longe da célula
        /// mexida), os blocos já desenhados que a luz desse trecho alcança são marcados para refazer.
        /// </summary>
        /// <param name="fresh">Cache recém-criado: só calcula, não há bloco desenhado para invalidar.</param>
        void RefreshBeams(PixelGrid grid, bool fresh)
        {
            if (_beamBottom == null || _beamBottom.Length != grid.Width)
            {
                _beamBottom = new int[grid.Width];
                fresh = true;
            }
            for (int x = 0; x < grid.Width; x++)
            {
                int bottom = BeamBottom(grid, x);
                int old = _beamBottom[x];
                if (bottom == old)
                    continue;
                _beamBottom[x] = bottom;
                if (fresh)
                    continue;
                // O trecho entre o fundo antigo e o novo ganhou ou perdeu luz cheia; ela se espalha até LightLevels células.
                int cx0 = Mathf.Max(0, x - LightLevels) >> PixelGrid.ChunkShift;
                int cx1 = Mathf.Min(grid.Width - 1, x + LightLevels) >> PixelGrid.ChunkShift;
                int cy0 = Mathf.Max(0, Mathf.Min(old, bottom) - LightLevels) >> PixelGrid.ChunkShift;
                int cy1 = Mathf.Min(grid.Height - 1, Mathf.Max(old, bottom) + LightLevels) >> PixelGrid.ChunkShift;
                for (int cy = cy0; cy <= cy1; cy++)
                for (int cx = cx0; cx <= cx1; cx++)
                    _drawnVersion[cy * grid.ChunkColumns + cx] = -1;
            }
        }

        /// <summary>
        /// Propaga a luz na região do bloco (origem <paramref name="ox"/>, <paramref name="oy"/>, tamanho <paramref name="w"/>
        /// x <paramref name="h"/>) com folga de <see cref="LightLevels"/>: nenhuma fonte de fora da folga alcança o bloco, então o
        /// resultado é exato. Fontes: o lado de fora e o feixe (<see cref="RefreshBeams"/> antes). Fila por nível, do mais forte
        /// para o mais fraco (cada célula sai com o nível final dela). Sem fonte na região (o fundo do formigueiro longe dos
        /// feixes), tudo fica no zero sem propagar.
        /// </summary>
        void ComputeLight(PixelGrid grid, int ox, int oy, int w, int h)
        {
            int margin = LightLevels;
            int stride = w + 2 * margin;
            int rows = h + 2 * margin;
            int count = stride * rows;
            if (_lightRegion == null || _lightRegion.Length < count)
            {
                _lightRegion = new byte[count];
                _lightSolidRegion = new bool[count];
            }

            int top = Mathf.Min(grid.Height - 1, oy - margin + rows - 1);
            // Uma coluna tem fonte na região se o trecho iluminado dela (do fundo do feixe para cima) chega à linha de cima.
            bool anySource = false;
            for (int x = Mathf.Max(0, ox - margin); x < Mathf.Min(grid.Width, ox + w + margin) && !anySource; x++)
                anySource = _beamBottom[x] <= top;
            if (!anySource || _outsideLight == 0)
            {
                System.Array.Clear(_lightRegion, 0, count);
                return;
            }

            for (int r = 0; r < rows; r++)
            {
                int y = oy - margin + r;
                for (int c = 0; c < stride; c++)
                {
                    int x = ox - margin + c;
                    int i = r * stride + c;
                    // Fora da grade conta como terra sem fonte.
                    bool inside = grid.InBounds(x, y);
                    // Do fundo do feixe para cima: lado de fora ou feixe, luz cheia.
                    bool source = inside && y >= _beamBottom[x];
                    _lightSolidRegion[i] = !inside || !IsEdgeOpen(grid, x, y);
                    _lightRegion[i] = source ? (byte)_outsideLight : (byte)0;
                    if (source)
                        _lightQueues[_outsideLight].Add(i);
                }
            }

            int solidCost = Mathf.Max(1, _solidLightCost);
            for (int level = LightLevels; level > 1; level--)
            {
                List<int> queue = _lightQueues[level];
                for (int q = 0; q < queue.Count; q++)
                {
                    int i = queue[q];
                    if (_lightRegion[i] != level)
                        continue;
                    int r = i / stride, c = i - r * stride;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nr = r + dy;
                        if ((uint)nr >= (uint)rows)
                            continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nc = c + dx;
                            if ((dx == 0 && dy == 0) || (uint)nc >= (uint)stride)
                                continue;
                            int n = nr * stride + nc;
                            int next = level - (_lightSolidRegion[n] ? solidCost : 1);
                            if (next > _lightRegion[n])
                            {
                                _lightRegion[n] = (byte)next;
                                _lightQueues[next].Add(n);
                            }
                        }
                    }
                }
                queue.Clear();
            }
            _lightQueues[1].Clear();
        }

        /// <summary>Nível de luz da célula do bloco (<paramref name="lx"/>, <paramref name="ly"/>); precisa de <see cref="ComputeLight"/>.</summary>
        byte LightAt(int lx, int ly, int w) => _lightRegion[(ly + LightLevels) * (w + 2 * LightLevels) + lx + LightLevels];

        /// <summary>
        /// Brilho da célula inteira, quando a luz é aplicada na CPU (sem tiles, ou embaixo de uma marcação): o nível em
        /// <see cref="_lightSteps"/> degraus, com o piso de penumbra no túnel. Igual ao que o shader faz por texel.
        /// </summary>
        float LightFactor(int level, bool tunnel)
        {
            float stepped = Mathf.Round(level / (float)LightLevels * _lightSteps) / _lightSteps;
            return tunnel ? Mathf.Max(stepped, _interiorAmbient) : stepped;
        }

        /// <summary>Nível gravado no canal B da textura de materiais (0..255).</summary>
        static byte LightByte(int level) => (byte)((level * 255 + LightLevels / 2) / LightLevels);

        /// <summary>
        /// Uma camada do frame (pincel, estacas, gotas, formigas de fallback) vai ser desenhada na célula <paramref name="i"/>
        /// da janela: com tiles, a cor de baixo recebe a luz aqui e a célula fica marcada como já iluminada, para o shader não
        /// escurecer a camada junto. Sem tiles a cor já vem iluminada.
        /// </summary>
        void LightUnderOverlay(int i)
        {
            if (_materialIds == null || _cellTiles == null)
                return;
            int t = TileBytes * i;
            byte id = _materialIds[t];
            if (_materialIds[t + 3] != 0 || id == CellTileArt.SkyId)
                return;
            int gridIndex = (_originY + i / _width) * _cachedGrid.Width + _originX + i % _width;
            _pixels[i] = Darken(_pixels[i], LightFactor(_cellLight[gridIndex], id == (byte)CellMaterial.Empty));
            _materialIds[t + 3] = 255;
        }
    }
}
