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
        [Tooltip("Posição da cria carregada (arte do BroodView, 3x2 células) em relação ao pivô, formiga virada para a direita.")]
        [SerializeField] Vector2 _carriedBroodOffset = new Vector2(2.4f, 0.5f);
        [Tooltip("Cor do nutriente que a formiga leva à rainha (a mesma do nutriente guardado no AntFarmView).")]
        [SerializeField] Color32 _nutrientColor = new Color32(232, 214, 92, 255);
        [Tooltip("Cor do ovo que a formiga leva ao berçário (a do ovo desenhado no BroodView).")]
        [SerializeField] Color32 _eggColor = new Color32(240, 222, 190, 255);
        [Tooltip("Tamanho da rainha em relação às formigas (mesmos quadros, escalados).")]
        [SerializeField, Min(1f)] float _queenScale = 2f;

        [Header("Barra do próximo ovo")]
        [Tooltip("Altura da barra sobre o centro da rainha (células).")]
        [SerializeField, Min(0f)] float _layBarHeight = 3f;
        [SerializeField, Min(1f)] float _layBarWidth = 8f;
        [SerializeField, Min(0f)] float _layBarBorder = 0.25f;
        [SerializeField] Color _layBarBackColor = new Color(0.1f, 0.07f, 0.05f, 0.85f);
        [SerializeField] Color _layBarFillColor = new Color(0.94f, 0.87f, 0.75f, 1f);
        [Tooltip("Cor do preenchimento quando a rainha não pode pôr (sem recursos, vaga ou pausada).")]
        [SerializeField] Color _layBarBlockedColor = new Color(0.55f, 0.52f, 0.5f, 1f);

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
            /// <summary>Escala do sprite (rainha &gt; 1); a base é levantada para as patas continuarem na superfície.</summary>
            public float Scale = 1f;
        }

        readonly List<AntVisual> _visuals = new List<AntVisual>();
        AntVisual _queenVisual;
        SpriteRenderer _layBarBack;
        SpriteRenderer _layBarFill;
        Texture2D _pixelTexture;
        Sprite _pixelSprite;

        BroodCycle _brood;
        BroodView _broodView;

        /// <summary>Liga o ciclo de criação e a arte das crias: a formiga que carrega uma cria a mostra na boca com essa arte.</summary>
        public void SetBrood(BroodCycle brood, BroodView broodView)
        {
            _brood = brood;
            _broodView = broodView;
        }

        public bool HasFrames => _walkFrames != null && _walkFrames.Length > 0 && _walkFrames[0] != null;
        bool HasTopFrames => _topFrames != null && _topFrames.Length > 0 && _topFrames[0] != null;

        /// <param name="simulationTime">Tempo simulado atual, incluindo a fração ainda não processada do passo.</param>
        /// <param name="queen">Rainha, desenhada maior (<c>_queenScale</c>) e atrás das operárias. Null = sem rainha.</param>
        public void Render(PixelGrid grid, IReadOnlyList<Ant> ants, Vector3 origin, float simulationTime, float deltaTime,
                           Ant queen = null, float layProgress = -1f, bool laying = true)
        {
            EnsurePixelSprite();
            if (queen != null)
            {
                if (_queenVisual == null)
                    _queenVisual = CreateVisual("Queen", _queenScale, _sortingOrder - 2);
                UpdateVisual(_queenVisual, queen, grid, origin, simulationTime, deltaTime);
                UpdateLayBar(layProgress, laying);
            }
            else if (_queenVisual != null && _queenVisual.GameObject.activeSelf)
            {
                _queenVisual.GameObject.SetActive(false);
                _queenVisual.Initialized = false;
                UpdateLayBar(-1f, false);
            }
            while (_visuals.Count < ants.Count)
                _visuals.Add(CreateVisual($"Ant_{_visuals.Count:000}", 1f, _sortingOrder));

            for (int i = 0; i < ants.Count; i++)
                UpdateVisual(_visuals[i], ants[i], grid, origin, simulationTime, deltaTime);
            for (int i = ants.Count; i < _visuals.Count; i++)
            {
                if (_visuals[i].GameObject.activeSelf)
                    _visuals[i].GameObject.SetActive(false);
                _visuals[i].Initialized = false;
            }
        }

        /// <summary>
        /// Barra de progresso do próximo ovo, nivelada (não gira com a rainha) logo acima dela. Cheia e cinza = o ciclo
        /// está pronto mas as condições (alimento, vaga, modo) ainda não deixam pôr. Progresso negativo esconde a barra.
        /// </summary>
        void UpdateLayBar(float progress, bool laying)
        {
            bool show = progress >= 0f;
            if (show && _layBarBack == null)
            {
                _layBarBack = CreateBarPart("LayBarBack", _sortingOrder + 2, _layBarBackColor);
                _layBarFill = CreateBarPart("LayBarFill", _sortingOrder + 3, _layBarFillColor);
            }
            if (_layBarBack == null)
                return;
            if (_layBarBack.gameObject.activeSelf != show)
            {
                _layBarBack.gameObject.SetActive(show);
                _layBarFill.gameObject.SetActive(show);
            }
            if (!show)
                return;

            Vector3 center = _queenVisual.Transform.position + new Vector3(0f, _layBarHeight, 0f);
            float width = _layBarWidth;
            _layBarBack.transform.position = center;
            _layBarBack.transform.localScale = new Vector3(width + 2f * _layBarBorder, 1f + 2f * _layBarBorder, 1f);
            float filled = Mathf.Max(0.001f, width * Mathf.Clamp01(progress));
            _layBarFill.transform.position = center + new Vector3((filled - width) * 0.5f, 0f, 0f);
            _layBarFill.transform.localScale = new Vector3(filled, 1f, 1f);
            _layBarFill.color = laying ? _layBarFillColor : _layBarBlockedColor;
        }

        SpriteRenderer CreateBarPart(string objectName, int sortingOrder, Color color)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _pixelSprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = color;
            return renderer;
        }

        void UpdateVisual(AntVisual visual, Ant ant, PixelGrid grid, Vector3 origin, float time, float deltaTime)
        {
            if (!visual.GameObject.activeSelf)
                visual.GameObject.SetActive(true);

            float t = ant.MoveDuration > 0f ? Mathf.Clamp01((time - ant.MoveStart) / ant.MoveDuration) : 1f;
            var cell = new Vector2(Mathf.Lerp(ant.FromX, ant.X, t), Mathf.Lerp(ant.FromY, ant.Y, t));

            // Direção: o último passo, ou o pixel que está cavando.
            bool working = ant.State == AntState.Digging || ant.State == AntState.Gathering || ant.State == AntState.Placing;
            Vector2 heading = working
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
            Vector3 position = origin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
            // De lado, o pivô fica meia célula (na escala 1) acima das patas: maior, ele sobe junto, no "cima" do sprite.
            if (!topView && visual.Scale != 1f)
                position += visual.Transform.rotation * new Vector3(0f, (visual.Scale - 1f) * 0.5f, 0f);
            visual.Transform.position = position;

            visual.Walked += (cell - visual.LastPosition).magnitude;
            visual.LastPosition = cell;
            int frame = working
                ? (int)(time * _digFramesPerSecond)
                : (int)(visual.Walked * _framesPerCell);
            Sprite[] frames = topView ? _topFrames : _walkFrames;
            visual.Body.sprite = frames[frame % frames.Length];
            visual.Body.color = RoleTint(ant.Role);

            bool delivering = ant.State == AntState.Delivering;
            bool carryingEgg = ant.State == AntState.CarryingEgg || ant.State == AntState.MovingBrood;
            bool carrying = ant.CarriedCount > 0 || delivering || carryingEgg;
            if (visual.Carried.gameObject.activeSelf != carrying)
                visual.Carried.gameObject.SetActive(carrying);
            if (carrying)
            {
                // A cria vai na boca com a própria arte (ovo, larva ou pupa); sem arte, cai no pixel colorido.
                Sprite broodArt = null;
                if (carryingEgg && _broodView != null && _broodView.HasFrames && _brood != null
                    && _brood.TryGet(ant.EggId, out BroodRecord carriedBrood))
                    broodArt = _broodView.FrameOf(carriedBrood);
                visual.Carried.sprite = broodArt != null ? broodArt : _pixelSprite;
                visual.Carried.flipX = broodArt != null && visual.FlipX;
                visual.Carried.transform.localScale = broodArt != null
                    ? Vector3.one
                    : new Vector3(_carriedSize, _carriedSize, 1f);
                if (broodArt != null)
                    visual.Carried.color = Color.white;
                else if (carryingEgg)
                    visual.Carried.color = _eggColor;
                else
                    visual.Carried.color = delivering ? (Color)_nutrientColor : CellMaterials.ShadedColor(ant.Carried, ant.CarriedShade);
                Vector2 offset = broodArt != null ? _carriedBroodOffset : topView ? _carriedOffsetTop : _carriedOffset;
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

        /// <summary>Matiz de cada papel sobre o sprite único (a silhueta própria de cada um ainda não existe).</summary>
        public static Color RoleTint(AntRole role)
        {
            switch (role)
            {
                case AntRole.Gardener: return new Color(0.6f, 1f, 0.55f);
                case AntRole.Carrier: return new Color(0.6f, 0.8f, 1f);
                case AntRole.Cutter: return new Color(1f, 0.65f, 0.45f);
                case AntRole.Digger: return new Color(1f, 0.92f, 0.4f);
                default: return Color.white;
            }
        }

        static float Snap45(float degrees) => Mathf.Round(degrees / 45f) * 45f;

        AntVisual CreateVisual(string objectName, float scale, int sortingOrder)
        {
            var root = new GameObject(objectName);
            root.transform.SetParent(transform, false);
            root.transform.localScale = new Vector3(scale, scale, 1f);
            var body = root.AddComponent<SpriteRenderer>();
            body.sortingOrder = sortingOrder;

            var carriedGo = new GameObject("Carried");
            carriedGo.transform.SetParent(root.transform, false);
            carriedGo.transform.localScale = new Vector3(_carriedSize, _carriedSize, 1f);
            var carried = carriedGo.AddComponent<SpriteRenderer>();
            carried.sprite = _pixelSprite;
            carried.sortingOrder = sortingOrder + 1;
            carriedGo.SetActive(false);

            return new AntVisual { GameObject = root, Transform = root.transform, Body = body, Carried = carried, Scale = scale };
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
