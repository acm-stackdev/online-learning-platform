using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.InstructorApplication
{
    public class SubmitInstructorApplicationDto
    {
        [Required]
        public string Message { get; set; }
    }
}
