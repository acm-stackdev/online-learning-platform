using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Course
{
    public class CreateCourseDto
    {
        [Required, StringLength(200, MinimumLength = 3)]
        public string Title { get; set; }

        [Required, StringLength(2000, MinimumLength = 10)]
        public string Description { get; set; }

        public string? ThumbnailUrl { get; set; }

        public string? Category { get; set; }
    }
}
