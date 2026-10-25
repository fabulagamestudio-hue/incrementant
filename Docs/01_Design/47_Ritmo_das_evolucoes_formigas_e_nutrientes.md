# 47 — Ritmo das evoluções: formigas e nutrientes

Proposta do projeto de 2026-10-24 (REQ-077). A direção vem do desenvolvedor (compras mais baratas, jogo centrado em criar formigas e salas, regra de começo). **Todos os números são suposição do agente**, a calibrar com medição num save novo e com playtest.

Complementa o [documento 46](46_Arvore_de_evolucao_em_niveis_e_tuneis.md) (posição dos nós) e a etapa de custos do REQ-074 (`UpgradeCatalog.TierCostFactors`), que esta proposta substitui.

## Estado (2026-10-24)

Aprovado pelo desenvolvedor ("Mantém as 6 operárias iniciais e pode implementar a proposta do documento 47"). Implementado: propostas 1 a 4 e a abertura dos níveis por compras. Ficou de fora a proposta 5 (marcos visíveis), que depende de definir o efeito na colônia. Também mudou a reserva da rainha: passou a ser a menor entre 30 N e o custo de um ovo, senão o ovo de 6 N ficaria travado pela reserva fixa. Detalhes técnicos: [UpgradesGuide](../../Assets/Project/Scripts/Runtime/Upgrades/UpgradesGuide.md). Falta medir num save novo.

## Pedido do desenvolvedor

- "Hoje em cerca de 20 minutos eu consegui 20 nutrientes. Precisamos que isso seja em algo em torno de 3 minutos. O crescimento tem que ser rápido e contínuo, em pequenos passos."
- "Temos que em pouco tempo o jogador já conseguir fazer os upgrades para ver a colônia se modificando."
- "A velocidade hoje está boa, mas não está tendo evolução do jogador." Considerando "a casa das 500 evoluções" e um prestígio futuro.
- Das ideias do agente, escolheu as **compras mais baratas** no começo.
- "Manter o jogo em criar formigas e salas."

## Regras fixas (decisão do desenvolvedor, 2026-10-24)

1. **Nunca começar com salas extras.** Toda sala é pintada pelo jogador. O kit inicial (sala de nutrientes pronta, canteiros plantados) proposto pelo agente foi recusado.
2. **Nunca começar com formigas extras.** Toda formiga nova nasce de um ovo posto pela rainha. Hoje a partida começa com a rainha e 6 operárias (`ColonySettings._initialAnts`); essa proposta não aumenta esse número. Ver "Decisões pendentes" sobre as 6 iniciais.
3. **A velocidade das formigas fica como está** (REQ-074: terra 10 s na comum, 8 células/s). O ritmo vem do custo e do valor das evoluções, não de andar ou cavar mais rápido de graça.

## Diagnóstico

- Renda medida num save já montado: ~1,4 N/s (~84 N/min). A renda de regime está boa.
- Renda da partida: 20 N em 20 min (relato do desenvolvedor), ~1 N/min. O primeiro nutriente depende de uma corrente longa: cavar a sala, cortar planta, levar, regar, esperar o canteiro (30 s) e entregar à rainha.
- Primeiras compras custam 24 a 54 N (C+1 24, B0 30, H+1 36, C1 54, já com o fator ×0,6 do N1); a operária custa 18 N. Com 1 N/min, o jogador fica minutos sem nada para comprar.
- O volume de evoluções já existe: 75 câmaras e 55 galerias, com 469 níveis nos tetos e 6 galerias sem teto. O problema é o **preço** e a **ordem**, não a quantidade. Parte desses nós ainda não tem sistema (KI-024).

## Meta de ritmo

Medida pelo **tempo até a próxima compra**, não pelo custo. Curva-alvo para ~500 compras até o prestígio, em ~8 h:

| Compra nº | Minuto | Intervalo até a próxima | Renda | Custo de uma compra |
|---|---|---|---|---|
| 1 | ~1,3 | 20 s | ~10 N/min | 2 N |
| 5 | ~2,7 | 20 s | ~11 N/min | 2 N |
| 10 | ~4,5 | 21 s | ~12 N/min | 3 N |
| 30 | ~12 | 25 s | ~16 N/min | 4 N |
| 50 | ~21 | 28 s | ~21 N/min | 6 N |
| 100 | ~48 | 36 s | ~40 N/min | 14 N |
| 200 | ~2 h | 52 s | ~120 N/min | 64 N |
| 300 | ~3,7 h | 68 s | ~350 N/min | 236 N |
| 400 | ~5,8 h | 84 s | ~930 N/min | 780 N |
| 500 | ~8,4 h | 100 s | ~2400 N/min | 2400 N |

Premissas: primeiro nutriente por volta de 1 min; ~60% da renda vai para evoluções e ~40% para ovos. **Meta do pedido:** 20 N juntados até o minuto 3, com ~5 compras feitas.

A renda cresce ~240× na partida. A conta fecha com a população (6 → ~100 formigas, ~16×) vezes o rendimento por formiga e por nutriente (~15×, somando galerias e marcos).

## Proposta

### 1. Custos por nível do formigueiro

