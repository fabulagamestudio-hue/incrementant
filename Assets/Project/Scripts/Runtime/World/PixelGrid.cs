using System;
using UnityEngine;

namespace Fabula.Incrementant.World
{
    /// <summary>
    /// Grade de células do terreno. Origem no canto inferior esquerdo, y para cima, índice = y * Width + x.
    /// <see cref="Width"/>/<see cref="Height"/> são a capacidade (o mundo inteiro); só a área ativa
    /// (<see cref="Active"/>, já gerada) existe para o jogo. Fora dela é tratado como <see cref="CellMaterial.Bedrock"/>.
    /// A área ativa só cresce (<see cref="ExpandActive"/>), conforme o mapa procedural é revelado.
    /// </summary>
    public sealed class PixelGrid
    {
        readonly CellMaterial[] _cells;
        readonly byte[] _shades;
        readonly bool[] _marked;
        readonly byte[] _wetness;
        readonly int[] _surfaceY;

        // Blocos de ChunkSize x ChunkSize células. Toda mudança de célula "toca" o bloco dela e os vizinhos que
        // encostam na célula: acorda o bloco para a simulação granular e muda a versão do bloco (caches de navegação).
        readonly bool[] _chunkAwake;
        readonly bool[] _chunkAwakeNext;
        readonly bool[] _rowAwake;
        readonly bool[] _rowAwakeNext;
        readonly int[] _chunkVersion;
        readonly int[] _chunkMarked;
        int _touchCounter;

        int _minX;
        int _minY;
        int _maxX;
        int _maxY;

        public int Width { get; }
        public int Height { get; }

        /// <summary>Muda sempre que uma célula passa de vazia para sólida ou o contrário (invalida a navegação).</summary>
        public int StructureVersion { get; private set; }

        /// <summary>Muda sempre que uma marcação de escavação é criada ou removida.</summary>
        public int MarkVersion { get; private set; }

        public int MarkedCount { get; private set; }

        /// <summary>Quantas células têm umidade (chuva leve) maior que zero.</summary>
        public int WetCount { get; private set; }

        public PixelGrid(int width, int height)
        {
            if (width < 3 || height < 3)
                throw new ArgumentOutOfRangeException(nameof(width), "A grade precisa de pelo menos 3x3 células.");

            Width = width;
            Height = height;
            _cells = new CellMaterial[width * height];
            _shades = new byte[width * height];
            _marked = new bool[width * height];
            _wetness = new byte[width * height];
            _surfaceY = new int[width];
            for (int x = 0; x < width; x++)
                _surfaceY[x] = -1;
            _maxX = width;
            _maxY = height;

            ChunkColumns = (width + ChunkSize - 1) >> ChunkShift;
            ChunkRows = (height + ChunkSize - 1) >> ChunkShift;
            int chunks = ChunkColumns * ChunkRows;
            _chunkAwake = new bool[chunks];
            _chunkAwakeNext = new bool[chunks];
            _rowAwake = new bool[ChunkRows];
            _rowAwakeNext = new bool[ChunkRows];
            _chunkVersion = new int[chunks];
            _chunkMarked = new int[chunks];
            TouchAll();
        }

        public const int ChunkShift = 4;
        public const int ChunkSize = 1 << ChunkShift;
        public int ChunkColumns { get; }
        public int ChunkRows { get; }

        /// <summary>Área já gerada, em células. Começa igual à grade inteira.</summary>
        public RectInt Active => new RectInt(_minX, _minY, _maxX - _minX, _maxY - _minY);

        /// <summary>Dentro da área ativa (gerada). É o limite que vale para simulação, navegação e marcação.</summary>
        public bool InBounds(int x, int y) => x >= _minX && x < _maxX && y >= _minY && y < _maxY;

        /// <summary>Restringe a área ativa a <paramref name="area"/> (recortada à grade). Usado ao começar um mapa expansível.</summary>
        public void SetActive(RectInt area)
        {
            _minX = Math.Max(0, area.xMin);
            _minY = Math.Max(0, area.yMin);
            _maxX = Math.Min(Width, area.xMax);
            _maxY = Math.Min(Height, area.yMax);
            StructureVersion++;
            TouchAll();
        }

