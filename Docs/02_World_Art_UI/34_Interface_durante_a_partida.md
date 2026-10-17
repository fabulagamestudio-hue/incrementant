# 34 — Interface durante a partida

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 34. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

## Informação sempre visível

Saldo de nutrientes e taxa real recente de produção, com unidade N/s.

Quantidade de adultas por tipo, total e capacidade, incluindo indicação de vagas reservadas.

Ovos, larvas e pupas em desenvolvimento e tempo estimado até o próximo nascimento.

Modo da rainha e motivo de interrupção de reprodução.

Acessos à árvore do formigueiro, árvore da rainha, seleção de pincéis e menu de pausa.

Orçamento e nome do pincel ao pintar; cor e função de cada área marcada.

## Painéis por seleção

| Seleção | Informações e ações |
| --- | --- |
| Rainha | Modo automático, tipo específico ou pausado; tipos desbloqueados; reserva mínima; próximo ciclo; vida e alertas |
| Cultivo | Área funcional, matéria prima por origem, fungo processável, taxa real e potencial, resíduos e melhorias |
| Preparo | Estoque triturado, perecibilidade e destinos alcançáveis |
| Berçário | Vagas, reservas, ovos a transferir, larvas, pupas e tempos |
| Depósito | Folhas e frutos, capacidade, cargas grandes e idade dos lotes |
| Resíduos | Quantidade, capacidade, decomposição e material em transporte |
| Túnel ou obra | Função permanente, células a remover, acesso, material e progresso |
| Árvore | Custo, requisitos, base e resultado após compra, estado comprado ou bloqueado |

Avisos destacam invasão, submersão, superlotação, falta de acesso, sala concluída e desbloqueio. Nascimentos comuns usam efeito discreto em vez de uma janela modal a cada adulta. Contadores podem animar o acréscimo sem impedir leitura do saldo verdadeiro.

A função do ambiente depende da marcação e não do formato. O destaque deve ajudar a enxergar áreas estreitas de berçário e cultivos alongados. Não ocultar erro de pintura atrás de um sistema que renomeia automaticamente a área.

## Leitura da colônia e estados das ações

Informação permanente orienta decisões frequentes; detalhes de sala aparecem na seleção. Uma pessoa deve conseguir distinguir produção limitada por matéria prima, por processamento ou por acesso. A taxa exibida precisa representar a produção que realmente chega ao saldo, e não apenas um potencial máximo.

O pincel especial usa sua tarifa mesmo num acesso estreito. Sua seleção deve deixar isso claro antes da confirmação. Uma obra sem caminho pode mostrar espera por acesso, sem ser apagada automaticamente. Um berçário cheio mostra falta de vaga; uma rainha em pausa mostra a escolha de parar novos ciclos.

Abrir árvores, painel da rainha ou painel de sala não tem regra de pausa confirmada. O menu de pausa sempre congela. A equipe ainda precisa decidir se os painéis de gestão mantêm o tempo correndo, e comunicar essa escolha de forma consistente.

---

[← 33 — Menus fluxo de telas e configurações](../02_World_Art_UI/33_Menus_fluxo_de_telas_e_configuracoes.md) · [35 — Áudio atmosfera e juice →](../02_World_Art_UI/35_Audio_atmosfera_e_juice.md)
