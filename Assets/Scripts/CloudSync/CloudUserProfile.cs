namespace DLS.CloudSync
{
	public enum AppUserRole
	{
		Student,
		Teacher
	}

	public sealed class CloudUserProfile
	{
		public static readonly CloudUserProfile Offline = new(
			string.Empty,
			string.Empty,
			"Offline",
			AppUserRole.Student,
			false,
			string.Empty,
			string.Empty,
			false,
			string.Empty);

		public string UserId { get; }
		public string Email { get; }
		public string DisplayName { get; }
		public AppUserRole Role { get; }
		public bool IsApproved { get; }
		public string RegistrationNumber { get; }
		public string TeacherName { get; }
		public string TurmaId { get; }
		public bool HasCompletedStudentProfile { get; }
		public bool IsTeacher => Role == AppUserRole.Teacher;
		public bool RequiresStudentProfileCompletion => !IsTeacher && (!HasCompletedStudentProfile || string.IsNullOrEmpty(TurmaId));
		public string RoleLabel => IsTeacher ? "Professor" : "Aluno";

		public CloudUserProfile(string userId, string email, string displayName, AppUserRole role, bool isApproved, string registrationNumber, string teacherName, bool hasCompletedStudentProfile, string turmaId = "")
		{
			UserId = userId ?? string.Empty;
			Email = email ?? string.Empty;
			DisplayName = string.IsNullOrWhiteSpace(displayName) ? email ?? userId ?? "Usuario" : displayName.Trim();
			Role = role;
			IsApproved = isApproved;
			RegistrationNumber = registrationNumber?.Trim() ?? string.Empty;
			TeacherName = teacherName?.Trim() ?? string.Empty;
			TurmaId = turmaId?.Trim() ?? string.Empty;
			HasCompletedStudentProfile = IsTeacher || hasCompletedStudentProfile;
		}
	}
}
