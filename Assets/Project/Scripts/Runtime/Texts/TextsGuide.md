# Textos e idiomas — guia

Como a localização funciona e como mexer. Onde estão os arquivos: [Runtime/README.md](../README.md). A opção de idioma mora nas configurações: [SettingsGuide.md](../Settings/SettingsGuide.md).

## Como funciona

- **Unity Localization** (`com.unity.localization` 1.5.13). Uma tabela de strings só, `Game`, com os Locales `pt-BR` (idioma de origem e padrão) e `en`. Os assets ficam em `Assets/Project/Localization` (Locales, Tables, Localization Settings).
- **Fonte dos textos:** `Editor/Localization/LocalizationCatalog.cs` (chave, português, inglês). Os nós das árvores têm o português nas próprias definições (`QueenTree`, `HiveTreeInfo`) e só o inglês no catálogo. `LocalizationSetup` (menu **Incrementant/Setup/Configurar localização**) grava o catálogo nos assets; é idempotente.
- **Texto estático das telas** (rótulos montados pelo setup do HUD e do menu principal): `AntFarmHudSetup.BuildText` chama `LocalizationSetup.Bind`, que acha a chave pelo português do rótulo e põe um `LocalizeStringEvent` no `Text`. O texto some/aparece no idioma certo ao ligar o objeto e quando o idioma muda. Rótulo em caixa alta que só existe em caixa baixa no catálogo ganha a variante `<chave>.U` sozinha. Texto sem correspondência vira `auto.*` (inglês = português) com aviso no Console: acrescente ao catálogo. Texto neutro (`1X`, `Q0`, `100%`) não entra na tabela.
- **Texto montado por código** (valores do HUD, avisos, tutorial, fim de jogo, mensagens de salvar): `Loc.T("chave")` ou `Loc.T("chave", args)`, com `{0}`, `{1}`… preenchidos por `string.Format` (cultura invariante). As entradas não usam Smart Format. Chave inexistente devolve a própria chave (visível, sem exceção). `Loc` guarda os textos num cache limpo a cada troca de idioma.
- **Idioma.** `GameSettingsData.Language` (`settings.json`, padrão `pt-BR`) → `GameSettings.Apply` → `Loc.SetLanguage`. A tela de configurações tem a linha **IDIOMA** (setas, no fim). Trocar o idioma atualiza na hora os textos estáticos ativos (os de painéis fechados atualizam ao abrir) e os dinâmicos (o HUD refaz os textos a cada 0,25 s; `Loc.LanguageChanged` avisa quem guarda texto: tela de configurações e rodapé do menu principal). O seletor de idioma inicial do Localization está fixo em `pt-BR`: quem manda é a configuração salva.

## Como adicionar

1. **Texto novo por código:** linha no catálogo (`Add(l, "area.nome", "português", "english")`), `Loc.T("area.nome")` no código, rode **Configurar localização**. O teste `LocalizationTests` falha se uma chave usada em `Runtime/` não existe na tabela, se falta idioma ou se os `{n}` diferem entre idiomas.
2. **Rótulo novo no HUD/menu:** escreva em português no setup (`BuildText`/`BuildButton`), acrescente o par ao catálogo e remonte a cena (**Montar HUD da partida** / **Criar cena MainMenu**).
3. **Idioma novo:** código em `Loc.Codes` e `Loc.NativeNames`, inglês→novo idioma em cada entrada do catálogo (hoje o catálogo tem duas colunas fixas; estenda `Entry` e `LocalizationSetup.Write`), e confira se a fonte (Press Start 2P) tem os caracteres.

## O que evitar

- Não deixe texto de jogador literal no código: use `Loc.T`. Texto de depuração (painel F2, F11, Gizmos, logs, exceções) fica fora.
- Não compare textos exibidos (`text == "..."`): compare chaves/enums.
- Não use Smart Format nas entradas (o código formata); não coloque `{`/`}` que não sejam `{n}`.
- Texto estático ligado a um `LocalizeStringEvent` não deve ser sobrescrito por código sem necessidade; os que o código reescreve a cada quadro (rótulo de compra, tipos de formiga) são sobrescritos de qualquer forma.

## Pendências

- Só pt-BR e en. Painel de depuração (F2), estatísticas (F11) e mensagens de exceção ficam em português/inglês cru.
- Textos do `GameSnapshot` (exceções de save corrompido) não são traduzidos.
- Plural/gênero não tratados (os textos foram escritos sem concordância variável).
