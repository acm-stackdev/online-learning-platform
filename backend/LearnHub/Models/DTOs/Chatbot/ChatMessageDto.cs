using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Chatbot
{
    public class ChatMessageDto
    {
        [Required]
        public string Role { get; set; }

        [Required, StringLength(4000)]
        public string Content { get; set; }
    }
}
