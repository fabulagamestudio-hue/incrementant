using UnityEngine;

namespace Fabula.Incrementant.World
{
    /// <summary>
    /// Depósito pintado pelo jogador (pedido do desenvolvedor em 2026-10-19). Valor gravado no save: não renumerar.
    /// <see cref="Waste"/> não é pintado por esta camada (a área de resíduos tem a dela); só classifica o item guardado.
    /// </summary>
    public enum StorageKind : byte
    {
        None = 0,
        /// <summary>Alimentos: pedaços de planta cortados.</summary>
        Food = 1,
        /// <summary>Construção: blocos de terra cavados.</summary>
        Blocks = 2,
        /// <summary>Água: blocos de água tirados do mapa.</summary>
        Water = 3,
        /// <summary>Resíduo depositado na área de resíduos (entulho e resto de cupim).</summary>
        Waste = 4,
        /// <summary>Areia: a comida dos pulgões da fazenda (pedido do desenvolvedor em 2026-10-22).</summary>
        Sand = 5,
        /// <summary>
        /// Cogumelos luminosos colhidos dos pulgões, o material do pincel Cogumelo luminoso (pedido do desenvolvedor em
        /// 2026-10-22: depósito separado do de construção).
        /// </summary>
        Mushroom = 6,
        /// <summary>Serapilheira cavada no bioma Serapilheira, o insumo único dele (REQ-070, 2026-10-23).</summary>
        Litter = 7,
        /// <summary>Néctar que as flores do Canteiro de flores soltam, o insumo único dele (REQ-070, 2026-10-23).</summary>
        Nectar = 8,
        /// <summary>Minério cavado dos veios: cobre, ferro e ouro (documento 45).</summary>
        Ore = 9,
        /// <summary>
        /// Fundição (documento 45): sala pintada como depósito. Guarda minério e serapilheira (a entrada) e, depois de fundidos,
        /// as barras (a saída), que vão à forja.
        /// </summary>
        Smelter = 10,
        /// <summary>Forja: guarda as barras (a entrada) e, depois de forjados, os equipamentos (a saída), que vão aos armazéns.</summary>
        Forge = 11,
        /// <summary>Armazém de ferramentas e armas.</summary>
        Armory = 12,
        /// <summary>Armazém de roupas e armaduras.</summary>
        Wardrobe = 13,
    }

    /// <summary>Item guardado numa célula (<see cref="PixelGrid.GetStored"/>): o valor do material, ou <see cref="Water"/>.</summary>
    public static class StoredItems
    {
        public const byte None = 0;
        /// <summary>Bloco de água guardado (não é material da grade).</summary>
        public const byte Water = 255;
        /// <summary>Grão de areia guardado (depósito de areia, cocho dos pulgões, saída da área de resíduos).</summary>
        public const byte Sand = (byte)CellMaterial.Sand;
        /// <summary>
        /// Cogumelo luminoso colhido dos pulgões: material do pincel Cogumelo luminoso. Fica no depósito de cogumelos (ou
        /// solto onde a ordenha o deixou, sem ele).
        /// </summary>
        public const byte Mushroom = (byte)CellMaterial.GlowMushroom;
        /// <summary>Serapilheira guardada no depósito de serapilheira.</summary>
        public const byte Litter = (byte)CellMaterial.Litter;
        /// <summary>Gota de néctar: no chão, ao pé da flor que a soltou, ou no depósito de néctar.</summary>
        public const byte Nectar = (byte)CellMaterial.Nectar;

        public static byte Of(CellMaterial material) => (byte)material;

        public static CellMaterial MaterialOf(byte item) => item == Water ? CellMaterial.Empty : (CellMaterial)item;

