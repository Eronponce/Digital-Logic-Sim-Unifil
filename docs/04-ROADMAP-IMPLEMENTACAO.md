# Roadmap de Implementacao

## Objetivo central

Fazer com que o banco reflita o trabalho real dos alunos com confiabilidade suficiente para analytics pedagogico, sem explodir custo nem complicar a distribuicao do app.

## Fase 0: alinhamento de contexto

Entregas:

- `AGENTS.md` na raiz
- documentacao canonica em `docs/`
- skill repo-especifico em `.agents/skills/repo-planning/`

Status:

- iniciado nesta sessao

## Fase 1: tornar o sync confiavel

Meta:

Garantir que Firestore consiga restaurar um projeto completo, e nao apenas um `ProjectDescription`.

Itens:

1. Baixar chips do Firestore junto com o projeto.
2. Evitar eco de sync ao salvar dados baixados da nuvem.
3. Revisar estrategia de IDs para evitar colisao por nome.
4. Implementar delete de subcolecoes de forma segura.
5. Versionar `firestore.rules`.
6. Criar um checklist de roundtrip:
   - criar conta
   - criar projeto
   - criar chip
   - salvar
   - fechar
   - logar em outra maquina
   - abrir projeto

Status em 2026-04-09:

- restauracao de `ProjectDescription + Chips`: implementada
- sync completo no logout: implementado
- eco de sync ao baixar dados cloud: corrigido
- delete com subcolecao `chips`: implementado no app
- checklist manual documentado: implementado em `docs/06-FASE-1-ROUNDTRIP-E-PERFIS.md`
- estrategia de IDs ainda centralizada por nome atual, com `lookupKey` salvo para evolucao futura

Observacao de prioridade:

- mesmo que o login Google entre antes na interface, a prioridade tecnica continua sendo fechar o roundtrip completo de projeto + chips
- sem isso, o login funciona mas a experiencia de trocar de computador continua incompleta

## Fase 2: instrumentar atividade do aluno

Meta:

Separar snapshot operacional de telemetria de aprendizagem.

Eventos minimos sugeridos:

- `session_started`
- `session_ended`
- `project_opened`
- `project_saved`
- `chip_saved`
- `chip_deleted`
- `sync_failed`

Agregados minimos por sessao:

- tempo ativo
- projeto principal usado
- quantidade de saves
- quantidade de chips criados e alterados
- ultima sincronizacao bem sucedida

## Fase 2.5: migrar autenticacao para Google

Meta:

Trocar o fluxo atual de `Email/Password` por `Google Sign-In` real, sem exigir restricao de dominio nesta primeira versao.

Itens:

1. Ajustar o texto e a UX do `LoginMenu`.
2. Implementar fluxo real de Google Sign-In.
3. Persistir dados minimos do usuario autenticado.
4. Manter possibilidade de evoluir depois para allowlist de dominios, se necessario.

Observacao:

- a decisao atual e `Google login`, mas `nao necessariamente so @edu.unifil`
- portanto a primeira versao deve focar em autenticacao funcional e vinculacao do usuario ao banco, sem complicar com bloqueio de dominio agora

## Fase 3: disponibilizar acompanhamento para professor

Meta:

Dar visibilidade sem obrigar o professor a usar o console do Firebase.

Opcoes:

1. Tela admin futura no proprio app ou em web
2. Exportacao diaria para `Google Sheets`
3. Relatorios CSV por turma

Recomendacao:

Comecar por `Firestore + Sheets` com dados agregados.

## Fase 4: build e distribuicao

Meta:

Permitir piloto com usuarios reais.

Checklist minimo:

1. Padronizar build Windows.
2. Documentar passo a passo de publicacao.
3. Publicar zip em `GitHub Releases` para testes internos.
4. Se quiser ampliar acesso, publicar build em `itch.io`.
5. Definir numeracao de versao e changelog simplificado.

## Melhorias que eu priorizaria

### Alta prioridade

- restauracao completa de projeto + chips
- autenticacao Google real
- telemetria basica por sessao
- regras versionadas do Firestore
- correcoes de UX do login

### Media prioridade

- exportacao agregada para Sheets
- build automatizado
- agrupamento por turma, professor e atividade

### Baixa prioridade

- dashboard web completo
- analytics detalhado por acao fina
- multiplas plataformas logo de inicio

## Perguntas em aberto para discutir juntos

- Voce quer acompanhar atividade em tempo quase real ou o suficiente para consolidar a cada minuto?
- O professor precisa ver o circuito bruto do aluno ou so indicadores e links de abertura?
- A relacao `aluno -> turma -> atividade` precisa entrar na primeira versao ou pode vir depois?
- O piloto inicial sera com poucas pessoas internas ou ja com distribuicao mais ampla?
