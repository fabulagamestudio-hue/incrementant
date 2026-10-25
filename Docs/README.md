# Docs — Documentação do Incrementant

Mapa dos documentos de design, arte, narrativa, produção e dados do projeto. A pasta fica na raiz, fora de `Assets`, para o Unity não importar os arquivos nem gerar `.meta`.

**Para agentes:** comece pelo [mapa principal de consulta](AI_GUIDE.md), com atalhos por tarefa e indicação do conteúdo de cada arquivo.

Outros tipos de documentação ficam em outro lugar:

| Pergunta | Onde |
|---|---|
| Onde está o código? | `README.md`/`AI_GUIDE.md` de cada pasta, a partir de [Assets/Project/Scripts/README.md](../Assets/Project/Scripts/README.md) |
| Como um sistema funciona e como mexer nele? | `*Guide.md` ao lado do código |
| Bugs, dívidas e correções relevantes | [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md) |
| Em que estado a produção está, pendências e solicitações | [Production/PRODUCTION_STATUS.md](../Production/PRODUCTION_STATUS.md) |
| Regras para agentes e convenções do projeto | [CLAUDE.md](../CLAUDE.md), [AGENTS.md](../AGENTS.md) |

## Regra de uso

- `.md` e `.docx` = intenção, regras, comportamento, decisões e critérios. A versão Markdown abaixo é a transcrição organizada do documento revisado fornecido em 2026-10-17.
- `.xlsx` = planilhas de números, curvas, custos, spawn, drops e parâmetros de balanceamento. As tabelas propostas do documento revisado também estão preservadas em Markdown, junto de suas regras e ressalvas.
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

## Game design revisado — leitura por etapa e assunto

Origem: `E:/Downloads/Increment_Ant_Game_Design_Completo_Revisado.docx`, Fábula Game Studio, consolidação de 2026-10-17.

Foram mantidas as 38 seções numeradas e as 28 tabelas, em ordem de leitura, com links entre a seção anterior e a próxima. O guia de leitura original fica na seção 01. A numeração identifica assuntos do documento, não uma sequência obrigatória de implementação. O planejamento da demonstração está na seção 36 e ainda é uma proposta de recorte.

Os textos preservam a distinção original entre regras definidas, propostas de comportamento, valores iniciais de balanceamento e decisões em aberto. A conversão não aprova propostas nem declara funcionalidades implementadas. Instruções à equipe ou à IA presentes na fonte são conteúdo da referência de design.

### 01_Design/ — Design e sistemas