        /// <summary>A que depósito o item pertence.</summary>
        public static StorageKind KindOf(byte item)
        {
            if (item == None)
                return StorageKind.None;
            if (item == Water)
                return StorageKind.Water;
            CellMaterial m = (CellMaterial)item;
            if (CellMaterials.IsPlant(m))
                return StorageKind.Food;
            if (m == CellMaterial.Sand)
                return StorageKind.Sand;
            if (m == CellMaterial.GlowMushroom)
                return StorageKind.Mushroom;
            if (m == CellMaterial.Litter)
                return StorageKind.Litter;
            if (m == CellMaterial.Nectar)
                return StorageKind.Nectar;
            if (CellMaterials.IsOre(m))
                return StorageKind.Ore;
            if (Equipment.IsBar(m))
                return StorageKind.Forge;
            switch (Equipment.KindOf(m))
            {
                case EquipKind.Tool:
                case EquipKind.Weapon: return StorageKind.Armory;
                case EquipKind.Armor: return StorageKind.Wardrobe;
            }
            return CellMaterials.IsDepositedWaste(m) ? StorageKind.Waste : StorageKind.Blocks;
        }
    }

    /// <summary>
    /// Depósitos e itens guardados. O item é uma camada à parte, como o nutriente: fica numa célula sem material, não
    /// bloqueia a passagem e não muda a estrutura. Material sólido entrando na célula (grão caindo, obra) destrói o item.
    /// Preenchimento vertical (REQ-072, etapa 2): a célula guarda uma pilha de itens iguais (<see cref="GetStoredAmount"/>),
    /// até a capacidade do depósito em que está (<see cref="CellCapacity"/>, 1 por padrão); cada item ocupa 1/capacidade da
    /// altura da célula (<see cref="GetStoredFill"/>). Com a capacidade subindo de 1 para 2 (melhoria de espaço do depósito),
    /// o item que enchia a célula passa a ocupar metade dela, e outro igual cabe em cima.
    /// </summary>
    public sealed partial class PixelGrid
    {
        const int StorageKinds = 14;
        /// <summary>Maior capacidade por célula (8 = um item por linha de texel da célula).</summary>
        public const int MaxItemsPerCell = 8;
        byte[] _storageRoom;
        byte[] _stored;
        byte[] _storedShade;
        // Itens da pilha da célula (0 sem item; o tom é o do primeiro).
        byte[] _storedAmount;
        // Fator de capacidade de cada depósito (índice = StorageKind; 0 = fora de depósito, sempre 1).
        readonly float[] _capacityFactor = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
        const float CapacityEpsilon = 0.0001f;
        readonly int[] _storageRoomCount = new int[StorageKinds];
        readonly int[] _storedCount = new int[StorageKinds];
        int[] _chunkStorageRoom;
        int[] _chunkStored;

        void InitStorage(int cells, int chunks)
        {
            _storageRoom = new byte[cells];
            _stored = new byte[cells];
            _storedShade = new byte[cells];
            _storedAmount = new byte[cells];
            _chunkStorageRoom = new int[chunks];
            _chunkStored = new int[chunks];
        }

        /// <summary>Muda quando um depósito é pintado/apagado ou um item é guardado, retirado ou perdido.</summary>
        public int StoreVersion { get; private set; }

        /// <summary>Depósito pintado na célula (<see cref="StorageKind.None"/> = nenhum).</summary>
        public StorageKind GetStorageRoom(int x, int y) => InBounds(x, y) ? (StorageKind)_storageRoom[y * Width + x] : StorageKind.None;

        /// <summary>Células pintadas como depósito do tipo dado.</summary>
        public int StorageRoomCount(StorageKind kind) => _storageRoomCount[(int)kind];

        /// <summary>Células pintadas como algum depósito no bloco.</summary>
        public int GetChunkStorageRoomCount(int cx, int cy) => _chunkStorageRoom[cy * ChunkColumns + cx];

