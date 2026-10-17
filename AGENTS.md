# AGENTS.md — Incrementant

Instruções para agentes de código (Codex e similares) neste projeto. As regras são as mesmas de `CLAUDE.md`, adaptadas às ferramentas de cada agente. Ao alterar uma regra em um dos arquivos, atualize o outro na mesma tarefa para que não divirjam.

A prioridade é entregar alterações completas, verificadas e fáceis de manter, preservando o trabalho que já estiver no diretório.

## Contexto do projeto

- Engine: Unity `6000.3.11f1`.
- Renderização: URP `17.3.0` (conforme `Packages/manifest.json`). Respeite o pipeline existente; não introduza materiais ou shaders de outro pipeline sem uma migração deliberada.
- Entrada: Input System `1.19.0`. Não introduzir o Input Manager legado.
- Código e conteúdo próprios ficam sob `Assets/Project`.
- Documentos de design, arte, narrativa, produção e planilhas de valores (`.docx`/`.xlsx`) ficam em `Docs/`, na raiz, fora de `Assets`; o índice é `Docs/README.md`. Não crie documentos desse tipo dentro de `Assets`.
- Cenas no build (ordem): `Assets/Project/Scenes/AntFarm.unity` (protótipo do formigário, gerada por `Incrementant/Setup/Criar cena AntFarm`), `Assets/Scenes/SampleScene.unity` (cena de template, a substituir).
- A serialização de assets está em Force Text. Ainda assim, cenas, prefabs e outros YAMLs da Unity exigem cuidado especial.
- Ambiente: Windows. Há PowerShell 5.1 (sem `&&`, `||` ou `??`) e Git Bash; use a sintaxe do shell escolhido.

O projeto está em fase inicial. Atualize este documento (e `CLAUDE.md`) quando uma decisão arquitetural concreta tornar alguma instrução obsoleta.

## Como começar uma tarefa

1. Leia este arquivo e procure `CLAUDE.md` ou `AGENTS.md` mais específicos em subpastas de `Assets` relacionadas à tarefa.
2. Restrinja buscas e leituras à pasta `Assets`, de preferência `Assets/Project`. Só explore outras pastas (`Packages`, `ProjectSettings`, `Library`, `Logs`, `UserSettings`, raiz do projeto etc.) quando o usuário pedir ou quando a tarefa depender diretamente delas. Isso evita ler arquivos gerados ou irrelevantes.
3. Inspecione os arquivos diretamente relacionados ao pedido antes de propor ou editar algo.
4. Se houver Git, confira `git status --short` e preserve todas as alterações existentes do usuário. A ausência de Git não é motivo para interromper o trabalho.
5. Confirme a versão da Unity em `ProjectSettings/ProjectVersion.txt` e os pacotes relevantes em `Packages/manifest.json` quando a tarefa depender deles.
6. Faça suposições pequenas e reversíveis para avançar. Pergunte ao usuário apenas quando faltar uma decisão de produto ou quando a ação tiver impacto destrutivo, externo ou difícil de reverter.

## Forma de trabalhar

- Responda e documente em português, salvo pedido em contrário.
- Trabalhe com autonomia até alcançar o resultado solicitado; não pare apenas na análise ou em um plano quando a implementação estiver autorizada.
- Prefira mudanças pequenas e focadas. Não faça refatorações laterais sem benefício necessário para a tarefa.
- Investigue a causa de um problema antes de aplicar correções. Não esconda sintomas com valores mágicos, delays ou tratamento genérico de exceções.
- Preserve compatibilidade com conteúdo serializado existente. Renomear classes, campos `[SerializeField]`, namespaces ou caminhos de assets pode quebrar referências.
- Não reverta, sobrescreva ou reorganize mudanças do usuário que não pertençam à tarefa. Leia um arquivo antes de sobrescrevê-lo.
- Use um diretório temporário da sessão para arquivos temporários (logs, scripts auxiliares, saídas intermediárias), nunca a raiz do projeto nem `Assets`.
- Ao final, informe objetivamente o que mudou, como foi validado e qualquer limitação real que permaneça. Referencie arquivos como links clicáveis (`caminho:linha`).

## Organização do conteúdo

Mantenha todo conteúdo autoral dentro de `Assets/Project`. Crie as pastas abaixo apenas quando forem necessárias:

```text
Assets/Project/
  Art/
  Audio/
  Data/
  Materials/
  Prefabs/
  Scenes/
  Scripts/
    Runtime/
    Editor/
    Tests/
  Settings/
  UI/
```

