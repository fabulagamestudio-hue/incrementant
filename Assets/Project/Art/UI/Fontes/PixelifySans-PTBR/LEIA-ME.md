# Pixelify Sans — português para Unity

Arquivos originais estáticos Regular e Bold, obtidos do repositório do próprio autor. Não foram convertidos nem modificados.

Fonte: https://github.com/eifetx/Pixelify-Sans/tree/main/fonts/ttf
Licença: SIL Open Font License 1.1 (incluída em OFL.txt).

## Cobertura verificada

Foi inspecionada a tabela Unicode cmap dos dois TTFs: 125 caracteres testados, nenhum ausente. O conjunto contém ASCII imprimível, os acentos abaixo, suas maiúsculas e ª º ° ×.

á à â ã é ê í ó ô õ ú ü ç
Á À Â Ã É Ê Í Ó Ô Õ Ú Ü Ç

Esse teste verifica que os glifos existem no arquivo. O uso no projeto Unity ainda depende de gerar e atribuir o Font Asset com esses caracteres. Não foi executado teste dentro do Editor Unity.

## TextMeshPro

1. Copie os dois TTFs, o arquivo Caracteres-PTBR.txt e a licença para uma pasta do projeto, por exemplo Assets/Art/Fonts/PixelifySans.
2. Abra Window > TextMeshPro > Font Asset Creator.
3. Em Source Font File, selecione PixelifySans-Regular.ttf.
4. Em Character Set, escolha Characters from File e selecione Caracteres-PTBR.txt. Não use apenas ASCII.
5. Para a apresentação pixel art, comece com Render Mode RASTER ou RASTER_HINTED, Sampling Point Size Custom e um atlas 512 × 512. Confira a prévia no tamanho desejado; evite escalas fracionárias. Use material Bitmap e Filter Mode Point no atlas.
6. Generate Font Atlas e Save. Atribua esse novo Font Asset ao componente TextMeshProUGUI. Repita para Bold se necessário.
7. Teste: Colônia · Evolução · Berçário · Resíduos · Configurações · Próxima cria.

Se os acentos virarem quadradinhos apesar de existirem no TTF, verifique se o componente usa o novo Font Asset e se o atlas foi regenerado e salvo com Caracteres-PTBR.txt. Trocar o TTF sem atualizar o Font Asset não corrige um atlas antigo limitado a ASCII.

Para textos recebidos em Unicode decomposto, normalize as strings para NFC (em C#: texto.Normalize(System.Text.NormalizationForm.FormC)) antes de exibir. O conjunto entregue prioriza os caracteres acentuados pré-compostos do português.

## Licença

OFL permite incorporar a fonte a jogos, inclusive comerciais, mantendo o aviso de copyright e a licença com a distribuição da fonte. Não exige que o jogo adote a OFL. Não venda os arquivos da fonte isoladamente. Consulte OFL.txt para os termos completos.

## Arquivos

- PixelifySans-Regular.ttf: textos e rótulos.
- PixelifySans-Bold.ttf: títulos e destaques.
- Caracteres-PTBR.txt: conjunto de caracteres UTF-8 para o atlas.
- Verificacao.json: relatório dos glifos testados.
- Previa-Portugues.png: amostra renderizada fora da Unity, sem suavização.
- prepare_fonts.py: obtenção dos originais e verificação dos glifos.
