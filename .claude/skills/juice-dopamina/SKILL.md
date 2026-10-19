---
name: juice-dopamina
description: Cria, revisa e amplia efeitos de "juice"/dopamina/game feel em qualquer jogo — feedback de ação, acerto, dano, morte, combos e marcos, recompensa e coleta, risco, movimento, câmera, botões e telas (hover, clique, compra, negado), números flutuantes, partículas, pausa de impacto, câmera lenta, música e sons de feedback — sempre ligados a eventos reais do jogo. Use sempre que a tarefa pedir feedback visual ou sonoro, polimento, "game feel", "deixar mais gostoso", animação de UI, partículas, celebração, resposta a compra/coleta/dano/morte, ou ao adicionar um sistema novo que precise de resposta perceptível. Use também para propor um pacote de dopamina para um jogo novo (inventário de eventos → efeitos) e para revisar se um efeito respeita opções de acessibilidade, orçamento e legibilidade. Trigger words: juice, dopamina, feedback, game feel, polimento, hit stop, pausa de impacto, câmera lenta, shake, tremor, partícula, número flutuante, combo, marco, celebração, hover, clique, botão animado, squash, stretch, pulso, VFX, SFX de feedback, música adaptativa.
---

# Juice e dopamina — para qualquer jogo

Esta skill diz **como criar feedback que dá prazer ao jogar sem mudar o jogo**: o que reagir, com quê, em que intensidade, como validar. Ela é independente de gênero, engine e estilo. O que depende do projeto (tecnologia, paleta, opções, orçamento) é descoberto no passo 0 do fluxo; os exemplos reais ficam em [references/exemplos/](references/exemplos/).

## Princípios (valem para todo efeito)

1. **Reagir, nunca decidir.** Todo efeito é consequência de um fato que a simulação já produziu. O feedback não altera dano, economia nem progressão. Um fato que o jogo não expõe vira um **evento de fato** (o quê, onde, quem, quanto), nunca um evento visual. Recusa e erro têm resposta própria, diferente da de sucesso.
2. **Proporcional.** A intensidade segue a magnitude do evento: dano, tamanho da sequência, raridade, marco. Pequeno é discreto; raro é grande. O que acontece toda hora nunca pode ser forte.
3. **Em camadas.** O momento importante recebe vários canais ao mesmo tempo (visual, som, movimento, número, tempo/câmera). Um canal só raramente basta, e muitos canais num evento frequente viram ruído.
4. **Legível.** O efeito tem de ser visto na resolução e na distância reais da câmera, e não pode esconder o que o jogador precisa ler. Agrupe repetições (um "+12", não doze "+1"), limite a quantidade simultânea e prefira poucos efeitos nítidos a muitos fracos.
5. **Coerente com o mundo e com a arte.** O efeito obedece ao cenário (no espaço nada cai nem sobe; em pixel art, sem blur) e à paleta e ao tom do jogo. Um jogo calmo não ganha sirene; um frenético não ganha apenas um pulinho.
6. **Controlável.** Toda família de efeito forte (tremor, flash, pausa de impacto, números, alertas, brilhos) tem opção para reduzir ou desligar. A informação continua passando com tudo desligado.
7. **Orçamento.** Sem alocação, `Find*` nem instanciar/destruir por efeito em caminho quente. Pool, buffers reaproveitados, tetos e intervalos mínimos por tipo. Efeito não pode aparecer como custo relevante no profiler.
8. **Termina limpo.** Todo efeito devolve o alvo ao estado exato (escala 1, alfa 1, cor, tempo) ao acabar, ser interrompido ou ter o objeto desativado. Efeitos que mexem em tempo ou câmera têm trava de segurança.
9. **Provisório até o playtest.** Valores ficam em constantes ajustáveis e documentados como provisórios. Só viram regra depois de jogar.

## Fluxo de trabalho

0. **Descubra o projeto.** Leia as instruções do projeto (`CLAUDE.md`, `AGENTS.md`, guias) e levante: engine e pacotes de efeitos disponíveis, estilo de arte e paleta, escala de tempo (pausa por `timeScale`? passo fixo?), onde ficam as opções de acessibilidade/efeitos, e se há localização (texto novo não vai literal em código). Confira também o que já existe de feedback, para ampliar em vez de duplicar. Modelo em [references/exemplos/incrementant/estado-atual.md](references/exemplos/incrementant/estado-atual.md).
1. **Inventarie os eventos.** Liste os fatos que o jogo já emite (ação, acerto, morte, dano, coleta, compra, onda, chefe, marco, fim de sessão, abertura de tela) e os que faltam. Cada linha: o quê, onde, quem, quanto. Veja [references/arquitetura.md](references/arquitetura.md).
2. **Mapeie fato → efeito** pelos catálogos: [interface e progressão](references/catalogo-ui.md) e [mundo e combate](references/catalogo-mundo.md). Escolha a intensidade pela magnitude e o canal pela frequência.
3. **Defina o orçamento:** simultâneos, intervalo mínimo, agregação, comportamento em velocidade alta ou muitos eventos por segundo.
4. **Ligue às opções** (reduzido e desligado, explicitamente) e confira que a informação sobrevive desligada.
5. **Implemente** pelas regras do projeto. Um **diretor central** assina os eventos e despacha; componentes pequenos fazem cada efeito. Montagem de cena/UI por script idempotente quando o projeto exigir. Curvas, durações e técnicas prontas em [references/receitas.md](references/receitas.md); som em [references/audio.md](references/audio.md).
6. **Valide** conforme [references/validacao.md](references/validacao.md): compilar, testar a parte pura, rodar o caminho real, **olhar** o efeito (captura) e medir o estado quando não der para ver. Diga explicitamente o que não foi visto nem ouvido.
7. **Documente** pelo padrão do projeto (mapa, guia, pendências, acompanhamento de produção). Valores provisórios e limitações entram no guia.

