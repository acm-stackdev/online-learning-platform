using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Course
{
    public class CreateSectionDto
    {
        [Required]
        public long CourseId { get; set; }

        [Required, StringLength(200, MinimumLength = 1)]
        public string Title { get; set; }
    }
}
