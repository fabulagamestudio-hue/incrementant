# 06 — Fluxo completo do início do jogo

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 06. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

1. Exibir a splash da Fábula Game Studio e abrir o menu principal.

2. Ao escolher Iniciar, criar uma nova colônia e reproduzir a cinemática de fundação.

3. Mostrar a tanajura pousando, perdendo as asas, cavando o acesso e a câmara de postura. A passagem de tempo apresenta as primeiras operárias. A abertura pode ser quase um vídeo; não exige simular toda essa fundação.

4. Entregar ao jogador a rainha, três operárias comuns na proposta base, o pincel genérico, C0 liberado e 100 N. Testar três versus cinco operárias.

5. Pedir a primeira pintura de cultivo. Recomendar 4 B e um acesso curto, mostrar preço e aceitar a confirmação. A pintura é paga com a reserva inicial.

6. Operárias escavam a área e levam material para a superfície. A região de tutorial oferece terreno e folhas acessíveis.

7. Instalar o cultivo usando o fragmento de fungo trazido pela rainha. As comuns podem executar a instalação sem jardineira.

8. Dividir a equipe entre coleta e cuidados. Folhas chegam ao estoque do cultivo e alimentam a produção de fungo e de nutrientes.

9. Mostrar saldo, taxa, estoques e objetivo do berçário. A rainha ainda não inicia novas crias.

10. Acumular nutrientes, comprar B0 e pintar um berçário de referência de 2 B, com acesso. Comuns alternam a obra com a manutenção da produção.

11. Ao concluir o berçário e haver alimento e vagas, ativar reprodução automática de operárias comuns, respeitando a reserva mínima de N.

12. Acompanhar ovo na postura, transferência para o berçário, larva, pupa e nascimento. Só a adulta incrementa o contador da classe.

13. Após a primeira nova geração, iniciar o envelhecimento distribuído das operárias iniciais. Liberar gradualmente as ameaças conforme a regra proposta de proteção.

14. Apresentar melhorias e especialistas. O jogador escolhe entre produção, armazenamento, população e espaço. O funcionamento da colônia torna as próximas compras compreensíveis.

## Leitura da primeira sessão

Até o cultivo, o objetivo é entender pintura, escavação e transporte. Do cultivo ao berçário, o objetivo é entender de onde vêm os nutrientes e por que vale esperar por uma nova função. O primeiro nascimento demonstra que a colônia pode se renovar automaticamente.

O berçário é o primeiro objetivo de progressão, mas a pessoa pode investir em outra compra disponível. Nesse caso, a coleta e o cultivo mantêm a possibilidade de juntar novamente. O tutorial apresenta um caminho recomendado, não substitui as escolhas por uma sequência de tarefas manuais obrigatórias.

É uma decisão de apresentação ainda aberta permitir pular a cinemática. Se houver essa opção, ela deve entregar o mesmo estado inicial da abertura completa. A existência de três ou cinco comuns na fundação é uma hipótese numérica a testar; ambas preservam a lógica do começo.

---

[← 05 — Terreno escavação e navegação](../01_Design/05_Terreno_escavacao_e_navegacao.md) · [07 — Garantias do tutorial e primeiros objetivos →](../01_Design/07_Garantias_do_tutorial_e_primeiros_objetivos.md)
