# Release v2.1.11 - 2026-06-18

## Resumo

Release foca no sistema de turmas, persistencia de sessao e usabilidade do login.

## O que mudou

- **Sistema de Turmas**: aluno seleciona turma (class) no perfil; turmas gerenciadas pelo professor no Teacher Web
- **Forcado a completar perfil**: alunos sem turmaId sao redirecionados para tela de perfil ao fazer login
- **ProfileMenu atualizado**: tela "Edit Profile" no jogo agora mostra seletor de turma em vez de lista fixa de professores
- **Manter logado**: checkbox na tela de login persiste sessao via PlayerPrefs
- **Tab/Enter no login**: Tab muda foco email→senha; Enter na senha dispara login
- **Pre-preenchimento do formulario**: nome e matricula ja salvos aparecem ao abrir tela de completar perfil
- **Versao atualizada**: `v2.1.11`, data `18 Jun 2026`

## Teacher Web

- Tela "Turmas" adicionada no painel do professor (botao "Turmas" no header)
- Deploy feito em: https://logisim-eron.web.app
- Firestore rules atualizadas: colecao `/turmas` com read=autenticado, write=professor
- Bug corrigido: `getDocs` chamado sem argumento extra em `listTurmas` e `listStudentsByTurma`

## Firestore

- Nova colecao `/turmas/{turmaId}`: campos `teacherName`, `projectName`, `displayName`, `active`, `createdByUid`
- Perfil do aluno: campos `turmaId` e `turmaProjectName` adicionados
- `profileCompleted` agora exige `turmaId` nao-vazio para alunos

## Bugs corrigidos

- Seed do formulario de perfil rodava com perfil Offline antes do Firestore carregar → nome mostrava "Offline"
- `UpdateStudentProfileAsync` validava sem `turmaId` → turmas sem professor (ex: "Outros") nao salvavam
- `ProfileMenu` nao mostrava seletor de turma

## Artefato

- `Builds/Digital-Logic-Sim-Unifil-v2.1.11-Windows.zip` (48 MB)
- GitHub Release: https://github.com/Eronponce/Digital-Logic-Sim-Unifil/releases/tag/v2.1.11

## Aprendizados / Notas para proximas releases

- Deploy do Teacher Web: `firebase deploy --only hosting` (build em `teacher-web/dist`)
- Deploy de regras: `firebase deploy --only firestore:rules` — gratis no Spark
- Release no GitHub: `gh release create vX.Y.Z arquivo.zip --repo Eronponce/Digital-Logic-Sim-Unifil`
- `gh` pode mirar remote errado quando existe `upstream`; sempre usar `--repo`
- Lembrar de atualizar `Main.cs` (DLSVersion + LastUpdatedString) antes do build final
- Unity headless build: `Unity.exe -batchmode -quit -projectPath . -executeMethod DLS.EditorTools.LocalBuildScript.BuildWindowsPlayerDev`