Troca os fatores do REQ-074 por faixas de custo que acompanham a curva. Cada nó continua com o custo da planilha como peso relativo dentro do nível.

| Nível | Fase esperada | Custo do nível 1 de uma galeria / câmara | Hoje (aprox.) |
|---|---|---|---|
| N1 | 0 a 20 min | 2 a 6 N | 24 a 90 N |
| N2 | 20 a 80 min | 6 a 30 N | 112 a 630 N |
| N3 | 80 a 170 min | 30 a 125 N | 360 a 1300 N |
| N4 | 170 a 350 min | 125 a 780 N | 1080 a 2250 N |
| N5 | 350 a 500 min | 780 a 2400 N | 720 a 3150 N |

A abertura dos níveis (`DepthToOpenTier`, hoje 0, 0, 5, 20, 50, 100) passa a contar **compras** e fica coerente com a tabela: N2 em ~40 compras, N3 em ~130, N4 em ~240, N5 em ~380.

### 2. Crescimento por nível mais suave

| Tipo | Hoje | Proposta | Nível 10 / nível 1 |
|---|---|---|---|
| Física | ×1,35 | ×1,15 | 15× → 3,5× |
| Economia | ×1,15 | ×1,12 | 3,5× → 2,8× |
| Combate | ×1,22 | ×1,15 | 6× → 3,5× |

Com isso cada nível custa pouco mais que o anterior e a compra seguinte está sempre perto.

### 3. Primeiros passos: compras baratas no N1, sobre formigas e nutrientes

Galerias que já têm sistema, trazidas para o N1 e baratas. O jogador as compra durante a escavação das primeiras salas, e cada nível é visível no jogo.

| Galeria | Efeito por nível (atual) | Hoje | Proposta |
|---|---|---|---|
| C+1 Canteiro ágil | canteiro 8% mais rápido | N1, 24 N | N1, 2 N, sem teto |
| O+1 Passo da comum | operária 4% mais rápida | N2, 112 N | N1, 2 N, teto 25 |
| T+2 Escavação | cavar 5% mais rápido | N2, 210 N, exige T1 e E0 | N1, 3 N, exige só T0, teto 10 |
| O+3 Corte da comum | cortar 3% mais rápido | N2, 112 N | N1, 3 N, teto 16 |
| Q+1 Postura | ovo 3% mais rápido | N2, 280 N | N1, 3 N, teto 10 |
| Q+2 Ovo barato | ovo 2% mais barato | N1, 90 N | N1, 4 N, teto 15 |
| H+1 Vagas de adulta | vagas por área 4% maiores | N1, 36 N | N1, 4 N |
| C+4, B+3, Q+4 Área | +30 / +40 / +40 células | N1, 36 N | N1, 3 N |
| C+2 Nutriente nutritivo | entrega vale 5% mais | N2, 168 N, exige C3 | N1, 5 N, exige C1, sem teto |

Com os tetos acima, só esses nós somam ~150 níveis baratos para a primeira hora.

Na implementação, descer um nó de nível muda o teste `UpgradeCatalogTests` que compara níveis com os anéis do documento 39 (hoje a única exceção é O+2); a lista de exceções cresce com esses nós.

### 4. Formigas como compra: custo do ovo cresce com a colônia

Criar formigas é a outra metade do loop e precisa de uma escada própria, também em passos pequenos.

- Custo do ovo da operária = **6 × 1,04^(adultas − 6)**: 6 N com 6 adultas, ~10 N com 20, ~34 N com 50, ~240 N com 100 (hoje: 18 N fixo).
- As outras classes mantêm a proporção atual (`AntRoles.FoodCost` ÷ 18): a carregadora custa 1,33× a operária, o soldado 1,9×.
- Q+2 e N3 continuam baixando esse custo.

### 5. Marcos nas galerias

Hoje: ×1,5 a cada 10 níveis nas galerias de economia e de combate, e um marco qualitativo no 25. Proposta: em todas as galerias de formiga e nutriente, um **marco visível no nível 5, no 10 e no 25** (número grande, efeito na colônia), para o jogador ver a colônia mudar além do número.

### 6. Contagem até o prestígio

| Nível | Compras previstas |
|---|---|
| N1 | ~180 (câmaras e primeiros passos) |
| N2 | ~130 |
| N3 | ~90 |
| N4 | ~60 |
| N5 | ~40 |

Total de ~500. Os nós sem sistema (KI-024) não contam até ganharem um.

## Como validar

1. Num save novo, sem kit, medir em que minuto acontece cada compra e o saldo no minuto 3.
2. Comparar com a tabela "Meta de ritmo": o critério sugerido é 20 N juntados até o minuto 3 e nunca mais de ~2× o intervalo-alvo sem compra disponível.
3. Ajustar só as tabelas de custo, crescimento e ovo; os efeitos ficam.

## Decisões pendentes

- ~~As 6 operárias iniciais seguem?~~ Decidido em 2026-10-24: seguem.
- Calibrar a curva, os custos e o ovo depois da medição num save novo.
- O+3 ficou com teto 16, não 25: a regra do documento 39 limita a redução a 50% (16 × 3% = 48%).
- Efeito dos marcos visíveis (proposta 5).
