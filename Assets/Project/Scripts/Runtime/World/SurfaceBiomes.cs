using System;
using UnityEngine;

namespace Fabula.Incrementant.World
{
    /// <summary>Bioma da superfície numa coluna (documento 44, REQ-070). Não vai para o save: sai da seed.</summary>
    public enum SurfaceBiome : byte
    {
        /// <summary>Gramado: o centro do mapa, em volta do formigueiro inicial. Grama e trevos colhíveis.</summary>
        Meadow = 0,
        /// <summary>Serapilheira: chão coberto de folhas secas (<see cref="CellMaterial.Litter"/>), gravetos e folhas secas no cenário.</summary>
        LeafLitter = 1,
        /// <summary>Canteiro de flores: chão de terra adubada (<see cref="CellMaterial.Compost"/>), flores que soltam néctar.</summary>
        FlowerBed = 2,
        /// <summary>Areal, o deserto do Terraria (documento 45): chão de areia e areia em profundidade, sem plantas.</summary>
        Desert = 3,
        /// <summary>Fruteira, a selva do Terraria (documento 45): terra adubada e plantas folhosas densas.</summary>
        FruitGrove = 4,
    }

    /// <summary>Onde fica cada bioma da superfície (documento 44, "Como o mundo se monta").</summary>
    [Serializable]
    public sealed class BiomeSettings
    {
        [Tooltip("Início da Serapilheira, em células à direita da entrada do formigueiro.")]
        [SerializeField, Min(0)] int _litterOffset = 240;
        [Tooltip("Largura da Serapilheira, em células. 0 desliga o bioma.")]
        [SerializeField, Min(0)] int _litterWidth = 360;
        [Tooltip("Fim do Canteiro de flores, em células à esquerda da entrada do formigueiro.")]
        [SerializeField, Min(0)] int _flowerOffset = 240;
        [Tooltip("Largura do Canteiro de flores, em células. 0 desliga o bioma.")]
        [SerializeField, Min(0)] int _flowerWidth = 360;
        [Tooltip("Largura da transição entre dois biomas, em células: as colunas se misturam, sem fronteira reta.")]
        [SerializeField, Min(0)] int _blendWidth = 24;
        [Tooltip("Areal e Fruteira nas pontas do mundo, depois do Canteiro e da Serapilheira, até a borda (documento 45).")]
        [SerializeField] bool _edgeBiomes = true;
        [Tooltip("Sorteia pela seed de que lado fica cada bioma, como o Terraria faz com a selva e a neve. Desligado, a Serapilheira fica à direita.")]
        [SerializeField] bool _randomSide = true;
        [Tooltip("Profundidade da areia sob o Areal, em células a partir do solo (o deserto subterrâneo).")]
        [SerializeField, Min(2)] int _desertDepth = 40;

        public int LitterOffset => _litterOffset;
        public int LitterWidth => _litterWidth;
        public int FlowerOffset => _flowerOffset;
        public int FlowerWidth => _flowerWidth;
        public int BlendWidth => _blendWidth;
        public bool EdgeBiomes => _edgeBiomes;
        public bool RandomSide => _randomSide;
        public int DesertDepth => _desertDepth;
    }

    /// <summary>
    /// Bioma de cada coluna do mundo. O mundo cresce para os lados em faixas (o modelo de Terraria do documento 44): o
    /// gramado no centro, a Serapilheira de um lado e o Canteiro de flores do outro, cada um a uma distância da entrada, e
    /// nas pontas a Fruteira (depois da Serapilheira) e o Areal (depois do Canteiro), até a borda. O lado sai da seed.
    /// Determinístico pela seed e pela coluna, então blocos gerados em qualquer ordem casam.
    /// </summary>
    public sealed class SurfaceBiomes
    {
        readonly byte[] _columns;

        SurfaceBiomes(byte[] columns, int litterSide = 1)
        {
            _columns = columns;
            LitterSide = litterSide;
        }

        public int Width => _columns.Length;

        /// <summary>Mundo só de gramado (mapa de imagem, testes antigos).</summary>
        public static SurfaceBiomes Uniform(int width) => new SurfaceBiomes(new byte[Mathf.Max(0, width)]);

        /// <summary>Gramado com <paramref name="biome"/> nas colunas [<paramref name="from"/>, <paramref name="to"/>), sem transição (testes, mapas montados à mão).</summary>
        public static SurfaceBiomes Band(int width, SurfaceBiome biome, int from, int to)
        {
            var columns = new byte[Mathf.Max(0, width)];
            for (int x = Mathf.Max(0, from); x < Mathf.Min(columns.Length, to); x++)
                columns[x] = (byte)biome;
            return new SurfaceBiomes(columns);
        }

