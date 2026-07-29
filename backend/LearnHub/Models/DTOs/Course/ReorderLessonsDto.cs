using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Course
{
    public class ReorderLessonsDto
    {
        [Required]
        public long SectionId { get; set; }

        [Required]
        public List<long> OrderedLessonIds { get; set; } = new();
    }
}
