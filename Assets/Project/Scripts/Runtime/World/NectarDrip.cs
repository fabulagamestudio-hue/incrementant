using System;
using UnityEngine;

namespace Fabula.Incrementant.World
{
    [Serializable]
    public sealed class NectarSettings
    {
        [Tooltip("Segundos (tempo simulado) entre duas gotas de néctar de cada flor inteira do Canteiro de flores. 0 desliga.")]
        [SerializeField, Min(0f)] float _secondsPerDrop = 30f;
        [Tooltip("Gotas no chão em volta de uma flor a partir das quais ela para de soltar outra.")]
        [SerializeField, Min(1)] int _maxPerFlower = 2;
        [Tooltip("Até quantas colunas da raiz da flor a gota pode cair.")]
        [SerializeField, Min(1)] int _reach = 6;

        public float SecondsPerDrop => _secondsPerDrop;
        public int MaxPerFlower => _maxPerFlower;
        public int Reach => _reach;
    }

    /// <summary>
    /// Néctar, o insumo do Canteiro de flores (documento 44, decisão do desenvolvedor em 2026-10-23: "gotas que caem").
    /// A cada <see cref="NectarSettings.SecondsPerDrop"/>, cada flor crescida por inteiro (sem linha enterrada e com a ponta)
    /// solta uma gota no chão ao pé dela, como item guardado (<see cref="StoredItems.Nectar"/>), até
    /// <see cref="NectarSettings.MaxPerFlower"/> gotas em volta. A flor não perde nada; as formigas levam a gota ao
    /// depósito de néctar, e sem ele a gota fica onde caiu.
    /// </summary>
    public sealed class NectarDrip
    {
        readonly NectarSettings _settings;
        readonly System.Random _random;
        float _timer;

        public NectarDrip(NectarSettings settings, int seed)
        {
            _settings = settings ?? new NectarSettings();
            _random = new System.Random(seed ^ 0x2EC7A);
        }

        /// <summary>Gotas que as flores soltaram desde o começo do mapa (não vai para o save).</summary>
        public int Dropped { get; private set; }

        public void Step(PixelGrid grid, VegetationSimulation vegetation, float deltaTime)
        {
            float period = _settings.SecondsPerDrop;
            if (period <= 0f || vegetation == null)
                return;
            _timer += deltaTime;
            while (_timer >= period)
            {
                _timer -= period;
                foreach (int root in vegetation.Plants)
                    if (TryDrop(grid, vegetation, root))
                        Dropped++;
            }
        }

        /// <summary>A flor com raiz em <paramref name="root"/> solta uma gota, se está inteira e cabe mais uma. Público para os testes.</summary>
        public bool TryDrop(PixelGrid grid, VegetationSimulation vegetation, int root)
        {
            if (!IsWholeFlower(grid, vegetation, root))
                return false;
            int reach = _settings.Reach;
            if (CountAround(grid, root, reach + 2) >= _settings.MaxPerFlower)
                return false;
            // Uma coluna sorteada perto da raiz; se não serve, as vizinhas para fora.
            int first = _random.Next(1, reach + 1) * (_random.Next(2) == 0 ? -1 : 1);
            for (int i = 0; i < 2 * reach; i++)
            {
                int offset = first + (i % 2 == 0 ? i / 2 : -(i / 2 + 1)) * Math.Sign(first);
                if (offset == 0 || Math.Abs(offset) > reach)
                    continue;
                int x = root + offset;
                if (TryGroundCell(grid, x, out int y) && grid.SetStored(x, y, StoredItems.Nectar))
                    return true;
            }
            return false;
        }

        /// <summary>Flor crescida por inteiro: nenhuma linha enterrada e a linha de cima da forma ainda no lugar.</summary>
        static bool IsWholeFlower(PixelGrid grid, VegetationSimulation vegetation, int root)
        {
            if (!vegetation.TryGetPlant(root, out PlantKind kind, out PlantShape shape, out _) || kind != PlantKind.Flower ||
                vegetation.GetSunkRows(root) != 0)
                return false;
            int rootY = grid.GetSurfaceY(root) + 1;
            foreach (Vector2Int c in shape.Cells)
                if (c.y == shape.Height - 1 && CellMaterials.IsPlant(grid.Get(root + c.x, rootY + c.y)))
                    return true;
            return false;
        }

        /// <summary>Primeira célula vazia sobre chão firme, a partir do solo original: onde a gota pousa.</summary>
        static bool TryGroundCell(PixelGrid grid, int x, out int y)
        {
            int surface = grid.GetSurfaceY(x);
            for (y = surface + 1; y <= surface + 4; y++)
            {
                if (!grid.InBounds(x, y) || grid.IsSolid(x, y))
                    continue;
                CellMaterial below = grid.Get(x, y - 1);
                return CellMaterials.IsSolid(below) && !CellMaterials.IsAnyPlant(below) && !grid.HasWater(x, y) &&
                       grid.GetStored(x, y) == StoredItems.None;
            }
            return false;
        }

        static int CountAround(PixelGrid grid, int root, int reach)
        {
            int count = 0;
            for (int x = root - reach; x <= root + reach; x++)
            {
                int surface = grid.GetSurfaceY(x);
                for (int y = surface - 1; y <= surface + 5; y++)
                    if (grid.InBounds(x, y) && grid.GetStored(x, y) == StoredItems.Nectar)
                        count++;
            }
            return count;
        }
    }
}
