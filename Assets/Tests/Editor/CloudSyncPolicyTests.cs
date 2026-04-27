using DLS.CloudSync;
using DLS.Description;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DLS.Tests.Editor
{
	public class CloudSyncPolicyTests
	{
		[Test]
		public void CreateLookupKey_NormalizesWhitespaceAndCase()
		{
			string lookupKey = CloudSyncPolicy.CreateLookupKey("  Projeto Final  ");

			Assert.AreEqual("projeto final", lookupKey);
		}

		[Test]
		public void ShouldRestoreCloudProject_ReturnsTrueWhenLocalChipDataIsIncomplete()
		{
			ProjectDescription localProject = CreateProject(lastSaveTime: new DateTime(2026, 4, 9, 10, 0, 0));
			ProjectDescription cloudProject = CreateProject(lastSaveTime: new DateTime(2026, 4, 9, 9, 0, 0));

			bool shouldRestore = CloudSyncPolicy.ShouldRestoreCloudProject(localProject, cloudProject, localChipDataComplete: false);

			Assert.IsTrue(shouldRestore);
		}

		[Test]
		public void ShouldRestoreCloudProject_ReturnsTrueWhenCloudIsNewer()
		{
			ProjectDescription localProject = CreateProject(lastSaveTime: new DateTime(2026, 4, 9, 10, 0, 0));
			ProjectDescription cloudProject = CreateProject(lastSaveTime: new DateTime(2026, 4, 9, 11, 0, 0));

			bool shouldRestore = CloudSyncPolicy.ShouldRestoreCloudProject(localProject, cloudProject, localChipDataComplete: true);

			Assert.IsTrue(shouldRestore);
		}

		[Test]
		public void ShouldRestoreCloudProject_ReturnsFalseWhenLocalIsCompleteAndNewer()
		{
			ProjectDescription localProject = CreateProject(lastSaveTime: new DateTime(2026, 4, 9, 11, 0, 0));
			ProjectDescription cloudProject = CreateProject(lastSaveTime: new DateTime(2026, 4, 9, 10, 0, 0));

			bool shouldRestore = CloudSyncPolicy.ShouldRestoreCloudProject(localProject, cloudProject, localChipDataComplete: true);

			Assert.IsFalse(shouldRestore);
		}

		[Test]
		public void ResolveSuggestedRole_MatchesTeacherAllowlistIgnoringCase()
		{
			AppUserRole role = CloudSyncPolicy.ResolveSuggestedRole(
				"Professor@Unifil.Br",
				new List<string> { "professor@unifil.br" });

			Assert.AreEqual(AppUserRole.Teacher, role);
		}

		[Test]
		public void PreferExistingRole_PreservesTeacherPromotion()
		{
			AppUserRole role = CloudSyncPolicy.PreferExistingRole(AppUserRole.Teacher, AppUserRole.Student);

			Assert.AreEqual(AppUserRole.Teacher, role);
		}

		[Test]
		public void NormalizeTeacherNameOrEmpty_ReturnsCanonicalTeacherName()
		{
			string normalizedTeacher = CloudSyncPolicy.NormalizeTeacherNameOrEmpty("  gustavo ");

			Assert.AreEqual("GUSTAVO", normalizedTeacher);
		}

		[Test]
		public void HasRequiredStudentMetadata_ReturnsTrueForValidStudentProfile()
		{
			bool hasRequiredMetadata = CloudSyncPolicy.HasRequiredStudentMetadata("Eron Ponce", "202600123", "ERON");

			Assert.IsTrue(hasRequiredMetadata);
		}

		[Test]
		public void HasRequiredStudentMetadata_ReturnsFalseWhenTeacherIsInvalid()
		{
			bool hasRequiredMetadata = CloudSyncPolicy.HasRequiredStudentMetadata("Eron Ponce", "202600123", "Professor X");

			Assert.IsFalse(hasRequiredMetadata);
		}

		static ProjectDescription CreateProject(DateTime lastSaveTime)
		{
			return new ProjectDescription
			{
				ProjectName = "Projeto",
				LastSaveTime = lastSaveTime,
				AllCustomChipNames = Array.Empty<string>(),
				StarredList = new List<StarredItem>(),
				ChipCollections = new List<ChipCollection>()
			};
		}
	}
}
