# Arquitetura proposta (fases 0 a 4 implementadas em 2026-10-18)

> **Situação em 2026-10-19.** O que está abaixo é o plano original. O código seguiu o plano com estas diferenças: não há `Tween.cs` (cada componente tem seu `Update` em tempo não escalado); o evento é `Fx(FxId, Vector2Int, CellMaterial)` com material para a cor; o gasto de nutrientes é um evento à parte com quantia (`NutrientsSpent(int)`); cadência e agregação ficam em `FxLimiter` e `FxAggregator` (lógica pura, testada). Como funciona hoje: `Assets/Project/Scripts/Runtime/Juice/JuiceGuide.md`.

> **Exemplo específico do Incrementant** (incremental 2D cozy em pixel art, análise de 2026-10-18). Serve de modelo de como aplicar a skill; os nomes e valores aqui não valem para outros jogos. A versão genérica está na pasta `references/` acima.

Objetivo: um núcleo pequeno e reutilizável, sem acoplar simulação a visual, que qualquer feature nova use em poucas linhas.

## Pastas e namespaces

```text
Assets/Project/Scripts/Runtime/Juice/          namespace Fabula.Incrementant.Juice
  JuiceGuide.md                                 guia (como funciona / como estender)
  Vfx.cs                                        ponto único: Level, Enabled, Scale(n) — lê GameSettings.Vfx
  Tween.cs                                      tweener leve sem alocação (struct de estado + curvas)
  Easing.cs                                     funções de curva (OutBack, OutElastic, InOutSine…)
  ButtonJuice.cs                                hover/press/release/negado em qualquer Selectable (AttachAll)
  PanelAnimator.cs                              fade+slide+escala de entrada/saída de painéis (CanvasGroup)
  Pulse.cs                                      pulso de escala/cor em RectTransform/Graphic (contadores)
  FloatingText.cs                               números flutuantes em canvas Overlay próprio (pool)
  WorldFx.cs                                    serviço de partículas e ecos no mundo (pool, tetos, agregação)
  FxId.cs                                       enum dos fatos visuais (Dig, Drop, BuildDone, Hatch, Hurt…)
  CameraKick.cs                                 tremor/empurrão aditivo da câmera (respeita Vfx.Level)
Assets/Project/Scripts/Runtime/Colony/AntColony.Fx.cs   evento de fato com posição (ver abaixo)
Assets/Project/Scripts/Tests/EditMode/Juice*Tests.cs     curvas, tetos, VfxLevel (lógica pura)
```

Só `Easing`, `Tween` (matemática) e as regras de teto/agregação são lógica pura e testável em EditMode. O resto é MonoBehaviour fino.

## Evento de fato com posição

Espelha `AntColony.Audio.cs`. A colônia **não conhece** `ParticleSystem` nem `Transform`:

```csharp
// Colony/AntColony.Fx.cs
public sealed partial class AntColony
{
    /// <summary>Algo visível aconteceu na célula dada (escavar, nascer, obra pronta…). Sem ouvinte não custa nada.</summary>
    public event Action<FxId, Vector2Int> Fx;
    void EmitFx(FxId id, Vector2Int cell) => Fx?.Invoke(id, cell);
}
```

- Os pontos de `Emit(SfxId.X)` existentes (`AntColony.cs`, `.Cultivation.cs`, `.Dew.cs`, `.Raids.cs`, `.Waste.cs`) ganham um `EmitFx` ao lado, **só onde há posição natural** (a célula escavada, a vaga do berçário, o cupim atingido). Muitos sons não precisam de FX.
- Quem escuta é `AntFarmGame` (já inscreve/desinscreve `Sfx` em `OnEnable`/`OnDisable`, e **não** inscreve no fundo mudo do menu). Replique a mesma simetria para `Fx`. No fundo do menu (`MenuShowcase`) pode-se ligar o `WorldFx` de propósito para dar vida, com `Vfx.Level` respeitado.
- Para efeitos presos a uma formiga/cupim (dano, nascimento), use a posição da entidade no momento do evento; a **reação corporal** (squash, pisca) é lida pela view a partir de timestamps já existentes (`LastHitTime`) ou de um novo `LastEventTime`, sem a colônia saber da view.
- Eventos de produção repetida (entrega de nutriente, escavar) são **agregados por janela** (ex.: 0,4 s por sala) em `WorldFx`/`FloatingText`, não na colônia.

