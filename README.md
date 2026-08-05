# Digital Logic Sim — Unifil Edition

A fork of [Sebastian Lague's Digital Logic Sim](https://github.com/SebLague/Digital-Logic-Sim) (v2.1.5) with additional features for both classroom use and the wider community.

---

## Downloads — v2.3.0

| Platform | Variant | Download |
|----------|---------|----------|
| Windows | community (offline) | `Digital-Logic-Sim-Unifil-Windows-v2.3.0-community.zip` |
| Windows | turma (cloud) | `Digital-Logic-Sim-Unifil-Windows-v2.3.0-turma.zip` |
| Windows | turma installer | `DigitalLogicSim-Unifil-Setup-v2.3.0-turma.exe` |
| Linux | community (offline) | `Digital-Logic-Sim-Unifil-Linux-v2.3.0-community.zip` |
| Linux | turma (cloud) | `Digital-Logic-Sim-Unifil-Linux-v2.3.0-turma.zip` |
| Mac ⚠️ | community (offline) | `Digital-Logic-Sim-Unifil-Mac-v2.3.0-community.zip` |
| Mac ⚠️ | turma (cloud) | `Digital-Logic-Sim-Unifil-Mac-v2.3.0-turma.zip` |

> ⚠️ **Build Mac não assinada.** Foi gerada sem acesso a um Mac físico (backend
> Mono do Unity), então não tem assinatura/notarização da Apple. Na primeira
> abertura o macOS vai bloquear com "desenvolvedor não identificado" —
> clique com o botão direito no `.app` → **Abrir** → confirme **Abrir** de
> novo. Instruções completas dentro do zip (`LEIA-ME-Mac.txt`).

See the [release notes](docs/13-RELEASE-v2.3.0-2026-07-22.md) for what's new.

---

## Variants

### Community
Fully offline. No login screen, no credentials required. Just download, extract, and run.
Suitable for anyone who wants to use the simulator without cloud features.

### Turma (cloud)
Includes email/password authentication and cloud sync of projects/chips.
Designed for institutions that want students to log in, complete a profile, and have their work saved to the cloud.

> **Backend (2026-07):** o Firebase foi substituído por **Supabase self-hosted**
> rodando no servidor Dell. A autenticação usa Supabase Auth (GoTrue) via
> `SupabaseAuthClient.cs` — REST puro sobre `HttpClient`/`UnityWebRequest`, sem
> SDK nativo. Os dados (perfil, projetos, chips) são salvos via a API `server-pg`
> (Postgres) em `MirrorApiClient.cs`. A URL do servidor é descoberta em runtime
> por `MirrorConfigProvider` (consulta um `config.json` publicado no GitHub, já
> que o túnel gratuito usado no backend não tem endereço fixo). O SDK do
> Firebase foi removido por completo do projeto (Auth e Firestore).
>
> Contas antigas do Firebase não têm senha portável — quem tinha conta lá
> recria com o **mesmo email** no Supabase; no primeiro login o servidor religa
> automaticamente os projetos antigos à conta nova (via `uid_aliases`).
>
> Ver `conexao-remota/ARQUITETURA.md` para o desenho completo da infraestrutura.

---

## What's New in v2.3.0

- **Firebase removed entirely** — cloud backend migrated to self-hosted Supabase
  (Postgres + GoTrue Auth). No more Firestore/Firebase Auth SDK in the project.
- Legacy Firebase accounts recreate with the same email and get their old
  projects/chips relinked automatically on first login.
- **Offline retry queue (Outbox)** — saves/deletes made while offline are queued
  and drained automatically once connectivity returns, instead of being lost or
  hanging forever.
- Cloud save status indicator (bottom bar): "Salvando...", "Salvo", "Sem
  conexão — N pendentes", "Erro" — always reflects the real queue state.
- **Logout no longer looks broken** — it used to flash back to the main screen
  before the (async) sign-out actually finished. Now a "Saindo..." overlay
  blocks the screen until it's genuinely done.
- Edit Profile screen: fixed a title overlapping the persistent header, and
  saving now returns to the main menu automatically instead of requiring an
  extra click.
- Turma picker (login, signup and edit profile) is now a proper scrollable
  list instead of a cramped single row that broke with more than ~3 turmas.
- Tab key now moves between fields on the login/signup/profile screens.

Full details: [docs/13-RELEASE-v2.3.0-2026-07-22.md](docs/13-RELEASE-v2.3.0-2026-07-22.md)

---

## Building from Source

**Requirements:** Unity 6000.0.46f1

### Community build (no Firebase)

```powershell
.\scripts\package-students.ps1 -Community
```

Produces `Builds/Release/Digital-Logic-Sim-Unifil-Windows-vX.Y.Z-community.zip`.
The Firebase credential file is automatically stripped from the artifact.

### Turma build (with cloud sync)

No Firebase config needed anymore. The client discovers the backend URL at
runtime (`MirrorConfigProvider`, reads a `config.json` published on GitHub) and
authenticates against the self-hosted Supabase instance — the anon key and
base URL fallback live in `Assets/Scripts/CloudSync/CloudConfig.cs`.

```powershell
.\scripts\package-students.ps1
```

Produces a zip and a Windows installer under `Builds/Release/`.

Local dev/test builds can be generated directly from Unity batchmode via
`Assets/Editor/LocalBuildScript.cs` (menu `Build/Build Windows Test App`, or
`-executeMethod DLS.EditorTools.LocalBuildScript.BuildWindowsPlayerRelease`).

### Linux build

```powershell
.\scripts\package-linux.ps1              # turma (cloud)
.\scripts\package-linux.ps1 -Community   # community (offline)
```

### Mac build (não assinada)

Requer o módulo **Mac Build Support (Mono)** instalado no Editor (baixável
pelo Unity Hub mesmo a partir de um host Windows — build IL2CPP/assinado
exigiria rodar o Editor num Mac de verdade, o que este projeto não faz).

```powershell
.\scripts\package-mac.ps1              # turma (cloud)
.\scripts\package-mac.ps1 -Community   # community (offline)
```

Gera um `.app` sem assinatura da Apple — o zip inclui um `LEIA-ME-Mac.txt`
com o passo a passo pra liberar no Gatekeeper na primeira abertura.

---

## Project Structure

| Path | Purpose |
|------|---------|
| `Assets/Scripts/Game/` | Simulation engine, chip logic, interaction |
| `Assets/Scripts/Graphics/` | Rendering and UI menus (`LoginMenu.cs`, `ProfileMenu.cs`, `MainMenu.cs`) |
| `Assets/Scripts/CloudSync/` | Supabase Auth (`SupabaseAuthClient.cs`) + REST client for `server-pg` (`MirrorApiClient.cs`, `MirrorConfigProvider.cs`), offline retry queue (`Outbox.cs`) |
| `Assets/Scripts/Description/` | Chip/wire/pin serialisation types |
| `scripts/` | PowerShell build and packaging scripts |
| `docs/` | Release notes |

### Cloud sync flow (turma variant)

`FirebaseAuthManager` (name kept for compatibility) drives sign-in/sign-up/logout
against `SupabaseAuthClient`. On successful auth, `FinalizeSignIn` upserts the
profile (`PUT /api/users/:uid/profile` — this is also where legacy-account
relinking fires), then `SaverCloudExtension.LoadAllProjectsFromCloud` restores
projects while `IsRestoringCloudProjects` blocks interaction behind a loading
overlay. Logout (`SignOut`) syncs pending local changes to the cloud first,
then clears the session — `IsSigningOut` blocks the UI with a "Saindo..."
overlay for the duration, since it's async and `IsLoggedIn` doesn't flip until
it finishes.

---

## Credits

All core simulation engine work, rendering system, built-in chips, wire editing, undo/redo, bus support, LEDs, ROM, and the overall architecture are [Sebastian Lague's](https://github.com/SebLague/Digital-Logic-Sim) work.

The features listed above are additions built on top of that base.
