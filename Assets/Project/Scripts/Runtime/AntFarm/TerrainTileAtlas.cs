using System;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Atlas RGB do terreno em uso (formato em <see cref="CellTileArt"/>), lido pelo shader <c>Incrementant/TerrainTiles</c>
    /// e pelo bloco carregado (<c>AntSwarmView</c>), para os dois mostrarem a mesma arte. Guarda, por linha, a cor de
    /// referência (a cor que a CPU grava numa célula do material sem tom nem marca) e a média do atlas: o shader e o bloco
    /// desenham <c>lerp(média, texel, força da textura) × (cor da célula / referência)</c>, em espaço linear. Assim o tom,
    /// a umidade, as marcas e a água chegam ao desenho como ajuste sobre a arte, e com o zoom aberto sobra a média.
    /// </summary>
    public sealed class TerrainTileAtlas
    {
        readonly Color32[] _pixels;
        readonly Color32[] _references;
        readonly Color32[] _means;
        readonly bool _ownsTexture;

        /// <summary>Atlas em si (sRGB, Point, sem mipmaps).</summary>
        public Texture2D Texture { get; }

        /// <summary>2 x <see cref="CellTileArt.RowCount"/>: coluna 0 = cor de referência da linha, coluna 1 = média do atlas.</summary>
        public Texture2D Colors { get; }

        /// <summary>Verdadeiro quando o atlas desenhado não pôde ser usado e este é o gerado por código.</summary>
        public bool IsFallback { get; }

        TerrainTileAtlas(Texture2D texture, Color32[] pixels, Color32[] references, bool ownsTexture, bool isFallback)
        {
            Texture = texture;
            _pixels = pixels;
            _references = references;
            _ownsTexture = ownsTexture;
            IsFallback = isFallback;
            _means = new Color32[CellTileArt.RowCount];
            for (int row = 0; row < _means.Length; row++)
                _means[row] = MeanOf(row);

            Colors = new Texture2D(2, CellTileArt.RowCount, TextureFormat.RGBA32, false, false)
            {
                name = "TerrainTileColors", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
            };
            var colors = new Color32[2 * CellTileArt.RowCount];
            for (int row = 0; row < CellTileArt.RowCount; row++)
            {
                colors[row * 2] = references[row];
                colors[row * 2 + 1] = _means[row];
            }
            Colors.SetPixels32(colors);
            Colors.Apply(false, true);
        }

        /// <summary>
        /// Atlas a usar: o desenhado (<paramref name="drawn"/>), se estiver no formato, ou o gerado por código sobre as
        /// <paramref name="references"/> (uma cor por linha). Um atlas fora do formato gera um erro que diz o que corrigir.
        /// </summary>
        public static TerrainTileAtlas Create(Texture2D drawn, Color32[] references, UnityEngine.Object context)
        {
            if (references == null || references.Length != CellTileArt.RowCount)
                throw new ArgumentException($"São {CellTileArt.RowCount} cores de referência, uma por linha do atlas.", nameof(references));

            if (drawn != null)
            {
                string problem = FormatProblem(drawn);
                if (problem == null)
                    return new TerrainTileAtlas(drawn, drawn.GetPixels32(), references, false, false);
                Debug.LogError($"Tile Atlas '{drawn.name}' fora do formato ({problem}); usando o atlas gerado por código. " +
                               $"Esperado: {CellTileArt.TileSize} x {CellTileArt.TileSize * CellTileArt.RowCount} px, Read/Write ligado, " +
                               "uma linha por CellMaterial, a parede de fundo e as salas construídas (ver AntFarmGuide, \"Atlas RGB\").", context);
            }

            Color32[] pixels = CellTileArt.Generate(references);
            var texture = new Texture2D(CellTileArt.TileSize, CellTileArt.TileSize * CellTileArt.RowCount, TextureFormat.RGBA32, false, false)
            {
                name = "TerrainTileAtlas", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return new TerrainTileAtlas(texture, pixels, references, true, drawn != null);
        }

        /// <summary>O que impede o uso de <paramref name="texture"/> como atlas, ou null se estiver no formato.</summary>
        public static string FormatProblem(Texture2D texture)
        {
            if (texture.width != CellTileArt.TileSize || texture.height != CellTileArt.TileSize * CellTileArt.RowCount)
                return $"tem {texture.width} x {texture.height} px";
            if (!texture.isReadable)
                return "sem Read/Write";
            return null;
        }

        public Color32 Reference(int row) => _references[row];

        public Color32 Mean(int row) => _means[row];

        /// <summary>Texel da linha <paramref name="row"/> na posição de mundo (em texels), com a repetição do atlas.</summary>
        public Color32 Texel(int row, int worldTexelX, int worldTexelY)
        {
            int size = CellTileArt.TileSize;
            int x = ((worldTexelX % size) + size) % size;
            int y = ((worldTexelY % size) + size) % size;
            return _pixels[(row * size + y) * size + x];
        }

        public void Destroy()
        {
            if (_ownsTexture && Texture != null)
                UnityEngine.Object.Destroy(Texture);
            if (Colors != null)
                UnityEngine.Object.Destroy(Colors);
        }

        /// <summary>Média da linha em espaço linear, devolvida em sRGB (o que o filtro da GPU faria).</summary>
        Color32 MeanOf(int row)
        {
            int size = CellTileArt.TileSize;
            double r = 0, g = 0, b = 0;
            int start = row * size * size, end = start + size * size;
            for (int i = start; i < end; i++)
            {
                Color linear = ((Color)_pixels[i]).linear;
                r += linear.r;
                g += linear.g;
                b += linear.b;
            }
            double n = size * size;
            return new Color((float)(r / n), (float)(g / n), (float)(b / n), 1f).gamma;
        }
    }
}
