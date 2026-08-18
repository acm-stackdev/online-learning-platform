using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Course;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services
{
    public class LessonService
    {
        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mov", ".webm" };
        private static readonly HashSet<string> PdfExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf" };
        private const long MaxVideoBytes = 500L * 1024 * 1024;
        private const long MaxPdfBytes = 20L * 1024 * 1024;

        private readonly AppDbContext _db;
        private readonly IFileUploadService _fileUploadService;

        public LessonService(AppDbContext db, IFileUploadService fileUploadService)
        {
            _db = db;
            _fileUploadService = fileUploadService;
        }

        public async Task<LessonSummaryDto> CreateAsync(CreateLessonDto dto, long instructorId)
        {
            var section = await _db.Set<Section>().Include(s => s.Course).FirstOrDefaultAsync(s => s.Id == dto.SectionId);
            if (section is null)
                throw new ApiException("Section not found.", 404);

            if (section.Course.InstructorId != instructorId)
                throw new ApiException("You do not own this course.", 403);

            ValidateFile(dto.File, dto.ContentType);

            var contentUrl = await _fileUploadService.UploadAsync(dto.File, dto.ContentType);

            var maxOrder = await _db.Lessons
                .Where(l => l.SectionId == dto.SectionId)
                .Select(l => (int?)l.Order)
                .MaxAsync() ?? 0;

            var lesson = new Lesson
            {
                SectionId = dto.SectionId,
                Title = dto.Title,
                ContentType = dto.ContentType,
                ContentUrl = contentUrl,
                Duration = dto.Duration,
                Order = maxOrder + 1,
            };

            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();

            return MapLesson(lesson);
        }

        public async Task<LessonSummaryDto> UpdateAsync(long id, UpdateLessonDto dto, long instructorId)
        {
            var lesson = await GetOwnedLessonAsync(id, instructorId);

            lesson.Title = dto.Title;
            lesson.Duration = dto.Duration;

            string? oldContentUrl = null;
            if (dto.File is not null)
            {
                ValidateFile(dto.File, lesson.ContentType);
                oldContentUrl = lesson.ContentUrl;
                lesson.ContentUrl = await _fileUploadService.UploadAsync(dto.File, lesson.ContentType);
            }

            await _db.SaveChangesAsync();

            if (oldContentUrl is not null)
                await _fileUploadService.DeleteAsync(oldContentUrl, lesson.ContentType);

            return MapLesson(lesson);
        }

        public async Task DeleteAsync(long id, long instructorId)
        {
            var lesson = await GetOwnedLessonAsync(id, instructorId);

            _db.Lessons.Remove(lesson);
            await _db.SaveChangesAsync();

            await _fileUploadService.DeleteAsync(lesson.ContentUrl, lesson.ContentType);
        }

        public async Task ReorderAsync(ReorderLessonsDto dto, long instructorId)
        {
            var section = await _db.Set<Section>().Include(s => s.Course).FirstOrDefaultAsync(s => s.Id == dto.SectionId);
            if (section is null)
                throw new ApiException("Section not found.", 404);

            if (section.Course.InstructorId != instructorId)
                throw new ApiException("You do not own this course.", 403);

            var lessons = await _db.Lessons.Where(l => l.SectionId == dto.SectionId).ToListAsync();

            var existingIds = lessons.Select(l => l.Id).ToHashSet();
            if (!existingIds.SetEquals(dto.OrderedLessonIds))
                throw new ApiException("Provided lesson ids do not match this section's lessons.", 400);

            for (var i = 0; i < dto.OrderedLessonIds.Count; i++)
            {
                var lesson = lessons.First(l => l.Id == dto.OrderedLessonIds[i]);
                lesson.Order = i + 1;
            }

            await _db.SaveChangesAsync();
        }

        private static void ValidateFile(Microsoft.AspNetCore.Http.IFormFile file, ContentType contentType)
        {
            if (file is null || file.Length == 0)
                throw new ApiException("A lesson file is required.", 400);

            var extension = Path.GetExtension(file.FileName);
            var allowedExtensions = contentType == ContentType.Video ? VideoExtensions : PdfExtensions;
            if (!allowedExtensions.Contains(extension))
                throw new ApiException("File does not match the declared content type.", 400);

            var maxBytes = contentType == ContentType.Video ? MaxVideoBytes : MaxPdfBytes;
            if (file.Length > maxBytes)
                throw new ApiException("File exceeds maximum allowed size.", 400);
        }

        private async Task<Lesson> GetOwnedLessonAsync(long id, long instructorId)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Section)
                    .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson is null)
                throw new ApiException("Lesson not found.", 404);

            if (lesson.Section.Course.InstructorId != instructorId)
                throw new ApiException("You do not own this course.", 403);

            return lesson;
        }

        private static LessonSummaryDto MapLesson(Lesson lesson) => new()
        {
            Id = lesson.Id,
            Title = lesson.Title,
            ContentType = lesson.ContentType,
            ContentUrl = lesson.ContentUrl,
            Duration = lesson.Duration,
            Order = lesson.Order,
        };
    }
}
