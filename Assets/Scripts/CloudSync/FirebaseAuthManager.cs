using System;
using System.Collections;
using DLS.SaveSystem;
using Firebase.Auth;
using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Gerenciador de autenticação Firebase.
	/// Nesta fase o login operacional é email/senha, com perfil salvo no Firestore.
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
		public static CloudUserProfile CurrentUserProfile { get; private set; } = CloudUserProfile.Offline;
		public static bool IsTeacher => CurrentUserProfile.IsTeacher;
		public static bool RequiresStudentProfileCompletion => IsLoggedIn && CurrentUserProfile.RequiresStudentProfileCompletion;
		public static string CurrentUserRoleLabel => CurrentUserProfile.RoleLabel;

		public static event Action<FirebaseUser> OnLoginSuccess;
		public static event Action<CloudUserProfile> OnUserProfileReady;
		public static event Action OnLogout;
		public static event Action<string> OnAuthInfo;
		public static event Action<string> OnAuthError;

		[Header("Debug")]
		[SerializeField] bool showDebugLogs = true;

		[Header("Session Settings")]
		[SerializeField] bool persistSession = true;

		[Header("Role Bootstrap")]
		[SerializeField] string[] teacherEmailAllowlist = Array.Empty<string>();

		string lastProcessedUserId = string.Empty;
		Coroutine signInBootstrapCoroutine;
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
			Auth.StateChanged -= OnAuthStateChanged;
			Auth.StateChanged += OnAuthStateChanged;

			Log($"Auth initialized. Persist session: {persistSession}");
			HandleCurrentAuthState();
		}

		void OnAuthStateChanged(object sender, EventArgs args)
		{
			HandleCurrentAuthState();
		}

		void HandleCurrentAuthState()
		{
			string currentUserId = CurrentUser?.UserId ?? string.Empty;
			if (string.Equals(currentUserId, lastProcessedUserId, StringComparison.Ordinal))
			{
				return;
			}

			lastProcessedUserId = currentUserId;

			if (signInBootstrapCoroutine != null)
			{
				StopCoroutine(signInBootstrapCoroutine);
				signInBootstrapCoroutine = null;
			}

			if (CurrentUser != null)
			{
				Log($"User signed in: {CurrentUser.DisplayName ?? CurrentUser.Email}");
				signInBootstrapCoroutine = StartCoroutine(BootstrapSignedInUser(CurrentUser));
			}
			else
			{
				signOutInProgress = false;
				pendingStudentProfileData = null;
				CurrentUserProfile = CloudUserProfile.Offline;
				SavePaths.UseOfflineProfile();
				Log("User signed out");
				OnLogout?.Invoke();
			}
		}

		IEnumerator BootstrapSignedInUser(FirebaseUser user)
		{
			const float timeoutSeconds = 5f;
			float elapsed = 0f;

			while (!FirestoreDataManager.IsReady && elapsed < timeoutSeconds)
			{
				if (CurrentUser == null || CurrentUser.UserId != user.UserId)
				{
					yield break;
				}

				elapsed += Time.unscaledDeltaTime;
				yield return null;
			}

			signInBootstrapCoroutine = null;

			if (CurrentUser == null || CurrentUser.UserId != user.UserId)
			{
				yield break;
			}

			FinalizeSignIn(user);
		}

		void FinalizeSignIn(FirebaseUser user)
		{
			AppUserRole suggestedRole = CloudSyncPolicy.ResolveSuggestedRole(user.Email, teacherEmailAllowlist);
			CloudStudentProfileData studentProfileData = ConsumePendingStudentProfileData();

			if (!FirestoreDataManager.IsReady)
			{
				LogError("Firestore not ready during sign-in bootstrap. Continuing with fallback profile.");
				CompleteSignIn(user, CreateFallbackProfile(user, suggestedRole, studentProfileData));
				return;
			}

			FirestoreDataManager.UpsertUserProfile(user, suggestedRole, studentProfileData,
				onSuccess: profile => CompleteSignIn(user, profile),
				onError: error =>
				{
					LogError($"Failed to sync user profile: {error}");
					CompleteSignIn(user, CreateFallbackProfile(user, suggestedRole, studentProfileData));
				}
			);
		}

		void CompleteSignIn(FirebaseUser user, CloudUserProfile profile)
		{
			ApplyUserProfile(user, profile);

			SaverCloudExtension.LoadAllProjectsFromCloud(loadedCount =>
			{
				if (loadedCount > 0)
				{
					Log($"Loaded {loadedCount} projects from cloud");
				}
			});

			OnLoginSuccess?.Invoke(user);
		}

		void ApplyUserProfile(FirebaseUser user, CloudUserProfile profile)
		{
			CurrentUserProfile = profile;
			SavePaths.UseCloudProfile(user.UserId);
			Log($"Save profile switched to: {SavePaths.ActiveProfileDataPath}");
			OnUserProfileReady?.Invoke(profile);
		}

		CloudUserProfile CreateFallbackProfile(FirebaseUser user, AppUserRole role, CloudStudentProfileData studentProfileData)
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
			Instance?.Log("Google Sign-In nao implementado nesta fase. Use email/senha para testes.");
			OnAuthError?.Invoke("Google Sign-In ainda nao foi implementado nesta fase do projeto.");
		}

		public static void SignOut()
		{
			if (Auth == null || Instance == null)
			{
				return;
			}

			if (Instance.signOutInProgress)
			{
				Instance.Log("Sign-out already in progress");
				return;
			}

			Instance.signOutInProgress = true;
			Instance.Log("Starting logout process...");
			SaverCloudExtension.SyncAllProjectsToCloud(() =>
			{
				Instance.Log("Full sync complete. Signing out...");
				Instance.signOutInProgress = false;
				Auth.SignOut();
			});
		}

		async void SignInWithEmailPasswordAsync(string email, string password)
		{
			try
			{
				Log($"Attempting sign-in with email: {email}");

				if (Auth == null)
				{
					OnAuthError?.Invoke("Authentication system not ready. Please wait and try again.");
					return;
				}

				await Auth.SignInWithEmailAndPasswordAsync(email, password);
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

		public static void CreateAccount(string email, string password, CloudStudentProfileData studentProfileData = null)
		{
			Instance?.CreateAccountAsync(email, password, studentProfileData);
		}

		public static void SendPasswordReset(string email)
		{
			Instance?.SendPasswordResetAsync(email);
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

				if (Auth == null)
				{
					OnAuthError?.Invoke("Authentication system not ready. Please wait and try again.");
					pendingStudentProfileData = null;
					return;
				}

				AuthResult result = await Auth.CreateUserWithEmailAndPasswordAsync(email, password);
				FirebaseUser user = result.User;

				if (!string.IsNullOrWhiteSpace(studentProfileData?.StudentName))
				{
					UserProfile profile = new() { DisplayName = studentProfileData.StudentName };
					await user.UpdateUserProfileAsync(profile);
					await user.ReloadAsync();
				}

				Log($"Account created: {user.Email}");
			}
			catch (Firebase.FirebaseException fbEx)
			{
				string friendlyError = GetFriendlyErrorMessage(fbEx);
				LogError($"Account creation failed: {friendlyError} (Code: {fbEx.ErrorCode})");
				pendingStudentProfileData = null;
				OnAuthError?.Invoke(friendlyError);
			}
			catch (Exception ex)
			{
				LogError($"Account creation failed: {ex.Message}\nStack: {ex.StackTrace}");
				pendingStudentProfileData = null;
				OnAuthError?.Invoke("Failed to create account. Please try again.");
			}
		}

		async void SendPasswordResetAsync(string email)
		{
			try
			{
				if (Auth == null)
				{
					OnAuthError?.Invoke("Authentication system not ready. Please wait and try again.");
					return;
				}

				await Auth.SendPasswordResetEmailAsync(email);
				Log($"Password reset email sent: {email}");
				OnAuthInfo?.Invoke("Password reset email sent. Check your inbox.");
			}
			catch (Firebase.FirebaseException fbEx)
			{
				string friendlyError = GetFriendlyErrorMessage(fbEx);
				LogError($"Password reset failed: {friendlyError} (Code: {fbEx.ErrorCode})");
				OnAuthError?.Invoke(friendlyError);
			}
			catch (Exception ex)
			{
				LogError($"Password reset failed: {ex.Message}\nStack: {ex.StackTrace}");
				OnAuthError?.Invoke("Failed to send password reset email. Please try again.");
			}
		}

		async void UpdateStudentProfileAsync(CloudStudentProfileData studentProfileData)
		{
			try
			{
				if (Auth == null || CurrentUser == null)
				{
					OnAuthError?.Invoke("No authenticated user available to update the profile.");
					return;
				}

				if (studentProfileData == null || !CloudSyncPolicy.HasRequiredStudentMetadata(studentProfileData.StudentName, studentProfileData.RegistrationNumber, studentProfileData.TeacherName))
				{
					OnAuthError?.Invoke("Fill in name, matrícula and professor before saving the profile.");
					return;
				}

				if (!string.Equals(CurrentUser.DisplayName, studentProfileData.StudentName, StringComparison.Ordinal))
				{
					UserProfile authProfile = new() { DisplayName = studentProfileData.StudentName };
					await CurrentUser.UpdateUserProfileAsync(authProfile);
					await CurrentUser.ReloadAsync();
				}

				AppUserRole suggestedRole = GetSuggestedRoleForEmail(CurrentUser.Email);
				FirestoreDataManager.UpdateCurrentStudentProfile(studentProfileData,
					onSuccess: profile =>
					{
						ApplyUserProfile(CurrentUser, profile);
						OnAuthInfo?.Invoke("Profile updated successfully.");
					},
					onError: error =>
					{
						LogError($"Failed to update student profile: {error}");
						OnAuthError?.Invoke(error);
					});
			}
			catch (Firebase.FirebaseException fbEx)
			{
				string friendlyError = GetFriendlyErrorMessage(fbEx);
				LogError($"Profile update failed: {friendlyError} (Code: {fbEx.ErrorCode})");
				OnAuthError?.Invoke(friendlyError);
			}
			catch (Exception ex)
			{
				LogError($"Profile update failed: {ex.Message}\nStack: {ex.StackTrace}");
				OnAuthError?.Invoke("Failed to update profile. Please try again.");
			}
		}

		public static void SendVerificationEmail()
		{
			if (CurrentUser != null && !CurrentUser.IsEmailVerified)
			{
				CurrentUser.SendEmailVerificationAsync();
				Instance?.Log("Verification email sent");
			}
		}

		string GetFriendlyErrorMessage(Firebase.FirebaseException ex)
		{
			string rawMessage = ex?.Message ?? string.Empty;
			string normalizedMessage = rawMessage.ToLowerInvariant();

			if (normalizedMessage.Contains("operation_not_allowed")
				|| normalizedMessage.Contains("this operation is not allowed")
				|| normalizedMessage.Contains("enable this service in the console"))
			{
				return "Email/Password auth is disabled in Firebase. Open Authentication > Sign-in method > Email/Password and click Enable.";
			}

			switch (ex.ErrorCode)
			{
				case 17007:
					return "This email is already registered. Please sign in instead.";
				case 17008:
					return "Invalid email address format.";
				case 17009:
					return "Password is too weak. Use at least 6 characters.";
				case 17011:
					return "No account found with this email. Please create an account first.";
				case 17012:
					return "Incorrect password. Please try again.";
				case 17020:
					return "Network error. Please check your internet connection.";
				case 17999:
					return "Firebase internal error. Make sure Email/Password auth is enabled in Firebase Console.";
				default:
					return $"{ex.Message} (Code: {ex.ErrorCode})";
			}
		}

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
