using LearnHub.Helpers;
using LearnHub.Models.DTOs.Course;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers
{
    [ApiController]
    [Route("api/lessons")]
    [Authorize(Roles = "Instructor")]
    public class LessonsController : ControllerBase
    {
        private const long MaxUploadBytes = 500L * 1024 * 1024;

        private readonly LessonService _lessonService;

        public LessonsController(LessonService lessonService)
        {
            _lessonService = lessonService;
        }

        [HttpPost]
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
        public async Task<IActionResult> Reorder(ReorderLessonsDto dto)
        {
            try
            {
                await _lessonService.ReorderAsync(dto, User.GetUserId());
                return Ok();
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPut("{id:long}")]
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
    }
}
