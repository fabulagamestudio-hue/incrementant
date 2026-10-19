# Arquitetura do feedback (genérica)

Objetivo: um núcleo pequeno, sem acoplar a simulação ao visual, que qualquer feature nova use em poucas linhas. Os nomes abaixo são papéis; use os do projeto.

## Fluxo: fato → diretor → efeito

```text
Simulação  --evento de fato-->  Diretor de feedback  --despacha-->  Efeitos (UI, mundo, câmera, tempo, som)
(não conhece o visual)          (assina e desassina)               (pequenos, com pool e tetos)
```

- **Evento de fato:** diz o quê, onde, quem e quanto (`Morreu(inimigo, posição, escudos)`, `Coletou(tipo, quantia, posição)`, `Comprou(item, custo)`). Nunca carrega tipo visual. Sem ouvinte, não custa nada.
- **Só emita onde há posição ou magnitude naturais.** Nem todo som ou fato precisa de efeito visual.
- **Eventos estáticos** (globais por tipo) evitam procurar objetos: o diretor assina sem referência a cena ou prefab. Zere-os na inicialização do domínio para não herdar inscrições entre execuções.
- **Inscrição simétrica:** assine em `OnEnable`, desassine em `OnDisable`.
- **Diretor único:** uma classe cria-se sozinha (inicialização depois da primeira cena), atravessa trocas de cena e mantém um mapa gatilho → efeito. Em troca de cena, zera sequências, números pendentes e texto flutuante.
- **Estados de entidade por timestamp:** reações corporais (piscar, squash) são função do tempo e de um instante guardado na entidade (`ultimoDano`), calculadas ao desenhar. Sem corrotina por entidade.

## Ponto único das opções

Nenhum efeito lê as configurações diretamente. Uma fachada responde:

```text
Movimento?  (há qualquer movimento/partícula)      Forte? (tremor, flash, pausa, câmera)      Escala(n) (quantidade em reduzido)
```

Cada família com opção própria (tremor, pausa de impacto, números, marcadores, alerta de dano, brilho de coleta) tem um interruptor lido nessa fachada. Facilita trocar a política e testar. Som não depende da opção visual.

## Agregação e tetos

- Eventos repetidos (dano, coleta, produção) são **agregados por janela** curta e por alvo/sala: o primeiro aparece na hora, os seguintes somam e saem juntos.
- Tetos por tipo (simultâneos, por segundo) e **intervalo mínimo** entre disparos. Medidos em tempo real, não em passo de simulação, para não explodir em velocidade alta.
- Não emita fora da vista da câmera.
- Pool para números, partículas e marcadores. Se acabar, recicle o mais antigo.

## Tempo

- UI e overlays usam tempo não escalado.
- Pausa de impacto e câmera lenta: um único dono de cada uma, com regras em [receitas.md](receitas.md). Nenhum outro código mexe na escala de tempo.
- Efeito que atravessa tela pausada (Crafting, Hangar, menu) roda em tempo real.

## Tween leve

Sem alocação por animação: lista pré-dimensionada de estados (alvo, início, fim, duração, curva) atualizada num único componente, ou uma corrotina por componente de UI (poucas instâncias, fora do caminho quente). **Não** use corrotina por partícula, por entidade ou por número flutuante.

## Montagem de cena e UI

- Serviços de runtime criados em código sob um objeto próprio, ou por script de setup idempotente. Nada de editar YAML à mão.
- UI gerada por script: acrescente comportamento em runtime (anexar a todos os botões) para sobreviver à recriação.
- Prefabs de efeito num diretório de recursos próprio; se faltarem, o log diz qual comando do setup rodar.
- Texto de tela de feedback: só números e símbolos, ou chaves de localização do projeto.

## Testes

- Lógica pura: curvas (0 em t=0, 1 em t=1, sem NaN), fórmulas de recompensa e proporção, tetos e agregação, escala nos níveis de opção, determinismo com semente.
- Teste de síntese de som: nível, pico e ausência de estalo.
- Teste de existência dos gatilhos por reflexão, se o laboratório de áudio os dispara assim.
- Detalhes em [validacao.md](validacao.md).
