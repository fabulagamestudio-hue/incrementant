# Receitas — movimento, cor e técnicas prontas

Padrões para manter o "toque" consistente entre efeitos. Ajuste jogando e registre no guia do projeto quando um valor virar regra. Versão com paleta e código do Incrementant: [exemplos/incrementant/receitas.md](exemplos/incrementant/receitas.md).

## Gramática de movimento

| Intenção | Curva | Duração típica |
|---|---|---|
| Entrar, aparecer | ease-out com overshoot (~1,1) ou cúbica | 0,15–0,25 s |
| Sair, sumir | ease-in, mais curta que a entrada | 0,08–0,15 s |
| Hover | ease-out | 0,08–0,12 s |
| Pressionar | ease-out, muito rápida | 0,05–0,07 s |
| Soltar/retorno | elástica leve ou overshoot | 0,2–0,3 s |
| Pulso de contador | subida ease-out + volta seno | 0,15 s |
| Respirar (loop) | seno entrada-saída | 1,5–2,5 s por ciclo |
| Queda | gravidade + quique pequeno | 0,3–0,5 s |
| Erro/recusa | oscilação amortecida (3 ciclos) | 0,18 s |
| Golpe de número/placa | nasce pequeno, estoura, assenta como mola, endireita de uma inclinação sorteada | ~0,16 s |

Princípios para parecer vivo: **antecipação** (recuar antes de avançar), **squash & stretch** preservando volume (x·y ≈ 1), **follow-through** (overshoot e retorno), **escalonar** vários itens (10–50 ms) e **sobrepor** canais (som + movimento + cor no mesmo quadro).

## Cor como linguagem

Defina a paleta de feedback **a partir da arte do jogo**, e mantenha o significado estável:

| Papel | Ideia |
|---|---|
| Ganho/produção/crédito | dourado quente |
| Leitura/escudo/kill/marco | ciano ou azul da UI |
| Vida/reparo/nascimento | verde |
| Dano/negado/perigo | vermelho (terra em jogo cozy) |
| Ameaça/chegada de onda | âmbar |
| Água | azul |
| Neutro/poeira | cor do material afetado |
| Destaque de UI | branco quente |

Evite branco puro e neon em jogos calmos. Confirme com a direção de arte antes de fixar.

## Técnicas prontas

**Pausa de impacto (hit stop).** Reduz a escala de tempo a ~0,05 por 0,05–0,2 s medidos em **tempo real**. Regras: só começa com a escala exatamente 1 (pausa, menus e tutoriais nunca são interrompidos); intervalo mínimo ~0,35 s; duração máxima ~0,2 s; se outra tela mexer no tempo durante a pausa, abandona sem devolver o valor; um relógio de segurança que vê 0,05 fora de uma pausa devolve 1; nenhum outro código usa esse valor. Opção própria.

**Câmera lenta.** Escala 0,3 por ~0,7 s e um degrau de 0,65 por ~0,18 s antes de voltar a 1, em tempo real. Espera uma pausa de impacto em curso e só começa em tempo normal. Mesma trava de segurança (reconhece os dois valores fixos soltos).

**Soco de campo de visão.** Subtrai (impacto) ou soma (velocidade) alguns graus de uma vez e volta com queda quadrática. Aplicado **depois** da suavização do FOV e em tempo real, para funcionar durante câmera lenta. Segue a opção de tremor.

**Tremor de câmera.** Força e duração por evento; o mais forte em curso não é cortado por um mais fraco; atenua com a distância; sem teto artificial, mas calibrado em pixels de tela (a câmera distante pode fazer 1 u valer poucos pixels).

**Brilho nas bordas.** Cor pelo tipo do evento, força e duração pela magnitude; pulso contínuo para estados (vida baixa) e flash para eventos.

**Faíscas e cometa.** Partículas ou fita de UI que ligam origem e destino (recompensa → contador, compra → saldo). A fita não guarda histórico: cada ponto é o caminho avaliado `k × passo` atrás, e pontos anteriores à partida ficam de fora; bordas com alfa 0.

**Escala musical na cadeia.** Pentatônica ascendente (0, 2, 4, 7, 9, 12 semitons), fecha na oitava; soa bem em qualquer ordem. Aplique o tom depois do sorteio de variação do tocador.

**Contagem animada.** Interpola do valor antigo ao novo em 0,2–0,6 s com tique de som, soco e clarão; nunca mostra mais que o real.

**Cascata.** Cada filho ativo ganha grupo de opacidade, parte de alfa 0 e ~92% de escala e entra com overshoot, um atrás do outro; não mexe na posição (pertence ao layout); nova cascata restaura antes de recomeçar; restaure ao fechar a tela.

**Janela de recorte.** Para placas que "saem de dentro" de um painel, use uma máscara de retângulo cujo topo coincide com a borda do painel.

**Idle com sorteio.** Um diretor escolhe um botão por vez para respirar/brilhar, em vez de todos juntos.

## Trechos de referência (C#/Unity; adapte a outras engines)

```csharp
public delegate float EaseFn(float t);
public static class Easing
{
    public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
    public static float InQuad(float t) => t * t;
    public static float OutCubic(float t) { float u = 1f - t; return 1f - u * u * u; }
    public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
    public static float OutBack(float t, float s = 1.70158f) { float u = t - 1f; return 1f + u * u * ((s + 1f) * u + s); }
    public static float OutElastic(float t)
    {
        if (t <= 0f) return 0f; if (t >= 1f) return 1f;
        const float c = 2f * Mathf.PI / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c) + 1f;
    }
}
```

Teste das curvas: 0 em t=0, 1 em t=1, `OutBack` passa de 1 no meio, nenhuma devolve NaN em [0,1].

Botão com hover/pressionar/soltar (esqueleto): componente em cada `Selectable` que guarda a escala de repouso, anima por `unscaledDeltaTime` a partir do **valor atual**, volta ao repouso em `OnDisable` e se anexa a todos os botões por uma chamada estática sobre a raiz da UI. Veja o esqueleto completo em [exemplos/incrementant/receitas.md](exemplos/incrementant/receitas.md).

Disparo de partículas sem alocar: um sistema pré-criado por tipo; `Emit(params, n)` na posição, com a quantidade passando pela fachada das opções e pelo teto/cooldown por tipo; nada de `Instantiate`.
