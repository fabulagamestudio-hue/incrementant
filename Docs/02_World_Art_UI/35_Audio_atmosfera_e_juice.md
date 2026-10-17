# 35 — Áudio atmosfera e juice

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 35. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

## Direção de áudio definida

Música calma e tranquila, com tom cozy e agradável para acompanhar uma sessão longa. Os sons do formigueiro compõem o ambiente: passos, corte de folhas, escavação, transporte e descarga de terra, trabalho no fungo, cuidados com crias, água e interface. O jogo deve parecer vivo mesmo quando o jogador só observa.

Usar variações curtas e limites de simultaneidade para centenas de ações não virarem ruído constante. Sugerir intensidade conforme câmera e distância. Eventos de risco podem usar alertas reconhecíveis sem transformar a trilha em tensão permanente.

## Feedback visual abundante e legível

| Ação | Resposta visual e sonora proposta |
| --- | --- |
| Clique e seleção | Pulso de seleção, contorno do objeto, pequeno som e texto contextual |
| Pintura | Prévia colorida, custo acompanhando o pincel e confirmação com preenchimento animado |
| Compra | Texto de custo saindo do saldo, destaque do nó e transformação no objeto correspondente |
| Entrega e produção | Carga descarregada, partículas pequenas e indicação de nutrientes gerados no cultivo |
| Nascimento | Emergência da adulta, pulso no contador e som leve de conquista |
| Nova classe | Destaque da silhueta, breve apresentação do papel e efeito de desbloqueio |
| Obra concluída | Realce da área, desaparecimento da marca de obra e início das tarefas |
| Dano ou afogamento | Sinal de risco, reação corporal e leitura clara da causa |
| Decomposição | Fragmentos e redução progressiva do volume das pilhas |

A intenção expressa é que cada clique tenha animações de texto e feedbacks visuais ricos. Agrupar efeitos repetidos de produção evita inundar a tela de textos que escondam formigas. Câmera, números e caminhos precisam continuar legíveis. As animações reforçam aquilo que realmente ocorreu na simulação.

## Direção de sensação e legibilidade

A música e os sons devem sustentar um clima calmo e acolhedor. A atividade do formigueiro preenche o mundo, sem transformar cada passo de cada indivíduo em disputa por atenção. A referência é a sensação cozy, sem especificar instrumentos, formato de arquivo ou produção musical.

O juice reforça cada clique e cada conquista. Textos animados, destaque de seleção, cargas, fungo crescendo e transformações de sala precisam acompanhar um resultado verdadeiro. Compra recusada recebe resposta diferente de compra concluída. Uma nova classe merece um momento reconhecível.

A direção de arte permanece 2D em pixel art, com papéis distinguíveis e materiais legíveis. Não se fixa aqui resolução, paleta, ferramenta de animação ou pipeline de áudio. Opções para reduzir efeitos e controlar música separadamente são propostas de conforto, além dos volumes já definidos.

---

[← 34 — Interface durante a partida](../02_World_Art_UI/34_Interface_durante_a_partida.md) · [36 — Escopo completo e demonstração da jam →](../03_Production/36_Escopo_completo_e_demonstracao_da_jam.md)
