# Mapa principal — consulta dos agentes

Entrada rápida para os 38 arquivos de design derivados do documento revisado e para os documentos do projeto criados depois (39 em diante). Este mapa indica **onde procurar**; regras, números e decisões ficam nos documentos apontados. O [README](README.md) reúne a origem, as convenções e a conferência da conversão.

## Como consultar

1. Escolha o assunto na tabela de atalhos ou procure uma palavra-chave neste arquivo.
2. Abra o documento indicado como primeira consulta. Consulte os complementos apenas se a tarefa depender deles.
3. Dentro do documento, localize o subtítulo relevante antes de ler o restante. Exemplo no terminal: `rg -n '^##' Docs/01_Design/03_Pintura_de_ambientes_e_custo_de_expansao.md`.
4. Preserve a distinção entre regra definida, valor proposto e decisão em aberto. A numeração é de assuntos, não de ordem obrigatória de implementação.

## Atalhos por tarefa

| Pedido ou sistema | Primeira consulta | Complementos conforme a necessidade |
|---|---|---|
| Entender a proposta e os termos | [02 — Identidade mundo e papel do jogador](01_Design/02_Identidade_mundo_e_papel_do_jogador.md) | [01 — Apresentação e uso do documento](01_Design/01_Apresentacao_e_uso_do_documento.md), [08 — Fluxo contínuo da colônia](01_Design/08_Fluxo_continuo_da_colonia.md) |
| Pintar, cobrar área ou tratar sobreposição | [03 — Pintura de ambientes e custo de expansão](01_Design/03_Pintura_de_ambientes_e_custo_de_expansao.md) | [04 — Proposta de unidade de área e cobrança](04_Data/04_Proposta_de_unidade_de_area_e_cobranca.md), [05 — Terreno escavação e navegação](01_Design/05_Terreno_escavacao_e_navegacao.md) |
| Escavar, navegar ou transportar terra | [05 — Terreno escavação e navegação](01_Design/05_Terreno_escavacao_e_navegacao.md) | [25 — Escavadora habilidades e números](01_Design/25_Escavadora_habilidades_e_numeros.md), [30 — Chuva água e perigos ambientais](02_World_Art_UI/30_Chuva_agua_e_perigos_ambientais.md) |
| Nova partida e tutorial | [06 — Fluxo completo do início do jogo](01_Design/06_Fluxo_completo_do_inicio_do_jogo.md) | [07 — Garantias do tutorial e primeiros objetivos](01_Design/07_Garantias_do_tutorial_e_primeiros_objetivos.md), [04 — Proposta de unidade de área e cobrança](04_Data/04_Proposta_de_unidade_de_area_e_cobranca.md) |
| Economia, alimento, fungo ou deterioração | [09 — Recursos fungo e perecibilidade](01_Design/09_Recursos_fungo_e_perecibilidade.md) | [10 — Tabela base dos recursos e tarefas do cultivo](04_Data/10_Tabela_base_dos_recursos_e_tarefas_do_cultivo.md), [18 — Cultivo habilidades e balanceamento](01_Design/18_Cultivo_habilidades_e_balanceamento.md), [20 — Depósito habilidades e balanceamento](01_Design/20_Deposito_habilidades_e_balanceamento.md), [21 — Resíduos habilidades e balanceamento](01_Design/21_Residuos_habilidades_e_balanceamento.md), [29 — Consistência econômica e cenários de teste](04_Data/29_Consistencia_economica_e_cenarios_de_teste.md) |
| Reprodução, nascimento ou recuperação | [11 — Reprodução e política da rainha](01_Design/11_Reproducao_e_politica_da_rainha.md) | [12 — Ciclo de vida contadores e recuperação](01_Design/12_Ciclo_de_vida_contadores_e_recuperacao.md), [13 — Capacidade superlotação e morte da rainha](01_Design/13_Capacidade_superlotacao_e_morte_da_rainha.md), [19 — Berçário habilidades e balanceamento](01_Design/19_Bercario_habilidades_e_balanceamento.md) |
| Classes, atributos e distribuição de tarefas | [14 — Tipos de formiga e divisão do trabalho](01_Design/14_Tipos_de_formiga_e_divisao_do_trabalho.md) | [15 — Valores base de cada formiga](04_Data/15_Valores_base_de_cada_formiga.md), [16 — Fluxos de tarefas e emergências](01_Design/16_Fluxos_de_tarefas_e_emergencias.md), [22 — Mapa da árvore da rainha](01_Design/22_Mapa_da_arvore_da_rainha.md) |
| Desbloqueios e melhorias de salas | [17 — Mapa da árvore do formigueiro](01_Design/17_Mapa_da_arvore_do_formigueiro.md) | [18 — Cultivo habilidades e balanceamento](01_Design/18_Cultivo_habilidades_e_balanceamento.md), [19 — Berçário habilidades e balanceamento](01_Design/19_Bercario_habilidades_e_balanceamento.md), [20 — Depósito habilidades e balanceamento](01_Design/20_Deposito_habilidades_e_balanceamento.md), [21 — Resíduos habilidades e balanceamento](01_Design/21_Residuos_habilidades_e_balanceamento.md), [28 — Custos totais e regras de aplicação de melhorias](04_Data/28_Custos_totais_e_regras_de_aplicacao_de_melhorias.md) |
| Desbloqueios e melhorias das formigas | [22 — Mapa da árvore da rainha](01_Design/22_Mapa_da_arvore_da_rainha.md) | [23 — Rainha e operária habilidades e números](01_Design/23_Rainha_e_operaria_habilidades_e_numeros.md), [24 — Carregadora e cortadeira habilidades e números](01_Design/24_Carregadora_e_cortadeira_habilidades_e_numeros.md), [25 — Escavadora habilidades e números](01_Design/25_Escavadora_habilidades_e_numeros.md), [26 — Jardineira e cuidadora habilidades e números](01_Design/26_Jardineira_e_cuidadora_habilidades_e_numeros.md), [27 — Soldado habilidades e números](01_Design/27_Soldado_habilidades_e_numeros.md), [28 — Custos totais e regras de aplicação de melhorias](04_Data/28_Custos_totais_e_regras_de_aplicacao_de_melhorias.md) |
| Upgrades incrementais, níveis, marcos e dependências entre árvores | [39 — Melhorias incrementais e formigueiro de dependências](01_Design/39_Melhorias_incrementais_e_formigueiro_de_dependencias.md) | [Planilha de valores das melhorias](04_Data/39_Melhorias_incrementais_valores.xlsx), [17 — Mapa da árvore do formigueiro](01_Design/17_Mapa_da_arvore_do_formigueiro.md), [22 — Mapa da árvore da rainha](01_Design/22_Mapa_da_arvore_da_rainha.md), [28 — Custos totais e regras de aplicação de melhorias](04_Data/28_Custos_totais_e_regras_de_aplicacao_de_melhorias.md) |
| Chuva, água ou recursos da superfície | [30 — Chuva água e perigos ambientais](02_World_Art_UI/30_Chuva_agua_e_perigos_ambientais.md) | [31 — Jardim frutas folhas e visitantes](02_World_Art_UI/31_Jardim_frutas_folhas_e_visitantes.md), [05 — Terreno escavação e navegação](01_Design/05_Terreno_escavacao_e_navegacao.md) |
| Invasão e combate | [32 — Invasoras combate e consequências](01_Design/32_Invasoras_combate_e_consequencias.md) | [27 — Soldado habilidades e números](01_Design/27_Soldado_habilidades_e_numeros.md), [13 — Capacidade superlotação e morte da rainha](01_Design/13_Capacidade_superlotacao_e_morte_da_rainha.md) |
| Menus, HUD ou configurações | [33 — Menus fluxo de telas e configurações](02_World_Art_UI/33_Menus_fluxo_de_telas_e_configuracoes.md) | [34 — Interface durante a partida](02_World_Art_UI/34_Interface_durante_a_partida.md), [37 — Continuidade pausa e game over](01_Design/37_Continuidade_pausa_e_game_over.md) |
| Áudio e feedback visual | [35 — Áudio atmosfera e juice](02_World_Art_UI/35_Audio_atmosfera_e_juice.md) | [34 — Interface durante a partida](02_World_Art_UI/34_Interface_durante_a_partida.md) |
| Jam, demonstração ou escopo | [36 — Escopo completo e demonstração da jam](03_Production/36_Escopo_completo_e_demonstracao_da_jam.md) | [06 — Fluxo completo do início do jogo](01_Design/06_Fluxo_completo_do_inicio_do_jogo.md), [07 — Garantias do tutorial e primeiros objetivos](01_Design/07_Garantias_do_tutorial_e_primeiros_objetivos.md), [29 — Consistência econômica e cenários de teste](04_Data/29_Consistencia_economica_e_cenarios_de_teste.md) |
| Salvar, continuar, pausar ou perder | [37 — Continuidade pausa e game over](01_Design/37_Continuidade_pausa_e_game_over.md) | [12 — Ciclo de vida contadores e recuperação](01_Design/12_Ciclo_de_vida_contadores_e_recuperacao.md), [13 — Capacidade superlotação e morte da rainha](01_Design/13_Capacidade_superlotacao_e_morte_da_rainha.md) |
| Pesquisa de biologia e ambientação | [38 — Referências da ambientação](05_Narrative/38_Referencias_da_ambientacao.md) | [02 — Identidade mundo e papel do jogador](01_Design/02_Identidade_mundo_e_papel_do_jogador.md) |

