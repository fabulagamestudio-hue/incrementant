using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Câmera ortográfica do formigário: arrastar com o botão do meio ou WASD/setas move, scroll ou Q/E dá zoom.
    /// A vista nunca sai dos limites do mundo (mais uma folga) e o zoom não abre mais que o enquadramento inicial,
    /// o que segura o custo de render e o tamanho do que precisa estar gerado.
    /// Zoom em passos inteiros (etapa 1 do documento 40): de <see cref="_pixelStep"/> px de tela por célula para cima, o
    /// zoom só para em múltiplos dele (16, 32), para cada pixel de arte (1/16 de célula) virar exatamente 1x1 ou 2x2 pixels
    /// da tela; a troca de nível é uma transição curta que termina no valor exato, e parada num nível a câmera se alinha à
    /// grade de pixels da arte. Mais aberto que isso o zoom é contínuo (a textura do terreno some aos poucos).
    /// </summary>
    [Serializable]
    public sealed class MapCamera
    {
        [Tooltip("Velocidade do teclado, em alturas de tela por segundo.")]
        [SerializeField, Min(0.1f)] float _panSpeed = 0.9f;
        [Tooltip("Zoom máximo, em pixels da tela por célula (múltiplo de Pixel Step; D7, 2026-10-21: até 32).")]
        [SerializeField, Min(1)] int _maxPixelsPerCell = 32;
        [Tooltip("Pixels de arte por célula: de Pixel Step px de tela por célula para cima, o zoom só para em múltiplos dele.")]
        [SerializeField, Min(1)] int _pixelStep = 16;
        [Tooltip("Duração da transição entre dois níveis de zoom, em segundos.")]
        [SerializeField, Min(0f)] float _levelSeconds = 0.12f;
        [Tooltip("Fator por passo de zoom (scroll).")]
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
        [NonSerialized] bool _moving;
        [NonSerialized] Vector3 _fromPosition;
        [NonSerialized] Vector3 _toPosition;
        [NonSerialized] float _fromSize;
        [NonSerialized] float _toSize;
        [NonSerialized] float _moveElapsed;
        [NonSerialized] float _moveDuration;
        [NonSerialized] int _screenHeight = 1080;
        // Nível de zoom em que a câmera está (px de tela por célula, múltiplo de _pixelStep), 0 = zoom contínuo.
        [NonSerialized] int _level;
        // Transição entre níveis: o ponto do mundo sob _zoomScreen fica parado.
        [NonSerialized] bool _zooming;
        [NonSerialized] float _zoomFrom;
        [NonSerialized] float _zoomTo;
        [NonSerialized] float _zoomElapsed;
        [NonSerialized] Vector2 _zoomScreen;
        [NonSerialized] Vector3 _zoomWorld;

        /// <summary>Há um deslocamento automático em andamento (tutorial): o input do jogador não move a câmera.</summary>
        public bool IsMoving => _moving;

        /// <summary>Pula direto para o enquadramento mais aberto (área inicial inteira), sem transição.</summary>
        public void SnapToOverview(Camera camera)
        {
            _moving = false;
            _zooming = false;
            _level = 0;
            _screenHeight = Mathf.Max(1, camera.pixelHeight);
            camera.orthographicSize = MaxHalfHeight(camera.aspect);
            camera.transform.position = new Vector3(_startView.center.x, _startView.center.y, camera.transform.position.z);
            Clamp(camera);
        }

        /// <summary>Menor e maior meia altura de vista permitidas, em células.</summary>
        public void HalfHeightRange(float aspect, out float min, out float max)
        {
            min = MinHalfHeight(aspect);
            max = MaxHalfHeight(aspect);
        }

        /// <summary>Desloca suavemente a câmera até <paramref name="center"/> com a meia altura dada (limitada ao zoom e ao mundo).</summary>
        public void MoveTo(Camera camera, Vector2 center, float halfHeight, float seconds)
        {
            HalfHeightRange(Mathf.Max(0.01f, camera.aspect), out float min, out float max);
            _fromPosition = camera.transform.position;
            _fromSize = camera.orthographicSize;
            _toSize = Mathf.Clamp(halfHeight, min, max);
            _toPosition = new Vector3(center.x, center.y, _fromPosition.z);
            _moveElapsed = 0f;
            _moveDuration = Mathf.Max(0.01f, seconds);
            _dragging = false;
            _moving = true;
        }

        /// <summary>Avança o deslocamento automático, se houver. Devolve true enquanto ele ainda dura (neste quadro).</summary>
        public bool StepTransition(Camera camera, float deltaTime)
        {
            if (!_moving)
                return false;
            _moveElapsed += deltaTime;
            float t = Mathf.Clamp01(_moveElapsed / _moveDuration);
            float eased = t * t * (3f - 2f * t);
            camera.orthographicSize = Mathf.Exp(Mathf.Lerp(Mathf.Log(_fromSize), Mathf.Log(_toSize), eased));
            camera.transform.position = Vector3.Lerp(_fromPosition, _toPosition, eased);
            _level = 0;
            if (t >= 1f)
            {
                _moving = false;
                // Termina num nível exato se parou na faixa dos níveis (o tutorial pede meias alturas quaisquer).
                SettleOnLevel(camera);
            }
            Clamp(camera);
            return true;
        }

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
            _moving = false;
            _zooming = false;
            _level = 0;
            _screenHeight = Mathf.Max(1, camera.pixelHeight);
            camera.orthographic = true;
            camera.orthographicSize = MaxHalfHeight(camera.aspect);
            Vector2 center = startView.center;
            if (_startPixelsPerCell > 0f)
            {
                camera.orthographicSize = camera.pixelHeight / (2f * _startPixelsPerCell);
                center = focus ?? center;
                SettleOnLevel(camera);
            }
            camera.transform.position = new Vector3(center.x, center.y, camera.transform.position.z);
            Clamp(camera);
        }

        /// <param name="pointerBlocked">O ponteiro está sobre o HUD: ignora scroll e arrasto novos.</param>
        public void Update(Camera camera, float deltaTime, bool pointerBlocked)
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            _screenHeight = Mathf.Max(1, camera.pixelHeight);

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
                // Q/E: contínuo abaixo dos níveis; neles, um nível por toque (segurar não acelera o salto).
                bool stepped = keyboard.qKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame;
                if (zoom != 0f && !_zooming)
                {
                    var center = new Vector2(camera.pixelWidth * 0.5f, camera.pixelHeight * 0.5f);
                    float next = camera.orthographicSize / Mathf.Pow(_keyZoomSpeed, zoom * deltaTime);
                    if (_level > 0 || PixelsPerCell(next) >= _pixelStep - LevelSnap)
                    {
                        if (stepped || _level == 0)
                            ZoomTo(camera, center, StepSize(camera.orthographicSize, zoom > 0f));
                    }
                    else
                    {
                        camera.orthographicSize = next;
                    }
                }
            }

            if (mouse != null)
            {
                Vector2 screen = mouse.position.ReadValue();
                bool ctrl = keyboard != null && keyboard.ctrlKey.isPressed;
                float scroll = mouse.scroll.ReadValue().y;
                if (!ctrl && scroll != 0f && !pointerBlocked)
                    ZoomAt(camera, screen, scroll > 0f);

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

            StepZoom(camera, deltaTime);
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

        /// <summary>Um passo de scroll: contínuo abaixo dos níveis, de nível em nível acima.</summary>
        void ZoomAt(Camera camera, Vector2 screen, bool zoomIn)
        {
            float size = _zooming ? _zoomTo : camera.orthographicSize;
            float next = zoomIn ? size / _zoomStep : size * _zoomStep;
            if (_level > 0 || PixelsPerCell(next) >= _pixelStep - LevelSnap)
            {
                ZoomTo(camera, screen, StepSize(size, zoomIn));
                return;
            }
            // Mantém o ponto sob o cursor parado durante o zoom.
            _zooming = false;
            Vector3 before = ScreenToWorld(camera, screen);
            camera.orthographicSize = Mathf.Clamp(next, MinHalfHeight(camera.aspect), MaxHalfHeight(camera.aspect));
            camera.transform.position += before - ScreenToWorld(camera, screen);
        }

        /// <summary>A menos disto (px por célula) do primeiro nível, o zoom contínuo trava nele.</summary>
        const float LevelSnap = 0.5f;

        float PixelsPerCell(float halfHeight) => _screenHeight / (2f * Mathf.Max(0.0001f, halfHeight));

        float SizeFor(int pixelsPerCell) => _screenHeight / (2f * pixelsPerCell);

        /// <summary>
        /// Meia altura do próximo nível a partir de <paramref name="size"/>: aproximando, o nível de cima (de fora dos
        /// níveis, o primeiro); afastando, o de baixo, e do primeiro nível volta ao zoom contínuo.
        /// </summary>
        float StepSize(float size, bool zoomIn)
        {
            int max = Mathf.Max(_pixelStep, _maxPixelsPerCell / _pixelStep * _pixelStep);
            int level = LevelOf(size);
            if (zoomIn)
                return SizeFor(level == 0 ? _pixelStep : Mathf.Min(level + _pixelStep, max));
            return level > _pixelStep ? SizeFor(level - _pixelStep) : size * _zoomStep;
        }

        /// <summary>Nível (px por célula) que a meia altura <paramref name="size"/> representa, ou 0 se não for um.</summary>
        int LevelOf(float size)
        {
            float ppc = PixelsPerCell(size);
            int level = Mathf.RoundToInt(ppc / _pixelStep) * _pixelStep;
            return level >= _pixelStep && Mathf.Abs(ppc - level) < 0.01f ? level : 0;
        }

        /// <summary>Começa a transição até <paramref name="size"/>, com o ponto sob <paramref name="screen"/> parado.</summary>
        void ZoomTo(Camera camera, Vector2 screen, float size)
        {
            _zoomFrom = camera.orthographicSize;
            _zoomTo = Mathf.Clamp(size, MinHalfHeight(camera.aspect), MaxHalfHeight(camera.aspect));
            _zoomScreen = screen;
            _zoomWorld = ScreenToWorld(camera, screen);
            _zoomElapsed = 0f;
            _zooming = true;
            _level = 0;
            if (_levelSeconds <= 0f)
                StepZoom(camera, 0f);
        }

        void StepZoom(Camera camera, float deltaTime)
        {
            if (!_zooming)
                return;
            _zoomElapsed += deltaTime;
            float t = _levelSeconds > 0f ? Mathf.Clamp01(_zoomElapsed / _levelSeconds) : 1f;
            float eased = t * t * (3f - 2f * t);
            camera.orthographicSize = Mathf.Exp(Mathf.Lerp(Mathf.Log(_zoomFrom), Mathf.Log(_zoomTo), eased));
            camera.transform.position += _zoomWorld - ScreenToWorld(camera, _zoomScreen);
            if (t >= 1f)
            {
                _zooming = false;
                SettleOnLevel(camera);
            }
        }

        /// <summary>Se o zoom está na faixa dos níveis, fixa no múltiplo de _pixelStep mais perto; senão, zoom contínuo.</summary>
        void SettleOnLevel(Camera camera)
        {
            _screenHeight = Mathf.Max(1, camera.pixelHeight);
            float ppc = PixelsPerCell(camera.orthographicSize);
            if (ppc < _pixelStep - LevelSnap)
            {
                _level = 0;
                return;
            }
            int max = Mathf.Max(_pixelStep, _maxPixelsPerCell / _pixelStep * _pixelStep);
            _level = Mathf.Clamp(Mathf.RoundToInt(ppc / _pixelStep) * _pixelStep, _pixelStep, max);
            camera.orthographicSize = SizeFor(_level);
        }

        void Clamp(Camera camera)
        {
            float aspect = Mathf.Max(0.01f, camera.aspect);
            // Parada num nível, a meia altura acompanha a resolução (janela redimensionada) para manter os px por célula.
            if (_level > 0 && !_zooming && !_moving)
                camera.orthographicSize = SizeFor(_level);
            camera.orthographicSize = Mathf.Clamp(camera.orthographicSize, MinHalfHeight(aspect), MaxHalfHeight(aspect));
            float halfHeight = camera.orthographicSize;
            Vector3 p = camera.transform.position;
            Vector2 c = ClampCenter(p, halfHeight * aspect, halfHeight, _bounds);
            if (_level > 0 && !_zooming && !_moving)
                c = SnapToPixels(camera, c);
            camera.transform.position = new Vector3(c.x, c.y, p.z);
        }

        /// <summary>
        /// Centro alinhado à grade de pixels da tela no nível atual: a borda de cada texel de arte cai na borda de um
        /// pixel. Com largura ou altura ímpar em pixels, o centro fica no meio de um pixel (meio pixel de deslocamento).
        /// </summary>
        Vector2 SnapToPixels(Camera camera, Vector2 center)
        {
            float pixel = 1f / _level;
            float offsetX = (camera.pixelWidth & 1) == 1 ? 0.5f * pixel : 0f;
            float offsetY = (camera.pixelHeight & 1) == 1 ? 0.5f * pixel : 0f;
            return new Vector2(Mathf.Round((center.x - offsetX) / pixel) * pixel + offsetX,
                               Mathf.Round((center.y - offsetY) / pixel) * pixel + offsetY);
        }

        /// <summary>Zoom mais aberto: o enquadramento inicial (área inicial + folga), como era a câmera fixa.</summary>
        float MaxHalfHeight(float aspect) =>
            Mathf.Max(_startView.height * 0.5f + _margin, (_startView.width * 0.5f + _margin) / Mathf.Max(0.01f, aspect));

        /// <summary>Zoom mais fechado: _maxPixelsPerCell px de tela por célula.</summary>
        float MinHalfHeight(float aspect) => Mathf.Min(SizeFor(Mathf.Max(1, _maxPixelsPerCell)), MaxHalfHeight(aspect));

        static Vector3 ScreenToWorld(Camera camera, Vector2 screen)
        {
            Vector3 w = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
            w.z = 0f;
            return w;
        }
    }
}
