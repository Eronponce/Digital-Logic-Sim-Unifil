using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Gerenciador principal do Firebase - Inicializa e verifica dependências
	/// </summary>
	public class FirebaseManager : MonoBehaviour
	{
		public static FirebaseManager Instance { get; private set; }

		public static bool IsInitialized { get; private set; }
		public static FirebaseApp App { get; private set; }

		public static event Action OnFirebaseReady;
		public static event Action<string> OnFirebaseError;

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

			InitializeFirebase();
		}

		void InitializeFirebase()
		{
#if DLS_COMMUNITY
			Log("Community build: Firebase disabled. Running offline.");
			return;
#else
			Log("Initializing Firebase...");

			FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
			{
				DependencyStatus dependencyStatus = task.Result;

				if (dependencyStatus == DependencyStatus.Available)
				{
					App = FirebaseApp.DefaultInstance;
					IsInitialized = true;

					Log("✅ Firebase initialized successfully!");
					OnFirebaseReady?.Invoke();
				}
				else
				{
					string error = $"Could not resolve Firebase dependencies: {dependencyStatus}";
					LogError(error);
					OnFirebaseError?.Invoke(error);
				}
			});
#endif
		}

		void Log(string message)
		{
			if (showDebugLogs)
			{
				Debug.Log($"[FirebaseManager] {message}");
			}
		}

		void LogError(string message)
		{
			Debug.LogError($"[FirebaseManager] {message}");
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
