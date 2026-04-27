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
				return true;
			}

			return cloudProject.LastSaveTime > localProject.LastSaveTime;
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

		public static bool HasRequiredStudentMetadata(string studentName, string registrationNumber, string teacherName)
		{
			return !string.IsNullOrWhiteSpace(studentName)
				&& !string.IsNullOrWhiteSpace(registrationNumber)
				&& TryNormalizeTeacherName(teacherName, out _);
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
