using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Progress
{
    public class UpdateLessonProgressDto
    {
        [Range(0, int.MaxValue)]
        public int WatchSeconds { get; set; }

        public bool IsCompleted { get; set; }
    }
}