- Código de runtime nunca deve depender de `UnityEditor`.
- Ferramentas, inspectors, importadores e scripts de configuração de cena devem ficar em uma pasta `Editor`.
- Organize código por feature/domínio quando ele crescer, não por uma coleção ampla de tipos como `Managers` ou `Utils`.
- Use `Fabula.Incrementant` como namespace raiz para código novo. Acrescente o domínio quando ajudar, por exemplo `Fabula.Incrementant.Player`.
- Considere `.asmdef` quando houver uma fronteira útil entre runtime, editor e testes; não crie assemblies vazios ou excessivamente fragmentados.
- Documentação de código: mapa (`README.md`/`AI_GUIDE.md` por pasta, só "onde está"), guia de sistema (`<Sistema>Guide.md`, "como funciona") e `Assets/Project/Scripts/KnownIssues.md` (bugs e dívidas). Mudança de arquivo/classe/API atualiza o mapa; mudança de comportamento atualiza o guia.

## Padrões de código C#

- Siga as convenções usuais do C#: `PascalCase` para tipos e membros públicos, `camelCase` para parâmetros e variáveis locais e `_camelCase` para campos privados.
- Prefira campos privados com `[SerializeField]` a campos públicos usados apenas pelo Inspector.
- Mantenha componentes focados e dependências explícitas. Prefira composição a hierarquias profundas de herança.
- Separe configuração durável de estado de execução. Use `ScriptableObject` quando houver dados compartilhados ou conteúdo de design que realmente se beneficie disso.
- Cache referências usadas com frequência. Evite `Find*`, alocações e consultas globais em `Update`, `FixedUpdate` ou outros caminhos quentes.
- Use `Update` para leitura de input e lógica por frame; use `FixedUpdate` para interação consistente com física.
- Faça inscrição e desinscrição de eventos de forma simétrica, normalmente em `OnEnable` e `OnDisable`.
- Valide referências obrigatórias cedo e produza mensagens de erro que indiquem o objeto e a correção esperada.
- Evite singletons globais como escolha padrão. Quando um serviço global for necessário, deixe explícitos seu ciclo de vida e sua forma de inicialização.
- Comente decisões e restrições não óbvias; não narre linha por linha o que o código já expressa.

## Unity, cenas e assets serializados

- Não edite `Library`, `Temp`, `Logs`, `obj`, `UserSettings` nem arquivos de projeto gerados (`*.csproj`, `*.slnx`). Eles não são fonte do projeto.
- Não altere `Packages/packages-lock.json` manualmente para simular instalação de pacote. Modificações de dependência devem ser intencionais e coerentes com `Packages/manifest.json`.
- Preserve arquivos `.meta` e GUIDs. Ao mover ou renomear um asset, mova também seu `.meta`; ao criar um asset manualmente, deixe a Unity gerar o `.meta` quando possível.
- Não faça edição manual extensa do YAML de cenas, prefabs ou assets. Para mudanças estruturais, prefira um script de Editor idempotente que abra/crie o conteúdo, configure referências, marque como alterado e salve.
- Um script de setup deve poder rodar mais de uma vez sem duplicar GameObjects, componentes ou assets. Procure conteúdo existente por referência estável antes de criar.
- Coloque ferramentas temporárias ou permanentes de setup sob `Assets/Project/Scripts/Editor/Setup`, com namespace de Editor.
- Depois de executar uma ferramenta de setup, confira a cena/prefab resultante e remova apenas artefatos realmente temporários. Não remova uma ferramenta reutilizável sem motivo.
- Ao adicionar uma cena destinada ao jogo, confirme se ela deve entrar em `ProjectSettings/EditorBuildSettings.asset`; não presuma que toda cena deve estar no build.

## Validação

Valide em proporção ao risco da alteração. Não declare sucesso apenas porque os arquivos foram editados.

### Verificações rápidas

- Procure referências quebradas, duplicações e usos antigos com busca no código.
- Confira o diff quando Git estiver disponível.
- Para alterações somente em código, compile os projetos relevantes depois que a Unity tiver regenerado os arquivos de projeto:

```powershell
dotnet build Assembly-CSharp.csproj
dotnet build Assembly-CSharp-Editor.csproj
```

Arquivos `.csproj` da Unity podem estar desatualizados. Se o resultado divergir do Editor, a compilação/importação da Unity é a fonte de verdade.

### Validação pela Unity

