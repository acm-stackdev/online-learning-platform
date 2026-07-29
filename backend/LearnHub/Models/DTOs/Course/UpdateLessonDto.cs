using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LearnHub.Models.DTOs.Course
{
    public class UpdateLessonDto
    {
        [Required, StringLength(200, MinimumLength = 1)]
        public string Title { get; set; }

        [Range(0, int.MaxValue)]
        public int Duration { get; set; }

        public IFormFile? File { get; set; }
    }
}
