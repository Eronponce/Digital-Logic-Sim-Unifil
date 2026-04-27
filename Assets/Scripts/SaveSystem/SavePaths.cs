using System.IO;
using System.Linq;
using UnityEngine;

namespace DLS.SaveSystem
{
	public static class SavePaths
	{
		const bool UseBuildPathInEditor = false;
		const string LocalProfileName = "Local";

		public const string ProjectFileName = "ProjectDescription.json";
		public static readonly string dataPath_Build = Application.persistentDataPath;
		static readonly string dataPath_Editor = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "TestData");
		public static readonly string AllData = Application.isEditor && !UseBuildPathInEditor ? dataPath_Editor : dataPath_Build;
		static string activeProfileDataPath = AllData;

		// Path to save folder for all projects
		public static string ActiveProfileName { get; private set; } = LocalProfileName;
		public static bool IsCloudProfileActive { get; private set; }
		public static string ActiveProfileDataPath => activeProfileDataPath;
		public static string ProfilesPath => Path.Combine(AllData, "Profiles");
		public static string ProjectsPath => Path.Combine(activeProfileDataPath, "Projects");
		public static string DeletedProjectsPath => Path.Combine(activeProfileDataPath, "Deleted Projects");
		public static string AppSettingsPath => Path.Combine(activeProfileDataPath, "AppSettings.json");

		public static void EnsureDirectoryExists(string directoryPath) => Directory.CreateDirectory(directoryPath);
		public static void EnsureActiveProfileDirectories() => EnsureDirectoryExists(ProjectsPath);

		public static void UseOfflineProfile()
		{
			ActiveProfileName = LocalProfileName;
			IsCloudProfileActive = false;
			activeProfileDataPath = AllData;
			EnsureActiveProfileDirectories();
		}

		public static void UseCloudProfile(string userId)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				UseOfflineProfile();
				return;
			}

			string safeUserId = SanitizePathSegment(userId);
			ActiveProfileName = safeUserId;
			IsCloudProfileActive = true;
			activeProfileDataPath = Path.Combine(ProfilesPath, safeUserId);
			EnsureActiveProfileDirectories();
		}

		// ---- Path to save folder for a specific project ----
		public static string GetProjectPath(string projectName) => Path.Combine(ProjectsPath, projectName);
		public static string GetDeletedProjectPath(string projectName) => Path.Combine(DeletedProjectsPath, projectName);
		public static string GetChipsPath(string projectName) => Path.Combine(GetProjectPath(projectName), "Chips");
		public static string GetDeletedChipsPath(string projectName) => Path.Combine(GetProjectPath(projectName), "Deleted Chips");
		public static string GetProjectDescriptionPath(string projectName) => Path.Combine(GetProjectPath(projectName), ProjectFileName);

		static string SanitizePathSegment(string value)
		{
			char[] invalidChars = Path.GetInvalidFileNameChars();
			string sanitized = new(value.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
			return string.IsNullOrWhiteSpace(sanitized) ? "unknown-user" : sanitized;
		}
	}
}
