# Juice (feedback visual) — como funciona

Guia do sistema `Fabula.Incrementant.Juice`. Princípios e catálogos completos: skill `juice-dopamina`.

## Estado

Fases 0 a 4 (PRG-038 a PRG-042): `Vfx`, `Easing`, animação de botões, entrada de painéis, pulso de contadores, partículas no mundo e números flutuantes. Ainda não existe reação corporal das formigas.

## Regras

- Efeitos nunca leem `GameSettings.Vfx` direto: usam `Vfx.Motion` (Completo e Reduzido), `Vfx.Strong` (só Completo) e `Vfx.Scale(n)`.
- UI usa `Time.unscaledDeltaTime`.
- Todo efeito termina em estado exato e se limpa em `OnDisable`.

## ButtonJuice

Anima escala e `anchoredPosition` do próprio RectTransform; não troca sprite nem `transition`, então convive com `SpriteSwap` e com o destaque por código do HUD. A única cor que toca é a de erro do negado (abaixo).

| Gesto | Completo | Reduzido | Desligado |
|---|---|---|---|
| Idle (parado e disponível) | todos flutuam ±2 px em passos de 1 px, ciclo de 1,2 a 2,0 s, fase e período próprios por botão (embaralhados pela razão áurea do id de instância) | nada | nada |
| Hover | escala 1,06 (0,10 s) | nada | nada |
| Pressionar | squash 1,05 × 0,94 (0,06 s) | escala 0,97 | nada |
| Soltar | retorno `OutBack` (0,22 s) | idem | nada |
| Negado (`ButtonJuice.Deny(gameObject)`) | tremidinha de 3 px, 3 ciclos, 0,18 s + cor de erro | cor de erro (vermelho-terra a 65%, 0,15 s) | nada |

- Idle: todos os botões flutuam, dessincronizados. Decisão do desenvolvedor em 2026-10-19, contra o "um botão por vez" do catálogo da skill (testado e achado ruim). O idle some em 0,25 s no hover, clique ou botão desabilitado, e volta depois. O deslocamento (idle e tremidinha) é aditivo: soma só a diferença do frame anterior à `anchoredPosition`, então não briga com layouts. Para um botão específico não flutuar, desmarque `_idle` no componente. Constantes (`IdlePixels`, `IdlePeriodMin`, `IdlePeriodMax`) no topo de `ButtonJuice.cs`; valores provisórios.
- Ligado por `ButtonJuice.AttachAll(transform.root)` em `AntFarmHud.Awake` e `MainMenu.Awake`, ao lado de `ButtonSfx.AttachAll`. Vale para `Button` e `Toggle` (sliders ficam de fora). Sobrevive à recriação do HUD por `AntFarmHudSetup`.
- Telas criadas depois (se houver) precisam chamar `AttachAll` de novo.
- Negado em botão: compra recusada da árvore (`SkillTreeScreen.Buy`, par com `SfxId.UiDeny`). Outros pontos de recusa em botão chamam `ButtonJuice.Deny`. Recusa no mundo (clique na rainha sem vaga/nutrientes, dedo com o jogo pausado): `AntFarmGame.DenyAt` toca `UiDeny` e avisa `WorldDenied`; o HUD mostra um `!` vermelho-terra na célula (números flutuantes).
- A cor de erro vai na cor de vértice (`Graphic.color`) do `targetGraphic`, não no `canvasRenderer` em que o `ColorTint` trabalha. Ela guarda a cor de repouso e volta a ela exata; se outro código trocar a cor no meio do efeito, o efeito desiste sem sobrescrever.
- Ao desligar os efeitos com o cursor em cima, a escala do hover também volta a 1.
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

