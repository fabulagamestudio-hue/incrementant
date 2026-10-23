using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Rachadura dos blocos golpeados (REQ-072): uma máscara de <see cref="CellTileArt.TexelsPerCell"/>² texels por estágio
    /// (<see cref="World.PixelGrid.CrackStages"/>), lado a lado da esquerda para a direita, cada uma contendo a anterior. O
    /// shader TerrainTiles pinta os texels marcados com a cor do contorno e espelha a máscara por célula para os blocos
    /// vizinhos não rachar igual. Arte provisória do agente, desenhada aqui para ser fácil de ajustar: '#' = rachado.
    /// </summary>
    public static class CrackArt
    {
        // Linha de cima primeiro (como se vê na tela).
        static readonly string[][] Stages =
        {
            new[]
            {
                "........",
                "........",
                "....#...",
                "...#....",
                "...#....",
                "........",
                "........",
                "........",
            },
            new[]
            {
                "........",
                ".....#..",
                "....#...",
                "...#....",
                "...##...",
                "....#...",
                "........",
                "........",
            },
            new[]
            {
                "......#.",
                ".....#..",
                ".#..#...",
                "..##....",
                "...##...",
                "....##..",
                "...#..#.",
                "........",
            },
            new[]
            {
                "#.....#.",
                ".#...#..",
                ".##.#..#",
                "..##..#.",
                "...####.",
                "....##..",
                "..##..#.",
                ".#.....#",
            },
        };

        public static int StageCount => Stages.Length;

        /// <summary>O texel (x, y), com y para cima como na textura, está rachado no estágio (1 a <see cref="StageCount"/>).</summary>
        public static bool IsCracked(int stage, int x, int y)
        {
            int size = CellTileArt.TexelsPerCell;
            return stage >= 1 && stage <= Stages.Length && Stages[stage - 1][size - 1 - y][x] == '#';
        }

        /// <summary>Textura das máscaras (alfa 255 no texel rachado), ponto, sem mipmap; quem chama a destrói.</summary>
        public static Texture2D CreateTexture()
        {
            int size = CellTileArt.TexelsPerCell;
            var texture = new Texture2D(size * Stages.Length, size, TextureFormat.Alpha8, false, true)
            {
                name = "AntFarmCracks",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new byte[size * Stages.Length * size];
            for (int stage = 1; stage <= Stages.Length; stage++)
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (IsCracked(stage, x, y))
                    pixels[y * size * Stages.Length + (stage - 1) * size + x] = 255;
            texture.SetPixelData(pixels, 0);
            texture.Apply(false, true);
            return texture;
        }
    }
}
