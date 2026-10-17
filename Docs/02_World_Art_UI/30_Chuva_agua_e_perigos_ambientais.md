# 30 — Chuva água e perigos ambientais

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 30. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

## Comportamentos definidos

Chuva fraca reduz as pilhas de terra que as operárias retiram do formigueiro. A redução impede acúmulos externos enormes. Chuva forte também reduz parte das pilhas, mas cria crosta rígida nas bordas, tornando essa estrutura menos propensa a desmoronar. Esses comportamentos definem como o terreno muda no jogo, sem exigir fidelidade à erosão real.

Água pode formar poças. Permanecer tempo demais dentro dela mata uma formiga, inclusive a rainha se for alcançada. O jardim e o ninho devem mostrar visualmente acúmulo, entrada e escoamento de água. Não existem pontes compráveis nesta concepção.

| Evento ou efeito | Valor proposto | Resultado |
| --- | --- | --- |
| Chuva fraca | Duração 25 s; intervalo elegível 180 a 300 s | Remove 1%/s da terra exposta de pilhas; mínimo de 1 célula por área ativa quando houver material |
| Chuva forte | Duração 35 s; intervalo elegível 300 a 480 s | Remove 0,5%/s da pilha exposta e endurece bordas |
| Crosta | Camada de até 2 células expostas | Converte em solo firme; respeita colisão |
| Poças | Chuva fraca: 0,5 célula de água/s; forte: 2 células/s por 256 células horizontais expostas | Primeira taxa normalizada para a simulação; medir acúmulo e escoamento reais |
| Submersão letal | 8 s de exposição acumulada | Morte; fora da água recupera 2 s de tolerância por segundo |
| Aviso de risco | Após 4 s de exposição | Ícone e efeito de perigo antes da morte |
| Proteção de tutorial | Até primeiro nascimento e ao menos 180 s | Sem chuva que bloqueie ou mate a população inicial |

Percentual de remoção aplica uma vez por segundo ao material exposto da pilha, não por gota visual. Gotas são apresentação; não multiplicar remoção conforme quantidade de efeitos visuais. A crosta torna a pilha mais estável, mas pode mudar acessos. A areia dentro de uma marcação continua respeitando a física local.

Aumentar umidade e recuperar plantas pela chuva foi discutido como interação do jardim. Esta proposta usa recuperação de vegetação acelerada em 25% durante chuva fraca, sem criar uma moeda de umidade. Retenção e drenagem como upgrades abstratos não integram a árvore aprovada. Invasões são um evento distinto, embora a fala tenha mencionado sua chegada na chuva; a vinculação obrigatória ao clima ainda precisa de confirmação.

## Água como perigo percebido

A chuva não elimina uma formiga instantaneamente ao tocar nela. O risco letal vem do tempo que ela fica submersa numa poça. Um sinal antes do limite permite perceber a ameaça e relacionar a morte à situação no mundo. Sair da água recupera tolerância pela taxa proposta.

Chuva fraca e forte mantêm seus efeitos diferentes nas pilhas externas. Esses efeitos podem mudar caminhos sem mudar a função de um ambiente pintado. Quando a água impede usar uma área, a capacidade pode cair e iniciar superlotação. Os processos voltam a funcionar se o espaço e o acesso forem recuperados.

A versão atual não oferece construção de pontes nem uma melhoria abstrata de drenagem. A ligação obrigatória entre invasão e chuva permanece em aberto: as invasoras já têm comportamento definido, mas a equipe ainda precisa fechar se chegam somente durante chuva ou também como evento independente.

---

[← 29 — Consistência econômica e cenários de teste](../04_Data/29_Consistencia_economica_e_cenarios_de_teste.md) · [31 — Jardim frutas folhas e visitantes →](../02_World_Art_UI/31_Jardim_frutas_folhas_e_visitantes.md)