        /// <summary>
        /// Une <paramref name="area"/> à área ativa (nunca encolhe). Quem chama preenche as células novas logo depois;
        /// até lá elas valem o que estiver no array (vazio). Devolve false se nada mudou.
        /// </summary>
        public bool ExpandActive(RectInt area)
        {
            int minX = Math.Max(0, Math.Min(_minX, area.xMin));
            int minY = Math.Max(0, Math.Min(_minY, area.yMin));
            int maxX = Math.Min(Width, Math.Max(_maxX, area.xMax));
            int maxY = Math.Min(Height, Math.Max(_maxY, area.yMax));
            if (minX == _minX && minY == _minY && maxX == _maxX && maxY == _maxY)
                return false;
            // As faixas novas deixam de valer Bedrock (InBounds): toca cada uma, com a borda antiga junto.
            TouchRect(minX, minY, _minX, maxY);
            TouchRect(_maxX, minY, maxX, maxY);
            TouchRect(_minX, minY, _maxX, _minY);
            TouchRect(_minX, _maxY, _maxX, maxY);
            _minX = minX;
            _minY = minY;
            _maxX = maxX;
            _maxY = maxY;
            StructureVersion++;
            return true;
        }

        public int ToIndex(int x, int y) => y * Width + x;

        public CellMaterial Get(int x, int y) => InBounds(x, y) ? _cells[y * Width + x] : CellMaterial.Bedrock;

        public byte GetShade(int x, int y) => InBounds(x, y) ? _shades[y * Width + x] : (byte)0;

        public bool IsSolid(int x, int y) => Get(x, y) != CellMaterial.Empty;

        public bool IsMarked(int x, int y) => InBounds(x, y) && _marked[y * Width + x];

        /// <summary>
        /// Umidade acumulada pela chuva leve (0 = seca). Acompanha a célula quando ela cai e zera quando o material
        /// muda (<see cref="Set"/>). É só estado visual/de erosão: não toca blocos nem muda versões.
        /// </summary>
        public byte GetWetness(int x, int y) => InBounds(x, y) ? _wetness[y * Width + x] : (byte)0;

        public void SetWetness(int x, int y, byte wetness)
        {
            if (!InBounds(x, y))
                return;
            int i = y * Width + x;
            SetWetnessAt(i, wetness);
        }

        void SetWetnessAt(int index, byte wetness)
        {
            byte old = _wetness[index];
            if (old == wetness)
                return;
            if (old == 0) WetCount++;
            else if (wetness == 0) WetCount--;
            _wetness[index] = wetness;
        }

        /// <summary>Tira 1 de umidade de toda célula molhada da área ativa (secagem).</summary>
        public void DryAll()
        {
            if (WetCount == 0)
                return;
            for (int y = _minY; y < _maxY; y++)
            {
                int row = y * Width;
                for (int i = row + _minX; i < row + _maxX; i++)
                {
                    if (_wetness[i] != 0)
                        SetWetnessAt(i, (byte)(_wetness[i] - 1));
                }
            }
        }

        internal CellMaterial GetAt(int index) => _cells[index];

        internal bool IsMarkedAt(int index) => _marked[index];

        public void Set(int x, int y, CellMaterial material, byte shade = 128)
        {
            if (!InBounds(x, y))
                return;

            int i = y * Width + x;
            bool wasSolid = _cells[i] != CellMaterial.Empty;
            _cells[i] = material;
            _shades[i] = shade;
            SetWetnessAt(i, 0);

            if (_marked[i] && !CellMaterials.IsDiggable(material))
                ClearMarkAt(i);
            if (wasSolid != (material != CellMaterial.Empty))
                StructureVersion++;
            Touch(x, y);
        }

        /// <summary>Marca/desmarca uma célula para escavação. Só células escaváveis aceitam marca.</summary>
        public bool SetMarked(int x, int y, bool marked)
        {
            if (!InBounds(x, y))
                return false;

            int i = y * Width + x;
            if (_marked[i] == marked)
                return false;
            if (marked && !CellMaterials.IsDiggable(_cells[i]))
                return false;

            _marked[i] = marked;
            MarkedCount += marked ? 1 : -1;
            _chunkMarked[ChunkOf(i)] += marked ? 1 : -1;
            MarkVersion++;
            return true;
        }

