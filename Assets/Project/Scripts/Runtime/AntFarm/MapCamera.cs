using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Câmera ortográfica do formigário: arrastar com o botão do meio ou WASD/setas move, Ctrl+scroll ou Q/E dá zoom.
    /// A vista nunca sai dos limites do mundo (mais uma folga) e o zoom não abre mais que o enquadramento inicial,
    /// o que segura o custo de render e o tamanho do que precisa estar gerado.
    /// </summary>
    [Serializable]
    public sealed class MapCamera
    {
        [Tooltip("Velocidade do teclado, em alturas de tela por segundo.")]
        [SerializeField, Min(0.1f)] float _panSpeed = 0.9f;
        [Tooltip("Meia altura mínima da vista, em células (zoom máximo).")]
        [SerializeField, Min(4f)] float _minHalfHeight = 24f;
        [Tooltip("Fator por passo de zoom (Ctrl+scroll).")]
        [SerializeField, Range(1.02f, 2f)] float _zoomStep = 1.15f;
        [Tooltip("Velocidade do zoom por teclado (Q/E), em fator por segundo.")]
        [SerializeField, Min(1.1f)] float _keyZoomSpeed = 2.5f;
        [Tooltip("Zoom inicial em pixels da tela por célula (16 = cada célula vira um bloco de 16x16 na tela). 0 = enquadra a área inicial inteira.")]
        [SerializeField, Min(0f)] float _startPixelsPerCell = 16f;

        [NonSerialized] Rect _bounds;
        [NonSerialized] Rect _startView;
        [NonSerialized] float _margin;
        [NonSerialized] bool _dragging;
        [NonSerialized] Vector3 _dragAnchor;

        /// <summary>
        /// Enquadra <paramref name="startView"/> (com <paramref name="margin"/>) e passa a limitar a câmera a
        /// <paramref name="bounds"/>, ambos em unidades de mundo. Com <b>Start Pixels Per Cell</b>, começa mais
        /// perto, centrada em <paramref name="focus"/> (ou no centro da vista inicial, se não vier).
        /// </summary>
        public void Reset(Camera camera, Rect bounds, Rect startView, float margin, Vector2? focus = null)
        {
            _bounds = bounds;
            _startView = startView;
            _margin = margin;
            _dragging = false;
            camera.orthographic = true;
            camera.orthographicSize = MaxHalfHeight(camera.aspect);
            Vector2 center = startView.center;
            if (_startPixelsPerCell > 0f)
            {
                camera.orthographicSize = camera.pixelHeight / (2f * _startPixelsPerCell);
                center = focus ?? center;
            }
            camera.transform.position = new Vector3(center.x, center.y, camera.transform.position.z);
            Clamp(camera);
        }

        /// <param name="pointerBlocked">O ponteiro está sobre o HUD: ignora scroll e arrasto novos.</param>
        public void Update(Camera camera, float deltaTime, bool pointerBlocked)
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null)
            {
                Vector2 dir = Vector2.zero;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) dir.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) dir.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) dir.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) dir.y += 1f;
                if (dir != Vector2.zero)
                    camera.transform.position += (Vector3)(dir.normalized * (_panSpeed * 2f * camera.orthographicSize * deltaTime));

                float zoom = 0f;
                if (keyboard.qKey.isPressed) zoom -= 1f;
                if (keyboard.eKey.isPressed) zoom += 1f;
                if (zoom != 0f)
                    camera.orthographicSize /= Mathf.Pow(_keyZoomSpeed, zoom * deltaTime);
            }

            if (mouse != null)
            {
                Vector2 screen = mouse.position.ReadValue();
                bool ctrl = keyboard != null && keyboard.ctrlKey.isPressed;
                float scroll = mouse.scroll.ReadValue().y;
                if (ctrl && scroll != 0f && !pointerBlocked)
                    ZoomAt(camera, screen, scroll > 0f ? 1f / _zoomStep : _zoomStep);

                if (mouse.middleButton.wasPressedThisFrame && !pointerBlocked)
                {
                    _dragging = true;
                    _dragAnchor = ScreenToWorld(camera, screen);
                }
                if (!mouse.middleButton.isPressed)
                    _dragging = false;
                if (_dragging)
                    camera.transform.position += _dragAnchor - ScreenToWorld(camera, screen);
            }

            Clamp(camera);
        }

        /// <summary>Retângulo visível, em unidades de mundo.</summary>
        public static Rect ViewRect(Camera camera)
        {
            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * camera.aspect;
            Vector3 c = camera.transform.position;
            return new Rect(c.x - halfWidth, c.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
        }

        /// <summary>
        /// Centro mais próximo de <paramref name="center"/> que mantém a vista dentro de <paramref name="bounds"/>.
        /// Num eixo em que a vista é maior que os limites, centraliza.
        /// </summary>
        public static Vector2 ClampCenter(Vector2 center, float halfWidth, float halfHeight, Rect bounds)
        {
            center.x = halfWidth * 2f >= bounds.width ? bounds.center.x : Mathf.Clamp(center.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth);
            center.y = halfHeight * 2f >= bounds.height ? bounds.center.y : Mathf.Clamp(center.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight);
            return center;
        }

        void ZoomAt(Camera camera, Vector2 screen, float factor)
        {
            // Mantém o ponto sob o cursor parado durante o zoom.
            Vector3 before = ScreenToWorld(camera, screen);
            camera.orthographicSize = Mathf.Clamp(camera.orthographicSize * factor, MinHalfHeight(camera.aspect), MaxHalfHeight(camera.aspect));
            camera.transform.position += before - ScreenToWorld(camera, screen);
        }

        void Clamp(Camera camera)
        {
            float aspect = Mathf.Max(0.01f, camera.aspect);
            camera.orthographicSize = Mathf.Clamp(camera.orthographicSize, MinHalfHeight(aspect), MaxHalfHeight(aspect));
            float halfHeight = camera.orthographicSize;
            Vector3 p = camera.transform.position;
            Vector2 c = ClampCenter(p, halfHeight * aspect, halfHeight, _bounds);
            camera.transform.position = new Vector3(c.x, c.y, p.z);
        }

        /// <summary>Zoom mais aberto: o enquadramento inicial (área inicial + folga), como era a câmera fixa.</summary>
        float MaxHalfHeight(float aspect) =>
            Mathf.Max(_startView.height * 0.5f + _margin, (_startView.width * 0.5f + _margin) / Mathf.Max(0.01f, aspect));

        float MinHalfHeight(float aspect) => Mathf.Min(_minHalfHeight, MaxHalfHeight(aspect));

        static Vector3 ScreenToWorld(Camera camera, Vector2 screen)
        {
            Vector3 w = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
            w.z = 0f;
            return w;
        }
    }
}
