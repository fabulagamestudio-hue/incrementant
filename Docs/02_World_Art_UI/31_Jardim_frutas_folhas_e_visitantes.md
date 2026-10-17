# 31 — Jardim frutas folhas e visitantes

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 31. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

## Ambiente vivo

A superfície contém gramíneas, plantas pequenas e flores com formas diferentes. Evitar árvores grandes pelo tamanho estrutural. Plantas perdem partes ao serem coletadas e podem se recuperar. Uma trilha muito usada fica com vegetação reduzida, e as formigas podem buscar outra fonte. As pilhas de terra afetam percursos e a chuva modifica o relevo.

Cogumelos podem surgir em períodos úmidos como elementos visuais, sem entrar no alimento definido. Visitantes podem atravessar o jardim, pousar nas flores e se abrigar. Alguns interferem: lagarta disputando planta é uma possibilidade ainda não detalhada; formiga invasora é a ameaça definida. Nem todo inseto precisa virar combate.

## Proposta de fontes e frequência

| Fonte ou evento | Configuração base proposta | Efeito |
| --- | --- | --- |
| Planta próxima | Reserva 20 UM; recupera 0,1 UM/s | Fonte regular; não deixar todas as fontes iniciais inacessíveis |
| Planta alternativa | 30 UM; percurso um pouco maior | Evita dependência permanente de uma única planta |
| Folha caída | 30 UM; dura até 60 s se não coletada | Aparece próxima; oportunidade de reduzir viagem |
| Intervalo de folha caída | 120 a 180 s após proteção | Varia a coleta sem clique de resgate obrigatório |
| Fruto caído | 40 UM em porções; dura até 90 s no jardim | Depois de coletado mantém idade para deterioração de 360 s |
| Intervalo de fruto | 240 a 360 s após início da produção | Recurso menos frequente com rendimento dobrado |
| Rendimento do fruto | 6 N/UM base, contra 3 N de folha | Somente após cultivo; C3 mantém fator 2 |

A duração no jardim limita a oportunidade visual de um evento. Transferência ao estoque não zera a idade. Definir idade do fruto no momento de surgir, preservar na coleta e limitar o período externo ao máximo da tabela. Não remover instantaneamente a carga de uma operária quando a oportunidade externa termina.

A espécie exata das flores, quantidade de visitantes e alcance do jardim continuam em aberto. Inverno e dia/noite ficaram fora da versão atual. Fruto não concede bônus de moeda na coleta: seu benefício é uma matéria prima que rende exatamente o dobro no cultivo.

## Oportunidades sem moedas adicionais

Uma folha caída muda a distância e a disponibilidade de coleta. Um fruto muda o rendimento da matéria prima. Nenhum dos dois aumenta N no momento da entrega. O jogador acompanha trilhas se reorganizando e depois percebe o resultado no cultivo.

A produção das plantas deve permitir voltar a coletar sem esgotar definitivamente toda a superfície por uma escolha normal. Durante a proteção inicial, uma fonte alcançável é garantida. Depois, relevo, chuva e visitantes podem tornar outra rota mais conveniente.

O tipo exato de flor e a relação de espécies decorativas ainda estão abertos. Cogumelos podem aparecer como ambientação, animais mortos e cogumelos não alimentam o cultivo definido. Dia/noite e inverno não integram o conjunto atual de mecânicas.

---

[← 30 — Chuva água e perigos ambientais](../02_World_Art_UI/30_Chuva_agua_e_perigos_ambientais.md) · [32 — Invasoras combate e consequências →](../01_Design/32_Invasoras_combate_e_consequencias.md)
