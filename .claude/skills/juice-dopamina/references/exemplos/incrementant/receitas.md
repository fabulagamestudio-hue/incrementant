# Receitas — curvas, tempos, cores e código

> **Exemplo específico do Incrementant** (incremental 2D cozy em pixel art, análise de 2026-10-18). Serve de modelo de como aplicar a skill; os nomes e valores aqui não valem para outros jogos. A versão genérica está na pasta `references/` acima.

Padrões prontos para manter o "toque" consistente entre efeitos. Ajuste em Play Mode, mas mude aqui se um valor virar regra do jogo.

## Gramática de movimento

| Intenção | Curva | Duração típica |
|---|---|---|
| Entrar, aparecer | `OutBack` (overshoot ~1,1) ou `OutCubic` | 0,15–0,25 s |
| Sair, sumir | `InQuad` ou `InCubic`, mais curta que a entrada | 0,08–0,15 s |
| Hover | `OutQuad` | 0,08–0,12 s |
| Pressionar | `OutQuad`, muito rápida | 0,05–0,07 s |
| Soltar/retorno | `OutElastic` leve ou `OutBack` | 0,2–0,3 s |
| Pulso de contador | `OutQuad` subida + `InOutSine` volta | 0,15 s |
| Respirar (loop) | `InOutSine` | 1,5–2,5 s por ciclo |
| Queda | gravidade (`InQuad`) + quique pequeno | 0,3–0,5 s |
| Erro/recusa | oscilação amortecida (3 ciclos) | 0,18 s |

Princípios de animação para sentir "vivo": **antecipação** (recuar antes de avançar), **squash & stretch** preservando volume (x×y ≈ 1), **follow-through** (overshoot e retorno), **escalonar** múltiplos itens (cascata de 10–50 ms) e **sobrepor** efeitos (som + movimento + cor no mesmo quadro).

## Paleta de feedback (usar a do jogo; proposta)

| Papel | Cor sugerida | Uso |
|---|---|---|
| Ganho / produção | dourado quente `#E8D65C` (mesmo do nutriente `232,214,92`) | nutrientes, brilho de compra |
| Vida / nascimento | verde-claro `#9BD66B` | brotos, nascimento |
| Água | azul `#549ADC` (`84,150,220`) | respingos, bolhas |
| Dano / negado | vermelho-terra `#C8483C` (aproximar do `_termiteHitColor`) | recusa, dano |
| Neutro/poeira | cor do material do terreno | escavar, despejar |
| Destaque de UI | branco quente `#FFF3D6` | hover, brilho de borda |

Confirme com o diretor de arte antes de fixar; evite branco puro (0,0 saturação) e tons neon, que quebram o clima cozy.

## `Easing` (lógica pura, testável)

```csharp
namespace Fabula.Incrementant.Juice
{
    public static class Easing
    {
        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float InQuad(float t) => t * t;
        public static float OutCubic(float t) { float u = 1f - t; return 1f - u * u * u; }
        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
        public static float OutBack(float t, float s = 1.70158f)
        {
            float u = t - 1f;
            return 1f + u * u * ((s + 1f) * u + s);
        }
        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c) + 1f;
        }
    }
}
```

Teste: cada curva dá 0 em t=0 e 1 em t=1 (tolerância); `OutBack` passa de 1 no meio; nenhuma devolve NaN em [0,1].

## Esqueleto de `ButtonJuice` (hover/pressionar/soltar/negado)

