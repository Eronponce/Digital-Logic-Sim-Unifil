using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using DLS.SaveSystem;

namespace DLS.CloudSync
{
	/// <summary>
	/// Gerenciador de autenticação Firebase - Login/Logout com Google
	/// </summary>
	public class FirebaseAuthManager : MonoBehaviour
	{
		public static FirebaseAuthManager Instance { get; private set; }

		public static FirebaseAuth Auth { get; private set; }
		public static FirebaseUser CurrentUser => Auth?.CurrentUser;
		public static bool IsLoggedIn => CurrentUser != null;

		public static string UserId => CurrentUser?.UserId;
		public static string UserEmail => CurrentUser?.Email;
		public static string UserDisplayName => CurrentUser?.DisplayName;

		// Events
		public static event Action<FirebaseUser> OnLoginSuccess;
		public static event Action OnLogout;
		public static event Action<string> OnAuthError;

		[Header("Debug")]
		[SerializeField] bool showDebugLogs = true;

		[Header("Session Settings")]
		[SerializeField] bool persistSession = true; // Manter logado entre sessões

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

		void Start()
		{
			FirebaseManager.OnFirebaseReady += InitializeAuth;

			if (FirebaseManager.IsInitialized)
			{
				InitializeAuth();
			}
		}

		void InitializeAuth()
		{
			Auth = FirebaseAuth.DefaultInstance;
			Auth.StateChanged += OnAuthStateChanged;

			Log("Auth initialized. Checking for existing session...");

			// Verifica se já existe uma sessão ativa
			if (CurrentUser != null)
			{
				Log($"Session found: {UserDisplayName} ({UserEmail})");
				OnLoginSuccess?.Invoke(CurrentUser);
			}
			else
			{
				Log("No active session found.");
			}
		}

		void OnAuthStateChanged(object sender, EventArgs args)
		{
			if (CurrentUser != null)
			{
				Log($"✅ User signed in: {UserDisplayName} ({UserEmail})");

				// Carrega projetos do Firebase quando fizer login
				SaverCloudExtension.LoadAllProjectsFromCloud((loadedCount) =>
				{
					if (loadedCount > 0)
					{
						Log($"Loaded {loadedCount} projects from cloud");
					}
				});

				OnLoginSuccess?.Invoke(CurrentUser);
			}
			else
			{
				Log("User signed out");
				OnLogout?.Invoke();
			}
		}

		// ============================================
		// MÉTODOS PÚBLICOS - LOGIN/LOGOUT
		// ============================================

		/// <summary>
		/// Login com Email e Senha (para testes)
		/// </summary>
		public static void SignInWithEmailPassword(string email, string password)
		{
			Instance.SignInWithEmailPasswordAsync(email, password);
		}

		/// <summary>
		/// Login com Google (via browser externo)
		/// NOTA: Para desktop, requer configuração adicional de OAuth
		/// </summary>
		public static void SignInWithGoogle()
		{
			Instance.Log("Google Sign-In não implementado ainda. Use email/senha para testes.");
			OnAuthError?.Invoke("Google Sign-In requer configuração adicional para desktop.");
		}

		/// <summary>
		/// Logout - Sincroniza todos os projetos antes de deslogar
		/// </summary>
		public static void SignOut()
		{
			if (Auth != null)
			{
				Instance.Log("Starting logout process...");

				// Sincroniza TODOS os projetos antes de deslogar
				SaverCloudExtension.SyncAllProjectsToCloud(() =>
				{
					Instance.Log("All projects synced. Signing out...");
					Auth.SignOut();
					Instance.Log("User signed out");
				});
			}
		}

		// ============================================
		// MÉTODOS INTERNOS (ASYNC)
		// ============================================

