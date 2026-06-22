# Release v2.2.0 — 2026-06-21

## Overview

v2.2.0 is a major feature release built on top of Sebastian Lague's
Digital-Logic-Sim (v2.1.5). It ships in two variants:

| Variant | Who it's for | Cloud login |
|---------|-------------|-------------|
| **turma** | Unifil students | ✅ Firebase (email/password, turmas, cloud sync) |
| **community** | Everyone else | ❌ Offline only — no credentials, no login screen |

---

## What's New

### Simulation

**One-tick propagation delay per chip instance**
Each chip instance now introduces exactly one simulation tick of delay,
regardless of internal complexity. This makes sequential circuits
(shift registers, counters, oscillators) behave predictably and
consistently with real hardware propagation delay.

---

### Editor — Wires

**Wire labels**
Any wire can now carry a text label. Labels are created via the wire
context menu and can be dragged freely along the wire to any position.
Useful for annotating bus lines, control signals, and clock wires.

**Wire style menu**
Output pins expose a style selector (solid, dashed, dotted, etc.).
The style applies to the wire drawn from that pin, making signal types
visually distinct at a glance.

**Wire style inheritance from subchips**
When a wire connects to a subchip output pin that already has a style
defined, the wire inherits that style automatically.

**Wire style persists across reconnections**
Disconnecting and reconnecting a wire no longer resets the style back
to default. The style follows the output pin it originated from.

**Wire style and label preserved on copy-paste**
Duplicating chips or wire segments now correctly carries over both the
label text and the visual style.

---

### Editor — Pin Colours

**16-colour pin colour system**
Output pins can be assigned one of 16 colours via a hue-wheel picker.
The colour propagates through wires to visually trace signal paths
across the canvas.

**Colour inheritance for subchips**
When a chip is placed as a subchip, its output pin colours are
inherited by connected wires in the parent chip. Deep hierarchies
stay readable without manual recolouring at every level.

**Quick colour swatches**
The chip customisation menu now includes a swatch bar for the most
recently used colours, allowing one-click reuse without reopening the
full colour picker.

---

### Editor — Visuals

**IEEE standard gate symbols**
AND, OR, NOT, NAND, NOR, XOR, and XNOR gates now display their IEEE
standard symbols (D-shape body, bubble, etc.) as an alternative to the
default rectangular style. Toggle per-chip from the context menu.

**Canvas annotations**
Free-floating text blocks can be placed anywhere on the canvas.
Double-click to enter inline editing mode. Useful for circuit
documentation, labels for chip regions, and teaching diagrams.

**NOT gate triangle tip fix**
The triangle tip of NOT and buffer gates no longer visually pierces
the inversion bubble. The geometry is now clean at all zoom levels.

---

### Editor — UX

**Hotkey guide (F1)**
Pressing F1 opens a two-column reference panel listing every keyboard
shortcut in the editor. Dismisses with Escape or F1 again.

---

### Cloud & Auth (turma variant only)

**Firebase email/password authentication**
Students log in with an email and password. First-time users register
directly inside the app. Password reset is available from the sign-in
screen without leaving the application.

**Mandatory student profile**
After first login, students are required to complete their profile:
display name, student ID (matrícula), and class (turma). The app
blocks access to the main editor until the profile is complete.

**Turmas (class) system**
Teachers create and manage classes (turmas) through the Teacher Web
dashboard at logisim-eron.web.app. Students select their turma during
profile setup. The student profile stores `turmaId` and
`turmaProjectName` for class-scoped project assignment.

**Cloud sync — projects and chips**
All projects and their chips are synced to Firestore using atomic
WriteBatch operations. Sync happens on save, on project open, and on
logout. The restore guard prevents chip data from being overwritten
during the initial load sequence.

**Keep logged in**
A checkbox on the sign-in screen persists the session across app
restarts via PlayerPrefs. Unchecking signs the user out on next launch.

**Tab/Enter navigation in login form**
Tab moves focus from the email field to the password field. Enter
while the password field is focused submits the sign-in form.

**Auth race condition fix**
An extended restore guard prevents auth state events from firing
before the Firestore profile load completes, eliminating a class of
race conditions that caused the wrong screen to be shown on startup.

**Chip data loss fix**
A timing issue where cloud restore could overwrite locally unsaved
chip changes during the bundle-save sequence has been resolved.

**Turmas retry loop fix**
A permission error from Firestore no longer triggers an infinite retry
loop in the turma selector. The UI now shows a manual retry button
instead.

---

### Build & Distribution

**Community build (`-Community` flag)**
Running `.\scripts\package-students.ps1 -Community` produces a
standalone zip with the `DLS_COMMUNITY` scripting define active.
Firebase is never initialised; the login screen is never shown; users
go directly to offline mode. The `google-services-desktop.json`
credential file is automatically stripped from the artifact.

Developers who want to add their own Firebase backend can remove the
`DLS_COMMUNITY` define from Player Settings and supply their own
`google-services-desktop.json`.

---

## Artifacts

| File | Variant | Format |
|------|---------|--------|
| `Digital-Logic-Sim-Unifil-Windows-v2.2.0-turma.zip` | turma | Portable zip |
| `DigitalLogicSim-Unifil-Setup-v2.2.0-turma.exe` | turma | Windows installer |
| `Digital-Logic-Sim-Unifil-Windows-v2.2.0-community.zip` | community | Portable zip |

---

## Credits

This project is a fork of
[Sebastian Lague's Digital-Logic-Sim](https://github.com/SebLague/Digital-Logic-Sim)
(v2.1.5). All simulator engine work, rendering system, built-in chips,
wire editing, undo/redo, bus support, LEDs, ROM, and the overall
architecture are Sebastian's work. The features listed in this release
are additions made on top of that base.

---

## Notes for next release

- GIFs for each editor feature should be recorded and embedded above.
- Linux variant not yet built for v2.2.0 — run `package-linux.ps1` when needed.
- Teacher Web deploy: `firebase deploy --only hosting` from `teacher-web/dist`.
- Firestore rules deploy: `firebase deploy --only firestore:rules`.
- GitHub release: `gh release create vX.Y.Z file.zip --repo Eronponce/Digital-Logic-Sim-Unifil`.
