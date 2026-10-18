# Arte e variantes de cor das formigas

## Inimigos

- [InimigaAlada/](InimigaAlada/README.md): 16 sprites em 144x144 — voo, pouso com perda de asas, caminhada lateral e vista superior. Arte preparada em 36 PPU; usada pelos cupins dos ataques desde 2026-10-18 (REQ-029).

## Melhoria gráfica — 2026-10-18 (REQ-026)

Os 72 quadros adultos Top/Walk foram redesenhados com corpo segmentado, cintura, antenas, mandíbulas e luz na carapaça, mantendo 108x72 px, os quatro quadros por vista, caminhos, GUIDs e pivôs. A base vermelha e as oito pastas usam a mesma animação; as funções mantêm suas referências de cor abaixo. Paleta de até 16 cores por quadro, alpha exclusivamente 0/255 e amostragem sem suavização: nenhum halo semitransparente nas bordas. A operária é idêntica à base. Ovo, larva e pupa não foram alterados.

As pranchas-base foram criadas pelo `image_gen.imagegen` integrado, usando os quadros anteriores como referência; recorte, redução à resolução final e aplicação das paletas preservam pixels inteiros. [Prompts e processamento](../PixelArtGeneration.json). A densidade de 36 pixels por unidade mantém o tamanho de 3x2 unidades. O menu de regeneração continua sendo o gerador procedural provisório e sobrescreve esta arte se acionado; o setup preserva arquivos existentes.

Validação desta entrega: 141 PNGs (72 adultos e 69 elementos de cenário), 1.390.673 pixels conferidos, paletas de até 24 cores, alpha binário, dimensões e metadados preservados. Prévias ampliadas inspecionadas. Importação na Unity e comportamento em Play Mode são verificações separadas; veja o registro ART-010 em `Production/ART.md`.

## Histórico e referências de cor

Cada pasta contém `Ant_Top_0.png` a `Ant_Top_3.png` e `Ant_Walk_0.png` a `Ant_Walk_3.png`, com 108x72 pixels e transparência. Em 2026-10-18, os 72 quadros adultos (raiz e oito pastas) foram ampliados exatamente 6x em cada eixo a partir dos originais 18x12, sem suavização nem alteração de cores. Há 36x mais pixels disponíveis para retoques; a ampliação por si só não acrescenta detalhes.

| Pasta | Cor | Referência da paleta |
|---|---|---|
| `Operaria/` | Vermelho original | `#9E311E` |
| `Soldado/` | Roxo escuro | `#6F339E` |
| `Coletora/` | Verde | `#318B39` |
| `Escavadora/` | Laranja | `#D36318` |
| `Cuidadora/` | Azul-claro | `#419EC7` |
| `Jardineira/` | Rosa | `#C2477F` |
| `Cortadeira/` | Verde-azulado | `#1F9E8E` |
| `Rainha/` | Dourado | `#CA9623` |

As variantes aplicam a paleta proposta na conversa de 2026-10-18; os nomes são categorias de arte e não adicionam classes ao jogo. A rainha mantém o desenho e o tamanho dos quadros originais: somente a cor muda.

Os caminhos dos PNGs da raiz e seus GUIDs foram preservados. `Operaria/` contém cópias idênticas desses quadros. Ovo, larva e pupa permanecem na raiz, sem alteração de resolução ou densidade.

As variantes já estão ligadas ao jogo (ver `AntFarmGuide.md`, "Formigas em sprite"). O importador `AntSpriteImporter` cobre as subpastas e configura filtro Point e 36 pixels por unidade para `Ant_Top_*` e `Ant_Walk_*`: 108/36 por 72/36 mantém o tamanho de 3x2 unidades no jogo. Os pivôs normalizados continuam Top (0,5; 0,5) e Walk (0,5; 0,25). Cenário e cria continuam usando a densidade base de 6 pixels por unidade. O gerador também exporta os adultos em 108x72.

Validação da ampliação em 2026-10-18: 72 PNGs, 559.872 pixels RGBA comparados com os originais da sessão; cada bloco 6x6 corresponde exatamente ao pixel original. Metadados comparados: somente `spritePixelsToUnits` passou de 6 para 36, preservando GUIDs e pivôs. Confirmação final de compilação/importação na Unity pendente: refresh excedeu o tempo de espera; console sem erros no momento da consulta. Nenhuma build gerada.

Validação em 2026-10-18: 48 PNGs e 10.368 pixels comparados com os originais, conferindo dimensões, alpha e substituição exata da paleta; hashes dos originais e das cópias vermelhas conferidos. Prévia ampliada inspecionada. Importação e reprodução na Unity ainda não verificadas.

Em 2026-10-18 foram criadas `Jardineira/` e `Cortadeira/` por troca de paleta a partir de `Operaria/` (mesmos 4 tons: patas escuras, patas do outro lado, corpo e brilho; olho preservado). Carregadora usa `Coletora/`.