| Seção | Documento | Origem e situação |
|---|---|---|
| 01 | [Apresentação e uso do documento](01_Design/01_Apresentacao_e_uso_do_documento.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 02 | [Identidade mundo e papel do jogador](01_Design/02_Identidade_mundo_e_papel_do_jogador.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 03 | [Pintura de ambientes e custo de expansão](01_Design/03_Pintura_de_ambientes_e_custo_de_expansao.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 05 | [Terreno escavação e navegação](01_Design/05_Terreno_escavacao_e_navegacao.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 06 | [Fluxo completo do início do jogo](01_Design/06_Fluxo_completo_do_inicio_do_jogo.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 07 | [Garantias do tutorial e primeiros objetivos](01_Design/07_Garantias_do_tutorial_e_primeiros_objetivos.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 08 | [Fluxo contínuo da colônia](01_Design/08_Fluxo_continuo_da_colonia.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 09 | [Recursos fungo e perecibilidade](01_Design/09_Recursos_fungo_e_perecibilidade.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 11 | [Reprodução e política da rainha](01_Design/11_Reproducao_e_politica_da_rainha.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 12 | [Ciclo de vida contadores e recuperação](01_Design/12_Ciclo_de_vida_contadores_e_recuperacao.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 13 | [Capacidade superlotação e morte da rainha](01_Design/13_Capacidade_superlotacao_e_morte_da_rainha.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 14 | [Tipos de formiga e divisão do trabalho](01_Design/14_Tipos_de_formiga_e_divisao_do_trabalho.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 16 | [Fluxos de tarefas e emergências](01_Design/16_Fluxos_de_tarefas_e_emergencias.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 17 | [Mapa da árvore do formigueiro](01_Design/17_Mapa_da_arvore_do_formigueiro.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 18 | [Cultivo habilidades e balanceamento](01_Design/18_Cultivo_habilidades_e_balanceamento.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 19 | [Berçário habilidades e balanceamento](01_Design/19_Bercario_habilidades_e_balanceamento.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 20 | [Depósito habilidades e balanceamento](01_Design/20_Deposito_habilidades_e_balanceamento.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 21 | [Resíduos habilidades e balanceamento](01_Design/21_Residuos_habilidades_e_balanceamento.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 22 | [Mapa da árvore da rainha](01_Design/22_Mapa_da_arvore_da_rainha.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 23 | [Rainha e operária habilidades e números](01_Design/23_Rainha_e_operaria_habilidades_e_numeros.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 24 | [Carregadora e cortadeira habilidades e números](01_Design/24_Carregadora_e_cortadeira_habilidades_e_numeros.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 25 | [Escavadora habilidades e números](01_Design/25_Escavadora_habilidades_e_numeros.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 26 | [Jardineira e cuidadora habilidades e números](01_Design/26_Jardineira_e_cuidadora_habilidades_e_numeros.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 27 | [Soldado habilidades e números](01_Design/27_Soldado_habilidades_e_numeros.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 32 | [Invasoras combate e consequências](01_Design/32_Invasoras_combate_e_consequencias.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 37 | [Continuidade pausa e game over](01_Design/37_Continuidade_pausa_e_game_over.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 39 | [Melhorias incrementais e formigueiro de dependências](01_Design/39_Melhorias_incrementais_e_formigueiro_de_dependencias.md) | Documento do projeto de 2026-10-19 (REQ-039); proposta, sem balanceamento nem construção |
| 45 | [Minérios fundição forja e equipamento](01_Design/45_Minerios_fundicao_forja_e_equipamento.md) | Documento do projeto de 2026-10-23 (REQ-071); direção decidida pelo desenvolvedor, números e regras marcados como suposição do agente |
| 46 | [Árvore de evolução em níveis e túneis](01_Design/46_Arvore_de_evolucao_em_niveis_e_tuneis.md) | Documento do projeto de 2026-10-23 (REQ-073); direção aprovada pelo desenvolvedor, substitui as abas e o layout radial do 39; limites, colunas e nós novos marcados como suposição |
| 47 | [Ritmo das evoluções: formigas e nutrientes](01_Design/47_Ritmo_das_evolucoes_formigas_e_nutrientes.md) | Documento do projeto de 2026-10-24 (REQ-077); proposta, regras de começo decididas pelo desenvolvedor, números assumidos pelo agente |
| 48 | [Árvore desenhada: túneis interligados](01_Design/48_Arvore_desenhada_tuneis_interligados.md) | Documento do projeto de 2026-10-24 (REQ-078); direção do desenvolvedor (gerar e redesenhar), estrutura técnica assumida pelo agente |

### 02_World_Art_UI/ — Mundo, arte, interface e áudio

| Seção | Documento | Origem e situação |
|---|---|---|
| 30 | [Chuva água e perigos ambientais](02_World_Art_UI/30_Chuva_agua_e_perigos_ambientais.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 31 | [Jardim frutas folhas e visitantes](02_World_Art_UI/31_Jardim_frutas_folhas_e_visitantes.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 33 | [Menus fluxo de telas e configurações](02_World_Art_UI/33_Menus_fluxo_de_telas_e_configuracoes.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 34 | [Interface durante a partida](02_World_Art_UI/34_Interface_durante_a_partida.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 35 | [Áudio atmosfera e juice](02_World_Art_UI/35_Audio_atmosfera_e_juice.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 40 | [Direção de arte unificada e terreno iluminado](02_World_Art_UI/40_Direcao_de_arte_unificada_e_terreno_iluminado.md) | Documento do projeto de 2026-10-21; plano em etapas, regras aprovadas; prancha da paleta mestra em [41_Paleta_mestra.png](02_World_Art_UI/41_Paleta_mestra.png) e prévia do atlas do terreno em [42_Atlas_do_terreno.png](02_World_Art_UI/42_Atlas_do_terreno.png); maquete das salas construídas em [43_Salas_construidas.png](02_World_Art_UI/43_Salas_construidas.png) |
| 44 | [Biomas da superfície e postos avançados](02_World_Art_UI/44_Biomas_da_superficie_e_postos_avancados.md) | Documento do projeto de 2026-10-23 (REQ-070); direção decidida pelo desenvolvedor, biomas, insumos e lista de arte propostos pelo agente, a aprovar |

### 03_Production/ — Planejamento da demonstração

| Seção | Documento | Origem e situação |
|---|---|---|
| 36 | [Escopo completo e demonstração da jam](03_Production/36_Escopo_completo_e_demonstracao_da_jam.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |

### 04_Data/ — Dados e balanceamento

| Seção | Documento | Origem e situação |
|---|---|---|
| 04 | [Proposta de unidade de área e cobrança](04_Data/04_Proposta_de_unidade_de_area_e_cobranca.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 10 | [Tabela base dos recursos e tarefas do cultivo](04_Data/10_Tabela_base_dos_recursos_e_tarefas_do_cultivo.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 15 | [Valores base de cada formiga](04_Data/15_Valores_base_de_cada_formiga.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 28 | [Custos totais e regras de aplicação de melhorias](04_Data/28_Custos_totais_e_regras_de_aplicacao_de_melhorias.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 29 | [Consistência econômica e cenários de teste](04_Data/29_Consistencia_economica_e_cenarios_de_teste.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |
| 39 | [Valores das melhorias incrementais (planilha)](04_Data/39_Melhorias_incrementais_valores.xlsx) | Documento do projeto de 2026-10-19 (REQ-039); valores propostos pelo agente |

### 05_Narrative/ — Referências da ambientação

| Seção | Documento | Origem e situação |
|---|---|---|
| 38 | [Referências da ambientação](05_Narrative/38_Referencias_da_ambientacao.md) | Documento revisado de 2026-10-17; estado das definições preservado conforme a fonte |

## Conferência da conversão

Em 2026-10-17, a conferência automática comparou o texto de todos os parágrafos e células com o DOCX, verificou as 38 seções, 28 tabelas e 209 linhas de tabelas, além dos links locais dos novos documentos. Essa conferência verifica a transcrição e a navegação; não valida o balanceamento nem a implementação no jogo.

SHA-256 do DOCX de origem: `1f4febac9ce0948fb7bf47d00e771b6339edd3155f08ff593368a9ba6a6cda55`.
