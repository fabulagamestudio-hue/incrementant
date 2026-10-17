# 14 — Tipos de formiga e divisão do trabalho

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 14. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Tipo | Função base e atividade visível |
| --- | --- |
| Rainha | Centro da reprodução; põe ovos na postura. Não é criada pelo jogador como outra operária. |
| Operária comum | Generalista: corta, coleta, transporta folhas e terra, escava, instala e cuida do cultivo, atende crias e remove resíduos. |
| Carregadora | Mais eficiente no transporte de folhas, frutos e materiais. Consegue cortar sozinha; não depende de cortadeira. |
| Cortadeira | Prepara folhas na superfície e reduz o trabalho de coleta das demais. Várias podem trabalhar na mesma folha desde o início. |
| Escavadora | Prioriza obras e desobstrução. Melhora a remoção de terra e, após evolução, quebra pedra. |
| Jardineira | Prioriza o cultivo, otimiza crescimento de fungo e pode transplantar fragmentos para novas áreas. |
| Cuidadora | Alimenta e transporta crias; evolui para alimentação em lote e resgate do berçário. |
| Soldado | Patrulha entradas, combate invasoras e evolui em dano, vida e contenção. |

Especializações melhoram tarefas que já funcionam com comuns. Não criar uma dependência circular em que uma sala exige uma especialista cujo desbloqueio depende dessa sala. Classes pertencem exclusivamente à árvore da rainha. Ambientes pertencem à árvore do formigueiro.

Comuns continuam úteis depois das compras. Soldados têm foco de defesa e não são classificados como produtivos na recuperação. Uma carregadora pode cortar e trazer material sem cortadeira; várias formigas trabalharem na mesma folha não é uma habilidade comprável.

A representação visual deve diferenciar papéis por silhueta, cabeça, escala relativa, carga e animação de trabalho, além de cor. São classes de gameplay inspiradas na divisão de trabalho de saúvas. Machos reprodutivos e novas rainhas aladas não entram no elenco controlável definido.

## Cooperação e identidade dos papéis

A comum garante independência do início. Especialistas não substituem a rainha ou uma estrutura; elas executam partes da rotina melhor. Uma classe desbloqueada, mas ainda sem indivíduos, não melhora a produção por si só.

Carregadora sem cortadeira pode buscar folha. Cortadeira sem carregadora ainda pode preparar pedaços que comuns transportam. Cultivo sem jardineira e berçário sem cuidadora continuam funcionando com comuns. Pedra é a exceção de capacidade: só uma escavadora com a melhoria apropriada consegue removê-la.

Reprodução cria indivíduos novos com papel escolhido, sem promover uma comum adulta instantaneamente. O objetivo de leitura visual é reconhecer função por comportamento e carga, sem depender de memorizar oito cores.

---

[← 13 — Capacidade superlotação e morte da rainha](../01_Design/13_Capacidade_superlotacao_e_morte_da_rainha.md) · [15 — Valores base de cada formiga →](../04_Data/15_Valores_base_de_cada_formiga.md)
