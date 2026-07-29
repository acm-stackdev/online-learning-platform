using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Course
{
    public class ReorderSectionsDto
    {
        [Required]
        public long CourseId { get; set; }

        [Required]
        public List<long> OrderedSectionIds { get; set; } = new();
    }
}
