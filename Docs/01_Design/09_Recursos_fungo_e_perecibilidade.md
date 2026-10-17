# 09 — Recursos fungo e perecibilidade

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 09. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

## Uma moeda e estoques físicos separados

Nutrientes são a única moeda. Folhas e frutos são matéria prima; fungo é o cultivo e pode conter unidades nutritivas processáveis; resíduos são manutenção. Nenhuma entrega concede N instantaneamente. Toda conversão alimentar passa pelo cultivo.

O depósito guarda material cru. A área de preparo, quando comprada, guarda material triturado. O cultivo aceita material cru mesmo sem preparo. Isso preserva o tutorial e evita dependência rígida de um ramo opcional. Frutos seguem o mesmo fluxo e carregam sua origem até a colheita, para garantir o rendimento dobrado.

## Modelo operacional proposto para separar as melhorias

Cada UM consumida pelo cultivo gera 1 UF processável, identificada como originada de folha ou fruto. A conversão aumenta o fungo visível até a capacidade local. A colheita de 1 UF gera 3 N se veio de folha e 6 N se veio de fruto. O modificador C3 aplica +20% aos dois: 3,6 e 7,2 N. Não pagar novamente na entrega ou no crescimento.

A capacidade de processamento por área limita a conversão de UM em UF. C1 aumenta essa taxa. J1 multiplica a taxa de crescimento somente na área atendida por jardineira. A colheita também tem capacidade finita. Se ela virar gargalo, o fungo processável se acumula e bloqueia crescimento adicional, em vez de gerar N duas vezes.

O desenho visual do jardim permanece depois de uma colheita: colher estruturas nutritivas não apaga toda a plantação. Fungos usados para transplante são uma pequena porção de instalação, sem permitir criar nutrientes pela repetição de plantar e colher.

## Deterioração e ocupação

Folhas e frutos são perecíveis. Material cru e preparado possui idade e perde quantidade gradualmente. Sobras deterioradas se tornam resíduo que ocupa seu local de origem até ser transportado. Restos do substrato e partes velhas do fungo também viram descarte. Isso cria atividade contínua no cultivo.

Proposta de comportamento: o alimento mais antigo sai primeiro e mantém sua origem e idade ao mudar de lugar. Preparar, mover ou armazenar uma carga não rejuvenesce o alimento. A decomposição automática que libera espaço ocorre na área de resíduos. O exterior não elimina magicamente uma carga descartada em lugar errado.

## Etapas do alimento até os nutrientes

Coleta: sai material de uma planta ou fruto e surge uma carga. Entrega: a carga entra no depósito ou no abastecimento do cultivo, sem pagar moeda. Preparo opcional: material vira porções trituradas. Cultivo: porções alimentam crescimento de fungo. Colheita: fungo nutritivo vira N. Deterioração: material que perde utilidade vira resíduo a remover.

A quantidade de folhas acumuladas representa autonomia de abastecimento, não um multiplicador ilimitado de produção. Um estoque enorme não transforma um cultivo pequeno em fábrica instantânea. O rendimento dobrado de frutos vale na saída do cultivo, inclusive quando folhas e frutos ficam misturados no mesmo estoque.

Por exemplo, processar 2 UM de folha e 1 UM de fruto produz 12 N antes de bônus, se todo o material completar o fluxo. Com Cultivo produtivo, a mesma quantidade rende 14,4 N. Se o fruto apodrece antes do processamento, a parte perdida gera resíduo e não concede seus nutrientes potenciais.

---

[← 08 — Fluxo contínuo da colônia](../01_Design/08_Fluxo_continuo_da_colonia.md) · [10 — Tabela base dos recursos e tarefas do cultivo →](../04_Data/10_Tabela_base_dos_recursos_e_tarefas_do_cultivo.md)
