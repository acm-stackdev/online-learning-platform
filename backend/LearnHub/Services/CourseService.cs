using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Common;
using LearnHub.Models.DTOs.Course;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services
{
    public class CourseService
    {
        private readonly AppDbContext _db;

        public CourseService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CourseListItemDto> CreateAsync(CreateCourseDto dto, long instructorId)
        {
            var course = new Course
            {
                Title = dto.Title,
                Description = dto.Description,
                ThumbnailUrl = dto.ThumbnailUrl,
                Category = dto.Category,
                InstructorId = instructorId,
                Status = CourseStatus.Draft,
                CreatedAt = DateTime.UtcNow,
            };

            _db.Courses.Add(course);
            await _db.SaveChangesAsync();

            return await ToListItemAsync(course.Id);
        }

        public async Task<PagedResult<CourseListItemDto>> GetCatalogueAsync(int page, int pageSize, string? search, string? category)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _db.Courses.Include(c => c.Instructor).Where(c => c.Status == CourseStatus.Published);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = search.ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(pattern) || c.Description.ToLower().Contains(pattern));
            }

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(c => c.Category == category);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => MapListItem(c))
                .ToListAsync();

            return new PagedResult<CourseListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<CourseDetailDto> GetDetailAsync(long id, long? requestingUserId, bool isAdmin)
        {
            var course = await _db.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course is null)
                throw new ApiException("Course not found.", 404);

            var isOwner = requestingUserId.HasValue && course.InstructorId == requestingUserId.Value;
            if (course.Status != CourseStatus.Published && !isOwner && !isAdmin)
                throw new ApiException("Course not found.", 404);

            var isEnrolled = requestingUserId.HasValue &&
                await _db.Enrollments.AnyAsync(e => e.StudentId == requestingUserId.Value && e.CourseId == id);
            var canSeeContent = isOwner || isAdmin || isEnrolled;

            return new CourseDetailDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                ThumbnailUrl = course.ThumbnailUrl,
                Category = course.Category,
                Status = course.Status,
                InstructorId = course.InstructorId,
                InstructorName = course.Instructor.Username,
                CreatedAt = course.CreatedAt,
                IsEnrolled = isEnrolled,
                IsOwner = isOwner,
                Sections = course.Sections
                    .OrderBy(s => s.Order)
                    .Select(s => new SectionSummaryDto
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Order = s.Order,
                        Lessons = s.Lessons
                            .OrderBy(l => l.Order)
                            .Select(l => new LessonSummaryDto
                            {
                                Id = l.Id,
                                Title = l.Title,
                                ContentType = l.ContentType,
                                ContentUrl = canSeeContent ? l.ContentUrl : null,
                                Duration = l.Duration,
                                Order = l.Order,
                            }).ToList(),
                    }).ToList(),
            };
        }

        public async Task<CourseListItemDto> UpdateAsync(long id, UpdateCourseDto dto, long instructorId)
        {
            var course = await GetOwnedCourseAsync(id, instructorId);

            course.Title = dto.Title;
            course.Description = dto.Description;
            course.ThumbnailUrl = dto.ThumbnailUrl;
            course.Category = dto.Category;

            await _db.SaveChangesAsync();

            return await ToListItemAsync(course.Id);
        }

        public async Task DeleteAsync(long id, long instructorId)
        {
            var course = await GetOwnedCourseAsync(id, instructorId);

            if (course.Status == CourseStatus.Published)
                throw new ApiException("Unpublish the course before deleting it.", 400);

            _db.Courses.Remove(course);
            await _db.SaveChangesAsync();
        }

        public async Task<CourseListItemDto> SubmitForReviewAsync(long id, long instructorId)
        {
            var course = await GetOwnedCourseWithContentAsync(id, instructorId);

            if (course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
                throw new ApiException("Course is already published or under review.", 400);

            var hasContent = course.Sections.Any(s => s.Lessons.Any());
            if (!hasContent)
                throw new ApiException("Add at least one section with a lesson before publishing.", 400);

            course.Status = CourseStatus.PendingApproval;
            await _db.SaveChangesAsync();

            return MapListItem(course);
        }

        public async Task<CourseListItemDto> UnpublishAsync(long id, long instructorId)
        {
            var course = await GetOwnedCourseAsync(id, instructorId);

            if (course.Status != CourseStatus.Published)
                throw new ApiException("Course is not published.", 400);

            course.Status = CourseStatus.Draft;
            await _db.SaveChangesAsync();

            return await ToListItemAsync(course.Id);
        }

        public async Task<CourseListItemDto> ApproveAsync(long id)
        {
            var course = await _db.Courses.Include(c => c.Instructor).FirstOrDefaultAsync(c => c.Id == id);
            if (course is null)
                throw new ApiException("Course not found.", 404);

            if (course.Status != CourseStatus.PendingApproval)
                throw new ApiException("Course is not awaiting review.", 400);

            course.Status = CourseStatus.Published;
            await _db.SaveChangesAsync();

            return MapListItem(course);
        }

        public async Task<CourseListItemDto> RejectAsync(long id)
        {
            var course = await _db.Courses.Include(c => c.Instructor).FirstOrDefaultAsync(c => c.Id == id);
            if (course is null)
                throw new ApiException("Course not found.", 404);

            if (course.Status != CourseStatus.PendingApproval)
                throw new ApiException("Course is not awaiting review.", 400);

            course.Status = CourseStatus.Rejected;
            await _db.SaveChangesAsync();

            return MapListItem(course);
        }

        public async Task<CourseListItemDto> ForceUnpublishAsync(long id)
        {
            var course = await _db.Courses.Include(c => c.Instructor).FirstOrDefaultAsync(c => c.Id == id);
            if (course is null)
                throw new ApiException("Course not found.", 404);

            if (course.Status != CourseStatus.Published)
                throw new ApiException("Only published courses can be force-unpublished.", 400);

            course.Status = CourseStatus.Draft;
            await _db.SaveChangesAsync();

            return MapListItem(course);
        }

        public async Task<PagedResult<CourseListItemDto>> GetPendingApprovalAsync(int page, int pageSize)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _db.Courses.Include(c => c.Instructor).Where(c => c.Status == CourseStatus.PendingApproval);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => MapListItem(c))
                .ToListAsync();

            return new PagedResult<CourseListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }

        private async Task<Course> GetOwnedCourseWithContentAsync(long id, long instructorId)
        {
            var course = await _db.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course is null)
                throw new ApiException("Course not found.", 404);

            if (course.InstructorId != instructorId)
                throw new ApiException("You do not own this course.", 403);

            return course;
        }

        private async Task<Course> GetOwnedCourseAsync(long id, long instructorId)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course is null)
                throw new ApiException("Course not found.", 404);

            if (course.InstructorId != instructorId)
                throw new ApiException("You do not own this course.", 403);

            return course;
        }

        private async Task<CourseListItemDto> ToListItemAsync(long courseId)
        {
            var course = await _db.Courses.Include(c => c.Instructor).FirstAsync(c => c.Id == courseId);
            return MapListItem(course);
        }

        private static CourseListItemDto MapListItem(Course course) => new()
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            ThumbnailUrl = course.ThumbnailUrl,
            Category = course.Category,
            Status = course.Status,
            InstructorId = course.InstructorId,
            InstructorName = course.Instructor.Username,
            CreatedAt = course.CreatedAt,
        };
    }
}
