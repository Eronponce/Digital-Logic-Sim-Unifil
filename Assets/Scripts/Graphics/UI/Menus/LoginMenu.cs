using System;
using System.Collections.Generic;
using DLS.CloudSync;
using Seb.Helpers;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	/// <summary>
	/// Menu de login Firebase.
	/// Fluxo atual: email/senha, reset de senha e completude de perfil do aluno.
	/// </summary>
	public static class LoginMenu
	{
		static readonly UIHandle ID_EmailInput = new("LoginMenu_EmailInput");
		static readonly UIHandle ID_PasswordInput = new("LoginMenu_PasswordInput");
		static readonly UIHandle ID_DisplayNameInput = new("LoginMenu_DisplayNameInput");
		static readonly UIHandle ID_RegistrationInput = new("LoginMenu_RegistrationInput");
		static readonly UIHandle ID_TurmaScroll = new("LoginMenu_TurmaScroll");
		static readonly UI.ScrollViewDrawElementFunc drawTurmaEntry = DrawTurmaEntry;
		const int TurmaVisibleRows = 4;

		const string FirebaseAuthProvidersUrl = "https://console.firebase.google.com/project/logisim-eron/authentication/providers";

		static string email = "";
		static string passwordActual = "";
		static string studentName = "";
		static string registrationNumber = "";
		static string statusMessage = "";
		static bool isCreatingAccount;
		static bool wantsOfflineMode;
		static bool showPassword;
		static bool eventsRegistered;
		static bool authProviderDisabled;
		static int selectedTeacherIndex = -1;
		static List<TurmaData> availableTurmas = new();
		static int selectedTurmaIndex = -1;
		static bool turmasLoading = false;
		static bool turmasLoaded = false;
		static bool turmasFailed = false;
		static float nextTurmaRetryTime = 0f; // auto-retry: reagenda ao falhar
		static string lastSeededProfileUserId = string.Empty;

		static bool IsCompletingProfile => FirebaseAuthManager.RequiresStudentProfileCompletion;

		public static void DrawFullLoginScreen()
		{
			SeedProfileFormFromCurrentUserIfNeeded();
			// Auto-carrega e AUTO-REPETE sozinho (sem depender de clique). Ao falhar,
			// nextTurmaRetryTime é reagendado 2s à frente; enquanto não carregar, tenta
			// de novo automaticamente. Cobre a corrida com a restauração de projetos no
			// login e a indisponibilidade momentânea do túnel.
			if (!turmasLoaded && !turmasLoading && Time.realtimeSinceStartup >= nextTurmaRetryTime && FirestoreDataManager.IsReady)
			{
				LoadTurmas();
			}
			GetLayout(out Vector2 startPos, out Vector2 inputSize, out Vector2 primaryButtonSize, out float ySpacing);
			DrawLoginForm(startPos, inputSize, primaryButtonSize, ySpacing);
		}

		public static bool NeedsAuthentication()
		{
#if DLS_COMMUNITY
			return false;
#else
			return (!FirebaseAuthManager.IsLoggedIn || FirebaseAuthManager.RequiresStudentProfileCompletion) && !wantsOfflineMode;
#endif
		}

		public static bool CanProceedToMainMenu()
		{
#if DLS_COMMUNITY
			return true;
#else
			return (FirebaseAuthManager.IsLoggedIn && !FirebaseAuthManager.RequiresStudentProfileCompletion) || wantsOfflineMode;
#endif
		}

		public static void ReturnToSignIn()
		{
			wantsOfflineMode = false;
			isCreatingAccount = false;
			showPassword = false;
			authProviderDisabled = false;
			statusMessage = "";
		}

		static void GetLayout(out Vector2 startPos, out Vector2 inputSize, out Vector2 primaryButtonSize, out float ySpacing)
		{
			bool denseForm = isCreatingAccount || IsCompletingProfile;
			float inputWidth = Mathf.Clamp(UI.Width * (denseForm ? 0.58f : 0.64f), 30f, 52f);
			float buttonWidth = Mathf.Clamp(UI.Width * 0.24f, 14f, 22f);
			float buttonHeight = DrawSettings.ButtonHeight;

			inputSize = new Vector2(inputWidth, buttonHeight * 1.15f);
			primaryButtonSize = new Vector2(buttonWidth, buttonHeight);
			ySpacing = IsCompletingProfile ? 2.4f : (isCreatingAccount ? 2.55f : 3.0f);

			float startYOffset = IsCompletingProfile ? 17.4f : (isCreatingAccount ? 16.8f : 13.0f);
			startPos = UI.Centre + Vector2.up * startYOffset;
		}

		static void DrawLoginForm(Vector2 pos, Vector2 inputSize, Vector2 primaryButtonSize, float ySpacing)
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			string title = IsCompletingProfile ? "COMPLETE PROFILE" : (isCreatingAccount ? "CREATE ACCOUNT" : "SIGN IN");

			UI.DrawText(title, theme.FontBold, theme.FontSizeRegular * 1.35f, pos, Anchor.Centre, Color.white);
			pos.y -= ySpacing * 0.9f;

			string subtitle = GetSubtitle();
			Color subtitleColor = authProviderDisabled ? new Color(1f, 0.82f, 0.35f) : Color.gray;
			UI.DrawText(subtitle, theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.Centre, subtitleColor);
			pos.y -= ySpacing * 0.95f;

			if (IsCompletingProfile)
			{
				DrawCompleteProfileForm(ref pos, inputSize, primaryButtonSize, ySpacing, theme);
			}
			else if (isCreatingAccount)
			{
				DrawCreateAccountForm(ref pos, inputSize, primaryButtonSize, ySpacing, theme);
			}
			else
			{
				DrawSignInForm(ref pos, inputSize, primaryButtonSize, ySpacing, theme);
			}
		}

		static string GetSubtitle()
		{
			if (authProviderDisabled)
			{
				return "Email/password is disabled in Firebase for this project";
			}

			if (IsCompletingProfile)
			{
				return "Students must save professor, name and matricula before continuing";
			}

			return "Phase 1: email/password login with Firebase sync";
		}

		// Tab move o foco para o proximo campo da sequencia (o ultimo volta para o primeiro).
		static void HandleTabNavigation(params UIHandle[] fields)
		{
			if (!InputHelper.IsKeyDownThisFrame(KeyCode.Tab)) return;

			for (int i = 0; i < fields.Length; i++)
			{
				if (!UI.GetInputFieldState(fields[i]).focused) continue;

				UI.GetInputFieldState(fields[i]).SetFocus(false);
				UI.GetInputFieldState(fields[(i + 1) % fields.Length]).SetFocus(true);
				return;
			}
		}

		static void DrawSignInForm(ref Vector2 pos, Vector2 inputSize, Vector2 primaryButtonSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			DrawEmailField(ref pos, inputSize, ySpacing, theme, "(yourname@email.com)");
			DrawPasswordField(ref pos, inputSize, ySpacing, theme, string.Empty);

			HandleTabNavigation(ID_EmailInput, ID_PasswordInput);

			bool enterOnPassword = UI.GetInputFieldState(ID_PasswordInput).focused &&
				(InputHelper.IsKeyDownThisFrame(KeyCode.Return) || InputHelper.IsKeyDownThisFrame(KeyCode.KeypadEnter));

			DrawKeepLoggedInToggle(ref pos, primaryButtonSize, ySpacing, theme);

			if (Button("Sign In", pos, primaryButtonSize) || enterOnPassword)
			{
				if (ValidateInputForLogin())
				{
					FirebaseAuthManager.SignInWithEmailPassword(email, passwordActual);
					statusMessage = "Signing in...";
					LoadingOverlay.Show("Signing in...");
				}
			}
			pos.y -= ySpacing * 0.88f;

			if (Button("Reset Password", pos, new Vector2(primaryButtonSize.x + 2f, primaryButtonSize.y * 0.92f)))
			{
				if (ValidateEmailInput())
				{
					FirebaseAuthManager.SendPasswordReset(email);
					statusMessage = "Sending password reset email...";
					LoadingOverlay.Show("Sending password reset email...");
				}
			}
			pos.y -= ySpacing * 0.92f;

			UI.DrawText("Don't have an account yet?", theme.FontRegular, theme.FontSizeRegular * 0.85f, pos, Anchor.Centre, Color.gray);
			pos.y -= ySpacing * 0.74f;

			if (Button("Create Account", pos, primaryButtonSize))
			{
				isCreatingAccount = true;
				showPassword = false;
				statusMessage = "";
			}
			pos.y -= ySpacing * 1.08f;

			DrawStatusAndAuxButtons(ref pos, ySpacing, theme, showContinueOffline: true);
		}

		static void DrawCreateAccountForm(ref Vector2 pos, Vector2 inputSize, Vector2 primaryButtonSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			DrawEmailField(ref pos, inputSize, ySpacing, theme, string.Empty);
			DrawPasswordField(ref pos, inputSize, ySpacing, theme, "(min 6 characters)");
			DrawStudentNameField(ref pos, inputSize, ySpacing, theme);
			DrawRegistrationField(ref pos, inputSize, ySpacing, theme);
			DrawTurmaSelector(ref pos, inputSize, ySpacing, theme);

			HandleTabNavigation(ID_EmailInput, ID_PasswordInput, ID_DisplayNameInput, ID_RegistrationInput);

			if (Button("Create Account", pos, primaryButtonSize))
			{
				if (ValidateInputForSignup())
				{
					FirebaseAuthManager.CreateAccount(email, passwordActual, BuildStudentProfileData());
					statusMessage = "Creating account...";
					LoadingOverlay.Show("Creating account...");
				}
			}
			pos.y -= ySpacing * 0.84f;

			if (Button("Back to Sign In", pos, primaryButtonSize))
			{
				isCreatingAccount = false;
				showPassword = false;
				statusMessage = "";
			}
			pos.y -= ySpacing * 0.98f;

			DrawStatusAndAuxButtons(ref pos, ySpacing, theme, showContinueOffline: true);
		}

		static void DrawCompleteProfileForm(ref Vector2 pos, Vector2 inputSize, Vector2 primaryButtonSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			DrawReadOnlyEmail(ref pos, inputSize, ySpacing, theme);
			DrawStudentNameField(ref pos, inputSize, ySpacing, theme);
			DrawRegistrationField(ref pos, inputSize, ySpacing, theme);
			DrawTurmaSelector(ref pos, inputSize, ySpacing, theme);

			HandleTabNavigation(ID_DisplayNameInput, ID_RegistrationInput);

			if (Button("Save Profile", pos, primaryButtonSize))
			{
				if (ValidateStudentProfileInput())
				{
					FirebaseAuthManager.UpdateStudentProfile(BuildStudentProfileData());
					statusMessage = "Saving profile...";
					LoadingOverlay.Show("Saving profile...");
				}
			}
			pos.y -= ySpacing * 0.84f;

			if (Button("Sign Out", pos, primaryButtonSize))
			{
				FirebaseAuthManager.SignOut();
				statusMessage = "";
			}
			pos.y -= ySpacing * 0.98f;

			DrawStatusAndAuxButtons(ref pos, ySpacing, theme, showContinueOffline: false);
		}

		static void DrawReadOnlyEmail(ref Vector2 pos, Vector2 inputSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			float halfWidth = inputSize.x * 0.5f;
			UI.DrawText("Email:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * halfWidth, Anchor.CentreLeft, Color.white);
			UI.DrawText(FirebaseAuthManager.UserEmail ?? string.Empty, theme.FontRegular, theme.FontSizeRegular * 0.82f, pos + Vector2.right * halfWidth, Anchor.CentreRight, Color.gray);
			pos.y -= ySpacing * 1.05f;
		}

		static void DrawEmailField(ref Vector2 pos, Vector2 inputSize, float ySpacing, DrawSettings.UIThemeDLS theme, string helperText)
		{
			float halfWidth = inputSize.x * 0.5f;
			UI.DrawText("Email:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * halfWidth, Anchor.CentreLeft, Color.white);
			if (!string.IsNullOrWhiteSpace(helperText))
			{
				UI.DrawText(helperText, theme.FontRegular, theme.FontSizeRegular * 0.75f, pos + Vector2.right * halfWidth, Anchor.CentreRight, Color.gray);
			}

			pos.y -= ySpacing * 0.68f;
			InputFieldState emailState = UI.InputField(ID_EmailInput, LoginInputTheme(theme), pos, inputSize, email, Anchor.Centre, 1);
			if (emailState.text != email)
			{
				email = emailState.text;
			}

			pos.y -= ySpacing * 1.02f;
		}

		static void DrawPasswordField(ref Vector2 pos, Vector2 inputSize, float ySpacing, DrawSettings.UIThemeDLS theme, string helperText)
		{
			float halfWidth = inputSize.x * 0.5f;
			Vector2 toggleSize = new(7.2f, DrawSettings.ButtonHeight * 0.8f);
			Vector2 togglePos = pos + Vector2.right * (halfWidth - toggleSize.x * 0.5f);

			UI.DrawText("Password:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * halfWidth, Anchor.CentreLeft, Color.white);
			if (!string.IsNullOrWhiteSpace(helperText))
			{
				UI.DrawText(helperText, theme.FontRegular, theme.FontSizeRegular * 0.75f, pos + Vector2.right * (halfWidth - toggleSize.x - 1.1f), Anchor.CentreRight, Color.gray);
			}

			if (Button(showPassword ? "Hide" : "Show", togglePos, toggleSize))
			{
				showPassword = !showPassword;
			}

			pos.y -= ySpacing * 0.68f;

			InputFieldState passwordState = UI.InputField(ID_PasswordInput, LoginInputTheme(theme), pos, inputSize, passwordActual, Anchor.Centre, 1, displayTextOverride: null, maskContents: !showPassword);
			if (passwordState.text != passwordActual)
			{
				passwordActual = passwordState.text;
			}

			pos.y -= ySpacing * 1.02f;
		}

		static void DrawKeepLoggedInToggle(ref Vector2 pos, Vector2 primaryButtonSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			bool current = FirebaseAuthManager.KeepLoggedIn;
			string label = current ? "[x] Manter logado" : "[ ] Manter logado";
			if (Button(label, pos, new Vector2(primaryButtonSize.x + 4f, primaryButtonSize.y * 0.85f)))
			{
				FirebaseAuthManager.KeepLoggedIn = !current;
			}
			pos.y -= ySpacing * 0.82f;
		}

		static void DrawTurmaSelector(ref Vector2 pos, Vector2 inputSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			float halfWidth = inputSize.x * 0.5f;
			UI.DrawText("Turma:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * halfWidth, Anchor.CentreLeft, Color.white);
			pos.y -= ySpacing * 0.6f;

			if (!turmasLoaded)
			{
				// Enquanto não carregou, está sempre carregando ou prestes a tentar de novo
				// (auto-retry), então a mensagem é sempre "Carregando...". O botão continua
				// como atalho para forçar uma tentativa imediata.
				UI.DrawText("Carregando turmas...", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.Centre, Color.gray);

				if (!turmasLoading && Button("Tentar agora", pos + Vector2.right * (inputSize.x * 0.3f), new Vector2(14f, DrawSettings.ButtonHeight * 0.8f)))
				{
					nextTurmaRetryTime = 0f;
					LoadTurmas();
				}
				pos.y -= ySpacing * 1.02f;
				return;
			}

			if (availableTurmas.Count == 0)
			{
				UI.DrawText("Nenhuma turma disponivel.", theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.Centre, Color.gray);
				pos.y -= ySpacing * 1.02f;
				return;
			}

			// Lista vertical rolável (em vez da fileira horizontal antiga, que
			// espremia os botões e sobrepunha o texto quando havia várias turmas).
			// Mostra até TurmaVisibleRows de uma vez; rola para ver o resto.
			float rowHeight = DrawSettings.ButtonHeight * 0.9f;
			float rowSpacing = 0.4f;
			int visibleRows = Mathf.Min(availableTurmas.Count, TurmaVisibleRows);
			float listHeight = visibleRows * rowHeight + Mathf.Max(0, visibleRows - 1) * rowSpacing;
			Vector2 topLeft = pos + new Vector2(-inputSize.x * 0.5f, 0f);

			UI.DrawScrollView(ID_TurmaScroll, topLeft, new Vector2(inputSize.x, listHeight), rowSpacing, Anchor.TopLeft, theme.ScrollTheme, drawTurmaEntry, availableTurmas.Count);

			pos.y -= listHeight + ySpacing * 0.5f;
		}

		static void DrawTurmaEntry(Vector2 topLeft, float width, int index, bool isLayoutPass)
		{
			// UI.Button precisa ser chamado nos DOIS passes do ScrollView (medida e
			// desenho) — ele sempre atualiza PrevBounds/o bounds-scope no final,
			// mesmo sem renderizar (Seb.Vis.UI.OnFinishedDrawingUIElement roda
			// incondicionalmente). Pular a chamada no passe de medida (como estava
			// antes) fazia o ScrollView calcular altura de conteúdo zero, travando
			// o scroll e tornando as turmas além da primeira inalcançáveis.
			float rowHeight = DrawSettings.ButtonHeight * 0.9f;
			ButtonTheme btnTheme = index == selectedTurmaIndex
				? DrawSettings.ActiveUITheme.ProjectSelectionButtonSelected
				: DrawSettings.ActiveUITheme.ProjectSelectionButton;
			if (UI.Button(TurmaEntryLabel(index), btnTheme, topLeft, new Vector2(width, rowHeight), true, false, false, Anchor.TopLeft))
			{
				selectedTurmaIndex = index;
			}

			if (!isLayoutPass)
			{
				// Linha fina separando as turmas — sem isto, com o tema de botão
				// não-selecionado transparente, as linhas ficam parecendo texto solto
				// empilhado em vez de uma lista de opções clicáveis.
				Vector2 dividerPos = topLeft + Vector2.down * rowHeight;
				UI.DrawPanel(dividerPos, new Vector2(width, 0.12f), new Color(1f, 1f, 1f, 0.08f), Anchor.TopLeft);
			}
		}

		// DisplayName não é único (duas turmas podem se chamar "Turma A"). Quando
		// há colisão de nome, junta o professor/projeto para o aluno conseguir
		// diferenciar qual é qual — sem isto, clicar escolhe um id arbitrário
		// entre as duplicatas sem nenhuma pista visual de qual foi selecionado.
		static string TurmaEntryLabel(int index)
		{
			TurmaData turma = availableTurmas[index];
			bool nameIsAmbiguous = false;
			for (int i = 0; i < availableTurmas.Count; i++)
			{
				if (i != index && string.Equals(availableTurmas[i].DisplayName, turma.DisplayName, StringComparison.OrdinalIgnoreCase))
				{
					nameIsAmbiguous = true;
					break;
				}
			}
			return nameIsAmbiguous ? $"{turma.DisplayName} ({turma.TeacherName} · {turma.ProjectName})" : turma.DisplayName;
		}

		static void LoadTurmas()
		{
			if (turmasLoading) return;
			turmasLoading = true;
			turmasLoaded = false;
			turmasFailed = false;
			FirestoreDataManager.LoadTurmas(turmas =>
			{
				availableTurmas = turmas ?? new List<TurmaData>();
				turmasLoading = false;
				turmasLoaded = true;
			}, err =>
			{
				turmasLoading = false;
				turmasLoaded = false;
				turmasFailed = true;
				nextTurmaRetryTime = Time.realtimeSinceStartup + 2f; // tenta de novo em 2s
			});
		}

		static void DrawStudentNameField(ref Vector2 pos, Vector2 inputSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			float halfWidth = inputSize.x * 0.5f;
			UI.DrawText("Name:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * halfWidth, Anchor.CentreLeft, Color.white);
			pos.y -= ySpacing * 0.68f;

			InputFieldState nameState = UI.InputField(ID_DisplayNameInput, LoginInputTheme(theme), pos, inputSize, studentName, Anchor.Centre, 1);
			if (nameState.text != studentName)
			{
				studentName = nameState.text;
			}

			pos.y -= ySpacing * 1.02f;
		}

		static void DrawRegistrationField(ref Vector2 pos, Vector2 inputSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			float halfWidth = inputSize.x * 0.5f;
			UI.DrawText("Matricula:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * halfWidth, Anchor.CentreLeft, Color.white);
			pos.y -= ySpacing * 0.68f;

			InputFieldState registrationState = UI.InputField(ID_RegistrationInput, LoginInputTheme(theme), pos, inputSize, registrationNumber, Anchor.Centre, 1);
			if (registrationState.text != registrationNumber)
			{
				registrationNumber = registrationState.text;
			}

			pos.y -= ySpacing * 1.02f;
		}

		static void DrawTeacherSelector(ref Vector2 pos, Vector2 inputSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			float halfWidth = inputSize.x * 0.5f;
			UI.DrawText("Professor:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * halfWidth, Anchor.CentreLeft, Color.white);
			pos.y -= ySpacing * 0.6f;

			float buttonWidth = Mathf.Min(10.5f, inputSize.x * 0.32f);
			float spacing = 1.2f;
			float totalWidth = buttonWidth * CloudSyncPolicy.SupportedTeacherNames.Count + spacing * (CloudSyncPolicy.SupportedTeacherNames.Count - 1);
			float startX = pos.x - totalWidth * 0.5f + buttonWidth * 0.5f;

			for (int i = 0; i < CloudSyncPolicy.SupportedTeacherNames.Count; i++)
			{
				ButtonTheme themeToUse = i == selectedTeacherIndex ? theme.ProjectSelectionButtonSelected : theme.ProjectSelectionButton;
				Vector2 buttonPos = new(startX + i * (buttonWidth + spacing), pos.y);
				if (UI.Button(CloudSyncPolicy.SupportedTeacherNames[i], themeToUse, buttonPos, new Vector2(buttonWidth, DrawSettings.ButtonHeight * 0.88f), true, false, false, Anchor.Centre))
				{
					selectedTeacherIndex = i;
				}
			}

			pos.y -= ySpacing * 1.02f;
		}

		static InputFieldTheme LoginInputTheme(DrawSettings.UIThemeDLS theme)
		{
			InputFieldTheme inputTheme = theme.ChipNameInputField;
			inputTheme.font = theme.FontRegular;
			inputTheme.fontSize = theme.FontSizeRegular * 0.96f;
			return inputTheme;
		}

		static void DrawStatusAndAuxButtons(ref Vector2 pos, float ySpacing, DrawSettings.UIThemeDLS theme, bool showContinueOffline)
		{
			if (!string.IsNullOrEmpty(statusMessage))
			{
				Color messageColor = statusMessage.Contains("Error", StringComparison.OrdinalIgnoreCase)
					? new Color(1f, 0.35f, 0.35f)
					: (authProviderDisabled ? new Color(1f, 0.82f, 0.35f) : Color.gray);

				UI.DrawText(statusMessage, theme.FontRegular, theme.FontSizeRegular * 0.82f, pos, Anchor.Centre, messageColor);
				pos.y -= ySpacing * 0.8f;
			}

			if (authProviderDisabled)
			{
				if (Button("Open Firebase Auth Console", pos, new Vector2(24, DrawSettings.ButtonHeight * 0.9f)))
				{
					Application.OpenURL(FirebaseAuthProvidersUrl);
				}

				pos.y -= ySpacing * 0.86f;
			}

			if (showContinueOffline && Button("Continue Offline", pos, new Vector2(16, DrawSettings.ButtonHeight * 0.9f)))
			{
				wantsOfflineMode = true;
				isCreatingAccount = false;
				showPassword = false;
				statusMessage = "Working offline";
			}
		}

		static bool Button(string text, Vector2 pos, Vector2 size)
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			return UI.Button(text, theme.ButtonTheme, pos, size, true, false, false, Anchor.Centre);
		}

		static bool ValidateEmailInput()
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

			return true;
		}

		static bool ValidateInputForLogin()
		{
			if (!ValidateEmailInput())
			{
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
			if (!ValidateInputForLogin())
			{
				return false;
			}

			if (passwordActual.Length < 6)
			{
				statusMessage = "Error: Password must be at least 6 characters";
				return false;
			}

			AppUserRole role = FirebaseAuthManager.GetSuggestedRoleForEmail(email);
			if (CloudSyncPolicy.RequiresStudentProfile(role) && !ValidateStudentProfileInput())
			{
				return false;
			}

			return true;
		}

		static bool ValidateStudentProfileInput()
		{
			if (string.IsNullOrWhiteSpace(studentName))
			{
				statusMessage = "Error: Name required";
				return false;
			}

			if (string.IsNullOrWhiteSpace(registrationNumber))
			{
				statusMessage = "Error: Matricula required";
				return false;
			}

			if (selectedTurmaIndex < 0 || selectedTurmaIndex >= availableTurmas.Count)
			{
				statusMessage = "Error: Selecione sua turma";
				return false;
			}

			return true;
		}

		static CloudStudentProfileData BuildStudentProfileData()
		{
			TurmaData turma = selectedTurmaIndex >= 0 && selectedTurmaIndex < availableTurmas.Count
				? availableTurmas[selectedTurmaIndex]
				: null;
			string teacherName = turma?.TeacherName ?? GetSelectedTeacherName();
			string turmaId = turma?.Id ?? string.Empty;
			string turmaProjectName = turma?.ProjectName ?? string.Empty;
			return new CloudStudentProfileData(studentName, registrationNumber, teacherName, turmaId, turmaProjectName);
		}

		static string GetSelectedTeacherName()
		{
			return selectedTeacherIndex >= 0 && selectedTeacherIndex < CloudSyncPolicy.SupportedTeacherNames.Count
				? CloudSyncPolicy.SupportedTeacherNames[selectedTeacherIndex]
				: string.Empty;
		}

		static void SeedProfileFormFromCurrentUserIfNeeded()
		{
			if (!IsCompletingProfile)
			{
				lastSeededProfileUserId = string.Empty;
				return;
			}

			string currentUserId = FirebaseAuthManager.UserId ?? string.Empty;
			if (string.IsNullOrWhiteSpace(currentUserId) || currentUserId == lastSeededProfileUserId)
			{
				return;
			}

			lastSeededProfileUserId = currentUserId;
			studentName = FirebaseAuthManager.CurrentUserProfile.DisplayName;
			registrationNumber = FirebaseAuthManager.CurrentUserProfile.RegistrationNumber;
			selectedTeacherIndex = CloudSyncPolicy.GetTeacherIndex(FirebaseAuthManager.CurrentUserProfile.TeacherName);
			string currentTurmaId = FirebaseAuthManager.CurrentUserProfile.TurmaId;
			selectedTurmaIndex = availableTurmas.FindIndex(t => t.Id == currentTurmaId);
			SetInputFieldText(ID_DisplayNameInput, studentName);
			SetInputFieldText(ID_RegistrationInput, registrationNumber);
			statusMessage = "Complete your profile before continuing.";
		}

		static void SetInputFieldText(UIHandle id, string value)
		{
			InputFieldState state = UI.GetInputFieldState(id);
			state.SetText(value ?? string.Empty, focus: false);
		}

		static void ClearAllFormFields(bool keepEmail = false)
		{
			if (!keepEmail)
			{
				email = "";
				UI.GetInputFieldState(ID_EmailInput).ClearText();
			}

			passwordActual = "";
			studentName = "";
			registrationNumber = "";
			selectedTeacherIndex = -1;
			selectedTurmaIndex = -1;
			lastSeededProfileUserId = string.Empty;
			UI.GetInputFieldState(ID_PasswordInput).ClearText();
			UI.GetInputFieldState(ID_DisplayNameInput).ClearText();
			UI.GetInputFieldState(ID_RegistrationInput).ClearText();
		}

		public static void Initialize()
		{
			if (eventsRegistered)
			{
				return;
			}

			FirebaseAuthManager.OnLoginSuccess += OnLoginSuccess;
			FirebaseAuthManager.OnUserProfileReady += OnUserProfileReady;
			FirebaseAuthManager.OnLogout += OnLogout;
			FirebaseAuthManager.OnAuthInfo += OnAuthInfo;
			FirebaseAuthManager.OnAuthError += OnAuthError;
			eventsRegistered = true;
		}

		static void OnLoginSuccess(AuthUser user)
		{
			authProviderDisabled = false;
			showPassword = false;
			passwordActual = "";
			UI.GetInputFieldState(ID_PasswordInput).ClearText();

			if (FirebaseAuthManager.RequiresStudentProfileCompletion)
			{
				SeedProfileFormFromCurrentUserIfNeeded();
				statusMessage = "Complete your profile before continuing.";
				LoadingOverlay.Hide();
				return;
			}

			statusMessage = $"Welcome, {FirebaseAuthManager.CurrentUserProfile.DisplayName}! Role: {FirebaseAuthManager.CurrentUserRoleLabel}";
			ClearAllFormFields();
			LoadingOverlay.Hide();
		}

		static void OnUserProfileReady(CloudUserProfile profile)
		{
			if (profile.RequiresStudentProfileCompletion)
			{
				lastSeededProfileUserId = string.Empty;
				SeedProfileFormFromCurrentUserIfNeeded();
				statusMessage = "Complete your profile before continuing.";
				LoadingOverlay.Hide();
				return;
			}

			if (FirebaseAuthManager.IsLoggedIn)
			{
				statusMessage = $"Welcome, {profile.DisplayName}! Role: {profile.RoleLabel}";
			}

			LoadingOverlay.Hide();
		}

		static void OnLogout()
		{
			statusMessage = "";
			wantsOfflineMode = false;
			isCreatingAccount = false;
			showPassword = false;
			authProviderDisabled = false;
			selectedTurmaIndex = -1;
			turmasLoaded = false;
			turmasFailed = false;
			ClearAllFormFields();
			LoadingOverlay.Hide();
		}

		static void OnAuthInfo(string message)
		{
			authProviderDisabled = false;
			statusMessage = message ?? string.Empty;
			LoadingOverlay.Hide();
		}

		static void OnAuthError(string error)
		{
			authProviderDisabled = IsFirebaseProviderDisabledError(error);
			statusMessage = $"Error: {error}";
			LoadingOverlay.Hide();
		}

		static bool IsFirebaseProviderDisabledError(string error)
		{
			if (string.IsNullOrWhiteSpace(error))
			{
				return false;
			}

			string normalized = error.ToLowerInvariant();
			return normalized.Contains("operation_not_allowed")
				|| normalized.Contains("this operation is not allowed")
				|| normalized.Contains("email/password auth is disabled")
				|| normalized.Contains("authentication > sign-in method");
		}
	}
}
