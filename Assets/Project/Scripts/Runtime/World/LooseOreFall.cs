using System;
using UnityEngine;

namespace Fabula.Incrementant.World
{
    [Serializable]
    public sealed class OreFallSettings
    {
        [Tooltip("Segundos (tempo simulado) para o minério solto cair 1 célula. 0 desliga a queda.")]
        [SerializeField, Min(0f)] float _secondsPerCell = 0.06f;

        public float SecondsPerCell => _secondsPerCell;
    }

    /// <summary>
    /// Minério solto cai (pedido do desenvolvedor em 2026-10-23: "adicionar física quando não estiverem no depósito ou em
    /// uso"). O minério guardado fora do depósito de minério e da fundição desce 1 célula por
    /// <see cref="OreFallSettings.SecondsPerCell"/> enquanto embaixo houver vão sem item; parado em cima de outro item, escorrega
    /// para a diagonal livre, então os minérios soltos fazem monte. No depósito de minério e na fundição ele fica onde a
    /// formiga o pôs (inclusive na parede de fundo). Na mão da formiga não é item da grade, então não cai.
    /// </summary>
    public sealed class LooseOreFall
    {
        // Passos de queda no máximo por chamada, para um quadro lento não virar uma rajada.
        const int MaxPassesPerStep = 4;

        readonly OreFallSettings _settings;
        float _timer;
        int _pass;

        public LooseOreFall(OreFallSettings settings)
        {
            _settings = settings ?? new OreFallSettings();
        }

        /// <summary>Movimentos de queda desde o começo do mapa (não vai para o save).</summary>
        public int Moves { get; private set; }

        /// <summary>O item da célula é minério e está fora do depósito de minério e da fundição: cai.</summary>
        public static bool Falls(PixelGrid grid, int x, int y)
        {
            byte item = grid.GetStored(x, y);
            if (item == StoredItems.None || item == StoredItems.Water || !CellMaterials.IsOre((CellMaterial)item))
                return false;
            StorageKind room = grid.GetStorageRoom(x, y);
            return room != StorageKind.Ore && room != StorageKind.Smelter;
        }

        public void Step(PixelGrid grid, float deltaTime)
        {
            float period = _settings.SecondsPerCell;
            if (period <= 0f)
                return;
            _timer += deltaTime;
            int passes = 0;
            while (_timer >= period)
            {
                _timer -= period;
                if (passes++ < MaxPassesPerStep)
                    Pass(grid);
            }
        }

        /// <summary>Uma rodada: cada minério solto desce (ou escorrega) no máximo 1 célula, de baixo para cima.</summary>
        void Pass(PixelGrid grid)
        {
            if (grid.StoredCount(StorageKind.Ore) == 0)
                return;
            _pass++;
            RectInt area = grid.Active;
            int size = PixelGrid.ChunkSize;
            int cxMin = area.xMin / size, cxMax = (area.xMax - 1) / size;
            int cyMin = area.yMin / size, cyMax = (area.yMax - 1) / size;
            // Linha a linha de baixo para cima (só nos blocos com item): o que desceu para uma linha já vista não anda de novo.
            for (int cy = cyMin; cy <= cyMax; cy++)
            {
                int yMin = Mathf.Max(area.yMin, cy * size), yMax = Mathf.Min(area.yMax, (cy + 1) * size);
                for (int y = yMin; y < yMax; y++)
                for (int cx = cxMin; cx <= cxMax; cx++)
                {
                    if (grid.GetChunkStoredCount(cx, cy) == 0)
                        continue;
                    int xMin = Mathf.Max(area.xMin, cx * size), xMax = Mathf.Min(area.xMax, (cx + 1) * size);
                    for (int x = xMin; x < xMax; x++)
                        if (Falls(grid, x, y))
                            TryMove(grid, x, y);
                }
            }
        }

        void TryMove(PixelGrid grid, int x, int y)
        {
            if (Move(grid, x, y, x, y - 1))
                return;
            // Embaixo há item (não terra): escorrega para um lado livre, alternando o lado a cada rodada.
            if (!grid.InBounds(x, y - 1) || grid.IsSolid(x, y - 1) || grid.GetStored(x, y - 1) == StoredItems.None)
                return;
            int first = ((x + _pass) & 1) == 0 ? 1 : -1;
            for (int side = 0; side < 2; side++)
            {
                int dx = side == 0 ? first : -first;
                if (IsFree(grid, x + dx, y) && Move(grid, x, y, x + dx, y - 1))
                    return;
            }
        }

        static bool IsFree(PixelGrid grid, int x, int y) =>
            grid.InBounds(x, y) && !grid.IsSolid(x, y) && grid.GetStored(x, y) == StoredItems.None;

        bool Move(PixelGrid grid, int x, int y, int toX, int toY)
        {
            if (!IsFree(grid, toX, toY))
                return false;
            byte item = grid.GetStored(x, y);
            byte shade = grid.GetStoredShade(x, y);
            if (!grid.SetStored(toX, toY, item, shade))
                return false;
            grid.SetStored(x, y, StoredItems.None);
            Moves++;
            return true;
        }
    }
}