## `Vfx` — o ponto único do `VfxLevel`

```csharp
public static class Vfx
{
    public static VfxLevel Level => GameSettings.Vfx;
    public static bool Motion => Level != VfxLevel.Off;     // qualquer movimento/partícula
    public static bool Strong => Level == VfxLevel.Full;    // tremor, flash, explosões
    public static int Scale(int count) => Level switch { VfxLevel.Full => count, VfxLevel.Reduced => (count + 1) / 2, _ => 0 };
}
```

Regra: **nenhum efeito lê `GameSettings.Vfx` diretamente**; sempre via `Vfx`. Facilita trocar a política e testar.

## `Tween` leve

Sem alocação por animação: lista pré-dimensionada de estados (`struct` com alvo, início, fim, duração, curva, callback opcional) atualizada por um único `MonoBehaviour` (`Juice` no objeto de serviço) em `Update` com `Time.unscaledDeltaTime`. Alternativa aceitável para o primeiro passo: corrotina por componente (`ButtonJuice`, `PanelAnimator`) — são poucas instâncias e não ficam no caminho quente. **Não** use corrotina por partícula, por formiga ou por número flutuante: use o pool.

## Ordem de execução (registrada em `Production/`: REQ-034, PRG-038 a PRG-045)

| Fase | Item | Entrega | Depende de |
|---|---|---|---|
| 0 | PRG-038 | `Vfx` (único leitor do `VfxLevel`), `Easing`, tweener leve, testes EditMode | — |
| 1 | PRG-039 | `ButtonJuice.AttachAll` no HUD, menu principal e telas (hover, pressionar, soltar, negado) | 0 |
| 2 | PRG-040 | `PanelAnimator` (ESC, Configurações, Evolução, Game over, alerta, pop-ups) e `Pulse` nos contadores | 1 |
| 3 | PRG-041 | `AntColony.Fx` + `FxId` + `WorldFx` com pool; efeitos de escavar, despejar, obra, nascimento e cupim | 0 |
| 4 | PRG-042 | `FloatingText` agregado: nutrientes, custo de compra, dano | 3 |
| 5 | PRG-043 | Reação corporal de formigas, crias e cultivo (squash, pulo, pisca) | 3 |
| 6 | PRG-044 | Árvore de evolução, marcos (nova classe, ataque repelido, game over) e `CameraKick` | 2, 4, 5 |
| 7 | PRG-045 | Atmosfera, vida do menu principal e ajuste dos tetos no F11 | 3, 6 |

As fases 1–2 (UI) e 3–5 (mundo) são trilhas independentes depois da fase 0 e podem andar em paralelo, desde que não toquem os mesmos arquivos (`AntFarmHud` na UI; `AntColony*.cs`/views no mundo).

**Porta de cada fase:** compila; testes EditMode; Play Mode em 1x e 8x; os três `VfxLevel`; conferência no painel F11. Só então o item sobe de `IMPLEMENTED` para `VERIFIED` em `Production/`.

**Por que essa ordem:** a fase 0 é pequena e destrava tudo; os botões são o pedido explícito e dão ganho visível imediato; o mundo precisa do evento com posição antes de qualquer partícula; o tremor de câmera e os marcos ficam por último por serem os efeitos mais fortes e raros, e dependem de números e reações já existentes.

## Montagem e cena

- Serviços runtime (`Juice`, `WorldFx`, `FloatingText`) são criados em código sob um objeto `Juice` (padrão de `TermiteMarkers`/`HudTooltips`), ou por um setup de Editor idempotente em `Editor/Setup`. Nada de editar YAML à mão.
- Materiais/sprites de partícula: um quadrado branco 1x1 Point (gerado em código como as marcas dos cupins) e material URP 2D `Sprites/Default` já presente. Cor vem do material do terreno (`CellMaterials`) ou da paleta do HUD.
- Se criar assets (prefab de `ParticleSystem`, material), crie sob `Assets/Project/Prefabs/Juice` e `Materials`, deixando a Unity gerar os `.meta`.

## Testes

- EditMode: curvas (0→0, 1→1, monotonia onde esperado), `Vfx.Scale` nos três níveis, tetos/agregação do `WorldFx` e do `FloatingText` (lógica separada da parte Unity), determinismo quando houver aleatoriedade com semente.
- Play Mode manual: 1x e 8x, os três `VfxLevel`, pausa, menu do ESC aberto, troca de cena (nada vaza para o menu), e painel F11.
