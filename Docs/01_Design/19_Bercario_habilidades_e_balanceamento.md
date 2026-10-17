# 19 — Berçário habilidades e balanceamento

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 19. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Habilidade e requisito | Custo em N | Base e resultado proposto |
| --- | --- | --- |
| B0  Berçário inicial<br>Exige C0 concluído | 50 | Sem novas crias<br>Após compra: 2 vagas de cria por B |
| B1  Leitos de criação<br>Exige B0 | 110 | Larva: 14 s<br>Após compra: Larva: 10,5 s (−25%) |
| B2  Área de maturação<br>Exige B0 | 110 | Pupa: 12 s<br>Após compra: Pupa: 9 s (−25%) |
| B3  Berçário organizado<br>Exige B1 e B2 | 260 | 2 vagas/B<br>Após compra: 3 vagas/B (+50%) |

B0 Berçário inicial. Larvas e pupas cuidadas. Sua construção habilita a reprodução automática, desde que também existam alimento e capacidade adulta.

B1 Leitos de criação. Leitos de alimentação. As larvas ocupam leitos visíveis com visitas de alimentação. O bônus reduz a duração larval, sem aumentar postura.

B2 Área de maturação. Locais próprios para pupas. Locais específicos para pupas tornam a maturação identificável. A emergência de uma adulta é um marco visual.

B3 Berçário organizado. Leitos e pupas organizados de forma compacta. Leitos e pupas ganham disposição compacta. Mais vagas de crias não alteram a capacidade adulta calculada pela área total.

## Vagas e tempos

Um berçário funcional de 2 B atende quatro crias, ou seis depois de B3. A rainha verifica vagas antes de iniciar cada ovo, mesmo que ele ainda vá permanecer na postura por 8 s. Essa reserva previne nascerem mais larvas que o berçário pode atender.

Após B1 e B2, o tempo nominal de ovo até adulta é 8 + 10,5 + 9 = 27,5 s, mais deslocamentos e interrupções de atendimento. Atrasos de caminho congelam a etapa de transferência quando a cria não consegue chegar ao destino; não apagar a cria nem forçar passagem através do solo.

A cuidadora altera a logística e as prioridades de cuidado, incluindo alimentar várias larvas por viagem e resgatar. Ela não aplica outro bônus escondido de duração larval que duplique B1. Uma falta momentânea de cuidadora é atendida por comuns; uma falta de qualquer acesso precisa de correção física.

## Experiência das crias no espaço

A pessoa vê ovos na postura e larvas e pupas no berçário. Os leitos revelam atendimento larval, enquanto locais de maturação revelam a preparação da adulta. Uma compra muda um estágio identificável, e não apenas o contador final de população.

B3 aumenta vagas por área para permitir mais desenvolvimento simultâneo. Isso não cria uma adulta imediata nem aumenta por si só a capacidade do formigueiro. Um berçário espaçoso pode ficar vazio quando a reserva de nutrientes está alta por escolha do jogador ou quando a população adulta chegou ao limite.

Sem acesso ao berçário, uma transferência aguarda e o painel explica a interrupção. Uma sala soterrada continua marcada como berçário e pode ser recuperada. Os cuidados da comum mantêm o começo possível antes da compra de cuidadoras.

---

[← 18 — Cultivo habilidades e balanceamento](../01_Design/18_Cultivo_habilidades_e_balanceamento.md) · [20 — Depósito habilidades e balanceamento →](../01_Design/20_Deposito_habilidades_e_balanceamento.md)
