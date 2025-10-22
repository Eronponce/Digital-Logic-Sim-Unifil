using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;
using DLS.Description;
using DLS.SaveSystem;

namespace DLS.CloudSync
{
	/// <summary>
	/// Gerenciador de dados Firestore - CRUD de Projects e Chips
	/// Estrutura: users/{userId}/projects/{projectName}/chips/{chipName}
	/// </summary>
	public class FirestoreDataManager : MonoBehaviour
	{
		public static FirestoreDataManager Instance { get; private set; }

		public static FirebaseFirestore DB { get; private set; }

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
			DB = FirebaseFirestore.DefaultInstance;

			// Ativar cache offline
			if (enableOfflineCache)
			{
				DB.Settings.PersistenceEnabled = true;
				Log("Offline persistence enabled");
			}

			Log("Firestore initialized");
		}

		// ============================================
		// SALVAR PROJETO
		// ============================================

		public static void SaveProject(ProjectDescription project, Action onSuccess = null, Action<string> onError = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				onError?.Invoke("User not logged in");
				return;
			}

			Instance.SaveProjectAsync(project, onSuccess, onError);
		}

		async void SaveProjectAsync(ProjectDescription project, Action onSuccess, Action<string> onError)
		{
			try
			{
				string userId = FirebaseAuthManager.UserId;
				DocumentReference docRef = DB.Collection("users")
					.Document(userId)
					.Collection("projects")
					.Document(project.ProjectName);

				// Serializa para JSON
				string projectJson = Serializer.SerializeProjectDescription(project);

				Dictionary<string, object> data = new Dictionary<string, object>
				{
					{ "projectName", project.ProjectName },
					{ "projectData", projectJson },
					{ "lastModified", FieldValue.ServerTimestamp }
				};

				await docRef.SetAsync(data);

				Log($"✅ Project saved: {project.ProjectName}");
				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				LogError($"Failed to save project: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		// ============================================
		// SALVAR CHIP
		// ============================================

		public static void SaveChip(ChipDescription chip, string projectName, Action onSuccess = null, Action<string> onError = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				onError?.Invoke("User not logged in");
				return;
			}

			Instance.SaveChipAsync(chip, projectName, onSuccess, onError);
		}

		async void SaveChipAsync(ChipDescription chip, string projectName, Action onSuccess, Action<string> onError)
		{
			try
			{
				string userId = FirebaseAuthManager.UserId;
				DocumentReference docRef = DB.Collection("users")
					.Document(userId)
					.Collection("projects")
					.Document(projectName)
					.Collection("chips")
					.Document(chip.Name);

				string chipJson = Serializer.SerializeChipDescription(chip);

				Dictionary<string, object> data = new Dictionary<string, object>
				{
					{ "chipName", chip.Name },
					{ "chipData", chipJson },
					{ "lastModified", FieldValue.ServerTimestamp }
				};

				await docRef.SetAsync(data);

				Log($"✅ Chip saved: {chip.Name} (in {projectName})");
				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				LogError($"Failed to save chip: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		// ============================================
		// CARREGAR TODOS OS PROJETOS
		// ============================================

		public static void LoadAllProjects(Action<List<ProjectDescription>> onSuccess, Action<string> onError = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				onError?.Invoke("User not logged in");
				return;
			}

			Instance.LoadAllProjectsAsync(onSuccess, onError);
		}

		async void LoadAllProjectsAsync(Action<List<ProjectDescription>> onSuccess, Action<string> onError)
		{
			try
			{
				string userId = FirebaseAuthManager.UserId;
				CollectionReference colRef = DB.Collection("users")
					.Document(userId)
					.Collection("projects");

				QuerySnapshot snapshot = await colRef.GetSnapshotAsync();

				List<ProjectDescription> projects = new List<ProjectDescription>();

				foreach (DocumentSnapshot doc in snapshot.Documents)
				{
					if (doc.Exists)
					{
						string projectJson = doc.GetValue<string>("projectData");
						ProjectDescription project = Serializer.DeserializeProjectDescription(projectJson);
						projects.Add(project);
					}
				}

				Log($"✅ Loaded {projects.Count} projects");
				onSuccess?.Invoke(projects);
			}
			catch (Exception ex)
			{
				LogError($"Failed to load projects: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		// ============================================
		// CARREGAR CHIPS DE UM PROJETO
		// ============================================

		public static void LoadChips(string projectName, Action<List<ChipDescription>> onSuccess, Action<string> onError = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				onError?.Invoke("User not logged in");
				return;
			}

			Instance.LoadChipsAsync(projectName, onSuccess, onError);
		}

		async void LoadChipsAsync(string projectName, Action<List<ChipDescription>> onSuccess, Action<string> onError)
		{
			try
			{
				string userId = FirebaseAuthManager.UserId;
				CollectionReference colRef = DB.Collection("users")
					.Document(userId)
					.Collection("projects")
					.Document(projectName)
					.Collection("chips");

				QuerySnapshot snapshot = await colRef.GetSnapshotAsync();

				List<ChipDescription> chips = new List<ChipDescription>();

				foreach (DocumentSnapshot doc in snapshot.Documents)
				{
					if (doc.Exists)
					{
						string chipJson = doc.GetValue<string>("chipData");
						ChipDescription chip = Serializer.DeserializeChipDescription(chipJson);
						chips.Add(chip);
					}
				}

				Log($"✅ Loaded {chips.Count} chips from '{projectName}'");
				onSuccess?.Invoke(chips);
			}
			catch (Exception ex)
			{
				LogError($"Failed to load chips: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		// ============================================
		// DELETAR PROJETO
		// ============================================

		public static void DeleteProject(string projectName, Action onSuccess = null, Action<string> onError = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				onError?.Invoke("User not logged in");
				return;
			}

			Instance.DeleteProjectAsync(projectName, onSuccess, onError);
		}

		async void DeleteProjectAsync(string projectName, Action onSuccess, Action<string> onError)
		{
			try
			{
				string userId = FirebaseAuthManager.UserId;
				DocumentReference docRef = DB.Collection("users")
					.Document(userId)
					.Collection("projects")
					.Document(projectName);

				// NOTA: No Firestore, deletar documento não deleta subcoleções automaticamente
				// Precisaria de Cloud Function para deletar chips também
				// Por enquanto, apenas deleta o documento do projeto
				await docRef.DeleteAsync();

				Log($"✅ Project deleted: {projectName}");
				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				LogError($"Failed to delete project: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		// ============================================
		// DELETAR CHIP
		// ============================================

		public static void DeleteChip(string chipName, string projectName, Action onSuccess = null, Action<string> onError = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				onError?.Invoke("User not logged in");
				return;
			}

			Instance.DeleteChipAsync(chipName, projectName, onSuccess, onError);
		}

		async void DeleteChipAsync(string chipName, string projectName, Action onSuccess, Action<string> onError)
		{
			try
			{
				string userId = FirebaseAuthManager.UserId;
				DocumentReference docRef = DB.Collection("users")
					.Document(userId)
					.Collection("projects")
					.Document(projectName)
					.Collection("chips")
					.Document(chipName);

				await docRef.DeleteAsync();

				Log($"✅ Chip deleted: {chipName} (from {projectName})");
				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				LogError($"Failed to delete chip: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		// ============================================
		// DELETAR TODOS OS DADOS DO USUÁRIO (RESET ANUAL)
		// ============================================

		public static void DeleteAllUserData(Action onSuccess = null, Action<string> onError = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				onError?.Invoke("User not logged in");
				return;
			}

			Instance.DeleteAllUserDataAsync(onSuccess, onError);
		}

		async void DeleteAllUserDataAsync(Action onSuccess, Action<string> onError)
		{
			try
			{
				string userId = FirebaseAuthManager.UserId;
				DocumentReference userDoc = DB.Collection("users").Document(userId);

				// NOTA: Isso só deleta o documento do usuário
				// Subcoleções (projects) não são deletadas automaticamente
				// Para deletar tudo, seria necessária uma Cloud Function
				await userDoc.DeleteAsync();

				Log($"⚠️ User data deleted (subcollections may still exist)");
				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				LogError($"Failed to delete user data: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		// ============================================
		// DEBUG
		// ============================================

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
