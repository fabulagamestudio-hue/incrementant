# 12 — Ciclo de vida contadores e recuperação

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 12. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Etapa | Valor base proposto em segundos | Observação |
| --- | --- | --- |
| Intervalo de postura | 18 s | Q1: 12 s; Q2: até 3 ovos por ciclo |
| Ovo | 8 s | Câmara de postura |
| Larva | 14 s | B1: 10,5 s |
| Pupa | 12 s | B2: 9 s |
| Desenvolvimento após postura | 34 s | Com B1 e B2: 27,5 s; deslocamentos podem acrescentar tempo |
| Vida adulta | Sorteio uniforme entre 900 e 1.200 s | Mesma faixa para todas as classes produtivas e soldados |
| Rainha | Sem morte por idade nesta versão | Pode morrer por combate e água, causando game over |
| Recuperação especial | Uma leva de 5 comuns após 15 s | Sem custo ou berçário, se não restar produtiva e rainha estiver viva |

Os contadores distinguem rainha, adultas de cada tipo, ovos, larvas e pupas. Cada nascimento aumenta uma única vez a classe adulta e cada morte reduz uma única vez sua quantidade. A adulta não cresce fisicamente depois de emergir, salvo a representação explícita de uma melhoria. A evolução corporal natural pertence à fase larval.

O envelhecimento das iniciais fica suspenso até o primeiro novo nascimento após o berçário. Ao ativá-lo, distribuir tempos de vida individuais para evitar uma morte simultânea da fundação. A eficiência não cai por velhice nesta proposta. Pausa congela desenvolvimento, idade, água, combate e eventos.

## Recuperação não é garantia de saída

Definição da equipe: ao não restar nenhuma operária produtiva, a rainha cria automaticamente cinco comuns gratuitamente. Soldados sozinhos não sustentam a produção; comuns e especialistas de trabalho contam como produtivas. Proposta: capacidade temporariamente insuficiente não bloqueia essa leva emergencial, que pode gerar aviso de superlotação e precisa de regra de carência.

A recuperação é exceção ao custo e à dependência do berçário. Não concede nutrientes nem pintura gratuita. As cinco precisam remover material ou encontrar um trajeto. Se o terreno, falta de saldo para novo caminho ou bloqueio de pedra impedir saída, o jogo pode ficar em deadlock. Não prometer que cinco comuns sempre resolvem rocha ou água.

Disparar uma leva por episódio de ausência de produtivas, e não cinco continuamente. Se as cinco morrerem, um novo episódio pode ser elegível. A repetição exata e a interação com superlotação estão listadas para validação operacional.

## Limites da recuperação e do envelhecimento

A proteção contra idade pertence à população inicial, até a primeira nova geração. Não significa que novas adultas sejam eternas ou que o berçário precise ser comprado repetidamente. As faixas de longevidade propostas evitam perdas simultâneas da fundação.

A recuperação de cinco comuns é uma regra especial quando não há produtivas, inclusive se ainda restarem soldados. Ela não exige pagamento ou berçário. Não concede nutrientes para pintar, não quebra rocha com comuns e não abre um caminho impossível. Se não há saída, a sessão pode ficar travada, e retorno anterior ou reinício são as alternativas do jogador.

A forma de repetir a recuperação após novas perdas ainda precisa de decisão de design. A proposta atual cria uma leva por episódio de ausência de produtivas e permite novo episódio se essa leva também morrer. O período de carência da superlotação procura não punir imediatamente a própria tentativa de recuperação.

---

[← 11 — Reprodução e política da rainha](../01_Design/11_Reproducao_e_politica_da_rainha.md) · [13 — Capacidade superlotação e morte da rainha →](../01_Design/13_Capacidade_superlotacao_e_morte_da_rainha.md)
