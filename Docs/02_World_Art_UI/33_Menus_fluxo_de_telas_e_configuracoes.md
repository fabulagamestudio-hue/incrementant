# 33 — Menus fluxo de telas e configurações

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 33. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

1. Abrir o jogo e exibir splash da Fábula Game Studio.

2. Ir ao menu principal com Iniciar, Continuar, Configurações, Sobre o jogo e Sair.

3. Iniciar cria nova colônia e abre a cinemática. Propor confirmação se já existir save para evitar sobrescrita acidental.

4. Continuar carrega a sessão salva. Sem save válido, mostrar a opção indisponível com motivo.

5. Configurações permite alterar volume geral, efeitos sonoros, tamanho da tela e modo de exibição.

6. Sobre o jogo abre créditos da Fábula e participação da equipe. Nomes finais e créditos de assets devem ser fornecidos pela equipe.

7. Sair encerra o aplicativo.

8. Durante a partida, o menu de pausa congela a simulação e oferece Retomar, Configurações, Menu principal e Sair.

9. Configurações aberta pela pausa mantém a partida congelada. Voltar retorna à pausa, e Retomar volta à simulação.

10. Game over oferece Retomar ponto anterior ou Reiniciar formigueiro.

## Opções de configurações

| Grupo | Definido | Proposta de acabamento |
| --- | --- | --- |
| Áudio | Volume geral e volume de efeitos separados | Volume de música adicional para controlar trilha cozy |
| Tela | Tamanho e formato ou modo de tela | Resoluções disponíveis; janela, sem bordas e tela cheia conforme plataforma |
| Persistência | Continuar e retorno anterior após game over | Salvar preferências fora do estado da colônia |
| Feedback | Muitas animações e efeitos de interação | Opção de reduzir animações e volume de alertas |

Configurações de música separada, animação reduzida e confirmação de nova partida são propostas de usabilidade, não exigências já expressas. Plataforma alvo e lista de resoluções precisam ser confirmadas. O menu Sair pressupõe aplicativo que permita encerrar, devendo ser adaptado se a plataforma for navegador.

O painel da rainha e o clique nas salas são painéis de gameplay, distintos da pausa. Não presumir que abrir a árvore pausa o jogo; a regra de pausa desses painéis permanece uma decisão em aberto.

## Relações entre telas

Continuar abre uma partida existente. Iniciar começa outra colônia. A confirmação de perda de uma partida anterior é uma proposta para a tela de nova partida, não um custo dentro da economia. Sobre o jogo apresenta créditos e permite voltar ao menu principal.

A pausa congela a partida antes de abrir configurações. Ao sair das configurações, volta à pausa; só Retomar faz o tempo de jogo avançar. Mudar volume não deve exigir abandonar a colônia ou avançar o perigo enquanto a pessoa navega.

Plataforma alvo, resoluções finais e nomes nos créditos ainda precisam de fechamento. O menu principal tem Sair para fechar o jogo, com adaptação de apresentação se a plataforma não permitir encerrar a aplicação da mesma forma.

---

[← 32 — Invasoras combate e consequências](../01_Design/32_Invasoras_combate_e_consequencias.md) · [34 — Interface durante a partida →](../02_World_Art_UI/34_Interface_durante_a_partida.md)
