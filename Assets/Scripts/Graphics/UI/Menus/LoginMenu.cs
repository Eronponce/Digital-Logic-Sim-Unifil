using System;
using DLS.CloudSync;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	/// <summary>
	/// Menu de login Firebase - Google Authentication
	/// </summary>
	public static class LoginMenu
	{
		static readonly UIHandle ID_EmailInput = new("LoginMenu_EmailInput");
		static readonly UIHandle ID_PasswordInput = new("LoginMenu_PasswordInput");
		static readonly UIHandle ID_DisplayNameInput = new("LoginMenu_DisplayNameInput");

		static string email = "";
		static string passwordActual = ""; // Senha real digitada
		static string displayName = "";
		static string statusMessage = "";
		static bool isCreatingAccount = false;
		static bool wantsOfflineMode = false;
		static bool showPassword = false; // Toggle para mostrar/ocultar senha

		// Para prevenir recursão no processamento de senha mascarada
		static string lastPasswordDisplayed = "";

		/// <summary>
		/// Desenha tela de login completa (para MenuScreen.Login)
		/// </summary>
		public static void DrawFullLoginScreen()
		{
			// Posição mais alta na tela para centralizar melhor
			Vector2 startPos = UI.Centre + Vector2.up * 8;
			DrawLoginForm(startPos, 3.5f);
		}

		/// <summary>
		/// Verifica se o usuário precisa de autenticação
		/// </summary>
		public static bool NeedsAuthentication()
		{
			return !FirebaseAuthManager.IsLoggedIn && !wantsOfflineMode;
		}

		/// <summary>
		/// Verifica se pode prosseguir para o menu principal
		/// </summary>
		public static bool CanProceedToMainMenu()
		{
			return FirebaseAuthManager.IsLoggedIn || wantsOfflineMode;
		}

		static void DrawLoginForm(Vector2 pos, float ySpacing)
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			// Título
			UI.DrawText("SIGN IN", theme.FontBold, theme.FontSizeRegular * 1.5f, pos, Anchor.Centre, Color.white);
			pos.y -= ySpacing * 1.5f;

			if (isCreatingAccount)
			{
				DrawCreateAccountForm(pos, ySpacing, theme);
			}
			else
			{
				DrawSignInForm(pos, ySpacing, theme);
			}
		}

		static void DrawSignInForm(Vector2 pos, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			// Google Email input - MUITO MAIOR
			UI.DrawText("Google Email:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * 32, Anchor.CentreLeft, Color.white);
			UI.DrawText("(yourname@gmail.com)", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos + Vector2.right * 32, Anchor.CentreRight, Color.gray);
			pos.y -= ySpacing * 0.8f;

			Vector2 inputSize = new Vector2(65, DrawSettings.ButtonHeight * 1.4f); // MUITO maior e mais alto
			InputFieldState emailState = UI.InputField(ID_EmailInput, theme.ChipNameInputField, pos, inputSize, email, Anchor.Centre, 1);
			if (emailState.text != email) email = emailState.text;
			pos.y -= ySpacing * 1.8f;

			// Password input - MUITO MAIOR com botão de mostrar/ocultar
			UI.DrawText("Password:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * 32, Anchor.CentreLeft, Color.white);

			// Botão toggle para mostrar/ocultar senha
			Vector2 togglePos = pos + Vector2.right * 32;
			string toggleText = showPassword ? "[Hide]" : "[Show]";
			if (Button(toggleText, togglePos, new Vector2(8, DrawSettings.ButtonHeight * 0.8f)))
			{
				showPassword = !showPassword;
			}

			pos.y -= ySpacing * 0.8f;

			// Atualizar senha com mascaramento CORRIGIDO
			string displayPassword = showPassword ? passwordActual : new string('•', passwordActual.Length);

			// Previne loop infinito: só atualiza se for input do usuário
			if (displayPassword != lastPasswordDisplayed)
			{
				lastPasswordDisplayed = displayPassword;
			}

			InputFieldState passwordState = UI.InputField(ID_PasswordInput, theme.ChipNameInputField, pos, inputSize, displayPassword, Anchor.Centre, 1);

			// Processar mudanças na senha
			if (passwordState.text != displayPassword)
			{
				if (showPassword)
				{
					// Modo texto visível - atualiza direto
					passwordActual = passwordState.text;
				}
				else
				{
					// Modo mascarado - detecta diferenças
					int newLength = passwordState.text.Length;
					int oldLength = passwordActual.Length;

					if (newLength > oldLength)
					{
						// Caracteres adicionados - pega apenas os novos caracteres
						string newChars = passwordState.text.Substring(oldLength);
						// Remove os bullets e pega apenas caracteres válidos
						newChars = newChars.Replace("•", "");
						passwordActual += newChars;
					}
					else if (newLength < oldLength)
					{
						// Caracteres removidos
						passwordActual = passwordActual.Substring(0, newLength);
					}
				}

				// Atualiza o último estado exibido
				lastPasswordDisplayed = showPassword ? passwordActual : new string('•', passwordActual.Length);
			}

			pos.y -= ySpacing * 1.8f;

			// Botão Sign in with Google
			Vector2 buttonSize = new Vector2(20, DrawSettings.ButtonHeight);
			if (Button("Sign in with Google", pos, buttonSize))
			{
				if (ValidateInputForLogin())
				{
					FirebaseAuthManager.SignInWithEmailPassword(email, passwordActual);
					statusMessage = "Signing in...";
				}
			}
			pos.y -= ySpacing;

			// Link para criar conta
			UI.DrawText("Don't have an account?", theme.FontRegular, theme.FontSizeRegular * 0.9f, pos, Anchor.Centre, Color.gray);
			pos.y -= ySpacing * 0.8f;

			if (Button("Create Account", pos, new Vector2(15, DrawSettings.ButtonHeight)))
			{
				isCreatingAccount = true;
				statusMessage = "";
			}
			pos.y -= ySpacing * 1.5f;

			DrawStatusAndOfflineButton(pos, ySpacing, theme);
		}

		static void DrawCreateAccountForm(Vector2 pos, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			UI.DrawText("CREATE ACCOUNT", theme.FontBold, theme.FontSizeRegular * 1.2f, pos, Anchor.Centre, Color.white);
			pos.y -= ySpacing;

			Vector2 inputSize = new Vector2(65, DrawSettings.ButtonHeight * 1.4f); // MUITO maior e mais alto

			// Google Email
			UI.DrawText("Google Email:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * 32, Anchor.CentreLeft, Color.white);
			pos.y -= ySpacing * 0.8f;
			InputFieldState emailState = UI.InputField(ID_EmailInput, theme.ChipNameInputField, pos, inputSize, email, Anchor.Centre, 1);
			if (emailState.text != email) email = emailState.text;
			pos.y -= ySpacing * 1.5f;

			// Password com botão toggle
			UI.DrawText("Password:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * 32, Anchor.CentreLeft, Color.white);
			UI.DrawText("(min 6 characters)", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos + Vector2.right * 32, Anchor.CentreRight, Color.gray);

			// Botão toggle para mostrar/ocultar senha
			Vector2 togglePos = pos + Vector2.right * 10;
			string toggleText = showPassword ? "[Hide]" : "[Show]";
			if (Button(toggleText, togglePos, new Vector2(8, DrawSettings.ButtonHeight * 0.8f)))
			{
				showPassword = !showPassword;
			}

			pos.y -= ySpacing * 0.8f;

			// Atualizar senha com mascaramento CORRIGIDO
			string displayPassword = showPassword ? passwordActual : new string('•', passwordActual.Length);

			// Previne loop infinito
			if (displayPassword != lastPasswordDisplayed)
			{
				lastPasswordDisplayed = displayPassword;
			}

			InputFieldState passwordState = UI.InputField(ID_PasswordInput, theme.ChipNameInputField, pos, inputSize, displayPassword, Anchor.Centre, 1);

			// Processar mudanças na senha
			if (passwordState.text != displayPassword)
			{
				if (showPassword)
				{
					passwordActual = passwordState.text;
				}
				else
				{
					int newLength = passwordState.text.Length;
					int oldLength = passwordActual.Length;

					if (newLength > oldLength)
					{
						string newChars = passwordState.text.Substring(oldLength);
						newChars = newChars.Replace("•", "");
						passwordActual += newChars;
					}
					else if (newLength < oldLength)
					{
						passwordActual = passwordActual.Substring(0, newLength);
					}
				}

				lastPasswordDisplayed = showPassword ? passwordActual : new string('•', passwordActual.Length);
			}

			pos.y -= ySpacing * 1.5f;

			// Display Name
			UI.DrawText("Display Name:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * 32, Anchor.CentreLeft, Color.white);
			pos.y -= ySpacing * 0.8f;
			InputFieldState nameState = UI.InputField(ID_DisplayNameInput, theme.ChipNameInputField, pos, inputSize, displayName, Anchor.Centre, 1);
			if (nameState.text != displayName) displayName = nameState.text;
			pos.y -= ySpacing * 1.5f;

			// Botão Create Account
			Vector2 buttonSize = new Vector2(20, DrawSettings.ButtonHeight);
			if (Button("Create Account", pos, buttonSize))
			{
				if (ValidateInputForSignup())
				{
					FirebaseAuthManager.CreateAccount(email, passwordActual, displayName);
					statusMessage = "Creating account...";
				}
			}
			pos.y -= ySpacing;

			// Botão Back
			if (Button("Back to Sign In", pos, buttonSize))
			{
				isCreatingAccount = false;
				displayName = "";
				statusMessage = "";
			}
			pos.y -= ySpacing * 1.5f;

			DrawStatusAndOfflineButton(pos, ySpacing, theme);
		}

		static void DrawStatusAndOfflineButton(Vector2 pos, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			// Status message
			if (!string.IsNullOrEmpty(statusMessage))
			{
				Color messageColor = statusMessage.Contains("Error") || statusMessage.Contains("Failed")
					? new Color(1, 0.3f, 0.3f)
					: Color.gray;

				UI.DrawText(statusMessage, theme.FontRegular, theme.FontSizeRegular * 0.9f, pos, Anchor.Centre, messageColor);
			}

			// Botão "Continue Offline"
			pos.y -= ySpacing;
			if (Button("Continue Offline", pos, new Vector2(15, DrawSettings.ButtonHeight)))
			{
				wantsOfflineMode = true;
				statusMessage = "Working offline";
			}
		}

		static bool Button(string text, Vector2 pos, Vector2 size)
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			return UI.Button(text, theme.ButtonTheme, pos, size, true, false, false, Anchor.Centre);
		}

		static bool ValidateInputForLogin()
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				statusMessage = "Error: Email required";
				return false;
			}

			if (!email.Contains("@"))
			{
				statusMessage = "Error: Please enter a valid email";
				return false;
			}

			if (string.IsNullOrWhiteSpace(passwordActual))
			{
				statusMessage = "Error: Password required";
				return false;
			}

			return true;
		}

		static bool ValidateInputForSignup()
		{
			if (!ValidateInputForLogin()) return false;

			if (passwordActual.Length < 6)
			{
				statusMessage = "Error: Password must be at least 6 characters";
				return false;
			}

			if (string.IsNullOrWhiteSpace(displayName))
			{
				statusMessage = "Error: Display name required";
				return false;
			}

			return true;
		}

		/// <summary>
		/// Subscreve aos eventos do FirebaseAuth
		/// </summary>
		public static void Initialize()
		{
			FirebaseAuthManager.OnLoginSuccess += OnLoginSuccess;
			FirebaseAuthManager.OnLogout += OnLogout;
			FirebaseAuthManager.OnAuthError += OnAuthError;
		}

		static void OnLoginSuccess(Firebase.Auth.FirebaseUser user)
		{
			statusMessage = $"Welcome, {user.DisplayName ?? user.Email}!";
			email = "";
			passwordActual = "";
			displayName = "";
			lastPasswordDisplayed = "";
		}

		static void OnLogout()
		{
			statusMessage = "";
			wantsOfflineMode = false;
			showPassword = false;
			lastPasswordDisplayed = "";
		}

		static void OnAuthError(string error)
		{
			statusMessage = $"Error: {error}";
		}
	}
}
