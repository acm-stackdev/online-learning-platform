namespace LearnHub.Models.DTOs.Progress
{
    public class LessonProgressDto
    {
        public long LessonId { get; set; }
        public string LessonTitle { get; set; }
        public bool IsCompleted { get; set; }
        public int WatchSeconds { get; set; }
        public DateTime LastWatchedAt { get; set; }
    }
}
