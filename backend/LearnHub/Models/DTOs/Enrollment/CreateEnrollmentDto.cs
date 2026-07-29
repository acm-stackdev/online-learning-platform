using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Enrollment
{
    public class CreateEnrollmentDto
    {
        [Required]
        public long CourseId { get; set; }
    }
}