		async void SignInWithEmailPasswordAsync(string email, string password)
		{
			try
			{
				Log($"Attempting to sign in with email: {email}");

				if (Auth == null)
				{
					LogError("Firebase Auth is not initialized!");
					OnAuthError?.Invoke("Authentication system not ready. Please wait and try again.");
					return;
				}

				AuthResult result = await Auth.SignInWithEmailAndPasswordAsync(email, password);
				FirebaseUser user = result.User;

				Log($"✅ Sign in successful: {user.DisplayName ?? user.Email}");
			}
			catch (Firebase.FirebaseException fbEx)
			{
				string friendlyError = GetFriendlyErrorMessage(fbEx);
				LogError($"Sign in failed: {friendlyError} (Code: {fbEx.ErrorCode})");
				OnAuthError?.Invoke(friendlyError);
			}
			catch (Exception ex)
			{
				LogError($"Sign in failed: {ex.Message}\nStack: {ex.StackTrace}");
				OnAuthError?.Invoke("Sign in failed. Please check your credentials and try again.");
			}
		}

		/// <summary>
		/// Criar nova conta com email e senha
		/// </summary>
		public static void CreateAccount(string email, string password, string displayName = null)
		{
			Instance.CreateAccountAsync(email, password, displayName);
		}

		async void CreateAccountAsync(string email, string password, string displayName)
		{
			try
			{
				Log($"Creating new account: {email}");

				if (Auth == null)
				{
					LogError("Firebase Auth is not initialized!");
					OnAuthError?.Invoke("Authentication system not ready. Please wait and try again.");
					return;
				}

				AuthResult result = await Auth.CreateUserWithEmailAndPasswordAsync(email, password);
				FirebaseUser user = result.User;

				// Atualiza display name se fornecido
				if (!string.IsNullOrEmpty(displayName))
				{
					UserProfile profile = new UserProfile { DisplayName = displayName };
					await user.UpdateUserProfileAsync(profile);
					Log($"Display name updated to: {displayName}");
				}

				Log($"✅ Account created: {user.Email}");
			}
			catch (Firebase.FirebaseException fbEx)
			{
				string friendlyError = GetFriendlyErrorMessage(fbEx);
				LogError($"Account creation failed: {friendlyError} (Code: {fbEx.ErrorCode})");
				OnAuthError?.Invoke(friendlyError);
			}
			catch (Exception ex)
			{
				LogError($"Account creation failed: {ex.Message}\nStack: {ex.StackTrace}");
				OnAuthError?.Invoke("Failed to create account. Please try again.");
			}
		}

		/// <summary>
		/// Envia email de verificação
		/// </summary>
		public static void SendVerificationEmail()
		{
			if (CurrentUser != null && !CurrentUser.IsEmailVerified)
			{
				CurrentUser.SendEmailVerificationAsync();
				Instance.Log("Verification email sent");
			}
		}

		// ============================================
		// ERROR HANDLING
		// ============================================

		string GetFriendlyErrorMessage(Firebase.FirebaseException ex)
		{
			switch (ex.ErrorCode)
			{
				case 17007: // ERROR_EMAIL_ALREADY_IN_USE
					return "This email is already registered. Please sign in instead.";
				case 17008: // ERROR_INVALID_EMAIL
					return "Invalid email address format.";
				case 17009: // ERROR_WEAK_PASSWORD
					return "Password is too weak. Use at least 6 characters.";
				case 17011: // ERROR_USER_NOT_FOUND
					return "No account found with this email. Please create an account first.";
				case 17012: // ERROR_WRONG_PASSWORD
					return "Incorrect password. Please try again.";
				case 17020: // ERROR_NETWORK_REQUEST_FAILED
					return "Network error. Please check your internet connection.";
				case 17999: // ERROR_INTERNAL_ERROR
					return "Firebase internal error. Make sure Email/Password auth is enabled in Firebase Console.";
				default:
					return $"{ex.Message} (Code: {ex.ErrorCode})";
			}
		}

		// ============================================
		// DEBUG
		// ============================================

		void Log(string message)
		{
			if (showDebugLogs)
			{
				Debug.Log($"[FirebaseAuth] {message}");
			}
		}

		void LogError(string message)
		{
			Debug.LogError($"[FirebaseAuth] {message}");
		}

		void OnDestroy()
		{
			if (Instance == this)
			{
				if (Auth != null)
				{
					Auth.StateChanged -= OnAuthStateChanged;
				}
				Instance = null;
			}

			FirebaseManager.OnFirebaseReady -= InitializeAuth;
		}
	}
}
