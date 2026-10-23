using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fabula.Incrementant.World
{
    /// <summary>
    /// Um minério na geração: em que faixa de profundidade, quantos bolsões, de que tamanho e quantos fora da faixa
    /// (documento 45).
    /// </summary>
    [Serializable]
    public sealed class OreBand
    {
        [Tooltip("Faixa principal: profundidade abaixo do solo, em células (mín, máx). Máximo 0 = até o fundo do mundo.")]
        [SerializeField] Vector2Int _depth;
        [Tooltip("Bolsões na faixa principal a cada 1000 colunas do mundo. 0 desliga.")]
        [SerializeField, Min(0f)] float _veinsPer1000Columns;
        [Tooltip("Passos da caminhada de cada bolsão (mín, máx): pouco = redondo, muito = alongado.")]
        [SerializeField] Vector2Int _length;
        [Tooltip("Raio do bolsão no começo da caminhada, em células (mín, máx). Ele afina até a metade no fim.")]
        [SerializeField] Vector2 _radius;
        [Tooltip("Puxa os bolsões para o fundo da faixa: 0 = espalhados por igual, mais = mais perto do fundo.")]
        [SerializeField, Range(0f, 4f)] float _deepBias;
        [Tooltip("Bolsões menores a cada 1000 colunas abaixo da faixa principal, até o fundo do mundo (o resto do mapa). 0 desliga.")]
        [SerializeField, Min(0f)] float _scatterPer1000Columns;

        public OreBand(Vector2Int depth, float veinsPer1000Columns, Vector2Int length, Vector2 radius,
                       float deepBias = 0f, float scatterPer1000Columns = 0f)
        {
            _depth = depth;
            _veinsPer1000Columns = veinsPer1000Columns;
            _length = length;
            _radius = radius;
            _deepBias = deepBias;
            _scatterPer1000Columns = scatterPer1000Columns;
        }

        public Vector2Int Depth => _depth;
        public float VeinsPer1000Columns => _veinsPer1000Columns;
        public Vector2Int Length => _length;
        public Vector2 Radius => _radius;
        public float DeepBias => _deepBias;
        public float ScatterPer1000Columns => _scatterPer1000Columns;
    }

    [Serializable]
    public sealed class OreSettings
    {
        [SerializeField] OreBand _copper = new OreBand(new Vector2Int(12, 140), 40f, new Vector2Int(6, 14), new Vector2(3f, 5.5f), 0f, 8f);
        [SerializeField] OreBand _iron = new OreBand(new Vector2Int(60, 300), 30f, new Vector2Int(6, 14), new Vector2(3f, 5.5f), 0f, 8f);
        [SerializeField] OreBand _gold = new OreBand(new Vector2Int(180, 0), 40f, new Vector2Int(5, 12), new Vector2(2.5f, 4.5f), 1f, 0f);
        [Tooltip("Quanto o bolsão pode virar por passo, em radianos: pouco = reto, muito = enrolado.")]
        [SerializeField, Range(0f, 1.5f)] float _turn = 0.6f;

        public OreBand Copper => _copper;
        public OreBand Iron => _iron;
        public OreBand Gold => _gold;
        public float Turn => _turn;
    }

    /// <summary>
    /// Veios de minério (documento 45) em bolsões, como o <c>TileRunner</c> do Terraria: uma caminhada curta que carimba um
    /// disco de borda irregular a cada passo, afinando até o fim. Cada metal tem a faixa principal e alguns bolsões menores
    /// no resto do mapa, abaixo dela. Sorteados de uma vez para o mundo inteiro, com gerador próprio (o resto do mapa da mesma
    /// seed não muda) e só pela seed e pelo relevo, então blocos gerados em qualquer ordem casam.
    /// </summary>
    public sealed class OreVeins
    {
        const int Bottom = 2;
        // Bolsões fora da faixa principal saem com este tanto do raio.
        const float ScatterScale = 0.75f;

        readonly Dictionary<int, CellMaterial> _cells = new Dictionary<int, CellMaterial>();
        readonly bool[] _column;
        readonly int _width;

        public OreVeins(OreSettings settings, int seed, int[] surface, int worldHeight)
        {
            _width = surface.Length;
            _column = new bool[_width];
            if (settings == null)
                return;
            var random = new System.Random(seed ^ 0x0E5E1);
            Carve(random, settings.Copper, CellMaterial.CopperOre, settings.Turn, surface, worldHeight);
            Carve(random, settings.Iron, CellMaterial.IronOre, settings.Turn, surface, worldHeight);
            Carve(random, settings.Gold, CellMaterial.GoldOre, settings.Turn, surface, worldHeight);
        }

        public int CellCount => _cells.Count;

        /// <summary>Minério na célula, ou <see cref="CellMaterial.Empty"/> se não há veio nela.</summary>
        public CellMaterial At(int x, int y) =>
            (uint)x < (uint)_width && _column[x] && _cells.TryGetValue(y * _width + x, out CellMaterial ore) ? ore : CellMaterial.Empty;

        void Carve(System.Random random, OreBand band, CellMaterial ore, float turn, int[] surface, int worldHeight)
        {
            int minDepth = Mathf.Max(2, band.Depth.x);
            int count = Mathf.RoundToInt(band.VeinsPer1000Columns * _width / 1000f);
            for (int v = 0; v < count; v++)
            {
                int x0 = random.Next(2, Mathf.Max(3, _width - 2));
                int maxDepth = band.Depth.y > 0 ? band.Depth.y : surface[x0] - Bottom;
                Pocket(random, band, ore, turn, surface, worldHeight, x0, minDepth, maxDepth, band.DeepBias, 1f);
            }

            // O resto do mapa: bolsões menores do fundo da faixa até o fundo do mundo.
            if (band.Depth.y <= 0)
                return;
            int scatter = Mathf.RoundToInt(band.ScatterPer1000Columns * _width / 1000f);
            for (int v = 0; v < scatter; v++)
            {
                int x0 = random.Next(2, Mathf.Max(3, _width - 2));
                Pocket(random, band, ore, turn, surface, worldHeight, x0, band.Depth.y, surface[x0] - Bottom, 0f, ScatterScale);
            }
        }

        /// <summary>Um bolsão: caminhada de <see cref="OreBand.Length"/> passos que carimba discos cada vez menores.</summary>
        void Pocket(System.Random random, OreBand band, CellMaterial ore, float turn, int[] surface, int worldHeight,
                    int x0, int minDepth, int maxDepth, float deepBias, float scale)
        {
            if (maxDepth <= minDepth)
                return;
            float x = x0;
            float u = Mathf.Pow((float)random.NextDouble(), 1f / (1f + deepBias));
            float depth = minDepth + u * (maxDepth - minDepth);
            float angle = (float)(random.NextDouble() * Math.PI * 2.0);
            int steps = random.Next(band.Length.x, Mathf.Max(band.Length.x, band.Length.y) + 1);
            float radius0 = Mathf.Lerp(band.Radius.x, Mathf.Max(band.Radius.x, band.Radius.y), (float)random.NextDouble()) * scale;
            for (int step = 0; step < steps; step++)
            {
                int cx = Mathf.RoundToInt(x);
                if (cx < 1 || cx >= _width - 1)
                    break;
                int cy = surface[cx] - Mathf.RoundToInt(depth);
                if (cy < Bottom)
                    break;
                float radius = radius0 * (1f - 0.5f * step / Mathf.Max(1, steps));
                Stamp(random, cx, cy, radius, ore, surface, minDepth, worldHeight);
                angle += ((float)random.NextDouble() * 2f - 1f) * turn;
                x += Mathf.Cos(angle);
                depth -= Mathf.Sin(angle);
                // Fora da faixa, o bolsão volta para dentro (vira para cima ou para baixo).
                if (depth < minDepth || depth > maxDepth)
                {
                    angle = -angle;
                    depth = Mathf.Clamp(depth, minDepth, maxDepth);
                }
            }
        }

        /// <summary>Disco de raio <paramref name="radius"/> com a borda roída ao acaso, para o bolsão não sair redondo liso.</summary>
        void Stamp(System.Random random, int cx, int cy, float radius, CellMaterial ore, int[] surface, int minDepth, int worldHeight)
        {
            int r = Mathf.CeilToInt(radius);
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                float edge = radius - (float)random.NextDouble() * 0.9f;
                if (dx * dx + dy * dy <= edge * edge)
                    Mark(cx + dx, cy + dy, ore, surface, minDepth, worldHeight);
            }
        }

        void Mark(int x, int y, CellMaterial ore, int[] surface, int minDepth, int worldHeight)
        {
            if (x < 1 || x >= _width - 1 || y < 1 || y >= worldHeight || surface[x] - y < minDepth)
                return;
            _cells[y * _width + x] = ore;
            _column[x] = true;
        }
    }
}
