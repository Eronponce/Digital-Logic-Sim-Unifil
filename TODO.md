# TODO

## Auth e perfil do aluno

- [x] Corrigir o campo de senha mascarada no `Sign In` e no `Create Account`.
- [x] Adicionar reset de senha por email na tela de login.
- [x] Tornar obrigatorio para alunos informar `professor`, `nome` e `matricula`.
- [x] Salvar `teacherName`, `registrationNumber`, `studentName` e `profileCompleted` no Firestore.
- [x] Bloquear a ida ao menu principal enquanto o aluno logado nao completar o perfil.
- [x] Exibir professor e matricula no menu principal apos login.
- [x] Limpar a colecao `users` no Firestore para reiniciar os perfis salvos com o esquema novo.

## Validacao e acompanhamento

- [x] Validar compilacao do player Windows apos as mudancas de auth/perfil.
- [ ] Validar manualmente no build Windows:
  - cadastro de aluno novo
  - login com conta antiga sem perfil completo
  - reset de senha
  - logout/login com sync ativo
- [ ] Revisar UX final do formulario para telas menores.
- [ ] Regerar release `.exe` e `.zip` com esse fluxo novo depois da validacao manual.