```csharp
[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public sealed class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                                  IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] RectTransform _target;        // nulo = o próprio
    [SerializeField, Min(1f)] float _hoverScale = 1.06f;

    Selectable _selectable;
    Vector3 _rest = Vector3.one;
    float _from, _to, _t = 1f, _duration = 0.1f;   // escala escalar atual
    Easing.Fn _curve = Easing.OutQuad;

    void Awake() { _selectable = GetComponent<Selectable>(); if (_target == null) _target = (RectTransform)transform; _rest = _target.localScale; }
    void OnDisable() { _target.localScale = _rest; _t = 1f; }   // nunca deixar preso

    public void OnPointerEnter(PointerEventData e) { if (CanReact) Go(_hoverScale, 0.10f, Easing.OutQuad); }
    public void OnPointerExit(PointerEventData e)  { Go(1f, 0.12f, Easing.OutQuad); }
    public void OnPointerDown(PointerEventData e)  { if (CanReact) Go(0.94f, 0.06f, Easing.OutQuad); }
    public void OnPointerUp(PointerEventData e)    { Go(_hoverScale, 0.22f, Easing.OutBack); }

    bool CanReact => Vfx.Motion && _selectable.IsInteractable();

    void Go(float to, float seconds, Easing.Fn curve) { _from = Current; _to = to; _duration = seconds; _t = 0f; _curve = curve; }

    float Current => _target.localScale.x / _rest.x;

    void Update()
    {
        if (_t >= 1f) return;
        _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / _duration);
        _target.localScale = _rest * Mathf.LerpUnclamped(_from, _to, _curve(_t));
    }

    public static void AttachAll(Transform root)
    {
        foreach (Selectable s in root.GetComponentsInChildren<Selectable>(true))
            if (s is Button || s is Toggle)             // sliders têm interação própria: não escalar
                if (s.GetComponent<ButtonJuice>() == null) s.gameObject.AddComponent<ButtonJuice>();
    }
}
```

(Esboço de referência; defina `Easing.Fn` como `delegate float Fn(float t)`. Ao implementar de verdade, trate Squash em dois eixos, negado e ordem com `ButtonSfx`.)

Chamar `ButtonJuice.AttachAll(transform.root)` ao lado de `ButtonSfx.AttachAll(transform.root)` em `AntFarmHud.Awake` e `MainMenu.Awake`, e nas telas que se criam depois.

## Pulso de contador

```csharp
// Pulse.Play(rect, 1.12f, 0.15f): sobe e volta; nunca acumula (reinicia do valor atual) e termina em 1 exato.
```

Chame a partir da leitura do `AntFarmHud` quando o valor muda (compare com o último mostrado), com limite de um pulso por 0,25 s.

## Disparo de partículas sem alocar

```csharp
// WorldFx: um ParticleSystem pré-criado por FxId.
public void Burst(FxId id, Vector2Int cell, Color32 tint)
{
    if (!Vfx.Motion || !InView(cell)) return;
    int n = Vfx.Scale(_counts[(int)id]);
    if (n == 0 || !_cooldown.Ready(id, cell)) return;       // teto por tempo real
    var ps = _systems[(int)id];
    var emit = new ParticleSystem.EmitParams { position = CellToWorld(cell), startColor = tint };
    ps.Emit(emit, n);
}
```

Configure o `ParticleSystem` por script de Editor idempotente (módulos: Main — espaço Mundo, `maxParticles` = teto; Emission desligada; Shape ponto; Renderer — sprite 1x1 Point, ordem de ordenação acima do terreno e abaixo do HUD).

## Checklist de revisão de um efeito

- [ ] Ligado a um fato real da simulação (e a recusa tem resposta diferente do sucesso)?
- [ ] Os três `VfxLevel` foram tratados (e Desligado ainda mostra a informação)?
- [ ] Sem alocação por disparo; tetos e cadência definidos; testado a 8x?
- [ ] Termina em estado exato (escala 1, alfa 1, posição de repouso) e se limpa em `OnDisable`?
- [ ] Usa tempo não escalado na UI?
- [ ] Legível sobre o terreno e perto de muitas formigas?
- [ ] Pixel art preservada (Point, sem blur, texto em múltiplos de 8)?
- [ ] Par sonoro existente conferido (`SfxId`); texto novo em `LocalizationCatalog`?
- [ ] Documentação (mapa, guia) e `Production/` atualizados; teste EditMode para a parte pura?
