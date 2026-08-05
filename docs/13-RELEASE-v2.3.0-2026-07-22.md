# Release v2.3.0 — 2026-07-22

v2.3.0 replaces the entire cloud backend of the **turma** variant — Firebase is
gone — and fixes several rough edges in the login/cloud-sync experience found
during real classroom testing.

| Variant | Who it's for | Cloud login |
|---------|-------------|-------------|
| **turma** | Institutions / classrooms | ✅ Supabase Auth (email/password, classes, cloud sync) |
| **community** | Everyone else | ❌ Offline only — no credentials, no login screen |

---

## Backend migration: Firebase → Supabase

- The `turma` variant no longer talks to Firebase at all. Authentication runs
  against a self-hosted **Supabase** instance (Postgres + GoTrue), reached
  through a small REST client (`SupabaseAuthClient.cs`, `MirrorApiClient.cs`) —
  no native SDK, just `HttpClient`/`UnityWebRequest`.
- The backend URL is discovered at runtime (`MirrorConfigProvider`) instead of
  being baked into the build, so the server can move without a new release.
- Students with an old Firebase account can't have their password ported
  (Firebase's password hashing can't be verified by Supabase), so they simply
  **create a new account with the same email**. On first login the server
  detects the match and relinks all their old projects and chips automatically
  — nothing is lost, no manual step needed beyond re-registering.
- The Firebase SDK (Auth + Firestore) has been removed from the project
  entirely; the `google-services*.json` credential files are no longer needed.

## Offline reliability

- **Outbox** — a persistent local queue for saves/deletes made while offline.
  Previously, going offline mid-save could leave the UI stuck on "Salvando..."
  forever, or silently drop the change. Now every save/delete is enqueued and
  automatically retried once the connection comes back — closing and reopening
  the app is safe, the queue survives.
- The save-status indicator in the bottom bar now derives directly from the
  queue: **Salvando(N)** while draining, **Sem conexão — N pendentes** in
  yellow when offline, **Erro** in red on failure, **Salvo** in green once
  clear.

## Login / logout / profile fixes

- **Logout used to look broken**: clicking it flashed back to the main menu
  immediately, because the underlying sign-out is asynchronous (it syncs
  pending changes to the cloud first) and the screen switched before that
  finished. Now a "Saindo..." overlay blocks the screen for the duration, and
  the transition to the login screen happens automatically once it's genuinely
  done.
- **Edit Profile**: removed a duplicate title that was rendering on top of the
  persistent "DIGITAL LOGIC SIM" header; saving now returns to the main menu by
  itself instead of requiring an extra click on "Back".
- **Turma picker** (login, signup, and edit profile): replaced the old
  single-row layout — which visually broke and overlapped text once there were
  more than ~3 turmas — with a proper scrollable list showing four at a time.
  Turmas that happen to share a display name are now disambiguated with the
  teacher/project name.
- Tab now moves focus between fields on the login, signup, and edit-profile
  forms (email → password → …, wrapping back to the first field).

---

## Mac build (added after initial release, unsigned)

A Mac build was added to this release using Unity's Mono backend (no Mac
hardware was used to produce it). It is **not signed or notarized by
Apple** — on first launch, macOS Gatekeeper will block it with a
"developer cannot be verified" message. This is expected: right-click the
`.app` → **Open** → confirm **Open** again to run it (see `LEIA-ME-Mac.txt`
inside the download). It runs natively on Intel Macs and via Rosetta 2 on
Apple Silicon (M1/M2/M3/M4). A properly signed, native Apple Silicon
(IL2CPP) build would require building from an actual Mac and isn't
available yet.

Full architecture notes for anyone standing up their own backend:
`conexao-remota/ARQUITETURA.md` and
`Digital-Logic-Sim-Teacher-Web/read-mirror-server/README.md` in the companion
[Teacher Web](https://github.com/Eronponce/Digital-Logic-Sim-Teacher-Web) repo.
