using FluentAssertions;
using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Course;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.AspNetCore.Http;
using Moq;

namespace LearnHub.Tests.Services
{
    public class LessonServiceTests
    {
        private static (AppDbContext Db, LessonService Sut, Mock<IFileUploadService> FileUploadMock) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            var fileUploadMock = new Mock<IFileUploadService>();
            fileUploadMock
                .Setup(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<ContentType>()))
                .ReturnsAsync("https://cloudinary.example.com/file.mp4");
            fileUploadMock
                .Setup(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<ContentType>()))
                .Returns(Task.CompletedTask);
            var sut = new LessonService(db, fileUploadMock.Object);
            return (db, sut, fileUploadMock);
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

        private static Section SeedCourseWithSection(AppDbContext db, long instructorId)
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

            var section = new Section { CourseId = course.Id, Title = "Section 1", Order = 1 };
            db.Set<Section>().Add(section);
            db.SaveChanges();

            return section;
        }

        private static IFormFile BuildFormFile(string fileName, long length = 1024)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.Length).Returns(length);
            return file.Object;
        }

        // ----- CreateAsync -----

        [Fact]
        public async Task CreateAsync_ValidVideoFile_CreatesLesson()
        {
            var (db, sut, fileUploadMock) = CreateSut();
            var instructor = SeedInstructor(db);
            var section = SeedCourseWithSection(db, instructor.Id);
            var dto = new CreateLessonDto
            {
                SectionId = section.Id,
                Title = "Lesson 1",
                ContentType = ContentType.Video,
                Duration = 120,
                File = BuildFormFile("video.mp4"),
            };

            var result = await sut.CreateAsync(dto, instructor.Id);

            result.ContentUrl.Should().Be("https://cloudinary.example.com/file.mp4");
            result.Order.Should().Be(1);
            fileUploadMock.Verify(f => f.UploadAsync(It.IsAny<IFormFile>(), ContentType.Video), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_SectionNotFound_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var dto = new CreateLessonDto
            {
                SectionId = 12345,
                Title = "Lesson 1",
                ContentType = ContentType.Video,
                Duration = 120,
                File = BuildFormFile("video.mp4"),
            };

            var act = async () => await sut.CreateAsync(dto, instructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreateAsync_NonOwner_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var section = SeedCourseWithSection(db, instructor.Id);
            var dto = new CreateLessonDto
            {
                SectionId = section.Id,
                Title = "Lesson 1",
                ContentType = ContentType.Video,
                Duration = 120,
                File = BuildFormFile("video.mp4"),
            };

            var act = async () => await sut.CreateAsync(dto, otherInstructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task CreateAsync_WrongExtensionForContentType_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var section = SeedCourseWithSection(db, instructor.Id);
            var dto = new CreateLessonDto
            {
                SectionId = section.Id,
                Title = "Lesson 1",
                ContentType = ContentType.Video,
                Duration = 120,
                File = BuildFormFile("notes.pdf"),
            };

            var act = async () => await sut.CreateAsync(dto, instructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreateAsync_FileTooLarge_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var section = SeedCourseWithSection(db, instructor.Id);
            var dto = new CreateLessonDto
            {
                SectionId = section.Id,
                Title = "Lesson 1",
                ContentType = ContentType.Pdf,
                Duration = 120,
                File = BuildFormFile("notes.pdf", 21L * 1024 * 1024),
            };

            var act = async () => await sut.CreateAsync(dto, instructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreateAsync_EmptyFile_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var section = SeedCourseWithSection(db, instructor.Id);
            var dto = new CreateLessonDto
            {
                SectionId = section.Id,
                Title = "Lesson 1",
                ContentType = ContentType.Video,
                Duration = 120,
                File = BuildFormFile("video.mp4", 0),
            };

            var act = async () => await sut.CreateAsync(dto, instructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreateAsync_SecondLesson_IncrementsOrder()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var section = SeedCourseWithSection(db, instructor.Id);
            await sut.CreateAsync(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") }, instructor.Id);

            var result = await sut.CreateAsync(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 2", ContentType = ContentType.Video, Duration = 90, File = BuildFormFile("video2.mp4") }, instructor.Id);

            result.Order.Should().Be(2);
        }

        // ----- UpdateAsync -----

        [Fact]
        public async Task UpdateAsync_Owner_UpdatesFields()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var section = SeedCourseWithSection(db, instructor.Id);
            var created = await sut.CreateAsync(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") }, instructor.Id);

            var result = await sut.UpdateAsync(created.Id, new UpdateLessonDto { Title = "Updated title", Duration = 200 }, instructor.Id);

            result.Title.Should().Be("Updated title");
            result.Duration.Should().Be(200);
        }

        [Fact]
        public async Task UpdateAsync_NonOwner_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var section = SeedCourseWithSection(db, instructor.Id);
            var created = await sut.CreateAsync(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") }, instructor.Id);

            var act = async () => await sut.UpdateAsync(created.Id, new UpdateLessonDto { Title = "Updated title", Duration = 200 }, otherInstructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task UpdateAsync_UnknownId_ThrowsApiException()
        {
            var (_, sut, _) = CreateSut();

            var act = async () => await sut.UpdateAsync(12345, new UpdateLessonDto { Title = "x", Duration = 1 }, 1);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateAsync_NewFileProvided_DeletesOldContentUrl()
        {
            var (db, sut, fileUploadMock) = CreateSut();
            var instructor = SeedInstructor(db);
            var section = SeedCourseWithSection(db, instructor.Id);
            var created = await sut.CreateAsync(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") }, instructor.Id);

            await sut.UpdateAsync(created.Id, new UpdateLessonDto { Title = "Lesson 1", Duration = 120, File = BuildFormFile("video2.mp4") }, instructor.Id);

            fileUploadMock.Verify(f => f.DeleteAsync("https://cloudinary.example.com/file.mp4", ContentType.Video), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NoFileProvided_DoesNotDeleteContentUrl()
        {
            var (db, sut, fileUploadMock) = CreateSut();
            var instructor = SeedInstructor(db);
            var section = SeedCourseWithSection(db, instructor.Id);
            var created = await sut.CreateAsync(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") }, instructor.Id);

            await sut.UpdateAsync(created.Id, new UpdateLessonDto { Title = "Updated title", Duration = 200 }, instructor.Id);

            fileUploadMock.Verify(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<ContentType>()), Times.Never);
        }

        // ----- DeleteAsync -----

        [Fact]
        public async Task DeleteAsync_Owner_DeletesLesson()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var section = SeedCourseWithSection(db, instructor.Id);
            var created = await sut.CreateAsync(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") }, instructor.Id);

            await sut.DeleteAsync(created.Id, instructor.Id);

            db.Lessons.Any(l => l.Id == created.Id).Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_Owner_DeletesCloudinaryFile()
        {
            var (db, sut, fileUploadMock) = CreateSut();
            var instructor = SeedInstructor(db);
            var section = SeedCourseWithSection(db, instructor.Id);
            var created = await sut.CreateAsync(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") }, instructor.Id);

            await sut.DeleteAsync(created.Id, instructor.Id);

            fileUploadMock.Verify(f => f.DeleteAsync("https://cloudinary.example.com/file.mp4", ContentType.Video), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonOwner_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var section = SeedCourseWithSection(db, instructor.Id);
            var created = await sut.CreateAsync(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") }, instructor.Id);

            var act = async () => await sut.DeleteAsync(created.Id, otherInstructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        // ----- ReorderAsync -----

        [Fact]
        public async Task ReorderAsync_ValidIds_UpdatesOrder()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var section = SeedCourseWithSection(db, instructor.Id);
            var lesson1 = await sut.CreateAsync(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") }, instructor.Id);
            var lesson2 = await sut.CreateAsync(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 2", ContentType = ContentType.Video, Duration = 90, File = BuildFormFile("video2.mp4") }, instructor.Id);

            await sut.ReorderAsync(new ReorderLessonsDto { SectionId = section.Id, OrderedLessonIds = new List<long> { lesson2.Id, lesson1.Id } }, instructor.Id);

            db.Lessons.First(l => l.Id == lesson2.Id).Order.Should().Be(1);
            db.Lessons.First(l => l.Id == lesson1.Id).Order.Should().Be(2);
        }

        [Fact]
        public async Task ReorderAsync_MismatchedIds_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var section = SeedCourseWithSection(db, instructor.Id);
            await sut.CreateAsync(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") }, instructor.Id);

            var act = async () => await sut.ReorderAsync(new ReorderLessonsDto { SectionId = section.Id, OrderedLessonIds = new List<long> { 999 } }, instructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ReorderAsync_NonOwner_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedInstructor(db);
            var otherInstructor = SeedInstructor(db, "other@learnhub.com");
            var section = SeedCourseWithSection(db, instructor.Id);

            var act = async () => await sut.ReorderAsync(new ReorderLessonsDto { SectionId = section.Id, OrderedLessonIds = new List<long>() }, otherInstructor.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }
    }
}
