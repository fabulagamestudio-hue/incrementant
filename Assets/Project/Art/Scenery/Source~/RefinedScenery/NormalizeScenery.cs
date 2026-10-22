using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// Ferramenta offline de fontes (Source~): nenhum assembly ou executável é exportado.
namespace Fabula.Incrementant.ArtTools
{
public static class NormalizeScenery
{
    static int[] Read(Bitmap image)
    {
        using (var converted = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(converted)) g.DrawImageUnscaled(image, 0, 0);
            var data = converted.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var pixels = new int[image.Width * image.Height];
            for (int y = 0; y < image.Height; y++) Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), pixels, y * image.Width, image.Width);
            converted.UnlockBits(data);
            return pixels;
        }
    }
    static int[] Bounds(int[] pixels, int width, int height)
    {
        int l = width, t = height, r = -1, b = -1;
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            if ((uint)pixels[y * width + x] >> 24 >= 200) { l = Math.Min(l, x); r = Math.Max(r, x); t = Math.Min(t, y); b = Math.Max(b, y); }
        if (r < l) throw new InvalidOperationException("Imagem vazia.");
        return new[] { l, t, r, b };
    }
    public static void Run(string sourcePath, string layoutPath, string outputPath, int width, int height, string[] hex)
    {
        using (var source = new Bitmap(sourcePath)) using (var layout = new Bitmap(layoutPath))
        {
            int[] input = Read(source), original = Read(layout), a = Bounds(input, source.Width, source.Height), b = Bounds(original, layout.Width, layout.Height);
            var palette = new int[hex.Length];
            for (int i = 0; i < hex.Length; i++) palette[i] = ColorTranslator.FromHtml("#" + hex[i].Trim().TrimStart('#')).ToArgb();
            double sw = a[2] - a[0] + 1, sh = a[3] - a[1] + 1;
            double ratioX = width / (double)layout.Width, ratioY = height / (double)layout.Height;
            double scale = Math.Min((b[2] - b[0] + 1) * ratioX / sw, (b[3] - b[1] + 1) * ratioY / sh);
            double left = ((b[0] + b[2] + 1) * ratioX - sw * scale) / 2, top = (b[3] + 1) * ratioY - sh * scale;
            var output = new int[width * height];
            var cache = new Dictionary<int, int>();
            for (int y = 0; y < height; y++)
            {
                int sy = (int)Math.Floor((y + 0.5 - top) / scale + a[1]);
                if (sy < 0 || sy >= source.Height) continue;
                for (int x = 0; x < width; x++)
                {
                    int sx = (int)Math.Floor((x + 0.5 - left) / scale + a[0]);
                    if (sx < 0 || sx >= source.Width) continue;
                    int color = input[sy * source.Width + sx];
                    if ((uint)color >> 24 < 200) continue;
                    int chosen;
                    if (!cache.TryGetValue(color, out chosen))
                    {
                        int best = int.MaxValue;
                        foreach (int p in palette)
                        {
                            int dr = ((color >> 16) & 255) - ((p >> 16) & 255), dg = ((color >> 8) & 255) - ((p >> 8) & 255), db = (color & 255) - (p & 255);
                            int distance = dr * dr + dg * dg + db * db;
                            if (distance < best) { best = distance; chosen = p; }
                        }
                        cache[color] = chosen;
                    }
                    output[y * width + x] = chosen;
                }
            }
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                if (output[i] == 0) continue;
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1 || output[i - 1] == 0 || output[i + 1] == 0 || output[i - width] == 0 || output[i + width] == 0) output[i] = palette[0];
            }
            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                for (int y = 0; y < height; y++) Marshal.Copy(output, y * width, IntPtr.Add(data.Scan0, y * data.Stride), width);
                bitmap.UnlockBits(data);
                bitmap.Save(outputPath, ImageFormat.Png);
            }
        }
    }
    public static void Validate(string path, int width, int height, string[] hex)
    {
        var allowed = new HashSet<int>();
        foreach (string s in hex) allowed.Add(ColorTranslator.FromHtml("#" + s.Trim().TrimStart('#')).ToArgb());
        using (var bitmap = new Bitmap(path))
        {
            if (bitmap.Width != width || bitmap.Height != height) throw new InvalidOperationException("Dimensoes incorretas: " + path);
            int opaque = 0;
            foreach (int c in Read(bitmap))
            {
                uint alpha = (uint)c >> 24;
                if (alpha == 0) continue;
                if (alpha != 255 || !allowed.Contains(c)) throw new InvalidOperationException("Cor ou alpha invalido: " + path);
                opaque++;
            }
            if (opaque == 0) throw new InvalidOperationException("Sprite vazio: " + path);
        }
    }
}
}
