using FluentAssertions;
using LearnHub.Data;
using LearnHub.Helpers;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Auth;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LearnHub.Tests.Services
{
    // GoogleLoginAsync is not covered here: it calls the static
    // GoogleJsonWebSignature.ValidateAsync from the Google API client directly.
    // A static call to an external SDK can't be intercepted by Moq without first
    // wrapping it behind an injectable interface (e.g. IGoogleTokenValidator).
    // That wrapper is a separate refactor, tracked as follow-up work.
    public class AuthServiceTests
    {
        private static (AppDbContext Db, AuthService Sut, JwtHelper JwtHelper, Mock<IEmailService> EmailMock) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            var config = TestConfigurationFactory.Create();
            var jwtHelper = new JwtHelper(config);
            var emailMock = new Mock<IEmailService>();
            var sut = new AuthService(db, jwtHelper, config, emailMock.Object);
            return (db, sut, jwtHelper, emailMock);
        }

        private static User SeedUser(AppDbContext db, string email, string? rawPassword, bool isVerified, Role role = Role.Student, string? googleId = null, bool isSuspended = false)
        {
            var user = new User
            {
                Username = email.Split('@')[0],
                Email = email,
                Role = role,
                IsEmailVerified = isVerified,
                GoogleId = googleId,
                IsSuspended = isSuspended,
                CreatedAt = DateTime.UtcNow,
            };

            if (rawPassword is not null)
                user.PasswordHash = new PasswordHasher<User>().HashPassword(user, rawPassword);

            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        // ----- RegisterAsync -----

        [Fact]
        public async Task RegisterAsync_ValidInput_CreatesUnverifiedUserAndSendsVerificationEmail()
        {
            var (db, sut, _, emailMock) = CreateSut();
            var dto = new RegisterDto { Username = "newstudent", Email = "new@student.com", Password = "password123", Role = Role.Student };

            var result = await sut.RegisterAsync(dto);

            result.IsEmailVerified.Should().BeFalse();
            result.Role.Should().Be(Role.Student);
            result.PasswordHash.Should().NotBeNullOrEmpty().And.NotBe(dto.Password);

            db.Users.Count().Should().Be(1);

            var token = db.VerificationTokens.SingleOrDefault(vt => vt.UserId == result.Id);
            token.Should().NotBeNull();
            token!.Purpose.Should().Be(TokenPurpose.EmailVerification);
            token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));

            emailMock.Verify(x => x.SendVerificationEmailAsync(dto.Email, dto.Username, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ThrowsApiException()
        {
            var (db, sut, _, emailMock) = CreateSut();
            SeedUser(db, "taken@student.com", "password123", isVerified: true);
            var dto = new RegisterDto { Username = "someoneelse", Email = "taken@student.com", Password = "password123", Role = Role.Student };

            var act = async () => await sut.RegisterAsync(dto);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(409);
            ex.Which.Message.Should().Contain("already exists");

            db.Users.Count().Should().Be(1);
            emailMock.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_InstructorRole_CreatesInstructor()
        {
            var (db, sut, _, _) = CreateSut();
            var dto = new RegisterDto { Username = "newinstructor", Email = "new@instructor.com", Password = "password123", Role = Role.Instructor };

            var result = await sut.RegisterAsync(dto);

            result.Role.Should().Be(Role.Instructor);
        }

        [Fact]
        public async Task RegisterAsync_AdminRole_ThrowsApiException()
        {
            var (db, sut, _, emailMock) = CreateSut();
            var dto = new RegisterDto { Username = "wannabeadmin", Email = "new@admin.com", Password = "password123", Role = Role.Admin };

            var act = async () => await sut.RegisterAsync(dto);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);

            db.Users.Count().Should().Be(0);
            emailMock.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_NoRole_ThrowsApiException()
        {
            var (db, sut, _, emailMock) = CreateSut();
            var dto = new RegisterDto { Username = "noroleuser", Email = "new@norole.com", Password = "password123", Role = null };

            var act = async () => await sut.RegisterAsync(dto);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);

            db.Users.Count().Should().Be(0);
            emailMock.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // ----- LoginAsync -----

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResultWithTokens()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            SeedUser(db, "verified@student.com", "password123", isVerified: true);
            var dto = new LoginDto { Email = "verified@student.com", Password = "password123" };

            var result = await sut.LoginAsync(dto);

            result.User.Email.Should().Be(dto.Email);
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();

            var storedHash = jwtHelper.HashToken(result.RefreshToken);
            db.RefreshTokens.Any(rt => rt.TokenHash == storedHash).Should().BeTrue();
        }

        [Fact]
        public async Task LoginAsync_UnknownEmail_ThrowsApiException()
        {
            var (_, sut, _, _) = CreateSut();
            var dto = new LoginDto { Email = "nobody@student.com", Password = "password123" };

            var act = async () => await sut.LoginAsync(dto);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task LoginAsync_GoogleOnlyAccount_ThrowsApiException()
        {
            var (db, sut, _, _) = CreateSut();
            SeedUser(db, "google@student.com", rawPassword: null, isVerified: true, googleId: "google-sub-123");
            var dto = new LoginDto { Email = "google@student.com", Password = "whatever" };

            var act = async () => await sut.LoginAsync(dto);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsApiException()
        {
            var (db, sut, _, _) = CreateSut();
            SeedUser(db, "verified@student.com", "correctpassword", isVerified: true);
            var dto = new LoginDto { Email = "verified@student.com", Password = "wrongpassword" };

            var act = async () => await sut.LoginAsync(dto);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task LoginAsync_UnverifiedEmail_ThrowsApiException()
        {
            var (db, sut, _, _) = CreateSut();
            SeedUser(db, "unverified@student.com", "password123", isVerified: false);
            var dto = new LoginDto { Email = "unverified@student.com", Password = "password123" };

            var act = async () => await sut.LoginAsync(dto);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task LoginAsync_SuspendedAccount_ThrowsApiException()
        {
            var (db, sut, _, _) = CreateSut();
            SeedUser(db, "suspended@student.com", "password123", isVerified: true, isSuspended: true);
            var dto = new LoginDto { Email = "suspended@student.com", Password = "password123" };

            var act = async () => await sut.LoginAsync(dto);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task LoginAsync_SuspendedAndUnverified_ReportsSuspensionFirst()
        {
            var (db, sut, _, _) = CreateSut();
            SeedUser(db, "suspendedunverified@student.com", "password123", isVerified: false, isSuspended: true);
            var dto = new LoginDto { Email = "suspendedunverified@student.com", Password = "password123" };

            var act = async () => await sut.LoginAsync(dto);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.Message.Should().Contain("suspended");
        }

        // ----- RefreshAsync -----

        [Fact]
        public async Task RefreshAsync_ValidToken_RotatesTokenAndRevokesOld()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            var user = SeedUser(db, "refresh@student.com", "password123", isVerified: true);
            var rawToken = jwtHelper.GenerateRefreshToken();
            var oldRow = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
            };
            db.RefreshTokens.Add(oldRow);
            await db.SaveChangesAsync();

            var result = await sut.RefreshAsync(rawToken);

            (await db.RefreshTokens.FindAsync(oldRow.Id))!.RevokedAt.Should().NotBeNull();
            result.AccessToken.Should().NotBeNullOrEmpty();
            var newHash = jwtHelper.HashToken(result.RefreshToken);
            db.RefreshTokens.Any(rt => rt.TokenHash == newHash).Should().BeTrue();
        }

        [Fact]
        public async Task RefreshAsync_UnknownToken_ThrowsApiException()
        {
            var (_, sut, jwtHelper, _) = CreateSut();

            var act = async () => await sut.RefreshAsync(jwtHelper.GenerateRefreshToken());

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task RefreshAsync_RevokedToken_ThrowsApiException()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            var user = SeedUser(db, "revoked@student.com", "password123", isVerified: true);
            var rawToken = jwtHelper.GenerateRefreshToken();
            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                RevokedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var act = async () => await sut.RefreshAsync(rawToken);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task RefreshAsync_ExpiredToken_ThrowsApiException()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            var user = SeedUser(db, "expired@student.com", "password123", isVerified: true);
            var rawToken = jwtHelper.GenerateRefreshToken();
            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                ExpiresAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-8),
            });
            await db.SaveChangesAsync();

            var act = async () => await sut.RefreshAsync(rawToken);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(401);
        }

        // ----- LogoutAsync -----

        [Fact]
        public async Task LogoutAsync_ValidToken_RevokesToken()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            var user = SeedUser(db, "logout@student.com", "password123", isVerified: true);
            var rawToken = jwtHelper.GenerateRefreshToken();
            var row = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
            };
            db.RefreshTokens.Add(row);
            await db.SaveChangesAsync();

            await sut.LogoutAsync(rawToken);

            (await db.RefreshTokens.FindAsync(row.Id))!.RevokedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task LogoutAsync_UnknownToken_DoesNotThrow()
        {
            var (_, sut, jwtHelper, _) = CreateSut();

            var act = async () => await sut.LogoutAsync(jwtHelper.GenerateRefreshToken());

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task LogoutAsync_AlreadyRevokedToken_LeavesRevokedAtUnchanged()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            var user = SeedUser(db, "already@student.com", "password123", isVerified: true);
            var rawToken = jwtHelper.GenerateRefreshToken();
            var fixedRevokedAt = DateTime.UtcNow.AddDays(-1);
            var row = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                RevokedAt = fixedRevokedAt,
            };
            db.RefreshTokens.Add(row);
            await db.SaveChangesAsync();

            await sut.LogoutAsync(rawToken);

            (await db.RefreshTokens.FindAsync(row.Id))!.RevokedAt.Should().Be(fixedRevokedAt);
        }

        // ----- VerifyEmailAsync -----

        [Fact]
        public async Task VerifyEmailAsync_ValidToken_MarksUserVerifiedAndConsumesToken()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            var user = SeedUser(db, "toverify@student.com", "password123", isVerified: false);
            var rawToken = jwtHelper.GenerateRefreshToken();
            var tokenRow = new VerificationToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                Purpose = TokenPurpose.EmailVerification,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow,
            };
            db.VerificationTokens.Add(tokenRow);
            await db.SaveChangesAsync();

            await sut.VerifyEmailAsync(rawToken);

            (await db.Users.FindAsync(user.Id))!.IsEmailVerified.Should().BeTrue();
            (await db.VerificationTokens.FindAsync(tokenRow.Id))!.UsedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task VerifyEmailAsync_UnknownToken_ThrowsApiException()
        {
            var (_, sut, jwtHelper, _) = CreateSut();

            var act = async () => await sut.VerifyEmailAsync(jwtHelper.GenerateRefreshToken());

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task VerifyEmailAsync_AlreadyUsedToken_ThrowsApiException()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            var user = SeedUser(db, "used@student.com", "password123", isVerified: false);
            var rawToken = jwtHelper.GenerateRefreshToken();
            db.VerificationTokens.Add(new VerificationToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                Purpose = TokenPurpose.EmailVerification,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UsedAt = DateTime.UtcNow.AddMinutes(-30),
            });
            await db.SaveChangesAsync();

            var act = async () => await sut.VerifyEmailAsync(rawToken);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task VerifyEmailAsync_ExpiredToken_ThrowsApiException()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            var user = SeedUser(db, "expiredtoken@student.com", "password123", isVerified: false);
            var rawToken = jwtHelper.GenerateRefreshToken();
            db.VerificationTokens.Add(new VerificationToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                Purpose = TokenPurpose.EmailVerification,
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                CreatedAt = DateTime.UtcNow.AddHours(-25),
            });
            await db.SaveChangesAsync();

            var act = async () => await sut.VerifyEmailAsync(rawToken);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        // ----- ForgotPasswordAsync -----

        [Fact]
        public async Task ForgotPasswordAsync_ExistingPasswordUser_SendsResetEmailAndCreatesToken()
        {
            var (db, sut, _, emailMock) = CreateSut();
            var user = SeedUser(db, "hasPassword@student.com", "password123", isVerified: true);

            await sut.ForgotPasswordAsync(new ForgotPasswordDto { Email = user.Email });

            db.VerificationTokens.Should().ContainSingle(vt => vt.UserId == user.Id && vt.Purpose == TokenPurpose.PasswordReset);
            emailMock.Verify(x => x.SendPasswordResetEmailAsync(user.Email, user.Username, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_UnknownEmail_DoesNotThrowAndSendsNoEmail()
        {
            var (db, sut, _, emailMock) = CreateSut();

            var act = async () => await sut.ForgotPasswordAsync(new ForgotPasswordDto { Email = "nobody@student.com" });

            await act.Should().NotThrowAsync();
            db.VerificationTokens.Should().BeEmpty();
            emailMock.Verify(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ForgotPasswordAsync_GoogleOnlyAccount_DoesNotThrowAndSendsNoEmail()
        {
            var (db, sut, _, emailMock) = CreateSut();
            var user = SeedUser(db, "googleonly@student.com", rawPassword: null, isVerified: true, googleId: "google-123");

            var act = async () => await sut.ForgotPasswordAsync(new ForgotPasswordDto { Email = user.Email });

            await act.Should().NotThrowAsync();
            db.VerificationTokens.Should().BeEmpty();
            emailMock.Verify(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // ----- ResetPasswordAsync -----

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordAndConsumesToken()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            var user = SeedUser(db, "resetme@student.com", "oldpassword123", isVerified: true);
            var rawToken = jwtHelper.GenerateRefreshToken();
            var tokenRow = new VerificationToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                Purpose = TokenPurpose.PasswordReset,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow,
            };
            db.VerificationTokens.Add(tokenRow);
            await db.SaveChangesAsync();

            await sut.ResetPasswordAsync(new ResetPasswordDto { Token = rawToken, NewPassword = "newpassword123" });

            var updatedUser = (await db.Users.FindAsync(user.Id))!;
            new PasswordHasher<User>().VerifyHashedPassword(updatedUser, updatedUser.PasswordHash!, "newpassword123")
                .Should().Be(PasswordVerificationResult.Success);
            (await db.VerificationTokens.FindAsync(tokenRow.Id))!.UsedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task ResetPasswordAsync_UnknownToken_ThrowsApiException()
        {
            var (_, sut, jwtHelper, _) = CreateSut();

            var act = async () => await sut.ResetPasswordAsync(new ResetPasswordDto { Token = jwtHelper.GenerateRefreshToken(), NewPassword = "newpassword123" });

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ResetPasswordAsync_AlreadyUsedToken_ThrowsApiException()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            var user = SeedUser(db, "usedreset@student.com", "password123", isVerified: true);
            var rawToken = jwtHelper.GenerateRefreshToken();
            db.VerificationTokens.Add(new VerificationToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                Purpose = TokenPurpose.PasswordReset,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                UsedAt = DateTime.UtcNow.AddMinutes(-10),
            });
            await db.SaveChangesAsync();

            var act = async () => await sut.ResetPasswordAsync(new ResetPasswordDto { Token = rawToken, NewPassword = "newpassword123" });

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ResetPasswordAsync_ExpiredToken_ThrowsApiException()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            var user = SeedUser(db, "expiredreset@student.com", "password123", isVerified: true);
            var rawToken = jwtHelper.GenerateRefreshToken();
            db.VerificationTokens.Add(new VerificationToken
            {
                UserId = user.Id,
                TokenHash = jwtHelper.HashToken(rawToken),
                Purpose = TokenPurpose.PasswordReset,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
            });
            await db.SaveChangesAsync();

            var act = async () => await sut.ResetPasswordAsync(new ResetPasswordDto { Token = rawToken, NewPassword = "newpassword123" });

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ResetPasswordAsync_VerificationTokenWrongPurpose_ThrowsApiException()
        {
            var (db, sut, jwtHelper, _) = CreateSut();
            var user = SeedUser(db, "wrongpurpose@student.com", "password123", isVerified: false);
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

            var act = async () => await sut.ResetPasswordAsync(new ResetPasswordDto { Token = rawToken, NewPassword = "newpassword123" });

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }
    }
}
