using LearnHub.Helpers;
using LearnHub.Models.DTOs.Admin;
using LearnHub.Models.Entities;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] Role? role = null, [FromQuery] bool? isSuspended = null)
        {
            var result = await _adminService.GetUsersAsync(page, pageSize, search, role, isSuspended);
            return Ok(result);
        }

        [HttpPut("users/{id:long}/role")]
        public async Task<IActionResult> ChangeRole(long id, ChangeUserRoleDto dto)
        {
            try
            {
                var result = await _adminService.ChangeRoleAsync(id, dto.Role, User.GetUserId());
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost("users/{id:long}/suspend")]
        public async Task<IActionResult> Suspend(long id)
        {
            try
            {
                var result = await _adminService.SuspendUserAsync(id, User.GetUserId());
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost("users/{id:long}/reinstate")]
        public async Task<IActionResult> Reinstate(long id)
        {
            try
            {
                var result = await _adminService.ReinstateUserAsync(id);
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var result = await _adminService.GetPlatformStatsAsync();
            return Ok(result);
        }
    }
}
