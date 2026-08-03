using FluentAssertions;
using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Course;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Tests.Services
{
    public class CourseServiceTests
    {
        private static (AppDbContext Db, CourseService Sut) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            return (db, new CourseService(db));
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

        private static Course SeedCourse(AppDbContext db, long instructorId, CourseStatus status, bool withContent = false)
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

            if (withContent)
            {
                var section = new Section { CourseId = course.Id, Title = "Section 1", Order = 1 };
                db.Set<Section>().Add(section);
                db.SaveChanges();

                db.Lessons.Add(new Lesson
                {
                    SectionId = section.Id,
                    Title = "Lesson 1",
                    ContentType = ContentType.Video,
                    ContentUrl = "https://example.com/video.mp4",
                    Duration = 10,
                    Order = 1,
                });
                db.SaveChanges();
            }

            return course;
        }

        // ----- CreateAsync -----

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesDraftCourse()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var dto = new CreateCourseDto { Title = "New Course", Description = "A brand new course description." };

            var result = await sut.CreateAsync(dto, instructor.Id);

            result.Status.Should().Be(CourseStatus.Draft);
            result.InstructorName.Should().Be(instructor.Username);
            db.Courses.Count().Should().Be(1);
        }

        // ----- GetCatalogueAsync -----

        [Fact]
        public async Task GetCatalogueAsync_OnlyReturnsPublishedCourses()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            SeedCourse(db, instructor.Id, CourseStatus.Draft);
            SeedCourse(db, instructor.Id, CourseStatus.PendingApproval);
            SeedCourse(db, instructor.Id, CourseStatus.Rejected);
            var published = SeedCourse(db, instructor.Id, CourseStatus.Published);

            var result = await sut.GetCatalogueAsync(1, 12, null, null);

            result.Items.Should().ContainSingle().Which.Id.Should().Be(published.Id);
        }

        [Fact]
        public async Task GetCatalogueAsync_FiltersBySearch()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var match = SeedCourse(db, instructor.Id, CourseStatus.Published);
            match.Title = "Advanced Testing Techniques";
            var noMatch = SeedCourse(db, instructor.Id, CourseStatus.Published);
            noMatch.Title = "Cooking Basics";
            await db.SaveChangesAsync();

            var result = await sut.GetCatalogueAsync(1, 12, "Testing", null);

            result.Items.Should().ContainSingle().Which.Id.Should().Be(match.Id);
        }

        [Fact]
        public async Task GetCatalogueAsync_FiltersByCategory()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var match = SeedCourse(db, instructor.Id, CourseStatus.Published);
            match.Category = "Programming";
            var noMatch = SeedCourse(db, instructor.Id, CourseStatus.Published);
            noMatch.Category = "Design";
            await db.SaveChangesAsync();

            var result = await sut.GetCatalogueAsync(1, 12, null, "Programming");

            result.Items.Should().ContainSingle().Which.Id.Should().Be(match.Id);
        }

        [Fact]
        public async Task GetCatalogueAsync_ClampsPageAndPageSize()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            SeedCourse(db, instructor.Id, CourseStatus.Published);

            var result = await sut.GetCatalogueAsync(page: 0, pageSize: 500, null, null);

            result.Page.Should().Be(1);
            result.PageSize.Should().Be(50);
        }

        [Fact]
        public async Task GetCatalogueAsync_PaginatesAndReportsTotalCount()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            for (var i = 0; i < 5; i++)
                SeedCourse(db, instructor.Id, CourseStatus.Published);

            var result = await sut.GetCatalogueAsync(page: 2, pageSize: 2, null, null);

            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(5);
        }

        // ----- GetDetailAsync -----

        [Fact]
        public async Task GetDetailAsync_PublishedCourse_VisibleToAnonymous()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published);

            var result = await sut.GetDetailAsync(course.Id, requestingUserId: null, isAdmin: false);

            result.Id.Should().Be(course.Id);
        }

        [Fact]
        public async Task GetDetailAsync_DraftCourse_VisibleToOwner()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            var result = await sut.GetDetailAsync(course.Id, requestingUserId: instructor.Id, isAdmin: false);

            result.Id.Should().Be(course.Id);
        }

        [Fact]
        public async Task GetDetailAsync_DraftCourse_NotVisibleToNonOwner_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            var act = async () => await sut.GetDetailAsync(course.Id, requestingUserId: otherInstructor.Id, isAdmin: false);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetDetailAsync_DraftCourse_VisibleToAdmin()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            var result = await sut.GetDetailAsync(course.Id, requestingUserId: 999, isAdmin: true);

            result.Id.Should().Be(course.Id);
        }

        [Fact]
        public async Task GetDetailAsync_UnknownId_ThrowsApiException()
        {
            var (_, sut) = CreateSut();

            var act = async () => await sut.GetDetailAsync(12345, requestingUserId: null, isAdmin: false);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetDetailAsync_NonEnrolledLoggedInUser_ContentUrlIsHidden()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published, withContent: true);
            var otherStudent = new User { Username = "student", Email = "student@learnhub.com", Role = Role.Student, IsEmailVerified = true, CreatedAt = DateTime.UtcNow };
            db.Users.Add(otherStudent);
            db.SaveChanges();

            var result = await sut.GetDetailAsync(course.Id, requestingUserId: otherStudent.Id, isAdmin: false);

            result.Sections.Single().Lessons.Single().ContentUrl.Should().BeNull();
            result.Sections.Single().Lessons.Single().Title.Should().Be("Lesson 1");
        }

        [Fact]
        public async Task GetDetailAsync_AnonymousViewer_ContentUrlIsHidden()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published, withContent: true);

            var result = await sut.GetDetailAsync(course.Id, requestingUserId: null, isAdmin: false);

            result.Sections.Single().Lessons.Single().ContentUrl.Should().BeNull();
        }

        [Fact]
        public async Task GetDetailAsync_EnrolledStudent_SeesContentUrl()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published, withContent: true);
            var student = new User { Username = "student", Email = "student@learnhub.com", Role = Role.Student, IsEmailVerified = true, CreatedAt = DateTime.UtcNow };
            db.Users.Add(student);
            db.SaveChanges();
            db.Enrollments.Add(new Enrollment { StudentId = student.Id, CourseId = course.Id, EnrolledAt = DateTime.UtcNow });
            db.SaveChanges();

            var result = await sut.GetDetailAsync(course.Id, requestingUserId: student.Id, isAdmin: false);

            result.Sections.Single().Lessons.Single().ContentUrl.Should().Be("https://example.com/video.mp4");
        }

        [Fact]
        public async Task GetDetailAsync_OwningInstructor_SeesContentUrl()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published, withContent: true);

            var result = await sut.GetDetailAsync(course.Id, requestingUserId: instructor.Id, isAdmin: false);

            result.Sections.Single().Lessons.Single().ContentUrl.Should().Be("https://example.com/video.mp4");
        }

        [Fact]
        public async Task GetDetailAsync_Admin_SeesContentUrl()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published, withContent: true);

            var result = await sut.GetDetailAsync(course.Id, requestingUserId: 999, isAdmin: true);

            result.Sections.Single().Lessons.Single().ContentUrl.Should().Be("https://example.com/video.mp4");
        }

        // ----- UpdateAsync -----

        [Fact]
        public async Task UpdateAsync_Owner_UpdatesFields()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);
            var dto = new UpdateCourseDto { Title = "Updated Title", Description = "Updated description text." };

            var result = await sut.UpdateAsync(course.Id, dto, instructor.Id);

            result.Title.Should().Be("Updated Title");
        }

        [Fact]
        public async Task UpdateAsync_NonOwner_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);
            var dto = new UpdateCourseDto { Title = "Updated Title", Description = "Updated description text." };

            var act = async () => await sut.UpdateAsync(course.Id, dto, otherInstructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        // ----- DeleteAsync -----

        [Fact]
        public async Task DeleteAsync_DraftCourse_Owner_Deletes()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            await sut.DeleteAsync(course.Id, instructor.Id);

            db.Courses.Any(c => c.Id == course.Id).Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_PublishedCourse_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published);

            var act = async () => await sut.DeleteAsync(course.Id, instructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task DeleteAsync_NonOwner_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            var act = async () => await sut.DeleteAsync(course.Id, otherInstructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        // ----- SubmitForReviewAsync -----

        [Fact]
        public async Task SubmitForReviewAsync_DraftWithContent_SetsPendingApproval()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft, withContent: true);

            var result = await sut.SubmitForReviewAsync(course.Id, instructor.Id);

            result.Status.Should().Be(CourseStatus.PendingApproval);
        }

        [Fact]
        public async Task SubmitForReviewAsync_RejectedWithContent_SetsPendingApproval()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Rejected, withContent: true);

            var result = await sut.SubmitForReviewAsync(course.Id, instructor.Id);

            result.Status.Should().Be(CourseStatus.PendingApproval);
        }

        [Fact]
        public async Task SubmitForReviewAsync_NoContent_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft, withContent: false);

            var act = async () => await sut.SubmitForReviewAsync(course.Id, instructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task SubmitForReviewAsync_AlreadyPublished_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published, withContent: true);

            var act = async () => await sut.SubmitForReviewAsync(course.Id, instructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task SubmitForReviewAsync_AlreadyPending_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.PendingApproval, withContent: true);

            var act = async () => await sut.SubmitForReviewAsync(course.Id, instructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task SubmitForReviewAsync_NonOwner_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft, withContent: true);

            var act = async () => await sut.SubmitForReviewAsync(course.Id, otherInstructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        // ----- UnpublishAsync -----

        [Fact]
        public async Task UnpublishAsync_PublishedCourse_SetsDraft()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published);

            var result = await sut.UnpublishAsync(course.Id, instructor.Id);

            result.Status.Should().Be(CourseStatus.Draft);
        }

        [Fact]
        public async Task UnpublishAsync_NotPublished_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            var act = async () => await sut.UnpublishAsync(course.Id, instructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task UnpublishAsync_NonOwner_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published);

            var act = async () => await sut.UnpublishAsync(course.Id, otherInstructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        // ----- ApproveAsync -----

        [Fact]
        public async Task ApproveAsync_PendingCourse_SetsPublished()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.PendingApproval);

            var result = await sut.ApproveAsync(course.Id);

            result.Status.Should().Be(CourseStatus.Published);
        }

        [Fact]
        public async Task ApproveAsync_NotPending_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            var act = async () => await sut.ApproveAsync(course.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ApproveAsync_UnknownId_ThrowsApiException()
        {
            var (_, sut) = CreateSut();

            var act = async () => await sut.ApproveAsync(12345);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        // ----- RejectAsync -----

        [Fact]
        public async Task RejectAsync_PendingCourse_SetsRejected()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.PendingApproval);

            var result = await sut.RejectAsync(course.Id);

            result.Status.Should().Be(CourseStatus.Rejected);
        }

        [Fact]
        public async Task RejectAsync_NotPending_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            var act = async () => await sut.RejectAsync(course.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task RejectAsync_UnknownId_ThrowsApiException()
        {
            var (_, sut) = CreateSut();

            var act = async () => await sut.RejectAsync(12345);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        // ----- ForceUnpublishAsync -----

        [Fact]
        public async Task ForceUnpublishAsync_PublishedCourse_SetsDraft()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Published);

            var result = await sut.ForceUnpublishAsync(course.Id);

            result.Status.Should().Be(CourseStatus.Draft);
        }

        [Fact]
        public async Task ForceUnpublishAsync_NotPublished_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            var act = async () => await sut.ForceUnpublishAsync(course.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ForceUnpublishAsync_UnknownId_ThrowsApiException()
        {
            var (_, sut) = CreateSut();

            var act = async () => await sut.ForceUnpublishAsync(12345);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        // ----- GetPendingApprovalAsync -----

        [Fact]
        public async Task GetPendingApprovalAsync_OnlyReturnsPendingCourses()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            SeedCourse(db, instructor.Id, CourseStatus.Draft);
            SeedCourse(db, instructor.Id, CourseStatus.Published);
            var pending = SeedCourse(db, instructor.Id, CourseStatus.PendingApproval);

            var result = await sut.GetPendingApprovalAsync(1, 12);

            result.Items.Should().ContainSingle().Which.Id.Should().Be(pending.Id);
        }

        [Fact]
        public async Task GetPendingApprovalAsync_OrdersOldestSubmissionFirst()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var older = SeedCourse(db, instructor.Id, CourseStatus.PendingApproval);
            older.CreatedAt = DateTime.UtcNow.AddDays(-2);
            var newer = SeedCourse(db, instructor.Id, CourseStatus.PendingApproval);
            newer.CreatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var result = await sut.GetPendingApprovalAsync(1, 12);

            result.Items.Select(i => i.Id).Should().ContainInOrder(older.Id, newer.Id);
        }
    }
}
