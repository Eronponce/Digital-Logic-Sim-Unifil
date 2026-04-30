# Documentacao Canonica

Este diretorio concentra a visao atual do projeto. Ele existe porque varios arquivos Markdown na raiz ficaram uteis como historico, mas ja nao representam com precisao o estado real do codigo.

## Ordem recomendada de leitura

1. `AGENTS.md`
2. `docs/01-ARQUITETURA-E-EXECUCAO.md`
3. `docs/02-ESTADO-ATUAL-DOS-DADOS.md`
4. `docs/03-OPCOES-DE-BACKEND-E-CUSTO.md`
5. `docs/04-ROADMAP-IMPLEMENTACAO.md`
6. `docs/05-CONFIGURACAO-FIREBASE.md`
7. `docs/06-FASE-1-ROUNDTRIP-E-PERFIS.md`
8. `docs/07-AUTH-CADASTRO-E-RESET-2026-04-23.md`
9. `docs/09-BUILD-PUSH-E-RELEASE-2026-04-30.md`
10. `.agents/skills/repo-planning/SKILL.md`

## Resumo rapido

- O projeto e um fork/adaptacao do `Digital Logic Sim` em Unity.
- O editor correto hoje e `Unity 6000.0.46f1`.
- A cena principal usada no build e `Assets/Build/DLS.unity`.
- O app salva em `TestData/` no editor e em `Application.persistentDataPath` no build.
- Ja existe uma camada Firebase/Auth/Firestore na cena principal.
- A sincronizacao atual espelha snapshots salvos, mas ainda nao fornece telemetria pedagogica confiavel.

## Onde a documentacao antiga diverge do codigo atual

- `CONTEXTO_REPOSITORIO.md` cita versao de Unity, cena principal e estrutura de saves que nao batem com o codigo atual.
- A documentacao de Firebase presume restauracao completa dos projetos, mas o codigo atual baixa apenas `ProjectDescription` e nao baixa os chips do projeto.
- O botao de login aparece como Google no menu, mas o fluxo implementado hoje usa email e senha.

## Objetivo destes arquivos

- Dar um mapa real do projeto para futuras sessoes.
- Deixar claro o estado atual do banco e da sincronizacao.
- Comparar Firestore vs Google Sheets com foco em custo, simplicidade e confiabilidade.
- Registrar um roadmap pratico para deixar o banco sempre atualizado com o que os alunos fazem.
