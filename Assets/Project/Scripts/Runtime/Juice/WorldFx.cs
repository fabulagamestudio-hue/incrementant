using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Juice
{
    /// <summary>
    /// Partículas no mundo, uma por fato da simulação (<see cref="FxId"/>). Um <see cref="ParticleSystem"/> pré-criado por
    /// tipo (sem emissão própria, sem alocação por disparo), quadradinhos de pixel na cor do material. Só emite o que está
    /// à vista da câmera, com cadência mínima por tipo (<see cref="FxLimiter"/>) e quantidade escalada por <see cref="Vfx"/>
    /// (Reduzido = metade, Desligado = nada). Criado por <c>AntFarmGame</c> na partida; o fundo do menu não o usa.
    /// </summary>
    public sealed class WorldFx : MonoBehaviour
    {
        struct Recipe
        {
            public int Count;          // partículas por disparo no Completo
            public float MinInterval;  // segundos reais entre disparos do tipo
            public float Size;         // lado do quadradinho, em células
            public float Speed;
            public float Lifetime;
            public float Gravity;
            public Color32 Tint;       // cor usada quando o evento não traz material
        }

        // Ordem = FxId. Cores de papel vêm de Juice/receitas: vida = verde, dano = vermelho-terra, poeira = material.
        // Tamanho = 2 células (o dobro do pixel que originou o efeito); no sorteio cada partícula fica entre 75% e 100%.
        const float Big = 2f;

        static readonly Recipe[] Recipes =
        {
            new Recipe { Count = 5, MinInterval = 0.04f, Size = Big, Speed = 2.6f, Lifetime = 0.65f, Gravity = 1.4f, Tint = new Color32(118, 82, 52, 255) },
            new Recipe { Count = 6, MinInterval = 0.05f, Size = Big, Speed = 2.0f, Lifetime = 0.7f, Gravity = 1.8f, Tint = new Color32(118, 82, 52, 255) },
            new Recipe { Count = 7, MinInterval = 0.08f, Size = Big, Speed = 2.2f, Lifetime = 0.65f, Gravity = 0.5f, Tint = new Color32(255, 243, 214, 255) },
            new Recipe { Count = 12, MinInterval = 0.10f, Size = Big, Speed = 3.2f, Lifetime = 0.85f, Gravity = 0.4f, Tint = new Color32(155, 214, 107, 255) },
            new Recipe { Count = 5, MinInterval = 0.05f, Size = Big, Speed = 2.8f, Lifetime = 0.5f, Gravity = 1.2f, Tint = new Color32(200, 72, 60, 255) },
            new Recipe { Count = 14, MinInterval = 0.10f, Size = Big, Speed = 3.4f, Lifetime = 0.85f, Gravity = 1.4f, Tint = new Color32(200, 72, 60, 255) },
        };

        const int MaxParticlesPerSystem = 200;
        const int SortingOrder = 12; // acima das formigas (10) e dos cupins (12 empata: partículas leves), abaixo do HUD (overlay)
        const float ViewMargin = 2f;

        ParticleSystem[] _systems;
        Material _material;
        FxLimiter _limiter;
        Camera _camera;
        Transform _origin;

        /// <param name="camera">Câmera do jogo: só emite o que ela enxerga.</param>
        /// <param name="origin">Transform do terreno: a célula (0,0) fica na posição dele.</param>
        public static WorldFx Create(Transform parent, Camera camera, Transform origin)
        {
            var go = new GameObject("WorldFx");
            go.transform.SetParent(parent, false);
            WorldFx fx = go.AddComponent<WorldFx>();
            fx._camera = camera;
            fx._origin = origin;
            fx.Build();
            return fx;
        }

        void Build()
        {
            var intervals = new float[Recipes.Length];
            _systems = new ParticleSystem[Recipes.Length];
            _material = new Material(Shader.Find("Sprites/Default")) { name = "WorldFx" };
            for (int i = 0; i < Recipes.Length; i++)
            {
                intervals[i] = Recipes[i].MinInterval;
                _systems[i] = BuildSystem(((FxId)i).ToString(), Recipes[i], _material);
            }
            _limiter = new FxLimiter(intervals);
        }

        // O material é criado em código: sem isto, cada partida deixaria uma cópia órfã na memória.
        void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }

        ParticleSystem BuildSystem(string systemName, Recipe r, Material material)
        {
            var go = new GameObject(systemName);
            go.transform.SetParent(transform, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = MaxParticlesPerSystem;
            main.startLifetime = new ParticleSystem.MinMaxCurve(r.Lifetime * 0.7f, r.Lifetime);
            main.startSize = new ParticleSystem.MinMaxCurve(r.Size * 0.75f, r.Size);
            main.gravityModifier = r.Gravity;
            main.useUnscaledTime = false;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;

            // Some devagar no fim (alfa), sem encolher: o quadradinho continua pixel.
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = SortingOrder;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        /// <summary>Dispara o efeito na célula dada. <paramref name="material"/> <c>Empty</c> usa a cor padrão do tipo.</summary>
        public void Play(FxId id, Vector2Int cell, CellMaterial material)
        {
            // Só os fatos com partículas têm receita; os demais (números flutuantes) são da UI.
            if (_systems == null || (int)id >= Recipes.Length || !Vfx.Motion || _origin == null || _camera == null)
                return;
            Vector3 world = _origin.position + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
            if (!InView(world))
                return;
            Recipe r = Recipes[(int)id];
            int count = Vfx.Scale(r.Count);
            if (count == 0 || !_limiter.TryUse(id, Time.unscaledTime))
                return;

            Color32 tint = material == CellMaterial.Empty ? r.Tint : CellMaterials.BaseColor(material);
            ParticleSystem ps = _systems[(int)id];
            var emit = new ParticleSystem.EmitParams { position = world, startColor = tint };
            for (int i = 0; i < count; i++)
            {
                // Espirra para cima e para os lados; a gravidade do tipo traz de volta.
                emit.velocity = new Vector3(Random.Range(-1f, 1f) * r.Speed, Random.Range(0.3f, 1f) * r.Speed, 0f);
                ps.Emit(emit, 1);
            }
        }

        bool InView(Vector3 world)
        {
            float half = _camera.orthographicSize;
            Vector3 c = _camera.transform.position;
            float halfW = half * _camera.aspect;
            return Mathf.Abs(world.x - c.x) <= halfW + ViewMargin && Mathf.Abs(world.y - c.y) <= half + ViewMargin;
        }
    }
}
