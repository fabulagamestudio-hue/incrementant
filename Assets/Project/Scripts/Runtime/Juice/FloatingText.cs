using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.Juice
{
    /// <summary>
    /// Números flutuantes ("+12", "-40", "-1"): textos que sobem e somem. Canvas Overlay próprio, abaixo do HUD, com um
    /// pool fixo de <see cref="Capacity"/> textos (sem alocação por disparo; ao estourar, recicla o mais antigo). Pode
    /// ancorar no mundo (reprojeta todo quadro pela câmera) ou na tela. Ganhos repetidos são somados numa janela
    /// (<see cref="Gain"/>). Completo = sobe e some; Reduzido = fica parado e some; Desligado = nada. Tempo não escalado.
    /// </summary>
    public sealed class FloatingText : MonoBehaviour
    {
        public const int Capacity = 12;
        const int FontSize = 24;               // múltiplo de 8: a fonte é Press Start 2P
        const float Life = 0.9f;
        const float RisePixels = 44f;
        const float GainWindow = 0.5f;

        public static readonly Color GoldColor = new Color32(232, 214, 92, 255);

        struct Item
        {
            public Text Text;
            public bool Active;
            public bool World;
            public Vector3 Anchor;   // mundo ou tela, conforme World
            public float Age;
            public Color Color;
        }

        Item[] _items;
        Camera _camera;
        readonly FxAggregator _gain = new FxAggregator(GainWindow);
        Vector3 _gainAnchor;

        /// <summary>Cria o canvas e o pool. O objeto não tem pai: quem cria o destrói junto com o HUD.</summary>
        public static FloatingText Create(Font font, Camera camera)
        {
            var go = new GameObject("FloatingText", typeof(RectTransform), typeof(Canvas));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -1; // abaixo do HUD (ordem 0): menus e painéis cobrem os números
            FloatingText ft = go.AddComponent<FloatingText>();
            ft._camera = camera;
            ft.Build(font);
            return ft;
        }

        void Build(Font font)
        {
            _items = new Item[Capacity];
            for (int i = 0; i < Capacity; i++)
            {
                var go = new GameObject($"Text_{i:00}", typeof(RectTransform), typeof(Text), typeof(Outline));
                go.transform.SetParent(transform, false);
                var text = go.GetComponent<Text>();
                text.font = font;
                text.fontSize = FontSize;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.raycastTarget = false; // não bloqueia o clique no mundo
                var outline = go.GetComponent<Outline>();
                outline.effectColor = new Color(0.12f, 0.08f, 0.05f, 1f);
                outline.effectDistance = new Vector2(2f, -2f);
                go.SetActive(false);
                _items[i].Text = text;
            }
        }

        /// <summary>Soma <paramref name="amount"/> ao ganho pendente; sai como "+N" dourado quando a janela vence.</summary>
        public void Gain(int amount, Vector3 world)
        {
            if (!Vfx.Motion)
                return;
            _gainAnchor = world;
            _gain.Add(amount, Time.unscaledTime);
        }

        public void SpawnWorld(Vector3 world, string text, Color color) => Spawn(true, world, text, color);

        public void SpawnScreen(Vector2 screen, string text, Color color) => Spawn(false, screen, text, color);

        void Spawn(bool world, Vector3 anchor, string text, Color color)
        {
            if (_items == null || !Vfx.Motion)
                return;
            int slot = 0;
            float oldest = -1f;
            for (int i = 0; i < _items.Length; i++)
            {
                if (!_items[i].Active)
                {
                    slot = i;
                    oldest = float.PositiveInfinity;
                    break;
                }
                if (_items[i].Age > oldest)
                {
                    oldest = _items[i].Age;
                    slot = i;
                }
            }
            Item item = _items[slot];
            item.Active = true;
            item.World = world;
            item.Anchor = anchor;
            item.Age = 0f;
            item.Color = color;
            item.Text.text = text;
            item.Text.color = color;
            item.Text.gameObject.SetActive(true);
            _items[slot] = item;
            Place(ref _items[slot], 0f);
        }

        void Update()
        {
            if (_items == null)
                return;
            if (_gain.TryFlush(Time.unscaledTime, out int total) && total > 0)
                SpawnWorld(_gainAnchor, "+" + total, GoldColor);

            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _items.Length; i++)
            {
                if (!_items[i].Active)
                    continue;
                _items[i].Age += dt;
                float t = _items[i].Age / Life;
                if (t >= 1f || !Vfx.Motion)
                {
                    _items[i].Active = false;
                    _items[i].Text.gameObject.SetActive(false);
                    continue;
                }
                Place(ref _items[i], t);
            }
        }

        void Place(ref Item item, float t)
        {
            Vector2 screen;
            if (item.World)
            {
                if (_camera == null)
                    return;
                Vector3 p = _camera.WorldToScreenPoint(item.Anchor);
                screen = new Vector2(p.x, p.y);
            }
            else
            {
                screen = item.Anchor;
            }
            float rise = Vfx.Strong ? RisePixels * Easing.OutCubic(t) : 0f;
            item.Text.rectTransform.position = new Vector3(Mathf.Round(screen.x), Mathf.Round(screen.y + rise), 0f);
            // Cheio até 60% da vida, depois some.
            Color c = item.Color;
            c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            item.Text.color = c;
        }
    }
}
