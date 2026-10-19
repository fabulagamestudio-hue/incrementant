# Áudio de feedback

Som é metade do efeito: o mesmo evento visual sem som parece vazio. A informação, porém, nunca depende só do som, e o som não depende da opção de efeitos visuais.

## Tipos de som

Um catálogo enxuto de tipos reaproveitados em muitos eventos, cada um com **descrição, finalidade e ao menos uma situação de teste**. Famílias comuns: tique de contagem, sino (etapa/marco), arpejo de onda limpa/compra, whoosh de chegada, estalo de carta, toque de kill, quebra de proteção, travas e descargas, batimento, sirene, arrancada, esgotado, hover e clique de UI, recusa, ping de descoberta, três notas de objetivo.

- Tipo novo entra **no fim** do enum (bancos guardam o número) e ganha descrição e situação de teste.
- O tom pode variar por evento (marcos mais agudos a cada degrau; dash mais agudo que corrida). Prefira clips sem altura marcante onde o código varia o tom.
- Sons que tocam muitas vezes seguidas (tique, estalo) têm duração curta e intervalo mínimo.

## Hierarquia sintetizado → autoral

1. **Sintetizado provisório** gerado por código na primeira vez que toca (camadas, nível igualado, pico limitado, sem estalo). Serve para o jogo já "soar" enquanto a arte não chega.
2. **Banco autoral:** uma entrada por tipo; com clip, toca no lugar do sintetizado; vazio, continua sintetizado. Vale em todo lugar que chama o tipo.
3. O usuário pode **manter propositalmente** alguns tipos sintetizados.

### Boas práticas de síntese

- Termine toda voz com um acabamento: tira o DC, fecha os últimos ~6 ms, iguala o RMS ao do tipo, limita o pico a ~0,95 com arredondamento suave.
- **Sinos:** parciais inarmônicos (1; 2; 2,76; 5,4), duas senoides levemente desafinadas por parcial (coro), queda de tom de ~1%, clique de ruído no ataque, parciais agudos morrendo mais depressa, eco curto para dar espaço.
- **Whoosh/rajada:** ruído por filtro ressonante varrendo o espectro. **Vidro:** três ressonâncias. **Recusa/escapamento:** dente de serra suavizado.
- Teste de síntese: finitos, sem clipar, nível mantido. **Isso não substitui ouvir.**

## Prompts de som para geração por IA

Descreva a **forma** antes do clima:
- "single", "isolated", sem música nem ambiente;
- ataque (seco, suave), ordem dos eventos, **cauda curta**, textura concreta (metal, vidro, console dos anos 90);
- duração pelo uso (tique ~0,05 s; sino ~0,5–1,5 s);
- coerência com a identidade: sons do jogador limpos e frios, de ameaça ásperos, e assim por diante.

Gere **poucas versões por tipo** (duas, quatro para os mais críticos) em lotes pequenos, converta para WAV 44,1 kHz e mostre ao usuário **prompts + uso + porquê** antes de gastar créditos.

## Laboratório de áudio (ferramenta de Editor)

Uma janela que, para cada tipo, deixa escolher entre o sintetizado e os candidatos, ouvir **no jogo** e gravar no banco.
- Escolha em preferências do Editor, valendo na hora por um banco em memória (inclusive em Play), sem mexer no asset; "Gravar" grava com desfazer; "Descartar" volta ao gravado.
- Entra em Play direto na cena de jogo e cria um controlador de som se faltar (quando o caminho pula a Splash); opção de tocar a trilha da run por baixo; jogador invulnerável.
- Dispara **situações do jogo** agrupadas por contexto (combate, nave, boss, estação, lore/treinamento, menus), pelo caminho real quando dá, por reflexão nos métodos privados do diretor, ou repetindo a sequência de sons nas telas que não abrem de qualquer lugar.
- Cuidado: renomear método usado por reflexão quebra o teste que lista os alvos; atualize a lista.

## Música em camadas por intensidade

- A trilha da run é a base, **sempre tocando**; camadas extras (stems) entram conforme a intensidade (tamanho do combo): ex. baixo com combo 5, bateria com combo 12; padrão de um perfil novo: 3 camadas com mínimo 5, 12 e 25.
- Cada camada liga ao atingir o mínimo (fade de entrada) e desliga quando a sequência cai (fade de saída).
- **Sincronia:** as fontes copiam a posição por amostra da trilha ao começar e se corrigem se a deriva passar de ~50 ms (checada a cada ~1 s).
- Volume = fade × volume da camada × volume de música das opções × ganho da cena. O stem recebe os mesmos efeitos de espaço da trilha, mas **não** o nivelador de loudness (ele desfaria o equilíbrio da mixagem).
- Só toca com a trilha da run em curso, sem override de diálogo nem música de chefe; parada no menu/hangar.
- **Entrega de stems:** mesma duração, andamento e ponto de loop da trilha; comece todos no compasso 1; sem silêncio extra; mesma taxa; WAV ou OGG (MP3 tem folga no início); cada stem deve soar bem **somado**, em ordem crescente de intensidade. Se a trilha mudar, refaça os stems.
- Para loop sem estalo, sobreponha os últimos ~15 ms aos primeiros em cada stem.
