# 23 — Rainha e operária habilidades e números

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 23. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Habilidade e requisito | Custo em N | Base e resultado proposto |
| --- | --- | --- |
| Q0  Postura inicial<br>Exige Berçário funcional | 0 | Sem ciclos antes do berçário<br>Após compra: 1 ovo a cada 18 s |
| Q1  Postura acelerada<br>Exige Q0 | 150 | 18 s por ciclo<br>Após compra: 12 s por ciclo (−33,3%) |
| Q2  Postura em lotes<br>Exige Q1 | 450 | 1 ovo por ciclo<br>Após compra: Até 3 ovos por ciclo de 12 s |
| O0  Operária comum<br>Exige Inicial | 0 | Generalista<br>Após compra: Mantém todas as tarefas básicas |
| O1  Carga ampliada<br>Exige O0 | 100 | Carga de 2 UM ou 32 células de terra<br>Após compra: Carga de 3 UM ou 48 células (+50%) |
| O2  Deslocamento rápido<br>Exige O0 | 100 | 24 células/s<br>Após compra: 30 células/s (+25%) |
| O3  Retirada de companheiras<br>Exige O0 | 240 | Sem puxar adultas<br>Após compra: Resgata adulta até 8 células da borda; puxada de 3 s |

Q0 Postura inicial. Postura e transporte de ovos. Só começa após o berçário. Modos automático, específico e pausado são básicos e não exigem compra.

Q1 Postura acelerada. Mais posturas a cada 60 s. A produção visual de ovos fica mais frequente. O tempo de desenvolvimento pertence ao berçário.

Q2 Postura em lotes. Pequenos lotes visíveis. Cada lote tenta até três ovos e paga cada tipo separadamente. Se houver alimento ou vagas apenas para um ou dois, produzir lote parcial.

O0 Operária comum. Equipe inicial. Três comuns compõem a proposta inicial. Testar cinco para comparar ritmo e leitura de tarefas.

O1 Carga ampliada. Carga maior nas mandíbulas. A carga maior serve a folhas, frutos, terra e resíduos, conforme a conversão indicada. A generalista não ultrapassa a especialista em sua função.

O2 Deslocamento rápido. Viagens mais rápidas. Acelera deslocamento, não corte, escavação ou alimentação. O efeito aparece nas viagens.

O3 Retirada de companheiras. Operária puxa companheira de poça rasa. Resgate de adultas permanece arriscado. Não concede respiração aquática nem garantia de salvar vítima profunda.

Cada melhoria de operária é independente: não exigir O1 para comprar O2 ou O3. Para custos de criação, usar 18 N por comum na configuração base. O desbloqueio da classe comum é inicial e não aparece como gasto repetido.

## Decisões e resultados dos ramos iniciais

Q1 aumenta frequência de postura, enquanto Q2 aumenta a quantidade possível em um ciclo. Comprar os dois aumenta a demanda por alimento, espaço futuro e atendimento. O painel continua apontando essas condições, para a pessoa entender por que uma rainha melhorada pode estar esperando.

As três melhorias da comum são independentes. O1 e O2 mantêm eficiência generalista, enquanto O3 acrescenta resgate de adultas. Carga maior não concede quebra de pedra, e velocidade maior não reduz a duração de larva. Nenhuma dessas compras troca o tipo das adultas.

---

[← 22 — Mapa da árvore da rainha](../01_Design/22_Mapa_da_arvore_da_rainha.md) · [24 — Carregadora e cortadeira habilidades e números →](../01_Design/24_Carregadora_e_cortadeira_habilidades_e_numeros.md)
