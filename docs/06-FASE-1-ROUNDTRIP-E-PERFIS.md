# Fase 1 Implementada: Roundtrip e Perfis

## O que entrou nesta fase

- restauracao cloud completa de `ProjectDescription` mais `Chips`
- sync de logout agora reenviando bundle completo de cada projeto
- salvamento local de dados baixados da nuvem sem eco de sync imediato
- delete de projeto no Firestore com remocao da subcolecao `chips`
- delete local real quando `DeleteProject(..., false)` for usado
- perfil minimo do usuario em `users/{uid}` com:
  - `uid`
  - `email`
  - `displayName`
  - `role`
  - `isTeacher`
  - `isApproved`
  - `createdAt`
  - `lastLoginAt`
- role bootstrap por allowlist de email de professor no `FirebaseAuthManager`
- UX de login alinhada ao que existe hoje: `email/password`, nao Google real

## Fluxo atual de aluno

1. aluno cria conta ou entra com email e senha
2. o app sincroniza o perfil em `users/{uid}`
3. o app baixa projetos e chips do subtree do proprio usuario
4. o aluno cria projeto, cria ou salva chips customizados e trabalha normalmente
5. ao sair, o app faz sync completo do bundle antes do logout

## Fluxo atual de professor

Nesta fase o professor ainda nao tem dashboard proprio, mas ja existe base para diferenciar perfil:

- professor pode ser reconhecido por email em allowlist
- o perfil fica salvo com `role = teacher`
- a UI principal ja mostra o papel carregado

Isso prepara a proxima etapa para:

- validacao por professor no proprio app
- dashboard web futura
- consultas agregadas por turma ou atividade

## Estrategia recomendada para validacao de professor

Curto prazo:

- usar allowlist de emails de professor no `FirebaseAuthManager`
- deixar o professor logar com conta propria
- manter leitura do proprio subtree por enquanto

Proxima fase:

- criar colecoes de turma e atividade
- relacionar `aluno -> turma -> atividade`
- expor visao de professor separada dos dados operacionais do aluno

## Checklist manual de roundtrip

Usar `Assets/Build/DLS.unity` em Play Mode e habilitar `openInMainMenu` no objeto `Main`.

Passos:

1. entrar com um usuario novo
2. criar um projeto
3. criar e salvar pelo menos um chip customizado
4. fechar sessao pelo botao `Logout`
5. abrir o app em outra maquina ou em um ambiente limpo de `TestData`
6. entrar com o mesmo usuario
7. confirmar que o projeto aparece no menu
8. abrir o projeto
9. confirmar que os chips customizados carregam sem faltar pasta `Chips`

## Limites que continuam em aberto

- `Google Sign-In` real continua para a fase 2.5
- dashboard de professor ainda nao foi implementado
- telemetria pedagogica por sessao ainda nao entrou; esta e a fase 2 do roadmap
