using LearnHub.Models.DTOs.Course;

namespace LearnHub.Models.DTOs.Dashboard
{
    public class InstructorDashboardDto
    {
        public int TotalCourses { get; set; }
        public int DraftCourseCount { get; set; }
        public int PendingApprovalCourseCount { get; set; }
        public int PublishedCourseCount { get; set; }
        public int RejectedCourseCount { get; set; }
        public int TotalStudentsEnrolled { get; set; }
        public List<CourseListItemDto> Courses { get; set; } = new();
    }
}
