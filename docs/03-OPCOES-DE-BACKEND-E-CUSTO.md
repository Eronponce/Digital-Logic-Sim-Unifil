# Opcoes de Backend e Custo

## Decisao recomendada

Usar `Firestore` como fonte de verdade operacional e, se fizer sentido para professores, espelhar dados agregados em `Google Sheets`.

Resumo:

- `Firestore` como banco principal: recomendado
- `Google Sheets` como banco principal: nao recomendado
- `Google Sheets` como camada de relatorio/exportacao: recomendado

Atualizacao desta decisao:

- o usuario decidiu seguir sem `Google Sheets` por enquanto
- portanto o plano ativo passa a ser `Firebase only` na primeira fase
- se no futuro professores precisarem de visualizacao simples, a exportacao para Sheets continua sendo opcional

## Opcao 1: Google Sheets como banco principal

### Vantagens

- custo inicial baixo
- interface familiar para professores
- facil exportar e olhar dados manualmente

### Desvantagens

- limite de `10 milhoes de celulas` por planilha
- quota da API de `300` leituras por minuto por projeto e `300` escritas por minuto por projeto
- quota da API de `60` leituras e `60` escritas por minuto por usuario por projeto
- se automatizar via Apps Script, ha quotas diarias e limites de runtime
- autenticacao e publicacao ficam mais chatas para um app desktop distribuido para terceiros
- concorrencia, queries e integridade sao piores do que em um banco orientado a documentos
- snapshots JSON grandes e eventos frequentes ficam desconfortaveis em linhas de planilha

### Veredito

`Google Sheets` nao e uma boa fonte de verdade para sincronizacao quase em tempo real de atividade estudantil. Ele funciona melhor como camada de visualizacao, exportacao e acompanhamento manual.

## Opcao 2: Firestore como banco principal

### Vantagens

- ja existe integracao no codigo
- modelo de documento combina com `ProjectDescription` e `ChipDescription`
- suporta cache offline
- combina naturalmente com Firebase Auth
- concorrencia e consultas sao melhores para esse tipo de app

### Custos e limites oficiais mais uteis para MVP

No free tier oficial do Cloud Firestore:

- `1 GiB` de armazenamento
- `50.000` leituras por dia
- `20.000` escritas por dia
- `20.000` deletes por dia
- `10 GiB` por mes de transferencia de saida

### Riscos

- se gravarmos evento demais, o free tier some rapido
- a estrutura atual do projeto ainda nao garante roundtrip completo
- sem batch e sem agregacao, analytics em tempo real pode gerar custo desnecessario

### Veredito

E a melhor base para o MVP, desde que os eventos sejam batelados e que a telemetria seja modelada com parcimonia.

Ressalva importante:

- no caso deste projeto, o app e desktop Unity
- a propria documentacao do Firebase para Unity ainda trata o fluxo desktop como beta

## Opcao 3: Firestore + Google Sheets

### Como fica

- `Firestore` guarda snapshots e telemetria operacional
- `Google Sheets` recebe relatorios agregados
- professores usam a planilha para acompanhamento sem depender do console do Firebase

### Beneficios

- melhor equilibrio entre confiabilidade tecnica e usabilidade
- mantem custo baixo
- evita transformar planilha em banco transacional

### Veredito

Esta e a opcao mais equilibrada para o objetivo de acompanhar alunos sem criar custo cedo demais.

## Estrategias para manter custo baixo

- gravar eventos em lote a cada `30-60s`
- gravar sempre em eventos de alto valor:
  - login
  - abrir projeto
  - save
  - criar, renomear e deletar chip
  - fechar sessao
- evitar log por clique fino ou movimento de mouse
- gerar sumarios por sessao e por dia
- mandar para Sheets apenas agregados e visoes para professor

## Minha recomendacao pratica

1. Corrigir a integridade do sync atual em Firestore.
2. Criar uma camada de telemetria com lotes pequenos.
3. Criar sumarios por sessao e por dia.
4. Exportar esses sumarios para uma planilha, se voce quiser uma visao sem custo operacional alto e sem abrir o Firebase Console.

## Referencias oficiais consultadas em 2026-04-08

- OpenAI Codex customization:
  - https://developers.openai.com/codex/concepts/customization
- Firestore quotas:
  - https://firebase.google.com/docs/firestore/quotas
- Google Sheets API limits:
  - https://developers.google.com/workspace/sheets/api/limits
- Google Sheets file limits:
  - https://support.google.com/drive/answer/37603
- Google Sheets scopes and verification:
  - https://developers.google.com/workspace/sheets/api/scopes
- Apps Script quotas:
  - https://developers.google.com/apps-script/guides/services/quotas
