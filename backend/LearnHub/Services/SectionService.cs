using LearnHub.Data;
using LearnHub.Models;
using LearnHub.Models.DTOs.Course;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services
{
    public class SectionService
    {
        private readonly AppDbContext _db;

        public SectionService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<SectionSummaryDto> CreateAsync(CreateSectionDto dto, long instructorId)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == dto.CourseId);
            if (course is null)
                throw new ApiException("Course not found.", 404);

            if (course.InstructorId != instructorId)
                throw new ApiException("You do not own this course.", 403);

            var maxOrder = await _db.Set<Section>()
                .Where(s => s.CourseId == dto.CourseId)
                .Select(s => (int?)s.Order)
                .MaxAsync() ?? 0;

            var section = new Section
            {
                CourseId = dto.CourseId,
                Title = dto.Title,
                Order = maxOrder + 1,
            };

            _db.Set<Section>().Add(section);
            await _db.SaveChangesAsync();

            return MapSection(section);
        }

        public async Task<SectionSummaryDto> UpdateAsync(long id, UpdateSectionDto dto, long instructorId)
        {
            var section = await GetOwnedSectionAsync(id, instructorId);

            section.Title = dto.Title;
            await _db.SaveChangesAsync();

            return MapSection(section);
        }

        public async Task DeleteAsync(long id, long instructorId)
        {
            var section = await GetOwnedSectionAsync(id, instructorId);

            _db.Set<Section>().Remove(section);
            await _db.SaveChangesAsync();
        }

        public async Task ReorderAsync(ReorderSectionsDto dto, long instructorId)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == dto.CourseId);
            if (course is null)
                throw new ApiException("Course not found.", 404);

            if (course.InstructorId != instructorId)
                throw new ApiException("You do not own this course.", 403);

            var sections = await _db.Set<Section>().Where(s => s.CourseId == dto.CourseId).ToListAsync();

            var existingIds = sections.Select(s => s.Id).ToHashSet();
            if (!existingIds.SetEquals(dto.OrderedSectionIds))
                throw new ApiException("Provided section ids do not match this course's sections.", 400);

            for (var i = 0; i < dto.OrderedSectionIds.Count; i++)
            {
                var section = sections.First(s => s.Id == dto.OrderedSectionIds[i]);
                section.Order = i + 1;
            }

            await _db.SaveChangesAsync();
        }

        private async Task<Section> GetOwnedSectionAsync(long id, long instructorId)
        {
            var section = await _db.Set<Section>().Include(s => s.Course).FirstOrDefaultAsync(s => s.Id == id);
            if (section is null)
                throw new ApiException("Section not found.", 404);

            if (section.Course.InstructorId != instructorId)
                throw new ApiException("You do not own this course.", 403);

            return section;
        }

        private static SectionSummaryDto MapSection(Section section) => new()
        {
            Id = section.Id,
            Title = section.Title,
            Order = section.Order,
        };
    }
}
