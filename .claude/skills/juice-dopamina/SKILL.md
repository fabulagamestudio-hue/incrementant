---
name: juice-dopamina
description: Cria e revisa efeitos de "juice"/dopamina no Incrementant — animação e feedback de botões (hover, clique, foco, negado), partículas, squash/stretch, pulos, fades, números flutuantes, tremor de câmera, pulsos e celebrações ligados a eventos reais da simulação. Use sempre que a tarefa pedir feedback visual, polimento, "game feel", animação de UI, partículas, efeito de compra/nascimento/escavação/ataque de cupins/game over, ou ao adicionar um sistema novo que precise de resposta visível. Use também ao revisar se um efeito respeita a opção Efeitos visuais (VfxLevel), o limite de orçamento e a legibilidade. Trigger words: juice, dopamina, feedback, hover, clique, botão animado, partícula, particle, squash, stretch, pulo, bounce, fade, shake, tremor, pulso, número flutuante, game feel, polimento, celebração, efeito visual, VFX.
---

# Juice e dopamina — Incrementant

Esta skill diz **como criar efeitos que dão prazer ao jogar sem quebrar o que o projeto já é**: um incremental cozy de formigueiro em pixel art 2D, onde centenas de ações por segundo acontecem sem o jogador. Ela nasceu de uma análise do código (2026-10-18); nada do que está descrito como "proposto" foi implementado ainda. O estado real está em [references/estado-atual.md](references/estado-atual.md).

Referência de design (fonte da intenção): `Docs/02_World_Art_UI/35_Audio_atmosfera_e_juice.md` — "o juice reforça cada clique e cada conquista… acompanha um resultado verdadeiro".

## Princípios (valem para todo efeito)

1. **Verdade primeiro.** Todo efeito é consequência de algo que a simulação fez. Nunca anime um ganho que não ocorreu (compra recusada tem resposta própria, não a de sucesso). A simulação **emite fatos**; a camada visual **reage** — mesma separação que `AntColony.Sfx` já faz com o som.
2. **Cozy, não arcade.** Movimentos curtos e macios (80–250 ms), cores quentes da paleta do jogo, sem flash branco de tela cheia, sem tremor forte, sem sirene. Risco (cupins, afogamento) usa sinal claro e **breve**, nunca tensão permanente.
3. **Legibilidade acima de tudo.** O jogador precisa continuar vendo formigas, caminhos e números. Agrupe repetições (um "+12" em vez de doze "+1"), limite quantidade simultânea e mantenha efeitos pequenos perto de muitas formigas.
4. **Pixel art.** Partículas são quadrados de 1 px do mundo (ou da grade da UI), sem blur, sem rotação suave em sprites de pixel, paleta limitada. Texto da UI é Press Start 2P: só tamanhos múltiplos de 8, então **não escale texto com valores fracionários** em repouso — use pulsos que voltam à escala 1 exata, ou anime posição/cor/alfa.
5. **Respeite `VfxLevel`.** Já existe em `GameSettingsData.Vfx` (Completo / Reduzido / Desligado) e hoje **nenhum código o consome**. Todo efeito novo consulta `GameSettings.Vfx`: Completo = tudo; Reduzido = sem tremor, sem flash, menos partículas, só feedback essencial; Desligado = só mudança de estado imediata (sem movimento). Sons não dependem dele.
6. **Orçamento.** Nada de `new`, `Find*`, `Instantiate`/`Destroy` por efeito em caminho quente. Pool, buffers reaproveitados, tetos por tipo. O projeto já mede desempenho (`SimProfiler`, painel F11): efeito novo não pode aparecer ali como custo relevante.
7. **Tempo certo.** UI e overlays usam `Time.unscaledDeltaTime` (o projeto não usa `timeScale`; a pausa só para o passo da simulação, então a UI e o menu do ESC continuam vivos). Efeitos de mundo disparados pela simulação param sozinhos na pausa porque os eventos param; animações já em curso podem terminar.
8. **Sem dependências novas.** Sem DOTween/LeanTween. Use corrotinas leves ou um pequeno tweener próprio (ver arquitetura). `ParticleSystem` (módulo já no manifest) para o mundo; o VFX Graph só para momentos grandes e únicos (já usado em `SplashLogoDissolve`).

## Fluxo de trabalho para um efeito novo

