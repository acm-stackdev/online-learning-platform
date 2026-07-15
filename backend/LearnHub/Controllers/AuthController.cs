using LearnHub.Data;
using LearnHub.Helpers;
using LearnHub.Models.DTOs.Auth;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private const string AccessCookie = "access_token";
        private const string RefreshCookie = "refresh_token";

        public AuthController(AuthService authService, AppDbContext db, IWebHostEnvironment env)
        {
            _authService = authService;
            _db = db;
            _env = env;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            try
            {
                var result = await _authService.RegisterAsync(dto);
                SetAuthCookies(result.AccessToken, result.RefreshToken);
                return Ok(ToUserResponse(result.User));
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                SetAuthCookies(result.AccessToken, result.RefreshToken);
                return Ok(ToUserResponse(result.User));
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost("google")]
        public async Task<IActionResult> Google(GoogleLoginDto dto)
        {
            try
            {
                var result = await _authService.GoogleLoginAsync(dto);
                SetAuthCookies(result.AccessToken, result.RefreshToken);
                return Ok(ToUserResponse(result.User));
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var rawRefreshToken = Request.Cookies[RefreshCookie];
            if (string.IsNullOrEmpty(rawRefreshToken))
                return Unauthorized(new { message = "No refresh token provided." });

            try
            {
                var tokens = await _authService.RefreshAsync(rawRefreshToken);
                SetAuthCookies(tokens.AccessToken, tokens.RefreshToken);
                return Ok();
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var rawRefreshToken = Request.Cookies[RefreshCookie];
            if (!string.IsNullOrEmpty(rawRefreshToken))
                await _authService.LogoutAsync(rawRefreshToken);

            Response.Cookies.Delete(AccessCookie);
            Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/auth/refresh" });
            return Ok();
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var user = await _db.Users.FindAsync(User.GetUserId());
            if (user is null) return Unauthorized();

            return Ok(ToUserResponse(user));
        }

        private void SetAuthCookies(string accessToken, string refreshToken)
        {
            var baseOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            };

            Response.Cookies.Append(AccessCookie, accessToken, new CookieOptions
            {
                HttpOnly = baseOptions.HttpOnly,
                Secure = baseOptions.Secure,
                SameSite = baseOptions.SameSite,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15),
            });

            Response.Cookies.Append(RefreshCookie, refreshToken, new CookieOptions
            {
                HttpOnly = baseOptions.HttpOnly,
                Secure = baseOptions.Secure,
                SameSite = baseOptions.SameSite,
                Path = "/api/auth",
                Expires = DateTimeOffset.UtcNow.AddDays(7),
            });
        }

        private static UserResponseDto ToUserResponse(Models.User user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
        };
    }
}
