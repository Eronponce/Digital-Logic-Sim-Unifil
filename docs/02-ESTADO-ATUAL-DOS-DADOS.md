# Estado Atual dos Dados

## O que o sistema guarda hoje localmente

### `AppSettings`

Guarda apenas configuracoes globais da aplicacao:

- resolucao
- fullscreen
- vsync

### `ProjectDescription`

Guarda o snapshot do projeto:

- nome do projeto
- versao do app usada no save
- timestamps de criacao e ultimo save
- preferencias de UI e simulacao
- lista de chips customizados
- itens favoritos
- colecoes de chips

### `ChipDescription`

Guarda o snapshot completo de um chip:

- nome
- tipo
- tamanho e cor
- pinos de entrada e saida
- subchips
- fios
- displays

## O que o Firestore guarda hoje

Estrutura atual:

- `users/{userId}`
- `users/{userId}/projects/{projectName}`
- `users/{userId}/projects/{projectName}/chips/{chipName}`

O documento raiz de usuario agora guarda:

- `uid`
- `email`
- `displayName`
- `studentName`
- `registrationNumber`
- `teacherName`
- `teacherLookupKey`
- `profileCompleted`
- `role`
- `isTeacher`
- `isApproved`
- `createdAt`
- `lastLoginAt`

Cada documento de projeto guarda:

- `projectName`
- `projectData` com o JSON serializado de `ProjectDescription`
- `lastModified` com `ServerTimestamp`

Cada documento de chip guarda:

- `chipName`
- `chipData` com o JSON serializado de `ChipDescription`
- `lastModified` com `ServerTimestamp`

## O que NAO esta sendo guardado hoje

Se o objetivo e acompanhar o que os alunos fazem, faltam dados de comportamento:

- inicio e fim de sessao
- projeto aberto e fechado
- tempo ativo por projeto
- tentativas e erros
- autosaves durante edicao
- quantidade de subchips inseridos e removidos
- eventos de simulacao relevantes
- falhas de sync
- contexto de turma, disciplina, atividade ou professor

## Achados importantes no codigo atual

### 1. Restauracao cloud incompleta

O login carrega apenas `ProjectDescription` via `LoadAllProjectsFromCloud()`. O metodo existe para carregar chips do Firestore, mas ele nao e chamado na restauracao atual.

Impacto:

- um projeto baixado da nuvem pode ficar sem a pasta `Chips/`
- abrir esse projeto depois pode falhar se houver chips customizados
- a documentacao antiga descreve uma restauracao mais completa do que o codigo realmente faz

### 2. Sync de logout incompleto

`SyncAllProjectsToCloud()` reenvia apenas documentos de projeto. Ele nao reenvia os chips do projeto no mesmo fluxo.

Impacto:

- o logout nao garante roundtrip completo de projeto + chips
- falhas temporarias de sync em chips podem ficar sem retentativa

### 3. Download cloud regrava timestamp e ecoa para a nuvem

`Saver.SaveProjectDescription(...)` sempre atualiza `LastSaveTime` local e dispara `SyncProjectToCloud(...)`.

Impacto:

- baixar um projeto da nuvem pode gerar uma nova escrita cloud imediatamente
- isso mascara a origem real da ultima alteracao

### 4. Delete de Firestore nao faz cascade

O proprio codigo ja anota que deletar projeto ou usuario nao remove subcolecoes automaticamente.

Impacto:

- risco de lixo em `chips/`
- risco de custo e inconsistencias historicas

### 5. `DeleteProject(..., false)` nao remove pasta local

O ramo sem backup nao faz `Directory.Delete(...)`.

Impacto:

- bug funcional
- expectativa de delete total nao e atendida

### 6. Texto do login nao bate com a implementacao

O botao mostra `Sign in with Google`, mas a acao chama `SignInWithEmailPassword(...)`.

Impacto:

- UX confusa
- suporte e troubleshooting mais caros

### 7. Eventos de login podem ser assinados mais de uma vez

`LoginMenu.Initialize()` adiciona handlers toda vez que `MainMenu.OnMenuOpened()` roda.

Impacto:

- mensagens duplicadas
- risco de efeitos colaterais repetidos na UI

### 8. Infra de regras nao esta versionada no repo

Nao existe `firestore.rules` no repositorio.

Impacto:

- configuracao critica fica fora do controle de versao
- dificil revisar seguranca e comportamento esperado

## O que isso significa para o objetivo pedagogico

Hoje o banco recebe snapshots quando o usuario salva algo, e nao uma trilha estruturada de atividade. Isso serve para backup e sincronizacao parcial, mas nao serve sozinho para responder perguntas como:

- quanto tempo um aluno ficou em uma atividade
- quais circuitos ele tentou antes de acertar
- onde ele mais errou
- quais chips ele usa mais
- qual foi a evolucao ao longo da aula

## Modelo recomendado para a proxima fase

Separar dois tipos de dado:

- `snapshot operacional`
  - estado atual do projeto e dos chips
  - usado para abrir, continuar e sincronizar trabalho
- `telemetria de aprendizagem`
  - eventos e agregados de sessao
  - usado para analytics e acompanhamento de turma

Estrutura alvo sugerida:

- `users/{uid}`
- `users/{uid}/projects/{projectId}`
- `users/{uid}/projects/{projectId}/chips/{chipId}`
- `users/{uid}/sessions/{sessionId}`
- `users/{uid}/sessions/{sessionId}/batches/{batchId}`
- `users/{uid}/daily_summaries/{yyyymmdd}`

## Regra pratica para "banco sempre atualizado"

Nao registrar cada pixel de movimento do mouse. Registrar:

- inicio e fim de sessao
- abertura e fechamento de projeto
- saves
- criacao, rename e delete de chip
- checkpoints periodicos em lote a cada `30-60s`
- agregados finais por sessao
