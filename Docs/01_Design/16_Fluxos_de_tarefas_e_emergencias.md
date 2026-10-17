# 16 — Fluxos de tarefas e emergências

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 16. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

## Alocação automática proposta

1. Soldados disponíveis atendem invasoras próximas. Cuidadoras aprimoradas e comuns com resgate atendem emergências possíveis.

2. Atender berçário e cultivo com especialistas, usando comuns quando faltarem.

3. Reservar parte da equipe para corte e coleta, para a construção não consumir todo o abastecimento.

4. Escavadoras atendem obras e reabertura. Comuns ajudam quando não houver especialista suficiente.

5. Carregadoras e cortadeiras atendem coleta conforme capacidades, inclusive transportes cooperativos após L2.

6. Comuns sem prioridade removem resíduos e abastecem áreas. Especialista ociosa pode executar tarefa básica alcançável quando apropriado.

Proposta de mínimo: com três comuns no tutorial, uma pode alternar manutenção e retirada enquanto duas alternam coleta e obra. Não fixar uma trabalhadora para sempre numa tarefa impossível. Mostrar o motivo se não há acesso ou material. A prioridade de alimento deve impedir uma obra longa de paralisar a economia.

A rotina de múltiplas formigas no mesmo recurso e de múltiplas retiradas do depósito está disponível desde o funcionamento básico. Cada carga tem uma trabalhadora ou dupla responsável durante o transporte. Se o caminho fica impossível, a tarefa deixa de prender indefinidamente outras formigas e o material continua sujeito às condições do local.

## Resgate

Com O3, uma comum pode puxar uma adulta em poça rasa até borda alcançável. Proposta: vítima até 8 células da borda, operação de 3 s e velocidade de puxada de 12 células/s. A socorrista continua sujeita à água e pode morrer se não houver tempo. Não teletransportar nem garantir resgate em qualquer profundidade.

Com N2, cuidadoras levam larvas e pupas a outro berçário acessível, funcional e com vagas. Proposta: duas crias por viagem. O destino passa a considerar as crias em trânsito como vagas ocupadas, e o berçário de origem deixa de contá-las como residentes após a retirada. Sem destino válido, não criar um berçário gratuito nem imunidade. A ação pode ser impossível e deve ser informada.

## Atendimento e tarefas impossíveis

Uma trabalhadora pode alternar tarefas para manter o fluxo sem precisar de comandos individuais. A construção não deve consumir toda a equipe e deixar o alimento parado quando existem comuns suficientes. Uma especialista sem sua tarefa principal pode ajudar no básico, conforme o papel e sem adquirir capacidades exclusivas de outra classe.

Se falta caminho, recurso ou destino, a tarefa fica pendente e deve ter um motivo compreensível. A pessoa pode desenhar uma ligação ou liberar espaço. Não cabe à formiga inventar uma ponte ou mudar a função de uma sala para resolver o bloqueio.

O resgate de adulta é da comum aprimorada e limita-se a poça rasa com borda acessível. O resgate de larvas e pupas é da cuidadora aprimorada e exige outro berçário com vagas. Os dois podem falhar se as condições físicas não permitem a ação. Não há sucesso garantido por apenas comprar a habilidade.

---

[← 15 — Valores base de cada formiga](../04_Data/15_Valores_base_de_cada_formiga.md) · [17 — Mapa da árvore do formigueiro →](../01_Design/17_Mapa_da_arvore_do_formigueiro.md)
