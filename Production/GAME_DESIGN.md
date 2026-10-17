# Game design — Acompanhamento de produção

**Projeto:** Incrementant
**Setor:** GAME_DESIGN
**Última avaliação:** 2026-10-17

## Resumo

Conceito informado pelo desenvolvedor em 2026-10-17: formigueiro visto de lado, mapa vertical em X/Y, formigas levam pixels do terreno para fora, com controle de mapa e pixels semelhante a Noita. O protótipo implementa a leitura do agente desse conceito (jogador marca o que cavar; terra entregue compra formigas). Loop, metas e progressão ainda não foram definidos.

## Itens

| ID | Título | Estado | Prioridade | Evidência | Dependências | Verificado em |
|---|---|---|---|---|---|---|
| GD-001 | Conceito: formigário de lado com terreno em pixels | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-17 | REQ-001 | 2026-10-17 |
| GD-002 | Chuva leve e chuva forte | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-17 | REQ-005 | 2026-10-17 |

## Detalhes

### GD-001 — Conceito: formigário de lado com terreno em pixels

Decisões tomadas pelo agente no protótipo, a confirmar: o jogador não controla formigas, só marca pixels a cavar; cada pixel entregue vale 1 de "terra", que compra formigas com custo crescente; o despejo é uma faixa fixa na superfície à direita da entrada. Ver [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md).

### GD-002 — Chuva leve e chuva forte

Regras dadas pelo desenvolvedor em 2026-10-17: chuva leve faz a terra que as formigas tiraram desaparecer com o contato da água; chuva forte solidifica os elementos com física. Interpretações do agente, a confirmar: só a terra despejada (`Spoil`) dissolve, areia despejada não; cada célula aguenta 4 gotas; a chuva forte compacta a crosta atingida (2 células de profundidade) de forma permanente, e o compactado continua escavável e volta a ficar solto ao ser despejado; o tempo é trocado manualmente (tecla C / HUD), sem ciclo automático. Ver [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Chuva").

## Riscos e bloqueios

- Nenhum.

## Não verificado

- Se o loop do protótipo é divertido: depende de playtest do desenvolvedor.