## Arquivo por arquivo

Os caminhos dos links são relativos a `Docs/`. Cada arquivo aparece uma vez no catálogo abaixo.

### 01_Design/ — Design e sistemas

| Arquivo | Consultar para |
|---|---|
| [01_Apresentacao_e_uso_do_documento.md](01_Design/01_Apresentacao_e_uso_do_documento.md) | Natureza das definições; guia de leitura; vocabulário; regras definidas versus propostas e decisões em aberto. |
| [02_Identidade_mundo_e_papel_do_jogador.md](01_Design/02_Identidade_mundo_e_papel_do_jogador.md) | Conceito, papel do jogador, autonomia das formigas, corte lateral, referência natural e promessa da experiência. |
| [03_Pintura_de_ambientes_e_custo_de_expansao.md](01_Design/03_Pintura_de_ambientes_e_custo_de_expansao.md) | Pincéis por função; pintura permanente; sobreposição; acesso desconectado; saldo insuficiente; reabertura de salas. |
| [05_Terreno_escavacao_e_navegacao.md](01_Design/05_Terreno_escavacao_e_navegacao.md) | Fluxo de escavação e transporte de terra; navegação; terra firme, areia, pedra e crosta; soterramento. |
| [06_Fluxo_completo_do_inicio_do_jogo.md](01_Design/06_Fluxo_completo_do_inicio_do_jogo.md) | Sequência da nova partida: fundação, primeiras obras, cultivo, berçário e leitura da primeira sessão. |
| [07_Garantias_do_tutorial_e_primeiros_objetivos.md](01_Design/07_Garantias_do_tutorial_e_primeiros_objetivos.md) | Garantias do tutorial; primeiros objetivos; marcos em segundos; segurança e limites da correção automática. |
| [08_Fluxo_continuo_da_colonia.md](01_Design/08_Fluxo_continuo_da_colonia.md) | Loop contínuo da colônia; ritmo de conquistas; objetivos visíveis e escolhas recorrentes. |
| [09_Recursos_fungo_e_perecibilidade.md](01_Design/09_Recursos_fungo_e_perecibilidade.md) | Nutrientes, matéria prima, fungo, deterioração e ocupação; etapas de conversão do alimento. |
| [11_Reproducao_e_politica_da_rainha.md](01_Design/11_Reproducao_e_politica_da_rainha.md) | Reprodução automática; política da rainha; três modos; estados da reprodução e reservas. |
| [12_Ciclo_de_vida_contadores_e_recuperacao.md](01_Design/12_Ciclo_de_vida_contadores_e_recuperacao.md) | Ovos, larvas, pupas e adultas; contadores; recuperação sem produtivas; envelhecimento e seus limites. |
| [13_Capacidade_superlotacao_e_morte_da_rainha.md](01_Design/13_Capacidade_superlotacao_e_morte_da_rainha.md) | Capacidade por espaço útil; superlotação; área marcada versus habitável; condições de morte da rainha. |
| [14_Tipos_de_formiga_e_divisao_do_trabalho.md](01_Design/14_Tipos_de_formiga_e_divisao_do_trabalho.md) | Tipos de formiga; divisão do trabalho; cooperação e identidade dos papéis. |
| [16_Fluxos_de_tarefas_e_emergencias.md](01_Design/16_Fluxos_de_tarefas_e_emergencias.md) | Alocação automática de tarefas; emergências; resgate; atendimento e trabalhos impossíveis. |
| [17_Mapa_da_arvore_do_formigueiro.md](01_Design/17_Mapa_da_arvore_do_formigueiro.md) | Estrutura e ramos da árvore do formigueiro; compras pertencentes à árvore e suas fronteiras. |
| [18_Cultivo_habilidades_e_balanceamento.md](01_Design/18_Cultivo_habilidades_e_balanceamento.md) | Melhorias do cultivo; custos, efeitos e escolhas do ramo; preparo e bifurcação do cultivo. |
| [19_Bercario_habilidades_e_balanceamento.md](01_Design/19_Bercario_habilidades_e_balanceamento.md) | Melhorias do berçário; vagas, tempos e transferências das crias; representação no espaço. |
| [20_Deposito_habilidades_e_balanceamento.md](01_Design/20_Deposito_habilidades_e_balanceamento.md) | Melhorias do depósito; capacidade, cargas grandes e armazenamento de matéria prima; relação com preparo. |
| [21_Residuos_habilidades_e_balanceamento.md](01_Design/21_Residuos_habilidades_e_balanceamento.md) | Melhorias de resíduos; transporte, descarte, decomposição, ocupação e falta de espaço. |
| [22_Mapa_da_arvore_da_rainha.md](01_Design/22_Mapa_da_arvore_da_rainha.md) | Estrutura da árvore da rainha e critérios de desbloqueio das classes. |
| [23_Rainha_e_operaria_habilidades_e_numeros.md](01_Design/23_Rainha_e_operaria_habilidades_e_numeros.md) | Habilidades, custos e efeitos dos ramos da rainha e da operária comum. |
| [24_Carregadora_e_cortadeira_habilidades_e_numeros.md](01_Design/24_Carregadora_e_cortadeira_habilidades_e_numeros.md) | Habilidades de carregadora e cortadeira; cargas cooperativas e corte paralelo. |
| [25_Escavadora_habilidades_e_numeros.md](01_Design/25_Escavadora_habilidades_e_numeros.md) | Habilidades de escavadora; escavação de rocha; rachaduras, transporte e acesso a novas regiões. |
| [26_Jardineira_e_cuidadora_habilidades_e_numeros.md](01_Design/26_Jardineira_e_cuidadora_habilidades_e_numeros.md) | Habilidades de jardineira e cuidadora; distinção entre efeitos da classe e do ambiente. |
| [27_Soldado_habilidades_e_numeros.md](01_Design/27_Soldado_habilidades_e_numeros.md) | Habilidades de soldado; combate, defesa e contenção. |
| [32_Invasoras_combate_e_consequencias.md](01_Design/32_Invasoras_combate_e_consequencias.md) | Invasoras; percurso da invasão; combate, consequências e exposição da rainha. |
| [37_Continuidade_pausa_e_game_over.md](01_Design/37_Continuidade_pausa_e_game_over.md) | Save e continuar; pontos de retorno; pausa; game over; reinício; estados preservados e ausência de progresso offline definido. |
| [39_Melhorias_incrementais_e_formigueiro_de_dependencias.md](01_Design/39_Melhorias_incrementais_e_formigueiro_de_dependencias.md) | Proposta de 2026-10-19 (projeto, não transcrição): câmaras, galerias com nível, marcos, profundidade e túneis de dependência nas duas abas da tela Evolução. |

