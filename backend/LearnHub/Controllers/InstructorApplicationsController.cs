using LearnHub.Helpers;
using LearnHub.Models.DTOs.InstructorApplication;
using LearnHub.Models.Entities;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers
{
    [ApiController]
    [Route("api/instructor-applications")]
    [Authorize]
    public class InstructorApplicationsController : ControllerBase
    {
        private readonly InstructorApplicationService _instructorApplicationService;

        public InstructorApplicationsController(InstructorApplicationService instructorApplicationService)
        {
            _instructorApplicationService = instructorApplicationService;
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Submit(SubmitInstructorApplicationDto dto)
        {
            try
            {
                var result = await _instructorApplicationService.SubmitAsync(User.GetUserId(), dto);
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpGet("mine")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMine()
        {
            var result = await _instructorApplicationService.GetMineAsync(User.GetUserId());
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] ApplicationStatus? status = null)
        {
            var result = await _instructorApplicationService.GetAllAsync(page, pageSize, status);
            return Ok(result);
        }

        [HttpPost("{id:long}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(long id)
        {
            try
            {
                var result = await _instructorApplicationService.ApproveAsync(id, User.GetUserId());
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost("{id:long}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(long id)
        {
            try
            {
                var result = await _instructorApplicationService.RejectAsync(id, User.GetUserId());
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