        public static SurfaceBiomes Generate(int width, int entranceX, BiomeSettings settings, int seed)
        {
            var columns = new byte[Mathf.Max(0, width)];
            if (settings == null)
                return new SurfaceBiomes(columns);

            // Gerador próprio, para não mudar o resto do mapa da mesma seed.
            var random = new System.Random(seed ^ 0x5E7A9);
            float noiseSeed = random.Next(0, 10000);
            int litterSide = settings.RandomSide && random.Next(2) == 1 ? -1 : 1;
            int litterNear = entranceX + litterSide * settings.LitterOffset;
            int litterFar = litterNear + litterSide * settings.LitterWidth;
            int flowerNear = entranceX - litterSide * settings.FlowerOffset;
            int flowerFar = flowerNear - litterSide * settings.FlowerWidth;
            Strip(columns, SurfaceBiome.LeafLitter, Mathf.Min(litterNear, litterFar), Mathf.Max(litterNear, litterFar), settings.BlendWidth, noiseSeed, seed);
            Strip(columns, SurfaceBiome.FlowerBed, Mathf.Min(flowerNear, flowerFar), Mathf.Max(flowerNear, flowerFar), settings.BlendWidth, noiseSeed, seed);
            if (settings.EdgeBiomes)
            {
                // Pontas: do meio da borda de fora de cada bioma lateral até a borda do mundo, só onde ficou gramado. A
                // fronteira com o lateral é a mistura que ele já fez, sem sobrar gramado entre os dois.
                // Começa dentro da transição de fora do lateral: as colunas que ele não tomou nela ficam com a ponta.
                int inner = Mathf.CeilToInt(settings.BlendWidth * 0.5f);
                if (settings.LitterWidth > 0)
                    FillBeyond(columns, SurfaceBiome.FruitGrove, litterFar - litterSide * inner, litterSide);
                if (settings.FlowerWidth > 0)
                    FillBeyond(columns, SurfaceBiome.Desert, flowerFar + litterSide * inner, -litterSide);
            }
            return new SurfaceBiomes(columns, litterSide);
        }

        /// <summary>Lado da Serapilheira e da Fruteira em relação à entrada: +1 à direita, -1 à esquerda.</summary>
        public int LitterSide { get; }

        /// <summary>Faixa [<paramref name="start"/>, <paramref name="end"/>) do bioma, com a transição misturada nas duas bordas.</summary>
        static void FillBeyond(byte[] columns, SurfaceBiome biome, int from, int side)
        {
            // O bioma lateral passou da borda do mundo: não sobra ponta.
            if (side > 0 ? from >= columns.Length : from < 0)
                return;
            for (int x = Mathf.Clamp(from, 0, columns.Length - 1); x >= 0 && x < columns.Length; x += side)
                if (columns[x] == (byte)SurfaceBiome.Meadow)
                    columns[x] = (byte)biome;
        }

        static void Strip(byte[] columns, SurfaceBiome biome, int start, int end, int blendWidth, float noiseSeed, int seed)
        {
            if (end <= start)
                return;
            float half = blendWidth * 0.5f;
            for (int x = Mathf.Max(0, Mathf.FloorToInt(start - half)); x < Mathf.Min(columns.Length, Mathf.CeilToInt(end + half)); x++)
            {
                // Distância, com sinal, até a borda mais perto: > 0 dentro do bioma. Na faixa de transição, o ruído da coluna
                // decide de que lado ela fica, então a fronteira sai irregular e mais densa para o lado do bioma.
                float inside = Mathf.Min(x - start, end - x);
                if (inside < -half)
                    continue;
                if (inside > half || half <= 0f)
                {
                    if (inside >= 0f)
                        columns[x] = (byte)biome;
                    continue;
                }
                float t = (inside + half) / (2f * half);
                float n = Mathf.PerlinNoise(noiseSeed + x * 0.35f, 3.7f) * 0.3f + Hash01(x, seed) * 0.7f;
                if (n < t)
                    columns[x] = (byte)biome;
            }
        }

        public SurfaceBiome At(int x) => (uint)x < (uint)_columns.Length ? (SurfaceBiome)_columns[x] : SurfaceBiome.Meadow;

        static float Hash01(int x, int seed)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)seed * 668265263u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }
    }
}
