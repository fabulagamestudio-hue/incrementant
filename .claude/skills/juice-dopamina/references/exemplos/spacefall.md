# Exemplo — SpaceFall (ação espacial 3D, câmera superior, URP)

Pacote de dopamina entregue em 2026-10 (REQ-006, 011 a 021). Serve como **modelo de cobertura e de ordem de grandeza**, não como regra: valores provisórios, parte nunca ouvida em playtest. Documentação completa no projeto: `Assets/Project/Scripts/Runtime/Presentation/Feedback/FeedbackGuide.md`.

## Decisões de estrutura que valeram a pena

- **Um `GameFeelDirector`** que se cria sozinho depois da primeira cena e assina eventos **estáticos** (`Hull.AnyDamaged`, `Hull.AnyDestroyed`, `Shield.AnyLayerBroken`, `EnemySpawner.WaveCleared/WaveStarted`, `Projectile.PlayerHullHit`, `PlayerWeaponController.AnyVolleyFired`, `ShipSprint.AnyStarted`…). Nada em cena ou prefab o referencia; zera os estáticos no `SubsystemRegistration`.
- **Nenhuma regra de jogo muda**: o pacote só reage.
- **Opções por família** (Opções → Jogabilidade): Pausa de impacto, Números de dano, Marcador de acerto, Clarão do disparo, Alerta de dano, Brilho da coleta, Tremor de tela.
- Prefabs de VFX em `Resources/Feedback`; o setup os recria e, se faltar algum, o log diz qual menu rodar.
- Sons: `FeedbackSfx` (18 tipos) → banco autoral → sintetizado. Laboratório de Áudio no Editor.

## Valores usados (pontos de partida)

| Família | Valores |
|---|---|
| Pausa de impacto | `timeScale` 0,05; ≤ 0,2 s; mínimo 0,35 s entre pausas; chefe 0,12 s; golpe ≥ 15% do casco 0,05 s |
| Câmera lenta (chefe) | 0,3 por 0,7 s + 0,65 por 0,18 s |
| Soco de FOV | chefe −4° em 1,3 s; marcos de run −2,2° a −3,4° em 0,8 s; marcos do combo −(1,1 + 0,5·índice) em 0,45 s; dash +2,4° |
| Números de dano | tamanho 0,85× a 1,7× (log); janela de soma 0,14 s; pool 40 |
| Combo | janela 4 s; visível a partir de 3; marcos 10/25/50/100; multikill ≥ 5 kills com ≤ 0,15 s entre elas |
| Recompensa do combo | créditos = kills × 0,6 × M; sucata = kills × 0,35 × M; energia = kills ÷ 10; M = 1 + kills ÷ 40; mínimo 5 kills; 2–8 pedaços |
| Marcos da run | 50, 100, 250, 500, 1000 e a cada 500 |
| Dano recebido | tremor `0,9 + 12·fração`; alerta de casco baixo < 30%, intensidade máxima em 5%; batimento 1,15 s → 0,55 s |
| Loot | brilho 5–7 u (grande por causa da pixelização); atração 0,7× → 3× em 0,45 s; voo até o contador 0,55 s |
| Cadeia de coleta | pentatônica 0/2/4/7/9/12 semitons, `MaximumSteps` 5 |
| Corrida/dash | linhas de velocidade de 4–7,5 px, até 48; rastros azuis; som com intervalo 0,3 s |
| Música | base `Other` sempre; `Bass` com combo 5, `Drums` com combo 12; deriva corrigida > 50 ms |

## Lições registradas

- Brilho em disco ou oval foi **rejeitado**; no espaço use raios, fogo e fumaça. Explosão é bola; nada cai nem sobe (sem morteiro, cratera nem coluna).
- Efeito fino (~2 px) sumia na pixelização; precisou ser grande, achado **vendo em captura**.
- Corrida ganhou soco de FOV e parecia impacto; ficou só o alargamento gradual.
- Capturas de divulgação mostram **tempo de run natural** (cronômetro aleatório de 8 a 15 min), nunca poucos segundos.
- Testar áudio entrando direto na Gameplay exige criar o controlador de som; Play Mode sem invulnerabilidade deixa os inimigos matarem o jogador e a run pode ir para o save.
- Texto de feedback só com números ou chaves de localização; TMP escrito por código entra na lista de textos controlados por código.
- Sons e vários visuais **não foram ouvidos/avaliados** por quem implementou; tudo ficou "provisório até o playtest".
