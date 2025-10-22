# 🔥 SETUP FINAL - FIREBASE NO UNITY

## ✅ O QUE JÁ FOI FEITO

1. ✅ Projeto Firebase configurado
2. ✅ Firebase Unity SDK importado
3. ✅ `google-services.json` na raiz do projeto
4. ✅ Scripts criados em `Assets/Scripts/CloudSync/`
5. ✅ `SaverCloudExtension.cs` criado
6. ✅ `LoginMenu.cs` criado

---

## 📋 PASSOS FINAIS NO UNITY

### PASSO 1: Modificar Saver.cs

**Arquivo:** `Assets/Scripts/SaveSystem/Saver.cs`

**Linha ~46 - Método `SaveChip`:**

**ENCONTRE:**
```csharp
public static void SaveChip(ChipDescription chipDescription, string projectName)
{
	string serializedDescription = CreateSerializedChipDescription(chipDescription);
	WriteToFile(serializedDescription, GetChipFilePath(chipDescription.Name, projectName));
}
```

**SUBSTITUA POR:**
```csharp
public static void SaveChip(ChipDescription chipDescription, string projectName)
{
	// Salvamento local
	string serializedDescription = CreateSerializedChipDescription(chipDescription);
	WriteToFile(serializedDescription, GetChipFilePath(chipDescription.Name, projectName));

	// Sincronização cloud
	SaverCloudExtension.SyncChipToCloud(chipDescription, projectName);
}
```

---

### PASSO 2: Criar GameObject com os Managers

No Unity:

1. **Abra a cena:** `Assets/Build/DLS.unity` (cena principal do jogo)

2. **Crie um GameObject vazio:**
   - Botão direito na Hierarchy → **Create Empty**
   - Nome: `FirebaseManagers`

3. **Adicione os componentes:**
   - Selecione `FirebaseManagers`
   - No Inspector, clique em **Add Component**
   - Adicione na ordem:
     1. `FirebaseManager`
     2. `FirebaseAuthManager`
     3. `FirestoreDataManager`

4. **Configure:**
   - Em `FirebaseManager`:
     - ✅ `Show Debug Logs` = TRUE (para ver logs inicialmente)
   - Em `FirebaseAuthManager`:
     - ✅ `Show Debug Logs` = TRUE
     - ✅ `Persist Session` = TRUE (manter logado)
   - Em `FirestoreDataManager`:
     - ✅ `Show Debug Logs` = TRUE
     - ✅ `Enable Offline Cache` = TRUE (cache local)

5. **Salve a cena:** `Ctrl+S`

---

### PASSO 3: Integrar LoginMenu no MainMenu

**Arquivo:** `Assets/Scripts/Graphics/UI/Menus/MainMenu.cs`

#### 3.1. Adicionar using no topo:
```csharp
using DLS.Graphics; // Se ainda não tiver
```

#### 3.2. No método `Draw()` (procure por onde desenha o menu principal)

**ENCONTRE algo como:**
```csharp
public static void Draw()
{
	// ... código existente ...
```

**ADICIONE logo no início do método:**
```csharp
public static void Draw()
{
	// Login section no topo
	Vector2 loginSectionPos = new Vector2(Screen.width / 2, 150);
	LoginMenu.DrawLoginSection(loginSectionPos);

	// ... resto do código existente ...
```

#### 3.3. No método onde o menu é inicializado (procure por `Initialize` ou construtor estático)

**ADICIONE:**
```csharp
LoginMenu.Initialize();
```

---

### PASSO 4: Testar!

1. **Compile o projeto:** `Ctrl+B` (Build) ou apenas rode no Editor

2. **Aperte Play no Unity**

3. **Verifique o Console:**
   - Deve aparecer:
     ```
     [FirebaseManager] Initializing Firebase...
     [FirebaseManager] ✅ Firebase initialized successfully!
     [FirebaseAuth] Auth initialized. Checking for existing session...
     [FirestoreData] Firestore initialized
     ```

4. **No MainMenu, você deve ver:**
   - Seção de LOGIN no topo com campos:
     - Email
     - Password
     - Botões: "Login" e "Create Account"
     - Botão: "Continue Offline"

---

## 🧪 TESTANDO O SISTEMA

### Teste 1: Criar Conta

1. Na tela de login, clique em **"Create Account"**
2. Preencha:
   - **Email:** `teste@example.com`
   - **Password:** `123456` (mínimo 6 caracteres)
   - **Name:** `Teste User`
3. Clique em **"Create Account"**
4. **Aguarde ~2 segundos**
5. Se der certo:
   - Status muda para "Welcome, Teste User!"
   - Formulário some e aparece "Logged in as: Teste User"

