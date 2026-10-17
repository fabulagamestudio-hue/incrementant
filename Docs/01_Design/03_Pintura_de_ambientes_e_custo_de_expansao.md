# 03 — Pintura de ambientes e custo de expansão

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 03. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

## O pincel determina a função

O pincel comum de escavação marca túneis genéricos e novas entradas. Um pincel especial marca sempre o ambiente escolhido: cultivo, berçário, depósito, resíduos ou a área de preparo quando desbloqueada. A forma não muda a classificação. Uma faixa comprida pintada como berçário é berçário; o jogo não tenta reinterpretá-la como túnel.

O jogador pode desenhar o acesso com o pincel comum ou com o próprio pincel de ambiente. Usar o pincel especial em um acesso também paga o preço da sala e cria área com essa função. A interface precisa explicar isso por cor, nome e orçamento da pintura.

## Marcação permanente

Uma área já pintada não aceita nova pintura, mudança de função ou apagamento. A restrição vale antes e depois da obra. A função da área permanece mesmo se o material físico mudar: se terra cair dentro, a sala continua sendo a mesma sala, apenas soterrada. Remover a terra recupera o espaço, sem apagar ou recomprar a função.

Não existe apagar ambiente. O jogador pode ampliar pintando terreno ainda não marcado. Uma operação só cobra pelas células novas aceitas. Sobreposição com marcações existentes não muda função e não deve gerar novo débito.

## Acesso é responsabilidade do projeto

É permitido pintar salas sem conexão pronta e abrir mais de uma entrada pela superfície. Não bloquear a compra para garantir automaticamente uma rota. Formigas escavam somente frentes que alcançam. Uma obra inacessível aguarda um caminho; o jogador pode marcar outra ligação. Erros físicos sem solução podem produzir um deadlock.

Não há posicionamento de salas de formato pronto nem conexão obrigatoriamente desenhada pelo jogo. Custos seguem toda a pintura: o túnel comum custa muito pouco; salas custam bastante mais e exigem economia. O desbloqueio é separado do custo por área.

## Pintura e situações especiais

Durante a marcação, a pessoa reconhece nome, cor, função e custo do pincel. Confirmar cria um compromisso permanente. A área pode permanecer sem acesso por escolha ou erro, e o jogador resolve isso pintando ligações adicionais ou uma nova entrada. O jogo não promete corrigir o projeto automaticamente.

Um traço que mistura terreno já marcado e terreno novo conserva o trecho antigo e considera apenas o trecho novo para função e cobrança. Um túnel antigo não pode virar cultivo por passar sobre ele com outra cor. Uma sala antiga não desaparece ao receber terra ou água.

A falta de ligação interrompe trabalho, mas não cancela compra. A falta de saldo impede confirmar uma nova pintura paga. Continua em aberto se o pincel deve limitar a prévia ao orçamento ou recusar todo o novo trecho quando o valor passa do saldo. Nenhuma dessas opções permite saldo negativo ou alteração de área anterior.

---

[← 02 — Identidade mundo e papel do jogador](../01_Design/02_Identidade_mundo_e_papel_do_jogador.md) · [04 — Proposta de unidade de área e cobrança →](../04_Data/04_Proposta_de_unidade_de_area_e_cobranca.md)
