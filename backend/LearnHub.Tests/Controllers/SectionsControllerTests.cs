using FluentAssertions;
using LearnHub.Controllers;
using LearnHub.Data;
using LearnHub.Models.DTOs.Course;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Tests.Controllers
{
    // [Authorize(Roles = "Instructor")] is enforced by ASP.NET Core's authorization
    // middleware, which never runs when a test constructs SectionsController directly -
    // same documented limitation as CoursesControllerTests.
    public class SectionsControllerTests
    {
        private static (AppDbContext Db, SectionsController Controller) CreateSut(System.Security.Claims.ClaimsPrincipal? user = null)
        {
            var db = TestDbContextFactory.Create();
            var controller = new SectionsController(new SectionService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(user)
            };
            return (db, controller);
        }

        private static User SeedInstructor(AppDbContext db, string email = "instructor@learnhub.com")
        {
            var instructor = new User { Username = email.Split('@')[0], Email = email, Role = Role.Instructor, IsEmailVerified = true, CreatedAt = DateTime.UtcNow };
            db.Users.Add(instructor);
            db.SaveChanges();
            return instructor;
        }

        private static Course SeedCourse(AppDbContext db, long instructorId)
        {
            var course = new Course { InstructorId = instructorId, Title = "Intro to Testing", Description = "Learn how to write unit tests", Status = CourseStatus.Draft, CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            db.SaveChanges();
            return course;
        }

        // ----- POST /api/sections -----

        [Fact]
        public async Task Create_Owner_ReturnsOk()
        {
            var db = TestDbContextFactory.Create();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id);
            var controller = new SectionsController(new SectionService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"))
            };

            var result = await controller.Create(new CreateSectionDto { CourseId = course.Id, Title = "Section 1" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Create_NonOwner_Returns403()
        {
            var db = TestDbContextFactory.Create();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var course = SeedCourse(db, instructor.Id);
            var controller = new SectionsController(new SectionService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(otherInstructor.Id, role: "Instructor"))
            };

            var result = await controller.Create(new CreateSectionDto { CourseId = course.Id, Title = "Section 1" });

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(403);
        }

        // ----- DELETE /api/sections/{id} -----

        [Fact]
        public async Task Delete_Owner_ReturnsNoContent()
        {
            var db = TestDbContextFactory.Create();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id);
            var controller = new SectionsController(new SectionService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"))
            };
            var created = (SectionSummaryDto)((OkObjectResult)await controller.Create(new CreateSectionDto { CourseId = course.Id, Title = "Section 1" })).Value!;

            var result = await controller.Delete(created.Id);

            result.Should().BeOfType<NoContentResult>();
        }

        // ----- PUT /api/sections/reorder -----

        [Fact]
        public async Task Reorder_MismatchedIds_Returns400()
        {
            var db = TestDbContextFactory.Create();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id);
            var controller = new SectionsController(new SectionService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"))
            };
            await controller.Create(new CreateSectionDto { CourseId = course.Id, Title = "Section 1" });

            var result = await controller.Reorder(new ReorderSectionsDto { CourseId = course.Id, OrderedSectionIds = new List<long> { 999 } });

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(400);
        }
    }
}