        /// <summary>
        /// Depósitos cuja célula pode empilhar itens: os que só guardam e entregam. A fundição e a forja transformam o item na
        /// própria célula (entrada vira saída) e os armazéns vestem as formigas; ficam com um item por célula.
        /// </summary>
        public static bool CanStack(StorageKind kind) =>
            kind == StorageKind.Food || kind == StorageKind.Blocks || kind == StorageKind.Water || kind == StorageKind.Sand ||
            kind == StorageKind.Mushroom || kind == StorageKind.Litter || kind == StorageKind.Nectar || kind == StorageKind.Ore;

        /// <summary>Maior quantidade de itens iguais que uma célula do depósito comporta (1 sem a melhoria de espaço).</summary>
        public int ItemsPerCell(StorageKind kind) => Mathf.Min(MaxItemsPerCell, Mathf.CeilToInt(_capacityFactor[(int)kind] - CapacityEpsilon));

        /// <summary>Fator de capacidade do depósito (1 = um item por célula; 1,5 = metade das células guarda dois).</summary>
        public float CapacityFactor(StorageKind kind) => _capacityFactor[(int)kind];

        /// <summary>
        /// Itens que a célula (x, y) comporta num depósito do tipo dado. Com fator fracionário a capacidade varia de célula
        /// em célula, em diagonal, de modo que a média do depósito seja o fator: 1,5 alterna 1 e 2; 1,25 dá 2 em uma de
        /// cada quatro. A conta depende só da posição, então não muda ao pintar ou apagar células vizinhas.
        /// </summary>
        public int CellCapacity(int x, int y, StorageKind kind)
        {
            float factor = _capacityFactor[(int)kind];
            if (factor <= 1f + CapacityEpsilon)
                return 1;
            int s = x + y;
            int capacity = Mathf.FloorToInt(factor * (s + 1) + CapacityEpsilon) - Mathf.FloorToInt(factor * s + CapacityEpsilon);
            return Mathf.Clamp(capacity, 1, MaxItemsPerCell);
        }

        /// <summary>
        /// Muda a capacidade por célula de todos os depósitos que empilham (<see cref="CanStack"/>): a melhoria de espaço
        /// (nó D1, "Pilhas organizadas"). O que já está guardado não sai: a pilha só passa a ocupar outra altura. Devolve false
        /// se nada mudou.
        /// </summary>
        public bool SetCapacityFactor(float factor)
        {
            bool changed = false;
            for (int k = 1; k < StorageKinds; k++)
                changed |= SetCapacityFactor((StorageKind)k, factor);
            return changed;
        }

        /// <summary>Como <see cref="SetCapacityFactor(float)"/>, de um depósito só (de 1 a <see cref="MaxItemsPerCell"/>).</summary>
        public bool SetCapacityFactor(StorageKind kind, float factor)
        {
            if (!CanStack(kind))
                return false;
            float value = Mathf.Clamp(factor, 1f, MaxItemsPerCell);
            if (Mathf.Abs(_capacityFactor[(int)kind] - value) < CapacityEpsilon)
                return false;
            _capacityFactor[(int)kind] = value;
            // A altura de cada item guardado nesse depósito muda: o render redesenha as células com item.
            for (int i = 0; i < _stored.Length; i++)
                if (_stored[i] != StoredItems.None && _storageRoom[i] == (byte)kind)
                    NoteRender(i);
            StoreVersion++;
            return true;
        }

        /// <summary>Capacidade inteira por célula, para depósitos com fator inteiro (a mesma de <see cref="SetCapacityFactor(StorageKind, float)"/>).</summary>
        public bool SetItemsPerCell(StorageKind kind, int count) => SetCapacityFactor(kind, count);

        // A capacidade do depósito só vale para os itens dele: item solto num depósito de outro tipo fica sozinho na célula.
        int CapacityFor(int index, byte item) =>
            StoredItems.KindOf(item) == (StorageKind)_storageRoom[index] ? CellCapacity(index % Width, index / Width, (StorageKind)_storageRoom[index]) : 1;

        /// <summary>Itens na pilha da célula (0 = nenhum).</summary>
        public int GetStoredAmount(int x, int y) => InBounds(x, y) ? _storedAmount[y * Width + x] : 0;

