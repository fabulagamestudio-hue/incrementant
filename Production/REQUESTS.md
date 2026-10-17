# Solicitações do desenvolvedor

**Projeto:** Incrementant
**Última atualização:** 2026-10-17

## Índice

| ID | Data | Título | Setores | Prioridade | Estado |
|---|---|---|---|---|---|
| REQ-001 | 2026-10-17 | Protótipo de viabilidade do formigário em pixels | PROGRAMMING, GAME_DESIGN | UNDEFINED | IMPLEMENTED |
| REQ-002 | 2026-10-17 | Cenário na superfície na escala das formigas | ART, PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-003 | 2026-10-17 | Mapa procedural que cresce com a câmera, paralaxe e limite de câmera | PROGRAMMING, ART | UNDEFINED | IMPLEMENTED |
| REQ-004 | 2026-10-17 | Velocidade 8x com o mesmo FPS e simulação constante | PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-005 | 2026-10-17 | Chuva leve (dissolve a terra despejada) e chuva forte (compacta o granular) | PROGRAMMING, GAME_DESIGN | UNDEFINED | IMPLEMENTED |

## Registros

### REQ-001 — Protótipo de viabilidade do formigário em pixels

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, GAME_DESIGN
- **Descrição:** Protótipo para avaliar a viabilidade do jogo: formigueiro visto de lado, como um formigário; mapa vertical em X e Y; formigas pegam pixels do terreno e levam para o lado de fora; controle de precisão de mapa e pixels semelhante ao de Noita. Pouco tempo de desenvolvimento.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** —
- **Itens relacionados:** PRG-001, PRG-002, PRG-003, PRG-004, PRG-005, PRG-006, PRG-007, PRG-008, ART-001, ART-002, GD-001
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md)
- **Decisões pendentes:** Validar com playtest se o recorte (marcar para cavar + comprar formigas) é o loop desejado; definir o que o protótipo precisa provar para ser considerado viável.
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: núcleo testado (7 testes EditMode) e exercitado em Play Mode pelo agente; falta playtest do desenvolvedor.
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: mapa, ninho e despejo fixos na cena em vez de gerados em runtime (PRG-005).
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: geração procedural como padrão, mapa fixo vira opção (PRG-005).
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: formigas com aparência de formiga, em sprite com pixels menores que o terreno (PRG-006, ART-001).
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: formigas andam nas paredes quando não há piso, em vez de cair (PRG-007).
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: formigas andam no fundo do formigueiro, além dos pisos (PRG-008, ART-002).

### REQ-002 — Cenário na superfície na escala das formigas

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** ART, PROGRAMMING
- **Descrição:** "Crie um cenário na superfície, considerando tamanho das formigas."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-001
- **Itens relacionados:** PRG-009, ART-003
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md)
- **Decisões pendentes:** Aprovação visual do cenário provisório; se o cenário deve ter vento/animação ou interagir com as formigas (hoje é só visual).
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: cenário pintado por código em três camadas, 4 testes EditMode novos (16/16) e conferido em Play Mode pelo agente; falta aprovação visual do desenvolvedor.
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: cada elemento do cenário vira um asset PNG separado, editável fora da Unity, e a geração posiciona esses assets (ART-003, PRG-009). 2 testes novos (36/36) e Play Mode conferido pelo agente.

### REQ-003 — Mapa procedural que cresce com a câmera, paralaxe e limite de câmera

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, ART
- **Descrição:** "Faz o mapa ser 'procedural', podendo crescer para os lados e para baixo, conforme movimentação de camera. Considere o mundo exterior para fazer paralax, e bote um limitador de movimento de camera para não ter mapa infinito."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-001, REQ-002
- **Itens relacionados:** PRG-010, PRG-011, ART-003
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md)
- **Decisões pendentes:** Tamanho do mundo (hoje 960x520, área inicial 320x200) e se o zoom máximo aberto deve continuar preso ao enquadramento inicial; controles da câmera (WASD/botão do meio, Q/E ou Ctrl+scroll) a aprovar.
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: mundo gerado por blocos conforme a câmera, limitador e paralaxe em duas faixas; 25/25 testes EditMode e Play Mode pelo agente (câmera movida por código); falta uso real de mouse/teclado pelo desenvolvedor.

### REQ-004 — Velocidade 8x com o mesmo FPS e simulação constante

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING
- **Descrição:** "Estamos limitados ao FPS local. Precisamos agora encontrar a otimização para poder fazer isso funcionar com o mesmo FPS no 8x, e manter a simulação constante."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-001, REQ-003
- **Itens relacionados:** PRG-012
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md), [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md) (KI-002, KI-004)
- **Decisões pendentes:** Confirmar em jogo que o 8x ficou no FPS desejado (medido só no editor, pelo agente).
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: simulação por blocos dormentes e navegação com cache; 30/30 testes EditMode (inclui equivalência com a varredura completa); Play Mode pelo agente com o mundo inteiro e 156 formigas: 102 FPS no 8x × 114 no 1x. Falta o desenvolvedor confirmar jogando.

### REQ-005 — Chuva leve (dissolve a terra despejada) e chuva forte (compacta o granular)

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, GAME_DESIGN
- **Descrição:** "Vamos adicionar 2 sistemas de chuva agora. A chuva leve faz os grãos de terra que as formigas pegaram irem desaparecendo conforme a água entra em contato com elas. A chuva forte solidifica os elementos com física."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-001
- **Itens relacionados:** PRG-013, GD-002
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Chuva")
- **Decisões pendentes:** Ver GD-002 (quando chove, se a compactação é permanente, se a chuva leve também afeta areia despejada e o compactado, ritmo da erosão).
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: `RainSimulation` com gotas por célula, 6 testes EditMode novos (suíte 42/42) e Play Mode pelo agente (tempo trocado por código); falta o desenvolvedor jogar e aprovar ritmo e visual.
