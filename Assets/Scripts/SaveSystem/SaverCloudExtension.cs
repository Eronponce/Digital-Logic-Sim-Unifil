using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLS.CloudSync;
using DLS.Description;
using UnityEngine;

namespace DLS.SaveSystem
{
	/// <summary>
	/// Extensão do Saver para sincronização com cloud.
	/// </summary>
	public static class SaverCloudExtension
	{
		public static void SyncProjectToCloud(ProjectDescription project)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				return;
			}

			FirestoreDataManager.SaveProject(project,
				onSuccess: () => Debug.Log($"[Cloud] Project '{project.ProjectName}' synced"),
				onError: error => Debug.LogWarning($"[Cloud] Failed to sync project: {error}")
			);
		}

		public static void SyncChipToCloud(ChipDescription chip, string projectName)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				return;
			}

			FirestoreDataManager.SaveChip(chip, projectName,
				onSuccess: () => Debug.Log($"[Cloud] Chip '{chip.Name}' synced"),
				onError: error => Debug.LogWarning($"[Cloud] Failed to sync chip: {error}")
			);
		}

		public static void DeleteProjectFromCloud(string projectName)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				return;
			}

			FirestoreDataManager.DeleteProject(projectName,
				onSuccess: () => Debug.Log($"[Cloud] Project '{projectName}' deleted from cloud"),
				onError: error => Debug.LogWarning($"[Cloud] Failed to delete project: {error}")
			);
		}

		public static void DeleteChipFromCloud(string chipName, string projectName)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				return;
			}

			FirestoreDataManager.DeleteChip(chipName, projectName,
				onSuccess: () => Debug.Log($"[Cloud] Chip '{chipName}' deleted from cloud"),
				onError: error => Debug.LogWarning($"[Cloud] Failed to delete chip: {error}")
			);
		}

		public static void SyncAllProjectsToCloud(Action onComplete = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				Debug.LogWarning("[Cloud] Cannot sync: user not logged in");
				onComplete?.Invoke();
				return;
			}

			ProjectDescription[] localProjects = Loader.LoadAllProjectDescriptions();
			if (localProjects.Length == 0)
			{
				Debug.Log("[Cloud] No projects to sync");
				onComplete?.Invoke();
				return;
			}

			Debug.Log($"[Cloud] Starting full sync of {localProjects.Length} projects...");

			int processedCount = 0;
			int totalToSync = localProjects.Length;

			foreach (ProjectDescription localProject in localProjects)
			{
				try
				{
					ChipDescription[] localChips = Loader.LoadAvailableChipDescriptions(localProject, out string[] missingChipNames);
					if (missingChipNames.Length > 0)
					{
						Debug.LogWarning($"[Cloud] Project '{localProject.ProjectName}' references {missingChipNames.Length} missing chips. Syncing available chips only: {string.Join(", ", missingChipNames)}");
					}

					ProjectDescription projectToSync = SyncProjectChipIndex(localProject, localChips);

					FirestoreDataManager.SaveProjectBundle(projectToSync, localChips,
						onSuccess: () => NotifySyncCompleted(projectToSync.ProjectName, null),
						onError: error => NotifySyncCompleted(projectToSync.ProjectName, error)
					);
				}
				catch (Exception ex)
				{
					NotifySyncCompleted(localProject.ProjectName, ex.Message);
				}
			}

			void NotifySyncCompleted(string projectName, string error)
			{
				processedCount++;

				if (string.IsNullOrWhiteSpace(error))
				{
					Debug.Log($"[Cloud] Synced {processedCount}/{totalToSync}: {projectName}");
				}
				else
				{
					Debug.LogError($"[Cloud] Failed to sync '{projectName}': {error}");
				}

				if (processedCount >= totalToSync)
				{
					Debug.Log($"[Cloud] Full sync finished ({totalToSync} projects processed)");
					onComplete?.Invoke();
				}
			}
		}

		public static void LoadAllProjectsFromCloud(Action<int> onComplete = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				Debug.LogWarning("[Cloud] Cannot load: user not logged in");
				onComplete?.Invoke(0);
				return;
			}

			Debug.Log("[Cloud] Loading project bundles from cloud...");

			FirestoreDataManager.LoadAllProjectBundles(
				onSuccess: bundles =>
				{
					Debug.Log($"[Cloud] Found {bundles.Count} project bundles in cloud");

					if (bundles.Count == 0)
					{
						onComplete?.Invoke(0);
						return;
					}

					int loadedCount = 0;
					int skippedCount = 0;

					foreach (CloudProjectBundle bundle in bundles)
					{
						try
						{
							ProjectDescription cloudProject = SyncProjectChipIndex(bundle.ProjectDescription, bundle.Chips);
							bool shouldRestore = !Loader.ProjectExists(cloudProject.ProjectName);

							if (!shouldRestore)
							{
								ProjectDescription localProject = Loader.LoadProjectDescription(cloudProject.ProjectName);
								bool localChipDataComplete = Loader.ProjectHasCompleteLocalChipData(localProject);
								shouldRestore = CloudSyncPolicy.ShouldRestoreCloudProject(localProject, cloudProject, localChipDataComplete);
							}

							if (shouldRestore)
							{
								RestoreProjectBundleLocally(cloudProject, bundle.Chips);
								loadedCount++;
								Debug.Log($"[Cloud] Restored '{cloudProject.ProjectName}' with {bundle.Chips.Count} chips");
							}
							else
							{
								skippedCount++;
								Debug.Log($"[Cloud] Skipped '{cloudProject.ProjectName}' (local version is up to date)");
							}
						}
						catch (Exception ex)
						{
							Debug.LogError($"[Cloud] Failed to restore project bundle: {ex.Message}");
						}
					}

					Debug.Log($"[Cloud] Project restore finished: {loadedCount} loaded, {skippedCount} skipped");
					onComplete?.Invoke(loadedCount);
				},
				onError: error =>
				{
					Debug.LogError($"[Cloud] Failed to load projects: {error}");
					onComplete?.Invoke(0);
				}
			);
		}

		static ProjectDescription SyncProjectChipIndex(ProjectDescription project, IReadOnlyList<ChipDescription> chips)
		{
			string[] actualChipNames = chips?
				.Select(chip => chip.Name)
				.Distinct(ChipDescription.NameComparer)
				.ToArray()
				?? Array.Empty<string>();

			string[] currentChipNames = project.AllCustomChipNames ?? Array.Empty<string>();
			bool alreadyInSync = currentChipNames.Length == actualChipNames.Length
				&& !currentChipNames.Except(actualChipNames, ChipDescription.NameComparer).Any();

			if (!alreadyInSync)
			{
				project.AllCustomChipNames = actualChipNames;
			}

			return project;
		}

		static void RestoreProjectBundleLocally(ProjectDescription project, IReadOnlyList<ChipDescription> chips)
		{
			string chipsPath = SavePaths.GetChipsPath(project.ProjectName);
			if (Directory.Exists(chipsPath))
			{
				Directory.Delete(chipsPath, true);
			}

			Saver.SaveProjectDescription(project, syncToCloud: false, updateSaveMetadata: false);

			foreach (ChipDescription chip in chips)
			{
				Saver.SaveChip(chip, project.ProjectName, syncToCloud: false);
			}
		}
	}
}
