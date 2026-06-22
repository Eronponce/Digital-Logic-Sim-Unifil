# Digital Logic Sim — Unifil Edition

A fork of [Sebastian Lague's Digital Logic Sim](https://github.com/SebLague/Digital-Logic-Sim) (v2.1.5) with additional features for both classroom use and the wider community.

---

## Downloads — v2.2.0

| Platform | Variant | Download |
|----------|---------|----------|
| Windows | community (offline) | `Digital-Logic-Sim-Unifil-Windows-v2.2.0-community.zip` |
| Windows | turma (cloud) | `Digital-Logic-Sim-Unifil-Windows-v2.2.0-turma.zip` |
| Windows | turma installer | `DigitalLogicSim-Unifil-Setup-v2.2.0-turma.exe` |
| Linux | community (offline) | `Digital-Logic-Sim-Unifil-Linux-v2.2.0.zip` |

See the [release notes](docs/12-RELEASE-v2.2.0-2026-06-21.md) for what's new.

---

## Variants

### Community
Fully offline. No login screen, no credentials required. Just download, extract, and run.
Suitable for anyone who wants to use the simulator without cloud features.

### Turma (cloud)
Includes Firebase Authentication (email/password) and Firestore cloud sync.
Designed for institutions that want students to log in, complete a profile, and have their work saved to the cloud.

To use this variant with your own Firebase project, see [Building from Source](#building-from-source) below.

---

## What's New in v2.2.0

- One-tick propagation delay per chip instance (predictable sequential circuits)
- Wire labels — drag text labels along any wire
- Wire style menu — per-pin colour and pattern (solid / dashed / double)
- IEEE standard gate symbols (AND, OR, NOT, etc.) as an alternative to rectangular style
- Canvas annotations — free-floating text blocks, double-click to edit
- Copy / paste preserves wire colours and labels
- Hotkey guide (F1)
- Cloud authentication and sync (turma variant)

Full details: [docs/12-RELEASE-v2.2.0-2026-06-21.md](docs/12-RELEASE-v2.2.0-2026-06-21.md)

---

## Building from Source

**Requirements:** Unity 6000.0.46f1

### Community build (no Firebase)

```powershell
.\scripts\package-students.ps1 -Community
```

Produces `Builds/Release/Digital-Logic-Sim-Unifil-Windows-vX.Y.Z-community.zip`.
The Firebase credential file is automatically stripped from the artifact.

### Turma build (with Firebase)

1. Create a Firebase project at [console.firebase.google.com](https://console.firebase.google.com)
2. Enable **Email/Password** authentication
3. Enable **Cloud Firestore**
4. Download your config files and place them:
   - `google-services.json` → project root
   - `google-services-desktop.json` → `Assets/StreamingAssets/`
5. Build:

```powershell
.\scripts\package-students.ps1
```

Produces a zip and a Windows installer under `Builds/Release/`.

### Linux build

```powershell
.\scripts\package-linux.ps1
```

---

## Project Structure

| Path | Purpose |
|------|---------|
| `Assets/Scripts/Game/` | Simulation engine, chip logic, interaction |
| `Assets/Scripts/Graphics/` | Rendering and UI menus |
| `Assets/Scripts/CloudSync/` | Firebase auth and Firestore sync |
| `Assets/Scripts/Description/` | Chip/wire/pin serialisation types |
| `scripts/` | PowerShell build and packaging scripts |
| `docs/` | Release notes |

---

## Credits

All core simulation engine work, rendering system, built-in chips, wire editing, undo/redo, bus support, LEDs, ROM, and the overall architecture are [Sebastian Lague's](https://github.com/SebLague/Digital-Logic-Sim) work.

The features listed in v2.2.0 are additions built on top of that base.
