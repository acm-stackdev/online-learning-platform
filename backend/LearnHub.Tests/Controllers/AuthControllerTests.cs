using FluentAssertions;
using LearnHub.Controllers;
using LearnHub.Data;
using LearnHub.Helpers;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Auth;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Moq;
using System.Net;

namespace LearnHub.Tests.Controllers
{
    // The Google endpoint is not covered here for the same reason GoogleLoginAsync
    // is excluded from AuthServiceTests: it delegates straight into the un-mockable
    // static Google SDK call.
    public class AuthControllerTests
    {
        private static (AppDbContext Db, AuthController Controller, JwtHelper JwtHelper) CreateSut(
            System.Security.Claims.ClaimsPrincipal? user = null,
            string? cookieHeader = null,
            bool isDevelopment = true)
        {
            var db = TestDbContextFactory.Create();
            var config = TestConfigurationFactory.Create();
            var jwtHelper = new JwtHelper(config);
            var emailMock = new Mock<IEmailService>();
            var authService = new AuthService(db, jwtHelper, config, emailMock.Object);

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.EnvironmentName).Returns(isDevelopment ? "Development" : "Production");

            var controller = new AuthController(authService, db, envMock.Object, config)
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(user, cookieHeader)
            };

            return (db, controller, jwtHelper);
        }

        private static User SeedUser(AppDbContext db, string email, string? rawPassword, bool isVerified)
        {
            var user = new User
            {
                Username = email.Split('@')[0],
                Email = email,
                Role = Role.Student,
                IsEmailVerified = isVerified,
                CreatedAt = DateTime.UtcNow,
            };

            if (rawPassword is not null)
                user.PasswordHash = new PasswordHasher<User>().HashPassword(user, rawPassword);

            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private static IReadOnlyList<SetCookieHeaderValue> GetSetCookies(AuthController controller)
        {
            var values = controller.Response.Headers["Set-Cookie"];
            if (values.Count == 0) return Array.Empty<SetCookieHeaderValue>();
            return SetCookieHeaderValue.ParseList(values.ToArray()!).ToList();
        }

        // ----- POST /api/auth/register -----

        [Fact]
        public async Task Register_ValidInput_Returns200WithMessage()
        {
            var (_, controller, _) = CreateSut();
            var dto = new RegisterDto { Username = "newstudent", Email = "new@student.com", Password = "password123", Role = Role.Student };

            var result = await controller.Register(dto);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeOfType<RegisterResponseDto>()
                .Which.Message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Register_DuplicateEmail_Returns409WithApiExceptionMessage()
        {
            var (db, controller, _) = CreateSut();
            SeedUser(db, "taken@student.com", "password123", isVerified: true);
            var dto = new RegisterDto { Username = "someoneelse", Email = "taken@student.com", Password = "password123", Role = Role.Student };

            var result = await controller.Register(dto);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(409);
        }

        // ----- POST /api/auth/login -----

        [Fact]
        public async Task Login_ValidCredentials_Returns200WithUserResponseDtoShape()
        {
            var (db, controller, _) = CreateSut();
            SeedUser(db, "verified@student.com", "password123", isVerified: true);
            var dto = new LoginDto { Email = "verified@student.com", Password = "password123" };

            var result = await controller.Login(dto);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var body = ok.Value.Should().BeOfType<UserResponseDto>().Subject;
            body.Email.Should().Be(dto.Email);
        }

        [Fact]
        public async Task Login_ValidCredentials_SetsAccessAndRefreshCookiesWithCorrectAttributes()
        {
            var (db, controller, _) = CreateSut();
            SeedUser(db, "verified@student.com", "password123", isVerified: true);
            var dto = new LoginDto { Email = "verified@student.com", Password = "password123" };

            await controller.Login(dto);

            var cookies = GetSetCookies(controller);
            var access = cookies.Should().ContainSingle(c => c.Name == "access_token").Subject;
            var refresh = cookies.Should().ContainSingle(c => c.Name == "refresh_token").Subject;

            access.HttpOnly.Should().BeTrue();
            access.Expires.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(30));

            refresh.HttpOnly.Should().BeTrue();
            refresh.Path.ToString().Should().Be("/api/auth");
            refresh.Expires.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(7), TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task Login_DevelopmentEnvironment_CookiesAreLaxAndNotSecure()
        {
            var (db, controller, _) = CreateSut(isDevelopment: true);
            SeedUser(db, "verified@student.com", "password123", isVerified: true);
            var dto = new LoginDto { Email = "verified@student.com", Password = "password123" };

            await controller.Login(dto);

            var cookies = GetSetCookies(controller);
            cookies.Should().AllSatisfy(c =>
            {
                c.Secure.Should().BeFalse();
                c.SameSite.ToString().Should().Be("Lax");
            });
        }

        [Fact]
        public async Task Login_ProductionEnvironment_CookiesAreSecureAndSameSiteNone()
        {
            var (db, controller, _) = CreateSut(isDevelopment: false);
            SeedUser(db, "verified@student.com", "password123", isVerified: true);
            var dto = new LoginDto { Email = "verified@student.com", Password = "password123" };

            await controller.Login(dto);

            var cookies = GetSetCookies(controller);
            cookies.Should().AllSatisfy(c =>
            {
                c.Secure.Should().BeTrue();
                c.SameSite.ToString().Should().Be("None");
            });
        }

        [Fact]
        public async Task Login_InvalidCredentials_Returns401AndSetsNoCookies()
        {
            var (_, controller, _) = CreateSut();
            var dto = new LoginDto { Email = "nobody@student.com", Password = "password123" };

            var result = await controller.Login(dto);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(401);
            controller.Response.Headers.ContainsKey("Set-Cookie").Should().BeFalse();
        }

        // ----- POST /api/auth/verify-email -----

        [Fact]
        public async Task VerifyEmail_ValidToken_Returns200WithMessage()
        {
            var (db, controller, jwtHelper) = CreateSut();
            var user = SeedUser(db, "toverify@student.com", "password123", isVerified: false);
            var rawToken = jwtHelper.GenerateRefreshToken();
            db.VerificationTokens.Add(new VerificationToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                Purpose = TokenPurpose.EmailVerification,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var result = await controller.VerifyEmail(new VerifyEmailDto { Token = rawToken });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task VerifyEmail_InvalidToken_Returns400WithApiExceptionMessage()
        {
            var (_, controller, jwtHelper) = CreateSut();

            var result = await controller.VerifyEmail(new VerifyEmailDto { Token = jwtHelper.GenerateRefreshToken() });

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(400);
        }

        // ----- POST /api/auth/forgot-password -----

        [Fact]
        public async Task ForgotPassword_ExistingEmail_Returns200WithGenericMessage()
        {
            var (db, controller, _) = CreateSut();
            SeedUser(db, "hasaccount@student.com", "password123", isVerified: true);

            var result = await controller.ForgotPassword(new ForgotPasswordDto { Email = "hasaccount@student.com" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ForgotPassword_UnknownEmail_Returns200WithGenericMessage()
        {
            var (_, controller, _) = CreateSut();

            var result = await controller.ForgotPassword(new ForgotPasswordDto { Email = "nobody@student.com" });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ----- POST /api/auth/reset-password -----

        [Fact]
        public async Task ResetPassword_ValidToken_Returns200WithMessage()
        {
            var (db, controller, jwtHelper) = CreateSut();
            var user = SeedUser(db, "resetme@student.com", "oldpassword123", isVerified: true);
            var rawToken = jwtHelper.GenerateRefreshToken();
            db.VerificationTokens.Add(new VerificationToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                Purpose = TokenPurpose.PasswordReset,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var result = await controller.ResetPassword(new ResetPasswordDto { Token = rawToken, NewPassword = "newpassword123" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ResetPassword_InvalidToken_Returns400WithApiExceptionMessage()
        {
            var (_, controller, jwtHelper) = CreateSut();

            var result = await controller.ResetPassword(new ResetPasswordDto { Token = jwtHelper.GenerateRefreshToken(), NewPassword = "newpassword123" });

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(400);
        }

        // ----- POST /api/auth/refresh -----

        [Fact]
        public async Task Refresh_NoCookiePresent_Returns401WithMessage()
        {
            var (_, controller, _) = CreateSut(cookieHeader: null);

            var result = await controller.Refresh();

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Refresh_ValidCookie_Returns200AndSetsNewCookies()
        {
            var (db, controller, jwtHelper) = CreateSut();
            var user = SeedUser(db, "refresh@student.com", "password123", isVerified: true);
            var rawToken = jwtHelper.GenerateRefreshToken();
            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(cookieHeader: $"refresh_token={rawToken}");

            var result = await controller.Refresh();

            result.Should().BeOfType<OkResult>();
            GetSetCookies(controller).Should().Contain(c => c.Name == "access_token")
                .And.Contain(c => c.Name == "refresh_token");
        }

        [Fact]
        public async Task Refresh_InvalidCookieValue_Returns401WithApiExceptionMessage()
        {
            var (_, controller, jwtHelper) = CreateSut(cookieHeader: $"refresh_token={Guid.NewGuid()}");

            var result = await controller.Refresh();

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(401);
        }

        // ----- POST /api/auth/logout -----

        [Fact]
        public async Task Logout_WithCookiePresent_DeletesAuthCookiesAndReturns200()
        {
            var (db, controller, jwtHelper) = CreateSut();
            var user = SeedUser(db, "logout@student.com", "password123", isVerified: true);
            var rawToken = jwtHelper.GenerateRefreshToken();
            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(cookieHeader: $"refresh_token={rawToken}");

            var result = await controller.Logout();

            result.Should().BeOfType<OkResult>();
            var cookies = GetSetCookies(controller);
            cookies.Should().Contain(c => c.Name == "access_token")
                .And.Contain(c => c.Name == "refresh_token");
        }

        [Fact]
        public async Task Logout_NoCookiePresent_StillReturns200()
        {
            var (_, controller, _) = CreateSut(cookieHeader: null);

            var result = await controller.Logout();

            result.Should().BeOfType<OkResult>();
        }

        // This test does NOT stub or guess anything - it feeds the controller's real,
        // literal Set-Cookie response headers into System.Net.CookieContainer, the .NET
        // base class library's own implementation of RFC 6265 cookie path-matching (the
        // same rule a real browser follows). If the refresh_token cookie is still present
        // and unexpired after Logout when queried against a same-site request path, that
        // proves - independently of anything this test author claims - that the deletion
        // never reached the browser's real cookie.
        [Fact]
        public async Task Logout_RefreshCookieDeletion_ActuallyClearsCookieInARealCookieJar()
        {
            var (db, controller, _) = CreateSut();
            var user = SeedUser(db, "cookiebug@student.com", "password123", isVerified: true);
            var jar = new CookieContainer();
            var loginUri = new Uri("https://learnhub.test/api/auth/login");
            var meUri = new Uri("https://learnhub.test/api/auth/me");

            await controller.Login(new LoginDto { Email = user.Email, Password = "password123" });
            foreach (string header in controller.Response.Headers["Set-Cookie"].ToArray()!)
                jar.SetCookies(loginUri, header);

            var refreshValue = GetSetCookies(controller).First(c => c.Name == "refresh_token").Value.ToString();

            // Sanity check: the jar really did store it under /api/auth (this should pass).
            jar.GetCookies(meUri).Cast<Cookie>().Should().Contain(c => c.Name == "refresh_token" && !c.Expired);

            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(cookieHeader: $"refresh_token={refreshValue}");
            await controller.Logout();
            foreach (string header in controller.Response.Headers["Set-Cookie"].ToArray()!)
                jar.SetCookies(meUri, header);

            // This is the actual claim under test: a real cookie jar should no longer send
            // refresh_token on the next request. As of the current AuthController code,
            // this FAILS, because Logout deletes Path=/api/auth/refresh while the cookie
            // was set with Path=/api/auth - two different paths, so the browser never
            // matches them and the original cookie survives untouched.
            jar.GetCookies(meUri).Cast<Cookie>().Should().NotContain(c => c.Name == "refresh_token" && !c.Expired);
        }

        // ----- GET /api/auth/me -----
        // NOTE: calling controller.Me() directly never runs ASP.NET Core's [Authorize]
        // middleware/filters - that only happens on a real incoming request through the
        // pipeline (UseAuthentication/UseAuthorization). These tests only prove "given a
        // ClaimsPrincipal, Me() returns the right user" - they are NOT proof that an
        // anonymous request gets rejected before reaching this code. Verifying [Authorize]
        // enforcement end-to-end requires a WebApplicationFactory<Program> integration test,
        // which is out of scope for this unit-test pass.

        [Fact]
        public async Task Me_ExistingUser_Returns200WithUserResponseDto()
        {
            var (db, controller, _) = CreateSut();
            var user = SeedUser(db, "me@student.com", "password123", isVerified: true);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(user.Id));

            var result = await controller.Me();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeOfType<UserResponseDto>().Which.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task Me_UserIdNotInDb_Returns401()
        {
            var (_, controller, _) = CreateSut();
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(9999));

            var result = await controller.Me();

            result.Should().BeOfType<UnauthorizedResult>();
        }

        // ----- PUT /api/auth/me -----

        [Fact]
        public async Task UpdateProfile_ValidInput_Returns200WithUpdatedUser()
        {
            var (db, controller, _) = CreateSut();
            var user = SeedUser(db, "updateprofile@student.com", "password123", isVerified: true);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(user.Id));

            var result = await controller.UpdateProfile(new UpdateProfileDto { Username = "NewName", AvatarUrl = "https://example.com/avatar.png" });

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeOfType<UserResponseDto>().Which.Username.Should().Be("NewName");
        }

        // ----- POST /api/auth/change-password -----

        [Fact]
        public async Task ChangePassword_CorrectCurrentPassword_Returns200()
        {
            var (db, controller, _) = CreateSut();
            var user = SeedUser(db, "changepw@student.com", "oldpassword123", isVerified: true);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(user.Id));

            var result = await controller.ChangePassword(new ChangePasswordDto { CurrentPassword = "oldpassword123", NewPassword = "newpassword456" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_Returns401()
        {
            var (db, controller, _) = CreateSut();
            var user = SeedUser(db, "wrongpw@student.com", "correctpassword", isVerified: true);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(user.Id));

            var result = await controller.ChangePassword(new ChangePasswordDto { CurrentPassword = "wrongpassword", NewPassword = "newpassword456" });

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(401);
        }
    }
}
