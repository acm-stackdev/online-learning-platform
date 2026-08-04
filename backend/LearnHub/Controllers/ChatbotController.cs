using LearnHub.Helpers;
using LearnHub.Models.DTOs.Chatbot;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers
{
    [ApiController]
    [Route("api/courses/{courseId:long}/chat")]
    [Authorize]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotService _chatbotService;

        public ChatbotController(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost]
        public async Task<IActionResult> Ask(long courseId, ChatRequestDto dto)
        {
            try
            {
                var result = await _chatbotService.AskAsync(courseId, User.GetUserId(), User.IsInRole("Admin"), dto);
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