- Use exatamente a versão registrada no projeto sempre que ela estiver instalada.
- Prefira batch mode para importação, compilação, execução de métodos de setup e testes automatizados.
- Grave logs no diretório temporário da sessão (ex.: `-logFile <temp>/unity.log`), inspecione erros e warnings relevantes e não deixe logs soltos em `Assets`.
- Se a Unity estiver aberta e bloquear o projeto, não apague lock files às cegas nem encerre o processo sem necessidade. Verifique o estado e use a instância existente quando for possível.
- Para mudanças visuais ou de cena, além de compilar, abra ou renderize o resultado e confira hierarquia, referências, enquadramento, materiais, iluminação e comportamento em Play Mode quando aplicável. Se não for possível verificar visualmente, diga isso explicitamente.
- Para lógica testável, adicione testes EditMode ou PlayMode em `Assets/Project/Scripts/Tests` conforme a dependência de runtime.

## Acompanhamento de produção

A pasta `Production/`, na raiz do repositório, registra **em que estado a produção está**: `PRODUCTION_STATUS.md` (resumo central, derivado dos demais), `REQUESTS.md` (solicitações do desenvolvedor) e um arquivo por setor (`ART.md`, `PROGRAMMING.md`, `GAME_DESIGN.md`, `AUDIO.md`, `RELEASE.md`). O Fábula Production Hub lê esses arquivos direto do repositório, então nomes, cabeçalhos, colunas, IDs e estados seguem um formato fixo. As regras e os modelos estão na skill `fabula-production-docs`: carregue-a antes de criar ou editar qualquer arquivo da pasta.

- `Production/` é um tipo de documento à parte, ao lado de mapa, guia e `KnownIssues.md`: guarda estado, pendência, bloqueio, risco e solicitação. Onde fica o código continua nos mapas, como funciona nos guias de sistema e os bugs em `Assets/Project/Scripts/KnownIssues.md`. `Production/` linka esses arquivos, não os copia, e não tem `BUGS.md`.
- Ao terminar uma tarefa que muda o estado de um item (sistema ou conteúdo novo, funcionalidade entregue, validação feita, bloqueio resolvido, decisão de design), atualize os setoriais afetados, o `REQUESTS.md` se ela atende uma solicitação e, por último, o `PRODUCTION_STATUS.md`. Correções pequenas e refactors sem efeito no estado não pedem atualização.
- Pedido novo do desenvolvedor que não seja uma correção pontual entra em `REQUESTS.md` como `PLANNED`, sem prioridade, prazo nem critério de aceitação que ele não tenha dito.
- Existir script, prefab ou guia prova no máximo `IMPLEMENTED`. `VERIFIED` pede a validação como evidência (teste executado, validador, Play Mode, build, relato do desenvolvedor), com data.
- Não refaça a varredura do projeto a cada tarefa; reavalie tudo só a pedido. Commit e push de `Production/` seguem a regra geral: só com pedido explícito.

## Critérios de conclusão

Uma tarefa está concluída quando:

- o comportamento solicitado foi implementado, não apenas esboçado;
- os arquivos alterados pertencem ao escopo da tarefa;
- o código compila ou qualquer impedimento de validação está claramente descrito;
- cenas, prefabs e assets continuam com referências válidas;
- o caminho principal foi exercitado e casos de borda relevantes foram considerados;
- não ficaram scripts, logs, assets duplicados ou outras sobras temporárias;
- a entrega resume mudanças, validações executadas e pendências reais.

## Ações que exigem cautela adicional

- Excluir ou substituir assets, cenas, prefabs, dados salvos ou configurações de projeto.
- Trocar versão da Unity, render pipeline, backend de input ou pacotes centrais.
- Alterar Player Settings, plataformas de build, qualidade, física ou camadas/tags com efeito global.
- Regenerar uma cena inteira para corrigir um detalhe localizado.
- Encerrar a Unity, apagar locks ou limpar caches enquanto houver uma sessão ativa.
- Executar operações Git destrutivas ou descartar alterações não confirmadas.
- Fazer commit, push ou qualquer ação externa sem pedido explícito.

Nesses casos, determine primeiro o alvo exato, escolha a alternativa reversível sempre que possível e peça confirmação quando o pedido original não autorizar claramente o impacto.

## Produção

`Production/` (raiz, fora de `Assets`) guarda o acompanhamento de produção (estado, pendências, solicitações), lido pelo Fábula Production Hub. Atualize-a após tarefas que mudem o estado de um item, seguindo a skill `fabula-production-docs`. Não é guia técnico nem registro de bugs (bugs ficam em `Assets/Project/Scripts/KnownIssues.md`).
