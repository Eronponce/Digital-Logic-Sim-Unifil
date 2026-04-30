# AGENTS.md

## Purpose

Repository instructions for Codex and other agents. Follow this file first.

## Read Order

1. `docs/00-INDEX.md`
2. `docs/01-ARQUITETURA-E-EXECUCAO.md`
3. `docs/02-ESTADO-ATUAL-DOS-DADOS.md`
4. `docs/03-OPCOES-DE-BACKEND-E-CUSTO.md`
5. `docs/04-ROADMAP-IMPLEMENTACAO.md`
6. `docs/09-BUILD-PUSH-E-RELEASE-2026-04-30.md`
7. `.agents/skills/repo-planning/SKILL.md`

If anything conflicts with older Markdown files in the repo root, prefer `AGENTS.md` and the files under `docs/`.

## Repo Facts

- Project type: Unity desktop app.
- Required editor: `Unity 6000.0.46f1`.
- Main scene in build settings: `Assets/Build/DLS.unity`.
- Runtime entry point: `Assets/Scripts/Game/Main/UnityMain.cs`.
- In the editor, save data goes to `TestData/`.
- In builds, save data goes to `Application.persistentDataPath`.
- The main scene already contains a `FirebaseManagers` object with `FirebaseManager`, `FirebaseAuthManager`, and `FirestoreDataManager`.
- Cloud sync is still experimental and must not be treated as a reliable source of truth until full project plus chip roundtrip is validated.

## Working Rules

- Before changing persistence or analytics, inspect:
  - `Assets/Scripts/SaveSystem/Saver.cs`
  - `Assets/Scripts/SaveSystem/SaverCloudExtension.cs`
  - `Assets/Scripts/SaveSystem/Loader.cs`
  - `Assets/Scripts/SaveSystem/SavePaths.cs`
  - `Assets/Scripts/CloudSync/FirestoreDataManager.cs`
  - `Assets/Scripts/CloudSync/FirebaseAuthManager.cs`
- Separate snapshot data from event telemetry. Project and chip JSON alone are not enough for classroom analytics.
- Prefer writing canonical documentation in `docs/` instead of expanding legacy root-level Markdown files.
- Keep assumptions explicit when discussing backend cost, deployment, student analytics, or classroom workflows.

## Validation

- Use Play Mode in `Assets/Build/DLS.unity` for manual validation.
- In the editor, `UnityMain` currently opens a test project unless `openInMainMenu` is enabled on the `Main` object.
- There is no CI/CD pipeline yet. Local scripted build/release flow is documented in `docs/09-BUILD-PUSH-E-RELEASE-2026-04-30.md`.