`AntFarmHud.PulseCounters` (chamado no início de `RefreshTexts`, 4x/s) pulsa Nutrientes (dourado ao subir, vermelho-terra ao descer) e Formigas (dourado ao nascer). A primeira leitura não pulsa. O pulso reage ao valor mostrado; os números flutuantes não saem daqui, e sim dos fatos (abaixo).

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
- Um `ParticleSystem` por tipo, criado em código (sem emissão própria, espaço Mundo, máx. 200, `Sprites/Default`, ordem 12), `Emit` por disparo sem alocar. O material criado em código é destruído no `OnDestroy` do `WorldFx`. Os números e a cadência ficam na tabela `Recipes` de `WorldFx.cs`.
- Só emite o que a câmera enxerga (margem de 2 células). Cadência mínima por tipo em tempo real (`FxLimiter`, testado): a 8x os eventos são muitos.
- `Vfx`: Reduzido = metade das partículas; Desligado = nada.
- Fora desta fase: despejos de `Waste`/`Dew`/`Cultivation`, chuva, afogamento e obra de sala (sem `EmitFx` ainda).

## Números flutuantes (FloatingText)

Canvas Overlay próprio (ordem −1, abaixo do HUD), criado por `AntFarmHud.Awake` com a fonte do HUD e destruído com ele. Copia o `CanvasScaler` do HUD (1920x1080, match 0,5), então fonte, contorno e subida acompanham a escala da interface. Pool fixo de 12 textos (Press Start 2P 24, contorno escuro de 2 px, sem alvo de raio); ao estourar, recicla o mais antigo. Vida de 0,9 s: sobe 44 unidades de referência (Completo) ou fica parado (Reduzido), cheio até 60% e some. Desligado = nada. Textos ancorados no mundo são reprojetados pela câmera todo quadro.

| Texto | Cor | Quando | Origem |
|---|---|---|---|
| `+N` | dourado | a rainha consome nutrientes; soma numa janela de 0,5 s | `FxId.NutrientGain` em `AntColony.cs` (2 pontos), junto da rainha (`Gain`) |
| `-N` | vermelho-terra | formigas feridas por cupim; soma numa janela de 0,3 s, onde caiu o primeiro golpe | `FxId.AntHurt` em `AntColony.Raids.cs` (`Hurt`) |
| `-N` | vermelho-terra | nutrientes gastos (compra na árvore, ovo posto ou forçado); soma numa janela de 0,5 s | `AntColony.NutrientsSpent(quantia)` em `TryBuy`, `ForceLay` e `TickBrood`; sai abaixo do cartão (`Spent`) |
| `!` | vermelho-terra | clique recusado no mundo | `AntFarmGame.WorldDenied`, na célula |

Cada número tem um canal com o próprio `FxAggregator` em `FloatingText`; a âncora é a do primeiro evento da janela (`FxAggregator.Open`). O gasto é um fato com quantia, não a diferença do contador: a diferença misturava ganho e gasto do mesmo intervalo. Os fatos chegam ao HUD por `AntFarmGame.ColonyFx` (repasse de `AntColony.Fx`), `AntFarmGame.ColonyNutrientsSpent` e `AntFarmGame.WorldDenied`; `AntFarmHud` se inscreve em `OnEnable`/`OnDisable`. Com os efeitos desligados não há número: os contadores do HUD continuam com a informação. `WorldFx` ignora os `FxId` sem receita de partícula (`NutrientGain`, `AntHurt`).

## Pendências

- Play Mode dos números flutuantes (fase 4) e das mudanças de 2026-10-19: idle mais rápido (1,2 a 2,0 s), cor de erro do negado, `!` de recusa no mundo, `-N` de gasto por fato, escala dos números fora de 1920x1080.
- Medir a 8x, no painel F11, quantas partículas ficam vivas (`Dig` é o evento mais frequente; tamanho ampliado a pedido do desenvolvedor).
- Saída animada dos painéis; Configurações e Game over sem `PanelAnimator`.
- Dano a cupim (`-N`) e ganhos de colheita/produção por sala.
- Demais fases (5 a 7). Todos os valores são provisórios até o playtest.
