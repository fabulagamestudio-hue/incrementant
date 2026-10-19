using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fabula.Incrementant.World
{
    /// <summary>
    /// Etapa de um canteiro da sala de nutrientes (valor gravado no save: não renumerar). A formiga põe a terra fofa
    /// (<see cref="Soil"/>), depois a planta (<see cref="Planted"/>) e depois a água (<see cref="Growing"/>); brotado, ele
    /// gera nutrientes prontos nos lados livres e volta a <see cref="Soil"/>.
    /// </summary>
    public enum BedStage : byte
    {
        None = 0,
        /// <summary>Só a terra fofa: espera um pedaço de planta.</summary>
        Soil = 1,
        /// <summary>Terra com planta: espera um bloco de água.</summary>
        Planted = 2,
        /// <summary>Regado: brota no tempo de <c>ColonySettings.NutrientCureSeconds</c>, com formiga cuidando da sala.</summary>
        Growing = 3,
    }

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
        // Sala de nutrientes: lugar pintado (como a sala da rainha). É onde as formigas montam os canteiros.
        readonly bool[] _nutrientRoom;
        // Berçário: lugar pintado (como as outras salas). Cada célula vazia e alcançável conta para as vagas de cria.
        readonly bool[] _nurseryRoom;
        // Área de resíduos: lugar pintado (como as outras salas). As formigas empilham ali entulho e restos, que se decompõem.
        readonly bool[] _wasteRoom;
        readonly bool[] _dewRoom;
        // Nutriente pronto: camada à parte, só em célula sem material. Não bloqueia a passagem (a célula continua vazia).
        readonly bool[] _nutrient;
        // Canteiro: etapa do cultivo de uma célula de terra fofa (Spoil) e segundos que faltam para ele brotar.
        readonly BedStage[] _bedStage;
        readonly float[] _bedGrow;
        // Células com canteiro, para percorrer só elas.
        readonly List<int> _beds = new List<int>();
        readonly byte[] _wetness;
        // Água: camada à parte, só em células sem material. Para navegação e estrutura a célula continua vazia.
        readonly bool[] _water;
        // Planta atrás do material: pixel de planta que terra (ou outro sólido) cobriu. Volta quando a célula esvazia.
        readonly CellMaterial[] _behind;
        readonly byte[] _behindShades;
        // Fundo do cenário: célula coberta pela arte de um elemento de perto (cogumelo, graveto, folha seca, pedrinha).
        // Não é material: só diz que ali há algo atrás do vidro em que a formiga pode andar (AntNavigation.IsWalkable).
        readonly bool[] _backdrop;
        // Obra: sólido encaixado por uma formiga construindo. Some quando o material da célula muda.
        readonly bool[] _built;
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
        readonly int[] _chunkNurseryRoom;
        readonly int[] _chunkWasteRoom;
        readonly int[] _chunkDewRoom;
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

        /// <summary>Pixels de planta soltos (<see cref="CellMaterials.IsPlantDebris"/>) à espera de colheita; cada um já ocupa um slot da sala de nutrientes.</summary>
        public int PlantDebrisCount { get; private set; }

        /// <summary>Células vazias marcadas para as formigas preencherem com terra.</summary>
        public int BuildMarkedCount { get; private set; }

        /// <summary>Células pintadas como sala da rainha.</summary>
        public int QueenRoomCount { get; private set; }

        /// <summary>Células pintadas como sala de nutrientes (slots, quando vazias).</summary>
        public int NutrientRoomCount { get; private set; }

        /// <summary>Células pintadas como berçário.</summary>
        public int NurseryRoomCount { get; private set; }

        /// <summary>Células pintadas como área de resíduos.</summary>
        public int WasteRoomCount { get; private set; }
        public int DewRoomCount { get; private set; }

        /// <summary>Nutrientes prontos no chão (células com a camada de nutriente), à espera de serem levados à rainha.</summary>
        public int NutrientCount { get; private set; }

        /// <summary>Canteiros (terra fofa posta pelas formigas na sala de nutrientes), em qualquer etapa.</summary>
        public int BedCount => _beds.Count;

        /// <summary>Canteiros regados que estão brotando (<see cref="BedStage.Growing"/>).</summary>
        public int GrowingBedCount { get; private set; }

        /// <summary>
        /// Muda quando a sala de nutrientes é pintada/apagada, um nutriente aparece, é retirado ou perdido, ou um canteiro
        /// é montado, muda de etapa ou se desfaz.
        /// </summary>
        public int NutrientVersion { get; private set; }

        /// <summary>Muda sempre que alguma célula ganha ou perde água (a água que escorre muda isto a cada passo).</summary>
        public int WaterVersion { get; private set; }

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
            _nurseryRoom = new bool[width * height];
            _wasteRoom = new bool[width * height];
            _dewRoom = new bool[width * height];
            _nutrient = new bool[width * height];
            _bedStage = new BedStage[width * height];
            _bedGrow = new float[width * height];
            _wetness = new byte[width * height];
            _water = new bool[width * height];
            _behind = new CellMaterial[width * height];
            _behindShades = new byte[width * height];
            _backdrop = new bool[width * height];
            _built = new bool[width * height];
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
            _chunkNurseryRoom = new int[chunks];
            _chunkWasteRoom = new int[chunks];
            _chunkDewRoom = new int[chunks];
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

        /// <summary>Célula pintada como berçário (vale o lugar, não o material).</summary>
        public bool IsNurseryRoom(int x, int y) => InBounds(x, y) && _nurseryRoom[y * Width + x];

        /// <summary>A célula está pintada como área de resíduos.</summary>
        public bool IsWasteRoom(int x, int y) => InBounds(x, y) && _wasteRoom[y * Width + x];

        /// <summary>A célula está pintada como sala Gotas de Orvalho.</summary>
        public bool IsDewRoom(int x, int y) => InBounds(x, y) && _dewRoom[y * Width + x];

        /// <summary>A célula guarda um nutriente (só células sem material guardam).</summary>
        public bool HasNutrient(int x, int y) => InBounds(x, y) && _nutrient[y * Width + x];

        /// <summary>Etapa do canteiro na célula (<see cref="BedStage.None"/> = não é canteiro).</summary>
        public BedStage GetBedStage(int x, int y) => InBounds(x, y) ? _bedStage[y * Width + x] : BedStage.None;

        internal bool IsBedAt(int index) => _bedStage[index] != BedStage.None;

        /// <summary>Segundos que faltam para o canteiro regado brotar (0 fora de <see cref="BedStage.Growing"/>).</summary>
        public float GetBedGrow(int x, int y) => InBounds(x, y) ? _bedGrow[y * Width + x] : 0f;

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

        /// <summary>
        /// Célula coberta pela arte do cenário de perto (<see cref="SetBackdrop"/>). Não é material: a célula continua
        /// vazia para a física e a água; só a navegação a usa (a formiga anda sobre a arte, como no fundo do túnel).
        /// </summary>
        public bool IsBackdrop(int x, int y) => InBounds(x, y) && _backdrop[y * Width + x];

        /// <summary>
        /// Marca/desmarca a célula como coberta pela arte do cenário. Vale fora da área ativa também (o cenário é pintado
        /// para o mundo inteiro e a área cresce depois). Muda a navegação: toca o bloco e a <see cref="StructureVersion"/>.
        /// </summary>
        public void SetBackdrop(int x, int y, bool backdrop)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
                return;
            int i = y * Width + x;
            if (_backdrop[i] == backdrop)
                return;
            _backdrop[i] = backdrop;
            StructureVersion++;
            Touch(x, y);
        }

        /// <summary>Tira o fundo do cenário de todas as células (antes de pintar o cenário de um mapa novo).</summary>
        public void ClearBackdrop()
        {
            for (int i = 0; i < _backdrop.Length; i++)
            {
                if (_backdrop[i])
                    SetBackdrop(i % Width, i / Width, false);
            }
        }

        /// <summary>Sólido encaixado por uma formiga construindo (<see cref="SetBuilt"/>); deixa de ser quando o material muda.</summary>
        public bool IsBuilt(int x, int y) => InBounds(x, y) && _built[y * Width + x];

        /// <summary>
        /// Marca/desmarca a célula como obra. Só célula sólida aceita a marca; <see cref="Set"/> a apaga. Muda a navegação
        /// (fora do formigueiro a formiga anda por dentro da obra). Devolve false se não aceitou.
        /// </summary>
        public bool SetBuilt(int x, int y, bool built)
        {
            if (!InBounds(x, y))
                return false;
            int i = y * Width + x;
            if (built && _cells[i] == CellMaterial.Empty)
                return false;
            if (_built[i] == built)
                return true;
            _built[i] = built;
            StructureVersion++;
            Touch(x, y);
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
            PlantDebrisCount += (CellMaterials.IsPlantDebris(material) ? 1 : 0) - (CellMaterials.IsPlantDebris(_cells[i]) ? 1 : 0);
            _cells[i] = material;
            _shades[i] = shade;
            SetWetnessAt(i, 0);
            // A obra é a peça encaixada: trocar o material (cavar, virar lama) desfaz a marca. Quem constrói marca de novo.
            bool wasBuilt = _built[i];
            _built[i] = false;
            // Material sólido por cima de um nutriente o destrói (terra caindo, obra).
            if (_nutrient[i] && material != CellMaterial.Empty)
                SetNutrientAt(i, false);
            // O canteiro é a terra fofa: cavada ou trocada por outro material, ele se desfaz (planta e água junto).
            if (_bedStage[i] != BedStage.None && material != CellMaterial.Spoil)
                RemoveBedAt(i);
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
            if (wasSolid != (material != CellMaterial.Empty) || wasBuilt)
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
            if (CellMaterials.IsPlant(_cells[i]))
                Touch(x, y);
            return true;
        }

        /// <summary>
        /// Marca/desmarca uma célula para construção. Só células vazias aceitam marca, dentro do formigueiro ou no
        /// céu acima do solo original (construção do lado de fora); a marca some quando a célula vira sólida.
        /// </summary>
        public bool SetBuildMarked(int x, int y, bool marked)
        {
            if (!InBounds(x, y))
                return false;

            int i = y * Width + x;
            if (_buildMarked[i] == marked)
                return false;
            if (marked && _cells[i] != CellMaterial.Empty)
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
            if (room && (y > GetSurfaceY(x) || _nutrientRoom[i] || _nurseryRoom[i] || _wasteRoom[i] || _dewRoom[i]))
                return false;

            _queenRoom[i] = room;
            QueenRoomCount += room ? 1 : -1;
            _chunkQueenRoom[ChunkOf(i)] += room ? 1 : -1;
            MarkVersion++;
            return true;
        }

        /// <summary>
        /// Pinta/apaga o berçário. Como a sala da rainha: só células do formigueiro (no solo original ou abaixo), sólidas ou
        /// não, e nunca por cima de outra sala.
        /// </summary>
        public bool SetNurseryRoom(int x, int y, bool room)
        {
            if (!InBounds(x, y))
                return false;

            int i = y * Width + x;
            if (_nurseryRoom[i] == room)
                return false;
            if (room && (y > GetSurfaceY(x) || _queenRoom[i] || _nutrientRoom[i] || _wasteRoom[i] || _dewRoom[i]))
                return false;

            _nurseryRoom[i] = room;
            NurseryRoomCount += room ? 1 : -1;
            _chunkNurseryRoom[ChunkOf(i)] += room ? 1 : -1;
            MarkVersion++;
            return true;
        }

        /// <summary>
        /// Pinta/apaga a área de resíduos. Como o berçário: só células do formigueiro (no solo original ou abaixo), sólidas ou
        /// não, e nunca por cima de outra sala. Apagar não tira o resíduo já depositado (ele termina de se decompor).
        /// </summary>
        public bool SetWasteRoom(int x, int y, bool room)
        {
            if (!InBounds(x, y))
                return false;

            int i = y * Width + x;
            if (_wasteRoom[i] == room)
                return false;
            if (room && (y > GetSurfaceY(x) || _queenRoom[i] || _nutrientRoom[i] || _nurseryRoom[i] || _dewRoom[i]))
                return false;

            _wasteRoom[i] = room;
            WasteRoomCount += room ? 1 : -1;
            _chunkWasteRoom[ChunkOf(i)] += room ? 1 : -1;
            MarkVersion++;
            return true;
        }

        /// <summary>
        /// Pinta/apaga a sala Gotas de Orvalho. Como o berçário: só células do formigueiro (no solo original ou abaixo), sólidas
        /// ou não, e nunca por cima de outra sala. A terra fofa (<see cref="CellMaterial.Spoil"/>) dentro dela fica presa onde a
        /// formiga a pôs (<see cref="GranularSimulation"/>) e a água dela não é absorvida (<see cref="WaterSimulation"/>).
        /// </summary>
        public bool SetDewRoom(int x, int y, bool room)
        {
            if (!InBounds(x, y))
                return false;

            int i = y * Width + x;
            if (_dewRoom[i] == room)
                return false;
            if (room && (y > GetSurfaceY(x) || _queenRoom[i] || _nutrientRoom[i] || _nurseryRoom[i] || _wasteRoom[i]))
                return false;

            _dewRoom[i] = room;
            DewRoomCount += room ? 1 : -1;
            _chunkDewRoom[ChunkOf(i)] += room ? 1 : -1;
            MarkVersion++;
            // A terra da célula passa a ter (ou deixa de ter) a física presa: acorda o bloco para a granular rever.
            Touch(x, y);
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
            if (room && (y > GetSurfaceY(x) || _queenRoom[i] || _nurseryRoom[i] || _wasteRoom[i] || _dewRoom[i]))
                return false;

            _nutrientRoom[i] = room;
            NutrientRoomCount += room ? 1 : -1;
            _chunkNutrientRoom[ChunkOf(i)] += room ? 1 : -1;
            NutrientVersion++;
            // Canteiro fora da sala não produz: perde a planta e a água e fica só a terra (a colônia a recolhe e reaproveita).
            if (!room && _bedStage[i] != BedStage.None)
            {
                if (_bedStage[i] == BedStage.Growing)
                    GrowingBedCount--;
                _bedStage[i] = BedStage.Soil;
                _bedGrow[i] = 0f;
            }
            return true;
        }

        /// <summary>
        /// Põe ou retira um nutriente pronto. Só célula sem material aceita (a sala não exige: o nutriente continua lá se a
        /// pintura for apagada). Devolve false se nada mudou.
        /// </summary>
        public bool SetNutrient(int x, int y, bool nutrient)
        {
            if (!InBounds(x, y))
                return false;
            int i = y * Width + x;
            if (_nutrient[i] == nutrient || (nutrient && _cells[i] != CellMaterial.Empty))
                return false;
            SetNutrientAt(i, nutrient);
            return true;
        }

        void SetNutrientAt(int index, bool nutrient)
        {
            _nutrient[index] = nutrient;
            NutrientCount += nutrient ? 1 : -1;
            _chunkNutrient[ChunkOf(index)] += nutrient ? 1 : -1;
            NutrientVersion++;
        }

        // ---------------------------------------------------------------- canteiros

        /// <summary>
        /// Muda a etapa do canteiro da célula. Só terra fofa (<see cref="CellMaterial.Spoil"/>) vira canteiro;
        /// <see cref="BedStage.None"/> o desfaz. Em <see cref="BedStage.Growing"/>, brota depois de
        /// <paramref name="growSeconds"/> (<see cref="AdvanceBedGrowth"/>). Devolve false se nada mudou.
        /// </summary>
        public bool SetBed(int x, int y, BedStage stage, float growSeconds = 0f)
        {
            if (!InBounds(x, y))
                return false;
            int i = y * Width + x;
            if (stage == BedStage.None)
            {
                if (_bedStage[i] == BedStage.None)
                    return false;
                RemoveBedAt(i);
                return true;
            }
            if (_cells[i] != CellMaterial.Spoil)
                return false;
            if (_bedStage[i] == BedStage.None)
                _beds.Add(i);
            else if (_bedStage[i] == BedStage.Growing)
                GrowingBedCount--;
            _bedStage[i] = stage;
            _bedGrow[i] = stage == BedStage.Growing ? Mathf.Max(0f, growSeconds) : 0f;
            if (stage == BedStage.Growing)
                GrowingBedCount++;
            NutrientVersion++;
            return true;
        }

        /// <summary>O canteiro de índice <paramref name="k"/> (0 a <see cref="BedCount"/>-1) e sua célula.</summary>
        public bool TryGetBed(int k, out int x, out int y)
        {
            if (k < 0 || k >= _beds.Count)
            {
                x = y = 0;
                return false;
            }
            int i = _beds[k];
            x = i % Width;
            y = i / Width;
            return true;
        }

        /// <summary>
        /// Avança os canteiros regados. O que termina de brotar põe um nutriente pronto em cada um dos 4 lados livres
        /// (célula sem material e sem nutriente; lado bloqueado não gera) e volta a <see cref="BedStage.Soil"/>: a terra
        /// fica, a planta e a água se gastam. Devolve quantos nutrientes nasceram.
        /// </summary>
        public int AdvanceBedGrowth(float deltaTime, float rate = 1f)
        {
            if (GrowingBedCount == 0 || deltaTime * rate <= 0f)
                return 0;
            deltaTime *= rate;
            int born = 0;
            for (int k = 0; k < _beds.Count; k++)
            {
                int i = _beds[k];
                if (_bedStage[i] != BedStage.Growing)
                    continue;
                _bedGrow[i] -= deltaTime;
                if (_bedGrow[i] <= 0f)
                    born += FinishBed(i);
            }
            return born;
        }

        /// <summary>Adianta o canteiro (o bônus de quem cuida dele); se terminar, brota na hora. Devolve os nutrientes que nasceram.</summary>
        public int ShaveBedGrowth(int x, int y, float seconds)
        {
            if (!InBounds(x, y) || seconds <= 0f)
                return 0;
            int i = y * Width + x;
            if (_bedStage[i] != BedStage.Growing)
                return 0;
            _bedGrow[i] -= seconds;
            return _bedGrow[i] <= 0f ? FinishBed(i) : 0;
        }

        /// <summary>Lados do canteiro onde um nutriente pode nascer (célula da área ativa, sem material e sem nutriente).</summary>
        public int FreeBedSides(int x, int y)
        {
            int free = 0;
            for (int k = 0; k < 4; k++)
            {
                int nx = x + SideDx[k], ny = y + SideDy[k];
                if (InBounds(nx, ny) && _cells[ny * Width + nx] == CellMaterial.Empty && !_nutrient[ny * Width + nx])
                    free++;
            }
            return free;
        }

        /// <summary>Os 4 lados do canteiro, na ordem esquerda, direita, cima, baixo.</summary>
        public static readonly int[] SideDx = { -1, 1, 0, 0 };
        public static readonly int[] SideDy = { 0, 0, 1, -1 };

        int FinishBed(int index)
        {
            int x = index % Width, y = index / Width;
            int born = 0;
            for (int k = 0; k < 4; k++)
            {
                int nx = x + SideDx[k], ny = y + SideDy[k];
                if (!InBounds(nx, ny))
                    continue;
                int n = ny * Width + nx;
                if (_cells[n] != CellMaterial.Empty || _nutrient[n])
                    continue;
                SetNutrientAt(n, true);
                born++;
            }
            _bedStage[index] = BedStage.Soil;
            _bedGrow[index] = 0f;
            GrowingBedCount--;
            NutrientVersion++;
            return born;
        }

        void RemoveBedAt(int index)
        {
            if (_bedStage[index] == BedStage.Growing)
                GrowingBedCount--;
            _bedStage[index] = BedStage.None;
            _bedGrow[index] = 0f;
            _beds.Remove(index);
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
            _built[to] = _built[from];
            _built[from] = false;
            if (_buildMarked[to])
                ClearBuildMarkAt(to);
            if (_nutrient[to])
                SetNutrientAt(to, false);
            // A física não move canteiro (GranularSimulation); se algo mover a terra, o canteiro não vai junto.
            if (_bedStage[from] != BedStage.None)
                RemoveBedAt(from);
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
            WaterVersion++;
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

        /// <summary>Quantas células pintadas como berçário há no bloco.</summary>
        public int GetChunkNurseryRoomCount(int cx, int cy) => _chunkNurseryRoom[cy * ChunkColumns + cx];

        public int GetChunkWasteRoomCount(int cx, int cy) => _chunkWasteRoom[cy * ChunkColumns + cx];

        public int GetChunkDewRoomCount(int cx, int cy) => _chunkDewRoom[cy * ChunkColumns + cx];

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
            // Marca de planta muda onde dá para pisar (AntNavigation.TouchesSolid).
            if (CellMaterials.IsPlant(_cells[index]))
                Touch(index % Width, index / Width);
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
