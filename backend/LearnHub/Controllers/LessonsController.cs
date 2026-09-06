using LearnHub.Helpers;
using LearnHub.Models.DTOs.Course;
using LearnHub.Models.DTOs.Progress;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers
{
    [ApiController]
    [Route("api/lessons")]
    [Authorize]
    public class LessonsController : ControllerBase
    {
        private const long MaxUploadBytes = 500L * 1024 * 1024;

        private readonly LessonService _lessonService;
        private readonly ProgressService _progressService;

        public LessonsController(LessonService lessonService, ProgressService progressService)
        {
            _lessonService = lessonService;
            _progressService = progressService;
        }

        [HttpPost]
        [Authorize(Roles = "Instructor")]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        public async Task<IActionResult> Create([FromForm] CreateLessonDto dto)
        {
            try
            {
                var result = await _lessonService.CreateAsync(dto, User.GetUserId());
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPut("reorder")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Reorder(ReorderLessonsDto dto)
        {
            try
            {
                await _lessonService.ReorderAsync(dto, User.GetUserId());
                return NoContent();
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "Instructor")]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        public async Task<IActionResult> Update(long id, [FromForm] UpdateLessonDto dto)
        {
            try
            {
                var result = await _lessonService.UpdateAsync(id, dto, User.GetUserId());
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _lessonService.DeleteAsync(id, User.GetUserId());
                return NoContent();
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPut("{id:long}/progress")]
        [Authorize(Roles = "Student,Instructor")]
        public async Task<IActionResult> UpdateProgress(long id, UpdateLessonProgressDto dto)
        {
            try
            {
                var result = await _progressService.UpdateProgressAsync(id, User.GetUserId(), dto);
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
