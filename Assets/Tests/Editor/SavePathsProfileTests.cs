using DLS.SaveSystem;
using NUnit.Framework;

namespace DLS.Tests.Editor
{
	public class SavePathsProfileTests
	{
		[TearDown]
		public void TearDown()
		{
			SavePaths.UseOfflineProfile();
		}

		[Test]
		public void UseOfflineProfile_PreservesLegacyAppDataProjectsPath()
		{
			SavePaths.UseOfflineProfile();

			Assert.IsFalse(SavePaths.IsCloudProfileActive);
			StringAssert.EndsWith("Projects", SavePaths.ProjectsPath.Replace('\\', '/'));
			Assert.IsFalse(SavePaths.ProjectsPath.Replace('\\', '/').Contains("/Profiles/"));
		}

		[Test]
		public void UseCloudProfile_IsolatesProjectsByUserId()
		{
			SavePaths.UseCloudProfile("firebase:user/abc");

			string normalizedPath = SavePaths.ProjectsPath.Replace('\\', '/');
			Assert.IsTrue(SavePaths.IsCloudProfileActive);
			StringAssert.Contains("/Profiles/firebase_user_abc/Projects", normalizedPath);
		}
	}
}
