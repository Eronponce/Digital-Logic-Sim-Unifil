using System;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace DLS.CloudSync
{
	/// <summary>
	/// Diagnostic file logger for cloud sync investigations.
	/// Active only in Unity Editor and Development Builds. Stripped from release builds
	/// via [Conditional] attribute, so no file IO ships to students.
	/// Writes to Desktop/cloud_sync_diag.log when active.
	/// </summary>
	public static class CloudSyncDiagnostics
	{
		const string DiagSymbol = "DEVELOPMENT_BUILD";
		const string EditorSymbol = "UNITY_EDITOR";

		static readonly string LogPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
			"cloud_sync_diag.log"
		);

		[Conditional(DiagSymbol), Conditional(EditorSymbol)]
		public static void Clear()
		{
			try
			{
				File.WriteAllText(LogPath, $"=== CloudSync Diagnostics — {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
				Debug.Log($"[CloudDiag] Log iniciado em: {LogPath}");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[CloudDiag] Nao conseguiu criar log: {ex.Message}");
			}
		}

		[Conditional(DiagSymbol), Conditional(EditorSymbol)]
		public static void Log(string message)
		{
			try
			{
				string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";
				File.AppendAllText(LogPath, line);
				Debug.Log($"[CloudDiag] {message}");
			}
			catch { }
		}

		[Conditional(DiagSymbol), Conditional(EditorSymbol)]
		public static void Section(string title)
		{
			Log($"\n--- {title} ---");
		}

		[Conditional(DiagSymbol), Conditional(EditorSymbol)]
		public static void DumpCurrentState()
		{
			Section("MANUAL DUMP");
			Log($"IsLoggedIn       : {FirebaseAuthManager.IsLoggedIn}");
			Log($"UserEmail        : {FirebaseAuthManager.UserEmail ?? "null"}");
			Log($"UserId           : {FirebaseAuthManager.UserId ?? "null"}");
			Log($"FirestoreReady   : {FirestoreDataManager.IsReady}");
			Log($"ProfilePath      : {DLS.SaveSystem.SavePaths.ActiveProfileDataPath}");
			Log($"AllData          : {DLS.SaveSystem.SavePaths.AllData}");

			string projectsPath = DLS.SaveSystem.SavePaths.ProjectsPath;
			Log($"ProjectsPath     : {projectsPath}");

			if (Directory.Exists(projectsPath))
			{
				foreach (string dir in Directory.GetDirectories(projectsPath))
				{
					string projName = Path.GetFileName(dir);
					string chipsPath = Path.Combine(dir, "Chips");
					string[] chips = Directory.Exists(chipsPath) ? Directory.GetFiles(chipsPath, "*.json") : Array.Empty<string>();
					Log($"  Project [{projName}]: {chips.Length} chip files on disk");
					foreach (string chip in chips)
						Log($"    - {Path.GetFileNameWithoutExtension(chip)}");
				}
			}
			else
			{
				Log("  ProjectsPath does not exist on disk");
			}
		}
	}
}
