# 25 — Escavadora habilidades e números

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 25. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Habilidade e requisito | Custo em N | Base e resultado proposto |
| --- | --- | --- |
| E0  Escavadora<br>Exige Árvore da rainha | 180 | Comum: 16 células/s<br>Após compra: 32 células/s; carga 64 células de terra |
| E1  Remoção ampliada<br>Exige E0 | 200 | 32 células/s; carga 64<br>Após compra: 48 células/s; carga 96 (+50%) |
| E2  Quebra de rocha<br>Exige E1 | 400 | Pedra bloqueia escavação<br>Após compra: 4 células de rocha/s; carga 12 fragmentos |

E0 Escavadora. Especialista nas obras. A escavadora é produzida pela rainha e acelera escavação e desobstrução de areia.

E1 Remoção ampliada. Porções maiores de terra. Acelera remoção e aumenta a carga para o transporte de terra acompanhar o avanço.

E2 Quebra de rocha. Rachaduras e fragmentos. A capacidade é da escavadora aprimorada, não da sala ou de todas as formigas. Comuns, carregadoras e escavadoras sem E2 continuam sem atravessar rocha.

## Comportamento da obra em rocha

1. O jogador pinta um projeto que encontra pedra. A marcação paga pode existir antes do desbloqueio.

2. Sem E2 ou sem escavadora apta alcançável, a frente de pedra aguarda.

3. Com E2, uma escavadora trabalha na pedra, produzindo rachaduras progressivas.

4. A célula quebrada vira carga de fragmentos e segue para a superfície.

5. As demais formigas usam o caminho só depois que ele fica realmente livre.

Remover pedra exige mais tempo que remover terra. A taxa proposta após E2 é 4 células/s, contra 48 células/s de terra após E1. Não permitir que um efeito de velocidade de caminhada altere a taxa de quebra sem descrição.

Proposta: uma carga de fragmentos contém até 12 células de rocha. Fragmentos descartados podem usar a apresentação das pilhas, mas não se dissolvem pela regra de terra da chuva até que a equipe defina esse material. Não assumir silenciosamente que rocha removida endurece novamente uma sala.

O desbloqueio E2 custa 400 N, além da escavadora e da remoção ampliada. Alimento para uma nova escavadora custa 28 N. A compra deve mostrar uma região possível de alcançar ou o valor da capacidade, para não ser um bônus invisível.

## Pedra e abertura de novas regiões

Uma marca sobre pedra pode existir antes de haver uma escavadora apta. Enquanto isso, aquela frente não avança. A pessoa pode economizar para E2, produzir a especialista necessária ou pintar um desvio. Outras frentes alcançáveis continuam trabalhando.

E2 faz a pedra ser removida aos poucos por indivíduos capazes. O caminho só passa a ser utilizável quando realmente abre. A posição da classe junto da obra deve ficar compreensível para quem acompanha rachaduras, fragmentos e viagens à superfície.

O comportamento posterior dos fragmentos de rocha expostos à chuva ainda é uma decisão de material em aberto. A proposta mantém fragmentos distintos da terra que se desfaz, sem assumir que a chuva dissolve pedra.

---

[← 24 — Carregadora e cortadeira habilidades e números](../01_Design/24_Carregadora_e_cortadeira_habilidades_e_numeros.md) · [26 — Jardineira e cuidadora habilidades e números →](../01_Design/26_Jardineira_e_cuidadora_habilidades_e_numeros.md)
