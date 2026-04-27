# Arquitetura e Execucao

## Stack atual

- Engine: `Unity 6000.0.46f1`
- Linguagem principal: `C#`
- Cena principal do build: `Assets/Build/DLS.unity`
- Entrada de runtime: `Assets/Scripts/Game/Main/UnityMain.cs`
- Persistencia local: JSON em disco
- Persistencia cloud atual: Firebase Auth + Firestore

## Risco importante de plataforma

A documentacao oficial do Firebase para Unity ainda descreve o workflow desktop como `beta` e orientado a desenvolvimento. Para piloto isso pode ser aceitavel, mas para distribuicao ampla precisamos tratar esse ponto com cuidado.

## Fluxo de execucao relevante

1. `UnityMain.Awake()` reseta estaticos, cria `AudioState` e chama `Main.Init(...)`.
2. `Main.Init(...)` garante diretorios de save e carrega `AppSettings`.
3. Em build, `UnityMain` sempre abre o menu principal.
4. No editor, a cena atual abre `MainTest` por padrao enquanto `openInMainMenu` estiver desligado.
5. `Main.Update()` faz o loop principal da aplicacao e delega para `Project.Update()`.
6. `Project.StartSimulation()` cria a thread dedicada da simulacao.

## Como rodar no editor

1. Abrir o projeto no `Unity 6000.0.46f1`.
2. Abrir a cena `Assets/Build/DLS.unity`.
3. Se o objetivo for testar login/menu, habilitar `openInMainMenu` no objeto `Main`.
4. Pressionar Play.

Observacao: com a configuracao atual da cena, o editor tende a abrir direto o projeto de teste `MainTest`, enquanto builds sempre entram no menu principal.

## Onde os dados ficam

- Editor: `TestData/`
- Build: `Application.persistentDataPath`

Arquivos locais principais:

- `TestData/AppSettings.json`
- `TestData/Projects/<Projeto>/ProjectDescription.json`
- `TestData/Projects/<Projeto>/Chips/*.json`

## Pecas de runtime ligadas ao Firebase

- A cena principal ja contem o objeto `FirebaseManagers`.
- Esse objeto ja referencia `FirebaseManager`, `FirebaseAuthManager` e `FirestoreDataManager`.
- O menu principal ja chama `LoginMenu.Initialize()` e alterna entre tela de login e tela principal.

## Como gerar e compartilhar builds

Hoje nao existe pipeline automatizado de build no repositorio. O caminho atual e manual:

1. Abrir `Build Settings`.
2. Confirmar que `Assets/Build/DLS.unity` esta na lista de cenas.
3. Escolher o alvo inicial, com foco em `Windows x86_64`.
4. Gerar o build.
5. Distribuir a pasta completa do build, nao apenas o `.exe`.

## Formas praticas de disponibilizar para outras pessoas

- `GitHub`
  - bom para colaboradores e versionamento do codigo
  - usar Releases para distribuir builds zipados
- `itch.io`
  - bom para testes com usuarios finais
  - entrega simples de builds desktop
- distribuicao interna por pasta zipada
  - suficiente para piloto pequeno com professores e alunos

## O que falta para a entrega ficar profissional

- script de build automatizado
- checklist de release por plataforma
- documentacao de configuracao Firebase por ambiente
- regras versionadas de Firestore/Auth
- processo claro para publicar build e rollback
