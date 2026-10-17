using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Desenha as formigas como sprites próprios, em resolução maior que a do terreno.
    /// Só visual: interpola a posição entre células, mantém as patas voltadas para a superfície em que ela está
    /// (piso, parede, teto ou quina), anima as patas pela distância andada e mostra o pixel carregado na mandíbula.
    /// Os sprites têm o pivô na altura das patas, a meia célula do centro, para pisarem na borda da célula sólida.
    /// </summary>
    public sealed class AntSwarmView : MonoBehaviour
    {
        [Tooltip("Quadros da caminhada de lado (piso, parede, teto), virados para a direita, patas para baixo.")]
        [SerializeField] Sprite[] _walkFrames;
        [Tooltip("Quadros da caminhada vista de cima, virados para a direita, usados no fundo do formigueiro. Vazio = usa os de lado.")]
        [SerializeField] Sprite[] _topFrames;
        [Tooltip("Quadros de animação trocados por célula andada.")]
        [SerializeField, Min(0.1f)] float _framesPerCell = 1.5f;
        [Tooltip("Quadros por segundo enquanto cava.")]
        [SerializeField, Min(0f)] float _digFramesPerSecond = 10f;
        [Tooltip("Velocidade de giro (graus por segundo).")]
        [SerializeField, Min(1f)] float _turnSpeed = 900f;
        [SerializeField] int _sortingOrder = 10;
        [Tooltip("Posição do pixel carregado em relação ao pivô (unidades = células), formiga virada para a direita.")]
        [SerializeField] Vector2 _carriedOffset = new Vector2(1.2f, 0.5f);
        [Tooltip("Posição do pixel carregado na vista de cima (pivô no centro do sprite).")]
        [SerializeField] Vector2 _carriedOffsetTop = new Vector2(1.35f, 0f);
        [SerializeField, Range(0.2f, 1.5f)] float _carriedSize = 0.8f;

        sealed class AntVisual
        {
            public GameObject GameObject;
            public Transform Transform;
            public SpriteRenderer Body;
            public SpriteRenderer Carried;
            public float Angle;
            public bool FlipX;
            public float Walked;
            public Vector2 LastPosition;
            public bool Initialized;
        }

        readonly List<AntVisual> _visuals = new List<AntVisual>();
        Texture2D _pixelTexture;
        Sprite _pixelSprite;

        public bool HasFrames => _walkFrames != null && _walkFrames.Length > 0 && _walkFrames[0] != null;
        bool HasTopFrames => _topFrames != null && _topFrames.Length > 0 && _topFrames[0] != null;

        /// <param name="simulationTime">Tempo simulado atual, incluindo a fração ainda não processada do passo.</param>
        public void Render(PixelGrid grid, IReadOnlyList<Ant> ants, Vector3 origin, float simulationTime, float deltaTime)
        {
            EnsurePixelSprite();
            while (_visuals.Count < ants.Count)
                _visuals.Add(CreateVisual(_visuals.Count));

            for (int i = 0; i < ants.Count; i++)
                UpdateVisual(_visuals[i], ants[i], grid, origin, simulationTime, deltaTime);
            for (int i = ants.Count; i < _visuals.Count; i++)
            {
                if (_visuals[i].GameObject.activeSelf)
                    _visuals[i].GameObject.SetActive(false);
                _visuals[i].Initialized = false;
            }
        }

        void UpdateVisual(AntVisual visual, Ant ant, PixelGrid grid, Vector3 origin, float time, float deltaTime)
        {
            if (!visual.GameObject.activeSelf)
                visual.GameObject.SetActive(true);

            float t = ant.MoveDuration > 0f ? Mathf.Clamp01((time - ant.MoveStart) / ant.MoveDuration) : 1f;
            var cell = new Vector2(Mathf.Lerp(ant.FromX, ant.X, t), Mathf.Lerp(ant.FromY, ant.Y, t));
            visual.Transform.position = origin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

            // Direção: o último passo, ou o pixel que está cavando.
            Vector2 heading = ant.State == AntState.Digging
                ? new Vector2(ant.DigX - ant.X, ant.DigY - ant.Y)
                : new Vector2(ant.X - ant.FromX, ant.Y - ant.FromY);
            if (!visual.Initialized)
            {
                visual.Angle = 0f;
                visual.FlipX = ant.FacingX < 0;
                visual.LastPosition = cell;
                visual.Initialized = true;
            }

            // No meio de um túnel, sem nada encostado, ela está na parede de trás: vista de cima, girando livre.
            bool topView = HasTopFrames
                           && AntNavigation.IsTunnelBackground(grid, ant.X, ant.Y)
                           && !AntNavigation.TouchesSolid(grid, ant.X, ant.Y);
            if (topView)
                UpdateTopOrientation(visual, heading, deltaTime);
            else
                UpdateOrientation(visual, GroundVector(grid, ant.X, ant.Y), heading, deltaTime);
            visual.Transform.rotation = Quaternion.Euler(0f, 0f, visual.Angle);
            visual.Body.flipX = visual.FlipX;

            visual.Walked += (cell - visual.LastPosition).magnitude;
            visual.LastPosition = cell;
            int frame = ant.State == AntState.Digging
                ? (int)(time * _digFramesPerSecond)
                : (int)(visual.Walked * _framesPerCell);
            Sprite[] frames = topView ? _topFrames : _walkFrames;
            visual.Body.sprite = frames[frame % frames.Length];

            bool carrying = ant.State == AntState.Carrying;
            if (visual.Carried.gameObject.activeSelf != carrying)
                visual.Carried.gameObject.SetActive(carrying);
            if (carrying)
            {
                visual.Carried.color = CellMaterials.ShadedColor(ant.Carried, ant.CarriedShade);
                Vector2 offset = topView ? _carriedOffsetTop : _carriedOffset;
                visual.Carried.transform.localPosition = new Vector3(visual.FlipX ? -offset.x : offset.x, offset.y, 0f);
            }
        }

        /// <summary>
        /// Patas sempre voltadas para a superfície: o "baixo" do sprite aponta para a soma dos vizinhos sólidos
        /// (piso, parede, teto ou quina, em degraus de 45°) e o espelhamento horizontal segue o sentido do passo
        /// ao longo dela. Sem um lado claro (túnel de 1 célula, queda), alinha o corpo ao passo.
        /// </summary>
        void UpdateOrientation(AntVisual visual, Vector2 ground, Vector2 heading, float deltaTime)
        {
            float target;
            if (ground.sqrMagnitude >= 0.5f)
            {
                target = Snap45(Mathf.Atan2(ground.y, ground.x) * Mathf.Rad2Deg) + 90f;
                float radians = target * Mathf.Deg2Rad;
                float along = Vector2.Dot(heading, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)));
                // Zona morta: passo perpendicular à superfície (ex.: cavando o piso) mantém o lado atual.
                if (along > 0.1f)
                    visual.FlipX = false;
                else if (along < -0.1f)
                    visual.FlipX = true;
            }
            else if (heading.sqrMagnitude > 0.01f)
            {
                target = Snap45(Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
                // Indo para a esquerda: espelha em vez de ficar de cabeça para baixo.
                visual.FlipX = Mathf.Cos(target * Mathf.Deg2Rad) < -0.01f;
                if (visual.FlipX)
                    target -= 180f;
            }
            else
            {
                return;
            }

            visual.Angle = Mathf.Abs(Mathf.DeltaAngle(visual.Angle, target)) > 135f
                ? target
                : Mathf.MoveTowardsAngle(visual.Angle, target, _turnSpeed * deltaTime);
        }

        /// <summary>Vista de cima: o corpo aponta para o passo em qualquer direção (degraus de 45°), sem espelhar.</summary>
        void UpdateTopOrientation(AntVisual visual, Vector2 heading, float deltaTime)
        {
            if (visual.FlipX)
            {
                // Vindo da vista de lado espelhada: converte para o ângulo equivalente sem espelho.
                visual.Angle += 180f;
                visual.FlipX = false;
            }
            if (heading.sqrMagnitude <= 0.01f)
                return;
            float target = Snap45(Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
            visual.Angle = Mathf.MoveTowardsAngle(visual.Angle, target, _turnSpeed * deltaTime);
        }

        static Vector2 GroundVector(PixelGrid grid, int x, int y)
        {
            Vector2 ground = Vector2.zero;
            for (int k = 0; k < 8; k++)
            {
                int dx = AntNavigation.Dx[k];
                int dy = AntNavigation.Dy[k];
                if (grid.IsSolid(x + dx, y + dy))
                    ground += new Vector2(dx, dy);
            }
            return ground;
        }

        static float Snap45(float degrees) => Mathf.Round(degrees / 45f) * 45f;

        AntVisual CreateVisual(int index)
        {
            var root = new GameObject($"Ant_{index:000}");
            root.transform.SetParent(transform, false);
            var body = root.AddComponent<SpriteRenderer>();
            body.sortingOrder = _sortingOrder;

            var carriedGo = new GameObject("Carried");
            carriedGo.transform.SetParent(root.transform, false);
            carriedGo.transform.localScale = new Vector3(_carriedSize, _carriedSize, 1f);
            var carried = carriedGo.AddComponent<SpriteRenderer>();
            carried.sprite = _pixelSprite;
            carried.sortingOrder = _sortingOrder + 1;
            carriedGo.SetActive(false);

            return new AntVisual { GameObject = root, Transform = root.transform, Body = body, Carried = carried };
        }

        void EnsurePixelSprite()
        {
            if (_pixelSprite != null)
                return;
            _pixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "AntCarriedPixel", filterMode = FilterMode.Point };
            _pixelTexture.SetPixel(0, 0, Color.white);
            _pixelTexture.Apply(false);
            _pixelSprite = Sprite.Create(_pixelTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            _pixelSprite.name = "AntCarriedPixel";
        }

        void OnDestroy()
        {
            if (_pixelSprite != null)
                Destroy(_pixelSprite);
            if (_pixelTexture != null)
                Destroy(_pixelTexture);
        }
    }
}
