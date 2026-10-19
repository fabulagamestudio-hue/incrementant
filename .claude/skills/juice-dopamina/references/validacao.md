# Validação e revisão

Efeito de game feel não se valida só compilando. Valide em proporção ao risco e **declare o que não foi visto nem ouvido**.

## O que rodar

1. **Compilar** e conferir o console.
2. **Testes puros:** curvas, fórmulas de recompensa e proporção, tetos/agregação, escala por nível de opção, síntese de som (nível, pico, sem estalo).
3. **Caminho real em Play:** dispare o fato pelo caminho do jogo (mate o inimigo, compre o item, abra a tela), não só por um botão de teste.
4. **Veja o efeito:** captura **do que interessa** (a janela do jogo, não a tela inteira) na resolução real. Se o efeito é rápido, use câmera lenta ou captura por quadro.
5. **Meça o estado quando não der para ver:** FOV, escala de tempo, quantidade de partículas vivas, valor de um campo.
6. **Variações:** velocidade normal e alta, todos os níveis de opção (a informação sobrevive desligada?), pausa, troca de cena (nada vaza), menu aberto, coop se houver.
7. **Profiler:** o efeito não deve aparecer como custo relevante.

## Armadilhas de teste

- Entrar direto na cena de jogo pode pular o controlador de som da Splash: crie um para testar áudio.
- Jogador **invulnerável** ao abrir o Play, para não perder a run (e não gravar morte no save ou na telemetria).
- Estático sujo: um teste que depende de contagem pode falhar depois de um Play Mode; recarregue o domínio.
- Para simular input, use um dispositivo virtual e restaure as configurações originais depois.
- Eventos estáticos zerados na inicialização, ou o segundo teste herda inscrições.
- Nunca valide efeitos de captura de tela inteira; recorte a janela do jogo.

## O que declarar na entrega

- O que foi **visto** (captura), **medido** (estado) e **só testado por lógica**.
- O que **não foi ouvido** (sons) e o que não foi avaliado em playtest (valores provisórios).
- Limitações reais: sem opção própria, só em um modo, só uma nave, etc.

## Checklist de revisão de um efeito

- [ ] Ligado a um fato real? A recusa tem resposta diferente do sucesso?
- [ ] Proporcional à magnitude? Frequente = leve, raro = forte?
- [ ] Tratado nos níveis de opção, e a informação sobrevive desligada?
- [ ] Sem alocação por disparo; tetos, cadência e agregação definidos; testado em velocidade alta?
- [ ] Termina em estado exato (escala, alfa, tempo, cor) e se limpa ao desativar ou interromper?
- [ ] Tempo certo: UI em tempo não escalado; pausa de impacto/câmera lenta com trava de segurança?
- [ ] Legível na resolução e na distância reais, e sem esconder informação?
- [ ] Coerente com o cenário e a arte (nada cai no espaço; pixel art sem blur)?
- [ ] Som par conferido; texto novo respeita a localização?
- [ ] Documentado (mapa, guia, pendências, produção) e com valores marcados como provisórios?