1. **Identifique o fato.** Qual evento real dispara isto? Se a simulação ainda não o expõe, adicione um evento de fato (não de visual) — ver [references/arquitetura.md](references/arquitetura.md).
2. **Escolha a receita** em [references/catalogo-ui.md](references/catalogo-ui.md) (botões, painéis, números) ou [references/catalogo-mundo.md](references/catalogo-mundo.md) (formigas, terreno, cupins, câmera) e a gramática de movimento em [references/receitas.md](references/receitas.md).
3. **Defina o orçamento:** quantos ao mesmo tempo, intervalo mínimo, o que acontece em 8x.
4. **Ligue ao `VfxLevel`** (os três níveis, explicitamente).
5. **Implemente** seguindo as regras do `CLAUDE.md` do projeto: C# em `Fabula.Incrementant.*`, runtime sem `UnityEditor`, campos `[SerializeField]`, inscrição/desinscrição simétrica, validação cedo. Montagem de cena/HUD só por script de Editor idempotente (o HUD é **recriado do zero** por `AntFarmHudSetup`: ajuste manual na cena se perde).
6. **Valide:** compilar, Play Mode olhando o efeito em 1x e 8x, os três `VfxLevel`, e o painel F11. Se não puder ver, diga isso.
7. **Documente** (mapa × guia): classe nova → mapa (`Runtime/README.md`); comportamento → guia do sistema (`Runtime/Juice/JuiceGuide.md`); dívida → `Scripts/KnownIssues.md`. Atualize `Production/` conforme a skill `fabula-production-docs` (item novo em `REQUESTS.md` como `PLANNED`; só vira `VERIFIED` com evidência).

## Armadilhas já conhecidas deste projeto

- **O HUD é gerado.** Não grave animação no prefab/cena à mão. Comportamento por runtime (`ButtonJuice.AttachAll(transform.root)`, o mesmo padrão de `ButtonSfx.AttachAll`) sobrevive a recriações do HUD.
- **`SpriteSwap` já usa o sprite `*_focus` como "pressionado"** em vários botões. Hover e clique animados devem ser **ortogonais** (escala/offset/cor via componente), sem trocar de `transition` nem de sprite.
- **Botões com `Transition.None`** (pincéis, abas) usam o destaque feito por código em `AntFarmHud` (todo frame). Não brigue com ele: some feedback por cima (pulso/escala do `RectTransform` do filho), não no `Image.sprite`.
- **Texto pixel e escala:** ver princípio 4. Pulso de texto = escala 1→1,15→1 em ≤150 ms terminando exatamente em 1, ou troque cor.
- **Chuva de eventos a 8x.** `Dig`, `Drop`, `AntBorn`… disparam várias vezes por segundo. Use o mesmo padrão de intervalo mínimo de `GameAudio.BuildRules`, e agregue por célula/sala.
- **Câmera tem limitador e transições** (`MapCamera.StepTransition`, `Clamp`). Tremor entra como **deslocamento aditivo aplicado depois** do clamp e removido no frame seguinte, nunca alterando a posição que a câmera guarda.
- **Formigas são `SpriteRenderer`s com rotação própria** (patas voltadas para a superfície). Squash/stretch vai num filho ou como multiplicador de `localScale`, sem mexer na rotação.
- **O terreno é uma textura** (`AntFarmView`) em janela visível: não há objeto por célula. Efeito de escavar/cair terra = partículas no mundo com a cor do material (`CellMaterials`), não animação da célula.
- **Textos acentuados em caixa alta** saem ruins na fonte; números flutuantes devem ser majoritariamente numéricos/símbolos.

## O que não fazer

- Não crie singleton global escondido para "FX". Se precisar de serviço estático, siga o molde de `GameAudio` (ciclo de vida explícito, `DontDestroyOnLoad` documentado).
- Não toque `AntColony` com tipos visuais (`ParticleSystem`, `Transform`…). Evento de fato com `Vector2Int` de célula e um id.
- Não use shake, flash ou zoom como recompensa frequente; reserve-os para momentos raros (nova classe, fim de ataque vencido).
- Não deixe efeito mudo para o jogador sem o par sonoro quando já existir um `SfxId` adequado, nem invente som novo sem passar por `SfxSynth` (ver `Audio/AudioGuide.md`).

## Arquivos desta skill

| Arquivo | Conteúdo |
|---|---|
| [references/estado-atual.md](references/estado-atual.md) | O que já existe de feedback no projeto e as lacunas encontradas na análise |
| [references/arquitetura.md](references/arquitetura.md) | Estrutura proposta (pastas, classes, eventos, pool, VfxLevel) e ordem de implementação |
| [references/catalogo-ui.md](references/catalogo-ui.md) | Efeitos de botões, painéis, HUD, telas e textos, com parâmetros |
| [references/catalogo-mundo.md](references/catalogo-mundo.md) | Efeitos de partículas e animação no mundo por evento da simulação, com orçamento |
| [references/receitas.md](references/receitas.md) | Curvas, durações, cores e trechos de código reutilizáveis |
