using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.CloudSync;
using UnityEngine;

namespace DLS.SaveSystem
{
	/// <summary>
	/// Extensão do Saver para sincronização com cloud
	/// </summary>
	public static class SaverCloudExtension
	{
		/// <summary>
		/// Sincroniza projeto com Firebase (se logado)
		/// </summary>
		public static void SyncProjectToCloud(ProjectDescription project)
		{
			if (!FirebaseAuthManager.IsLoggedIn) return;

			FirestoreDataManager.SaveProject(project,
				onSuccess: () => Debug.Log($"[Cloud] Project '{project.ProjectName}' synced"),
				onError: (error) => Debug.LogWarning($"[Cloud] Failed to sync project: {error}")
			);
		}

		/// <summary>
		/// Sincroniza chip com Firebase (se logado)
		/// </summary>
		public static void SyncChipToCloud(ChipDescription chip, string projectName)
		{
			if (!FirebaseAuthManager.IsLoggedIn) return;

			FirestoreDataManager.SaveChip(chip, projectName,
				onSuccess: () => Debug.Log($"[Cloud] Chip '{chip.Name}' synced"),
				onError: (error) => Debug.LogWarning($"[Cloud] Failed to sync chip: {error}")
			);
		}

		/// <summary>
		/// Deleta projeto do Firebase (se logado)
		/// </summary>
		public static void DeleteProjectFromCloud(string projectName)
		{
			if (!FirebaseAuthManager.IsLoggedIn) return;

			FirestoreDataManager.DeleteProject(projectName,
				onSuccess: () => Debug.Log($"[Cloud] Project '{projectName}' deleted from cloud"),
				onError: (error) => Debug.LogWarning($"[Cloud] Failed to delete project: {error}")
			);
		}

		/// <summary>
		/// Deleta chip do Firebase (se logado)
		/// </summary>
		public static void DeleteChipFromCloud(string chipName, string projectName)
		{
			if (!FirebaseAuthManager.IsLoggedIn) return;

			FirestoreDataManager.DeleteChip(chipName, projectName,
				onSuccess: () => Debug.Log($"[Cloud] Chip '{chipName}' deleted from cloud"),
				onError: (error) => Debug.LogWarning($"[Cloud] Failed to delete chip: {error}")
			);
		}

		/// <summary>
		/// Sincroniza TODOS os projetos locais para o Firebase
		/// Usado principalmente no logout para garantir que tudo foi salvo
		/// </summary>
		public static void SyncAllProjectsToCloud(Action onComplete = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				Debug.LogWarning("[Cloud] Cannot sync: user not logged in");
				onComplete?.Invoke();
				return;
			}

			// Carrega todos os projetos locais
			ProjectDescription[] localProjects = Loader.LoadAllProjectDescriptions();

			if (localProjects.Length == 0)
			{
				Debug.Log("[Cloud] No projects to sync");
				onComplete?.Invoke();
				return;
			}

			Debug.Log($"[Cloud] Starting sync of {localProjects.Length} projects...");

			int syncedCount = 0;
			int totalToSync = localProjects.Length;

			foreach (ProjectDescription project in localProjects)
			{
				FirestoreDataManager.SaveProject(project,
					onSuccess: () =>
					{
						syncedCount++;
						Debug.Log($"[Cloud] Synced {syncedCount}/{totalToSync}: {project.ProjectName}");

						// Quando todos forem sincronizados
						if (syncedCount >= totalToSync)
						{
							Debug.Log($"[Cloud] ✅ All {totalToSync} projects synced successfully!");
							onComplete?.Invoke();
						}
					},
					onError: (error) =>
					{
						syncedCount++;
						Debug.LogError($"[Cloud] Failed to sync '{project.ProjectName}': {error}");

						// Continua mesmo com erro
						if (syncedCount >= totalToSync)
						{
							Debug.LogWarning($"[Cloud] Sync completed with errors ({totalToSync} projects processed)");
							onComplete?.Invoke();
						}
					}
				);
			}
		}

		/// <summary>
		/// Carrega TODOS os projetos do Firebase e salva localmente
		/// Usado no login para restaurar projetos do usuário
		/// </summary>
		public static void LoadAllProjectsFromCloud(Action<int> onComplete = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				Debug.LogWarning("[Cloud] Cannot load: user not logged in");
				onComplete?.Invoke(0);
				return;
			}

			Debug.Log("[Cloud] Loading projects from cloud...");

			FirestoreDataManager.LoadAllProjects(
				onSuccess: (cloudProjects) =>
				{
					Debug.Log($"[Cloud] Found {cloudProjects.Count} projects in cloud");

					if (cloudProjects.Count == 0)
					{
						Debug.Log("[Cloud] No projects to download");
						onComplete?.Invoke(0);
						return;
					}

					int loadedCount = 0;
					int skippedCount = 0;

					foreach (ProjectDescription cloudProject in cloudProjects)
					{
						try
						{
							// Verifica se já existe localmente (não sobrescreve)
							if (Loader.ProjectExists(cloudProject.ProjectName))
							{
								// Compara datas - só sobrescreve se cloud for mais recente
								ProjectDescription localProject = Loader.LoadProjectDescription(cloudProject.ProjectName);

								if (cloudProject.LastSaveTime > localProject.LastSaveTime)
								{
									Debug.Log($"[Cloud] Updating '{cloudProject.ProjectName}' (cloud version is newer)");
									Saver.SaveProjectDescription(cloudProject);
									loadedCount++;
								}
								else
								{
									Debug.Log($"[Cloud] Skipping '{cloudProject.ProjectName}' (local version is newer or equal)");
									skippedCount++;
								}
							}
							else
							{
								// Projeto não existe localmente - baixa do cloud
								Debug.Log($"[Cloud] Downloading new project: '{cloudProject.ProjectName}'");
								Saver.SaveProjectDescription(cloudProject);
								loadedCount++;
							}
						}
						catch (Exception ex)
						{
							Debug.LogError($"[Cloud] Failed to save project '{cloudProject.ProjectName}': {ex.Message}");
						}
					}

					Debug.Log($"[Cloud] ✅ Projects loaded: {loadedCount} new/updated, {skippedCount} skipped");
					onComplete?.Invoke(loadedCount);
				},
				onError: (error) =>
				{
					Debug.LogError($"[Cloud] Failed to load projects: {error}");
					onComplete?.Invoke(0);
				}
			);
		}
	}
}
