# Docs — Documentação do Incrementant

Mapa dos documentos de design, arte, narrativa, produção e dados do projeto. A pasta fica na raiz, fora de `Assets`, para o Unity não importar os arquivos nem gerar `.meta`.

Outros tipos de documentação ficam em outro lugar:

| Pergunta | Onde |
|---|---|
| Onde está o código? | `README.md`/`AI_GUIDE.md` de cada pasta, a partir de [Assets/Project/Scripts/README.md](../Assets/Project/Scripts/README.md) |
| Como um sistema funciona e como mexer nele? | `*Guide.md` ao lado do código |
| Bugs, dívidas e correções relevantes | [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md) |
| Em que estado a produção está, pendências e solicitações | [Production/PRODUCTION_STATUS.md](../Production/PRODUCTION_STATUS.md) |
| Regras para agentes e convenções do projeto | [CLAUDE.md](../CLAUDE.md), [AGENTS.md](../AGENTS.md) |

## Regra de uso

- `.docx` = intenção, regras, comportamento, decisões e critérios.
- `.xlsx` = números, curvas, custos, spawn, drops e parâmetros de balanceamento.
- Os documentos das pastas numeradas são os canônicos e prevalecem sobre tudo em `Old/`.
- Quando um documento e o código divergirem, o código vence; a divergência é registrada em [Production/GAME_DESIGN.md](../Production/GAME_DESIGN.md) ou no `KnownIssues.md`.

## Estrutura

| Pasta | Conteúdo |
|---|---|
| `01_Design/` | Regras do jogo: GDD, progressão, sistemas, inimigos e encontros. |
| `02_World_Art_UI/` | Direção visual: arte, UI, mundo, conceitos. |
| `03_Production/` | Planejamento: plano de produção, recorte de demo, marcos. |
| `04_Data/` | Planilhas de valores (balanceamento, curvas, custos). |
| `05_Narrative/` | Lore, roteiro, diálogos, textos. |
| `Old/` | Material histórico, substituído pelos canônicos. |

Cada documento novo entra na tabela da sua pasta, com uma linha de conteúdo e a data de aprovação quando houver.
