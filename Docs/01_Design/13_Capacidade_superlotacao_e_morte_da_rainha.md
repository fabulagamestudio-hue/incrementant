# 13 — Capacidade superlotação e morte da rainha

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 13. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

## Capacidade depende de espaço útil

Contar células internas já escavadas e acessíveis, pertencentes a túneis ou ambientes pintados. Superfície, terreno não marcado e projetos ainda preenchidos ficam fora. Uma sala soterrada permanece registrada, mas perde temporariamente área útil. Água que impede utilização também pode retirar área da capacidade conforme a proposta abaixo.

Proposta: capacidade adulta = menor valor entre piso de área útil/256 e 200. O limite adicional de 200 é uma proposta de população máxima, ainda sujeita à experiência de circulação da colônia. A rainha fica fora dessa contagem. Adultas produtivas e soldados usam uma vaga cada. Crias contam pela vaga reservada antes do nascimento.

Para espaços com entradas múltiplas, propõe-se considerar componentes alcançáveis por uma trabalhadora a partir da colônia ou de suas entradas ativas. Regiões fisicamente isoladas sem acesso não somam capacidade apenas por estarem pintadas. A capacidade precisa corresponder ao espaço que as formigas realmente conseguem usar.

## No limite e acima do limite

Atingir a capacidade pausa novos ciclos da rainha. Não causa briga no instante do nascimento. A crise ocorre quando perda de área deixa adultas acima do limite. Primeiro avisar e reduzir eficiência; se persistir, conflitos internos e perdas representam uma regra fictícia do jogo, não uma explicação científica do comportamento das saúvas.

| Condição proposta | Consequência |
| --- | --- |
| População com reservas no limite | Pausar novos ciclos; mostrar necessidade de ampliar |
| Adultas acima da capacidade por 20 s | Aviso destacado e eficiência de trabalho ×0,8 |
| Excesso persistente por 60 s | Conflitos visuais; perda de 1 adulta a cada 10 s enquanto durar excesso |
| Capacidade recuperada | Encerrar conflitos e penalidade após 5 s de estabilidade |
| Leva emergencial de recuperação | Carência proposta de 60 s antes de conflitos |
| Rainha com vida zero | Parar a sessão e exibir game over |

A regra provisória de perdas exclui a rainha dos conflitos por superlotação para não transformar uma reposição em derrota automática. Reservas de crias acima da nova capacidade pausam novos pedidos; crias já alimentadas continuam e podem aumentar o excesso ao nascer. Esse caso precisa ser testado com soterramento e berçários danificados.

Se a rainha morrer por ataque ou água, o jogo termina. Game over oferece retomar um estado anterior salvo ou reiniciar todo o formigueiro. Rainha protegida para sempre e invulnerabilidade geral não fazem parte da definição atual.

## Espaço marcado não é espaço habitável

Comprar uma grande região no solo não permite produzir instantaneamente uma população grande: a área precisa estar escavada e utilizável. Uma câmara cheia de terra mantém seu nome e sua função planejada, mas o espaço soterrado deixa de ajudar naquele momento.

Atingir o limite normal apenas segura novos ciclos. Perder área quando já há adultas vivas pode criar excesso e começar a crise gradual. A diferença deve ser visível: “sem vagas para crescer” e “população acima do espaço disponível” são situações distintas.

Novas crias já alimentadas não desaparecem porque o teto caiu depois de uma chuva. Elas continuam sujeitas às condições físicas e, se nascerem, podem aumentar o excesso. Expandir uma passagem, remover terra ou recuperar acesso pode aliviar a crise sem comprar outra habilidade.

As decisões ainda abertas aqui são o que conta como acesso em áreas com várias entradas e o tratamento de regiões parcialmente alagadas. A proposta usa espaço efetivamente utilizável e mantém o teto adicional de 200 apenas como configuração inicial.

---

[← 12 — Ciclo de vida contadores e recuperação](../01_Design/12_Ciclo_de_vida_contadores_e_recuperacao.md) · [14 — Tipos de formiga e divisão do trabalho →](../01_Design/14_Tipos_de_formiga_e_divisao_do_trabalho.md)
