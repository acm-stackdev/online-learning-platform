using LearnHub.Models;

namespace LearnHub.Models.DTOs.Course
{
    public class CourseListItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Category { get; set; }
        public CourseStatus Status { get; set; }
        public long InstructorId { get; set; }
        public string InstructorName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