        /// <summary>Move material, tom e marca de uma célula para outra vazia (usado pela simulação granular).</summary>
        internal void MoveCell(int from, int to)
        {
            _cells[to] = _cells[from];
            _shades[to] = _shades[from];
            if (_marked[from])
            {
                _chunkMarked[ChunkOf(from)]--;
                _chunkMarked[ChunkOf(to)]++;
            }
            _marked[to] = _marked[from];
            SetWetnessAt(to, _wetness[from]);
            SetWetnessAt(from, 0);
            _cells[from] = CellMaterial.Empty;
            _shades[from] = 0;
            _marked[from] = false;
            StructureVersion++;
            Touch(from % Width, from / Width);
            Touch(to % Width, to / Width);
        }

        // ---------------------------------------------------------------- blocos

        /// <summary>
        /// Bloco com alguma mudança desde o começo do passo granular anterior. Bloco dormindo só tem grãos parados,
        /// então a simulação pode pulá-lo sem mudar o resultado.
        /// </summary>
        public bool IsChunkAwake(int cx, int cy) => _chunkAwake[cy * ChunkColumns + cx];

        /// <summary>Fim de um passo granular: só continua acordado o que foi tocado durante ele (ou depois dele).</summary>
        internal void AdvanceChunkWake()
        {
            Array.Copy(_chunkAwakeNext, _chunkAwake, _chunkAwake.Length);
            Array.Clear(_chunkAwakeNext, 0, _chunkAwakeNext.Length);
            Array.Copy(_rowAwakeNext, _rowAwake, _rowAwake.Length);
            Array.Clear(_rowAwakeNext, 0, _rowAwakeNext.Length);
        }

        /// <summary>Alguma faixa de blocos da linha <paramref name="cy"/> está acordada.</summary>
        internal bool IsChunkRowAwake(int cy) => _rowAwake[cy];

        /// <summary>Muda sempre que alguma célula do bloco (ou encostada nele) muda. Para caches por bloco.</summary>
        public int GetChunkVersion(int cx, int cy) => _chunkVersion[cy * ChunkColumns + cx];

        /// <summary>Quantas células marcadas há no bloco.</summary>
        public int GetChunkMarkedCount(int cx, int cy) => _chunkMarked[cy * ChunkColumns + cx];

        int ChunkOf(int index) => ((index / Width) >> ChunkShift) * ChunkColumns + ((index % Width) >> ChunkShift);

        /// <summary>Toca os blocos da célula e dos 8 vizinhos dela (o que depende dela: queda dos grãos e caminhabilidade).</summary>
        void Touch(int x, int y) => TouchRect(x, y, x + 1, y + 1);

        /// <summary>Toca os blocos de [xMin, xMax) x [yMin, yMax), com 1 célula de folga em volta.</summary>
        void TouchRect(int xMin, int yMin, int xMax, int yMax)
        {
            if (xMax <= xMin || yMax <= yMin)
                return;
            int cx0 = Math.Max(0, (xMin - 1) >> ChunkShift);
            int cy0 = Math.Max(0, (yMin - 1) >> ChunkShift);
            int cx1 = Math.Min(ChunkColumns - 1, xMax >> ChunkShift);
            int cy1 = Math.Min(ChunkRows - 1, yMax >> ChunkShift);
            int version = ++_touchCounter;
            for (int cy = cy0; cy <= cy1; cy++)
            {
                _rowAwake[cy] = true;
                _rowAwakeNext[cy] = true;
            }
            for (int cy = cy0; cy <= cy1; cy++)
            for (int cx = cx0; cx <= cx1; cx++)
            {
                int c = cy * ChunkColumns + cx;
                _chunkAwake[c] = true;
                _chunkAwakeNext[c] = true;
                _chunkVersion[c] = version;
            }
        }

        void TouchAll() => TouchRect(0, 0, Width, Height);

        /// <summary>
        /// Altura do solo original na coluna; acima dela o vazio é céu, abaixo é túnel.
        /// Vale para todas as colunas da grade, mesmo fora da área ativa (o gerador calcula o relevo inteiro de início).
        /// </summary>
        public int GetSurfaceY(int x) => (uint)x < (uint)Width ? _surfaceY[x] : -1;

        public void SetSurfaceY(int x, int y)
        {
            if ((uint)x < (uint)Width && _surfaceY[x] != y)
            {
                _surfaceY[x] = y;
                TouchRect(x, 0, x + 1, Height);
            }
        }

        void ClearMarkAt(int index)
        {
            _marked[index] = false;
            MarkedCount--;
            _chunkMarked[ChunkOf(index)]--;
            MarkVersion++;
        }
    }
}
