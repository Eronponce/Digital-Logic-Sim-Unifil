# Digital Logic Sim - Unifil

Fork/adaptacao do projeto original `Digital Logic Sim`, com foco atual em sincronizacao cloud, autenticacao e futura analise do que os alunos fazem no simulador.

## Estado atual

- Engine: `Unity 6000.0.46f1`
- Cena principal: `Assets/Build/DLS.unity`
- Entrada de runtime: `Assets/Scripts/Game/Main/UnityMain.cs`
- Persistencia local: JSON em disco
- Persistencia cloud atual: Firebase Auth + Firestore

## Rodando o projeto

1. Abra o repositorio no `Unity 6000.0.46f1`.
2. Abra a cena `Assets/Build/DLS.unity`.
3. No editor, habilite `openInMainMenu` no objeto `Main` se quiser testar login/menu ao inves do projeto de teste.
4. Pressione Play.

## Documentacao canonica

- `AGENTS.md`
- `docs/00-INDEX.md`
- `docs/01-ARQUITETURA-E-EXECUCAO.md`
- `docs/02-ESTADO-ATUAL-DOS-DADOS.md`
- `docs/03-OPCOES-DE-BACKEND-E-CUSTO.md`
- `docs/04-ROADMAP-IMPLEMENTACAO.md`

## Observacao importante

Os arquivos Markdown antigos da raiz continuam uteis como historico, mas podem divergir do codigo atual em pontos como versao do Unity, cena principal, caminho de save e comportamento real da sincronizacao Firebase.

## Projeto original

O projeto original foi criado por Sebastian Lague como parte da serie [Exploring How Computers Work](https://www.youtube.com/playlist?list=PLFt_AvWsXl0dPhqVsKt1Ni_46ARyiCGSq).
