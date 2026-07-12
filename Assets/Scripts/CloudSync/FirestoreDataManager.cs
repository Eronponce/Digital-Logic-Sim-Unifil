using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DLS.Description;
using DLS.SaveSystem;
using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Gerenciador de dados na nuvem - CRUD de perfil, projetos e chips.
	/// Historicamente falava com o Firestore; hoje fala com o mirror server via
	/// MirrorApiClient (Supabase Auth continua sendo a fonte do token).
	/// A API pública estática (callbacks onSuccess/onError) foi preservada, então
	/// os callers (menus, SaverCloudExtension) não mudam.
	/// Estrutura de dados no servidor (herdada do Firestore):
	/// users/{userId}
	/// users/{userId}/projects/{projectName}
	/// users/{userId}/projects/{projectName}/chips/{chipName}
	/// </summary>
	public class FirestoreDataManager : MonoBehaviour
	{
		public static FirestoreDataManager Instance { get; private set; }
		public static bool IsReady => Instance != null;

		[Header("Debug")]
		[SerializeField] bool showDebugLogs = true;

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
			// Pré-aquece a descoberta do endpoint
			_ = MirrorConfigProvider.GetBaseUrlAsync();
		}

		public static void SaveProject(ProjectDescription project, Action onSuccess = null, Action<string> onError = null)
		{
			if (!EnsureAuthenticated(onError))
			{
				return;
			}

			Instance.SaveProjectAsync(project, onSuccess, onError);
		}

		public static void SaveProjectBundle(ProjectDescription project, IReadOnlyList<ChipDescription> chips, Action onSuccess = null, Action<string> onError = null)
		{
			if (!EnsureAuthenticated(onError))
			{
				return;
			}

			Instance.SaveProjectBundleAsync(project, chips ?? Array.Empty<ChipDescription>(), onSuccess, onError);
		}

		public static void SaveChip(ChipDescription chip, string projectName, Action onSuccess = null, Action<string> onError = null)
		{
			if (!EnsureAuthenticated(onError))
			{
				return;
			}

			Instance.SaveChipAsync(chip, projectName, onSuccess, onError);
		}

		public static void LoadAllProjects(Action<List<ProjectDescription>> onSuccess, Action<string> onError = null)
		{
			if (!EnsureAuthenticated(onError))
			{
				return;
			}

			Instance.LoadAllProjectsAsync(onSuccess, onError);
		}

		public static void LoadAllProjectBundles(Action<List<CloudProjectBundle>> onSuccess, Action<string> onError = null)
		{
			if (!EnsureAuthenticated(onError))
			{
				return;
			}

			Instance.LoadAllProjectBundlesAsync(onSuccess, onError);
		}

		public static void LoadChips(string projectName, Action<List<ChipDescription>> onSuccess, Action<string> onError = null)
		{
			if (!EnsureAuthenticated(onError))
			{
				return;
			}

			Instance.LoadChipsAsync(projectName, onSuccess, onError);
		}

		public static void LoadTurmas(Action<List<TurmaData>> onSuccess, Action<string> onError = null)
		{
			if (!IsReady)
			{
				onError?.Invoke("Servidor não está pronto");
				return;
			}
			Instance.LoadTurmasAsync(onSuccess, onError);
		}

		public static void DeleteProject(string projectName, Action onSuccess = null, Action<string> onError = null)
		{
			if (!EnsureAuthenticated(onError))
			{
				return;
			}

			Instance.DeleteProjectAsync(projectName, onSuccess, onError);
		}

		public static void DeleteChip(string chipName, string projectName, Action onSuccess = null, Action<string> onError = null)
		{
			if (!EnsureAuthenticated(onError))
			{
				return;
			}

			Instance.DeleteChipAsync(chipName, projectName, onSuccess, onError);
		}

		public static void DeleteAllUserData(Action onSuccess = null, Action<string> onError = null)
		{
			if (!EnsureAuthenticated(onError))
			{
				return;
			}

			Instance.DeleteAllUserDataAsync(onSuccess, onError);
		}

		public static void UpsertUserProfile(AuthUser user, AppUserRole suggestedRole, CloudStudentProfileData studentProfileData = null, Action<CloudUserProfile> onSuccess = null, Action<string> onError = null)
		{
			if (user == null)
			{
				onError?.Invoke("User not available");
				return;
			}

			if (!EnsureReady(onError))
			{
				return;
			}

			Instance.UpsertUserProfileAsync(user, suggestedRole, studentProfileData, onSuccess, onError);
		}

		public static void UpdateCurrentStudentProfile(CloudStudentProfileData studentProfileData, Action<CloudUserProfile> onSuccess = null, Action<string> onError = null)
		{
			if (!EnsureAuthenticated(onError))
			{
				return;
			}

			if (studentProfileData == null)
			{
				onError?.Invoke("Student profile data not provided");
				return;
			}

			AppUserRole currentRole = FirebaseAuthManager.CurrentUserProfile?.Role ?? AppUserRole.Student;
			Instance.UpsertUserProfileAsync(FirebaseAuthManager.CurrentUser, currentRole, studentProfileData, onSuccess, onError);
		}

		static bool EnsureAuthenticated(Action<string> onError)
		{
			if (!EnsureReady(onError))
			{
				return false;
			}

			if (!FirebaseAuthManager.IsLoggedIn)
			{
				onError?.Invoke("User not logged in");
				return false;
			}

			return true;
		}

		static bool EnsureReady(Action<string> onError)
		{
			if (!IsReady)
			{
				onError?.Invoke("Servidor não está pronto");
				return false;
			}

			return true;
		}

		async void SaveProjectAsync(ProjectDescription project, Action onSuccess, Action<string> onError)
		{
			try
			{
				await MirrorApiClient.SaveProjectAsync(
					FirebaseAuthManager.UserId,
					project.ProjectName,
					project.ProjectName,
					Serializer.SerializeProjectDescription(project));
				Log($"Project saved: {project.ProjectName}");
				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				LogError($"Failed to save project: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		async void SaveProjectBundleAsync(ProjectDescription project, IReadOnlyList<ChipDescription> chips, Action onSuccess, Action<string> onError)
		{
			try
			{
				// Escrita atômica: o servidor grava projeto + chips numa transação Postgres
				// (o servidor grava numa transação Postgres, sem o limite de 450 ops do Firestore).
				var chipPayloads = chips.Select(chip => (object)BuildChipPayload(chip)).ToList();
				await MirrorApiClient.SaveBundleAsync(
					FirebaseAuthManager.UserId,
					project.ProjectName,
					BuildProjectPayload(project),
					chipPayloads);

				Log($"Project bundle saved (atomic): {project.ProjectName} ({chips.Count} chips)");
				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				LogError($"Failed to save project bundle: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		async void SaveChipAsync(ChipDescription chip, string projectName, Action onSuccess, Action<string> onError)
		{
			try
			{
				await MirrorApiClient.SaveChipAsync(
					FirebaseAuthManager.UserId,
					projectName,
					chip.Name,
					CloudSyncPolicy.CreateLookupKey(chip.Name),
					Serializer.SerializeChipDescription(chip));
				Log($"Chip saved: {chip.Name} (in {projectName})");
				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				LogError($"Failed to save chip: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		async void LoadAllProjectsAsync(Action<List<ProjectDescription>> onSuccess, Action<string> onError)
		{
			try
			{
				List<MirrorApiClient.ProjectItem> items = await MirrorApiClient.LoadAllProjectsAsync(FirebaseAuthManager.UserId);
				List<ProjectDescription> projects = new(items.Count);

				foreach (MirrorApiClient.ProjectItem item in items)
				{
					if (string.IsNullOrEmpty(item.ProjectData))
					{
						continue;
					}

					projects.Add(DeserializeProject(item.ProjectData, item.ProjectName ?? item.Id));
				}

				projects.Sort((a, b) => b.LastSaveTime.CompareTo(a.LastSaveTime));
				Log($"Loaded {projects.Count} project descriptions");
				onSuccess?.Invoke(projects);
			}
			catch (Exception ex)
			{
				LogError($"Failed to load projects: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		async void LoadAllProjectBundlesAsync(Action<List<CloudProjectBundle>> onSuccess, Action<string> onError)
		{
			try
			{
				List<MirrorApiClient.BundleItem> items = await MirrorApiClient.LoadAllBundlesAsync(FirebaseAuthManager.UserId);
				List<CloudProjectBundle> bundles = new(items.Count);

				foreach (MirrorApiClient.BundleItem item in items)
				{
					if (string.IsNullOrEmpty(item.ProjectData))
					{
						continue;
					}

					ProjectDescription project = DeserializeProject(item.ProjectData, item.ProjectName ?? item.Id);
					List<ChipDescription> chips = DeserializeChips(item.Chips);
					bundles.Add(new CloudProjectBundle(project, chips));
				}

				bundles.Sort((a, b) => b.ProjectDescription.LastSaveTime.CompareTo(a.ProjectDescription.LastSaveTime));
				Log($"Loaded {bundles.Count} project bundles");
				onSuccess?.Invoke(bundles);
			}
			catch (Exception ex)
			{
				LogError($"Failed to load project bundles: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		async void LoadChipsAsync(string projectName, Action<List<ChipDescription>> onSuccess, Action<string> onError)
		{
			try
			{
				List<MirrorApiClient.ChipItem> items = await MirrorApiClient.LoadChipsAsync(FirebaseAuthManager.UserId, projectName);
				List<ChipDescription> chips = DeserializeChips(items);
				Log($"Loaded {chips.Count} chips from '{projectName}'");
				onSuccess?.Invoke(chips);
			}
			catch (Exception ex)
			{
				LogError($"Failed to load chips: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		async void DeleteProjectAsync(string projectName, Action onSuccess, Action<string> onError)
		{
			try
			{
				// cascade de chips acontece no servidor, na mesma transação
				await MirrorApiClient.DeleteProjectAsync(FirebaseAuthManager.UserId, projectName);
				Log($"Project deleted: {projectName}");
				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				LogError($"Failed to delete project: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		async void DeleteChipAsync(string chipName, string projectName, Action onSuccess, Action<string> onError)
		{
			try
			{
				await MirrorApiClient.DeleteChipAsync(FirebaseAuthManager.UserId, projectName, chipName);
				Log($"Chip deleted: {chipName} (from {projectName})");
				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				LogError($"Failed to delete chip: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		async void DeleteAllUserDataAsync(Action onSuccess, Action<string> onError)
		{
			try
			{
				await MirrorApiClient.DeleteAllUserDataAsync(FirebaseAuthManager.UserId);
				Log("All user data deleted");
				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				LogError($"Failed to delete user data: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		async void LoadTurmasAsync(Action<List<TurmaData>> onSuccess, Action<string> onError)
		{
			try
			{
				// Guard: o endpoint de turmas exige usuário autenticado (idToken)
				if (!FirebaseAuthManager.IsLoggedIn)
				{
					onError?.Invoke("Not signed in");
					return;
				}

				List<MirrorApiClient.TurmaItem> items = await MirrorApiClient.LoadTurmasAsync();
				List<TurmaData> turmas = new(items.Count);
				foreach (MirrorApiClient.TurmaItem item in items)
				{
					turmas.Add(new TurmaData
					{
						Id = item.Id,
						TeacherName = item.TeacherName ?? string.Empty,
						ProjectName = item.ProjectName ?? string.Empty,
						DisplayName = item.DisplayName ?? string.Empty,
						Active = true
					});
				}
				onSuccess?.Invoke(turmas);
			}
			catch (Exception ex)
			{
				LogError($"Failed to load turmas: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		async void UpsertUserProfileAsync(AuthUser user, AppUserRole suggestedRole, CloudStudentProfileData studentProfileData, Action<CloudUserProfile> onSuccess, Action<string> onError)
		{
			try
			{
				// Lê o perfil existente para preservar papel/aprovação e preencher
				// campos ausentes — mesma lógica do SetOptions.MergeAll de antes.
				Dictionary<string, object> existing = await MirrorApiClient.GetUserProfileAsync(user.UserId);
				bool exists = existing != null;

				AppUserRole existingRole = AppUserRole.Student;
				bool approved = true;
				if (exists)
				{
					if (TryGetString(existing, "role", out string persistedRole))
					{
						existingRole = CloudSyncPolicy.ParseRole(persistedRole);
					}

					if (existing.TryGetValue("isApproved", out object persistedApproval) && persistedApproval is bool approvedBool)
					{
						approved = approvedBool;
					}
				}

				AppUserRole finalRole = CloudSyncPolicy.PreferExistingRole(existingRole, suggestedRole);
				string existingDisplayName = GetPersistedString(existing, "displayName", "studentName");
				string existingRegistrationNumber = GetPersistedString(existing, "registrationNumber", "matricula");
				string existingTeacherName = GetPersistedString(existing, "teacherName", "teacher");
				string existingTurmaId = GetPersistedString(existing, "turmaId");
				string displayName = ResolveDisplayName(user, studentProfileData, existingDisplayName);
				string registrationNumber = CloudSyncPolicy.RequiresStudentProfile(finalRole)
					? ResolveRegistrationNumber(studentProfileData, existingRegistrationNumber)
					: string.Empty;
				string teacherName = CloudSyncPolicy.RequiresStudentProfile(finalRole)
					? ResolveTeacherName(studentProfileData, existingTeacherName)
					: string.Empty;
				string turmaId = CloudSyncPolicy.RequiresStudentProfile(finalRole)
					? (string.IsNullOrWhiteSpace(studentProfileData?.TurmaId) ? existingTurmaId : studentProfileData.TurmaId)
					: string.Empty;
				string turmaProjectName = CloudSyncPolicy.RequiresStudentProfile(finalRole)
					? (studentProfileData?.TurmaProjectName ?? string.Empty)
					: string.Empty;
				bool profileCompleted = !CloudSyncPolicy.RequiresStudentProfile(finalRole)
					|| CloudSyncPolicy.HasRequiredStudentMetadata(displayName, registrationNumber, teacherName, turmaId);

				long nowMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				Dictionary<string, object> data = new()
				{
					{ "uid", user.UserId },
					{ "email", user.Email ?? string.Empty },
					{ "displayName", displayName },
					{ "studentName", displayName },
					{ "registrationNumber", registrationNumber },
					{ "matricula", registrationNumber },
					{ "teacherName", teacherName },
					{ "teacher", teacherName },
					{ "teacherLookupKey", CloudSyncPolicy.CreateLookupKey(teacherName) },
					{ "turmaId", turmaId },
					{ "turmaProjectName", turmaProjectName },
					{ "profileCompleted", profileCompleted },
					{ "role", CloudSyncPolicy.ToPersistedRole(finalRole) },
					{ "isTeacher", finalRole == AppUserRole.Teacher },
					{ "isApproved", approved },
					{ "lastLoginAt", nowMillis }
				};

				if (!exists)
				{
					data.Add("createdAt", nowMillis);
				}

				await MirrorApiClient.UpsertUserProfileAsync(user.UserId, data);

				CloudUserProfile profile = new(user.UserId, user.Email, displayName, finalRole, approved, registrationNumber, teacherName, profileCompleted, turmaId);
				Log($"User profile synced: {profile.DisplayName} ({profile.RoleLabel})");
				onSuccess?.Invoke(profile);
			}
			catch (Exception ex)
			{
				LogError($"Failed to sync user profile: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		static Dictionary<string, object> BuildProjectPayload(ProjectDescription project)
		{
			return new Dictionary<string, object>
			{
				{ "projectName", project.ProjectName },
				{ "projectData", Serializer.SerializeProjectDescription(project) },
				{ "lastModified", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
			};
		}

		static Dictionary<string, object> BuildChipPayload(ChipDescription chip)
		{
			return new Dictionary<string, object>
			{
				{ "chipId", chip.Name },
				{ "chipName", chip.Name },
				{ "chipLookupKey", CloudSyncPolicy.CreateLookupKey(chip.Name) },
				{ "chipData", Serializer.SerializeChipDescription(chip) },
				{ "lastModified", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
			};
		}

		static List<ChipDescription> DeserializeChips(List<MirrorApiClient.ChipItem> items)
		{
			List<ChipDescription> chips = new(items?.Count ?? 0);
			if (items == null)
			{
				return chips;
			}

			foreach (MirrorApiClient.ChipItem item in items)
			{
				if (string.IsNullOrEmpty(item.ChipData))
				{
					continue;
				}

				chips.Add(Serializer.DeserializeChipDescription(item.ChipData));
			}

			return chips.OrderBy(chip => chip.Name, ChipDescription.NameComparer).ToList();
		}

		static bool TryGetString(Dictionary<string, object> data, string key, out string value)
		{
			value = string.Empty;
			if (data != null && data.TryGetValue(key, out object raw) && raw is string s && !string.IsNullOrWhiteSpace(s))
			{
				value = s;
				return true;
			}

			return false;
		}

		static string GetPersistedString(Dictionary<string, object> data, params string[] fieldNames)
		{
			if (data == null || fieldNames == null)
			{
				return string.Empty;
			}

			foreach (string fieldName in fieldNames)
			{
				if (!string.IsNullOrWhiteSpace(fieldName) && TryGetString(data, fieldName, out string value))
				{
					return value.Trim();
				}
			}

			return string.Empty;
		}

		static string ResolveDisplayName(AuthUser user, CloudStudentProfileData studentProfileData, string existingDisplayName)
		{
			if (!string.IsNullOrWhiteSpace(studentProfileData?.StudentName))
			{
				return studentProfileData.StudentName;
			}

			if (!string.IsNullOrWhiteSpace(existingDisplayName))
			{
				return existingDisplayName.Trim();
			}

			if (!string.IsNullOrWhiteSpace(user.DisplayName))
			{
				return user.DisplayName.Trim();
			}

			return user.Email ?? user.UserId;
		}

		static string ResolveRegistrationNumber(CloudStudentProfileData studentProfileData, string existingRegistrationNumber)
		{
			if (!string.IsNullOrWhiteSpace(studentProfileData?.RegistrationNumber))
			{
				return studentProfileData.RegistrationNumber;
			}

			return existingRegistrationNumber?.Trim() ?? string.Empty;
		}

		static string ResolveTeacherName(CloudStudentProfileData studentProfileData, string existingTeacherName)
		{
			if (!string.IsNullOrWhiteSpace(studentProfileData?.TeacherName))
			{
				return studentProfileData.TeacherName;
			}

			return CloudSyncPolicy.NormalizeTeacherNameOrEmpty(existingTeacherName);
		}

		static ProjectDescription DeserializeProject(string projectJson, string fallbackName)
		{
			ProjectDescription project = Serializer.DeserializeProjectDescription(projectJson);

			if (string.IsNullOrWhiteSpace(project.ProjectName) && !string.IsNullOrWhiteSpace(fallbackName))
			{
				project.ProjectName = fallbackName;
			}

			return project;
		}

		void Log(string message)
		{
			if (showDebugLogs)
			{
				Debug.Log($"[FirestoreData] {message}");
			}
		}

		void LogError(string message)
		{
			Debug.LogError($"[FirestoreData] {message}");
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
