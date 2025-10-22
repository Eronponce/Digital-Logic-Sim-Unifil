# 🔧 TROUBLESHOOTING - Firebase Authentication

## ❌ ERRO: "An internal error has occurred"

### 📋 **CAUSA MAIS COMUM:**

**Email/Password authentication NÃO está ativado no Firebase Console!**

---

## ✅ SOLUÇÃO PASSO A PASSO:

### 1. Ative Email/Password no Firebase Console

```
1. Vá em: https://console.firebase.google.com
2. Selecione: "digital-logic-sim-unifil-fb0b8"
3. Clique em: Authentication (menu lateral)
4. Clique na aba: "Sign-in method"
5. Encontre: "Email/Password"
6. Clique nele
7. Ative: "Enable" (primeira opção)
8. Clique: "Save"
```

**IMPORTANTE:** A segunda opção "Email link (passwordless sign-in)" pode ficar DESATIVADA!

---

### 2. Verifique se está ativado

Depois de salvar, você deve ver:

```
┌─────────────────────────────────────────┐
│  Email/Password    [ ENABLED ] ✅       │
└─────────────────────────────────────────┘
```

---

### 3. Teste no Unity

1. **Feche e abra o Unity** (para garantir)
2. **Aperte Play**
3. **Crie uma nova conta:**
   - Google Email: `seuemail@gmail.com`
   - Password: `senha123` (mínimo 6 caracteres)
   - Display Name: `Seu Nome`
4. **Clique: "Create Account"**

---

## 🔍 OUTROS ERROS COMUNS:

### Error: "This email is already registered"
**Solução:** Use "Sign in with Google" ao invés de "Create Account"

### Error: "No account found with this email"
**Solução:** Crie uma conta primeiro usando "Create Account"

### Error: "Incorrect password"
**Solução:** Verifique a senha. Mínimo 6 caracteres.

### Error: "Password is too weak"
**Solução:** Use no mínimo 6 caracteres na senha

### Error: "Invalid email address format"
**Solução:** Use um email válido (exemplo@dominio.com)

### Error: "Network error"
**Solução:** Verifique sua conexão com a internet

### Error: "Authentication system not ready"
**Solução:** Aguarde alguns segundos e tente novamente. O Firebase ainda está inicializando.

---

## 📸 VERIFICAR CONFIGURAÇÃO:

### No Unity Console, você deve ver:

```
[FirebaseManager] Initializing Firebase...
[FirebaseManager] ✅ Firebase initialized successfully!
[FirebaseAuth] Auth initialized. Checking for existing session...
[FirebaseAuth] No active session found.
```

### Se não ver isso:

1. **Verifique se os GameObjects existem:**
   - Abra a cena: `Assets/Build/DLS.unity`
   - Procure GameObject: `FirebaseManagers`
   - Deve ter 3 componentes:
     - `FirebaseManager`
     - `FirebaseAuthManager`
     - `FirestoreDataManager`

2. **Verifique se o arquivo existe:**
   - `Assets/StreamingAssets/google-services-desktop.json`
   - Deve existir e ter conteúdo JSON válido

---

## 🧪 TESTE COM "CONTINUE OFFLINE"

Se ainda estiver com problemas, você pode testar o app sem autenticação:

1. **Aperte Play**
2. **Na tela de login, clique: "Continue Offline"**
3. **O app deve funcionar normalmente**

---

## 📝 LOGS DETALHADOS:

Agora o sistema mostra erros mais detalhados! Procure no Console por mensagens assim:

```
[FirebaseAuth] Sign in failed: <ERRO DETALHADO> (Code: XXXXX)
```

Copie a mensagem completa e me envie se precisar de mais ajuda!

---

## 🆘 AINDA COM PROBLEMA?

1. **Copie TODA a mensagem de erro do Unity Console**
2. **Tire um print da aba "Sign-in method" do Firebase Console**
3. **Me envie:**
   - A mensagem de erro completa
   - O print do Firebase Console
   - O que você tentou fazer

---

**Na maioria dos casos, o problema é simplesmente não ter ativado Email/Password no Firebase Console!** ✅
