using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLS.CloudSync;
using DLS.Description;
using DLS.Game;
using UnityEngine;

namespace DLS.SaveSystem
{
	/// <summary>
	/// Extensão do Saver para sincronização com cloud.
	/// </summary>
	public static class SaverCloudExtension
	{
		const int MaxStatusItems = 50;

		class BundleValidationIssue
		{
			public string ChipName { get; }
			public string[] MissingDependencies { get; }
			public Dictionary<string, int> MissingDependencyCounts { get; }

			public BundleValidationIssue(string chipName, IEnumerable<string> missingDependencies)
			{
				ChipName = chipName;

				MissingDependencyCounts = missingDependencies
					.Where(name => !string.IsNullOrWhiteSpace(name))
					.GroupBy(name => name, ChipDescription.NameComparer)
					.ToDictionary(group => group.Key, group => group.Count(), ChipDescription.NameComparer);

				MissingDependencies = MissingDependencyCounts.Keys
					.OrderBy(name => name, ChipDescription.NameComparer)
					.ToArray();
			}
		}

		class BundleValidationResult
		{
			public ChipDescription[] Chips { get; }
			public string[] MissingDeclaredChips { get; }
			public string[] MissingLibraryReferences { get; }
			public BundleValidationIssue[] DependencyIssues { get; }
			public bool IsValid => MissingDeclaredChips.Length == 0 && MissingLibraryReferences.Length == 0 && DependencyIssues.Length == 0;

			public BundleValidationResult(ChipDescription[] chips, string[] missingDeclaredChips, string[] missingLibraryReferences, BundleValidationIssue[] dependencyIssues)
			{
				Chips = chips;
				MissingDeclaredChips = missingDeclaredChips;
				MissingLibraryReferences = missingLibraryReferences;
				DependencyIssues = dependencyIssues;
			}

			public string CreateErrorMessage()
			{
				List<string> lines = new() { "Projeto desincronizado" };

				string[] chipsToRecreate = MissingDeclaredChips
					.Concat(MissingLibraryReferences)
					.Concat(DependencyIssues.SelectMany(issue => issue.MissingDependencies))
					.Where(name => !string.IsNullOrWhiteSpace(name))
					.Distinct(ChipDescription.NameComparer)
					.OrderBy(name => name, ChipDescription.NameComparer)
					.ToArray();

				if (chipsToRecreate.Length > 0)
				{
					lines.Add($"Refaca/salve este(s) circuito(s): {FormatList(chipsToRecreate, MaxStatusItems)}");
				}

				if (MissingDeclaredChips.Length > 0)
				{
					lines.Add($"Arquivo local faltando: {FormatList(MissingDeclaredChips, MaxStatusItems)}");
				}

				if (MissingLibraryReferences.Length > 0)
				{
					lines.Add($"Aparece no menu, mas nao existe: {FormatList(MissingLibraryReferences, MaxStatusItems)}");
				}

				if (DependencyIssues.Length > 0)
				{
					string[] missingDependencyNames = DependencyIssues
						.SelectMany(issue => issue.MissingDependencies)
						.Distinct(ChipDescription.NameComparer)
						.OrderBy(name => name, ChipDescription.NameComparer)
						.ToArray();

					foreach (string missingDependencyName in missingDependencyNames.Take(MaxStatusItems))
					{
						string[] affectedCircuits = DependencyIssues
							.Where(issue => issue.MissingDependencyCounts.ContainsKey(missingDependencyName))
							.Select(issue => issue.ChipName)
							.Distinct(ChipDescription.NameComparer)
							.OrderBy(name => name, ChipDescription.NameComparer)
							.ToArray();

						int missingCount = DependencyIssues.Sum(issue =>
							issue.MissingDependencyCounts.TryGetValue(missingDependencyName, out int count) ? count : 0
						);

						lines.Add($"{missingDependencyName} esta faltando {missingCount} vez(es). Refaca todos os circuitos que usam {missingDependencyName}: {FormatList(affectedCircuits, MaxStatusItems)}");
					}

					if (missingDependencyNames.Length > MaxStatusItems)
					{
						lines.Add($"+{missingDependencyNames.Length - MaxStatusItems} circuito(s) faltando a mais");
					}
				}

				return string.Join("\n", lines);
			}
		}

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

		public static void SyncProjectBundleToCloud(ProjectDescription project, Action onSuccess = null, Action<string> onError = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				Debug.LogWarning("[Cloud] Cannot sync project bundle: user not logged in");
				onError?.Invoke("Usuario nao esta logado");
				return;
			}

			try
			{
				BundleValidationResult validation = ValidateProjectBundle(project);
				if (!validation.IsValid)
				{
					string message = validation.CreateErrorMessage();
					Debug.LogWarning($"[Cloud] Project bundle '{project.ProjectName}' blocked: {message}");
					onError?.Invoke(message);
					return;
				}

				ProjectDescription projectToSync = SyncProjectChipIndex(project, validation.Chips);
				Debug.Log($"[Cloud] Starting project bundle sync: {projectToSync.ProjectName} ({validation.Chips.Length} chips)");

				FirestoreDataManager.SaveProjectBundle(projectToSync, validation.Chips,
					onSuccess: () =>
					{
						Debug.Log($"[Cloud] Project bundle '{projectToSync.ProjectName}' synced");
						onSuccess?.Invoke();
					},
					onError: error =>
					{
						Debug.LogWarning($"[Cloud] Failed to sync project bundle '{projectToSync.ProjectName}': {error}");
						onError?.Invoke(error);
					}
				);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[Cloud] Failed to prepare project bundle sync: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
		}

