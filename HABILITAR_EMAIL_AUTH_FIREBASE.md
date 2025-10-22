# ⚠️ HABILITAR AUTENTICAÇÃO GOOGLE NO FIREBASE

## 🔥 ERRO QUE VOCÊ ESTÁ VENDO:

```
[FirebaseAuth] Sign in failed: This operation is not allowed.
You must enable this service in the console.
```

## ✅ SOLUÇÃO: Ativar Google Authentication

### PASSO A PASSO:

1. **Acesse o Firebase Console:**
   - Abra: https://console.firebase.google.com
   - Faça login com sua conta Google

2. **Selecione seu projeto:**
   - Clique em: **"digital-logic-sim-unifil-fb0b8"**
   - (ou o nome do seu projeto)

3. **Vá para Authentication:**
   - No menu lateral esquerdo
   - Clique em: **"Authentication"** (ícone de cadeado com pessoa)

4. **Abra a aba Sign-in methods:**
   - Clique na aba: **"Sign-in method"**
   - Você verá uma lista de providers

5. **Ative Google E Email/Password:**

   **A. Google (Já deve estar ativo):**
   - Procure por: **"Google"**
   - Deve estar: **ENABLED** ✅
   - Se não estiver, clique e ative

   **B. Email/Password (IMPORTANTE!):**
   - Procure por: **"Email/Password"**
   - Clique nele
   - **Ative a opção "Enable"**
   - Clique em: **"Save"**

   > **NOTA:** Mesmo usando conta Google, o Firebase usa Email/Password internamente para desktop!

---

## 📸 VISUAL DO CONSOLE:

Você deve ver algo assim:

```
┌─────────────────────────────────────────┐
│  Sign-in method                         │
├─────────────────────────────────────────┤
│  Providers                              │
│                                         │
│  ▶ Google          [ DISABLED ]        │
│  ▶ Email/Password  [ DISABLED ] ← ESTE │
│  ▶ Anonymous       [ DISABLED ]        │
│  ▶ ...                                 │
└─────────────────────────────────────────┘
```

**Clique em "Email/Password" e ative!**

---

## ✅ VERIFICAÇÃO:

Depois de ativar, você deve ver:

```
┌─────────────────────────────────────────┐
│  ▶ Email/Password  [ ENABLED ] ✅       │
└─────────────────────────────────────────┘
```

---

## 🧪 TESTAR NO UNITY:

### Opção 1: Criar Nova Conta

1. **Volte para o Unity**
2. **Aperte Play** → Abre tela de login
3. **Clique em "Create Account"**
4. **Preencha:**
   - Google Email: `seuemail@gmail.com`
   - Password: `suasenha123` (mínimo 6 caracteres)
   - Display Name: `Seu Nome`
5. **Clique em "Create Account"**

**Deve funcionar agora! ✅**

### Opção 2: Criar Usuário Manualmente no Console

1. **No Firebase Console:**
   - Vá em: **Authentication → Users**
   - Clique em: **"Add user"**

2. **Preencha:**
   - Email: `teste@gmail.com`
   - Password: `teste123`
   - Clique em: **"Add user"**

3. **Use no Unity:**
   - Google Email: `teste@gmail.com`
   - Password: `teste123`
   - Clique em: **"Sign in with Google"**

---

## 🔍 TROUBLESHOOTING:

### Erro: "Email already in use"
- O usuário já foi criado
- Tente fazer login ao invés de criar conta

### Erro: "Invalid email"
- Use um email válido: `usuario@dominio.com`

### Erro: "Password too short"
- A senha precisa ter no mínimo 6 caracteres

---

## 📚 DOCUMENTAÇÃO OFICIAL:

- Autenticação Email/Password: https://firebase.google.com/docs/auth/unity/password-auth
- Firebase Console: https://console.firebase.google.com

---

**Pronto! Agora a autenticação Email/Password está ativa! 🎉**
