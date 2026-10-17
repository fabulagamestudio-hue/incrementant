# Incrementant — Estado da produção

## Identificação

- **Nome:** Incrementant
- **Descrição:** Formigueiro visto de lado com terreno em pixels; formigas cavam e levam a terra para fora (em fase de protótipo de viabilidade).
- **Engine:** Unity 6000.3.11f1, URP 17.3.0
- **Plataformas-alvo:** Pendente de definição
- **Marco atual:** Pendente de definição
- **Última avaliação:** 2026-10-17

## Resumo executivo

Protótipo de viabilidade do formigário (REQ-001) implementado na cena `AntFarm`: terreno em pixels com física granular, formigas que cavam pixels marcados pelo jogador e os despejam na superfície, e compra de formigas com a terra entregue. O mapa é procedural por padrão (novo a cada Play) e pode ser fixado na cena como PNG editável, com ninho e despejo posicionáveis. No modo procedural, o mapa cresce para os lados e para baixo conforme a câmera se move, até um limite de mundo (960x520), com paralaxe no céu e na grama distante (REQ-003). A velocidade 8x roda com FPS próximo do 1x graças à simulação por blocos (REQ-004). Há dois tipos de chuva, trocados no HUD ou com C: a leve dissolve a terra despejada aos poucos e a forte compacta areia e terra solta, que param de cair (REQ-005). As formigas são sprites animados com pixels menores que os do terreno, e a superfície tem um cenário (grama, flores, trevos, cogumelos, pedrinhas) na escala delas, montado a partir de PNGs isolados e editáveis de cada elemento (arte provisória, REQ-002). Núcleo testado e exercitado em Play Mode pelo agente; falta playtest do desenvolvedor para avaliar o loop.

## Estado por setor

| Setor | Situação | Documento |
|---|---|---|
| Arte | Só arte provisória (formigas e cenário da superfície) | [ART.md](ART.md) |
| Programação | Protótipo do formigário implementado; núcleo verificado | [PROGRAMMING.md](PROGRAMMING.md) |
| Game design | Conceito informado; loop do protótipo a validar | [GAME_DESIGN.md](GAME_DESIGN.md) |
| Áudio | Sem conteúdo | [AUDIO.md](AUDIO.md) |
| Release | Sem conteúdo | [RELEASE.md](RELEASE.md) |

### Arte

- **Entregas verificadas:** Nenhuma
- **Pendências:** ART-001 e ART-002 Sprites provisórios das formigas; ART-003 Cenário da superfície (aprovação visual)
- **Bloqueios:** Nenhum
- **Riscos:** Nenhum

### Programação

- **Entregas verificadas:** PRG-001 Grade e física granular; PRG-002 Formigas; PRG-004 Cena por setup; PRG-005 Modo do mapa; PRG-006 Formigas em sprite; PRG-007 Agarre em paredes; PRG-008 Andar no fundo do formigueiro; PRG-009 Cenário da superfície em camadas; PRG-010 Mundo expansível; PRG-012 Simulação por blocos (8x no mesmo FPS); PRG-013 Chuva leve e forte
- **Pendências:** PRG-003 Render, input e HUD; PRG-011 Câmera, limitador e paralaxe (falta uso real por mouse/teclado)
- **Bloqueios:** Nenhum
- **Riscos:** Render passou a ser o maior custo por frame (KI-001); soluço único ao montar o cache de navegação de áreas grandes (KI-004)

### Game design

- **Entregas verificadas:** Nenhuma
- **Pendências:** GD-001 Conceito (loop, metas e progressão); GD-002 Regras da chuva (interpretações a confirmar)
- **Bloqueios:** Nenhum
- **Riscos:** Nenhum

### Áudio

- **Entregas verificadas:** Nenhuma
- **Pendências:** Nenhuma registrada
- **Bloqueios:** Nenhum
- **Riscos:** Nenhum

### Release

