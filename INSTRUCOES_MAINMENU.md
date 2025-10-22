# INTEGRAÇÃO DO LOGINMENU NO MAINMENU

## 📝 MODIFICAÇÕES NECESSÁRIAS

Você precisa editar o arquivo: [`Assets/Scripts/Graphics/UI/Menus/MainMenu.cs`](Assets/Scripts/Graphics/UI/Menus/MainMenu.cs)

---

## 1️⃣ MODIFICAÇÃO NO MÉTODO `Draw()`

**Localização:** Linha ~81-82

### ANTES:
```csharp
public static void Draw()
{
	Simulator.UpdateInPausedState();

	if (KeyboardShortcuts.CancelShortcutTriggered && activePopup == PopupKind.None)
	{
		BackToMain();
	}
```

### DEPOIS:
```csharp
public static void Draw()
{
	Simulator.UpdateInPausedState();

	// === ADICIONE ESTA LINHA ===
	LoginMenu.DrawLoginSection(UI.Centre + Vector2.up * 20);

	if (KeyboardShortcuts.CancelShortcutTriggered && activePopup == PopupKind.None)
	{
		BackToMain();
	}
```

---

## 2️⃣ MODIFICAÇÃO NO MÉTODO `OnMenuOpened()`

**Localização:** Linha ~134-138

### ANTES:
```csharp
public static void OnMenuOpened()
{
	activeMenuScreen = MenuScreen.Main;
	activePopup = PopupKind.None;
	selectedProjectIndex = -1;
}
```

### DEPOIS:
```csharp
public static void OnMenuOpened()
{
	// === ADICIONE ESTA LINHA ===
	LoginMenu.Initialize();

	activeMenuScreen = MenuScreen.Main;
	activePopup = PopupKind.None;
	selectedProjectIndex = -1;
}
```

---

## ✅ VERIFICAÇÃO

Depois de fazer as modificações:

1. **Salve o arquivo** (Ctrl+S)
2. **Volte para o Unity**
3. **Aguarde a recompilação**
4. **Aperte Play**
5. **Abra o Main Menu** (ESC)
6. **Você deve ver:**
   - Formulário de login no topo da tela
   - Campos: Email, Password
   - Botões: Login, Create Account, Continue Offline

---

## 🎨 AJUSTAR POSIÇÃO (OPCIONAL)

Se o login aparecer em lugar errado, ajuste a posição modificando:

```csharp
LoginMenu.DrawLoginSection(UI.Centre + Vector2.up * 20);
//                                              ^^^ Ajuste este valor
```

**Valores recomendados:**
- `Vector2.up * 20` - No topo
- `Vector2.up * 10` - Meio-alto
- `Vector2.up * 0` - Centro
- `Vector2.down * 10` - Meio-baixo

---

## 📄 CÓDIGO COMPLETO PARA COPIAR

Se preferir, aqui está o código completo das duas modificações:

### Método `Draw()`:
```csharp
public static void Draw()
{
	Simulator.UpdateInPausedState();
	LoginMenu.DrawLoginSection(UI.Centre + Vector2.up * 20);

	if (KeyboardShortcuts.CancelShortcutTriggered && activePopup == PopupKind.None)
	{
		BackToMain();
	}

	UI.DrawFullscreenPanel(ColHelper.MakeCol255(47, 47, 53));
	const string title = "DIGITAL LOGIC SIM";
	// ... resto do código continua igual
```

### Método `OnMenuOpened()`:
```csharp
public static void OnMenuOpened()
{
	LoginMenu.Initialize();
	activeMenuScreen = MenuScreen.Main;
	activePopup = PopupKind.None;
	selectedProjectIndex = -1;
}
```

---

**Pronto! Agora o sistema de login estará integrado ao Main Menu! 🎉**
