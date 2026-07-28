using Google.Apis.Auth;
using LearnHub.Data;
using LearnHub.Helpers;
using LearnHub.Models;
using LearnHub.Models.DTOs.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services
{
    public record AuthResult(User User, string AccessToken, string RefreshToken);
    public record TokenPair(string AccessToken, string RefreshToken);
    public record GoogleLoginResult(User User, string? AccessToken, string? RefreshToken, bool VerificationRequired);

    public class AuthService
    {
        private readonly AppDbContext _db;
        private readonly JwtHelper _jwtHelper;
        private readonly IEmailService _emailService;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _config;
        private const int RefreshTokenDays = 7;
        private const int VerificationTokenHours = 24;

        public AuthService(AppDbContext db, JwtHelper jwtHelper, IConfiguration config, IEmailService emailService)
        {
            _db = db;
            _jwtHelper = jwtHelper;
            _config = config;
            _emailService = emailService;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<User> RegisterAsync(RegisterDto dto)
        {
            var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
                throw new ApiException("An account with this email already exists.", 409);

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                Role = Role.Student,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await IssueVerificationTokenAsync(user);

            return user;
        }

        public async Task<AuthResult> LoginAsync(LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user is null)
                throw new ApiException("Invalid email or password.", 401);

            if (user.PasswordHash is null)
                throw new ApiException("This account uses Google sign-in. Please continue with Google.", 401);

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new ApiException("Invalid email or password.", 401);

            if (!user.IsEmailVerified)
                throw new ApiException("Please verify your email before logging in.", 403);

            return await IssueTokensAsync(user);
        }

        public async Task<GoogleLoginResult> GoogleLoginAsync(GoogleLoginDto dto)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Google:ClientId"] }
                });
            }
            catch (InvalidJwtException)
            {
                throw new ApiException("Invalid Google token.", 401);
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject)
                       ?? await _db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

            var isNewUser = false;
            if (user is null)
            {
                user = new User
                {
                    Username = payload.Name ?? payload.Email.Split('@')[0],
                    Email = payload.Email,
                    GoogleId = payload.Subject,
                    Role = Role.Student,
                    IsEmailVerified = false,
                    CreatedAt = DateTime.UtcNow,
                };
                _db.Users.Add(user);
                isNewUser = true;
            }
            else if (user.GoogleId is null)
            {
                user.GoogleId = payload.Subject;
            }

            await _db.SaveChangesAsync();

            if (isNewUser)
            {
                await IssueVerificationTokenAsync(user);
                return new GoogleLoginResult(user, null, null, true);
            }

            if (!user.IsEmailVerified)
                throw new ApiException("Please verify your email before logging in.", 403);

            var result = await IssueTokensAsync(user);
            return new GoogleLoginResult(user, result.AccessToken, result.RefreshToken, false);
        }

        public async Task<TokenPair> RefreshAsync(string rawRefreshToken)
        {
            var hash = _jwtHelper.HashToken(rawRefreshToken);
            var stored = await _db.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt < DateTime.UtcNow)
                throw new ApiException("Invalid or expired refresh token.", 401);

            stored.RevokedAt = DateTime.UtcNow;

            var result = await IssueTokensAsync(stored.User);
            return new TokenPair(result.AccessToken, result.RefreshToken);
        }

        public async Task LogoutAsync(string rawRefreshToken)
        {
            var hash = _jwtHelper.HashToken(rawRefreshToken);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);
            if (stored is not null && stored.RevokedAt is null)
            {
                stored.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task VerifyEmailAsync(string rawToken)
        {
            var hash = _jwtHelper.HashToken(rawToken);
            var stored = await _db.VerificationTokens
                .Include(vt => vt.User)
                .FirstOrDefaultAsync(vt => vt.TokenHash == hash && vt.Purpose == TokenPurpose.EmailVerification);

            if (stored is null || stored.UsedAt is not null || stored.ExpiresAt < DateTime.UtcNow)
                throw new ApiException("Invalid or expired verification link.", 400);

            stored.UsedAt = DateTime.UtcNow;
            stored.User.IsEmailVerified = true;
            await _db.SaveChangesAsync();
        }

        private async Task IssueVerificationTokenAsync(User user)
        {
            var rawToken = _jwtHelper.GenerateRefreshToken();

            _db.VerificationTokens.Add(new VerificationToken
            {
                UserId = user.Id,
                TokenHash = _jwtHelper.HashToken(rawToken),
                Purpose = TokenPurpose.EmailVerification,
                ExpiresAt = DateTime.UtcNow.AddHours(VerificationTokenHours),
                CreatedAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();

            var baseUrl = _config["Frontend:BaseUrl"];
            var link = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(rawToken)}";
            await _emailService.SendVerificationEmailAsync(user.Email, user.Username, link);
        }

        private async Task<AuthResult> IssueTokensAsync(User user)
        {
            var accessToken = _jwtHelper.GenerateAccessToken(user);
            var refreshToken = _jwtHelper.GenerateRefreshToken();

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = _jwtHelper.HashToken(refreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
                CreatedAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();

            return new AuthResult(user, accessToken, refreshToken);
        }
    }
}
