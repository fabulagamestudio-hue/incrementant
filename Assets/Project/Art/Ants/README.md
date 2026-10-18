# Variantes de cor das formigas

Cada pasta contém `Ant_Top_0.png` a `Ant_Top_3.png` e `Ant_Walk_0.png` a `Ant_Walk_3.png`, mantendo os 18x12 pixels e a transparência dos originais.

| Pasta | Cor | Cor principal |
|---|---|---|
| `Operaria/` | Vermelho original | `#9E311E` |
| `Soldado/` | Roxo escuro | `#6F339E` |
| `Coletora/` | Verde | `#318B39` |
| `Escavadora/` | Laranja | `#D36318` |
| `Cuidadora/` | Azul-claro | `#419EC7` |
| `Rainha/` | Dourado | `#CA9623` |

As variantes aplicam a paleta proposta na conversa de 2026-10-18; os nomes são categorias de arte e não adicionam classes ao jogo. A rainha mantém o desenho e o tamanho dos quadros originais: somente a cor muda.

Os PNGs da raiz e seus `.meta` foram preservados para manter os caminhos usados pelo gerador e pelas ferramentas atuais. `Operaria/` contém cópias idênticas desses quadros. Ovo, larva e pupa permanecem na raiz.

A integração das variantes no jogo fica para uma tarefa posterior. O importador existente `AntSpriteImporter` também cobre as subpastas e configura filtro Point, 6 pixels por unidade e o pivô conforme Top ou Walk. Os `.meta` das cópias são gerados pela Unity na importação, com GUIDs próprios.

Validação em 2026-10-18: 48 PNGs e 10.368 pixels comparados com os originais, conferindo dimensões, alpha e substituição exata da paleta; hashes dos originais e das cópias vermelhas conferidos. Prévia ampliada inspecionada. Importação e reprodução na Unity ainda não verificadas.
