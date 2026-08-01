using System;
using System.Threading.Tasks;
using DLS.SaveSystem;
using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Gerenciador de autenticação. Antes usava Firebase Auth; agora usa o Supabase
	/// Auth (GoTrue) via SupabaseAuthClient. O nome da classe e a API pública estática
	/// foram preservados para não quebrar os menus que a consomem.
	/// Login operacional: email/senha, com perfil salvo no Postgres (via MirrorApiClient).
	/// </summary>
	public class FirebaseAuthManager : MonoBehaviour
	{
		public static FirebaseAuthManager Instance { get; private set; }
		public static AuthUser CurrentUser => SupabaseAuthClient.CurrentUser;
		public static bool IsLoggedIn => CurrentUser != null;
		public static string UserId => CurrentUser?.UserId;
		public static string UserEmail => CurrentUser?.Email;
		public static string UserDisplayName => CurrentUser?.DisplayName;
		public static CloudUserProfile CurrentUserProfile { get; private set; } = CloudUserProfile.Offline;
		public static bool IsTeacher => CurrentUserProfile.IsTeacher;
		public static bool RequiresStudentProfileCompletion => IsLoggedIn && CurrentUserProfile.RequiresStudentProfileCompletion;
		public static string CurrentUserRoleLabel => CurrentUserProfile.RoleLabel;

		// True do momento do login até a restauração dos projetos da nuvem terminar.
		// A UI usa isto para segurar a interação (overlay "Carregando...") no login,
		// evitando que o aluno clique antes de os projetos aparecerem.
		public static bool IsRestoringCloudProjects { get; private set; }

		// True do clique em "Sign Out" até o logout terminar de verdade (sync final
		// da nuvem + limpeza da sessão). Enquanto true, IsLoggedIn ainda é true — a
		// UI usa esta flag (não IsLoggedIn) para travar a tela com um overlay
		// "Saindo..." e evitar o bug de "volta pro menu principal sem deslogar":
		// sem o overlay, a tela de login via NeedsAuthentication()==false (porque
		// IsLoggedIn ainda não caiu) e voltava direto pro Main antes do logout
		// terminar de verdade.
		public static bool IsSigningOut { get; private set; }

		public static event Action<AuthUser> OnLoginSuccess;
		public static event Action<CloudUserProfile> OnUserProfileReady;
		public static event Action OnLogout;
		public static event Action<string> OnAuthInfo;
		public static event Action<string> OnAuthError;

		[Header("Debug")]
		[SerializeField] bool showDebugLogs = true;

		[Header("Role Bootstrap")]
		[SerializeField] string[] teacherEmailAllowlist = Array.Empty<string>();

		const string KeepLoggedInKey = "DLS_KeepLoggedIn";

		public static bool KeepLoggedIn
		{
			get => PlayerPrefs.GetInt(KeepLoggedInKey, 0) == 1;
			set { PlayerPrefs.SetInt(KeepLoggedInKey, value ? 1 : 0); PlayerPrefs.Save(); }
		}

		bool signOutInProgress;
		CloudStudentProfileData pendingStudentProfileData;

		void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);
		}

		async void Start()
		{
			// Descobre a URL do servidor (GitHub) antes de qualquer autenticação.
			await MirrorConfigProvider.EnsureDiscoveredAsync();

			// Em computador compartilhado, só restaura sessão se "manter logado".
			if (KeepLoggedIn)
			{
				AuthUser restored = await SupabaseAuthClient.TryRestoreSessionAsync();
				if (restored != null)
				{
					Log($"Sessão restaurada: {restored.Email}");
					await FinalizeSignIn(restored);
					return;
				}
			}
			else
			{
				SupabaseAuthClient.SignOut();
			}

			CurrentUserProfile = CloudUserProfile.Offline;
			SavePaths.UseOfflineProfile();
		}

		async Task FinalizeSignIn(AuthUser user)
		{
			AppUserRole suggestedRole = CloudSyncPolicy.ResolveSuggestedRole(user.Email, teacherEmailAllowlist);
			CloudStudentProfileData studentProfileData = ConsumePendingStudentProfileData();

			var tcs = new TaskCompletionSource<bool>();
			FirestoreDataManager.UpsertUserProfile(user, suggestedRole, studentProfileData,
				onSuccess: profile => { CompleteSignIn(user, profile); tcs.TrySetResult(true); },
				onError: error =>
				{
					LogError($"Falha ao sincronizar perfil: {error}");
					CompleteSignIn(user, CreateFallbackProfile(user, suggestedRole, studentProfileData));
					tcs.TrySetResult(true);
				});
			await tcs.Task;
		}

		void CompleteSignIn(AuthUser user, CloudUserProfile profile)
		{
			ApplyUserProfile(user, profile);

			// Segura a interação até o restore terminar (ver IsRestoringCloudProjects).
			// onComplete é SEMPRE chamado (não-logado/0 bundles/sucesso/erro), então a
			// flag nunca fica presa em true.
			IsRestoringCloudProjects = true;
			SaverCloudExtension.LoadAllProjectsFromCloud(loadedCount =>
			{
				IsRestoringCloudProjects = false;
				if (loadedCount > 0)
				{
					Log($"Loaded {loadedCount} projects from cloud");
				}
				// após reconciliar com a nuvem, empurra o estado local para a fila,
				// para que circuitos que ainda não subiram vão sozinhos (sem travar).
				SaverCloudExtension.EnqueueLocalProjectsForUpload();
			});

			OnLoginSuccess?.Invoke(user);
		}

		void ApplyUserProfile(AuthUser user, CloudUserProfile profile)
		{
			CurrentUserProfile = profile;
			SavePaths.UseCloudProfile(user.UserId);
			Log($"Save profile switched to: {SavePaths.ActiveProfileDataPath}");
			// carrega a fila de reenvio deste usuário e tenta drenar pendências
			CloudSaveStatus.Reset();
			Outbox.ReloadForActiveProfile();
			OnUserProfileReady?.Invoke(profile);
		}

		CloudUserProfile CreateFallbackProfile(AuthUser user, AppUserRole role, CloudStudentProfileData studentProfileData)
		{
			string displayName = string.IsNullOrWhiteSpace(studentProfileData?.StudentName)
				? (string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email ?? user.UserId : user.DisplayName)
				: studentProfileData.StudentName;
			string registrationNumber = role == AppUserRole.Teacher ? string.Empty : studentProfileData?.RegistrationNumber ?? string.Empty;
			string teacherName = role == AppUserRole.Teacher ? string.Empty : studentProfileData?.TeacherName ?? string.Empty;
			bool profileCompleted = !CloudSyncPolicy.RequiresStudentProfile(role)
				|| CloudSyncPolicy.HasRequiredStudentMetadata(displayName, registrationNumber, teacherName);
			return new CloudUserProfile(user.UserId, user.Email, displayName, role, true, registrationNumber, teacherName, profileCompleted);
		}

		CloudStudentProfileData ConsumePendingStudentProfileData()
		{
			CloudStudentProfileData data = pendingStudentProfileData;
			pendingStudentProfileData = null;
			return data;
		}

		public static void SignInWithEmailPassword(string email, string password)
		{
			Instance?.SignInWithEmailPasswordAsync(email, password);
		}

		public static AppUserRole GetSuggestedRoleForEmail(string email)
		{
			return Instance == null
				? AppUserRole.Student
				: CloudSyncPolicy.ResolveSuggestedRole(email, Instance.teacherEmailAllowlist);
		}

		public static void SignInWithGoogle()
		{
			OnAuthError?.Invoke("Login com Google ainda não foi implementado nesta fase do projeto.");
		}

		public static void SignOut()
		{
			if (Instance == null)
			{
				return;
			}

			KeepLoggedIn = false;

			if (Instance.signOutInProgress)
			{
				Instance.Log("Sign-out already in progress");
				return;
			}

			Instance.signOutInProgress = true;
			IsSigningOut = true;
			Instance.Log("Starting logout process...");
			SaverCloudExtension.SyncAllProjectsToCloud(() =>
			{
				Instance.Log("Full sync complete. Signing out...");
				Instance.signOutInProgress = false;
				SupabaseAuthClient.SignOut();
				CurrentUserProfile = CloudUserProfile.Offline;
				SavePaths.UseOfflineProfile();
				IsSigningOut = false;
				OnLogout?.Invoke();
			});
		}

		async void SignInWithEmailPasswordAsync(string email, string password)
		{
			try
			{
				Log($"Attempting sign-in with email: {email}");
				AuthUser user = await SupabaseAuthClient.SignInWithPasswordAsync(email, password);
				await FinalizeSignIn(user);
			}
			catch (AuthException ex)
			{
				string friendly = GetFriendlyErrorMessage(ex);
				LogError($"Sign in failed: {friendly} (HTTP {ex.StatusCode})");
				OnAuthError?.Invoke(friendly);
			}
			catch (Exception ex)
			{
				LogError($"Sign in failed: {ex.Message}");
				OnAuthError?.Invoke("Não foi possível conectar ao servidor. Verifique sua internet e tente novamente.");
			}
		}

		public static void CreateAccount(string email, string password, CloudStudentProfileData studentProfileData = null)
		{
			Instance?.CreateAccountAsync(email, password, studentProfileData);
		}

		public static void SendPasswordReset(string email)
		{
			Instance?.SendPasswordResetAsync(email);
		}

		public static void UpdatePassword(string newPassword)
		{
			Instance?.UpdatePasswordAsync(newPassword);
		}

		public static void UpdateStudentProfile(CloudStudentProfileData studentProfileData)
		{
			Instance?.UpdateStudentProfileAsync(studentProfileData);
		}

		async void CreateAccountAsync(string email, string password, CloudStudentProfileData studentProfileData)
		{
			try
			{
				Log($"Creating new account: {email}");
				pendingStudentProfileData = studentProfileData;
				AuthUser user = await SupabaseAuthClient.SignUpAsync(email, password, studentProfileData?.StudentName);
				Log($"Account created: {user.Email}");
				await FinalizeSignIn(user);
			}
			catch (AuthException ex)
			{
				string friendly = GetFriendlyErrorMessage(ex);
				LogError($"Account creation failed: {friendly} (HTTP {ex.StatusCode})");
				pendingStudentProfileData = null;
				OnAuthError?.Invoke(friendly);
			}
			catch (Exception ex)
			{
				LogError($"Account creation failed: {ex.Message}");
				pendingStudentProfileData = null;
				OnAuthError?.Invoke("Não foi possível criar a conta. Tente novamente.");
			}
		}

		async void SendPasswordResetAsync(string email)
		{
			try
			{
				await SupabaseAuthClient.SendPasswordResetAsync(email);
				Log($"Password reset email sent: {email}");
				OnAuthInfo?.Invoke("Email de redefinição enviado. Verifique sua caixa de entrada.");
			}
			catch (Exception ex)
			{
				LogError($"Password reset failed: {ex.Message}");
				OnAuthError?.Invoke("Não foi possível enviar o email de redefinição. Tente novamente.");
			}
		}

		async void UpdatePasswordAsync(string newPassword)
		{
			try
			{
				if (CurrentUser == null)
				{
					OnAuthError?.Invoke("Nenhum usuário autenticado para atualizar a senha.");
					return;
				}
				if (string.IsNullOrWhiteSpace(newPassword))
				{
					OnAuthError?.Invoke("A nova senha não pode ser vazia.");
					return;
				}
				await SupabaseAuthClient.UpdatePasswordAsync(newPassword);
				Log("Password updated successfully.");
				OnAuthInfo?.Invoke("Senha atualizada com sucesso.");
			}
			catch (Exception ex)
			{
				LogError($"Password update failed: {ex.Message}");
				OnAuthError?.Invoke("Não foi possível atualizar a senha. Tente novamente.");
			}
		}

		async void UpdateStudentProfileAsync(CloudStudentProfileData studentProfileData)
		{
			try
			{
				if (CurrentUser == null)
				{
					OnAuthError?.Invoke("Nenhum usuário autenticado para atualizar o perfil.");
					return;
				}

				if (studentProfileData == null || !CloudSyncPolicy.HasRequiredStudentMetadata(studentProfileData.StudentName, studentProfileData.RegistrationNumber, studentProfileData.TeacherName, studentProfileData.TurmaId))
				{
					OnAuthError?.Invoke("Preencha nome, matrícula e professor antes de salvar o perfil.");
					return;
				}

				if (!string.Equals(CurrentUser.DisplayName, studentProfileData.StudentName, StringComparison.Ordinal))
				{
					await SupabaseAuthClient.UpdateDisplayNameAsync(studentProfileData.StudentName);
				}

				FirestoreDataManager.UpdateCurrentStudentProfile(studentProfileData,
					onSuccess: profile =>
					{
						ApplyUserProfile(CurrentUser, profile);
						OnAuthInfo?.Invoke("Perfil atualizado com sucesso.");
					},
					onError: error =>
					{
						LogError($"Failed to update student profile: {error}");
						OnAuthError?.Invoke(error);
					});
			}
			catch (Exception ex)
			{
				LogError($"Profile update failed: {ex.Message}");
				OnAuthError?.Invoke("Não foi possível atualizar o perfil. Tente novamente.");
			}
		}

		public static void SendVerificationEmail()
		{
			// GoTrue: verificação por email depende de SMTP configurado no servidor.
			// Com ENABLE_EMAIL_AUTOCONFIRM=true a conta já entra confirmada.
			Instance?.Log("Verificação de email gerenciada pelo servidor (autoconfirm).");
		}

		string GetFriendlyErrorMessage(AuthException ex)
		{
			string msg = (ex?.Message ?? string.Empty).ToLowerInvariant();
			if (msg.Contains("already registered") || msg.Contains("user already registered"))
				return "Este email já está cadastrado. Faça login.";
			if (msg.Contains("invalid login credentials"))
				return "Email ou senha incorretos.";
			if (msg.Contains("password") && msg.Contains("least"))
				return "Senha muito curta. Use pelo menos 6 caracteres.";
			if (msg.Contains("email") && msg.Contains("invalid"))
				return "Formato de email inválido.";
			if (ex != null && ex.StatusCode >= 500)
				return "Erro no servidor de autenticação. Tente novamente em instantes.";
			return string.IsNullOrWhiteSpace(ex?.Message) ? "Falha na autenticação." : ex.Message;
		}

		void Log(string message)
		{
			if (showDebugLogs)
			{
				Debug.Log($"[Auth] {message}");
			}
		}

		void LogError(string message)
		{
			Debug.LogError($"[Auth] {message}");
		}

		void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}
	}
}
