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
    /// </summary>
    public sealed partial class PixelGrid
    {
        const int StorageKinds = 14;
        byte[] _storageRoom;
        byte[] _stored;
        byte[] _storedShade;
        readonly int[] _storageRoomCount = new int[StorageKinds];
        readonly int[] _storedCount = new int[StorageKinds];
        int[] _chunkStorageRoom;
        int[] _chunkStored;

        void InitStorage(int cells, int chunks)
        {
            _storageRoom = new byte[cells];
            _stored = new byte[cells];
            _storedShade = new byte[cells];
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
        /// Guarda (ou tira, com <see cref="StoredItems.None"/>) um item. Só célula sem material, sem nutriente e sem outro item
        /// aceita guardar. Devolve false se não aceitou.
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
                SetStoredAt(i, StoredItems.None, 0);
                return true;
            }
            if (_stored[i] != StoredItems.None || _cells[i] != CellMaterial.Empty || _nutrient[i])
                return false;
            SetStoredAt(i, item, shade);
            return true;
        }

        void SetStoredAt(int index, byte item, byte shade)
        {
            byte old = _stored[index];
            if (old != StoredItems.None)
            {
                _storedCount[(int)StoredItems.KindOf(old)]--;
                _chunkStored[ChunkOf(index)]--;
            }
            _stored[index] = item;
            DirtyRender(index);
            _storedShade[index] = item == StoredItems.None ? (byte)0 : shade;
            if (item != StoredItems.None)
            {
                _storedCount[(int)StoredItems.KindOf(item)]++;
                _chunkStored[ChunkOf(index)]++;
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
                SetStoredAt(index, StoredItems.None, 0);
        }
    }
}
