using LearnHub.Helpers;
using LearnHub.Models.DTOs.Enrollment;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers
{
    [ApiController]
    [Route("api/enrollments")]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly EnrollmentService _enrollmentService;
        private readonly ProgressService _progressService;

        public EnrollmentsController(EnrollmentService enrollmentService, ProgressService progressService)
        {
            _enrollmentService = enrollmentService;
            _progressService = progressService;
        }

        [HttpPost]
        [Authorize(Roles = "Student,Instructor")]
        public async Task<IActionResult> Create(CreateEnrollmentDto dto)
        {
            try
            {
                var result = await _enrollmentService.EnrollAsync(dto.CourseId, User.GetUserId(), User.GetRole());
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        // The enrolled student, the instructor who owns the course, or an Admin may remove an enrollment.
        [HttpDelete("{id:long}")]
        [Authorize(Roles = "Student,Instructor,Admin")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _enrollmentService.RemoveEnrollmentAsync(id, User.GetUserId(), User.IsInRole("Admin"));
                return NoContent();
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Student,Instructor")]
        public async Task<IActionResult> GetMine()
        {
            var result = await _enrollmentService.GetMyEnrollmentsAsync(User.GetUserId());
            return Ok(result);
        }

        [HttpGet("{id:long}/progress")]
        [Authorize(Roles = "Student,Instructor")]
        public async Task<IActionResult> GetProgress(long id)
        {
            try
            {
                var result = await _progressService.GetEnrollmentProgressAsync(id, User.GetUserId());
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpGet("course/{courseId:long}")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> GetRosterForCourse(long courseId)
        {
            try
            {
                var result = await _enrollmentService.GetCourseRosterAsync(courseId, User.GetUserId(), User.IsInRole("Admin"));
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
