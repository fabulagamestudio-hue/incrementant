# Árvore da rainha — guia

Como a árvore da rainha funciona e como mexer nela. Onde estão os arquivos: [Runtime/README.md](../README.md). Fonte do design: seções 22 a 28 em [Docs/](../../../../../Docs/README.md) ([22](../../../../../Docs/01_Design/22_Mapa_da_arvore_da_rainha.md), [23 a 27](../../../../../Docs/01_Design/23_Rainha_e_operaria_habilidades_e_numeros.md), [28](../../../../../Docs/04_Data/28_Custos_totais_e_regras_de_aplicacao_de_melhorias.md)).

## Como funciona

- **Dados.** `QueenTree` (`Colony/QueenTree.cs`) guarda a tabela dos 26 nós (`QueenNodeDef`: id, ramo, nome, efeito em texto, custo em nutrientes, pré-requisitos, `Implemented`, `Initial`) e o estado de compra: um bit por nó em `QueenTree.Mask`. **A ordem do enum `QueenNode` é o formato do save**: não reordenar nem remover; nó novo vai no fim.
- **Compra.** `AntColony.TryBuy(node)` é o único caminho. Confere (`QueenTree.Check`) já comprado, nó sem sistema no jogo, pré-requisito e saldo, nessa ordem; só então debita `Nutrients` e marca o bit. Recusa não debita nada. Cada nó é comprado uma vez e vale para as formigas existentes e futuras.
- **Nós iniciais.** Q0 e O0 custam 0 e já vêm adquiridos (a regra "só põe ovo com berçário" já está no ciclo de criação).
- **Nós sem sistema (`Implemented = false`).** Aparecem na tela como EM BREVE e não podem ser comprados, para não existir bônus invisível. Hoje: O3, L2, J2, N2 e os nós do soldado (S0 a S3). Quando o sistema existir, troque a flag na tabela e ligue o efeito.
- **Carga e rocha (2026-10-17).** `QueenTree.MaterialCapacity` (pixels de planta = UM: comum 2/O1 3, carregadora 4/L1 6, escavadora 4, demais 2) e `SoilCapacity` (terra, no décimo da escala do design: comum 3/O1 4, escavadora 6/E1 9, demais 3). `CutTimeFactor` (F1: 3/4 do tempo por pixel, equivalente a 4 UM em 1,5 s contra 2 UM em 1 s) e `CanBreakRock` (E2). Como o corte solta pixels, F1 é modelado como taxa e não como peça maior.
- **Pausa.** Abrir a árvore não pausa o jogo (regra em aberto no design, seção 34).

## Efeitos já ligados

| Nó | Efeito | Onde é aplicado |
|---|---|---|
| L0, F0, E0, J0 | Liberam a classe (carregadora, cortadeira, escavadora, jardineira). A comum é sempre liberada. | `QueenTree.IsRoleUnlocked`, usado em `AntColony.NextRole` (automático só entre liberadas; alvo específico bloqueado cai no automático) e no painel Rainha |
| Q1 | Intervalo de postura × 12/18 | `BroodCycle.LayIntervalFactor` |
| Q2 | Até 3 ovos por ciclo; cada um paga o custo do seu tipo; lote parcial se faltar alimento (reserva de 30 N incluída), vaga no berçário ou capacidade adulta | `BroodCycle.Tick` (laço do lote; `BroodContext.RoleProvider`/`CostProvider` resolvem o papel e o custo dos ovos seguintes) |
| O2 | Comuns 24 → 30 células/s | `QueenTree.SpeedFactor`, em `AntColony` (velocidade) |
| L3 | Carregadoras 28 → 35 células/s | idem |
| E1 | Escavadora 32 → 48 células/s ao escavar e pegar material | `QueenTree.DigTimeFactor`, em `AntColony.WorkTimeFactor` |

O que cada classe já fazia antes da árvore (velocidade da classe, escavar em metade do tempo, cortar em metade do tempo) é o valor "depois de L0/F0/E0": a árvore só decide se a rainha pode criá-la.

## Decisões de interpretação

- A colônia **começa só com comuns**; as comuns cortam as plantas (fonte inicial de nutrientes). Jardineiras só depois de J0 e, até existir o cultivo de fungo, ficam sem tarefa.
- Saves anteriores à versão 9 não têm árvore: ao carregar, L0, F0, E0 e J0 voltam liberados (`QueenTree.LegacyMask`), porque as classes já existiam. O custo de qualquer nó é o proposto no design (seção 28), não balanceado em jogo.
- Pré-requisito de L3 é só L0 (proposta do design).
- Custos por ramo conferem com a seção 28 (teste `TheCostsAndRequirementsFollowTheDesign`).

## Tela Evolução

Menu Colônia → EVOLUÇÃO → aba RAINHA E CLASSES (`SkillTreeScreen`, em tela cheia): um ramo por coluna, nós na ordem de compra, detalhe do nó escolhido (nome, efeito base → depois, requisito) e o botão DESBLOQUEAR. Como a tela funciona, os estados visuais e a arte: [HudGuide](../UI/HudGuide.md) ("Tela Evolução"). Os nós da rainha usam ícones gerais, porque o kit de arte da árvore só tem os do formigueiro.

## Como estender

- Novo nó: acrescente ao enum (no fim), à tabela `Defs` e ao total do teste de custos; se tiver efeito, ligue-o num dos métodos de `QueenTree` (`SpeedFactor`, `DigTimeFactor`…) e use-o onde o número é aplicado, nunca duplicando a regra.
- Cuidadora e soldado: criar o papel em `AntRole` (valor novo no fim, vai para o save), marcar N0/S0 como `Implemented` e ligar `IsRoleUnlocked`.
- Não incluir os itens que o design removeu (ponte sobre água, retenção de umidade, terceiro nível de resíduos).
