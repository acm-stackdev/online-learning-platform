using LearnHub.Models.DTOs.Course;

namespace LearnHub.Models.DTOs.Enrollment
{
    public class EnrollmentDto
    {
        public long Id { get; set; }
        public CourseListItemDto Course { get; set; }
        public DateTime EnrolledAt { get; set; }
        public bool IsCompleted { get; set; }
    }
}
