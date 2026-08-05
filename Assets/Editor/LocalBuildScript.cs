using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DLS.EditorTools {
	public static class LocalBuildScript {
		const string MainScenePath = "Assets/Build/DLS.unity";
		const string WindowsBuildOutput = "Builds/Windows/Digital-Logic-Sim-Unifil.exe";
		const string LinuxBuildOutput = "Builds/Linux/Digital-Logic-Sim-Unifil.x86_64";
		const string MacBuildOutput = "Builds/Mac/Digital-Logic-Sim-Unifil.app";

		[MenuItem("Build/Build Windows Test App")]
		public static void BuildWindowsPlayerFromMenu() {
			BuildWindowsPlayerDev();
		}

		[MenuItem("Build/Build Linux Test App")]
		public static void BuildLinuxPlayerFromMenu() {
			BuildLinuxPlayerDev();
		}

		[MenuItem("Build/Build Mac Test App")]
		public static void BuildMacPlayerFromMenu() {
			BuildMacPlayerDev();
		}

		public static void BuildWindowsPlayerDev() {
			BuildPlayer(WindowsBuildOutput, BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.AllowDebugging, "Windows");
		}

		public static void BuildWindowsPlayerRelease() {
			BuildPlayer(WindowsBuildOutput, BuildTarget.StandaloneWindows64, BuildOptions.None, "Windows");
		}

		public static void BuildWindowsPlayerCommunityRelease() {
			BuildPlayer(WindowsBuildOutput, BuildTarget.StandaloneWindows64, BuildOptions.None, "Windows (Community)", extraDefines: new[] { "DLS_COMMUNITY" });
		}

		public static void BuildLinuxPlayerDev() {
			BuildPlayer(LinuxBuildOutput, BuildTarget.StandaloneLinux64, BuildOptions.Development | BuildOptions.AllowDebugging, "Linux");
		}

		public static void BuildLinuxPlayerRelease() {
			BuildPlayer(LinuxBuildOutput, BuildTarget.StandaloneLinux64, BuildOptions.None, "Linux");
		}

		public static void BuildLinuxPlayerCommunityRelease() {
			BuildPlayer(LinuxBuildOutput, BuildTarget.StandaloneLinux64, BuildOptions.None, "Linux (Community)", extraDefines: new[] { "DLS_COMMUNITY" });
		}

		public static void BuildMacPlayerDev() {
			BuildPlayer(MacBuildOutput, BuildTarget.StandaloneOSX, BuildOptions.Development | BuildOptions.AllowDebugging, "Mac");
		}

		public static void BuildMacPlayerRelease() {
			BuildPlayer(MacBuildOutput, BuildTarget.StandaloneOSX, BuildOptions.None, "Mac");
		}

		public static void BuildMacPlayerCommunityRelease() {
			BuildPlayer(MacBuildOutput, BuildTarget.StandaloneOSX, BuildOptions.None, "Mac (Community)", extraDefines: new[] { "DLS_COMMUNITY" });
		}

		static void BuildPlayer(string relativeOutputPath, BuildTarget target, BuildOptions buildOptions, string platformLabel, string[] extraDefines = null) {
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string outputPath = Path.Combine(projectRoot, relativeOutputPath);
			string outputDirectory = Path.GetDirectoryName(outputPath);

			if (string.IsNullOrWhiteSpace(outputDirectory)) {
				throw new InvalidOperationException("Could not determine build output directory.");
			}

			Directory.CreateDirectory(outputDirectory);

			var buildPlayerOptions = new BuildPlayerOptions {
				scenes = new[] { MainScenePath },
				locationPathName = outputPath,
				target = target,
				options = buildOptions,
				extraScriptingDefines = extraDefines ?? Array.Empty<string>()
			};

			Debug.Log($"[Build] Starting {platformLabel} build at {outputPath}");
			BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
			BuildSummary summary = report.summary;

			if (summary.result != BuildResult.Succeeded) {
				throw new Exception($"{platformLabel} build failed: {summary.result}. See Unity build log for details.");
			}

			Debug.Log($"[Build] Build finished successfully: {outputPath}");
		}
	}
}
