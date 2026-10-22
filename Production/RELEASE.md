# Release — Acompanhamento de produção

**Projeto:** Incrementant
**Setor:** RELEASE
**Última avaliação:** 2026-10-22

## Resumo

Build Assistant (Iung) trazido do SpaceFall em 2026-10-22 (REQ-062) para preparar, validar, versionar e registrar o changelog das builds. Nenhuma build foi gerada ainda.

## Itens

| ID | Título | Estado | Prioridade | Evidência | Dependências | Verificado em |
|---|---|---|---|---|---|---|
| REL-001 | Build Assistant no projeto | VERIFIED | UNDEFINED | [BuildAssist Guide.md](../Assets/Iung/Build%20Assistant/BuildAssist%20Guide.md); compilação e janela conferidas na Unity | REQ-062 | 2026-10-22 |
| REL-002 | Versão da build na tela, em todas as cenas | VERIFIED | UNDEFINED | [HudGuide.md](../Assets/Project/Scripts/Runtime/UI/HudGuide.md); Play Mode no menu e na partida | REQ-063, REL-001 | 2026-10-22 |
| REL-003 | Histórico de versões no jogo | VERIFIED | UNDEFINED | [HudGuide.md](../Assets/Project/Scripts/Runtime/UI/HudGuide.md); Play Mode com versões de teste | REQ-064, REL-002 | 2026-10-22 |

## Detalhes

- REL-001: plugin em `Assets/Iung/Build Assistant` (caminho fixo no código), menu `Iung/Build Assistant`. Settings criado vazio em `Config/BuildAssistantSettings.asset`, sem o histórico do SpaceFall. Changelog e versão seguem a skill `build-assist-changelog`.
- REL-002: canto inferior direito, por cima de tudo e sem bloquear cliques. Mostra o rótulo que o Build Assistant grava antes de compilar; sem build pelo assistente, `v` + versão do Player Settings. Conferido só no editor.
- REL-003: clicar na versão abre a janela com `RuntimeChangelog` do Build Assistant; notas no idioma do jogo (pt-BR como tradução, en como principal). Sem build gravada, mostra que não há versões. Conferido só no editor, com dados de teste em memória.

## Riscos e bloqueios

- Nenhum.

## Não verificado

- Nenhuma build gerada pelo Build Assistant; identidade, pasta de saída, plataforma e versionamento ainda não definidos pelo desenvolvedor.