### Teste 2: Login

1. Se já criou conta, faça logout
2. Preencha:
   - **Email:** `teste@example.com`
   - **Password:** `123456`
3. Clique em **"Login"**
4. Deve logar automaticamente

### Teste 3: Salvar Circuito

1. **Estando logado**, crie um projeto novo
2. Crie um chip simples (ex: adicione um NAND)
3. Salve o chip (`Ctrl+S`)
4. **Veja no Console do Unity:**
   ```
   [Cloud] Chip 'MeuChip' synced
   ```

5. **Verifique no Firebase Console:**
   - Vá em: https://console.firebase.google.com
   - Seu projeto → **Firestore Database**
   - Você deve ver:
     ```
     users/
       └── {userId}/
           └── projects/
               └── {projectName}/
                   └── chips/
                       └── MeuChip
     ```

### Teste 4: Modo Offline

1. Clique em **"Continue Offline"**
2. Crie/edite circuitos normalmente
3. Eles serão salvos **apenas localmente**
4. Quando fizer login depois, pode fazer upload manual (se implementar)

---

## ⚠️ POSSÍVEIS ERROS E SOLUÇÕES

### Erro: "Could not resolve Firebase dependencies"
**Solução:**
- Certifique-se que o `google-services.json` está na **raiz** do projeto (não dentro de Assets/)
- Reimporte os pacotes Firebase

### Erro: "FirebaseAuthManager not found"
**Solução:**
- Verifique que o GameObject `FirebaseManagers` tem os 3 componentes
- Certifique-se que a cena está salva

### Erro: "Permission denied" ao salvar no Firestore
**Solução:**
- Verifique as Security Rules no Firebase Console
- Certifique-se que está **logado** antes de salvar

### Login não funciona
**Solução:**
- Verifique que Authentication está ativado no Firebase Console
- Provider "Email/Password" deve estar habilitado
- Veja o Console do Unity para mensagens de erro detalhadas

---

## 🎯 FUNCIONALIDADES IMPLEMENTADAS

✅ **Login/Logout** com email e senha
✅ **Criação de conta** nova
✅ **Sessão persistente** (fica logado entre sessões)
✅ **Salvamento automático** no cloud quando logado
✅ **Cache offline** (Firestore continua funcionando offline)
✅ **Modo offline** (continuar sem login)
✅ **Sincronização de Projetos** para Firestore
✅ **Sincronização de Chips** para Firestore

---

## 🚀 PRÓXIMOS PASSOS (OPCIONAL)

### Adicionar Login com Google (Desktop)

Para desktop, login com Google requer:
1. Implementar OAuth flow via browser externo
2. Usar `Application.OpenURL()` para abrir Google Auth
3. Usar deep linking ou servidor local para receber callback

**Recomendo usar email/senha por enquanto para simplicidade.**

---

### Carregar Circuitos da Nuvem

Atualmente, os circuitos são salvos automaticamente, mas para **carregar** da nuvem, você precisa:

1. Criar método em `Loader.cs`:
```csharp
public static void LoadProjectFromCloud(string projectName, Action<Project> callback)
{
	FirestoreDataManager.LoadChips(projectName, (chips) => {
		// Reconstruir projeto a partir dos chips
		callback(reconstructedProject);
	});
}
```

2. Adicionar botão no MainMenu:
   - "Load from Cloud"
   - Lista projetos salvos na nuvem
   - Baixa e carrega o selecionado

---

### Reset Anual Automático

Para apagar dados todo ano automaticamente:

**Opção 1: Cloud Function (Firebase)**
```javascript
// functions/index.js
exports.deleteOldUsers = functions.pubsub
  .schedule('every 1 year')
  .onRun(async (context) => {
    // Deletar todos usuários
  });
```

**Opção 2: Botão Manual no Unity**
```csharp
if (UI.Button("Reset All Data"))
{
	FirestoreDataManager.DeleteAllUserData(
		onSuccess: () => Debug.Log("Data deleted!"),
		onError: (err) => Debug.LogError(err)
	);
}
```

---

## 📞 SUPORTE

Se tiver problemas:

1. **Veja os logs do Console Unity** (muito importante!)
2. **Veja o Firebase Console** → Authentication → Users (para ver se conta foi criada)
3. **Veja o Firebase Console** → Firestore → Data (para ver se dados foram salvos)

---

## 🎉 PRONTO!

Seu Digital Logic Simulator agora tem:
- ✅ Login na nuvem
- ✅ Circuitos sincronizados automaticamente
- ✅ Acesso apenas durante sessão (ao deslogar, perde acesso)
- ✅ Modo offline para trabalhar sem internet

**Divirta-se criando circuitos!** 🔥⚡
