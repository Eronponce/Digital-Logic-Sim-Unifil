using DLS.CloudSync;
using NUnit.Framework;

namespace DLS.Tests.Editor
{
	public class CloudUserProfileTests
	{
		[Test]
		public void StudentProfile_RequiresCompletionWhenMetadataIsMissing()
		{
			CloudUserProfile profile = new("uid", "student@unifil.br", "Aluno", AppUserRole.Student, true, string.Empty, string.Empty, false);

			Assert.IsTrue(profile.RequiresStudentProfileCompletion);
		}

		[Test]
		public void TeacherProfile_NeverRequiresStudentCompletion()
		{
			CloudUserProfile profile = new("uid", "teacher@unifil.br", "Professor", AppUserRole.Teacher, true, string.Empty, string.Empty, false);

			Assert.IsFalse(profile.RequiresStudentProfileCompletion);
		}
	}
}
