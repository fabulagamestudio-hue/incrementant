using System.Collections.Generic;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Barras de progresso pequenas ao lado do que é produzido (KI-026), no mesmo padrão da barra de postura da rainha
    /// (<see cref="AntSwarmView"/>): fundo escuro com borda e enchimento claro; cinza = o ciclo está pronto mas algo impede.
    /// Uso por frame: <see cref="Begin"/>, uma chamada a <see cref="Add"/> por barra, <see cref="End"/>. Os objetos são reaproveitados.
    /// </summary>
    public sealed class ProgressBarsView : MonoBehaviour
    {
        [Tooltip("Acima das formigas (AntSwarmView usa 10, e a barra da rainha 12 e 13).")]
        [SerializeField] int _sortingOrder = 12;
        [SerializeField, Min(0f)] float _border = 0.25f;
        [SerializeField] Color _backColor = new Color(0.1f, 0.07f, 0.05f, 0.85f);
        [SerializeField] Color _fillColor = new Color(0.94f, 0.87f, 0.75f, 1f);
        [SerializeField] Color _blockedColor = new Color(0.55f, 0.52f, 0.5f, 1f);

        struct Bar
        {
            public SpriteRenderer Back, Fill;
        }

        readonly List<Bar> _bars = new List<Bar>();
        Sprite _pixel;
        int _used;

        public void Begin() => _used = 0;

        /// <param name="center">Centro da barra no mundo.</param>
        /// <param name="width">Largura em células; a altura é 1.</param>
        /// <param name="progress">0 a 1.</param>
        /// <param name="blocked">Cor de "pronto, mas parado".</param>
        public void Add(Vector3 center, float width, float progress, bool blocked = false)
        {
            if (_used == _bars.Count)
                _bars.Add(new Bar { Back = CreatePart("BarBack", _sortingOrder, _backColor), Fill = CreatePart("BarFill", _sortingOrder + 1, _fillColor) });
            Bar bar = _bars[_used++];
            if (!bar.Back.gameObject.activeSelf)
            {
                bar.Back.gameObject.SetActive(true);
                bar.Fill.gameObject.SetActive(true);
            }
            bar.Back.transform.position = center;
            bar.Back.transform.localScale = new Vector3(width + 2f * _border, 1f + 2f * _border, 1f);
            float filled = Mathf.Max(0.001f, width * Mathf.Clamp01(progress));
            bar.Fill.transform.position = center + new Vector3((filled - width) * 0.5f, 0f, 0f);
            bar.Fill.transform.localScale = new Vector3(filled, 1f, 1f);
            bar.Fill.color = blocked ? _blockedColor : _fillColor;
        }

        /// <summary>Esconde as barras que não foram pedidas neste frame.</summary>
        public void End()
        {
            for (int i = _used; i < _bars.Count; i++)
            {
                if (!_bars[i].Back.gameObject.activeSelf)
                    break; // as seguintes já estavam escondidas
                _bars[i].Back.gameObject.SetActive(false);
                _bars[i].Fill.gameObject.SetActive(false);
            }
        }

        SpriteRenderer CreatePart(string objectName, int sortingOrder, Color color)
        {
            if (_pixel == null)
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "ProgressBarPixel" };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _pixel;
            renderer.sortingOrder = sortingOrder;
            renderer.color = color;
            return renderer;
        }
    }
}
