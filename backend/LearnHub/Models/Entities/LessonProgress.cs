namespace LearnHub.Models.Entities
{
    public class LessonProgress
    {
        public long Id { get; set; }
        public long EnrollmentId { get; set; }
        public long LessonId { get; set; }
        public bool IsCompleted { get; set; }
        public int WatchSeconds { get; set; }
        public DateTime LastWatchedAt { get; set; }

        public Enrollment Enrollment { get; set; }
        public Lesson Lesson { get; set; }
    }
}