        /// <summary>Quanto da altura da célula a pilha ocupa, de 0 a 1 (cada item ocupa 1/<see cref="CellCapacity"/>).</summary>
        public float GetStoredFill(int x, int y)
        {
            if (!InBounds(x, y))
                return 0f;
            int i = y * Width + x;
            return _storedAmount[i] == 0 ? 0f : Mathf.Min(1f, _storedAmount[i] / (float)CapacityFor(i, _stored[i]));
        }

        /// <summary>
        /// Quantos itens <paramref name="item"/> ainda cabem na célula: a capacidade inteira se ela está livre, o que falta na
        /// pilha se ela já guarda o mesmo item, 0 se guarda outro (só itens iguais dividem a célula) ou não aceita item.
        /// </summary>
        public int StoredRoomFor(int x, int y, byte item)
        {
            if (!InBounds(x, y) || item == StoredItems.None)
                return 0;
            int i = y * Width + x;
            if (_cells[i] != CellMaterial.Empty || _nutrient[i])
                return 0;
            if (_stored[i] == StoredItems.None)
                return CapacityFor(i, item);
            return _stored[i] == item ? Mathf.Max(0, CapacityFor(i, item) - _storedAmount[i]) : 0;
        }

        /// <summary>A célula ainda tem espaço na pilha (vazia ou com item que não a enche); não diz qual item cabe.</summary>
        public bool HasStoredRoom(int x, int y)
        {
            if (!InBounds(x, y))
                return false;
            int i = y * Width + x;
            return _cells[i] == CellMaterial.Empty && !_nutrient[i] &&
                   (_stored[i] == StoredItems.None || _storedAmount[i] < CapacityFor(i, _stored[i]));
        }

        /// <summary>Itens guardados (em qualquer lugar) do tipo dado.</summary>
        public int StoredCount(StorageKind kind) => _storedCount[(int)kind];

        /// <summary>Itens guardados no bloco.</summary>
        public int GetChunkStoredCount(int cx, int cy) => _chunkStored[cy * ChunkColumns + cx];

        /// <summary>Item guardado na célula (<see cref="StoredItems.None"/> = nenhum).</summary>
        public byte GetStored(int x, int y) => InBounds(x, y) ? _stored[y * Width + x] : StoredItems.None;

        public byte GetStoredShade(int x, int y) => InBounds(x, y) ? _storedShade[y * Width + x] : (byte)0;

        internal bool HasStoredAt(int index) => _stored[index] != StoredItems.None;

        /// <summary>
        /// Pinta/apaga um depósito. Como as outras salas: só células do formigueiro (no solo original ou abaixo), sólidas ou
        /// não, e nunca por cima de outra sala ou de outro depósito. Apagar não tira o que está guardado: o item fica solto e
        /// as formigas o levam a um depósito do tipo dele (ou ao destino de sempre). Devolve false se nada mudou.
        /// </summary>
        public bool SetStorageRoom(int x, int y, StorageKind kind, bool room)
        {
            if (!InBounds(x, y) || kind == StorageKind.None || kind == StorageKind.Waste)
                return false;
            int i = y * Width + x;
            byte current = _storageRoom[i];
            if (!room)
            {
                if (current != (byte)kind)
                    return false;
                ChangeStorageRoom(i, StorageKind.None);
                return true;
            }
            if (current != 0 || y > GetSurfaceY(x) || _queenRoom[i] || _nutrientRoom[i] || _nurseryRoom[i] || _wasteRoom[i] || _dewRoom[i] || _farmRoom[i])
                return false;
            ChangeStorageRoom(i, kind);
            return true;
        }

