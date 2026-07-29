using LearnHub.Helpers;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers
{
    [ApiController]
    [Route("api/certificates")]
    [Authorize]
    public class CertificatesController : ControllerBase
    {
        private readonly CertificateService _certificateService;

        public CertificatesController(CertificateService certificateService)
        {
            _certificateService = certificateService;
        }

        [HttpGet("{enrollmentId:long}")]
        public async Task<IActionResult> Get(long enrollmentId)
        {
            try
            {
                var result = await _certificateService.GetForEnrollmentAsync(enrollmentId, User.GetUserId(), User.IsInRole("Admin"));
                return Ok(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
