# Build, Push e Release (2026-04-30)

## Objetivo

Registrar o processo pratico que funcionou para gerar build, fazer push e publicar release no GitHub neste repositorio.

## Estado atual no repositorio

- Versao do app no codigo: `Assets/Scripts/Game/Main/Main.cs` (`DLSVersion` e `LastUpdatedString`).
- Build local em lote via Unity batch mode:
  - `scripts/build-windows.ps1`
  - `scripts/build-linux.ps1`
- Empacotamento de release:
  - `scripts/package-students.ps1` (Windows setup + zip portatil)
  - `scripts/package-linux.ps1` (zip Linux)
- Saida de artefatos: `Builds/Release/`.

## Pre-requisitos

1. Unity `6000.0.46f1` instalado.
2. Linux Build Support instalado na mesma instalacao do Unity usada no build Linux.
3. Inno Setup 6 para gerar instalador Windows.
4. `gh` autenticado na conta correta.
5. Projeto Unity fechado durante os builds batch (`Temp/UnityLockfile` nao pode estar ativo por processo real).

## Fluxo de versao e release

### 1. Atualizar versao

Atualizar:

- `Assets/Scripts/Game/Main/Main.cs`
- `scripts/package-students.ps1`
- `scripts/package-linux.ps1`

### 2. Gerar artefatos Windows

```powershell
rtk proxy "powershell -ExecutionPolicy Bypass -File scripts/package-students.ps1 -Configuration release"
```

Arquivos esperados:

- `Builds/Release/DigitalLogicSim-Unifil-Setup-vX.Y.Z.exe`
- `Builds/Release/Digital-Logic-Sim-Unifil-Windows-vX.Y.Z.zip`

### 3. Gerar artefato Linux

```powershell
rtk proxy "powershell -ExecutionPolicy Bypass -File scripts/package-linux.ps1 -Configuration release"
```

Arquivo esperado:

- `Builds/Release/Digital-Logic-Sim-Unifil-Linux-vX.Y.Z.zip`

Se houver mais de uma instalacao do Unity, forcar a que possui Linux support:

```powershell
$env:UNITY_EXE='C:\Eron_Lab\Unity\6000.0.46f1\Editor\Unity.exe'
rtk proxy "powershell -ExecutionPolicy Bypass -File scripts/package-linux.ps1 -Configuration release"
```

### 4. Commit e push

```powershell
rtk proxy "powershell -NoProfile -Command git add <arquivos>; git commit -m <mensagem>; git push origin main"
```

### 5. Tag e release

```powershell
rtk proxy "powershell -NoProfile -Command git tag -a vX.Y.Z -m vX.Y.Z; git push origin vX.Y.Z"
```

```powershell
rtk proxy "powershell -NoProfile -Command gh release create vX.Y.Z Builds/Release/<asset1> Builds/Release/<asset2> Builds/Release/<asset3> --title vX.Y.Z --generate-notes --repo Eronponce/Digital-Logic-Sim-Unifil"
```

## Aprendizados importantes desta execucao

1. O `gh` pode mirar o remoto errado quando existe `upstream`; use `--repo Eronponce/Digital-Logic-Sim-Unifil`.
2. Para Linux, o Unity usado precisa ter `LinuxStandaloneSupport`; quando necessario, definir `UNITY_EXE`.
3. `Temp/UnityLockfile` pode ficar residual apos falha; confirmar que nao existe processo Unity aberto antes de remover.
4. Nesta maquina, comandos shell devem respeitar a regra de prefixo `rtk` descrita em `C:\Users\eronp\.codex\RTK.md`.

## Checklist rapido de release

1. Versao atualizada no codigo e scripts.
2. Build Windows release concluido.
3. Build Linux release concluido.
4. Artefatos `Builds/Release` gerados com a versao correta.
5. Commit enviado para `origin/main`.
6. Tag publicada.
7. GitHub Release criada com os tres artefatos.
