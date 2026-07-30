using FluentAssertions;
using LearnHub.Controllers;
using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Course;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Tests.Controllers
{
    // [Authorize(Roles = "...")] is enforced by ASP.NET Core's authorization middleware,
    // which never runs when a test constructs CoursesController directly and calls an
    // action method. These tests only prove "given a principal with this role, the action
    // body behaves correctly" - they do NOT prove the wrong role is actually rejected
    // before reaching the method. That needs a WebApplicationFactory integration test,
    // same documented limitation as AuthControllerTests.Me().
    public class CoursesControllerTests
    {
        private static (AppDbContext Db, CoursesController Controller) CreateSut(System.Security.Claims.ClaimsPrincipal? user = null)
        {
            var db = TestDbContextFactory.Create();
            var controller = new CoursesController(new CourseService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(user)
            };
            return (db, controller);
        }

        private static User SeedInstructor(AppDbContext db, string email = "instructor@learnhub.com")
        {
            var instructor = new User
            {
                Username = email.Split('@')[0],
                Email = email,
                Role = Role.Instructor,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
            };
            db.Users.Add(instructor);
            db.SaveChanges();
            return instructor;
        }

        private static Course SeedCourse(AppDbContext db, long instructorId, CourseStatus status)
        {
            var course = new Course
            {
                InstructorId = instructorId,
                Title = "Intro to Testing",
                Description = "Learn how to write unit tests",
                Status = status,
                CreatedAt = DateTime.UtcNow,
            };
            db.Courses.Add(course);
            db.SaveChanges();
            return course;
        }

        // ----- GET /api/courses -----

        [Fact]
        public async Task GetCatalogue_ReturnsOkWithPagedResult()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedInstructor(db);
            SeedCourse(db, instructor.Id, CourseStatus.Published);

            var result = await controller.GetCatalogue();

            result.Should().BeOfType<OkObjectResult>();
        }

        // ----- GET /api/courses/pending -----

        [Fact]
        public async Task GetPending_ReturnsOkWithPendingCourses()
        {
            var (db, controller) = CreateSut(ControllerTestHelpers.BuildUserPrincipal(1, role: "Admin"));
            var instructor = SeedInstructor(db);
            SeedCourse(db, instructor.Id, CourseStatus.PendingApproval);

            var result = await controller.GetPending();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeOfType<Models.DTOs.Common.PagedResult<CourseListItemDto>>()
                .Which.Items.Should().HaveCount(1);
        }

        // ----- GET /api/courses/{id} -----

        [Fact]
        public async Task GetDetail_AnonymousUser_PublishedCourse_ReturnsOk()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published);

            var result = await controller.GetDetail(course.Id);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetDetail_AnonymousUser_DraftCourse_Returns404()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            var result = await controller.GetDetail(course.Id);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetDetail_Owner_DraftCourse_ReturnsOk()
        {
            var db = TestDbContextFactory.Create();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);
            var controller = new CoursesController(new CourseService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"))
            };

            var result = await controller.GetDetail(course.Id);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetDetail_Admin_DraftCourse_ReturnsOk()
        {
            var (db, controller) = CreateSut(ControllerTestHelpers.BuildUserPrincipal(999, role: "Admin"));
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            var result = await controller.GetDetail(course.Id);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetDetail_UnknownId_Returns404()
        {
            var (_, controller) = CreateSut();

            var result = await controller.GetDetail(12345);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(404);
        }

        // ----- POST /api/courses -----

        [Fact]
        public async Task Create_ValidInput_ReturnsCreatedAtActionWithDraftCourse()
        {
            var db = TestDbContextFactory.Create();
            var instructor = SeedInstructor(db);
            var controller = new CoursesController(new CourseService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"))
            };
            var dto = new CreateCourseDto { Title = "New Course", Description = "A brand new course description." };

            var result = await controller.Create(dto);

            var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.Value.Should().BeOfType<CourseListItemDto>().Which.Status.Should().Be(CourseStatus.Draft);
        }

        // ----- PUT /api/courses/{id} -----

        [Fact]
        public async Task Update_Owner_ReturnsOk()
        {
            var db = TestDbContextFactory.Create();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);
            var controller = new CoursesController(new CourseService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"))
            };
            var dto = new UpdateCourseDto { Title = "Updated Title", Description = "Updated description text." };

            var result = await controller.Update(course.Id, dto);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Update_NonOwner_Returns403()
        {
            var db = TestDbContextFactory.Create();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);
            var controller = new CoursesController(new CourseService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(otherInstructor.Id, role: "Instructor"))
            };
            var dto = new UpdateCourseDto { Title = "Updated Title", Description = "Updated description text." };

            var result = await controller.Update(course.Id, dto);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(403);
        }

        // ----- DELETE /api/courses/{id} -----

        [Fact]
        public async Task Delete_DraftCourse_Owner_ReturnsNoContent()
        {
            var db = TestDbContextFactory.Create();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);
            var controller = new CoursesController(new CourseService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"))
            };

            var result = await controller.Delete(course.Id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_PublishedCourse_Returns400()
        {
            var db = TestDbContextFactory.Create();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published);
            var controller = new CoursesController(new CourseService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"))
            };

            var result = await controller.Delete(course.Id);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(400);
        }

        // ----- POST /api/courses/{id}/submit-for-review -----

        [Fact]
        public async Task SubmitForReview_NoContent_Returns400()
        {
            var db = TestDbContextFactory.Create();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);
            var controller = new CoursesController(new CourseService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"))
            };

            var result = await controller.SubmitForReview(course.Id);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(400);
        }

        // ----- PUT /api/courses/{id}/unpublish -----

        [Fact]
        public async Task Unpublish_Owner_ReturnsOk()
        {
            var db = TestDbContextFactory.Create();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published);
            var controller = new CoursesController(new CourseService(db))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"))
            };

            var result = await controller.Unpublish(course.Id);

            result.Should().BeOfType<OkObjectResult>();
        }

        // ----- POST /api/courses/{id}/approve -----

        [Fact]
        public async Task Approve_PendingCourse_ReturnsOk()
        {
            var (db, controller) = CreateSut(ControllerTestHelpers.BuildUserPrincipal(1, role: "Admin"));
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.PendingApproval);

            var result = await controller.Approve(course.Id);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Approve_NotPending_Returns400()
        {
            var (db, controller) = CreateSut(ControllerTestHelpers.BuildUserPrincipal(1, role: "Admin"));
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            var result = await controller.Approve(course.Id);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(400);
        }

        // ----- POST /api/courses/{id}/reject -----

        [Fact]
        public async Task Reject_PendingCourse_ReturnsOk()
        {
            var (db, controller) = CreateSut(ControllerTestHelpers.BuildUserPrincipal(1, role: "Admin"));
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.PendingApproval);

            var result = await controller.Reject(course.Id);

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
