using FluentAssertions;
using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Course;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;

namespace LearnHub.Tests.Services
{
    public class SectionServiceTests
    {
        private static (AppDbContext Db, SectionService Sut) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            return (db, new SectionService(db));
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

        private static Course SeedCourse(AppDbContext db, long instructorId)
        {
            var course = new Course
            {
                InstructorId = instructorId,
                Title = "Intro to Testing",
                Description = "Learn how to write unit tests",
                Status = CourseStatus.Draft,
                CreatedAt = DateTime.UtcNow,
            };
            db.Courses.Add(course);
            db.SaveChanges();
            return course;
        }

        // ----- CreateAsync -----

        [Fact]
        public async Task CreateAsync_Owner_CreatesSection()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id);
            var dto = new CreateSectionDto { CourseId = course.Id, Title = "Section 1" };

            var result = await sut.CreateAsync(dto, instructor.Id);

            result.Order.Should().Be(1);
            result.Title.Should().Be("Section 1");
        }

        [Fact]
        public async Task CreateAsync_SecondSection_IncrementsOrder()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id);
            await sut.CreateAsync(new CreateSectionDto { CourseId = course.Id, Title = "Section 1" }, instructor.Id);

            var result = await sut.CreateAsync(new CreateSectionDto { CourseId = course.Id, Title = "Section 2" }, instructor.Id);

            result.Order.Should().Be(2);
        }

        [Fact]
        public async Task CreateAsync_CourseNotFound_ThrowsApiException()
        {
            var (_, sut) = CreateSut();
            var dto = new CreateSectionDto { CourseId = 12345, Title = "Section 1" };

            var act = async () => await sut.CreateAsync(dto, 1);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreateAsync_NonOwner_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var course = SeedCourse(db, instructor.Id);
            var dto = new CreateSectionDto { CourseId = course.Id, Title = "Section 1" };

            var act = async () => await sut.CreateAsync(dto, otherInstructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        // ----- UpdateAsync -----

        [Fact]
        public async Task UpdateAsync_Owner_UpdatesTitle()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id);
            var created = await sut.CreateAsync(new CreateSectionDto { CourseId = course.Id, Title = "Section 1" }, instructor.Id);

            var result = await sut.UpdateAsync(created.Id, new UpdateSectionDto { Title = "Updated title" }, instructor.Id);

            result.Title.Should().Be("Updated title");
        }

        [Fact]
        public async Task UpdateAsync_NonOwner_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var course = SeedCourse(db, instructor.Id);
            var created = await sut.CreateAsync(new CreateSectionDto { CourseId = course.Id, Title = "Section 1" }, instructor.Id);

            var act = async () => await sut.UpdateAsync(created.Id, new UpdateSectionDto { Title = "Updated title" }, otherInstructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task UpdateAsync_UnknownId_ThrowsApiException()
        {
            var (_, sut) = CreateSut();

            var act = async () => await sut.UpdateAsync(12345, new UpdateSectionDto { Title = "x" }, 1);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        // ----- DeleteAsync -----

        [Fact]
        public async Task DeleteAsync_Owner_DeletesSection()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id);
            var created = await sut.CreateAsync(new CreateSectionDto { CourseId = course.Id, Title = "Section 1" }, instructor.Id);

            await sut.DeleteAsync(created.Id, instructor.Id);

            db.Set<Section>().Any(s => s.Id == created.Id).Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_NonOwner_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var course = SeedCourse(db, instructor.Id);
            var created = await sut.CreateAsync(new CreateSectionDto { CourseId = course.Id, Title = "Section 1" }, instructor.Id);

            var act = async () => await sut.DeleteAsync(created.Id, otherInstructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        // ----- ReorderAsync -----

        [Fact]
        public async Task ReorderAsync_ValidIds_UpdatesOrder()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id);
            var section1 = await sut.CreateAsync(new CreateSectionDto { CourseId = course.Id, Title = "Section 1" }, instructor.Id);
            var section2 = await sut.CreateAsync(new CreateSectionDto { CourseId = course.Id, Title = "Section 2" }, instructor.Id);

            await sut.ReorderAsync(new ReorderSectionsDto { CourseId = course.Id, OrderedSectionIds = new List<long> { section2.Id, section1.Id } }, instructor.Id);

            db.Set<Section>().First(s => s.Id == section2.Id).Order.Should().Be(1);
            db.Set<Section>().First(s => s.Id == section1.Id).Order.Should().Be(2);
        }

        [Fact]
        public async Task ReorderAsync_MismatchedIds_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var course = SeedCourse(db, instructor.Id);
            await sut.CreateAsync(new CreateSectionDto { CourseId = course.Id, Title = "Section 1" }, instructor.Id);

            var act = async () => await sut.ReorderAsync(new ReorderSectionsDto { CourseId = course.Id, OrderedSectionIds = new List<long> { 999 } }, instructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ReorderAsync_NonOwner_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var course = SeedCourse(db, instructor.Id);

            var act = async () => await sut.ReorderAsync(new ReorderSectionsDto { CourseId = course.Id, OrderedSectionIds = new List<long>() }, otherInstructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task ReorderAsync_CourseNotFound_ThrowsApiException()
        {
            var (_, sut) = CreateSut();

            var act = async () => await sut.ReorderAsync(new ReorderSectionsDto { CourseId = 12345, OrderedSectionIds = new List<long>() }, 1);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }
    }
}
