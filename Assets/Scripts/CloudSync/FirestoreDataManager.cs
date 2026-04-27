using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DLS.Description;
using DLS.SaveSystem;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Gerenciador de dados Firestore - CRUD de perfil, projetos e chips.
	/// Estrutura atual:
	/// users/{userId}
	/// users/{userId}/projects/{projectName}
	/// users/{userId}/projects/{projectName}/chips/{chipName}
	/// </summary>
	public class FirestoreDataManager : MonoBehaviour
	{
		public static FirestoreDataManager Instance { get; private set; }
		public static FirebaseFirestore DB { get; private set; }
		public static bool IsReady => Instance != null && DB != null;

		[Header("Debug")]
		[SerializeField] bool showDebugLogs = true;

		[Header("Settings")]
		[SerializeField] bool enableOfflineCache = true;

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
			FirebaseManager.OnFirebaseReady += InitializeFirestore;

			if (FirebaseManager.IsInitialized)
			{
				InitializeFirestore();
			}
		}

		void InitializeFirestore()
		{
			if (DB != null)
			{
				return;
			}

			DB = FirebaseFirestore.DefaultInstance;

			if (enableOfflineCache)
			{
				DB.Settings.PersistenceEnabled = true;
				Log("Offline persistence enabled");
			}

			Log("Firestore initialized");
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

		public static void UpsertUserProfile(FirebaseUser user, AppUserRole suggestedRole, CloudStudentProfileData studentProfileData = null, Action<CloudUserProfile> onSuccess = null, Action<string> onError = null)
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
			if (Instance == null || DB == null)
			{
				onError?.Invoke("Firestore not ready");
				return false;
			}

			return true;
		}

		async void SaveProjectAsync(ProjectDescription project, Action onSuccess, Action<string> onError)
		{
			try
			{
				await SaveProjectDocumentAsync(project, project.AllCustomChipNames?.Length ?? 0);
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
				await SaveProjectDocumentAsync(project, chips.Count);

				List<Task> chipTasks = new(chips.Count);
				foreach (ChipDescription chip in chips)
				{
					chipTasks.Add(SaveChipDocumentAsync(chip, project.ProjectName));
				}

				if (chipTasks.Count > 0)
				{
					await Task.WhenAll(chipTasks);
				}

				Log($"Project bundle saved: {project.ProjectName} ({chips.Count} chips)");
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
				await SaveChipDocumentAsync(chip, projectName);
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
				QuerySnapshot snapshot = await GetProjectsCollection(FirebaseAuthManager.UserId).GetSnapshotAsync();
				List<ProjectDescription> projects = new(snapshot.Count);

				foreach (DocumentSnapshot doc in snapshot.Documents)
				{
					if (!doc.Exists)
					{
						continue;
					}

					projects.Add(DeserializeProject(doc));
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
				QuerySnapshot snapshot = await GetProjectsCollection(FirebaseAuthManager.UserId).GetSnapshotAsync();
				List<CloudProjectBundle> bundles = new(snapshot.Count);

				foreach (DocumentSnapshot doc in snapshot.Documents)
				{
					if (!doc.Exists)
					{
						continue;
					}

					ProjectDescription project = DeserializeProject(doc);
					List<ChipDescription> chips = await LoadChipsForProjectAsync(doc.Reference);
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
				List<ChipDescription> chips = await LoadChipsForProjectAsync(GetProjectDocument(FirebaseAuthManager.UserId, projectName));
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
				DocumentReference projectDoc = GetProjectDocument(FirebaseAuthManager.UserId, projectName);
				await DeleteCollectionAsync(projectDoc.Collection("chips"));
				await projectDoc.DeleteAsync();

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
				await GetChipDocument(FirebaseAuthManager.UserId, projectName, chipName).DeleteAsync();
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
				string userId = FirebaseAuthManager.UserId;
				QuerySnapshot projectsSnapshot = await GetProjectsCollection(userId).GetSnapshotAsync();

				foreach (DocumentSnapshot projectDoc in projectsSnapshot.Documents)
				{
					await DeleteCollectionAsync(projectDoc.Reference.Collection("chips"));
					await projectDoc.Reference.DeleteAsync();
				}

				await GetUserDocument(userId).DeleteAsync();
				Log("All user data deleted");
				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				LogError($"Failed to delete user data: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		async void UpsertUserProfileAsync(FirebaseUser user, AppUserRole suggestedRole, CloudStudentProfileData studentProfileData, Action<CloudUserProfile> onSuccess, Action<string> onError)
		{
			try
			{
				DocumentReference userDoc = GetUserDocument(user.UserId);
				DocumentSnapshot existingSnapshot = await userDoc.GetSnapshotAsync();

				AppUserRole existingRole = AppUserRole.Student;
				bool approved = true;
				if (existingSnapshot.Exists)
				{
					if (existingSnapshot.TryGetValue("role", out string persistedRole))
					{
						existingRole = CloudSyncPolicy.ParseRole(persistedRole);
					}

					if (existingSnapshot.TryGetValue("isApproved", out bool persistedApproval))
					{
						approved = persistedApproval;
					}
				}

				AppUserRole finalRole = CloudSyncPolicy.PreferExistingRole(existingRole, suggestedRole);
				string existingDisplayName = GetPersistedString(existingSnapshot, "displayName", "studentName");
				string existingRegistrationNumber = GetPersistedString(existingSnapshot, "registrationNumber", "matricula");
				string existingTeacherName = GetPersistedString(existingSnapshot, "teacherName", "teacher");
				string displayName = ResolveDisplayName(user, studentProfileData, existingDisplayName);
				string registrationNumber = CloudSyncPolicy.RequiresStudentProfile(finalRole)
					? ResolveRegistrationNumber(studentProfileData, existingRegistrationNumber)
					: string.Empty;
				string teacherName = CloudSyncPolicy.RequiresStudentProfile(finalRole)
					? ResolveTeacherName(studentProfileData, existingTeacherName)
					: string.Empty;
				bool profileCompleted = !CloudSyncPolicy.RequiresStudentProfile(finalRole)
					|| CloudSyncPolicy.HasRequiredStudentMetadata(displayName, registrationNumber, teacherName);

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
					{ "profileCompleted", profileCompleted },
					{ "role", CloudSyncPolicy.ToPersistedRole(finalRole) },
					{ "isTeacher", finalRole == AppUserRole.Teacher },
					{ "isApproved", approved },
					{ "lastLoginAt", FieldValue.ServerTimestamp }
				};

				if (!existingSnapshot.Exists)
				{
					data.Add("createdAt", FieldValue.ServerTimestamp);
				}

				await userDoc.SetAsync(data, SetOptions.MergeAll);

				CloudUserProfile profile = new(user.UserId, user.Email, displayName, finalRole, approved, registrationNumber, teacherName, profileCompleted);
				Log($"User profile synced: {profile.DisplayName} ({profile.RoleLabel})");
				onSuccess?.Invoke(profile);
			}
			catch (Exception ex)
			{
				LogError($"Failed to sync user profile: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		async Task SaveProjectDocumentAsync(ProjectDescription project, int customChipCount)
		{
			DocumentReference docRef = GetProjectDocument(FirebaseAuthManager.UserId, project.ProjectName);
			string projectJson = Serializer.SerializeProjectDescription(project);

			Dictionary<string, object> data = new()
			{
				{ "projectName", project.ProjectName },
				{ "projectLookupKey", CloudSyncPolicy.CreateLookupKey(project.ProjectName) },
				{ "projectData", projectJson },
				{ "customChipCount", customChipCount },
				{ "lastModified", FieldValue.ServerTimestamp }
			};

			await docRef.SetAsync(data);
		}

		async Task SaveChipDocumentAsync(ChipDescription chip, string projectName)
		{
			DocumentReference docRef = GetChipDocument(FirebaseAuthManager.UserId, projectName, chip.Name);
			string chipJson = Serializer.SerializeChipDescription(chip);

			Dictionary<string, object> data = new()
			{
				{ "chipName", chip.Name },
				{ "chipLookupKey", CloudSyncPolicy.CreateLookupKey(chip.Name) },
				{ "projectLookupKey", CloudSyncPolicy.CreateLookupKey(projectName) },
				{ "chipData", chipJson },
				{ "lastModified", FieldValue.ServerTimestamp }
			};

			await docRef.SetAsync(data);
		}

		async Task<List<ChipDescription>> LoadChipsForProjectAsync(DocumentReference projectDocument)
		{
			QuerySnapshot snapshot = await projectDocument.Collection("chips").GetSnapshotAsync();
			List<ChipDescription> chips = new(snapshot.Count);

			foreach (DocumentSnapshot doc in snapshot.Documents)
			{
				if (!doc.Exists || !doc.TryGetValue("chipData", out string chipJson))
				{
					continue;
				}

				chips.Add(Serializer.DeserializeChipDescription(chipJson));
			}

			return chips.OrderBy(chip => chip.Name, ChipDescription.NameComparer).ToList();
		}

		async Task DeleteCollectionAsync(CollectionReference collection)
		{
			QuerySnapshot snapshot = await collection.GetSnapshotAsync();
			if (snapshot.Count == 0)
			{
				return;
			}

			WriteBatch batch = DB.StartBatch();
			int operationsInBatch = 0;

			foreach (DocumentSnapshot doc in snapshot.Documents)
			{
				batch.Delete(doc.Reference);
				operationsInBatch++;

				if (operationsInBatch >= 450)
				{
					await batch.CommitAsync();
					batch = DB.StartBatch();
					operationsInBatch = 0;
				}
			}

			if (operationsInBatch > 0)
			{
				await batch.CommitAsync();
			}
		}

		static string GetPersistedString(DocumentSnapshot snapshot, params string[] fieldNames)
		{
			if (snapshot == null || !snapshot.Exists || fieldNames == null)
			{
				return string.Empty;
			}

			foreach (string fieldName in fieldNames)
			{
				if (!string.IsNullOrWhiteSpace(fieldName) && snapshot.TryGetValue(fieldName, out string value) && !string.IsNullOrWhiteSpace(value))
				{
					return value.Trim();
				}
			}

			return string.Empty;
		}

		static string ResolveDisplayName(FirebaseUser user, CloudStudentProfileData studentProfileData, string existingDisplayName)
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

		static CollectionReference GetUsersCollection() => DB.Collection("users");
		static DocumentReference GetUserDocument(string userId) => GetUsersCollection().Document(userId);
		static CollectionReference GetProjectsCollection(string userId) => GetUserDocument(userId).Collection("projects");
		static DocumentReference GetProjectDocument(string userId, string projectName) => GetProjectsCollection(userId).Document(projectName);
		static DocumentReference GetChipDocument(string userId, string projectName, string chipName) => GetProjectDocument(userId, projectName).Collection("chips").Document(chipName);

		static ProjectDescription DeserializeProject(DocumentSnapshot doc)
		{
			string projectJson = doc.GetValue<string>("projectData");
			ProjectDescription project = Serializer.DeserializeProjectDescription(projectJson);

			if (string.IsNullOrWhiteSpace(project.ProjectName) && doc.TryGetValue("projectName", out string projectName))
			{
				project.ProjectName = projectName;
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

			FirebaseManager.OnFirebaseReady -= InitializeFirestore;
		}
	}
}
