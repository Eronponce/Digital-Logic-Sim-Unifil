namespace DLS.CloudSync
{
	public sealed class CloudStudentProfileData
	{
		public string StudentName { get; }
		public string RegistrationNumber { get; }
		public string TeacherName { get; }
		public string TurmaId { get; }
		public string TurmaProjectName { get; }

		public CloudStudentProfileData(string studentName, string registrationNumber, string teacherName, string turmaId = "", string turmaProjectName = "")
		{
			StudentName = studentName?.Trim() ?? string.Empty;
			RegistrationNumber = registrationNumber?.Trim() ?? string.Empty;
			TeacherName = CloudSyncPolicy.NormalizeTeacherNameOrEmpty(teacherName);
			TurmaId = turmaId?.Trim() ?? string.Empty;
			TurmaProjectName = turmaProjectName?.Trim() ?? string.Empty;
		}
	}
}
