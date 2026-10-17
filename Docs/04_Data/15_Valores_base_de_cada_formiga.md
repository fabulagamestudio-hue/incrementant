# 15 — Valores base de cada formiga

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 15. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Tipo | Alimento por cria em N | Vida | Velocidade células/s | Carga alimentar UM | Dano/s |
| --- | --- | --- | --- | --- | --- |
| Rainha | 0 | 80 | 0 | 0 | 0 |
| Operária comum | 18 | 6 | 24 | 2 | 1 |
| Carregadora | 24 | 8 | 28 | 4 | 1 |
| Cortadeira | 24 | 6 | 26 | 2 | 1 |
| Escavadora | 28 | 8 | 24 | 4 | 1 |
| Jardineira | 26 | 6 | 22 | 2 | 1 |
| Cuidadora | 26 | 6 | 26 | 2 | 1 |
| Soldado | 34 | 18 | 24 | 0 | 3 |

Vida significa pontos de dano até a morte e não duração de vida. Todas as adultas, exceto a rainha, vivem entre 900 e 1.200 s nesta proposta. Velocidades valem em caminho livre, sem aceleração de tarefa. Uma velocidade de emergência de 8 células/s para a rainha é uma proposta de design, condicionada à decisão sobre sua realocação. Ela não patrulha todo o formigueiro.

O dano básico das produtivas representa autodefesa de curta distância. Elas não devem abandonar a economia para perseguir invasoras. Propõe-se tentar recuar quando possível e atacar ao entrar em contato. Soldados priorizam combate. Esse comportamento de autodefesa é uma proposta operacional, com o resultado visual a validar.

## Taxas específicas e valores adicionais

| Tarefa | Comum | Especialista base |
| --- | --- | --- |
| Cortar folha | 2 UM em 2 s | Cortadeira: 2 UM em 1 s |
| Remover terra | 16 células/s; carga 32 células | Escavadora: 32 células/s; carga 64 células |
| Quebrar rocha | Não executa | Escavadora com E2: 4 células/s |
| Instalar fungo | 8 s por B | Jardineira J2: 4 s por B |
| Atender cultivo | 4 B por responsável | Jardineira: 6 B |
| Alimentar crias | 4 crias por viagem de 4 s | Cuidadora: 4 crias em 3 s |
| Transportar resíduo | 2 UR por viagem | Carregadora: 4 UR por viagem |
| Combater invasora | Dano 1/s; vida conforme tipo | Soldado: dano 3/s e vida 18 |

Cada melhoria altera somente a classe ou ambiente descrito. Bônus de O1 não melhora carregadoras, e L3 não acelera jardineiras. O custo alimentar por tipo é estável nesta primeira configuração: não cresce exponencialmente a cada nascimento, pois já existe reposição por idade e limitação espacial.

## Como ler os atributos das formigas

Vida em pontos mede resistência a dano. Longevidade em segundos mede envelhecimento. Velocidade altera somente deslocamento. Carga altera o material levado por viagem. Uma taxa de ação mede corte, escavação ou atendimento. Essas medidas têm efeitos próprios e não devem ser interpretadas como um único bônus geral de eficiência.

Uma melhoria da classe afeta suas adultas atuais e as futuras, mantendo o papel do indivíduo. Uma melhoria de ambiente vale para o trabalho naquele ambiente. As propostas de autodefesa e regeneração fora de combate detalham como as perdas podem funcionar, mas não foram apresentadas como habilidades extras compráveis.

---

[← 14 — Tipos de formiga e divisão do trabalho](../01_Design/14_Tipos_de_formiga_e_divisao_do_trabalho.md) · [16 — Fluxos de tarefas e emergências →](../01_Design/16_Fluxos_de_tarefas_e_emergencias.md)
