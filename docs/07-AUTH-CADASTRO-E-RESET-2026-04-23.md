# Auth, Cadastro e Reset de Senha

Data: `2026-04-23`

## Objetivo

Registrar a primeira iteracao do fluxo de autenticacao com dados minimos do aluno:

- senha mascarada funcionando em `Sign In` e `Create Account`
- reset de senha por email
- cadastro obrigatorio de `nome`, `matricula` e `professor`
- bloqueio do menu principal ate o perfil do aluno ficar completo

## Mudancas implementadas

### 1. Campo de senha

O `LoginMenu` deixou de resincronizar o `InputFieldState` da senha a cada frame.
O campo agora usa o texto real no estado interno e apenas mascara a exibicao com `*`.

Impacto:

- o aluno consegue digitar normalmente com a senha escondida
- o mesmo comportamento vale para `Sign In` e `Create Account`

### 2. Reset de senha

Foi adicionado um botao `Reset Password` na tela de `Sign In`.

Fluxo:

1. aluno informa o email
2. clica em `Reset Password`
3. o app chama `FirebaseAuth.SendPasswordResetEmailAsync(...)`
4. a UI mostra mensagem de sucesso ou erro

### 3. Perfil obrigatorio do aluno

Os perfis de aluno agora guardam e validam:

- `studentName`
- `registrationNumber`
- `teacherName`
- `teacherLookupKey`
- `profileCompleted`

Professores aceitos no cadastro do aluno:

- `ERON`
- `GUSTAVO`

### 4. Contas antigas

Contas antigas nao foram descartadas no nivel de autenticacao.
Se o usuario existir no Firebase Auth, mas o documento `users/{uid}` vier sem `teacherName` ou `registrationNumber`, o app entra em `Complete Profile` antes de liberar o menu principal.

### 5. Menu principal

Quando o aluno completa o perfil, o menu principal mostra:

- nome
- role
- email
- professor
- matricula

## Estrutura do Firestore

Documento raiz:

- `users/{uid}`

Campos relevantes agora:

- `uid`
- `email`
- `displayName`
- `studentName`
- `registrationNumber`
- `matricula`
- `teacherName`
- `teacher`
- `teacherLookupKey`
- `profileCompleted`
- `role`
- `isTeacher`
- `isApproved`
- `createdAt`
- `lastLoginAt`

## Limpeza de base

Nesta sessao a colecao `users` do projeto `logisim-eron` foi limpa com:

```bash
firebase firestore:delete users --project logisim-eron --recursive --force
```

Observacao:

- a limpeza atingiu os documentos do Firestore em `users`
- contas do Firebase Auth nao foram removidas
- por isso, contas antigas ainda podem entrar e completar o perfil no primeiro login

## Validacao executada

- compilacao de scripts no Unity: OK
- build Windows `release`: OK

Observacao sobre testes EditMode:

- o Unity recompilou sem erro
- o runner em batch nao gerou o arquivo XML de resultados nesta maquina
- por isso a validacao automatizada confiavel desta sessao ficou ancorada no build bem-sucedido do player

## Proximos passos recomendados

1. validar manualmente o cadastro novo no build Windows
2. validar reset de senha com um email real
3. verificar se o layout do `Create Account` esta confortavel em resolucoes menores
4. gerar nova release Windows com `.exe` e `.zip`
