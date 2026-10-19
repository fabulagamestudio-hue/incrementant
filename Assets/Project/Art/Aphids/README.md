# Arte do pulgão

Oito sprites de pixel art verde, com transparência: `Aphid_Walk_0.png` a `Aphid_Walk_3.png` (lateral) e `Aphid_Top_0.png` a `Aphid_Top_3.png` (superior). Todos virados para a direita, em ordem de caminhada. Corpo em forma de pera, seis patas, antenas e cornículos traseiros.

Cada quadro tem **72x48 px**, importado a **36 pixels por unidade**: canvas de **2 × 1,33 unidades**, dois terços do canvas das formigas adultas (108x72 a 36 PPU, 3 × 2 unidades). Filtro Point, sem mipmaps nem compressão, malha FullRect e alpha binário. Pivô lateral (0,5; 0,25); superior (0,5; 0,5). Alinhamento comum dentro de cada vista, sem recorte individual que faça o corpo saltar na animação.

Importação automática por `Scripts/Editor/AntFarm/AphidSpriteImporter.cs`. Os PNGs estão preparados como assets; ainda não há comportamento ou renderizador de pulgões no jogo. A velocidade da futura animação fica a cargo do consumidor.

Gerados em 2026-10-18 pelo `image_gen.imagegen` integrado, usando `Art/Ants/Ant_Walk_0.png` como referência de estilo. A prancha 4x2 foi separada, reduzida sem interpolação e limitada a 16 cores opacas, com transparência 0/255. Prompt em [Generation.json](Generation.json). Aprovação visual pelo desenvolvedor pendente.

Validação em 2026-10-18 pela API do Editor Unity: oito sprites carregáveis, 27.648 pixels, dimensões, PPU, pivôs, paleta, alpha e configurações de importação conferidos. Prévia lateral inspecionada. Configuração gravada nos assets durante Play Mode; a compilação do novo importador ainda depende da recarga de scripts no Editor. Nenhuma build gerada.
