using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Chatbot
{
    public class ChatRequestDto
    {
        [Required, StringLength(2000, MinimumLength = 1)]
        public string Message { get; set; }

        [MaxLength(20)]
        public List<ChatMessageDto>? History { get; set; }
    }
}
