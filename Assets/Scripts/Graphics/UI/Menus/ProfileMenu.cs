using System;
using System.Collections.Generic;
using DLS.CloudSync;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class ProfileMenu
	{
		static readonly UIHandle ID_StudentNameInput = new("ProfileMenu_StudentNameInput");
		static readonly UIHandle ID_RegistrationInput = new("ProfileMenu_RegistrationInput");
		static readonly UIHandle ID_NewPasswordInput = new("ProfileMenu_NewPasswordInput");
		static readonly UIHandle ID_ConfirmPasswordInput = new("ProfileMenu_ConfirmPasswordInput");
		static readonly UIHandle ID_TurmaScroll = new("ProfileMenu_TurmaScroll");
		static readonly UI.ScrollViewDrawElementFunc drawTurmaEntry = DrawTurmaEntry;
		const int TurmaVisibleRows = 4;

		static string studentName = string.Empty;
		static string registrationNumber = string.Empty;
		static string newPassword = string.Empty;
		static string confirmPassword = string.Empty;
		static int selectedTurmaIndex = -1;
		static List<TurmaData> availableTurmas = new();
		static bool turmasLoading;
		static bool turmasLoaded;
		static bool turmasFailed;
		static string statusMessage = string.Empty;
		static bool eventsRegistered;
		static bool isSavingProfile;
		static int pendingOperations;
		static bool saveHasError;
		static string lastSaveError = string.Empty;
		static bool returnToMainAfterSave;

		public static void Initialize()
		{
			RegisterEventsIfNeeded();

			CloudUserProfile profile = FirebaseAuthManager.CurrentUserProfile;
			studentName = profile?.DisplayName ?? string.Empty;
			registrationNumber = profile?.RegistrationNumber ?? string.Empty;
			newPassword = string.Empty;
			confirmPassword = string.Empty;
			statusMessage = string.Empty;
			isSavingProfile = false;
			pendingOperations = 0;
			saveHasError = false;
			lastSaveError = string.Empty;
			returnToMainAfterSave = false;

			SetInputFieldText(ID_StudentNameInput, studentName);
			SetInputFieldText(ID_RegistrationInput, registrationNumber);
			SetInputFieldText(ID_NewPasswordInput, string.Empty);
			SetInputFieldText(ID_ConfirmPasswordInput, string.Empty);

			if (!turmasLoaded && !turmasLoading && !turmasFailed && FirestoreDataManager.IsReady)
			{
				LoadTurmas(profile?.TurmaId ?? string.Empty);
			}
			else if (turmasLoaded)
			{
				string currentTurmaId = profile?.TurmaId ?? string.Empty;
				selectedTurmaIndex = availableTurmas.FindIndex(t => t.Id == currentTurmaId);
			}
		}

		public static bool DrawProfileScreen()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			Vector2 inputSize = new(Mathf.Clamp(UI.Width * 0.58f, 30f, 52f), DrawSettings.ButtonHeight * 1.15f);
			Vector2 primaryButtonSize = new(Mathf.Clamp(UI.Width * 0.24f, 14f, 22f), DrawSettings.ButtonHeight);
			float ySpacing = 2.4f;
			// pos começa abaixo do título fixo "DIGITAL LOGIC SIM" (desenhado por
			// MainMenu.Draw em todas as telas) — um valor mais alto aqui volta a
			// sobrepor os dois títulos, como ocorria antes.
			Vector2 pos = UI.Centre + Vector2.up * 11f;

			if (!turmasLoaded && !turmasLoading && !turmasFailed && FirestoreDataManager.IsReady)
			{
				LoadTurmas(FirebaseAuthManager.CurrentUserProfile?.TurmaId ?? string.Empty);
			}

			UI.DrawText("Update your student profile information", theme.FontRegular, theme.FontSizeRegular * 0.85f, pos, Anchor.Centre, Color.gray);
			pos.y -= ySpacing * 0.95f;

			DrawReadOnlyEmail(ref pos, inputSize, ySpacing, theme);
			DrawStudentNameField(ref pos, inputSize, ySpacing, theme);
			DrawRegistrationField(ref pos, inputSize, ySpacing, theme);
			DrawTurmaSelector(ref pos, inputSize, ySpacing, theme);
			DrawPasswordField(ref pos, inputSize, ySpacing, theme, "New Password:", ID_NewPasswordInput, ref newPassword);
			DrawPasswordField(ref pos, inputSize, ySpacing, theme, "Confirm Password:", ID_ConfirmPasswordInput, ref confirmPassword);

			bool canInteract = !isSavingProfile;
			Vector2 buttonGroupPos = pos;
			bool saveClicked = UI.Button("Save Changes", theme.ButtonTheme, buttonGroupPos + Vector2.left * (primaryButtonSize.x * 0.58f), primaryButtonSize, canInteract, false, false, Anchor.Centre);
			bool backClicked = UI.Button("Back", theme.ButtonTheme, buttonGroupPos + Vector2.right * (primaryButtonSize.x * 0.58f), primaryButtonSize, canInteract, false, false, Anchor.Centre);
			pos.y -= ySpacing * 0.9f;

			if (saveClicked)
			{
				SaveProfile();
			}

			if (!string.IsNullOrWhiteSpace(statusMessage))
			{
				Color messageColor = statusMessage.Contains("Error", StringComparison.OrdinalIgnoreCase)
					? new Color(1f, 0.35f, 0.35f)
					: Color.gray;
				UI.DrawText(statusMessage, theme.FontRegular, theme.FontSizeRegular * 0.82f, pos, Anchor.Centre, messageColor);
			}

			// Salvo com sucesso: some Salvar/Voltar sozinho, sem esperar o professor
			// clicar em "Back" — o pedido já terminou (statusMessage só chega aqui
			// depois que CompletePendingOperation confirma sucesso).
			bool autoReturn = returnToMainAfterSave;
			returnToMainAfterSave = false;
			return backClicked || autoReturn;
		}

		static void DrawReadOnlyEmail(ref Vector2 pos, Vector2 inputSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			float halfWidth = inputSize.x * 0.5f;
			UI.DrawText("Email:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * halfWidth, Anchor.CentreLeft, Color.white);
			UI.DrawText(FirebaseAuthManager.UserEmail ?? string.Empty, theme.FontRegular, theme.FontSizeRegular * 0.82f, pos + Vector2.right * halfWidth, Anchor.CentreRight, Color.gray);
			pos.y -= ySpacing * 1.05f;
		}

		static void DrawStudentNameField(ref Vector2 pos, Vector2 inputSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			float halfWidth = inputSize.x * 0.5f;
			UI.DrawText("Name:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * halfWidth, Anchor.CentreLeft, Color.white);
			pos.y -= ySpacing * 0.68f;

			InputFieldState nameState = UI.InputField(ID_StudentNameInput, CreateInputTheme(theme), pos, inputSize, studentName, Anchor.Centre, 1);
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

			InputFieldState registrationState = UI.InputField(ID_RegistrationInput, CreateInputTheme(theme), pos, inputSize, registrationNumber, Anchor.Centre, 1);
			if (registrationState.text != registrationNumber)
			{
				registrationNumber = registrationState.text;
			}

			pos.y -= ySpacing * 1.02f;
		}

		static void DrawTurmaSelector(ref Vector2 pos, Vector2 inputSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			float halfWidth = inputSize.x * 0.5f;
			UI.DrawText("Turma:", theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * halfWidth, Anchor.CentreLeft, Color.white);
			pos.y -= ySpacing * 0.6f;

			if (!turmasLoaded)
			{
				Color statusCol = turmasLoading ? Color.gray : new Color(1f, 0.7f, 0.3f);
				string msg = turmasLoading ? "Carregando turmas..." : "Sem turmas.";
				UI.DrawText(msg, theme.FontRegular, theme.FontSizeRegular * 0.8f, pos, Anchor.Centre, statusCol);

				if (!turmasLoading)
				{
					Vector2 reloadBtnPos = pos + Vector2.right * (inputSize.x * 0.3f);
					if (UI.Button("Carregar", theme.ButtonTheme, reloadBtnPos, new Vector2(12f, DrawSettings.ButtonHeight * 0.8f), true, false, false, Anchor.Centre))
					{
						LoadTurmas(FirebaseAuthManager.CurrentUserProfile?.TurmaId ?? string.Empty);
					}
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
			if (UI.Button(TurmaEntryLabel(index), btnTheme, topLeft, new Vector2(width, rowHeight), !isSavingProfile, false, false, Anchor.TopLeft))
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

		static void DrawPasswordField(ref Vector2 pos, Vector2 inputSize, float ySpacing, DrawSettings.UIThemeDLS theme, string label, UIHandle handle, ref string value)
		{
			float halfWidth = inputSize.x * 0.5f;
			UI.DrawText(label, theme.FontRegular, theme.FontSizeRegular, pos + Vector2.left * halfWidth, Anchor.CentreLeft, Color.white);
			pos.y -= ySpacing * 0.68f;

			InputFieldState passwordState = UI.InputField(handle, CreateInputTheme(theme), pos, inputSize, value, Anchor.Centre, 1, displayTextOverride: null, maskContents: true);
			if (passwordState.text != value)
			{
				value = passwordState.text;
			}

			pos.y -= ySpacing * 1.02f;
		}

		static InputFieldTheme CreateInputTheme(DrawSettings.UIThemeDLS theme)
		{
			InputFieldTheme inputTheme = theme.ChipNameInputField;
			inputTheme.font = theme.FontRegular;
			inputTheme.fontSize = theme.FontSizeRegular * 0.96f;
			return inputTheme;
		}

		static void LoadTurmas(string currentTurmaId)
		{
			if (turmasLoading) return;
			turmasLoading = true;
			turmasLoaded = false;
			turmasFailed = false;
			FirestoreDataManager.LoadTurmas(turmas =>
			{
				availableTurmas = turmas ?? new List<TurmaData>();
				selectedTurmaIndex = availableTurmas.FindIndex(t => t.Id == currentTurmaId);
				turmasLoading = false;
				turmasLoaded = true;
			}, _ =>
			{
				turmasLoading = false;
				turmasLoaded = false;
				turmasFailed = true;
			});
		}

		static bool ValidateInput()
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

			bool hasPasswordInput = !string.IsNullOrWhiteSpace(newPassword) || !string.IsNullOrWhiteSpace(confirmPassword);
			if (hasPasswordInput)
			{
				if (newPassword.Length < 6)
				{
					statusMessage = "Error: Password must be at least 6 characters";
					return false;
				}

				if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
				{
					statusMessage = "Error: Password confirmation does not match";
					return false;
				}
			}

			return true;
		}

		static void SaveProfile()
		{
			if (!ValidateInput())
			{
				return;
			}

			TurmaData turma = selectedTurmaIndex >= 0 && selectedTurmaIndex < availableTurmas.Count
				? availableTurmas[selectedTurmaIndex]
				: null;

			bool shouldUpdatePassword = !string.IsNullOrWhiteSpace(newPassword);

			isSavingProfile = true;
			pendingOperations = shouldUpdatePassword ? 2 : 1;
			saveHasError = false;
			lastSaveError = string.Empty;
			statusMessage = "Saving profile...";
			LoadingOverlay.Show("Saving profile...");

			if (shouldUpdatePassword)
			{
				FirebaseAuthManager.UpdatePassword(newPassword);
			}

			CloudStudentProfileData profileData = new(
				studentName,
				registrationNumber,
				turma?.TeacherName ?? string.Empty,
				turma?.Id ?? string.Empty,
				turma?.ProjectName ?? string.Empty);

			FirebaseAuthManager.UpdateStudentProfile(profileData);
		}

		static void RegisterEventsIfNeeded()
		{
			if (eventsRegistered)
			{
				return;
			}

			FirebaseAuthManager.OnAuthInfo += OnAuthInfo;
			FirebaseAuthManager.OnAuthError += OnAuthError;
			FirebaseAuthManager.OnLogout += OnLogout;
			eventsRegistered = true;
		}

		static void OnAuthInfo(string _)
		{
			if (!isSavingProfile)
			{
				return;
			}

			CompletePendingOperation(false, string.Empty);
		}

		static void OnAuthError(string error)
		{
			if (!isSavingProfile)
			{
				return;
			}

			CompletePendingOperation(true, error);
		}

		static void OnLogout()
		{
			isSavingProfile = false;
			pendingOperations = 0;
			saveHasError = false;
			lastSaveError = string.Empty;
			statusMessage = string.Empty;
			turmasLoaded = false;
			turmasFailed = false;
			LoadingOverlay.Hide();
		}

		static void CompletePendingOperation(bool failed, string errorMessage)
		{
			if (failed)
			{
				saveHasError = true;
				lastSaveError = string.IsNullOrWhiteSpace(errorMessage) ? "Failed to save profile." : errorMessage;
			}

			pendingOperations = Mathf.Max(0, pendingOperations - 1);
			if (pendingOperations > 0)
			{
				return;
			}

			isSavingProfile = false;
			LoadingOverlay.Hide();

			if (saveHasError)
			{
				statusMessage = $"Error: {lastSaveError}";
				return;
			}

			newPassword = string.Empty;
			confirmPassword = string.Empty;
			SetInputFieldText(ID_NewPasswordInput, string.Empty);
			SetInputFieldText(ID_ConfirmPasswordInput, string.Empty);
			statusMessage = "Profile updated successfully!";
			returnToMainAfterSave = true;
		}

		static void SetInputFieldText(UIHandle id, string value)
		{
			UI.GetInputFieldState(id).SetText(value ?? string.Empty, focus: false);
		}
	}
}
