using LearnHub.Data;
using LearnHub.Models;
using LearnHub.Models.DTOs.Progress;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services
{
    public class ProgressService
    {
        private readonly AppDbContext _db;
        private readonly CertificateService _certificateService;

        public ProgressService(AppDbContext db, CertificateService certificateService)
        {
            _db = db;
            _certificateService = certificateService;
        }

        public async Task<LessonProgressDto> UpdateProgressAsync(long lessonId, long studentId, UpdateLessonProgressDto dto)
        {
            var lesson = await _db.Lessons.Include(l => l.Section).FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson is null)
                throw new ApiException("Lesson not found.", 404);

            var courseId = lesson.Section.CourseId;

            var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
            if (enrollment is null)
                throw new ApiException("You are not enrolled in this course.", 403);

            var progress = await _db.LessonProgress.FirstOrDefaultAsync(p => p.EnrollmentId == enrollment.Id && p.LessonId == lessonId);
            if (progress is null)
            {
                progress = new LessonProgress
                {
                    EnrollmentId = enrollment.Id,
                    LessonId = lessonId,
                };
                _db.LessonProgress.Add(progress);
            }

            progress.WatchSeconds = dto.WatchSeconds;
            progress.IsCompleted = dto.IsCompleted;
            progress.LastWatchedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await RecomputeEnrollmentCompletionAsync(enrollment, courseId);

            return new LessonProgressDto
            {
                LessonId = lesson.Id,
                LessonTitle = lesson.Title,
                IsCompleted = progress.IsCompleted,
                WatchSeconds = progress.WatchSeconds,
                LastWatchedAt = progress.LastWatchedAt,
            };
        }

        public async Task<EnrollmentProgressDto> GetEnrollmentProgressAsync(long enrollmentId, long studentId)
        {
            var enrollment = await _db.Enrollments
                .Include(e => e.Course)
                    .ThenInclude(c => c.Sections)
                        .ThenInclude(s => s.Lessons)
                .Include(e => e.ProgressRecords)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment is null)
                throw new ApiException("Enrollment not found.", 404);

            if (enrollment.StudentId != studentId)
                throw new ApiException("This is not your enrollment.", 403);

            var allLessons = enrollment.Course.Sections.SelectMany(s => s.Lessons).ToList();
            var progressByLessonId = enrollment.ProgressRecords.ToDictionary(p => p.LessonId);

            var lessonDtos = allLessons.Select(l =>
            {
                progressByLessonId.TryGetValue(l.Id, out var progress);
                return new LessonProgressDto
                {
                    LessonId = l.Id,
                    LessonTitle = l.Title,
                    IsCompleted = progress?.IsCompleted ?? false,
                    WatchSeconds = progress?.WatchSeconds ?? 0,
                    LastWatchedAt = progress?.LastWatchedAt ?? default,
                };
            }).ToList();

            var totalLessons = allLessons.Count;
            var completedLessons = lessonDtos.Count(l => l.IsCompleted);

            return new EnrollmentProgressDto
            {
                EnrollmentId = enrollment.Id,
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                PercentComplete = totalLessons > 0 ? Math.Round((double)completedLessons / totalLessons * 100, 1) : 0,
                IsCourseCompleted = enrollment.IsCompleted,
                Lessons = lessonDtos,
            };
        }

        private async Task RecomputeEnrollmentCompletionAsync(Enrollment enrollment, long courseId)
        {
            var totalLessons = await _db.Lessons.CountAsync(l => l.Section.CourseId == courseId);
            if (totalLessons == 0)
                return;

            var completedLessons = await _db.LessonProgress
                .CountAsync(p => p.EnrollmentId == enrollment.Id && p.IsCompleted);

            if (completedLessons >= totalLessons && !enrollment.IsCompleted)
            {
                enrollment.IsCompleted = true;
                await _db.SaveChangesAsync();
                await _certificateService.IssueForEnrollmentAsync(enrollment.Id);
            }
        }
    }
}
