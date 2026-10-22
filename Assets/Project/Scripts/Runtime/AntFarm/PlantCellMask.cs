using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>Um texel por célula: alpha indica presença, vermelho indica marca de colheita.</summary>
    public sealed class PlantCellMask
    {
        public readonly int MinX;
        public readonly int MinY;
        public readonly int Width;
        public readonly int Height;
        public readonly Color32[] Pixels;
        public readonly Vector4 Mapping;
        public readonly Vector2 VisualOffset;
        public Vector4 Bounds => new Vector4(MinX, MinY, Width, Height);

        public PlantCellMask(Sprite sprite)
        {
            float ppu = sprite.pixelsPerUnit;
            var pixelPivot = new Vector2(Mathf.Round(sprite.pivot.x), Mathf.Round(sprite.pivot.y));
            VisualOffset = (sprite.pivot - pixelPivot) / ppu;
            Mapping = new Vector4(sprite.rect.width / ppu, sprite.rect.height / ppu, pixelPivot.x / ppu, pixelPivot.y / ppu);
            MinX = Mathf.FloorToInt(-Mapping.z + 0.5f);
            MinY = Mathf.FloorToInt(-Mapping.w);
            Width = Mathf.CeilToInt(Mapping.x - Mapping.z + 0.5f) - MinX;
            Height = Mathf.CeilToInt(Mapping.y - Mapping.w) - MinY;
            Pixels = new Color32[Width * Height];
            // A parte enterrada fica visível atrás do terreno, como no caminho legado.
            for (int y = MinY; y < 0; y++)
            for (int x = MinX; x < MinX + Width; x++)
                SetCell(new Vector2Int(x, y), 1);
        }

        public void SetCell(Vector2Int cell, byte state)
        {
            int x = cell.x - MinX, y = cell.y - MinY;
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
                return;
            Pixels[y * Width + x] = state == 0 ? default : new Color32(state == 2 ? (byte)255 : (byte)0, 0, 0, 255);
        }
    }
}
