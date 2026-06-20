using System;
using System.Collections.Generic;
using DLS.Description;

namespace DLS.CloudSync
{
	public static class CloudSyncPolicy
	{
		static readonly string[] supportedTeacherNames = { "ERON", "GUSTAVO" };
		public static IReadOnlyList<string> SupportedTeacherNames => supportedTeacherNames;

		public static string CreateLookupKey(string value)
		{
			return string.IsNullOrWhiteSpace(value)
				? string.Empty
				: value.Trim().ToLowerInvariant();
		}

		public static bool ShouldRestoreCloudProject(ProjectDescription localProject, ProjectDescription cloudProject, bool localChipDataComplete)
		{
			if (!localChipDataComplete)
			{
				// cloudProject.AllCustomChipNames is already reconciled by SyncProjectChipIndex to
				// reflect chips that actually exist in the Firestore subcollection.
				// Only restore if cloud has at least as many chips as local declares,
				// so an incomplete cloud bundle never overwrites more complete local data.
				int localDeclared = localProject.AllCustomChipNames?.Length ?? 0;
				int cloudActual = cloudProject.AllCustomChipNames?.Length ?? 0;
				bool restore = cloudActual >= localDeclared;
				CloudSyncDiagnostics.Log($"ShouldRestore [{localProject.ProjectName}]: localChipDataIncomplete → localDeclared={localDeclared} cloudActual={cloudActual} → restore={restore}");
				return restore;
			}

			bool result = cloudProject.LastSaveTime > localProject.LastSaveTime;
			CloudSyncDiagnostics.Log($"ShouldRestore [{localProject.ProjectName}]: localComplete → cloudTime={cloudProject.LastSaveTime:yyyy-MM-dd HH:mm:ss} localTime={localProject.LastSaveTime:yyyy-MM-dd HH:mm:ss} → restore={result}");
			return result;
		}

		public static AppUserRole ResolveSuggestedRole(string email, IEnumerable<string> teacherEmailAllowlist)
		{
			string normalizedEmail = CreateLookupKey(email);
			if (string.IsNullOrEmpty(normalizedEmail))
			{
				return AppUserRole.Student;
			}

			foreach (string teacherEmail in teacherEmailAllowlist ?? Array.Empty<string>())
			{
				if (CreateLookupKey(teacherEmail) == normalizedEmail)
				{
					return AppUserRole.Teacher;
				}
			}

			return AppUserRole.Student;
		}

		public static AppUserRole PreferExistingRole(AppUserRole existingRole, AppUserRole suggestedRole)
		{
			return existingRole == AppUserRole.Teacher ? AppUserRole.Teacher : suggestedRole;
		}

		public static bool RequiresStudentProfile(AppUserRole role)
		{
			return role != AppUserRole.Teacher;
		}

		public static bool TryNormalizeTeacherName(string teacherName, out string normalizedTeacherName)
		{
			string candidate = teacherName?.Trim() ?? string.Empty;

			foreach (string supportedTeacherName in supportedTeacherNames)
			{
				if (string.Equals(candidate, supportedTeacherName, StringComparison.OrdinalIgnoreCase))
				{
					normalizedTeacherName = supportedTeacherName;
					return true;
				}
			}

			normalizedTeacherName = string.Empty;
			return false;
		}

		public static string NormalizeTeacherNameOrEmpty(string teacherName)
		{
			return TryNormalizeTeacherName(teacherName, out string normalizedTeacherName)
				? normalizedTeacherName
				: string.Empty;
		}

		public static int GetTeacherIndex(string teacherName)
		{
			string normalizedTeacherName = NormalizeTeacherNameOrEmpty(teacherName);
			for (int i = 0; i < supportedTeacherNames.Length; i++)
			{
				if (supportedTeacherNames[i] == normalizedTeacherName)
				{
					return i;
				}
			}

			return -1;
		}

		public static bool HasRequiredStudentMetadata(string studentName, string registrationNumber, string teacherName, string turmaId = "")
		{
			return !string.IsNullOrWhiteSpace(studentName)
				&& !string.IsNullOrWhiteSpace(registrationNumber)
				&& (!string.IsNullOrWhiteSpace(turmaId) || TryNormalizeTeacherName(teacherName, out _));
		}

		public static AppUserRole ParseRole(string persistedRole)
		{
			return string.Equals(persistedRole, "teacher", StringComparison.OrdinalIgnoreCase)
				? AppUserRole.Teacher
				: AppUserRole.Student;
		}

		public static string ToPersistedRole(AppUserRole role)
		{
			return role == AppUserRole.Teacher ? "teacher" : "student";
		}
	}
}
