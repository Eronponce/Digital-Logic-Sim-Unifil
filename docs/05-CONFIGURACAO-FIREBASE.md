# Configuracao Firebase

## Decisao atual

Backend escolhido:

- `Firebase Auth`
- `Cloud Firestore`

Objetivo:

- aluno salvar projeto em casa
- aluno abrir o mesmo projeto em outro computador
- sem servidor proprio seu

## Projeto ativo em 2026-04-09

- Firebase project id: `logisim-eron`
- Conta autenticada no CLI local: `eron.pereira@unifil.br`
- Firestore criado: `projects/logisim-eron/databases/(default)`
- App Android registrado para gerar a configuracao usada pelo Unity:
  - package: `com.sebastianlague.digitallogicsim`
  - app id: `1:979038201351:android:463614ca97b7b4c6c3aca2`

Observacao:

- os arquivos reais de configuracao do Firebase nao ficam mais versionados neste repositorio
- o repositorio agora guarda apenas exemplos sanitizados em `google-services.json.example` e `google-services-desktop.json.example`
- para rodar login/cloud sync localmente, copie esses exemplos para os caminhos esperados e substitua pelos valores reais do seu projeto

## Observacao importante sobre Unity Desktop

A documentacao oficial do Firebase para Unity ainda descreve o workflow desktop como `beta` e diz que ele e voltado para desenvolvimento e testes, nao idealmente para distribuicao publica.

Referencia oficial:

- https://firebase.google.com/docs/unity/setup

O que isso significa para nos:

- para piloto e validacao, faz sentido continuar
- para distribuicao maior para muitos alunos, precisamos validar bem esse caminho desktop ou considerar um acesso via API oficial no futuro

## O que usar agora

Para o estado atual deste repositorio, o caminho mais simples e:

1. Criar um projeto Firebase na sua conta Google institucional `eron.pereira@unifil.br`.
2. Habilitar `Authentication` com `Email/Password`.
3. Criar o `Cloud Firestore` em `Native mode`.
4. Aplicar regras basicas por usuario.
5. Baixar os arquivos reais de configuracao do Firebase e colocar localmente nos caminhos esperados, sem commitar essas credenciais.

## Estado atual do codigo

Hoje o codigo implementa `Email/Password`, nao `Google Sign-In` real.

Importante:

- o botao da UI hoje fala Google, mas o fluxo real implementado e email e senha
- portanto a proxima fase de programacao precisa corrigir essa divergencia

## Direcao de produto decidida

Direcao atual:

- migrar para `Google login`
- sem obrigar restricao por `@edu.unifil` nesta primeira versao
- focar primeiro em autenticacao funcional e sincronizacao entre computadores

Isso significa:

- `Email/Password` pode ser usado temporariamente se precisarmos testar rapido
- mas a meta do produto agora e `Google Sign-In`

## Passo a passo no console

### 1. Criar o projeto Firebase

1. Entre em [Firebase Console](https://console.firebase.google.com/) com `eron.pereira@unifil.br`.
2. Clique em `Create a project`.
3. Escolha um nome, por exemplo `digital-logic-sim-unifil`.
4. Pode deixar Google Analytics desligado neste primeiro momento se quiser simplificar.

### 2. Registrar o app Unity

No setup oficial do Firebase para Unity, registre o app pelo fluxo de Unity e informe o identificador que o projeto usa.

Observacao pratica:

- o repositorio atual ja usa configuracao baseada em `google-services.json`
- se voce criar um novo projeto Firebase, vamos substituir os arquivos atuais pelos novos

### 3. Ativar Authentication

1. No console, abra `Authentication`.
2. Abra `Sign-in method`.
3. Ative `Google`.
4. Se precisarmos manter testes temporarios com o codigo atual, tambem podemos deixar `Email/Password` ativo ate a migracao terminar.

### 4. Configurar politica de senha

A documentacao oficial recomenda configurar password policy. Para piloto:

- minimo de 6 ou 8 caracteres
- modo `Notify` se voce nao quiser bloquear usuarios antigos

Referencia oficial:

- https://firebase.google.com/docs/auth/unity/password-auth

### 5. Criar Firestore

1. Abra `Firestore Database`.
2. Clique em `Create database`.
3. Escolha `Production mode` se ja quiser entrar com regras seguras.
4. Escolha a regiao mais adequada para seus alunos.

Sugestao:

- manter a mesma regiao para reduzir latencia e facilitar governanca

### 6. Aplicar regras

Use o arquivo `firestore.rules` deste repositorio como ponto de partida.

Ideia da regra atual:

- usuario autenticado so acessa o proprio subtree em `users/{uid}`

Referencia oficial:

- https://firebase.google.com/docs/firestore/security/get-started

## O que substituir no repositorio

Quando voce criar seu projeto Firebase:

1. Copie `google-services.json.example` para `google-services.json`.
2. Copie `google-services-desktop.json.example` para `Assets/StreamingAssets/google-services-desktop.json`.
3. Substitua os placeholders desses arquivos pelos valores reais baixados do seu projeto Firebase.
4. Nao faca commit dos arquivos reais; o `.gitignore` do repositorio ja bloqueia esses caminhos.

Observacao:

- este projeto continua usando `Assets/StreamingAssets/google-services-desktop.json` em runtime
- em clone novo, crie a pasta `Assets/StreamingAssets/` se ela ainda nao existir antes de copiar o arquivo
- se o Unity nao regenerar esse arquivo automaticamente, vamos alinhar manualmente no setup final

## Estrategia recomendada de implantacao

### Fase 1

- manter o projeto Firebase pronto para `Google login`
- cada aluno autentica com conta Google
- salvar projeto por usuario no Firestore

### Fase 2

- corrigir roundtrip completo de projeto + chips
- implementar Google Sign-In no app
- adicionar telemetria de sessao

### Fase 3

- decidir se vale restringir por dominio institucional

## Checklist para quando voce for configurar

- projeto Firebase criado com sua conta institucional
- Google ativado em Authentication
- Firestore criado
- regras aplicadas
- exemplos copiados para os caminhos locais finais
- placeholders trocados pelos valores reais baixados
- teste de login
- teste de salvar projeto
- teste de abrir em outro computador

## O que eu vou te pedir quando chegar a hora

Quando voce quiser fazer a configuracao real, o ideal e voce me dizer:

- se vamos usar o projeto Firebase atual ou criar um novo
- se voce quer seguir com Email/Password primeiro

A partir disso eu te guio arquivo por arquivo e console por console.
