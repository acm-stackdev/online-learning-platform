namespace LearnHub.Models.DTOs.Admin
{
    public class PlatformStatsDto
    {
        public int TotalUsers { get; set; }
        public int StudentCount { get; set; }
        public int InstructorCount { get; set; }
        public int AdminCount { get; set; }
        public int SuspendedCount { get; set; }

        public int TotalCourses { get; set; }
        public int DraftCourseCount { get; set; }
        public int PendingApprovalCourseCount { get; set; }
        public int PublishedCourseCount { get; set; }
        public int RejectedCourseCount { get; set; }

        public int TotalEnrollments { get; set; }
        public int CompletedEnrollmentCount { get; set; }
        public int InProgressEnrollmentCount { get; set; }
    }
}
