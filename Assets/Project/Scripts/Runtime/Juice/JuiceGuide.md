# Juice (feedback visual) — como funciona

Guia do sistema `Fabula.Incrementant.Juice`. Princípios e catálogos completos: skill `juice-dopamina`.

## Estado

Fases 0 a 4 (PRG-038 a PRG-042): `Vfx`, `Easing`, animação de botões, entrada de painéis, pulso de contadores, partículas no mundo e números flutuantes. Ainda não existe reação corporal das formigas.

## Regras

- Efeitos nunca leem `GameSettings.Vfx` direto: usam `Vfx.Motion` (Completo e Reduzido), `Vfx.Strong` (só Completo) e `Vfx.Scale(n)`.
- UI usa `Time.unscaledDeltaTime`.
- Todo efeito termina em estado exato e se limpa em `OnDisable`.

## ButtonJuice

Anima só escala e `anchoredPosition` do próprio RectTransform; não troca sprite, cor nem `transition`, então convive com `SpriteSwap` e com o destaque por código do HUD.

| Gesto | Completo | Reduzido | Desligado |
|---|---|---|---|
| Idle (parado e disponível) | flutua ±2 px em passos de 1 px, ciclo de 2,4 a 4,2 s, fase e período próprios por botão (embaralhados pela razão áurea do id de instância) | nada | nada |
| Hover | escala 1,06 (0,10 s) | nada | nada |
| Pressionar | squash 1,05 × 0,94 (0,06 s) | escala 0,97 | nada |
| Soltar | retorno `OutBack` (0,22 s) | idem | nada |
| Negado (`ButtonJuice.Deny(gameObject)`) | tremidinha de 3 px, 3 ciclos, 0,18 s | nada | nada |

- O idle some em 0,25 s no hover, clique ou botão desabilitado, e volta depois. O deslocamento (idle e tremidinha) é aditivo: soma só a diferença do frame anterior à `anchoredPosition`, então não briga com layouts. Para um botão específico não flutuar, desmarque `_idle` no componente. Constantes (`IdlePixels`, `IdlePeriod`) no topo de `ButtonJuice.cs`.
- Ligado por `ButtonJuice.AttachAll(transform.root)` em `AntFarmHud.Awake` e `MainMenu.Awake`, ao lado de `ButtonSfx.AttachAll`. Vale para `Button` e `Toggle` (sliders ficam de fora). Sobrevive à recriação do HUD por `AntFarmHudSetup`.
- Telas criadas depois (se houver) precisam chamar `AttachAll` de novo.
- Negado hoje só na compra recusada da árvore (`SkillTreeScreen.Buy`, par com `SfxId.UiDeny`). Outros pontos de recusa chamam `ButtonJuice.Deny`.
- Não faz tint de cor no negado: o `ColorTint` do botão brigaria com ele.
- Foco por teclado/gamepad não anima (o foco do mouse ficaria grudado após o clique).

## PanelAnimator

Anima a **entrada**: a cada `SetActive(true)` o painel aparece com fade. A saída é imediata de propósito (o HUD e o ESC dependem de `activeSelf`). Ligado em `AntFarmHud.Awake` com `PanelAnimator.Attach(painel, estilo)`, depois do `SetActive(false)` inicial.

| Estilo | Efeito (Completo) | Usado em |
|---|---|---|
| `Pop` | fade 0,20 s + sobe 8 px + escala 0,96→1 `OutBack` | Pintar, Ambientes, Rainha, alerta de cupins |
| `Fade` | fade 0,14 s | árvore de evolução, menu do ESC (têm fundo que não pode se mover) |

Reduzido = só fade; Desligado = nada. Termina em alfa 1, posição e escala originais. Adiciona um `CanvasGroup` se faltar. Configurações e Game over ainda não usam.

## Pulse

`Pulse.Play(componente, cor)` em um texto/ícone: escala 1→1,12→1 (0,15 s) e cor até o destaque e de volta (0,3 s), no máximo um pulso a cada 0,25 s. Guarda a cor de repouso no `Awake`, então o gráfico não pode ter a cor trocada por outro código.

