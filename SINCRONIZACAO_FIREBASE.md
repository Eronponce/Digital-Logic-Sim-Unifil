# Sistema de Sincronização Automática com Firebase

## Visão Geral

O sistema agora sincroniza **automaticamente** todos os projetos e circuitos do usuário com o Firebase. Isso garante que:

1. ✅ Todo projeto salvo é enviado para o Firebase imediatamente
2. ✅ Todo chip/circuito salvo é enviado para o Firebase imediatamente
3. ✅ Quando o usuário faz logout, **TODOS** os projetos locais são sincronizados
4. ✅ Quando o usuário faz login, **TODOS** os projetos do Firebase são baixados

## Como Funciona

### 🔄 Sincronização Automática em Tempo Real

#### Salvamento de Projetos
Sempre que `Saver.SaveProjectDescription()` é chamado:
- ✅ Salva localmente (como sempre)
- ✅ **Envia automaticamente para o Firebase** (se logado)

**Código:** [Saver.cs:27](Assets/Scripts/SaveSystem/Saver.cs#L27)
```csharp
SaverCloudExtension.SyncProjectToCloud(projectDescription);
```

#### Salvamento de Chips/Circuitos
Sempre que `Saver.SaveChip()` é chamado:
- ✅ Salva localmente (como sempre)
- ✅ **Envia automaticamente para o Firebase** (se logado)

**Código:** [Saver.cs:53](Assets/Scripts/SaveSystem/Saver.cs#L53)
```csharp
SaverCloudExtension.SyncChipToCloud(chipDescription, projectName);
```

#### Deleção de Projetos
Sempre que `Saver.DeleteProject()` é chamado:
- ✅ Deleta localmente (como sempre)
- ✅ **Deleta do Firebase** (se logado)

**Código:** [Saver.cs:99](Assets/Scripts/SaveSystem/Saver.cs#L99)

#### Deleção de Chips
Sempre que `Saver.DeleteChip()` é chamado:
- ✅ Deleta localmente (como sempre)
- ✅ **Deleta do Firebase** (se logado)

**Código:** [Saver.cs:83](Assets/Scripts/SaveSystem/Saver.cs#L83)

---

### 🚪 Sincronização no Logout

Quando o usuário faz logout via `FirebaseAuthManager.SignOut()`:

1. **PRIMEIRO**: Sincroniza TODOS os projetos locais para o Firebase
2. **DEPOIS**: Faz o logout

Isso garante que nada seja perdido, mesmo que o usuário tenha feito alterações offline.

**Código:** [FirebaseAuthManager.cs:133](Assets/Scripts/CloudSync/FirebaseAuthManager.cs#L133)
```csharp
SaverCloudExtension.SyncAllProjectsToCloud(() =>
{
    Auth.SignOut();
});
```

---

### 📥 Carregamento Automático no Login

Quando o usuário faz login:

1. **Carrega todos os projetos do Firebase**
2. **Compara com versões locais:**
   - Se o projeto **NÃO existe localmente** → Baixa do Firebase
   - Se o projeto **existe localmente** → Compara datas:
     - Se **Firebase mais recente** → Sobrescreve local
     - Se **local mais recente ou igual** → Mantém local

Isso permite trabalhar offline e depois sincronizar automaticamente!

**Código:** [FirebaseAuthManager.cs:84](Assets/Scripts/CloudSync/FirebaseAuthManager.cs#L84)
```csharp
SaverCloudExtension.LoadAllProjectsFromCloud((loadedCount) =>
{
    Log($"Loaded {loadedCount} projects from cloud");
});
```

**Lógica de merge:** [SaverCloudExtension.cs:156-184](Assets/Scripts/SaveSystem/SaverCloudExtension.cs#L156-L184)

---

## Estrutura de Dados no Firebase

```
Firestore:
└── users/
    └── {userId}/
        └── projects/
            ├── {projectName}/
            │   ├── projectName: string
            │   ├── projectData: JSON (serializado)
            │   ├── lastModified: timestamp
            │   └── chips/
            │       └── {chipName}/
            │           ├── chipName: string
            │           ├── chipData: JSON (serializado)
            │           └── lastModified: timestamp
            └── ...
```

Cada usuário tem seus próprios projetos isolados por `userId`.

---

## Benefícios

### ✅ Para o Usuário
- **Backup automático** - Nunca perde o trabalho
- **Sincronização multi-dispositivo** - Trabalha em casa e na escola
- **Trabalho offline** - Pode trabalhar sem internet, sincroniza depois
- **Sem preocupação** - Tudo é salvo automaticamente

### ✅ Para o Desenvolvedor
- **Sistema transparente** - Nenhuma mudança no código de salvamento
- **Opt-in automático** - Só sincroniza se logado
- **Fallback offline** - Continua funcionando sem Firebase
- **Debug fácil** - Logs detalhados de todas as operações

---

## Componentes Principais

### 1. **SaverCloudExtension.cs**
Extensão do `Saver` que adiciona funcionalidade de cloud sem modificar o código original.

**Métodos principais:**
- `SyncProjectToCloud()` - Sincroniza projeto individual
- `SyncChipToCloud()` - Sincroniza chip individual
- `SyncAllProjectsToCloud()` - Sincroniza TODOS os projetos (usado no logout)
- `LoadAllProjectsFromCloud()` - Carrega TODOS os projetos (usado no login)
- `DeleteProjectFromCloud()` - Deleta projeto do Firebase
- `DeleteChipFromCloud()` - Deleta chip do Firebase

### 2. **FirebaseAuthManager.cs**
Gerencia autenticação e triggers de sincronização.

**Hooks de sincronização:**
- `OnAuthStateChanged()` → Login detectado → Carrega projetos
- `SignOut()` → Antes de deslogar → Sincroniza tudo

### 3. **FirestoreDataManager.cs**
Gerencia operações diretas com Firestore (CRUD de projetos/chips).

**Métodos principais:**
- `SaveProject()` / `LoadAllProjects()`
- `SaveChip()` / `LoadChips()`
- `DeleteProject()` / `DeleteChip()`

---

## Logs de Debug

O sistema emite logs detalhados para facilitar debugging:

```
[Cloud] Project 'MyCircuit' synced
[Cloud] Chip 'AND Gate' synced
[Cloud] Starting sync of 5 projects...
[Cloud] Synced 1/5: Project A
[Cloud] Synced 2/5: Project B
...
[Cloud] ✅ All 5 projects synced successfully!
[Cloud] Loaded 3 projects from cloud
[Cloud] Updating 'ProjectX' (cloud version is newer)
[Cloud] Skipping 'ProjectY' (local version is newer or equal)
```

---

## Modo Offline

Se o usuário **NÃO** estiver logado:
- Tudo funciona normalmente (salvamento local)
- Nenhuma sincronização acontece
- Nenhum erro é exibido
- Quando logar, tudo será sincronizado

---

## Testes Recomendados

### Teste 1: Salvamento Automático
1. Fazer login
2. Criar/editar um projeto
3. Salvar
4. Verificar logs: `[Cloud] Project 'X' synced`
5. Verificar no Firebase Console se apareceu

### Teste 2: Sincronização no Logout
1. Criar 3 projetos localmente enquanto logado
2. Fazer logout
3. Verificar logs: `[Cloud] Starting sync of 3 projects...`
4. Verificar no Firebase Console se todos foram salvos

### Teste 3: Carregamento no Login
1. Deletar projeto local
2. Fazer login
3. Verificar logs: `[Cloud] Downloading new project: 'X'`
4. Verificar se projeto reapareceu localmente

### Teste 4: Merge de Versões
1. Criar projeto local offline
2. Modificar projeto no Firebase manualmente (com data mais recente)
3. Fazer login
4. Verificar logs: `[Cloud] Updating 'X' (cloud version is newer)`
5. Verificar se versão local foi atualizada

---

## Problemas Conhecidos

### ⚠️ Subcoleções no Firestore
Deletar um projeto **NÃO deleta automaticamente** os chips dentro dele no Firestore.

**Motivo:** Limitação do Firestore - subcoleções não são deletadas automaticamente.

**Solução futura:** Implementar Cloud Function para deletar subcoleções recursivamente.

**Impacto atual:** Mínimo - os chips órfãos não causam problemas, apenas ocupam espaço.

---

## Próximos Passos (Opcional)

1. **Indicador visual de sincronização** - Mostrar "sincronizando..." na UI
2. **Resolver conflitos manualmente** - Perguntar ao usuário qual versão manter
3. **Sincronização de chips individual** - Baixar chips de um projeto específico
4. **Cloud Function para limpeza** - Deletar subcoleções automaticamente
5. **Compressão de dados** - Reduzir tamanho dos JSONs no Firebase
6. **Versionamento** - Manter histórico de versões dos projetos

---

## Resumo

✅ **Salvamento automático** - Cada save vai pro Firebase
✅ **Sincronização no logout** - TODOS os projetos são enviados
✅ **Carregamento no login** - TODOS os projetos são baixados
✅ **Merge inteligente** - Versão mais recente prevalece
✅ **Trabalho offline** - Sincroniza quando conectar
✅ **Zero configuração** - Funciona transparentemente

**Nenhuma ação manual necessária!** 🎉
