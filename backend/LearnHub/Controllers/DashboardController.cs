using LearnHub.Helpers;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;

        public DashboardController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("student")]
        public async Task<IActionResult> GetStudentDashboard()
        {
            var result = await _dashboardService.GetStudentDashboardAsync(User.GetUserId());
            return Ok(result);
        }

        [HttpGet("instructor")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> GetInstructorDashboard()
        {
            var result = await _dashboardService.GetInstructorDashboardAsync(User.GetUserId());
            return Ok(result);
        }
    }
}
