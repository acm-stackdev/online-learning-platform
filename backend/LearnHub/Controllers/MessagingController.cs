using LearnHub.Helpers;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers
{
    [ApiController]
    [Route("api/messaging")]
    [Authorize]
    public class MessagingController : ControllerBase
    {
        private readonly MessagingService _messagingService;

        public MessagingController(MessagingService messagingService)
        {
            _messagingService = messagingService;
        }

        [HttpGet("conversations")]
        [Authorize(Roles = "Student,Instructor")]
        public async Task<IActionResult> GetMyConversations()
        {
            var result = await _messagingService.GetMyConversationsAsync(User.GetUserId());
            return Ok(result);
        }

        [HttpGet("conversations/{conversationId:long}/messages")]
        [Authorize(Roles = "Student,Instructor")]
        public async Task<IActionResult> GetHistory(long conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _messagingService.GetConversationHistoryAsync(conversationId, User.GetUserId(), page, pageSize);
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
