using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Marca os cupins vivos na tela: um círculo vermelho em volta de cada um e, se estiver fora da tela, uma seta vermelha
    /// na borda apontando para ele. A marca some quando o cupim morre. Canvas próprio, criado em código (sem mexer na cena),
    /// com os gráficos sem alvo de raio para não bloquear o clique no mundo.
    /// </summary>
    public sealed class TermiteMarkers
    {
        const float EdgeInset = 28f;
        const float MinRingPixels = 36f;
        const float RingCells = 6f;
        const float ArrowPixels = 36f;
        static readonly Color MarkerColor = new Color(0.92f, 0.12f, 0.1f, 0.95f);

        sealed class Marker
        {
            public RectTransform Root;
            public Image Ring;
            public Image Arrow;
        }

        readonly List<Marker> _markers = new List<Marker>();
        GameObject _canvasObject;
        RectTransform _canvasRect;
        Sprite _ringSprite;
        Sprite _arrowSprite;

        /// <summary>Atualiza as marcas deste quadro. <paramref name="simulationTime"/> é o tempo do desenho (voo do cupim).</summary>
        public void Update(Camera camera, Vector3 origin, IReadOnlyList<Termite> termites, float simulationTime)
        {
            int used = 0;
            if (camera != null && termites != null)
            {
                EnsureCanvas();
                float cellPixels = Mathf.Abs(camera.WorldToScreenPoint(origin + Vector3.right).x - camera.WorldToScreenPoint(origin).x);
                float ringSize = Mathf.Max(MinRingPixels, cellPixels * RingCells);
                var screenRect = new Rect(0f, 0f, Screen.width, Screen.height);
                Vector2 center = screenRect.center;
                Rect inner = Rect.MinMaxRect(EdgeInset, EdgeInset, Screen.width - EdgeInset, Screen.height - EdgeInset);

                foreach (Termite termite in termites)
                {
                    if (!termite.Alive)
                        continue;
                    Vector3 screen = camera.WorldToScreenPoint(origin + CellPosition(termite, simulationTime));
                    var point = new Vector2(screen.x, screen.y);
                    Marker marker = Acquire(used++);
                    bool visible = screenRect.Contains(point);
                    marker.Ring.gameObject.SetActive(visible);
                    marker.Arrow.gameObject.SetActive(!visible);
                    if (visible)
                    {
                        marker.Root.anchoredPosition = point;
                        marker.Ring.rectTransform.sizeDelta = new Vector2(ringSize, ringSize);
                    }
                    else
                    {
                        // A seta fica na borda, na linha do centro da tela até o cupim, e aponta para ele.
                        Vector2 dir = point - center;
                        float scale = Mathf.Min(
                            dir.x != 0f ? (inner.width * 0.5f) / Mathf.Abs(dir.x) : float.MaxValue,
                            dir.y != 0f ? (inner.height * 0.5f) / Mathf.Abs(dir.y) : float.MaxValue);
                        marker.Root.anchoredPosition = center + dir * scale;
                        marker.Arrow.rectTransform.sizeDelta = new Vector2(ArrowPixels, ArrowPixels);
                        marker.Arrow.rectTransform.localRotation =
                            Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
                    }
                }
            }
            for (int i = used; i < _markers.Count; i++)
            {
                if (_markers[i].Root.gameObject.activeSelf)
                    _markers[i].Root.gameObject.SetActive(false);
            }
        }

        /// <summary>Libera o canvas e os sprites criados em código.</summary>
        public void Destroy()
        {
            if (_canvasObject != null)
                Object.Destroy(_canvasObject);
            if (_ringSprite != null)
            {
                Object.Destroy(_ringSprite.texture);
                Object.Destroy(_ringSprite);
            }
            if (_arrowSprite != null)
            {
                Object.Destroy(_arrowSprite.texture);
                Object.Destroy(_arrowSprite);
            }
            _markers.Clear();
            _canvasObject = null;
            _ringSprite = _arrowSprite = null;
        }

        /// <summary>Posição desenhada do cupim em células (no voo, a mesma interpolação do desenho do enxame).</summary>
        static Vector3 CellPosition(Termite termite, float simulationTime)
        {
            var landing = new Vector2(termite.X, termite.Y);
            Vector2 cell = landing;
            if (termite.Phase == TermitePhase.Flying)
            {
                float progress = Mathf.Clamp01((simulationTime - termite.PhaseStart) / Mathf.Max(0.01f, termite.PhaseDuration));
                float eased = 1f - (1f - progress) * (1f - progress);
                cell = Vector2.Lerp(new Vector2(termite.FlyFromX, termite.FlyFromY), landing, eased);
            }
            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        Marker Acquire(int index)
        {
            while (_markers.Count <= index)
                _markers.Add(CreateMarker(_markers.Count));
            Marker marker = _markers[index];
            if (!marker.Root.gameObject.activeSelf)
                marker.Root.gameObject.SetActive(true);
            return marker;
        }

        Marker CreateMarker(int index)
        {
            var root = new GameObject($"TermiteMarker_{index:000}", typeof(RectTransform));
            root.transform.SetParent(_canvasRect, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return new Marker { Root = rect, Ring = CreateImage(rect, "Ring", _ringSprite), Arrow = CreateImage(rect, "Arrow", _arrowSprite) };
        }

        static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = MarkerColor;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            return image;
        }

        void EnsureCanvas()
        {
            if (_canvasObject != null)
                return;
            _ringSprite = MakeRingSprite(64, 5f);
            _arrowSprite = MakeArrowSprite(64);
            _canvasObject = new GameObject("TermiteMarkers", typeof(RectTransform), typeof(Canvas));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -1; // overlay abaixo do HUD (ordem 0): menus e painéis cobrem as marcas
            _canvasRect = (RectTransform)_canvasObject.transform;
        }

        internal static Sprite MakeRingSprite(int size, float thickness)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            float radius = size * 0.5f - 1f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f));
                float alpha = Mathf.Clamp01(radius - d + 0.5f) * Mathf.Clamp01(d - (radius - thickness) + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>Seta apontando para cima (o código a gira), com a ponta no topo.</summary>
        internal static Sprite MakeArrowSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float v = (y + 0.5f) / size;
                float u = Mathf.Abs((x + 0.5f) / size - 0.5f);
                // Triângulo: do topo (v = 0,95) abre até a base (v = 0,1), largura máxima 0,45 de cada lado.
                float halfWidth = Mathf.InverseLerp(0.95f, 0.1f, v) * 0.45f;
                float alpha = v >= 0.1f && v <= 0.95f ? Mathf.Clamp01((halfWidth - u) * size * 0.5f + 0.5f) : 0f;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