### 02_World_Art_UI/ — Mundo, arte, interface e áudio

| Arquivo | Consultar para |
|---|---|
| [30_Chuva_agua_e_perigos_ambientais.md](02_World_Art_UI/30_Chuva_agua_e_perigos_ambientais.md) | Chuva, água e perigos ambientais; comportamentos definidos e percepção do perigo. |
| [31_Jardim_frutas_folhas_e_visitantes.md](02_World_Art_UI/31_Jardim_frutas_folhas_e_visitantes.md) | Jardim, frutas, folhas e visitantes; fontes de alimento e frequência propostas. |
| [33_Menus_fluxo_de_telas_e_configuracoes.md](02_World_Art_UI/33_Menus_fluxo_de_telas_e_configuracoes.md) | Menus; fluxo e relações entre telas; opções de configurações. |
| [34_Interface_durante_a_partida.md](02_World_Art_UI/34_Interface_durante_a_partida.md) | HUD e informação permanente; painéis por seleção; estados das ações e leitura da colônia. |
| [35_Audio_atmosfera_e_juice.md](02_World_Art_UI/35_Audio_atmosfera_e_juice.md) | Áudio, atmosfera, feedback visual e juice; sensação e legibilidade. |

### 03_Production/ — Planejamento

| Arquivo | Consultar para |
|---|---|
| [36_Escopo_completo_e_demonstracao_da_jam.md](03_Production/36_Escopo_completo_e_demonstracao_da_jam.md) | Visão completa; proposta de recorte da jam; escopo da demonstração e medição de interesse. |