		public static void SyncProjectBundleToCloud(ProjectDescription project, IReadOnlyList<ChipDescription> currentSessionChips, Action onSuccess = null, Action<string> onError = null)
		{
			if (!FirebaseAuthManager.IsLoggedIn)
			{
				Debug.LogWarning("[Cloud] Cannot sync project bundle: user not logged in");
				onError?.Invoke("Usuario nao esta logado");
				return;
			}

			try
			{
				BundleValidationResult validation = ValidateProjectBundle(project, currentSessionChips ?? Array.Empty<ChipDescription>(), Array.Empty<string>());
				if (!validation.IsValid)
				{
					string message = validation.CreateErrorMessage();
					Debug.LogWarning($"[Cloud] Project bundle '{project.ProjectName}' blocked: {message}");
					onError?.Invoke(message);
					return;
				}

				ProjectDescription projectToSync = SyncProjectChipIndex(project, validation.Chips);
				Debug.Log($"[Cloud] Starting current-session project bundle sync: {projectToSync.ProjectName} ({validation.Chips.Length} chips)");

				FirestoreDataManager.SaveProjectBundle(projectToSync, validation.Chips,
					onSuccess: () =>
					{
						Debug.Log($"[Cloud] Project bundle '{projectToSync.ProjectName}' synced");
						onSuccess?.Invoke();
					},
					onError: error =>
					{
						Debug.LogWarning($"[Cloud] Failed to sync project bundle '{projectToSync.ProjectName}': {error}");
						onError?.Invoke(error);
					}
				);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[Cloud] Failed to prepare project bundle sync: {ex.Message}");
				onError?.Invoke(ex.Message);
			}
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
					BundleValidationResult validation = ValidateProjectBundle(localProject);
					if (!validation.IsValid)
					{
						NotifySyncCompleted(localProject.ProjectName, validation.CreateErrorMessage());
						continue;
					}

					ProjectDescription projectToSync = SyncProjectChipIndex(localProject, validation.Chips);

					FirestoreDataManager.SaveProjectBundle(projectToSync, validation.Chips,
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

		static BundleValidationResult ValidateProjectBundle(ProjectDescription project)
		{
			ChipDescription[] localChips = Loader.LoadAvailableChipDescriptions(project, out string[] missingChipNames);
			return ValidateProjectBundle(project, localChips, missingChipNames);
		}

		static BundleValidationResult ValidateProjectBundle(ProjectDescription project, IReadOnlyList<ChipDescription> sourceChips, string[] missingChipNames)
		{
			ChipDescription[] localChips = sourceChips?
				.Where(chip => chip != null)
				.GroupBy(chip => chip.Name, ChipDescription.NameComparer)
				.Select(group => group.First())
				.ToArray()
				?? Array.Empty<ChipDescription>();

			HashSet<string> availableCustomChips = new(localChips.Select(chip => chip.Name), ChipDescription.NameComparer);
			HashSet<string> builtinChips = new(BuiltinChipCreator.CreateAllBuiltinChipDescriptions().Select(chip => chip.Name), ChipDescription.NameComparer);
			string[] missingLibraryReferences = GetMissingLibraryReferences(project, availableCustomChips, builtinChips);
			List<BundleValidationIssue> dependencyIssues = new();

			foreach (ChipDescription chip in localChips)
			{
				string[] missingDependencies = (chip.SubChips ?? Array.Empty<SubChipDescription>())
					.Select(subChip => subChip.Name)
					.Where(name => !builtinChips.Contains(name) && !availableCustomChips.Contains(name))
					.ToArray();

				if (missingDependencies.Length > 0)
				{
					dependencyIssues.Add(new BundleValidationIssue(chip.Name, missingDependencies));
				}
			}

			return new BundleValidationResult(localChips, missingChipNames ?? Array.Empty<string>(), missingLibraryReferences, dependencyIssues.ToArray());
		}

		static string[] GetMissingLibraryReferences(ProjectDescription project, HashSet<string> availableCustomChips, HashSet<string> builtinChips)
		{
			HashSet<string> referencedChipNames = new(ChipDescription.NameComparer);

			foreach (StarredItem item in project.StarredList ?? new List<StarredItem>())
			{
				if (!item.IsCollection)
				{
					referencedChipNames.Add(item.Name);
				}
			}

			foreach (ChipCollection collection in project.ChipCollections ?? new List<ChipCollection>())
			{
				foreach (string chipName in collection.Chips)
				{
					referencedChipNames.Add(chipName);
				}
			}

			return referencedChipNames
				.Where(name => !string.IsNullOrWhiteSpace(name) && !availableCustomChips.Contains(name) && !builtinChips.Contains(name))
				.OrderBy(name => name, ChipDescription.NameComparer)
				.ToArray();
		}

		static string FormatList(IEnumerable<string> values, int maxItems = 8, int itemsPerLine = 8)
		{
			string[] names = values
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.Distinct(ChipDescription.NameComparer)
				.Take(maxItems)
				.ToArray();

			if (names.Length == 0)
			{
				return "nenhum";
			}

			List<string> rows = new();
			for (int i = 0; i < names.Length; i += Math.Max(1, itemsPerLine))
			{
				rows.Add(string.Join(", ", names.Skip(i).Take(itemsPerLine)));
			}

			return string.Join("\n", rows);
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
