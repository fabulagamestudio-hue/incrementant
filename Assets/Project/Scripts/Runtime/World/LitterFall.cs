using System;
using UnityEngine;

namespace Fabula.Incrementant.World
{
    [Serializable]
    public sealed class LitterSettings
    {
        [Tooltip("Segundos (tempo simulado) entre duas folhas que voltam a cobrir o chão da Serapilheira. 0 desliga.")]
        [SerializeField, Min(0f)] float _secondsPerCell = 4f;
        [Tooltip("Espessura da cobertura de folhas, em células a partir do solo original (a mesma da grama do gerador).")]
        [SerializeField, Range(1, 4)] int _layerDepth = 2;

        public float SecondsPerCell => _secondsPerCell;
        public int LayerDepth => _layerDepth;
    }

    /// <summary>
    /// Folhas que voltam a cobrir o chão da Serapilheira (documento 44): o insumo do bioma não se esgota de vez. A cada
    /// <see cref="LitterSettings.SecondsPerCell"/>, uma coluna sorteada do bioma recupera uma célula da cobertura, de baixo
    /// para cima e só até o solo original. Só repõe onde a terra embaixo continua: não tapa poço, túnel nem buraco aberto,
    /// não cobre item, água, marca ou sala, e não cai em cima de formiga.
    /// </summary>
    public sealed class LitterFall
    {
        const int Attempts = 16;

        readonly LitterSettings _settings;
        readonly System.Random _random;
        float _timer;

        public LitterFall(LitterSettings settings, int seed)
        {
            _settings = settings ?? new LitterSettings();
            _random = new System.Random(seed ^ 0x11773);
        }

        /// <summary>Células de cobertura repostas desde o começo do mapa (não vai para o save).</summary>
        public int Restored { get; private set; }

        /// <param name="occupied">Verdadeiro se há formiga (ou rainha) na célula: ali não cai folha.</param>
        public void Step(PixelGrid grid, float deltaTime, Func<int, int, bool> occupied = null)
        {
            float period = _settings.SecondsPerCell;
            if (period <= 0f)
                return;
            _timer += deltaTime;
            while (_timer >= period)
            {
                _timer -= period;
                TryRestoreOne(grid, occupied);
            }
        }

        bool TryRestoreOne(PixelGrid grid, Func<int, int, bool> occupied)
        {
            RectInt area = grid.Active;
            SurfaceBiomes biomes = grid.Biomes;
            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                int x = _random.Next(area.xMin + 1, area.xMax - 1);
                if (biomes.At(x) != SurfaceBiome.LeafLitter)
                    continue;
                if (TryRestoreAt(grid, x, _settings.LayerDepth, occupied))
                {
                    Restored++;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Repõe a célula mais baixa que falta na cobertura da coluna, se ela pode voltar. Público para os testes.</summary>
        public static bool TryRestoreAt(PixelGrid grid, int x, int layerDepth, Func<int, int, bool> occupied = null)
        {
            int surface = grid.GetSurfaceY(x);
            int bottom = surface - Mathf.Max(1, layerDepth) + 1;
            if (surface < 0 || !grid.InBounds(x, bottom - 1) || !grid.InBounds(x, surface + 1))
                return false;
            // Acima do solo original só céu: monte de terra despejada, obra ou planta em cima seguram a cobertura.
            if (grid.IsSolid(x, surface + 1))
                return false;
            for (int y = bottom; y <= surface; y++)
            {
                if (grid.IsSolid(x, y))
                    continue;
                // A primeira célula que falta: só volta apoiada em terra, e com tudo acima dela livre até o solo original.
                if (!grid.IsSolid(x, y - 1))
                    return false;
                for (int above = y + 1; above <= surface; above++)
                    if (grid.IsSolid(x, above))
                        return false;
                if (!CanCover(grid, x, y) || (occupied != null && occupied(x, y)))
                    return false;
                grid.Set(x, y, CellMaterial.Litter, TerrainMapCodec.ShadeAt(x, y, surface));
                return true;
            }
            return false;
        }

        static bool CanCover(PixelGrid grid, int x, int y) =>
            !grid.HasWater(x, y) && !grid.IsMarked(x, y) && grid.GetStored(x, y) == StoredItems.None && !grid.HasNutrient(x, y) &&
            !IsRoomAt(grid, x, y);

        static bool IsRoomAt(PixelGrid grid, int x, int y) =>
            grid.IsQueenRoom(x, y) || grid.IsNutrientRoom(x, y) || grid.IsNurseryRoom(x, y) || grid.IsWasteRoom(x, y) ||
            grid.IsDewRoom(x, y) || grid.IsFarmRoom(x, y) || grid.GetStorageRoom(x, y) != StorageKind.None;
    }
}
