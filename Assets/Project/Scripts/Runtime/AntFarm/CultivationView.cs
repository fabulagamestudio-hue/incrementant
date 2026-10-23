using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Animação dos canteiros da sala de nutrientes. Só visual: a terra fofa é a própria célula do terreno; por cima dela
    /// este componente desenha o broto (a planta posta pela formiga), que cresce e balança enquanto o canteiro brota, a gota
    /// de água que a terra absorve logo depois de regada e, no fim, os nutrientes inchando nos lados livres, onde eles vão
    /// nascer. Os sprites são gerados por código (16 texels por célula, como os tiles do terreno).
    /// </summary>
    public sealed class CultivationView : MonoBehaviour
    {
        [Tooltip("Na frente do terreno e atrás das crias (BroodView 8) e das formigas (AntSwarmView 10).")]
        [SerializeField] int _sortingOrder = 7;
        [Tooltip("Tamanho do broto recém-plantado (1 = uma célula de altura).")]
        [SerializeField, Range(0.1f, 1.5f)] float _seedlingScale = 0.7f;
        [Tooltip("Tamanho do broto quando o canteiro termina de brotar.")]
        [SerializeField, Range(0.3f, 3f)] float _grownScale = 1.6f;
        [Tooltip("Parte inicial do brotar (0 a 1) em que a gota de água some na terra.")]
        [SerializeField, Range(0.05f, 1f)] float _soakPortion = 0.25f;
        [Tooltip("A partir de que parte do brotar (0 a 1) os nutrientes começam a inchar nos lados livres.")]
        [SerializeField, Range(0f, 0.95f)] float _budStart = 0.6f;
        [Tooltip("Balanço do broto (graus).")]
        [SerializeField, Range(0f, 30f)] float _swayDegrees = 8f;
        [SerializeField] Color32 _stemColor = new Color32(70, 140, 52, 255);
        [SerializeField] Color32 _leafColor = new Color32(118, 190, 76, 255);
        [SerializeField] Color32 _waterColor = new Color32(84, 150, 220, 235);
        [Tooltip("Cor do nutriente brotando (a mesma do nutriente pronto no AntFarmView).")]
        [SerializeField] Color32 _nutrientColor = new Color32(232, 214, 92, 255);

        /// <summary>Lado da pintura dos sprites; o sprite sai com a metade (8 texels por célula, experimento 8x8).</summary>
        const int Texels = 16;

        readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        Sprite _sprout;
        Sprite _drop;
        Sprite _bud;
        int _used;

        /// <param name="growSeconds">Tempo total de brotar (<c>AntColony.NutrientCureSeconds</c>), para o progresso.</param>
        /// <param name="origin">Posição do mundo da célula (0, 0), a mesma usada pelas formigas.</param>
        public void Render(PixelGrid grid, float growSeconds, Vector3 origin, float time)
        {
            if (_sprout == null)
                BuildSprites();
            _used = 0;
            for (int k = 0; k < grid.BedCount; k++)
            {
                if (!grid.TryGetBed(k, out int x, out int y))
                    break;
                BedStage stage = grid.GetBedStage(x, y);
                if (stage == BedStage.Soil)
                    continue;

                float progress = 0f;
                if (stage == BedStage.Growing)
                    progress = growSeconds > 0f ? Mathf.Clamp01(1f - grid.GetBedGrow(x, y) / growSeconds) : 1f;
                // Cada canteiro balança no seu tempo.
                float phase = (x * 0.37f + y * 0.61f) % 1f * Mathf.PI * 2f;

                // Broto: pisa na borda de cima da terra e cresce enquanto brota; some no fim, quando vira nutriente.
                float eased = progress * progress * (3f - 2f * progress);
                float scale = Mathf.Lerp(_seedlingScale, _grownScale, eased);
                float fade = progress > 0.9f ? 1f - (progress - 0.9f) / 0.1f : 1f;
                SpriteRenderer sprout = Next(_sprout, new Color(1f, 1f, 1f, fade));
                sprout.transform.position = origin + new Vector3(x + 0.5f, y + 1f, 0f);
                sprout.transform.localScale = new Vector3(scale, scale, 1f);
                float sway = stage == BedStage.Growing ? _swayDegrees * Mathf.Sin(time * 2.2f + phase) * (0.4f + 0.6f * eased) : 0f;
                sprout.transform.localRotation = Quaternion.Euler(0f, 0f, sway);

                if (stage != BedStage.Growing)
                    continue;

                // Gota recém-posta, afundando na terra.
                if (progress < _soakPortion)
                {
                    float soak = progress / _soakPortion;
                    SpriteRenderer drop = Next(_drop, new Color(1f, 1f, 1f, 1f - soak));
                    drop.transform.position = origin + new Vector3(x + 0.5f, y + 0.8f - 0.5f * soak, 0f);
                    drop.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 0.35f, soak);
                    drop.transform.localRotation = Quaternion.identity;
                }

                // Nutrientes inchando onde vão nascer (só nos lados livres; o de cima nasce no lugar do broto).
                if (progress < _budStart)
                    continue;
                float bud = (progress - _budStart) / (1f - _budStart);
                float pulse = 1f + 0.08f * Mathf.Sin(time * 6f + phase);
                for (int s = 0; s < 4; s++)
                {
                    int nx = x + PixelGrid.SideDx[s], ny = y + PixelGrid.SideDy[s];
                    if (!grid.InBounds(nx, ny) || grid.IsSolid(nx, ny) || grid.HasNutrient(nx, ny))
                        continue;
                    SpriteRenderer b = Next(_bud, new Color(1f, 1f, 1f, Mathf.Clamp01(bud * 1.5f)));
                    b.transform.position = origin + new Vector3(nx + 0.5f, ny + 0.5f, 0f);
                    b.transform.localScale = Vector3.one * (0.2f + 0.65f * bud) * pulse;
                    b.transform.localRotation = Quaternion.identity;
                }
            }

            for (int i = _used; i < _renderers.Count; i++)
            {
                if (_renderers[i].gameObject.activeSelf)
                    _renderers[i].gameObject.SetActive(false);
            }
        }

        SpriteRenderer Next(Sprite sprite, Color color)
        {
            if (_used == _renderers.Count)
            {
                var go = new GameObject($"Bed_{_renderers.Count:000}");
                go.transform.SetParent(transform, false);
                var r = go.AddComponent<SpriteRenderer>();
                r.sortingOrder = _sortingOrder;
                _renderers.Add(r);
            }
            SpriteRenderer renderer = _renderers[_used++];
            if (!renderer.gameObject.activeSelf)
                renderer.gameObject.SetActive(true);
            renderer.sprite = sprite;
            renderer.color = color;
            return renderer;
        }

        // ---------------------------------------------------------------- sprites gerados

        void BuildSprites()
        {
            _sprout = MakeSprite("Sprout", new Vector2(0.5f, 0f), PaintSprout);
            _drop = MakeSprite("Drop", new Vector2(0.5f, 0.5f), PaintDrop);
            _bud = MakeSprite("NutrientBud", new Vector2(0.5f, 0.5f), PaintBud);
        }

        static Sprite MakeSprite(string spriteName, Vector2 pivot, System.Action<Color32[]> paint)
        {
            var painted = new Color32[Texels * Texels];
            paint(painted);
            // Experimento 8x8: pinta a 16, reduz para a densidade do mundo e põe o contorno preto, como os PNGs.
            const int size = Texels / 2;
            Color32[] pixels = PixelArtFilters.Halve(painted, Texels, Texels);
            PixelArtFilters.Outline(pixels, size, size);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = spriteName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), pivot, size);
            sprite.name = spriteName;
            return sprite;
        }

        static void Put(Color32[] pixels, int x, int y, Color32 color)
        {
            if (x >= 0 && x < Texels && y >= 0 && y < Texels)
                pixels[y * Texels + x] = color;
        }

        /// <summary>Haste grossa com duas folhas largas e um par de folhinhas na ponta, pisando na linha de baixo.</summary>
        void PaintSprout(Color32[] p)
        {
            var dark = new Color32((byte)(_leafColor.r * 0.7f), (byte)(_leafColor.g * 0.75f), (byte)(_leafColor.b * 0.7f), 255);
            FillEllipse(p, 4.2f, 6.2f, 3.6f, 1.9f, 25f, _leafColor, dark);
            FillEllipse(p, 11.6f, 8.8f, 3.6f, 1.9f, -25f, _leafColor, dark);
            FillEllipse(p, 6.2f, 13.2f, 2.2f, 1.3f, 40f, _leafColor, dark);
            FillEllipse(p, 9.8f, 13.4f, 2.2f, 1.3f, -40f, _leafColor, dark);
            for (int y = 0; y < 13; y++)
            {
                Put(p, 7, y, _stemColor);
                Put(p, 8, y, _stemColor);
            }
        }

        /// <summary>Elipse girada (graus) com a metade de baixo um pouco mais escura, para a folha ter volume.</summary>
        static void FillEllipse(Color32[] p, float cx, float cy, float rx, float ry, float degrees, Color32 color, Color32 shade)
        {
            float a = degrees * Mathf.Deg2Rad, cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            for (int y = 0; y < Texels; y++)
            for (int x = 0; x < Texels; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float u = dx * cos + dy * sin, v = -dx * sin + dy * cos;
                if (u * u / (rx * rx) + v * v / (ry * ry) <= 1f)
                    p[y * Texels + x] = v < -ry * 0.35f ? shade : color;
            }
        }

        /// <summary>Gota: ponta em cima, corpo redondo embaixo, com um brilho.</summary>
        void PaintDrop(Color32[] p)
        {
            for (int y = 0; y < Texels; y++)
            for (int x = 0; x < Texels; x++)
            {
                float dx = x + 0.5f - 8f, dy = y + 0.5f - 6f;
                // Círculo de raio 5 embaixo, afinando até a ponta em y = 14.
                float radius = dy <= 0f ? 5f : 5f * (1f - dy / 8.5f);
                if (dx * dx + (dy <= 0f ? dy * dy : 0f) <= radius * radius && dy < 8.5f)
                    p[y * Texels + x] = _waterColor;
            }
            var shine = new Color32(210, 235, 255, 255);
            Put(p, 6, 6, shine);
            Put(p, 6, 7, shine);
            Put(p, 7, 8, shine);
        }

        /// <summary>Nutriente: bolinha da cor do nutriente com sombra embaixo e brilho em cima.</summary>
        void PaintBud(Color32[] p)
        {
            var shade = new Color32((byte)(_nutrientColor.r * 0.75f), (byte)(_nutrientColor.g * 0.75f), (byte)(_nutrientColor.b * 0.6f), 255);
            var light = new Color32(255, 248, 200, 255);
            for (int y = 0; y < Texels; y++)
            for (int x = 0; x < Texels; x++)
            {
                float dx = x + 0.5f - 8f, dy = y + 0.5f - 8f;
                float d2 = dx * dx + dy * dy;
                if (d2 > 6.5f * 6.5f)
                    continue;
                p[y * Texels + x] = dy < -2.5f ? shade : _nutrientColor;
            }
            Put(p, 5, 10, light);
            Put(p, 6, 11, light);
            Put(p, 5, 11, light);
        }
    }
}
