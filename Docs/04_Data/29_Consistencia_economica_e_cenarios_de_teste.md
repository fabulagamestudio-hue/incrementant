# 29 — Consistência econômica e cenários de teste

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 29. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

## Produção potencial e gargalos

Para uma área de 4 B, cultivo base processa no máximo 1 UM/s e gera até 3 N/s com folhas após colheita. Se a equipe traz só 0,5 UM/s, o limite é 1,5 N/s. Não anunciar 3 N/s como produção garantida: deslocamento, cuidado, fungo, deterioração e resíduos podem reduzir o resultado.

Com C1 e J1 na área atendida, o potencial de crescimento é 1 × 1,3 × 1,3 = 1,69 UF/s. Com C3, isso representa até 6,084 N/s de folha ou 12,168 N/s de fruto, desde que a coleta e colheita sustentem a taxa. A interface deve mostrar taxa real recente e explicar o gargalo.

## Duração de escavação com transporte

Na proposta, a comum remove 16 células/s e leva até 32 células de terra. Uma carga leva 2 s para escavar. Com 4 s de viagem e 1 s de descarga por ciclo, três comuns removem em média cerca de 13,7 células/s, abrindo 1.024 células de cultivo em aproximadamente 74,7 s de trabalho contínuo. Deslocamentos iniciais, acesso e instalação acrescentam tempo. Taxa de escavação isolada não basta para estimar duração de obra.

## Reprodução e mortalidade

Postura base admite até 1/18 = 0,0556 adulta/s após estabilizar a fila. Q1 admite até 1/12 = 0,0833. Q2 admite até 3/12 = 0,25, limitado por vagas e alimento. Para 200 adultas com vida média de 1.050 s, a reposição média necessária é 200/1.050 = 0,1905 adulta/s. Q2 tem margem teórica para manter o teto, Q0 não.

A renovação custa alimento. Com uma composição média que custa aproximadamente 25 N por cria, sustentar 200 adultas requer cerca de 4,76 N/s só para reposição. O jogador precisa expandir produção antes desse patamar. Testar o ritmo para que crescimento não vire somente manutenção do número atual.

## Reserva e orçamento inicial

Depois da pintura inicial de 64,5 N, restam 35,5 N. Berçário B0 e 2 B pintados custam 90 N, além de um acesso barato. O excedente necessário é cerca de 55 N. A 1 N/s real, são cerca de 55 s de produção depois do cultivo, mais a duração da obra. O alvo de berçário em 120 a 240 s inclui escavação e logística.

Depois do berçário, reserva mínima de 30 N deixa moeda para escolhas. Não impor aumento exponencial de preço de cada nascimento. Os custos de habilidades e área crescem pelo conteúdo escolhido, e a população é limitada por espaço e limite adicional de população. Essas estimativas expressam o ritmo pretendido e ainda precisam ser confrontadas com a experiência de jogar.

## Objetivo do primeiro balanceamento

Os custos devem manter escolhas acessíveis sem tornar todo novo saldo uma compra óbvia. A pessoa pode guardar para uma transformação maior, melhorar uma atividade que está limitando o conjunto ou manter recursos para nascerem especialistas.

As estimativas numéricas partem de fluxos médios e condições alcançáveis. Uma área sem caminho, uma cria sem berçário funcional ou um cultivo cheio de lixo não entrega sua taxa potencial. O ritmo real precisa conservar a sensação de progresso que motivou o jogo, inclusive depois que surge a reposição por idade.

---

[← 28 — Custos totais e regras de aplicação de melhorias](../04_Data/28_Custos_totais_e_regras_de_aplicacao_de_melhorias.md) · [30 — Chuva água e perigos ambientais →](../02_World_Art_UI/30_Chuva_agua_e_perigos_ambientais.md)
