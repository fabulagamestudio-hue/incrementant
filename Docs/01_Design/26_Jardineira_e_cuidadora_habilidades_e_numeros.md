# 26 — Jardineira e cuidadora habilidades e números

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 26. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Habilidade e requisito | Custo em N | Base e resultado proposto |
| --- | --- | --- |
| J0  Jardineira<br>Exige Árvore da rainha | 180 | Comum: atende 4 B de cultivo<br>Após compra: Atende 6 B; manejo especializado |
| J1  Produção otimizada de fungo<br>Exige J0 | 240 | Crescimento a 1 vez a taxa do cultivo<br>Após compra: Crescimento ×1,3 na área atendida |
| J2  Transplante de fungo<br>Exige J1 | 300 | Instalação comum: 8 s por B<br>Após compra: Instalação: 4 s por B; carga de fungo visível |
| N0  Cuidadora<br>Exige Árvore da rainha | 200 | Comum: atende 4 crias em 4 s<br>Após compra: Atende 4 crias em 3 s |
| N1  Alimentação em lote<br>Exige N0 | 240 | 4 crias por viagem<br>Após compra: 8 crias por viagem de 3 s |
| N2  Resgate de crias<br>Exige N1 | 320 | Sem evacuação especializada<br>Após compra: Transporta 2 crias por viagem; prioridade de emergência |

J0 Jardineira. Jardineiras no fungo. Mantém o cultivo e atende área maior que uma comum, liberando generalistas para outras atividades.

J1 Produção otimizada de fungo. Fungo ocupa o espaço mais depressa. Produz mais fungo por tempo na área disponível, sem ultrapassar a capacidade de folhas, espaço e colheita. Não aumenta o valor por unidade, que pertence a C3.

J2 Transplante de fungo. Transporte de fragmentos de fungo. Transporta uma porção visível de fungo para iniciar outra área. A comum ainda consegue fazer a instalação básica do tutorial.

N0 Cuidadora. Cuidado dedicado no berçário. Prioriza alimentação e transporte de crias. Não torna o berçário inoperante quando a classe ainda não existe.

N1 Alimentação em lote. Carga e sequência de atendimentos. Uma viagem atende oito crias em vez de quatro. É logística visível, não segundo desconto de duração de larva.

N2 Resgate de crias. Retirada para outro berçário. Exige outro berçário acessível e com vagas; emergência não cria espaço automaticamente.

Alimento por nova jardineira: 26 N. Por cuidadora: 26 N. O volume de fungo visível é uma consequência do trabalho e deve ser limitado pela área pintada. Uma compra de J1 melhora jardineiras existentes e futuras; não transforma comuns em jardineiras.

A sobreposição entre competência da cuidadora e B1/B2 deve ser controlada: estruturas alteram tempos, cuidadora altera disponibilidade de atendimento e deslocamentos. Uma cria pode esperar cuidado se ninguém alcançar o berçário, sem consumir outra vez o valor monetário já reservado.

## Fronteiras de efeito entre classe e ambiente

J1 aumenta fungo produzido no espaço atendido; C3 aumenta o rendimento de nutrientes por material. J2 acelera instalar outra área; a comum continua capaz de instalar o primeiro jardim. Essas fronteiras impedem que uma mesma compra receba dois nomes e o mesmo efeito.

N1 permite alimentar mais crias por viagem. B1 e B2 encurtam os tempos dos estágios. N2 muda uma reação a perigo, transferindo crias, e exige destino de verdade. Uma cuidadora sem destino válido não garante salvar o berçário alagado.

---

[← 25 — Escavadora habilidades e números](../01_Design/25_Escavadora_habilidades_e_numeros.md) · [27 — Soldado habilidades e números →](../01_Design/27_Soldado_habilidades_e_numeros.md)
