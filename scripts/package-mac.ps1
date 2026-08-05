param(
    [ValidateSet("dev", "release")]
    [string]$Configuration = "release",

    [switch]$SkipBuild,
    [switch]$Community
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$buildDir = Join-Path $repoRoot "Builds\Mac"
$releaseDir = Join-Path $repoRoot "Builds\Release"
$appPath = Join-Path $buildDir "Digital-Logic-Sim-Unifil.app"
$version = "2.3.0"
$variant = if ($Community) { "community" } else { "turma" }
$archiveRootName = "Digital-Logic-Sim-Unifil-Mac-v$version-$variant"
$stageDir = Join-Path $releaseDir $archiveRootName
$zipPath = Join-Path $releaseDir "$archiveRootName.zip"
$readmePath = Join-Path $stageDir "LEIA-ME-Mac.txt"

if (-not $SkipBuild) {
    $buildArgs = @($Configuration)
    if ($Community) { $buildArgs += "-Community" }
    & (Join-Path $repoRoot "build-mac.bat") @buildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Mac build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path $appPath)) {
    throw "Mac build output was not found: $appPath"
}

New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

if (Test-Path $stageDir) {
    Remove-Item -LiteralPath $stageDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $stageDir | Out-Null
Copy-Item -Path $appPath -Destination $stageDir -Recurse -Force

$readme = @(
    "Digital Logic Sim Unifil - build para Mac (nao assinada pela Apple)",
    "",
    "IMPORTANTE - este build usa o backend Mono do Unity, gerado sem acesso a",
    "um Mac fisico. Isso significa que ele NAO tem assinatura/notarizacao da",
    "Apple. Na primeira vez que voce abrir, o macOS vai bloquear com uma",
    "mensagem do tipo 'nao foi possivel verificar o desenvolvedor' ou",
    "'Digital-Logic-Sim-Unifil nao pode ser aberto'. Isso e esperado - o app",
    "nao e malicioso, so nao tem o selo pago da Apple.",
    "",
    "Como abrir mesmo assim:",
    "  1. Va ate a pasta onde extraiu o .app.",
    "  2. Clique com o botao direito (ou Control+clique) em",
    "     Digital-Logic-Sim-Unifil.app e escolha 'Abrir'.",
    "  3. Na janela de aviso, clique em 'Abrir' de novo (esse passo so e",
    "     necessario na primeira vez).",
    "  Alternativa: Ajustes do Sistema > Privacidade e Seguranca > role ate",
    "  o aviso sobre o app bloqueado > 'Abrir Assim Mesmo'.",
    "",
    "Compatibilidade: builds Mac deste projeto rodam nativamente em Mac Intel.",
    "Em Mac com chip Apple (M1/M2/M3/M4) rodam via Rosetta 2 (o macOS oferece",
    "para instalar automaticamente na primeira execucao de um app Intel)."
) -join [Environment]::NewLine

Set-Content -Path $readmePath -Value $readme -Encoding UTF8

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path $stageDir -DestinationPath $zipPath -CompressionLevel Optimal
Remove-Item -LiteralPath $stageDir -Recurse -Force

Write-Host "Mac release zip: $zipPath"
