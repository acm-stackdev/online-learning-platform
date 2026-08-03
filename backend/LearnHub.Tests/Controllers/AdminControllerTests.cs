using FluentAssertions;
using LearnHub.Controllers;
using LearnHub.Data;
using LearnHub.Models.DTOs.Admin;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Tests.Controllers
{
    // [Authorize(Roles = "Admin")] is enforced by ASP.NET Core's authorization middleware,
    // which never runs when a test constructs AdminController directly and calls an
    // action method. These tests only prove "given a principal with this role, the action
    // body behaves correctly" - same documented limitation as CoursesControllerTests.
    public class AdminControllerTests
    {
        private static User SeedUser(AppDbContext db, string email, Role role = Role.Student, bool isSuspended = false)
        {
            var user = new User
            {
                Username = email.Split('@')[0],
                Email = email,
                Role = role,
                IsEmailVerified = true,
                IsSuspended = isSuspended,
                CreatedAt = DateTime.UtcNow,
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private static (AppDbContext Db, AdminController Controller, User Admin) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);
            var controller = new AdminController(new AdminService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(admin.Id, role: "Admin"))
            };
            return (db, controller, admin);
        }

        // ----- GET /api/admin/users -----

        [Fact]
        public async Task GetUsers_ReturnsOkWithPagedResult()
        {
            var (db, controller, _) = CreateSut();
            SeedUser(db, "student@learnhub.com");

            var result = await controller.GetUsers();

            result.Should().BeOfType<OkObjectResult>();
        }

        // ----- PUT /api/admin/users/{id}/role -----

        [Fact]
        public async Task ChangeRole_ValidRole_ReturnsOk()
        {
            var (db, controller, _) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com");

            var result = await controller.ChangeRole(student.Id, new ChangeUserRoleDto { Role = Role.Instructor });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ChangeRole_Self_Returns400()
        {
            var (db, controller, admin) = CreateSut();

            var result = await controller.ChangeRole(admin.Id, new ChangeUserRoleDto { Role = Role.Student });

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(400);
        }

        // ----- POST /api/admin/users/{id}/suspend -----

        [Fact]
        public async Task Suspend_ActiveUser_ReturnsOk()
        {
            var (db, controller, _) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com");

            var result = await controller.Suspend(student.Id);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Suspend_AlreadySuspended_Returns400()
        {
            var (db, controller, _) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", isSuspended: true);

            var result = await controller.Suspend(student.Id);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(400);
        }

        // ----- POST /api/admin/users/{id}/reinstate -----

        [Fact]
        public async Task Reinstate_SuspendedUser_ReturnsOk()
        {
            var (db, controller, _) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", isSuspended: true);

            var result = await controller.Reinstate(student.Id);

            result.Should().BeOfType<OkObjectResult>();
        }

        // ----- GET /api/admin/stats -----

        [Fact]
        public async Task GetStats_ReturnsOk()
        {
            var (db, controller, _) = CreateSut();
            SeedUser(db, "student@learnhub.com");

            var result = await controller.GetStats();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeOfType<PlatformStatsDto>();
        }
    }
}