### 04_Data/ — Dados e balanceamento

| Arquivo | Consultar para |
|---|---|
| [04_Proposta_de_unidade_de_area_e_cobranca.md](04_Data/04_Proposta_de_unidade_de_area_e_cobranca.md) | Unidade B = 256 células; preço por área; custos dos pincéis; frações de nutrientes; reserva e orçamento inicial. |
| [10_Tabela_base_dos_recursos_e_tarefas_do_cultivo.md](04_Data/10_Tabela_base_dos_recursos_e_tarefas_do_cultivo.md) | Tabela base dos recursos e tarefas do cultivo; idade dos materiais e interrupção da produção. |
| [15_Valores_base_de_cada_formiga.md](04_Data/15_Valores_base_de_cada_formiga.md) | Atributos base por classe: vida, velocidade, carga, dano e alimento por cria; taxas por tarefa. |
| [28_Custos_totais_e_regras_de_aplicacao_de_melhorias.md](04_Data/28_Custos_totais_e_regras_de_aplicacao_de_melhorias.md) | Custos totais das árvores; aplicação das compras e continuidade dos efeitos das melhorias. |
| [29_Consistencia_economica_e_cenarios_de_teste.md](04_Data/29_Consistencia_economica_e_cenarios_de_teste.md) | Cenários de balanceamento: produção, gargalos, escavação com transporte, reprodução, mortalidade e orçamento. |
| [39_Melhorias_incrementais_valores.xlsx](04_Data/39_Melhorias_incrementais_valores.xlsx) | Custos, crescimento por tipo, efeito por nível, tetos, curvas de custo e resumo das melhorias do documento 39. |

### 05_Narrative/ — Referências da ambientação

| Arquivo | Consultar para |
|---|---|
| [38_Referencias_da_ambientacao.md](05_Narrative/38_Referencias_da_ambientacao.md) | Fontes de biologia e ambientação; saúvas, fundação e cultivo; limites da adaptação ao gameplay. |

## Outros pontos de entrada do projeto

| Necessidade | Entrada |
|---|---|
| Localizar scripts e sistemas implementados | [Mapa do código](../Assets/Project/Scripts/README.md) |
| Entender a implementação de um sistema | Guia `*Guide.md` ao lado do código; localize pelo mapa do código |
| Consultar bugs e dívidas | [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md) |
| Consultar estado, validações e pendências de produção | [PRODUCTION_STATUS.md](../Production/PRODUCTION_STATUS.md) |
| Consultar solicitações do desenvolvedor | [REQUESTS.md](../Production/REQUESTS.md) |
| Consultar convenções da documentação | [README.md](README.md) |

## Manutenção

Ao criar, mover, renomear ou mudar o assunto de um documento, atualize seu caminho e indicação de consulta neste mapa e no índice do README. Evite copiar regras ou valores para o mapa, para manter uma única referência por assunto.
