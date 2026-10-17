using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.AntFarm
{
    /// <summary>
    /// Gera a arte provisória das formigas como PNGs editáveis em <see cref="AntSpriteImporter.AntsFolder"/>,
    /// um arquivo por quadro de caminhada, pixel art 18x12 virada para a direita:
    /// <c>Ant_Walk_*</c> de lado (patas para baixo, para piso/parede/teto) e <c>Ant_Top_*</c> vista de cima
    /// (para quando anda no fundo do formigueiro).
    /// Os arquivos existentes não são sobrescritos pelo setup, só pelo menu (para não perder retoques de arte).
    /// </summary>
    public static class AntSpriteGenerator
    {
        public const int Width = 18;
        public const int Height = 12;
        public const int FrameCount = 4;

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 Body = new Color32(30, 18, 14, 255);
        static readonly Color32 Shine = new Color32(84, 52, 36, 255);
        static readonly Color32 NearLeg = new Color32(22, 13, 10, 255);
        static readonly Color32 FarLeg = new Color32(58, 38, 28, 255);
        static readonly Color32 Eye = new Color32(120, 84, 60, 255);

        public static string FramePath(int index) => $"{AntSpriteImporter.AntsFolder}Ant_Walk_{index}.png";
        public static string TopFramePath(int index) => $"{AntSpriteImporter.AntsFolder}{AntSpriteImporter.TopViewPrefix}{index}.png";

        [MenuItem("Incrementant/Arte/Regerar sprites das formigas (sobrescreve)")]
        static void RegenerateFromMenu()
        {
            Generate(overwrite: true);
            GenerateTop(overwrite: true);
        }

        /// <summary>Quadros de lado: grava os que faltam (ou todos, com <paramref name="overwrite"/>) e devolve os sprites.</summary>
        public static Sprite[] Generate(bool overwrite) => GenerateSet(FramePath, DrawFrame, overwrite);

        /// <summary>Quadros vista de cima: grava os que faltam (ou todos, com <paramref name="overwrite"/>) e devolve os sprites.</summary>
        public static Sprite[] GenerateTop(bool overwrite) => GenerateSet(TopFramePath, DrawTopFrame, overwrite);

        static Sprite[] GenerateSet(System.Func<int, string> pathOf, System.Func<float, Color32[]> draw, bool overwrite)
        {
            Directory.CreateDirectory(AntSpriteImporter.AntsFolder);
            var sprites = new Sprite[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                string path = pathOf(i);
                if (overwrite || !File.Exists(path))
                {
                    WritePng(path, draw(i / (float)FrameCount));
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return sprites;
        }

        /// <summary>Vista de cima, simétrica em relação à linha central (y = 6): 3 pares de patas abertas, antenas em V.</summary>
        static Color32[] DrawTopFrame(float phase)
        {
            var px = new Color32[Width * Height];
            for (int i = 0; i < px.Length; i++)
                px[i] = Clear;

            // Marcha em tripé: dianteira e traseira de um lado andam junto com a do meio do outro lado.
            for (int side = 0; side < 2; side++)
            {
                bool upper = side == 0;
                float sidePhase = phase + (upper ? 0f : 0.5f);
                DrawTopLeg(px, 9.5f, 6f, sidePhase + 0.5f, upper);    // traseira
                DrawTopLeg(px, 10.5f, 10.5f, sidePhase, upper);       // meio
                DrawTopLeg(px, 11.5f, 15f, sidePhase + 0.5f, upper);  // dianteira
            }

            Ellipse(px, 3.8f, 6f, 3.3f, 2.3f, dorsal: true);   // gáster
            Ellipse(px, 7.5f, 6f, 0.7f, 0.6f, dorsal: true);   // pecíolo
            Ellipse(px, 9.9f, 6f, 1.7f, 1.1f, dorsal: true);   // tórax
            Ellipse(px, 13.6f, 6f, 1.8f, 1.7f, dorsal: true);  // cabeça
            Put(px, 14, 7, Eye);
            Put(px, 14, 4, Eye);
            Line(px, 15, 7, 16, 7, Body);  // mandíbulas
            Line(px, 15, 4, 16, 4, Body);
            Line(px, 14, 8, 15, 10, Body); // antenas
            Line(px, 15, 10, 17, 11, Body);
            Line(px, 14, 3, 15, 1, Body);
            Line(px, 15, 1, 17, 0, Body);
            return px;
        }

        static void DrawTopLeg(Color32[] px, float hipX, float footBaseX, float phase, bool upper)
        {
            float angle = phase * Mathf.PI * 2f;
            int footX = Mathf.RoundToInt(footBaseX + Mathf.Sin(angle) * 1.2f);
            int hipX0 = Mathf.RoundToInt(hipX);
            int kneeX = Mathf.RoundToInt(hipX + (footX - hipX) * 0.6f);
            // Linhas na metade de cima; a de baixo é o espelho (linha r ↔ 11 - r).
            int hip = 7, knee = 9, foot = Mathf.Cos(angle) > 0.3f ? 10 : 11;
            if (!upper)
            {
                hip = Height - 1 - hip;
                knee = Height - 1 - knee;
                foot = Height - 1 - foot;
            }
            Line(px, hipX0, hip, kneeX, knee, NearLeg);
            Line(px, kneeX, knee, footX, foot, NearLeg);
        }

        static Color32[] DrawFrame(float phase)
        {
            var px = new Color32[Width * Height];
            for (int i = 0; i < px.Length; i++)
                px[i] = Clear;

            // Patas do lado de lá primeiro (ficam atrás do corpo), em fase oposta às do lado de cá.
            DrawLegs(px, phase + 0.5f, FarLeg, 0.8f);

            Ellipse(px, 3.6f, 6.2f, 3.3f, 2.5f);   // gáster (abdômen)
            Ellipse(px, 7.5f, 5.5f, 0.7f, 0.6f);   // pecíolo (cintura fina)
            Ellipse(px, 9.9f, 6.0f, 1.7f, 1.0f);   // tórax
            Ellipse(px, 13.7f, 6.6f, 1.9f, 1.6f);  // cabeça
            Put(px, 14, 7, Eye);
            Line(px, 15, 5, 16, 4, Body);          // mandíbula
            Line(px, 13, 8, 13, 10, Body);         // antena (escapo)
            Line(px, 14, 10, 16, 11, Body);        // antena (flagelo)

            DrawLegs(px, phase, NearLeg, 0f);
            return px;
        }

        /// <summary>Três patas presas ao tórax; marcha em tripé: a do meio em fase oposta à dianteira e à traseira.</summary>
        static void DrawLegs(Color32[] px, float phase, Color32 color, float shift)
        {
            DrawLeg(px, 9f + shift, 5.5f + shift, phase + 0.5f, color);    // traseira
            DrawLeg(px, 10f + shift, 10f + shift, phase, color);           // meio
            DrawLeg(px, 11f + shift, 14f + shift, phase + 0.5f, color);    // dianteira
        }

        static void DrawLeg(Color32[] px, float hipX, float footBaseX, float phase, Color32 color)
        {
            float angle = phase * Mathf.PI * 2f;
            float footX = footBaseX + Mathf.Sin(angle) * 1.2f;
            // Na metade do ciclo em que a pata volta para a frente, ela fica levantada.
            int footY = Mathf.Cos(angle) > 0.3f ? 1 : 0;
            // Fêmur para fora e para cima do joelho; tíbia descendo até o chão.
            int hip = Mathf.RoundToInt(hipX);
            int kneeX = Mathf.RoundToInt(hipX + (footX - hipX) * 0.7f);
            Line(px, hip, 5, kneeX, 4, color);
            Line(px, kneeX, 4, Mathf.RoundToInt(footX), footY, color);
        }

        static void Ellipse(Color32[] px, float cx, float cy, float rx, float ry, bool dorsal = false)
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                float dx = (x + 0.5f - cx) / rx;
                float dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy > 1f)
                    continue;
                // Brilho: na parte de cima de cada segmento (de lado) ou no centro do gáster (de cima; nos segmentos pequenos parece furo).
                bool shine = dorsal
                    ? rx >= 3f && Mathf.Abs(dy) < 0.3f && dx > -0.35f && dx < 0.25f
                    : dy > 0.5f && dx < 0.5f && dx > -0.6f;
                px[y * Width + x] = shine ? Shine : Body;
            }
        }

        static void Line(Color32[] px, int x0, int y0, int x1, int y1, Color32 color)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                Put(px, x0, y0, color);
                if (x0 == x1 && y0 == y1)
                    break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        static void Put(Color32[] px, int x, int y, Color32 color)
        {
            if ((uint)x < Width && (uint)y < Height)
                px[y * Width + x] = color;
        }

        static void WritePng(string path, Color32[] pixels)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
