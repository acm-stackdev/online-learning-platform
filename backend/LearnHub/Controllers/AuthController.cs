using LearnHub.Data;
using LearnHub.Helpers;
using LearnHub.Models.DTOs.Auth;
using LearnHub.Models.Entities;
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
        private readonly IConfiguration _config;
        private const string AccessCookie = "access_token";
        private const string RefreshCookie = "refresh_token";

        public AuthController(AuthService authService, AppDbContext db, IWebHostEnvironment env, IConfiguration config)
        {
            _authService = authService;
            _db = db;
            _env = env;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            try
            {
                await _authService.RegisterAsync(dto);
                return Ok(new RegisterResponseDto { Message = "Registered. Please check your email to verify your account." });
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
                if (result.VerificationRequired)
                    return Ok(new RegisterResponseDto { Message = "Registered. Please check your email to verify your account." });

                SetAuthCookies(result.AccessToken!, result.RefreshToken!);
                return Ok(ToUserResponse(result.User));
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailDto dto)
        {
            try
            {
                await _authService.VerifyEmailAsync(dto.Token);
                return Ok(new { message = "Email verified. You can now log in." });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            await _authService.ForgotPasswordAsync(dto);
            return Ok(new { message = "If an account with this email exists, we've sent password reset instructions." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            try
            {
                await _authService.ResetPasswordAsync(dto);
                return Ok(new { message = "Password reset successfully. You can now log in." });
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
            Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/auth" });
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

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            try
            {
                var user = await _authService.UpdateProfileAsync(User.GetUserId(), dto);
                return Ok(ToUserResponse(user));
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            try
            {
                await _authService.ChangePasswordAsync(User.GetUserId(), dto);
                return Ok(new { message = "Password changed successfully." });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        private void SetAuthCookies(string accessToken, string refreshToken)
        {
            var baseOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            };

            var accessTokenExpiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "15");

            Response.Cookies.Append(AccessCookie, accessToken, new CookieOptions
            {
                HttpOnly = baseOptions.HttpOnly,
                Secure = baseOptions.Secure,
                SameSite = baseOptions.SameSite,
                Expires = DateTimeOffset.UtcNow.AddMinutes(accessTokenExpiryMinutes),
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

        private static UserResponseDto ToUserResponse(User user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            AvatarUrl = user.AvatarUrl,
            PresenceStatus = user.PresenceStatus.ToString(),
        };
    }
}
