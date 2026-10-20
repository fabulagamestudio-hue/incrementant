using UnityEngine;
using UnityEngine.EventSystems;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Janela arrastável da árvore da tela Evolução: arrastar move o conteúdo, a roda do mouse aproxima ou afasta em torno
    /// do ponteiro. O conteúdo (<see cref="_content"/>) tem o centro da árvore na origem e é ancorado no centro desta
    /// janela; a posição fica presa para o centro do formigueiro nunca sair de vista por mais que o raio do conteúdo.
    /// Os botões dos nós não tratam arrasto, então o arrasto que começa num nó chega aqui (e o EventSystem cancela o clique).
    /// </summary>
    public sealed class TreeViewport : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        [SerializeField] RectTransform _content;
        [Tooltip("Raio do conteúdo em pixels de UI (UpgradeLayout.ContentRadius).")]
        [SerializeField, Min(100f)] float _contentRadius = 2000f;
        [SerializeField, Range(0.1f, 1f)] float _minZoom = 0.3f;
        [SerializeField, Range(0.5f, 3f)] float _maxZoom = 1.25f;
        [SerializeField, Range(0.1f, 2f)] float _startZoom = 0.6f;
        [Tooltip("Fator por clique da roda.")]
        [SerializeField, Range(1.01f, 1.5f)] float _zoomStep = 1.12f;

        RectTransform _rect;
        Canvas _canvas;
        float _zoom = 1f;

        public float Zoom => _zoom;
        public bool Dragging { get; private set; }
        public RectTransform Content => _content;

        void Awake()
        {
            _rect = (RectTransform)transform;
            if (_content == null)
            {
                Debug.LogError($"{name}: TreeViewport sem conteúdo (rode Incrementant/Setup/Montar HUD da partida).", this);
                enabled = false;
                return;
            }
            _zoom = _content.localScale.x;
        }

        /// <summary>Volta ao enquadramento inicial: centro da árvore no meio da janela, zoom de abertura.</summary>
        public void ResetView()
        {
            SetZoom(_startZoom);
            CenterOn(Vector2.zero);
        }

        /// <summary>Põe o ponto do conteúdo (coordenadas da árvore, y para cima) no meio da janela.</summary>
        public void CenterOn(Vector2 contentPoint)
        {
            if (_content == null)
                return;
            _content.anchoredPosition = -contentPoint * _zoom;
            Clamp();
        }

        public void OnBeginDrag(PointerEventData eventData) => Dragging = eventData.button == PointerEventData.InputButton.Left;

        public void OnDrag(PointerEventData eventData)
        {
            if (!Dragging || _content == null)
                return;
            _content.anchoredPosition += eventData.delta / ScaleFactor();
            Clamp();
        }

        public void OnEndDrag(PointerEventData eventData) => Dragging = false;

        public void OnScroll(PointerEventData eventData)
        {
            if (_content == null || Mathf.Approximately(eventData.scrollDelta.y, 0f))
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera,
                                                                         out Vector2 local))
                return;
            // O ponto do conteúdo sob o ponteiro continua sob o ponteiro depois do zoom.
            Vector2 offset = local - _rect.rect.center;
            Vector2 contentPoint = (offset - _content.anchoredPosition) / _zoom;
            SetZoom(_zoom * Mathf.Pow(_zoomStep, Mathf.Sign(eventData.scrollDelta.y)));
            _content.anchoredPosition = offset - contentPoint * _zoom;
            Clamp();
        }

        void SetZoom(float zoom)
        {
            _zoom = Mathf.Clamp(zoom, _minZoom, _maxZoom);
            _content.localScale = new Vector3(_zoom, _zoom, 1f);
        }

        void Clamp()
        {
            float limit = _contentRadius * _zoom;
            Vector2 p = _content.anchoredPosition;
            _content.anchoredPosition = new Vector2(Mathf.Clamp(p.x, -limit, limit), Mathf.Clamp(p.y, -limit, limit));
        }

        float ScaleFactor()
        {
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();
            return _canvas != null ? Mathf.Max(0.01f, _canvas.rootCanvas.scaleFactor) : 1f;
        }
    }
}
