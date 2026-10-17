# 18 — Cultivo habilidades e balanceamento

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 18. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Habilidade e requisito | Custo em N | Base e resultado proposto |
| --- | --- | --- |
| C0  Cultivo inicial<br>Exige Inicial | 0 | Sem produção<br>Após compra: 0,25 UM/s por B; 3 N por UM de folha |
| C1  Canteiros organizados<br>Exige C0 | 90 | 0,25 UM/s por B<br>Após compra: 0,325 UM/s por B (+30%) |
| C2  Área de preparo<br>Exige C1 | 180 | Folhas diretas<br>Após compra: Prepara 2 UM em 2 s; reserva de 12 UM/B |
| C3  Cultivo produtivo<br>Exige C1 | 200 | 3 N/UM folha; 6 N/UM fruto<br>Após compra: 3,6 N/UM folha; 7,2 N/UM fruto (+20%) |
| C4  Cultivo integrado<br>Exige C2 e C3 | 400 | Distribuição básica alcançável<br>Após compra: Redistribui reserva preparada entre cultivos; alvo de 6 UM por B |

C0 Cultivo inicial. Fungo cresce e recebe folhas. O pincel tem desbloqueio gratuito. A área pintada custa 16 N/B e deve ser escavada e instalada para funcionar.

C1 Canteiros organizados. Canteiros definidos. A compra organiza o jardim em conjuntos reconhecíveis. Não basta apenas alterar o contador de produção.

C2 Área de preparo. Trituração e pilhas de material preparado. Libera uma extensão pintável de preparo a 14 N/B. O cultivo continua aceitando folhas sem preparo. Material preparado se conserva por 240 s na proposta.

C3 Cultivo produtivo. Mais estruturas nutritivas no fungo. O efeito é rendimento final: não aumentar simultaneamente a taxa como se fosse a mesma melhoria. Frutos mantêm o dobro das folhas.

C4 Cultivo integrado. Novas rotas entre preparo e cultivos. A integração busca matéria prima preparada entre áreas para abastecer cultivos com pouca reserva. A distribuição básica para um cultivo alcançável já funciona antes dessa compra.

## Escolhas do ramo

No terceiro nível, escolher C2 ajuda a absorver interrupções e organizar preparo. Escolher C3 ajuda quando a matéria prima é o recurso mais limitado. Após ambos, C4 cria rotas de redistribuição mais amplas. O jogador não é obrigado a comprar tudo no mesmo instante.

O benefício de reserva preparada pode se sobrepor ao depósito. A proposta inicial permite preparar fora do horário de demanda e abre o abastecimento integrado; não concede rendimento extra por triturar. Se nos testes for somente uma viagem a mais sem ganho compreensível, a equipe deve ajustar a regra, mantendo essa dúvida explicitada.

Jardineira J1 pertence à outra árvore e acelera crescimento de fungo na área atendida. C3 aumenta N por UF. Para uma área com C1 e J1, taxa = base × 1,3 × 1,3, respeitando folhas, fungo acumulado e trabalho de colheita. Não somar a mesma contribuição por cada jardineira presente.

## Experiência de compra e bifurcação do cultivo

C0 permite começar, C1 torna o trabalho mais rápido, C2 acrescenta preparação e reserva, C3 aumenta retorno por material e C4 organiza uma rede de abastecimento preparado. A pessoa pode reconhecer tanto uma mudança de aparência quanto uma mudança no fluxo.

Escolher C2 antes de C3 favorece preparar material quando há oportunidade de coleta. Escolher C3 antes de C2 favorece extrair mais do que já chega. Nenhum dos dois deve impedir o cultivo direto existente. C4 exige ambos e não torna obrigatório pintar uma sala nova se já houver espaço funcional suficiente para a atividade.

Ainda é necessário validar a vantagem da reserva preparada frente ao depósito de alimento cru e a diferença entre distribuição básica e integrada. Essas dúvidas pertencem ao próprio cultivo: o funcionamento básico de uma sala alcançável não pode depender de C4.

---

[← 17 — Mapa da árvore do formigueiro](../01_Design/17_Mapa_da_arvore_do_formigueiro.md) · [19 — Berçário habilidades e balanceamento →](../01_Design/19_Bercario_habilidades_e_balanceamento.md)
