# Paleta mestra

As 50 cores (32 aprovadas e 18 das formigas) de onde sai toda a arte do mundo, da UI e as cores dos inspectors (etapa 0 do [documento 40](../../../../Docs/02_World_Art_UI/40_Direcao_de_arte_unificada_e_terreno_iluminado.md)). Prancha com nomes e índices: [41_Paleta_mestra.png](../../../../Docs/02_World_Art_UI/41_Paleta_mestra.png).

| Arquivo | Conteúdo |
|---|---|
| `MasterPalette.png` | 50×1 px, uma cor por pixel na ordem dos índices (Point, sem compressão, sem mipmaps, legível por script) |
| `MasterPalette.hex` | As mesmas 50 cores, uma por linha (formato aceito pelo Aseprite e pelo Lospec) |

| Índices | Grupo | Cores |
|---|---|---|
| 0 | Contorno | `1B1815` |
| 1–7 | Terras | `382414` `463123` `623E23` `785A40` `9A7F5E` `B9986D` `DEB06D` |
| 8–13 | Quentes | `823A10` `7B4F2E` `9E6534` `C5833E` `D6872F` `F4B815` |
| 14–16 | Cremes | `CFB691` `E4D2AE` `F5EDD9` |
| 17–20 | Pedras | `504D55` `7D7676` `A59685` `BBB29F` |
| 21–28 | Verdes | `0F2105` `173709` `32550E` `437122` `638929` `91B13B` `B3CF51` `E2EA7A` |
| 29–31 | Água | `1F3E70` `346EB4` `6EAADC` |
| 32–34 | Vermelho (formigas) | `5C1408` `9E311E` `D3592A` |
| 35–37 | Roxo (formigas) | `3A1A5C` `6F339E` `A06BD0` |
| 38–40 | Rosa (formigas) | `6B1F45` `C2477F` `E889B4` |
| 41–43 | Verde-azulado (formigas) | `0F4F47` `1F9E8E` `6FD6C4` |
| 44–46 | Branco (lixeira, 2026-10-23) | `9A9AA8` `E8E8EE` `FFFFFF` |
| 47–49 | Azul vivo (fazendeira, 2026-10-23) | `0A2A9E` `0144F7` `5C8CFF` |

**Rampas das formigas (2026-10-21, etapa 8):** as cores das formigas (vermelho da operária e as cores de função) não existiam nas 32; o desenvolvedor escolheu ampliar a paleta com uma rampa de 3 tons (sombra, base, luz) para cada cor que faltava, com a base igual à cor anterior da pasta. Laranja, dourado, azul-claro e verde usam rampas que já existiam (ver `Art/Ants/README.md`). As cores novas entram no fim, então os índices 0–31 não mudaram.

## Como foi montada

- Origem (D5): as 192 cores dos 69 PNGs de `Art/Scenery`. Cada família de cenário pesa igual (a grama tem 58% dos pixels e dominaria a média), com peso pela raiz da contagem dentro da família.
- Agrupadas por matiz, saturação e brilho; cada grupo reduzido por k-means em OKLab e cada centro trocado pela cor real do cenário mais próxima. Assim, as 29 cores fora da água existem no cenário.
- `823A10` e `D6872F` foram acrescentadas à mão para o laranja saturado das folhas secas.
- A água é a única exceção: o cenário não tem azul. As três cores seguem as águas atuais do jogo (`#2C68C4`, `#3C78C8`).
- Remapeando o cenário inteiro para a paleta, folhas, cogumelos, pedras e flores ficam quase iguais ao original.

## Como usar

- Desenhe sprites e atlas novos só com estas cores (as rampas 32–43 são das formigas); o contorno de 1 px é o índice 0, nunca preto puro.
- A luz e o bloom tingem as cores na tela; a paleta vale para os pixels desenhados, não para o resultado iluminado.
- Correspondência atual do terreno (cor mais próxima): túnel e rocha-mãe → 1, terra → 9, terra despejada → 10, areia → 7, areia compactada → 6, pedra → 18, grama → 24. A troca dessas cores no código e no atlas é das etapas 4 e 5.
