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
        [Tooltip("Arte própria de cada papel (pastas Art/Ants/<Papel>). Papel sem entrada usa os quadros acima; a rainha usa os dela, abaixo.")]
        [SerializeField] RoleArt[] _roleArt;
        [Tooltip("Quadros de lado da rainha (Art/Ants/Rainha, 48x32 a 16 px por unidade com Queen Scale 2 = pixel do tamanho do texel do terreno, experimento 8x8). Vazio = os quadros de cima, ampliados.")]
        [SerializeField] Sprite[] _queenWalkFrames;
        [Tooltip("Quadros vista de cima da rainha. Vazio = os de cima, ampliados.")]
        [SerializeField] Sprite[] _queenTopFrames;
        [Header("Cupins (Art/Ants/InimigaAlada)")]
        [Tooltip("Chegada em voo (4 quadros).")]
        [SerializeField] Sprite[] _termiteFly;
        [Tooltip("Pouso, as asas caem (4 quadros, tocados uma vez).")]
        [SerializeField] Sprite[] _termiteLand;
        [SerializeField] Sprite[] _termiteWalk;
        [SerializeField] Sprite[] _termiteTop;
        [Tooltip("Quadros por segundo do bater de asas.")]
        [SerializeField, Min(1f)] float _termiteFlyFps = 14f;
        [Tooltip("Cor do cupim que acabou de levar uma mordida (pisca por um instante).")]
        [SerializeField] Color _termiteHitColor = new Color(1f, 0.45f, 0.4f, 1f);
        [Tooltip("Cor do corpo do cupim morto, à espera de ser desmembrado.")]
        [SerializeField] Color _termiteCorpseColor = new Color(0.55f, 0.5f, 0.5f, 1f);
        [Tooltip("Pulgões da fazenda: caminhada de lado e vista de cima (Art/Aphids), virados para a direita. Sem eles, os pulgões existem mas não aparecem.")]
        [SerializeField] Sprite[] _aphidWalk;
        [SerializeField] Sprite[] _aphidTop;
        [Tooltip("Tinta do pulgão preto (mordido por um cupim, do lado deles).")]
        [SerializeField] Color _blackAphidColor = new Color(0.16f, 0.14f, 0.18f, 1f);
        [Header("Cuspe da cuspideira")]
        [SerializeField] Color _spitColor = new Color(0.72f, 0.86f, 0.25f, 1f);
        [Tooltip("Lado do cuspe, em células.")]
        [SerializeField, Range(0.1f, 1f)] float _spitSize = 0.45f;
        [Tooltip("Altura máxima do arco do cuspe, em células por célula de distância.")]
        [SerializeField, Range(0f, 0.5f)] float _spitArc = 0.15f;
        [Tooltip("Papel sem arte própria (pasta em Art/Ants) usa os quadros da operária tingidos com a cor do papel nesta força.")]
        [SerializeField, Range(0f, 1f)] float _placeholderTint = 0.6f;
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
        [Tooltip("Tamanho do pixel carregado, em células (1 = o mesmo bloco que estava no terreno).")]
        [SerializeField, Range(0.2f, 1.5f)] float _carriedSize = 1f;
        [Tooltip("Desenha a terra/planta carregada como o bloco 16x16 da célula de onde saiu (textura do material, CellTileArt). Desligado: um quadrado da cor do pixel.")]
        [SerializeField] bool _carriedAsBlock = true;
        [Tooltip("Posição da cria carregada (arte do BroodView, 3x2 células) em relação ao pivô, formiga virada para a direita.")]
        [SerializeField] Vector2 _carriedBroodOffset = new Vector2(2.4f, 0.5f);
        [Tooltip("Cor do nutriente que a formiga leva à rainha (a mesma do nutriente guardado no AntFarmView).")]
        [SerializeField] Color32 _nutrientColor = new Color32(232, 214, 92, 255);
        [Tooltip("Cor do bloco de água que a formiga leva para regar um canteiro.")]
        [SerializeField] Color32 _waterColor = new Color32(84, 150, 220, 235);
        [Tooltip("Cor do ovo que a formiga leva ao berçário (a do ovo desenhado no BroodView).")]
        [SerializeField] Color32 _eggColor = new Color32(240, 222, 190, 255);
        [Tooltip("Nutriente na boca da formiga (Art/Ants/Item_Nutrient, 1 célula, na paleta). Vazio = quadrado na cor acima.")]
        [SerializeField] Sprite _nutrientSprite;
        [Tooltip("Gota de água na boca da formiga (Art/Ants/Item_Water). Vazio = quadrado na cor acima.")]
        [SerializeField] Sprite _waterSprite;
        [Tooltip("Minério na boca da formiga: cobre, ferro e ouro, nesta ordem (Art/Ants/Item_*Ore, o mesmo sprite do minério solto no AntFarmView). Vazio = o bloco do material.")]
        [SerializeField] Sprite[] _oreSprites = new Sprite[0];
        [Tooltip("Ferramenta, arma e armadura na boca da formiga a caminho do armazém (Art/Ants/Item_<Metal><Tipo>): ferramenta, arma, armadura, cada uma em cobre, ferro e ouro (9). Vazio = o bloco do material.")]
        [SerializeField] Sprite[] _equipItemSprites = new Sprite[0];
        [Tooltip("Camadas do equipamento vestido, desenhadas sobre a formiga (Art/Ants/Equipamento), de lado. Ordem: ferramenta, arma, armadura, cada uma em cobre, ferro e ouro (9). Vazio = não mostra.")]
        [SerializeField] Sprite[] _equipWalk = new Sprite[0];
        [Tooltip("As mesmas 9 camadas, vista de cima.")]
        [SerializeField] Sprite[] _equipTop = new Sprite[0];
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

        /// <summary>Quadros de um papel: caminhada de lado e vista de cima, virados para a direita.</summary>
        [System.Serializable]
        public sealed class RoleArt
        {
            public AntRole Role;
            public Sprite[] Walk;
            public Sprite[] Top;
        }

        sealed class AntVisual
        {
            public GameObject GameObject;
            public Transform Transform;
            public SpriteRenderer Body;
            public SpriteRenderer Carried;
            /// <summary>Camadas do equipamento (ferramenta, arma, armadura), criadas na primeira vez que a formiga veste algo.</summary>
            public SpriteRenderer[] Equip;
            public float Angle;
            public bool FlipX;
            public float Walked;
            public Vector2 LastPosition;
            public bool Initialized;
            /// <summary>Escala do sprite (rainha &gt; 1); a base é levantada para as patas continuarem na superfície.</summary>
            public float Scale = 1f;
            /// <summary>A rainha tem arte própria e não segue o papel da simulação.</summary>
            public bool IsQueen;
        }

        readonly List<AntVisual> _visuals = new List<AntVisual>();
        readonly List<AntVisual> _termiteVisuals = new List<AntVisual>();
        readonly List<AntVisual> _aphidVisuals = new List<AntVisual>();
        readonly List<SpriteRenderer> _spitRenderers = new List<SpriteRenderer>();
        const float HitFlashSeconds = 0.15f;
        AntVisual _queenVisual;
        SpriteRenderer _layBarBack;
        SpriteRenderer _layBarFill;
        Texture2D _pixelTexture;
        Sprite _pixelSprite;
        // Blocos carregados já montados: material, tom (16 faixas) e posição da célula no ciclo de 8x8 do atlas.
        readonly Dictionary<int, Sprite> _blockSprites = new Dictionary<int, Sprite>();
        TerrainTileAtlas _tileAtlas;
        float _tileToneStrength = 1f;

        BroodCycle _brood;
        BroodView _broodView;

        /// <summary>
        /// Atlas do terreno (<see cref="AntFarmView.TileAtlas"/>) e força do tom: o bloco carregado mostra a mesma arte do
        /// terreno. Null: bloco liso na cor do material.
        /// </summary>
        public void SetTileAtlas(TerrainTileAtlas atlas, float toneStrength)
        {
            if (atlas == _tileAtlas && Mathf.Approximately(toneStrength, _tileToneStrength))
                return;
            _tileAtlas = atlas;
            _tileToneStrength = toneStrength;
            ClearBlockSprites();
        }

        /// <summary>Liga o ciclo de criação e a arte das crias: a formiga que carrega uma cria a mostra na boca com essa arte.</summary>
        public void SetBrood(BroodCycle brood, BroodView broodView)
        {
            _brood = brood;
            _broodView = broodView;
        }

        public bool HasFrames => _walkFrames != null && _walkFrames.Length > 0 && _walkFrames[0] != null;

        static bool HasAll(Sprite[] frames) => frames != null && frames.Length > 0 && frames[0] != null;

        /// <summary>
        /// Cupins: descendo do céu com os quadros de voo, perdendo as asas no pouso, andando (como as formigas, com os quadros
        /// sem asas) e, mortos, de barriga para cima e apagados. Sem os quadros na cena, não desenha nada.
        /// </summary>
        public void RenderTermites(PixelGrid grid, IReadOnlyList<Termite> termites, Vector3 origin, float simulationTime, float deltaTime)
        {
            bool art = HasAll(_termiteWalk) && HasAll(_termiteFly) && HasAll(_termiteLand);
            int count = art ? termites.Count : 0;
            while (_termiteVisuals.Count < count)
                _termiteVisuals.Add(CreateVisual($"Termite_{_termiteVisuals.Count:000}", 1f, _sortingOrder + 2));
            for (int i = 0; i < count; i++)
                UpdateTermiteVisual(_termiteVisuals[i], termites[i], grid, origin, simulationTime, deltaTime);
            for (int i = count; i < _termiteVisuals.Count; i++)
            {
                if (_termiteVisuals[i].GameObject.activeSelf)
                    _termiteVisuals[i].GameObject.SetActive(false);
                _termiteVisuals[i].Initialized = false;
            }
        }

        /// <summary>
        /// Pulgões da fazenda, com os quadros de Art/Aphids (como as formigas: de lado no chão, de cima no fundo do túnel). Sem
        /// os quadros na cena, não desenha nada. O pulgão preto é desenhado com os cupins (<see cref="RenderTermites"/>).
        /// </summary>
        public void RenderAphids(PixelGrid grid, IReadOnlyList<Aphid> aphids, Vector3 origin, float simulationTime, float deltaTime)
        {
            int count = HasAll(_aphidWalk) ? aphids.Count : 0;
            while (_aphidVisuals.Count < count)
                _aphidVisuals.Add(CreateVisual($"Aphid_{_aphidVisuals.Count:000}", 1f, _sortingOrder - 1));
            Sprite[] top = HasAll(_aphidTop) ? _aphidTop : _aphidWalk;
            for (int i = 0; i < count; i++)
            {
                UpdateVisual(_aphidVisuals[i], aphids[i].Body, grid, origin, simulationTime, deltaTime, _aphidWalk, top);
                _aphidVisuals[i].Body.color = Color.white;
            }
            for (int i = count; i < _aphidVisuals.Count; i++)
            {
                if (_aphidVisuals[i].GameObject.activeSelf)
                    _aphidVisuals[i].GameObject.SetActive(false);
                _aphidVisuals[i].Initialized = false;
            }
        }

        /// <summary>Cuspes das cuspideiras no ar: um quadrado da cor do ácido em arco, da célula de onde saiu até o cupim.</summary>
        public void RenderSpits(IReadOnlyList<SpitShot> spits, Vector3 origin)
        {
            EnsurePixelSprite();
            while (_spitRenderers.Count < spits.Count)
            {
                var go = new GameObject($"Spit_{_spitRenderers.Count:000}");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = _pixelSprite;
                renderer.sortingOrder = _sortingOrder + 3;
                _spitRenderers.Add(renderer);
            }
            for (int i = 0; i < spits.Count; i++)
            {
                SpitShot spit = spits[i];
                SpriteRenderer renderer = _spitRenderers[i];
                var from = new Vector2(spit.FromX, spit.FromY);
                var to = new Vector2(spit.Target.X, spit.Target.Y);
                float t = spit.Progress;
                Vector2 cell = Vector2.Lerp(from, to, t) + Vector2.up * (4f * t * (1f - t) * _spitArc * Vector2.Distance(from, to));
                renderer.transform.position = origin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
                renderer.transform.localScale = new Vector3(_spitSize, _spitSize, 1f);
                renderer.color = _spitColor;
                if (!renderer.gameObject.activeSelf)
                    renderer.gameObject.SetActive(true);
            }
            for (int i = spits.Count; i < _spitRenderers.Count; i++)
            {
                if (_spitRenderers[i].gameObject.activeSelf)
                    _spitRenderers[i].gameObject.SetActive(false);
            }
        }

        void UpdateTermiteVisual(AntVisual visual, Termite termite, PixelGrid grid, Vector3 origin, float time, float deltaTime)
        {
            Ant body = termite.Body;
            // O pulgão preto anda com a arte dos pulgões, tingida de preto.
            bool aphid = termite.Aphid && HasAll(_aphidWalk);
            Sprite[] walk = aphid ? _aphidWalk : _termiteWalk;
            Sprite[] top = aphid ? (HasAll(_aphidTop) ? _aphidTop : _aphidWalk) : HasAll(_termiteTop) ? _termiteTop : _termiteWalk;
            switch (termite.Phase)
            {
                case TermitePhase.Raiding:
                case TermitePhase.Leaving:
                    UpdateVisual(visual, body, grid, origin, time, deltaTime, walk, top);
                    break;
                case TermitePhase.Dead:
                    UpdateVisual(visual, body, grid, origin, time, 0f, top, top);
                    visual.Body.sprite = top[0];
                    visual.Transform.rotation = Quaternion.Euler(0f, 0f, visual.Angle + 180f);
                    break;
                default:
                {
                    if (!visual.GameObject.activeSelf)
                        visual.GameObject.SetActive(true);
                    visual.Carried.gameObject.SetActive(false);
                    float progress = Mathf.Clamp01((time - termite.PhaseStart) / Mathf.Max(0.01f, termite.PhaseDuration));
                    var landing = new Vector2(body.X, body.Y);
                    Vector2 cell = landing;
                    Sprite[] frames;
                    int frame;
                    if (termite.Phase == TermitePhase.Flying)
                    {
                        // Desacelera ao chegar, como quem plana até o chão.
                        float eased = 1f - (1f - progress) * (1f - progress);
                        cell = Vector2.Lerp(new Vector2(termite.FlyFromX, termite.FlyFromY), landing, eased);
                        frames = _termiteFly;
                        frame = (int)(time * _termiteFlyFps);
                        visual.FlipX = termite.FlyFromX > landing.x;
                    }
                    else
                    {
                        frames = _termiteLand;
                        frame = Mathf.Min(frames.Length - 1, (int)(progress * frames.Length));
                        visual.FlipX = body.FacingX < 0;
                    }
                    visual.Angle = 0f;
                    visual.Initialized = false; // ao andar, começa do zero (posição e lado)
                    visual.Transform.rotation = Quaternion.identity;
                    visual.Body.flipX = visual.FlipX;
                    visual.Body.sprite = frames[frame % frames.Length];
                    visual.Transform.position = origin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
                    break;
                }
            }
            bool hit = time - termite.LastHitTime < HitFlashSeconds;
            Color alive = termite.Aphid ? _blackAphidColor : Color.white;
            visual.Body.color = !termite.Alive ? _termiteCorpseColor * alive : hit ? _termiteHitColor : alive;
        }

        /// <param name="simulationTime">Tempo simulado atual, incluindo a fração ainda não processada do passo.</param>
        /// <param name="queen">Rainha, desenhada maior (<c>_queenScale</c>) e atrás das operárias. Null = sem rainha.</param>
        public void Render(PixelGrid grid, IReadOnlyList<Ant> ants, Vector3 origin, float simulationTime, float deltaTime,
                           Ant queen = null, float layProgress = -1f, bool laying = true)
        {
            EnsurePixelSprite();
            if (queen != null)
            {
                if (_queenVisual == null)
                {
                    _queenVisual = CreateVisual("Queen", _queenScale, _sortingOrder - 2);
                    _queenVisual.IsQueen = true;
                }
                UpdateVisual(_queenVisual, queen, grid, origin, simulationTime, deltaTime,
                             HasAll(_queenWalkFrames) ? _queenWalkFrames : null, HasAll(_queenTopFrames) ? _queenTopFrames : null);
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

        /// <param name="walkOverride">Quadros de lado no lugar dos do papel (cupins); <paramref name="topOverride"/> idem, vista de cima.</param>
        void UpdateVisual(AntVisual visual, Ant ant, PixelGrid grid, Vector3 origin, float time, float deltaTime,
                          Sprite[] walkOverride = null, Sprite[] topOverride = null)
        {
            if (!visual.GameObject.activeSelf)
                visual.GameObject.SetActive(true);

            float t = ant.MoveDuration > 0f ? Mathf.Clamp01((time - ant.MoveStart) / ant.MoveDuration) : 1f;
            var cell = new Vector2(Mathf.Lerp(ant.FromX, ant.X, t), Mathf.Lerp(ant.FromY, ant.Y, t));

            // Direção: o último passo, ou o pixel que está cavando.
            // Mordendo um cupim ou desmembrando um corpo também anima como trabalho, virada para o alvo (DigX/DigY).
            bool working = ant.State == AntState.Digging || ant.State == AntState.Gathering || ant.State == AntState.Placing || ant.Biting;
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

            // No meio de um túnel ou sobre a arte do cenário, sem nada encostado, ela está no fundo: vista de cima,
            // girando livre. Dentro da obra e da terra de fora também (anda pela face do bloco, não pelo contorno).
            Sprite[] walkFrames = walkOverride ?? _walkFrames;
            Sprite[] topFrames = topOverride ?? _topFrames;
            // Papel sem arte própria na cena (pasta em Art/Ants não ligada): quadros da operária tingidos com a cor do papel.
            Color tint = Color.white;
            if (!visual.IsQueen && walkOverride == null && !ApplyRoleArt(ant.Role, ref walkFrames, ref topFrames) &&
                ant.Role != AntRole.Worker)
                tint = Color.Lerp(Color.white, RoleColor(ant.Role), _placeholderTint);
            visual.Body.color = tint;
            bool topView = topFrames != null && topFrames.Length > 0 && topFrames[0] != null
                           && (AntNavigation.IsBackground(grid, ant.X, ant.Y) && !AntNavigation.TouchesSolid(grid, ant.X, ant.Y)
                               || AntNavigation.IsOutdoorWall(grid, ant.X, ant.Y));
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
            Sprite[] frames = topView ? topFrames : walkFrames;
            visual.Body.sprite = frames[frame % frames.Length];
            if (!visual.IsQueen)
            {
                UpdateEquipLayer(visual, 0, ant.Tool, topView);
                UpdateEquipLayer(visual, 1, ant.Weapon, topView);
                UpdateEquipLayer(visual, 2, ant.Armor, topView);
            }

            bool delivering = ant.State == AntState.Delivering;
            bool carryingEgg = ant.State == AntState.CarryingEgg || ant.State == AntState.MovingBrood;
            bool carryingWater = ant.State == AntState.CarryingWater ||
                                 ant.State == AntState.FeedingAphid && ant.FarmItem == StoredItems.Water;
            bool carrying = ant.CarriedCount > 0 || delivering || carryingEgg || carryingWater;
            if (visual.Carried.gameObject.activeSelf != carrying)
                visual.Carried.gameObject.SetActive(carrying);
            if (carrying)
            {
                // A cria vai na boca com a própria arte (ovo, larva ou pupa); sem arte, cai no pixel colorido.
                Sprite broodArt = null;
                if (carryingEgg && _broodView != null && _broodView.HasFrames && _brood != null
                    && _brood.TryGet(ant.EggId, out BroodRecord carriedBrood))
                    broodArt = _broodView.FrameOf(carriedBrood);
                bool block = broodArt == null && !carryingEgg && !delivering && !carryingWater && _carriedAsBlock;
                // Nutriente e água com a arte da paleta (etapa 9); sem ela, o quadrado tingido de antes.
                Sprite item = broodArt == null && !carryingEgg ? (delivering ? _nutrientSprite : carryingWater ? _waterSprite : OreSprite(ant.Carried)) : null;
                visual.Carried.sprite = broodArt != null ? broodArt
                    : item != null ? item
                    : block ? BlockSprite(ant.Carried, ant.CarriedShade, ant.CarriedFrom >= 0 ? ant.CarriedFrom % grid.Width : ant.X,
                                          ant.CarriedFrom >= 0 ? ant.CarriedFrom / grid.Width : ant.Y)
                    : _pixelSprite;
                visual.Carried.flipX = broodArt != null && visual.FlipX;
                visual.Carried.transform.localScale = broodArt != null
                    ? Vector3.one
                    : new Vector3(_carriedSize, _carriedSize, 1f);
                if (broodArt != null || block || item != null)
                    visual.Carried.color = Color.white;
                else if (carryingEgg)
                    visual.Carried.color = _eggColor;
                else if (carryingWater)
                    visual.Carried.color = _waterColor;
                else
                    visual.Carried.color = delivering ? (Color)_nutrientColor : CellMaterials.ShadedColor(ant.Carried, ant.CarriedShade);
                Vector2 offset = broodArt != null ? _carriedBroodOffset : topView ? _carriedOffsetTop : _carriedOffset;
                visual.Carried.transform.localPosition = new Vector3(visual.FlipX ? -offset.x : offset.x, offset.y, 0f);
            }
        }

        /// <summary>
        /// Camada do equipamento vestido (REQ-071): mesmo canvas e pivô do corpo, então basta herdar a rotação do pai e o espelho.
        /// O corpo não muda entre os quadros de caminhada, por isso uma camada por vista serve para todos.
        /// </summary>
        void UpdateEquipLayer(AntVisual visual, int slot, CellMaterial item, bool topView)
        {
            Sprite sprite = null;
            Sprite[] set = topView ? _equipTop : _equipWalk;
            int tier = Equipment.IsItem(item) ? Equipment.TierOf(item) : -1;
            if (tier >= 0 && set != null)
            {
                int index = slot * Equipment.Tiers + tier;
                if (index < set.Length)
                    sprite = set[index];
            }

            SpriteRenderer layer = visual.Equip?[slot];
            if (sprite == null)
            {
                if (layer != null && layer.gameObject.activeSelf)
                    layer.gameObject.SetActive(false);
                return;
            }
            if (layer == null)
            {
                visual.Equip ??= new SpriteRenderer[3];
                var go = new GameObject("Equip" + slot);
                go.transform.SetParent(visual.Transform, false);
                layer = go.AddComponent<SpriteRenderer>();
                layer.sortingOrder = visual.Body.sortingOrder + 1;
                visual.Equip[slot] = layer;
            }
            if (!layer.gameObject.activeSelf)
                layer.gameObject.SetActive(true);
            layer.sprite = sprite;
            layer.flipX = visual.FlipX;
        }

        /// <summary>Sprite do minério ou do equipamento carregado (REQ-071), ou null se é outra coisa ou falta o sprite dele.</summary>
        Sprite OreSprite(CellMaterial carried)
        {
            if (CellMaterials.IsOre(carried))
            {
                int ore = (int)carried - (int)CellMaterial.CopperOre;
                return _oreSprites != null && ore < _oreSprites.Length ? _oreSprites[ore] : null;
            }
            EquipKind kind = Equipment.KindOf(carried);
            if (kind == EquipKind.None)
                return null;
            int index = ((int)kind - 1) * Equipment.Tiers + Equipment.TierOf(carried);
            return _equipItemSprites != null && index < _equipItemSprites.Length ? _equipItemSprites[index] : null;
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

        /// <summary>Troca os quadros padrão pelos do papel, quando ele tem arte própria completa.</summary>
        /// <summary>Troca os quadros pelos do papel; <c>false</c> = o papel não tem arte própria na cena.</summary>
        bool ApplyRoleArt(AntRole role, ref Sprite[] walk, ref Sprite[] top)
        {
            if (_roleArt == null)
                return false;
            for (int i = 0; i < _roleArt.Length; i++)
            {
                RoleArt art = _roleArt[i];
                if (art == null || art.Role != role || art.Walk == null || art.Walk.Length == 0 || art.Walk[0] == null)
                    continue;
                walk = art.Walk;
                if (art.Top != null && art.Top.Length > 0 && art.Top[0] != null)
                    top = art.Top;
                return true;
            }
            return false;
        }

        /// <summary>Cor principal da arte de cada papel (ícones do HUD), a mesma das variantes em Art/Ants/README.md.</summary>
        public static Color RoleColor(AntRole role)
        {
            switch (role)
            {
                case AntRole.Carrier: return new Color32(0x31, 0x8B, 0x39, 255);
                case AntRole.Cutter: return new Color32(0x1F, 0x9E, 0x8E, 255);
                case AntRole.Gardener: return new Color32(0xC2, 0x47, 0x7F, 255);
                case AntRole.Digger: return new Color32(0xD3, 0x63, 0x18, 255);
                case AntRole.Nurse: return new Color32(0x41, 0x9E, 0xC7, 255);
                case AntRole.Soldier: return new Color32(0x6F, 0x33, 0x9E, 255);
                case AntRole.Spitter: return new Color32(0xB3, 0xCF, 0x51, 255);
                case AntRole.Midden: return new Color32(0x7D, 0x76, 0x76, 255);
                case AntRole.Farmer: return new Color32(0x9E, 0x65, 0x34, 255);
                default: return new Color32(0x9E, 0x31, 0x1E, 255);
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
            carried.sortingOrder = sortingOrder + 2; // acima das camadas do equipamento (+1)
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

        /// <summary>
        /// O bloco 16x16 da célula (<paramref name="cellX"/>, <paramref name="cellY"/>) como estava no terreno: o texel do
        /// atlas naquela posição vezes o tom (a mesma conta do shader, ver <see cref="TerrainTileAtlas"/>), e o contorno.
        /// O atlas repete a cada 8 células e o tom é agrupado em 16 faixas, então há no máximo 16 x 64 sprites por
        /// material, montados sob demanda.
        /// </summary>
        Sprite BlockSprite(CellMaterial material, byte shade, int cellX, int cellY)
        {
            const int cells = CellTileArt.TileCells;
            int ox = cellX & (cells - 1), oy = cellY & (cells - 1);
            byte band = (byte)(shade & 0xF0 | 0x08);
            int key = (((int)material * 16 + (shade >> 4)) * cells + ox) * cells + oy;
            if (_blockSprites.TryGetValue(key, out Sprite sprite))
                return sprite;

            const int size = CellTileArt.TexelsPerCell;
            Color cell = ((Color)CellMaterials.ShadedColor(material, band, _tileAtlas != null ? _tileToneStrength : 1f)).linear;
            var tone = new Vector3(1f, 1f, 1f);
            if (_tileAtlas != null)
            {
                Color reference = ((Color)_tileAtlas.Reference((int)material)).linear;
                tone = new Vector3(cell.r / Mathf.Max(reference.r, 0.0005f), cell.g / Mathf.Max(reference.g, 0.0005f),
                                   cell.b / Mathf.Max(reference.b, 0.0005f));
            }
            var pixels = new Color32[size * size];
            for (int ty = 0; ty < size; ty++)
            for (int tx = 0; tx < size; tx++)
            {
                Color texel = _tileAtlas != null
                    ? ((Color)_tileAtlas.Texel((int)material, ox * size + tx, oy * size + ty)).linear
                    : cell;
                // Contorno de 1 texel no preto da paleta, como o da terra e o dos sprites (experimento 8x8).
                if (tx == 0 || ty == 0 || tx == size - 1 || ty == size - 1)
                {
                    pixels[ty * size + tx] = CellTileArt.OutlineColor;
                    continue;
                }
                if (_tileAtlas != null)
                    texel = new Color(texel.r * tone.x, texel.g * tone.y, texel.b * tone.z);
                pixels[ty * size + tx] = new Color(texel.r, texel.g, texel.b, 1f).gamma;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "AntCarriedBlock", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
            sprite.name = "AntCarriedBlock";
            _blockSprites[key] = sprite;
            return sprite;
        }

        void OnDestroy()
        {
            if (_pixelSprite != null)
                Destroy(_pixelSprite);
            if (_pixelTexture != null)
                Destroy(_pixelTexture);
            ClearBlockSprites();
        }

        void ClearBlockSprites()
        {
            foreach (Sprite sprite in _blockSprites.Values)
            {
                Destroy(sprite.texture);
                Destroy(sprite);
            }
            _blockSprites.Clear();
        }
    }
}