## O que um pacote completo costuma cobrir

Use como lista de verificação ao propor um pacote para um jogo novo; adapte ao que o jogo tem de fato.

| Família | Exemplos de momento |
|---|---|
| Ação do jogador | disparo, golpe, salto, uso de habilidade |
| Acerto e dano causado | marcador de acerto, número flutuante, clarão no alvo, quebra de camadas |
| Morte e abate | explosão, marcador de kill, pausa de impacto, tremor proporcional |
| Encadeamento | combo, marcos, janela esvaziando, "salvar" a sequência, recompensa paga no fim |
| Progresso da sessão | marcos de total acumulado, celebração crescente |
| Ritmo | chegada de onda, onda limpa, aviso de ameaça maior |
| Clímax | morte de chefe: pausa, câmera lenta, soco de campo de visão, fanfarra |
| Dano recebido e risco | tremor, bordas da tela por tipo de dano, alerta de vida baixa, reparo, proteção |
| Movimento | arrancada, rastro, linhas de velocidade, esgotar o recurso |
| Recompensa e coleta | brilho no loot, atração, voo até o contador, escala ascendente na cadeia, contagem animada |
| Interface | hover, clique, negado, painéis, compra, revelação por raridade, estado "comprável", idle, ambiente vivo |
| Progresso fora do combate | objetivos de tutorial, descoberta de conteúdo opcional |
| Fim de sessão | relatório com contagem em tique, sinos por etapa |
| Áudio | sons de feedback, música em camadas por intensidade |

## Armadilhas recorrentes

- **Controle de tempo:** pausa de impacto e câmera lenta mexem na escala de tempo. Telas que guardam o valor ao abrir podem restaurar o valor errado. Comece só com o tempo normal, limite duração e frequência e tenha uma trava de segurança que devolve o tempo a 1.
- **Tremor ou soco de câmera** aplicado antes da suavização perde força, e depois do limitador desloca a câmera para sempre. Aplique aditivo, depois do pós-processamento de posição, e remova no quadro seguinte.
- **Efeito fino demais** some em jogo pixelado ou câmera distante. Meça em captura na resolução real.
- **Repetição sem teto** (metralhadora de sons ou números): intervalo mínimo por tipo.
- **Cena/UI recriada por script:** efeito gravado à mão se perde. Prefira comportamento anexado em runtime ou ajuste o script de montagem.
- **Texto novo em código:** respeite a localização do projeto. Números e símbolos não precisam de tradução.
- **Recompensa perdida:** se a sessão acaba antes de o bônus ser pago (morte, saída), isso é decisão de design e tem de estar explícita.
- **Multijogador local:** efeito de tela cheia não diz de quem é o dano; decida por jogador ou desligue.
- **Efeito de recompensa frequente forte** (flash, tremor, zoom) cansa e machuca. Reserve-os a momentos raros.
- **Estado estático sujo entre execuções de teste:** eventos estáticos precisam ser zerados na inicialização, ou o segundo teste herda inscrições.

## O que não fazer

- Não anime um ganho que não aconteceu, nem dê a mesma resposta a sucesso e a recusa.
- Não acople a simulação a tipos visuais (partículas, transform). Ela emite fatos.
- Não crie singleton global escondido para "FX"; se for preciso um serviço, deixe ciclo de vida e inicialização explícitos.
- Não adicione dependência de tween ou VFX sem necessidade; uma corrotina leve ou um tweener próprio costuma bastar para UI.
- Não dependa de som para a informação ser entendida, nem deixe o efeito mudo quando já existe o som par.
- Não declare sucesso por ter compilado: efeito se valida vendo e ouvindo, ou dizendo que não foi possível.

## Arquivos desta skill

| Arquivo | Conteúdo |
|---|---|
| [references/arquitetura.md](references/arquitetura.md) | Eventos de fato, diretor central, ponto único das opções, pool, tween, ciclo de vida, testes |
| [references/catalogo-ui.md](references/catalogo-ui.md) | Botões, painéis, contadores, números flutuantes, compra, raridade, marcos, menus |
| [references/catalogo-mundo.md](references/catalogo-mundo.md) | Ação, acerto, morte, combo, chefe, dano recebido, movimento, loot, câmera, partículas |
| [references/receitas.md](references/receitas.md) | Gramática de movimento, cor, técnicas prontas (pausa de impacto, soco de FOV, faíscas, contagem, cascata), trechos de código |
| [references/audio.md](references/audio.md) | Sons de feedback, síntese provisória, prompts, banco autoral, música em camadas |
| [references/validacao.md](references/validacao.md) | Como validar, o que declarar, checklist de revisão de um efeito |
| [references/exemplos/spacefall.md](references/exemplos/spacefall.md) | Exemplo: jogo de ação espacial 3D em câmera superior, com valores reais |
| [references/exemplos/incrementant/](references/exemplos/incrementant/) | Exemplo: incremental 2D cozy em pixel art, com análise de lacunas e plano em fases |
