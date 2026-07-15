using LearnHub.Helpers;
using LearnHub.Models.DTOs.Course;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers
{
    [ApiController]
    [Route("api/courses")]
    public class CoursesController : ControllerBase
    {
        private readonly CourseService _courseService;

        public CoursesController(CourseService courseService)
        {
            _courseService = courseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCatalogue([FromQuery] int page = 1, [FromQuery] int pageSize = 12, [FromQuery] string? search = null, [FromQuery] string? category = null)
        {
            var result = await _courseService.GetCatalogueAsync(page, pageSize, search, category);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(long id)
        {
            try
            {
                long? requestingUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
                var result = await _courseService.GetDetailAsync(id, requestingUserId);
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Create(CreateCourseDto dto)
        {
            var result = await _courseService.CreateAsync(dto, User.GetUserId());
            return CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Update(long id, UpdateCourseDto dto)
        {
            try
            {
                var result = await _courseService.UpdateAsync(id, dto, User.GetUserId());
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _courseService.DeleteAsync(id, User.GetUserId());
                return NoContent();
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPut("{id}/publish")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> TogglePublish(long id)
        {
            try
            {
                var result = await _courseService.TogglePublishAsync(id, User.GetUserId());
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
