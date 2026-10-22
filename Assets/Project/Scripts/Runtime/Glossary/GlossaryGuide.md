# Glossário — guia

Como o glossário funciona e como mexer nele. Onde estão os arquivos: [Runtime/README.md](../README.md) (pasta `Glossary/`).

## Como funciona

- **Pedido** (REQ-066, 2026-10-22): uma janela com a explicação de tudo, aberta por um botão na barra inferior e pelo menu do ESC; o fim do tour aponta para ela.
- **Acesso.** Botão `?` (quinto da `BarraInferior`, dica `tip.glossary`) e a entrada **GLOSSÁRIO** do menu do ESC, entre Configurações e Salvar. A parada "Dicas e glossário" do tutorial destaca o botão `?` e a despedida o cita.
- **Janela** (`GlossaryScreen`, objeto `Glossario` do HUD, último filho do canvas: fica por cima do menu do ESC). Mesmo molde do histórico de versões: fundo escurecido, lista à esquerda com o nome de cada categoria e um botão por entrada, título, categoria e texto à direita, botão Voltar. Abre na última entrada vista (a primeira na primeira vez).
- **Pausa e input.** Aberta, pausa o jogo e trava o input do mundo; ao fechar, devolve os dois como estavam (aberta pelo menu do ESC, volta ao menu ainda pausado).
- **ESC.** Quem trata é o `AntFarmHud.HandleEscape`: com o glossário aberto, o ESC só o fecha.
- **Conteúdo.** Ordem e ids em `GlossaryContent.Categories` (9 categorias, 49 entradas: o básico, recursos e mundo, ferramentas, ambientes, formigas, menu da rainha, evolução, ameaças e interface). Textos em `Editor/Localization/LocalizationCatalog.Glossary.cs`, chaves `glossary.cat.<categoria>`, `glossary.<entrada>.title` e `glossary.<entrada>`. Escritos a partir das dicas do HUD, do tour e dos guias de sistema (2026-10-22).
- **Imagens** (pedido de 2026-10-22: misturar fotos do jogo e sprites). 30 imagens 640x360 em `Art/UI/Glossary`, nomeadas pelo id da entrada: fotos de salas redondas montadas num mapa limpo (salas, luz, cavar e construir) e sprites ampliados (nutriente, crias, rainha, papéis, cupim, pulgão preto, terra, água, entulho e plantas). Aparecem entre a categoria e o texto (`_picture`); entrada sem imagem só mostra texto. Como foram feitas e como refazer: [README da pasta](../../../Art/UI/Glossary/README.md).
- **Montagem.** `AntFarmHudSetup.Glossary.cs` (`BuildGlossary`), chamado pelo **Incrementant/Setup/Montar HUD da partida**; o runtime clona os modelos `ModeloCategoria` e `ModeloEntrada` em `Awake`. Ajustes à mão no objeto se perdem ao remontar.

## Como estender

- **Entrada nova:** id na categoria certa de `GlossaryContent`, uma linha `AddGlossaryEntry` no catálogo e **Incrementant/Setup/Configurar localização**. Não precisa remontar o HUD. O teste `LocalizationTests.EveryGlossaryEntryHasItsTexts` falha se faltar título, texto ou categoria, ou se um id se repetir.
- **Imagem nova:** PNG 640x360 em `Art/UI/Glossary` com o id da entrada como nome e **Montar HUD da partida** (o setup liga a pasta inteira).
- **Categoria nova:** `new Category(...)` em `GlossaryContent` e a chave `glossary.cat.<id>`.
- O texto cabe em umas 49 letras por linha na coluna da direita; passou da altura, rola.

## O que evitar

- Não escreva no glossário regra que o jogo não tem: ao mudar um sistema (pincel, sala, papel, ataque), revise a entrada correspondente.
- Não mude a ordem dos filhos do canvas: o glossário precisa ficar depois do menu do ESC.

## Pendências

- Sem ícone de livro no kit de UI: o botão da barra leva um `?` em texto e a entrada do menu do ESC usa a folha.
- Os nomes das categorias saem em caixa alta, e a fonte comprime o acento das maiúsculas ("O BÁSICO" parece "O BASICO").
- Falta uma pessoa ler o glossário inteiro e revisar os textos; o agente conferiu abrir, fechar, ESC e o menu do ESC em Play Mode (2026-10-22).
