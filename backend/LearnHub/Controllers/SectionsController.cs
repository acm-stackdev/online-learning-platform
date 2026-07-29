using LearnHub.Helpers;
using LearnHub.Models.DTOs.Course;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers
{
    [ApiController]
    [Route("api/sections")]
    [Authorize(Roles = "Instructor")]
    public class SectionsController : ControllerBase
    {
        private readonly SectionService _sectionService;

        public SectionsController(SectionService sectionService)
        {
            _sectionService = sectionService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateSectionDto dto)
        {
            try
            {
                var result = await _sectionService.CreateAsync(dto, User.GetUserId());
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> Reorder(ReorderSectionsDto dto)
        {
            try
            {
                await _sectionService.ReorderAsync(dto, User.GetUserId());
                return Ok();
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, UpdateSectionDto dto)
        {
            try
            {
                var result = await _sectionService.UpdateAsync(id, dto, User.GetUserId());
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
                await _sectionService.DeleteAsync(id, User.GetUserId());
                return NoContent();
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
