using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Course
{
    public class UpdateSectionDto
    {
        [Required, StringLength(200, MinimumLength = 1)]
        public string Title { get; set; }
    }
}
