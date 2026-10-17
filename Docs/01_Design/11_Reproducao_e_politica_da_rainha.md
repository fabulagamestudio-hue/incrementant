# 11 — Reprodução e política da rainha

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 11. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

1. Não iniciar ciclos antes de berçário funcional. A cinemática fornece o grupo inicial.

2. Consultar modo, tipos desbloqueados e necessidades. No modo específico, considerar só o tipo escolhido.

3. Verificar vaga no berçário e capacidade adulta livre após incluir todas as crias já reservadas.

4. Verificar saldo necessário para alimentar o tipo e preservar a reserva mínima configurada.

5. Debitar uma única vez o custo alimentar e reservar sua vaga adulta e de cria.

6. Aguardar a próxima postura e criar o ovo, ou lote de até três após Q2. Um lote parcial pode conter menos ovos por falta de condições.

7. Após 8 s como ovo, transferir a larva a um berçário alcançável com vaga. Larva leva 14 s e pupa 12 s antes dos bônus.

8. Ao nascer a adulta, remover o registro da pupa, liberar vaga de berçário e converter a reserva adulta em uma formiga viva do tipo escolhido.

## Três modos disponíveis sem compra

Automático escolhe pelas necessidades da colônia e pelos tipos já desbloqueados. Tipo específico direciona novos ovos a uma classe. Pausado impede novas reservas e posturas, mas mantém crias já iniciadas. Trocar modo nunca muda o tipo de ovo, larva ou pupa existente.

Proposta de prioridades do automático: assegurar duas comuns enquanto houver capacidade; cobrir obras com escavadora; atender cultivo e berçário com especialistas disponíveis; sustentar coleta; então manter soldados e composição-alvo. Evitar sorteio puro. Metas proporcionais sugeridas: comuns 20%, carregadoras 20%, cortadeiras 10%, escavadoras 15%, jardineiras 15%, cuidadoras 10% e soldados 10%. Redistribuir apenas entre tipos desbloqueados e tarefas existentes.

O painel informa modo, próximo tipo, progresso, reserva mínima, vagas e motivo de pausa. Reserva inicial sugerida: 30 N. Não assumir manutenção financeira permanente das adultas. O custo de criação cobre o ciclo completo, e saldo zero depois da reserva não impede o nascimento.

## Estados da reprodução vistos pelo jogador

Disponível: o próximo ovo tem tipo, alimento e espaço possíveis. Aguardando recursos: o saldo que ultrapassa a reserva mínima ainda não cobre a cria. Aguardando berçário: falta vaga funcional. Aguardando expansão: falta capacidade adulta futura. Pausada: escolha explícita do jogador. Cada estado deve apresentar o motivo sem sugerir que a rainha está com defeito.

No modo específico, escolher soldado mantém esse tipo para próximos ciclos enquanto as condições permitirem. O jogo não pode trocar silenciosamente para uma comum porque o soldado custa mais. No automático, as prioridades sugeridas podem trocar o próximo tipo antes da postura, preservando as crias que já existem.

A produção em lotes reserva cada ovo separadamente. Com espaço para dois e alimento para três, o lote máximo é dois. Com três vagas e alimento para um, é um. Com zero condição, aguarda. Trocar modo ou pausar depois da postura preserva o tipo e a alimentação dos ovos existentes.

---

[← 10 — Tabela base dos recursos e tarefas do cultivo](../04_Data/10_Tabela_base_dos_recursos_e_tarefas_do_cultivo.md) · [12 — Ciclo de vida contadores e recuperação →](../01_Design/12_Ciclo_de_vida_contadores_e_recuperacao.md)
