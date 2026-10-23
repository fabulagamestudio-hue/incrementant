using Fabula.Incrementant.World;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Cenário da superfície (céu, grama, trevos, flor, cogumelos, galhos, folhas secas, pedrinhas) na escala das formigas,
    /// montado por <see cref="SurfaceSceneryPainter"/> a cada mapa novo com os PNGs de <see cref="SceneryArtSet"/>. SpriteRenderers filhos, criados em runtime:
    /// céu e duas faixas de grama distante com paralaxe (<see cref="UpdateParallax"/>), perto atrás do terreno
    /// (o céu do terreno fica transparente) e frente sobre as formigas. Não entra na grade como material; a área da
    /// camada de perto vira fundo caminhável (<see cref="PixelGrid.IsBackdrop"/>).
    /// Também desenha as plantas colhíveis da grade com a mesma arte (<see cref="PlantSprites"/>, <see cref="UpdatePlants"/>).
    /// </summary>
    public sealed class SurfaceSceneryView : MonoBehaviour
    {
        [Tooltip("PNGs de cada elemento (grama, cogumelo, pedra...). Sem ele, ou num elemento vazio, usa a arte procedural.")]
        [SerializeField] SceneryArtSet _art;
        [SerializeField] SurfaceScenerySettings _settings = new SurfaceScenerySettings();
        [Tooltip("Material com o shader Incrementant/SceneryCells (etapa 10): desenha cada célula da arte em 16x16, com contorno, realce e sombra. Usado nas camadas de perto e da frente e nas plantas colhíveis. Vazio: o pixel da arte chapado, como antes.")]
        [SerializeField] Material _cellMaterial;
        [SerializeField] Color32 _skyTop = new Color32(120, 170, 215, 255);
        [SerializeField] Color32 _skyBottom = new Color32(190, 220, 235, 255);
        [Tooltip("Ordem de desenho: terreno = 0, formigas = 10.")]
        [SerializeField] int _skySortingOrder = -30;
        [SerializeField] int _farSortingOrder = -20;
        [SerializeField] int _midSortingOrder = -15;
        [SerializeField] int _nearSortingOrder = -10;
        [SerializeField] int _frontSortingOrder = 20;
        [Header("Plantas colhíveis")]
        [Tooltip("Ordem das plantas colhíveis: na frente do cenário perto e atrás do terreno (a parte ainda não crescida fica enterrada).")]
        [SerializeField] int _plantSortingOrder = -5;
        [Tooltip("Cor puxada nos pedaços da planta marcados para colheita.")]
        [SerializeField] Color32 _plantMarkTint = new Color32(255, 214, 90, 255);
        [SerializeField, Range(0f, 1f)] float _plantMarkStrength = 0.5f;
        [Header("Paralaxe")]
        [Tooltip("Quanto a grama distante acompanha a câmera (0 = presa ao mapa, 1 = presa à tela).")]
        [SerializeField, Range(0f, 1f)] float _farParallax = 0.75f;
        [Tooltip("Quanto a grama média acompanha a câmera (0 = presa ao mapa, 1 = presa à tela).")]
        [SerializeField, Range(0f, 1f)] float _midParallax = 0.5f;
        [Header("Kit do graveto (experimento 8x8)")]
        [Tooltip("Kit de peças a 8 texels por célula: os gravetos do cenário passam a ser montados por ele (variantes do elemento Graveto), no lugar dos PNGs de Art/Scenery/Twigs. Vazio = os PNGs.")]
        [FormerlySerializedAs("_twigKitPilot")]
        [SerializeField] TwigKit _twigKit;

        sealed class LayerView
        {
            public SpriteRenderer Renderer;
            public Texture2D Texture;
            public Sprite Sprite;
            public SceneryLayer Layer;
        }

        LayerView _sky;
        LayerView _far;
        LayerView _mid;
        LayerView _near;
        LayerView _front;
        SceneryStampLibrary _library;
        PlantSprites _plants;
        Vector3 _mapOrigin;
        Vector2 _parallaxAnchor;
        readonly System.Collections.Generic.List<GameObject> _detailedProps = new();
        readonly System.Collections.Generic.List<Object> _twigKitAssets = new();

        /// <summary>
        /// Pinta o cenário para o mapa atual. <paramref name="mapOrigin"/> é a posição do canto inferior esquerdo do mapa;
        /// <paramref name="parallaxAnchor"/> é a posição da câmera em que as faixas distantes ficam no horizonte.
        /// </summary>
        /// <param name="isLakeColumn">Colunas com laguinho (opcional): o cenário perto/frente não nasce nelas.</param>
        /// <param name="skyAbove">Células de céu acima do topo do mapa (até onde a câmera sobe).</param>
        public void Build(PixelGrid grid, TerrainLayout layout, int seed, Vector3 mapOrigin, int skyBaseY, Vector2 parallaxAnchor,
                          System.Func<int, bool> isLakeColumn = null, int skyAbove = SurfaceSceneryPainter.DefaultSkyAbove)
        {
            _mapOrigin = mapOrigin;
            _parallaxAnchor = parallaxAnchor;
            // Lido uma vez por sessão de Play: os PNGs não mudam enquanto o jogo roda.
            EnsureLibrary();
            _plants?.Clear();
            SceneryLayers layers = SurfaceSceneryPainter.Paint(grid, layout, _settings, seed, _skyTop, _skyBottom, skyBaseY, _library, isLakeColumn, skyAbove);
            // A arte de perto vira fundo onde a formiga anda (navegação); o resto continua só visual.
            SurfaceSceneryPainter.ApplyBackdrop(grid, layers);
            Apply(ref _sky, "ScenerySky", _skySortingOrder, layers.Sky, SpriteDrawMode.Simple);
            Apply(ref _far, "SceneryFar", _farSortingOrder, layers.Far, SpriteDrawMode.Tiled);
            Apply(ref _mid, "SceneryMid", _midSortingOrder, layers.Mid, SpriteDrawMode.Tiled);
            Apply(ref _near, "SceneryNear", _nearSortingOrder, layers.Near, SpriteDrawMode.Simple, _cellMaterial);
            Apply(ref _front, "SceneryFront", _frontSortingOrder, layers.Front, SpriteDrawMode.Simple, _cellMaterial);
            _near.Renderer.transform.position = mapOrigin + new Vector3(0f, layers.Near.OriginY, 0f);
            _front.Renderer.transform.position = mapOrigin + new Vector3(0f, layers.Front.OriginY, 0f);
            ClearDetailedProps();
            DrawDetailedProps(layers.Near, _nearSortingOrder + 1);
            DrawDetailedProps(layers.Front, _frontSortingOrder + 1);
        }

        void ClearDetailedProps()
        {
            foreach (GameObject prop in _detailedProps)
            {
                prop.SetActive(false);
                Destroy(prop);
            }
            _detailedProps.Clear();
        }

        void DrawDetailedProps(SceneryLayer layer, int sortingOrder)
        {
            foreach (var placed in layer.SpriteStamps)
            {
                Sprite sprite = placed.Stamp.VisualSprite;
                var prop = new GameObject(sprite.name);
                prop.transform.SetParent(transform, false);
                float ppu = sprite.pixelsPerUnit;
                float x = placed.Left / (float)layer.PixelsPerCell + sprite.pivot.x / ppu;
                float y = layer.OriginY + placed.Bottom / (float)layer.PixelsPerCell + sprite.pivot.y / ppu;
                // A máscara usa outra grade; alinhar o visual ao pixel das formigas.
                prop.transform.position = _mapOrigin + new Vector3(
                    Mathf.Round(x * ppu) / ppu, Mathf.Round(y * ppu) / ppu, 0f);
                var renderer = prop.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = sortingOrder;
                _detailedProps.Add(prop);
            }
        }

        void EnsureLibrary()
        {
            if (_library != null || _art == null)
                return;
            _library = _art.CreateLibrary();
            if (_twigKit != null)
                UseTwigKit(_library);
        }

        /// <summary>
        /// Troca as variantes do elemento Graveto por gravetos montados pelo <see cref="TwigKit"/> (2026-10-23): cada um vira
        /// um sprite a 8 texels por célula e um carimbo detalhado (<see cref="SceneryStamp.FromSprite"/>), então o cenário o
        /// posiciona, apoia e transforma em fundo caminhável como faz com as pedras refinadas. Pivô no meio do comprimento, meia
        /// célula acima do ponto mais baixo do tronco (afunda meia célula no chão). Seeds fixas: os mesmos gravetos sempre.
        /// </summary>
        void UseTwigKit(SceneryStampLibrary library)
        {
            const int texels = CellTileArt.TexelsPerCell;
            int count = SceneryStampLibrary.VariantCount(SceneryElement.Twig);
            var stamps = new System.Collections.Generic.List<SceneryStamp>(count);
            for (int i = 0; i < count; i++)
            {
                PixelImage image = _twigKit.Build(SceneryStampLibrary.DefaultSeed + i);
                if (image == null)
                    return;
                var texture = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false)
                {
                    name = $"TwigKit_{i}", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
                };
                texture.SetPixels32(image.Pixels);
                // Continua legível: FromSprite lê os texels para a máscara de navegação.
                texture.Apply(false, false);
                var pivot = new Vector2(0.5f, (TwigKitBuilder.Margin + texels * 0.5f) / image.Height);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, image.Width, image.Height), pivot, texels, 0, SpriteMeshType.FullRect);
                sprite.name = texture.name;
                _twigKitAssets.Add(texture);
                _twigKitAssets.Add(sprite);
                stamps.Add(SceneryStamp.FromSprite(sprite));
            }
            library.Set(SceneryElement.Twig, stamps.ToArray());
        }

        PlantSprites Plants()
        {
            EnsureLibrary();
            return _plants ??= new PlantSprites(transform, _library, _plantSortingOrder, _cellMaterial);
        }

        /// <summary>
        /// Forma de cada variante de arte do tipo de planta (a silhueta do sprite na grade): passe para
        /// <see cref="VegetationSimulation"/>, para a planta da grade ter a forma do desenho.
        /// </summary>
        public PlantShape[] PlantShapes(PlantKind kind) => Plants().Shapes(kind);

        /// <summary>Acompanha a altura e as marcas das plantas colhíveis. Chamar todo frame.</summary>
        public void UpdatePlants(PixelGrid grid, VegetationSimulation vegetation) =>
            Plants().Update(grid, vegetation, _mapOrigin, _plantMarkTint, _plantMarkStrength);

        /// <summary>Posiciona céu e faixas distantes para a vista atual da câmera. Chamar todo frame, depois de mover a câmera.</summary>
        public void UpdateParallax(Camera camera)
        {
            if (_sky == null || _sky.Layer == null)
                return;

            Vector3 c = camera.transform.position;
            float halfWidth = camera.orthographicSize * camera.aspect;
            int ppc = _sky.Layer.PixelsPerCell;

            // O céu é uma coluna de 1 pixel esticada na largura da vista; o gradiente fica preso à altura do mapa.
            _sky.Renderer.transform.position = new Vector3(c.x - halfWidth - 1f, _mapOrigin.y + _sky.Layer.OriginY, _mapOrigin.z);
            _sky.Renderer.transform.localScale = new Vector3((2f * halfWidth + 2f) * ppc, 1f, 1f);

            PlaceBand(_far, _farParallax, c, halfWidth);
            PlaceBand(_mid, _midParallax, c, halfWidth);
        }

        void PlaceBand(LayerView view, float parallax, Vector3 cameraPosition, float halfWidth)
        {
            SceneryLayer layer = view.Layer;
            float ppc = layer.PixelsPerCell;
            float tile = layer.Width / ppc;
            // Onde o ladrilho começa (módulo a largura dele) com a câmera deslocada da âncora.
            float phase = _mapOrigin.x + (cameraPosition.x - _parallaxAnchor.x) * parallax;
            float left = phase + tile * Mathf.Floor((cameraPosition.x - halfWidth - phase) / tile);
            float width = tile * (Mathf.Ceil(2f * halfWidth / tile) + 1f);
            // A faixa sobe com a câmera (paralaxe), mas o campo liso abaixo dela só cobre BandFill células: passando disso
            // abriria um vão de céu entre a faixa e o chão quando a câmera sobe até o chão quase sair da vista. Daí em
            // diante a faixa para de subir (fica presa ao mundo) em vez de deixar o vão aparecer.
            float rise = Mathf.Min((cameraPosition.y - _parallaxAnchor.y) * parallax, SurfaceSceneryPainter.BandFill - 4f);
            float bottom = _mapOrigin.y + layer.OriginY + rise;

            var size = new Vector2(width, layer.Height / ppc);
            if (view.Renderer.size != size)
                view.Renderer.size = size;
            // Encaixa na grade de pixels da arte, para o Point filter não tremer.
            view.Renderer.transform.position = new Vector3(Mathf.Round(left * ppc) / ppc, Mathf.Round(bottom * ppc) / ppc, _mapOrigin.z);
        }

        /// <param name="material">Material da camada (null = o padrão de sprite).</param>
        void Apply(ref LayerView view, string layerName, int sortingOrder, SceneryLayer layer, SpriteDrawMode drawMode,
                   Material material = null)
        {
            if (view == null)
            {
                var go = new GameObject(layerName) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                view = new LayerView { Renderer = go.AddComponent<SpriteRenderer>() };
                view.Renderer.sortingOrder = sortingOrder;
                if (material != null)
                    view.Renderer.sharedMaterial = material;
            }
            Release(view);
            view.Layer = layer;
            view.Texture = new Texture2D(layer.Width, layer.Height, TextureFormat.RGBA32, false)
            {
                name = layerName,
                filterMode = FilterMode.Point,
                wrapMode = layer.WrapX ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
            };
            view.Texture.SetPixels32(layer.Pixels);
            view.Texture.Apply(false, true);
            view.Sprite = Sprite.Create(view.Texture, new Rect(0, 0, layer.Width, layer.Height), Vector2.zero,
                                        layer.PixelsPerCell, 0, SpriteMeshType.FullRect);
            view.Sprite.name = layerName;
            view.Renderer.sprite = view.Sprite;
            view.Renderer.drawMode = drawMode;
            if (drawMode == SpriteDrawMode.Tiled)
                view.Renderer.tileMode = SpriteTileMode.Continuous;
        }

        static void Release(LayerView view)
        {
            if (view.Sprite != null)
                Destroy(view.Sprite);
            if (view.Texture != null)
                Destroy(view.Texture);
            view.Sprite = null;
            view.Texture = null;
        }

        void OnDestroy()
        {
            if (_sky != null) Release(_sky);
            if (_far != null) Release(_far);
            if (_mid != null) Release(_mid);
            if (_near != null) Release(_near);
            if (_front != null) Release(_front);
            foreach (Object asset in _twigKitAssets)
                Destroy(asset);
            _twigKitAssets.Clear();
            _plants?.Dispose();
        }
    }
}
