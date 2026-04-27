using System;
using DLS.CloudSync;
using Firebase.Auth;
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
		static string lastSeededProfileUserId = string.Empty;

		static bool IsCompletingProfile => FirebaseAuthManager.RequiresStudentProfileCompletion;

		public static void DrawFullLoginScreen()
		{
			SeedProfileFormFromCurrentUserIfNeeded();
			GetLayout(out Vector2 startPos, out Vector2 inputSize, out Vector2 primaryButtonSize, out float ySpacing);
			DrawLoginForm(startPos, inputSize, primaryButtonSize, ySpacing);
		}

		public static bool NeedsAuthentication()
		{
			return (!FirebaseAuthManager.IsLoggedIn || FirebaseAuthManager.RequiresStudentProfileCompletion) && !wantsOfflineMode;
		}

		public static bool CanProceedToMainMenu()
		{
			return (FirebaseAuthManager.IsLoggedIn && !FirebaseAuthManager.RequiresStudentProfileCompletion) || wantsOfflineMode;
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

		static void DrawSignInForm(ref Vector2 pos, Vector2 inputSize, Vector2 primaryButtonSize, float ySpacing, DrawSettings.UIThemeDLS theme)
		{
			DrawEmailField(ref pos, inputSize, ySpacing, theme, "(yourname@email.com)");
			DrawPasswordField(ref pos, inputSize, ySpacing, theme, string.Empty);

			if (Button("Sign In", pos, primaryButtonSize))
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
			DrawTeacherSelector(ref pos, inputSize, ySpacing, theme);

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
			DrawTeacherSelector(ref pos, inputSize, ySpacing, theme);

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

			if (selectedTeacherIndex < 0 || selectedTeacherIndex >= CloudSyncPolicy.SupportedTeacherNames.Count)
			{
				statusMessage = "Error: Select ERON or GUSTAVO";
				return false;
			}

			return true;
		}

		static CloudStudentProfileData BuildStudentProfileData()
		{
			return new CloudStudentProfileData(studentName, registrationNumber, GetSelectedTeacherName());
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

		static void OnLoginSuccess(FirebaseUser user)
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