`AntFarmHud.PulseCounters` (chamado no início de `RefreshTexts`, 4x/s) pulsa Nutrientes (dourado ao subir, vermelho-terra ao descer) e Formigas (dourado ao nascer). A primeira leitura não pulsa.

## Efeitos no mundo (WorldFx)

Fluxo: a simulação chama `EmitFx(FxId, x, y, material)` (`Colony/AntColony.Fx.cs`, ao lado dos `Emit(SfxId...)`), o evento `AntColony.Fx` chega em `AntFarmGame.PlayColonyFx` e `WorldFx.Play` solta as partículas. A colônia não conhece `ParticleSystem`. `AntFarmGame` cria o `WorldFx` e se inscreve/desinscreve junto do `Sfx`; o fundo mudo do menu não usa.

| `FxId` | Disparo | Cor | Partículas (Completo) |
|---|---|---|---|
| `Dig` | formiga tira terra (3 pontos em `AntColony.cs`) | material da célula | 5 |
| `Drop` | terra despejada fora (`TryDrop`) | material carregado | 6 |
| `Build` | pixel de obra encaixado (`FinishPlace`) | material carregado | 7 |
| `Hatch` | formiga nasceu (`TickBrood`) | verde-claro | 12 |
| `TermiteHit` / `TermiteKilled` | `HitTermite` | vermelho-terra | 5 / 14 |

- Cada partícula mede 2 células (entre 75% e 100%): o dobro do pixel que originou o efeito, constante `Big` em `WorldFx.cs`.
- Um `ParticleSystem` por tipo, criado em código (sem emissão própria, espaço Mundo, máx. 200, `Sprites/Default`, ordem 12), `Emit` por disparo sem alocar. Os números e a cadência ficam na tabela `Recipes` de `WorldFx.cs`.
- Só emite o que a câmera enxerga (margem de 2 células). Cadência mínima por tipo em tempo real (`FxLimiter`, testado): a 8x os eventos são muitos.
- `Vfx`: Reduzido = metade das partículas; Desligado = nada.
- Fora desta fase: despejos de `Waste`/`Dew`/`Cultivation`, chuva, afogamento e obra de sala (sem `EmitFx` ainda).

## Números flutuantes (FloatingText)

Canvas Overlay próprio (ordem −1, abaixo do HUD), criado por `AntFarmHud.Awake` com a fonte do HUD e destruído com ele. Pool fixo de 12 textos (Press Start 2P 24, contorno escuro de 2 px, sem alvo de raio); ao estourar, recicla o mais antigo. Vida de 0,9 s: sobe 44 px (Completo) ou fica parado (Reduzido), cheio até 60% e some. Desligado = nada. Textos ancorados no mundo são reprojetados pela câmera todo quadro.

| Texto | Cor | Quando | Origem |
|---|---|---|---|
| `+N` | dourado | a rainha consome nutrientes; soma numa janela de 0,5 s (`FxAggregator`) | `FxId.NutrientGain` em `AntColony.cs` (2 pontos), junto da rainha |
| `-1` | vermelho-terra | formiga ferida por cupim | `FxId.AntHurt` em `AntColony.Raids.cs`, na formiga |
| `-N` | vermelho-terra | nutrientes caíram (compra, ovo) | `AntFarmHud.PulseCounters` (diff do contador), sai abaixo do cartão |

Os fatos chegam ao HUD por `AntFarmGame.ColonyFx` (repasse de `AntColony.Fx`; `AntFarmHud` se inscreve em `OnEnable`/`OnDisable`). `WorldFx` ignora os `FxId` sem receita de partícula (`NutrientGain`, `AntHurt`).

## Pendências

Validação das partículas em Play Mode (1x e 8x, os três níveis de efeitos, painel F11), saída animada dos painéis, Configurações/Game over, dano a cupim (`-N`) e ganhos de colheita/produção por sala, e demais fases (5 a 7).
