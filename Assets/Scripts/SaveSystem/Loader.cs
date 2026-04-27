using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLS.Description;
using DLS.Game;

namespace DLS.SaveSystem
{
	public static class Loader
	{
		public static AppSettings LoadAppSettings()
		{
			if (File.Exists(SavePaths.AppSettingsPath))
			{
				string settingsString = File.ReadAllText(SavePaths.AppSettingsPath);
				return Serializer.DeserializeAppSettings(settingsString);
			}

			return AppSettings.Default();
		}

		public static Project LoadProject(string projectName)
		{
			ProjectDescription projectDescription = LoadProjectDescription(projectName);
			ChipLibrary chipLibrary = LoadChipLibrary(projectDescription);
			return new Project(projectDescription, chipLibrary);
		}

		public static bool ProjectExists(string projectName)
		{
			string path = SavePaths.GetProjectDescriptionPath(projectName);
			return File.Exists(path);
		}

		public static ProjectDescription LoadProjectDescription(string projectName)
		{
			string path = SavePaths.GetProjectDescriptionPath(projectName);
			if (!File.Exists(path)) throw new Exception("No project description found at " + path);

			ProjectDescription desc = Serializer.DeserializeProjectDescription(File.ReadAllText(path));
			desc.ProjectName = projectName; // Enforce name = directory name (in case player modifies manually -- operations like deleting projects rely on this)

			for (int i = 0; i < desc.StarredList.Count; i++)
			{
				StarredItem starred = desc.StarredList[i];
				starred.CacheDisplayStrings();
				desc.StarredList[i] = starred;
			}

			foreach (ChipCollection collection in desc.ChipCollections)
			{
				collection.UpdateDisplayStrings();
			}

			return desc;
		}

		// Get list of saved project descriptions (ordered by last save time)
		public static ProjectDescription[] LoadAllProjectDescriptions()
		{
			List<ProjectDescription> projectDescriptions = new();
			if (!Directory.Exists(SavePaths.ProjectsPath))
			{
				return Array.Empty<ProjectDescription>();
			}

			foreach (string dir in Directory.EnumerateDirectories(SavePaths.ProjectsPath))
			{
				try
				{
					string projectName = Path.GetFileName(dir);
					projectDescriptions.Add(LoadProjectDescription(projectName));
				}
				catch (Exception)
				{
					// Ignore invalid project directory
				}
			}

			projectDescriptions.Sort((a, b) => b.LastSaveTime.CompareTo(a.LastSaveTime));
			return projectDescriptions.ToArray();
		}

		public static ChipDescription[] LoadChipDescriptions(string projectName)
		{
			return LoadChipDescriptions(LoadProjectDescription(projectName));
		}

		public static ChipDescription[] LoadChipDescriptions(ProjectDescription projectDescription)
		{
			return LoadChipDescriptions(projectDescription, skipMissingChips: false, out _);
		}

		public static ChipDescription[] LoadAvailableChipDescriptions(ProjectDescription projectDescription, out string[] missingChipNames)
		{
			return LoadChipDescriptions(projectDescription, skipMissingChips: true, out missingChipNames);
		}

		static ChipDescription[] LoadChipDescriptions(ProjectDescription projectDescription, bool skipMissingChips, out string[] missingChipNames)
		{
			string chipDirectoryPath = SavePaths.GetChipsPath(projectDescription.ProjectName);
			string[] customChipNames = projectDescription.AllCustomChipNames ?? Array.Empty<string>();
			List<string> missingChips = new();

			if (customChipNames.Length == 0)
			{
				missingChipNames = Array.Empty<string>();
				return Array.Empty<ChipDescription>();
			}

			if (!Directory.Exists(chipDirectoryPath))
			{
				if (skipMissingChips)
				{
					missingChipNames = customChipNames;
					return Array.Empty<ChipDescription>();
				}

				throw new DirectoryNotFoundException(chipDirectoryPath);
			}

			List<ChipDescription> loadedChips = new(customChipNames.Length);
			foreach (string chipName in customChipNames)
			{
				string chipPath = Path.Combine(chipDirectoryPath, chipName + ".json");
				if (!File.Exists(chipPath))
				{
					if (skipMissingChips)
					{
						missingChips.Add(chipName);
						continue;
					}

					throw new FileNotFoundException("Chip save file not found", chipPath);
				}

				string chipSaveString = File.ReadAllText(chipPath);
				loadedChips.Add(Serializer.DeserializeChipDescription(chipSaveString));
			}

			missingChipNames = missingChips.ToArray();
			return loadedChips.ToArray();
		}

		public static bool ProjectHasCompleteLocalChipData(ProjectDescription projectDescription)
		{
			string[] customChipNames = projectDescription.AllCustomChipNames ?? Array.Empty<string>();
			if (customChipNames.Length == 0)
			{
				return true;
			}

			string chipDirectoryPath = SavePaths.GetChipsPath(projectDescription.ProjectName);
			if (!Directory.Exists(chipDirectoryPath))
			{
				return false;
			}

			foreach (string chipName in customChipNames)
			{
				string chipPath = Path.Combine(chipDirectoryPath, chipName + ".json");
				if (!File.Exists(chipPath))
				{
					return false;
				}
			}

			return true;
		}

		static ChipLibrary LoadChipLibrary(ProjectDescription projectDescription)
		{
			ChipDescription[] loadedChips = LoadChipDescriptions(projectDescription);
			ChipDescription[] builtinChips = BuiltinChipCreator.CreateAllBuiltinChipDescriptions();
			HashSet<string> customChipNameHashset = new(ChipDescription.NameComparer);

			for (int i = 0; i < loadedChips.Length; i++)
			{
				customChipNameHashset.Add(loadedChips[i].Name);
			}


			// If built-in chip name conflicts with a custom chip, the built-in chip must have been added in a newer version.
			// In that case, simply exclude the built-in chip. TODO: warn player that they should rename their chip if they want access to new builtin version
			builtinChips = builtinChips.Where(b => !customChipNameHashset.Contains(b.Name)).ToArray();

			UpgradeHelper.ApplyVersionChanges(loadedChips, builtinChips);
			return new ChipLibrary(loadedChips, builtinChips);
		}
	}
}
