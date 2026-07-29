namespace LearnHub.Models.DTOs.Progress
{
    public class EnrollmentProgressDto
    {
        public long EnrollmentId { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public double PercentComplete { get; set; }
        public bool IsCourseCompleted { get; set; }
        public List<LessonProgressDto> Lessons { get; set; } = new();
    }
}
