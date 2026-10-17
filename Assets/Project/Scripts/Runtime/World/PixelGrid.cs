using System;
using System.Collections.Generic;
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
        readonly bool[] _buildMarked;
        // Sala da rainha: lugar pintado pelo jogador, independente do material (não acompanha grãos que caem).
        readonly bool[] _queenRoom;
        // Sala de nutrientes: lugar pintado (como a sala da rainha). Cada célula vazia pintada é um slot.
        readonly bool[] _nutrientRoom;
        // Nutriente guardado: camada à parte, só em célula sem material. Não bloqueia a passagem (a célula continua vazia).
        readonly bool[] _nutrient;
        // Segundos que faltam para o pixel guardado virar nutriente que pode ser levado (0 = pronto).
        readonly float[] _nutrientCure;
        // Células guardadas ainda curando (as com _nutrientCure > 0), para avançar só elas.
        readonly List<int> _curingNutrients = new List<int>();
        readonly byte[] _wetness;
        // Água: camada à parte, só em células sem material. Para navegação e estrutura a célula continua vazia.
        readonly bool[] _water;
        // Planta atrás do material: pixel de planta que terra (ou outro sólido) cobriu. Volta quando a célula esvazia.
        readonly CellMaterial[] _behind;
        readonly byte[] _behindShades;
        readonly int[] _surfaceY;

        // Blocos de ChunkSize x ChunkSize células. Toda mudança de célula "toca" o bloco dela e os vizinhos que
        // encostam na célula: acorda o bloco para a simulação granular e muda a versão do bloco (caches de navegação).
        readonly bool[] _chunkAwake;
        readonly bool[] _chunkAwakeNext;
        readonly bool[] _rowAwake;
        readonly bool[] _rowAwakeNext;
        readonly int[] _chunkVersion;
        readonly int[] _chunkMarked;
        readonly int[] _chunkBuildMarked;
        readonly int[] _chunkQueenRoom;
        readonly int[] _chunkNutrientRoom;
        readonly int[] _chunkNutrient;
        readonly int[] _chunkWater;
        readonly int[] _rowWater;
        // Despertar próprio da água (avançado por WaterSimulation), independente do da física granular.
        readonly bool[] _waterAwake;
        readonly bool[] _waterAwakeNext;
        int _touchCounter;

        int _minX;
        int _minY;
        int _maxX;
        int _maxY;

        public int Width { get; }
        public int Height { get; }

        /// <summary>Muda sempre que uma célula passa de vazia para sólida ou o contrário (invalida a navegação).</summary>
        public int StructureVersion { get; private set; }

        /// <summary>Muda sempre que uma marcação (de escavação, de construção ou de sala da rainha) é criada ou removida.</summary>
        public int MarkVersion { get; private set; }

        /// <summary>Células marcadas para escavar.</summary>
        public int MarkedCount { get; private set; }

        /// <summary>Células vazias marcadas para as formigas preencherem com terra.</summary>
        public int BuildMarkedCount { get; private set; }

        /// <summary>Células pintadas como sala da rainha.</summary>
        public int QueenRoomCount { get; private set; }

        /// <summary>Células pintadas como sala de nutrientes (slots, quando vazias).</summary>
        public int NutrientRoomCount { get; private set; }

        /// <summary>Nutrientes guardados (células com a camada de nutriente).</summary>
        public int NutrientCount { get; private set; }

        /// <summary>Nutrientes guardados que já terminaram de curar e podem ser levados à rainha.</summary>
        public int ReadyNutrientCount => NutrientCount - _curingNutrients.Count;

        /// <summary>Muda quando a sala de nutrientes é pintada/apagada ou um nutriente é guardado, retirado ou perdido.</summary>
        public int NutrientVersion { get; private set; }

        /// <summary>Quantas células têm umidade (chuva leve) maior que zero.</summary>
        public int WetCount { get; private set; }

        /// <summary>Quantas células têm água.</summary>
        public int WaterCount { get; private set; }

        public PixelGrid(int width, int height)
        {
            if (width < 3 || height < 3)
                throw new ArgumentOutOfRangeException(nameof(width), "A grade precisa de pelo menos 3x3 células.");

            Width = width;
            Height = height;
            _cells = new CellMaterial[width * height];
            _shades = new byte[width * height];
            _marked = new bool[width * height];
            _buildMarked = new bool[width * height];
            _queenRoom = new bool[width * height];
            _nutrientRoom = new bool[width * height];
            _nutrient = new bool[width * height];
            _nutrientCure = new float[width * height];
            _wetness = new byte[width * height];
            _water = new bool[width * height];
            _behind = new CellMaterial[width * height];
            _behindShades = new byte[width * height];
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
            _chunkBuildMarked = new int[chunks];
            _chunkQueenRoom = new int[chunks];
            _chunkNutrientRoom = new int[chunks];
            _chunkNutrient = new int[chunks];
            _chunkWater = new int[chunks];
            _rowWater = new int[ChunkRows];
            _waterAwake = new bool[chunks];
            _waterAwakeNext = new bool[chunks];
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

        /// <summary>Marca de construção: célula vazia que as formigas devem preencher com terra.</summary>
        public bool IsBuildMarked(int x, int y) => InBounds(x, y) && _buildMarked[y * Width + x];

        /// <summary>Célula pintada como sala da rainha (vale o lugar, não o material: pode estar pintada e ainda maciça).</summary>
        public bool IsQueenRoom(int x, int y) => InBounds(x, y) && _queenRoom[y * Width + x];

        /// <summary>Célula pintada como sala de nutrientes (vale o lugar, não o material).</summary>
        public bool IsNutrientRoom(int x, int y) => InBounds(x, y) && _nutrientRoom[y * Width + x];

        /// <summary>A célula guarda um nutriente (só células sem material guardam).</summary>
        public bool HasNutrient(int x, int y) => InBounds(x, y) && _nutrient[y * Width + x];

        /// <summary>A célula guarda um nutriente que já curou (pode ser levado à rainha).</summary>
        public bool HasReadyNutrient(int x, int y) =>
            InBounds(x, y) && _nutrient[y * Width + x] && _nutrientCure[y * Width + x] <= 0f;

        /// <summary>Segundos que faltam para o nutriente guardado na célula curar (0 = pronto ou sem nutriente).</summary>
        public float GetNutrientCure(int x, int y) => InBounds(x, y) ? _nutrientCure[y * Width + x] : 0f;

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

        /// <summary>
        /// Planta guardada atrás do material da célula (<see cref="CellMaterial.Empty"/> = nenhuma). As plantas ficam numa
        /// profundidade atrás da terra: grão que cai numa célula de planta a cobre em vez de parar em cima, e a planta
        /// volta quando a célula esvazia (o grão segue caindo, a formiga cava). Não conta como material para nada.
        /// </summary>
        public CellMaterial GetBehind(int x, int y) => InBounds(x, y) ? _behind[y * Width + x] : CellMaterial.Empty;

        public byte GetBehindShade(int x, int y) => InBounds(x, y) ? _behindShades[y * Width + x] : (byte)0;

        /// <summary>
        /// Guarda (ou tira, com <see cref="CellMaterial.Empty"/>) uma planta atrás do material. Só planta e só em célula
        /// com material que não é planta (save, crescimento). Devolve false se não aceitou.
        /// </summary>
        public bool SetBehind(int x, int y, CellMaterial plant, byte shade = 128)
        {
            if (!InBounds(x, y))
                return false;
            int i = y * Width + x;
            if (plant != CellMaterial.Empty &&
                (!CellMaterials.IsPlant(plant) || _cells[i] == CellMaterial.Empty || CellMaterials.IsPlant(_cells[i])))
                return false;
            _behind[i] = plant;
            _behindShades[i] = plant == CellMaterial.Empty ? (byte)0 : shade;
            return true;
        }

        /// <summary>Grão pode cair na célula: vazia ou planta (que fica atrás dele).</summary>
        internal bool CanFallIntoAt(int index)
        {
            CellMaterial m = _cells[index];
            return m == CellMaterial.Empty || CellMaterials.IsPlant(m);
        }

        /// <summary>Guarda a planta da célula atrás do material que vai entrar (sem marca de colheita).</summary>
        void PushBehindAt(int index)
        {
            _behind[index] = _cells[index];
            _behindShades[index] = _shades[index];
            if (_marked[index])
                ClearMarkAt(index);
        }

        internal bool IsMarkedAt(int index) => _marked[index];

        /// <summary>
        /// Troca o material da célula. Material sólido sobre água a expulsa para a célula de cima, se ela estiver livre
        /// (vazia e sem água); senão a água some.
        /// </summary>
        public void Set(int x, int y, CellMaterial material, byte shade = 128)
        {
            if (!InBounds(x, y))
                return;

            int i = y * Width + x;
            bool wasSolid = _cells[i] != CellMaterial.Empty;
            if (material != CellMaterial.Empty && !CellMaterials.IsPlant(material) && CellMaterials.IsPlant(_cells[i]))
                PushBehindAt(i);
            else if (CellMaterials.IsPlant(material))
                _behind[i] = CellMaterial.Empty;
            if (material == CellMaterial.Empty && _behind[i] != CellMaterial.Empty)
            {
                // A planta que estava atrás volta (a terra que a cobria saiu).
                material = _behind[i];
                shade = _behindShades[i];
                _behind[i] = CellMaterial.Empty;
            }
            _cells[i] = material;
            _shades[i] = shade;
            SetWetnessAt(i, 0);
            // Material sólido por cima de um nutriente guardado o destrói (terra caindo, obra).
            if (_nutrient[i] && material != CellMaterial.Empty)
                SetNutrientAt(i, false);
            if (_water[i] && material != CellMaterial.Empty)
            {
                SetWaterAt(i, false);
                if (IsOpen(x, y + 1))
                    SetWaterAt(i + Width, true);
            }

            if (_marked[i] && !CellMaterials.IsMarkable(material))
                ClearMarkAt(i);
            if (_buildMarked[i] && material != CellMaterial.Empty)
                ClearBuildMarkAt(i);
            if (wasSolid != (material != CellMaterial.Empty))
                StructureVersion++;
            Touch(x, y);
        }

        /// <summary>
        /// Marca/desmarca uma célula para escavação (ou colheita, se for planta). Só células escaváveis ou plantas
        /// aceitam marca.
        /// </summary>
        public bool SetMarked(int x, int y, bool marked)
        {
            if (!InBounds(x, y))
                return false;

            int i = y * Width + x;
            if (_marked[i] == marked)
                return false;
            if (marked && !CellMaterials.IsMarkable(_cells[i]))
                return false;

            _marked[i] = marked;
            MarkedCount += marked ? 1 : -1;
            _chunkMarked[ChunkOf(i)] += marked ? 1 : -1;
            MarkVersion++;
            return true;
        }

        /// <summary>
        /// Marca/desmarca uma célula para construção. Só células vazias abaixo do solo original aceitam marca (o céu
        /// não se constrói); a marca some quando a célula vira sólida.
        /// </summary>
        public bool SetBuildMarked(int x, int y, bool marked)
        {
            if (!InBounds(x, y))
                return false;

            int i = y * Width + x;
            if (_buildMarked[i] == marked)
                return false;
            if (marked && (_cells[i] != CellMaterial.Empty || y > GetSurfaceY(x)))
                return false;

            _buildMarked[i] = marked;
            BuildMarkedCount += marked ? 1 : -1;
            _chunkBuildMarked[ChunkOf(i)] += marked ? 1 : -1;
            MarkVersion++;
            return true;
        }

        /// <summary>
        /// Pinta/apaga a sala da rainha. Só células do formigueiro (no solo original ou abaixo dele) aceitam, sólidas ou
        /// não: dá para pintar antes de cavar. Só as vazias contam como área da rainha.
        /// </summary>
        public bool SetQueenRoom(int x, int y, bool room)
        {
            if (!InBounds(x, y))
                return false;

            int i = y * Width + x;
            if (_queenRoom[i] == room)
                return false;
            if (room && (y > GetSurfaceY(x) || _nutrientRoom[i]))
                return false;

            _queenRoom[i] = room;
            QueenRoomCount += room ? 1 : -1;
            _chunkQueenRoom[ChunkOf(i)] += room ? 1 : -1;
            MarkVersion++;
            return true;
        }

        /// <summary>
        /// Pinta/apaga a sala de nutrientes. Como a sala da rainha: só células do formigueiro (no solo original ou abaixo),
        /// sólidas ou não, e nunca por cima da sala da rainha. Apagar não destrói o nutriente guardado ali (ele ainda pode
        /// ser levado à rainha), só tira o slot.
        /// </summary>
        public bool SetNutrientRoom(int x, int y, bool room)
        {
            if (!InBounds(x, y))
                return false;

            int i = y * Width + x;
            if (_nutrientRoom[i] == room)
                return false;
            if (room && (y > GetSurfaceY(x) || _queenRoom[i]))
                return false;

            _nutrientRoom[i] = room;
            NutrientRoomCount += room ? 1 : -1;
            _chunkNutrientRoom[ChunkOf(i)] += room ? 1 : -1;
            NutrientVersion++;
            return true;
        }

        /// <summary>
        /// Guarda ou retira um nutriente. Só célula sem material guarda (a sala não exige: nutriente guardado continua
        /// lá se a pintura for apagada). Guardado com <paramref name="cureSeconds"/> &gt; 0, ocupa o slot mas só vira
        /// nutriente pronto depois desse tempo (<see cref="AdvanceNutrientCure"/>). Devolve false se nada mudou.
        /// </summary>
        public bool SetNutrient(int x, int y, bool nutrient, float cureSeconds = 0f)
        {
            if (!InBounds(x, y))
                return false;
            int i = y * Width + x;
            if (_nutrient[i] == nutrient || (nutrient && _cells[i] != CellMaterial.Empty))
                return false;
            SetNutrientAt(i, nutrient);
            if (nutrient && cureSeconds > 0f)
            {
                _nutrientCure[i] = cureSeconds;
                _curingNutrients.Add(i);
            }
            return true;
        }

        /// <summary>
        /// Avança a cura dos pixels guardados. Os que terminam viram nutrientes prontos e mudam
        /// <see cref="NutrientVersion"/> (refaz o campo de busca das formigas).
        /// </summary>
        public void AdvanceNutrientCure(float deltaTime)
        {
            bool cured = false;
            for (int k = _curingNutrients.Count - 1; k >= 0; k--)
            {
                int i = _curingNutrients[k];
                _nutrientCure[i] -= deltaTime;
                if (_nutrientCure[i] > 0f)
                    continue;
                _nutrientCure[i] = 0f;
                _curingNutrients.RemoveAt(k);
                cured = true;
            }
            if (cured)
                NutrientVersion++;
        }

        void SetNutrientAt(int index, bool nutrient)
        {
            if (!nutrient && _nutrientCure[index] > 0f)
            {
                _nutrientCure[index] = 0f;
                _curingNutrients.Remove(index);
            }
            _nutrient[index] = nutrient;
            NutrientCount += nutrient ? 1 : -1;
            _chunkNutrient[ChunkOf(index)] += nutrient ? 1 : -1;
            NutrientVersion++;
        }

        /// <summary>
        /// Move material, tom e marca de uma célula para outra vazia ou com planta (usado pela simulação granular). Se o
        /// destino tinha água, ela troca de lugar com o grão (o grão afunda); se tinha planta, ela fica atrás do grão
        /// (<see cref="GetBehind"/>). A planta que estava atrás da origem volta para ela.
        /// </summary>
        internal void MoveCell(int from, int to)
        {
            if (CellMaterials.IsPlant(_cells[to]))
                PushBehindAt(to);
            if (_water[to])
            {
                SetWaterAt(to, false);
                // A origem só fica livre para a água se não houver planta atrás do grão para voltar a ela.
                if (_behind[from] == CellMaterial.Empty)
                    SetWaterAt(from, true);
            }
            _cells[to] = _cells[from];
            _shades[to] = _shades[from];
            if (_marked[from])
            {
                _chunkMarked[ChunkOf(from)]--;
                _chunkMarked[ChunkOf(to)]++;
            }
            _marked[to] = _marked[from];
            if (_buildMarked[to])
                ClearBuildMarkAt(to);
            if (_nutrient[to])
                SetNutrientAt(to, false);
            SetWetnessAt(to, _wetness[from]);
            SetWetnessAt(from, 0);
            _cells[from] = _behind[from];
            _shades[from] = _behindShades[from];
            _behind[from] = CellMaterial.Empty;
            _behindShades[from] = 0;
            _marked[from] = false;
            StructureVersion++;
            Touch(from % Width, from / Width);
            Touch(to % Width, to / Width);
        }

        // ---------------------------------------------------------------- água

        /// <summary>A célula tem água (só células sem material têm).</summary>
        public bool HasWater(int x, int y) => InBounds(x, y) && _water[y * Width + x];

        internal bool HasWaterAt(int index) => _water[index];

        /// <summary>Dentro da área ativa, sem material e sem água: a água (ou uma gota) pode entrar.</summary>
        public bool IsOpen(int x, int y)
        {
            if (!InBounds(x, y))
                return false;
            int i = y * Width + x;
            return _cells[i] == CellMaterial.Empty && !_water[i];
        }

        /// <summary>Põe ou tira água. Só célula sem material aceita água. Devolve false se nada mudou.</summary>
        public bool SetWater(int x, int y, bool water)
        {
            if (!InBounds(x, y))
                return false;
            int i = y * Width + x;
            if (_water[i] == water || (water && _cells[i] != CellMaterial.Empty))
                return false;
            SetWaterAt(i, water);
            return true;
        }

        /// <summary>Move a água de uma célula para outra livre (usado pela simulação da água).</summary>
        internal void MoveWater(int from, int to)
        {
            SetWaterAt(from, false);
            SetWaterAt(to, true);
        }

        void SetWaterAt(int index, bool water)
        {
            if (_water[index] == water)
                return;
            _water[index] = water;
            int delta = water ? 1 : -1;
            WaterCount += delta;
            int y = index / Width;
            _chunkWater[(y >> ChunkShift) * ChunkColumns + ((index % Width) >> ChunkShift)] += delta;
            _rowWater[y >> ChunkShift] += delta;
            Touch(index % Width, y);
        }

        /// <summary>Quantas células com água há no bloco.</summary>
        public int GetChunkWaterCount(int cx, int cy) => _chunkWater[cy * ChunkColumns + cx];

        /// <summary>Quantas células com água há na faixa de blocos <paramref name="cy"/>.</summary>
        internal int GetChunkRowWaterCount(int cy) => _rowWater[cy];

        /// <summary>Bloco tocado desde o começo do passo da água anterior (a água nele pode se mexer).</summary>
        internal bool IsWaterChunkAwake(int cx, int cy) => _waterAwake[cy * ChunkColumns + cx];

        /// <summary>Fim de um passo da água: só continua acordado o que foi tocado durante ele.</summary>
        internal void AdvanceWaterWake()
        {
            Array.Copy(_waterAwakeNext, _waterAwake, _waterAwake.Length);
            Array.Clear(_waterAwakeNext, 0, _waterAwakeNext.Length);
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

        /// <summary>Quantas células com marca de construção há no bloco.</summary>
        public int GetChunkBuildMarkedCount(int cx, int cy) => _chunkBuildMarked[cy * ChunkColumns + cx];

        /// <summary>Quantas células pintadas como sala da rainha há no bloco.</summary>
        public int GetChunkQueenRoomCount(int cx, int cy) => _chunkQueenRoom[cy * ChunkColumns + cx];

        /// <summary>Quantas células pintadas como sala de nutrientes há no bloco.</summary>
        public int GetChunkNutrientRoomCount(int cx, int cy) => _chunkNutrientRoom[cy * ChunkColumns + cx];

        /// <summary>Quantos nutrientes guardados há no bloco.</summary>
        public int GetChunkNutrientCount(int cx, int cy) => _chunkNutrient[cy * ChunkColumns + cx];

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
                _waterAwake[c] = true;
                _waterAwakeNext[c] = true;
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

        void ClearBuildMarkAt(int index)
        {
            _buildMarked[index] = false;
            BuildMarkedCount--;
            _chunkBuildMarked[ChunkOf(index)]--;
            MarkVersion++;
        }
    }
}
