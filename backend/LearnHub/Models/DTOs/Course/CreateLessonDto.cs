using System.ComponentModel.DataAnnotations;
using LearnHub.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace LearnHub.Models.DTOs.Course
{
    public class CreateLessonDto
    {
        [Required]
        public long SectionId { get; set; }

        [Required, StringLength(200, MinimumLength = 1)]
        public string Title { get; set; }

        [Required]
        public ContentType ContentType { get; set; }

        [Range(0, int.MaxValue)]
        public int Duration { get; set; }

        [Required]
        public IFormFile File { get; set; }
    }
}
