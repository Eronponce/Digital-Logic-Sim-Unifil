namespace DLS.CloudSync
{
	public sealed class CloudStudentProfileData
	{
		public string StudentName { get; }
		public string RegistrationNumber { get; }
		public string TeacherName { get; }

		public CloudStudentProfileData(string studentName, string registrationNumber, string teacherName)
		{
			StudentName = studentName?.Trim() ?? string.Empty;
			RegistrationNumber = registrationNumber?.Trim() ?? string.Empty;
			TeacherName = CloudSyncPolicy.NormalizeTeacherNameOrEmpty(teacherName);
		}
	}
}