- **Entregas verificadas:** Nenhuma
- **Pendências:** Nenhuma registrada
- **Bloqueios:** Nenhum
- **Riscos:** Nenhum

## Funcionalidades principais

| Sistema | Estado | Item |
|---|---|---|
| Terreno em pixels e física granular | VERIFIED | PRG-001 |
| Formigas: cavar e despejar | VERIFIED | PRG-002 |
| Render, input e HUD | IMPLEMENTED | PRG-003 |
| Modo do mapa: procedural (padrão) ou PNG fixo em cena | VERIFIED | PRG-005 |
| Formigas em sprite (rotação, animação, grão na boca) | VERIFIED | PRG-006 |
| Formigas se agarram em paredes/teto sem piso | VERIFIED | PRG-007 |
| Formigas andam no fundo do formigueiro (vista de cima) | VERIFIED | PRG-008 |
| Cenário da superfície na escala das formigas | VERIFIED | PRG-009 |
| Mundo expansível com geração por blocos e limite | VERIFIED | PRG-010 |
| Câmera móvel, limitador e paralaxe | IMPLEMENTED | PRG-011 |
| Simulação por blocos e navegação com cache (8x) | VERIFIED | PRG-012 |
| Chuva leve (erosão) e chuva forte (compactação) | VERIFIED | PRG-013 |

## Problemas e riscos

- O render da textura do terreno (~4,3 ms por frame no editor) é agora o maior custo; a simulação no 8x caiu para ~2 ms com o mundo inteiro (KI-002 resolvido). Montar o cache de navegação de uma área grande de uma vez custa ~14 ms (KI-004). Ver [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md).

## Solicitações em aberto

| ID | Título | Estado | Prioridade |
|---|---|---|---|
| REQ-001 | Protótipo de viabilidade do formigário em pixels | IMPLEMENTED | UNDEFINED |
| REQ-002 | Cenário na superfície na escala das formigas | IMPLEMENTED | UNDEFINED |
| REQ-003 | Mapa procedural que cresce com a câmera, paralaxe e limite de câmera | IMPLEMENTED | UNDEFINED |
| REQ-004 | Velocidade 8x com o mesmo FPS e simulação constante | IMPLEMENTED | UNDEFINED |
| REQ-005 | Chuva leve (dissolve a terra despejada) e chuva forte (compacta o granular) | IMPLEMENTED | UNDEFINED |

Registro completo: [REQUESTS.md](REQUESTS.md)

## Próximas prioridades

### Definidas pelo desenvolvedor

- Nenhuma definida.

### Sugeridas pelo agente

- Playtest do protótipo pelo desenvolvedor (cena `AntFarm`), para validar o loop e o input (REQ-001, PRG-003).
- Aprovação visual do cenário da superfície (REQ-002, ART-003).
- Testar a câmera (WASD/botão do meio, Q/E ou Ctrl+scroll) e o crescimento do mapa (REQ-003, PRG-011).
- Jogar no 8x com o mapa grande e confirmar o FPS (REQ-004, PRG-012).
- Jogar com as duas chuvas e ajustar ritmo de erosão e densidade das gotas (REQ-005, PRG-013).
- Definir o que o protótipo precisa provar para ser considerado viável (critérios de REQ-001).

## Decisões necessárias

- Tamanho do mundo (960x520) e zoom máximo aberto preso ao enquadramento inicial estão bons? (REQ-003)
- Regras da chuva: quando chove (manual × ciclo automático), compactação permanente ou temporária, se a chuva leve afeta areia despejada e o compactado (GD-002)
- O loop do protótipo (marcar pixels para cavar; terra entregue compra formigas) é o desejado? (GD-001)
- Descrição, plataformas-alvo e marco atual do jogo.
- Substituir a cena de template `Assets/Scenes/SampleScene.unity` e mover `Assets/Settings` para `Assets/Project`? (não feito: quebraria referências; fica como tarefa separada).

## Limitações da avaliação

- Input real e HUD não foram usados por uma pessoa; desempenho medido só no editor.
