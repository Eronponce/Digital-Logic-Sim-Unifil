---
name: repo-planning
description: Planejar arquitetura, sincronizacao, analytics, deploy e documentacao deste repositorio Unity. Use quando a tarefa envolver roadmap tecnico, avaliacao de backend, custo, acompanhamento de alunos, estrategia de sincronizacao, ou organizacao de contexto e Markdown para este projeto.
---

# Repo Planning

## Overview

Use este skill para transformar perguntas amplas sobre este repositorio em um plano tecnico executavel e em documentacao canonicamente organizada.

## Workflow

1. Ler `AGENTS.md`.
2. Ler `docs/00-INDEX.md`.
3. Se a tarefa tocar dados, sync ou analytics, inspecionar primeiro:
   - `Assets/Scripts/SaveSystem/Saver.cs`
   - `Assets/Scripts/SaveSystem/SaverCloudExtension.cs`
   - `Assets/Scripts/SaveSystem/Loader.cs`
   - `Assets/Scripts/CloudSync/FirestoreDataManager.cs`
   - `Assets/Scripts/CloudSync/FirebaseAuthManager.cs`
4. Distinguir claramente:
   - snapshot operacional
   - telemetria de aprendizagem
   - relatorio para professor
5. Preferir documentar decisoes em `docs/` e manter arquivos root antigos apenas como historico, salvo quando houver motivo forte para atualiza-los.

## Repo-Specific Guidance

- O editor correto e `Unity 6000.0.46f1`.
- A cena principal do build e `Assets/Build/DLS.unity`.
- No editor, os saves ficam em `TestData/`.
- A camada Firebase atual ainda nao deve ser tratada como fonte de verdade confiavel sem validar roundtrip completo de projeto mais chips.
- Para discussoes de custo, o padrao recomendado e `Firestore como banco principal` e `Google Sheets como camada de relatorio`, salvo se o usuario pedir outra direcao.

## Expected Outputs

Quando a tarefa for de planejamento, entregar pelo menos:

- estado atual resumido
- riscos e gaps principais
- recomendacao tecnica
- roadmap em fases
- arquivos Markdown atualizados em `docs/` se isso fizer parte do pedido
