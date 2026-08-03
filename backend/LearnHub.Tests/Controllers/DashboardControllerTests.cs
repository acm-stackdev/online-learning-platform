using FluentAssertions;
using LearnHub.Controllers;
using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Tests.Controllers
{
    // [Authorize(Roles = "Instructor")] on GetInstructorDashboard is enforced by ASP.NET Core's
    // authorization middleware, which never runs when a test constructs DashboardController
    // directly - same documented limitation as CoursesControllerTests.
    public class DashboardControllerTests
    {
        private static (AppDbContext Db, DashboardController Controller) CreateSut(long userId, string role)
        {
            var db = TestDbContextFactory.Create();
            var dashboardService = new DashboardService(db, new EnrollmentService(db), new InstructorApplicationService(db));
            var controller = new DashboardController(dashboardService)
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(userId, role: role))
            };
            return (db, controller);
        }

        private static User SeedUser(AppDbContext db, string email, Role role = Role.Student)
        {
            var user = new User
            {
                Username = email.Split('@')[0],
                Email = email,
                Role = role,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        // ----- GET /api/dashboard/student -----

        [Fact]
        public async Task GetStudentDashboard_ReturnsOk()
        {
            var (db, controller) = CreateSut(1, "Student");
            SeedUser(db, "student@learnhub.com");

            var result = await controller.GetStudentDashboard();

            result.Should().BeOfType<OkObjectResult>();
        }

        // ----- GET /api/dashboard/instructor -----

        [Fact]
        public async Task GetInstructorDashboard_ReturnsOk()
        {
            var (db, controller) = CreateSut(1, "Instructor");
            SeedUser(db, "instructor@learnhub.com", Role.Instructor);

            var result = await controller.GetInstructorDashboard();

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