        void ChangeStorageRoom(int index, StorageKind kind)
        {
            StorageKind old = (StorageKind)_storageRoom[index];
            if (old != StorageKind.None)
            {
                _storageRoomCount[(int)old]--;
                _chunkStorageRoom[ChunkOf(index)]--;
            }
            _storageRoom[index] = (byte)kind;
            DirtyRender(index);
            if (kind != StorageKind.None)
            {
                _storageRoomCount[(int)kind]++;
                _chunkStorageRoom[ChunkOf(index)]++;
            }
            MarkVersion++;
            StoreVersion++;
        }

        /// <summary>A célula já é de algum depósito (as outras salas não pintam por cima).</summary>
        bool IsStorageAt(int index) => _storageRoom[index] != 0;

        /// <summary>
        /// Guarda um item (ou tira um, com <see cref="StoredItems.None"/>). Só célula sem material e sem nutriente aceita
        /// guardar, e só se estiver livre ou com uma pilha do mesmo item que ainda não chegou à capacidade do depósito; tirar
        /// leva o item de cima da pilha. Devolve false se não aceitou.
        /// </summary>
        public bool SetStored(int x, int y, byte item, byte shade = 128)
        {
            if (!InBounds(x, y))
                return false;
            int i = y * Width + x;
            if (item == StoredItems.None)
            {
                if (_stored[i] == StoredItems.None)
                    return false;
                SetStoredAt(i, _stored[i], _storedShade[i], _storedAmount[i] - 1);
                return true;
            }
            if (StoredRoomFor(x, y, item) <= 0)
                return false;
            bool first = _stored[i] == StoredItems.None;
            SetStoredAt(i, item, first ? shade : _storedShade[i], _storedAmount[i] + 1);
            return true;
        }

        /// <summary>Tira a pilha inteira da célula. Devolve false se não havia item.</summary>
        public bool ClearStored(int x, int y)
        {
            if (!InBounds(x, y) || _stored[y * Width + x] == StoredItems.None)
                return false;
            SetStoredAt(y * Width + x, StoredItems.None, 0, 0);
            return true;
        }

        /// <summary>
        /// Põe a pilha como estava no save, sem olhar a capacidade (ela vem das melhorias, que podem ainda não ter sido
        /// aplicadas): uma pilha maior que a capacidade continua inteira e só não aceita mais itens.
        /// </summary>
        public bool RestoreStored(int x, int y, byte item, byte shade, int amount)
        {
            if (!InBounds(x, y))
                return false;
            int i = y * Width + x;
            if (item == StoredItems.None || amount <= 0)
                return ClearStored(x, y);
            if (_cells[i] != CellMaterial.Empty || _nutrient[i])
                return false;
            SetStoredAt(i, item, shade, Mathf.Min(amount, MaxItemsPerCell));
            return true;
        }

        void SetStoredAt(int index, byte item, byte shade, int amount)
        {
            byte old = _stored[index];
            if (old != StoredItems.None)
            {
                _storedCount[(int)StoredItems.KindOf(old)] -= _storedAmount[index];
                _chunkStored[ChunkOf(index)] -= _storedAmount[index];
            }
            if (item == StoredItems.None || amount <= 0)
            {
                item = StoredItems.None;
                amount = 0;
            }
            _stored[index] = item;
            _storedAmount[index] = (byte)amount;
            DirtyRender(index);
            _storedShade[index] = item == StoredItems.None ? (byte)0 : shade;
            if (item != StoredItems.None)
            {
                _storedCount[(int)StoredItems.KindOf(item)] += amount;
                _chunkStored[ChunkOf(index)] += amount;
            }
            StoreVersion++;
        }

        /// <summary>
        /// Avisa que a regra de quais itens são soltos mudou sem nenhum item mudar (modo da área de resíduos): os campos dos
        /// itens são refeitos na próxima rodada.
        /// </summary>
        public void TouchStore() => StoreVersion++;

        /// <summary>Material sólido entrou na célula: o item guardado ali se perde.</summary>
        void LoseStoredAt(int index)
        {
            if (_stored[index] != StoredItems.None)
                SetStoredAt(index, StoredItems.None, 0, 0);
        }
    }
}
