using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.Juice
{
    /// <summary>
    /// Números flutuantes ("+12", "-40", "-1"): textos que sobem e somem. Canvas Overlay próprio, abaixo do HUD, com um
    /// pool fixo de <see cref="Capacity"/> textos (sem alocação por disparo; ao estourar, recicla o mais antigo). Pode
    /// ancorar no mundo (reprojeta todo quadro pela câmera) ou na tela. Ganho, dano e gasto repetidos são somados numa
    /// janela por canal (<see cref="Gain"/>, <see cref="Hurt"/>, <see cref="Spent"/>), ancorados onde o primeiro aconteceu.
    /// O canvas copia o <see cref="CanvasScaler"/> do HUD, então fonte e subida acompanham a escala da interface.
    /// Completo = sobe e some; Reduzido = fica parado e some; Desligado = nada. Tempo não escalado.
    /// </summary>
    public sealed class FloatingText : MonoBehaviour
    {
        public const int Capacity = 12;
        const int FontSize = 24;               // múltiplo de 8: a fonte é Press Start 2P (unidades do canvas de referência)
        const float Life = 0.9f;
        const float RisePixels = 44f;          // unidades do canvas de referência; multiplicado pelo scaleFactor
        const float GainWindow = 0.5f;
        const float HurtWindow = 0.3f;
        const float SpentWindow = 0.5f;

        public static readonly Color GoldColor = new Color32(232, 214, 92, 255);
        public static readonly Color LossColor = new Color32(200, 72, 60, 255);

        // Soma de um tipo de número; o texto sai onde o primeiro evento da janela aconteceu.
        struct Channel
        {
            public FxAggregator Sum;
            public bool World;
            public Vector3 Anchor;
            public string Sign;
            public Color Color;
        }

        const int GainChannel = 0;
        const int HurtChannel = 1;
        const int SpentChannel = 2;

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
        Canvas _canvas;
        readonly Channel[] _channels =
        {
            new Channel { Sum = new FxAggregator(GainWindow), World = true, Sign = "+", Color = GoldColor },
            new Channel { Sum = new FxAggregator(HurtWindow), World = true, Sign = "-", Color = LossColor },
            new Channel { Sum = new FxAggregator(SpentWindow), World = false, Sign = "-", Color = LossColor },
        };

        /// <summary>
        /// Cria o canvas e o pool. O objeto não tem pai: quem cria o destrói junto com o HUD. <paramref name="hudScaler"/>
        /// (opcional) é copiado para que os números tenham a mesma escala da interface.
        /// </summary>
        public static FloatingText Create(Font font, Camera camera, CanvasScaler hudScaler = null)
        {
            var go = new GameObject("FloatingText", typeof(RectTransform), typeof(Canvas));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -1; // abaixo do HUD (ordem 0): menus e painéis cobrem os números
            if (hudScaler != null)
            {
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = hudScaler.uiScaleMode;
                scaler.referenceResolution = hudScaler.referenceResolution;
                scaler.screenMatchMode = hudScaler.screenMatchMode;
                scaler.matchWidthOrHeight = hudScaler.matchWidthOrHeight;
                scaler.scaleFactor = hudScaler.scaleFactor;
            }
            FloatingText ft = go.AddComponent<FloatingText>();
            ft._camera = camera;
            ft._canvas = canvas;
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
        public void Gain(int amount, Vector3 world) => Add(GainChannel, amount, world);

        /// <summary>Soma dano recebido (formigas feridas); sai como "-N" vermelho-terra onde o primeiro golpe da janela caiu.</summary>
        public void Hurt(int amount, Vector3 world) => Add(HurtChannel, amount, world);

        /// <summary>Soma nutrientes gastos; sai como "-N" vermelho-terra no ponto de tela dado (o cartão do contador).</summary>
        public void Spent(int amount, Vector2 screen) => Add(SpentChannel, amount, screen);

        void Add(int channel, int amount, Vector3 anchor)
        {
            if (!Vfx.Motion || amount <= 0)
                return;
            ref Channel ch = ref _channels[channel];
            if (!ch.Sum.Open)
                ch.Anchor = anchor;
            ch.Sum.Add(amount, Time.unscaledTime);
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
            for (int c = 0; c < _channels.Length; c++)
            {
                // Um texto por janela (no máximo ~2-3 por segundo por canal): a string do número é a única alocação.
                if (_channels[c].Sum.TryFlush(Time.unscaledTime, out int total) && total > 0)
                    Spawn(_channels[c].World, _channels[c].Anchor, _channels[c].Sign + total, _channels[c].Color);
            }

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
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            float rise = Vfx.Strong ? RisePixels * scale * Easing.OutCubic(t) : 0f;
            item.Text.rectTransform.position = new Vector3(Mathf.Round(screen.x), Mathf.Round(screen.y + rise), 0f);
            // Cheio até 60% da vida, depois some.
            Color c = item.Color;
            c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            item.Text.color = c;
        }
    }
}
