using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Admin;
using LearnHub.Models.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services
{
    public class AdminService
    {
        private readonly AppDbContext _db;

        public AdminService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(int page, int pageSize, string? search, Role? role, bool? isSuspended)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));

            if (role.HasValue)
                query = query.Where(u => u.Role == role.Value);

            if (isSuspended.HasValue)
                query = query.Where(u => u.IsSuspended == isSuspended.Value);

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AdminUserListItemDto>
            {
                Items = users.Select(MapListItem).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<AdminUserListItemDto> SuspendUserAsync(long userId, long actingAdminId)
        {
            if (userId == actingAdminId)
                throw new ApiException("You cannot modify your own account.", 400);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
                throw new ApiException("User not found.", 404);

            if (user.IsSuspended)
                throw new ApiException("User is already suspended.", 400);

            user.IsSuspended = true;
            await _db.SaveChangesAsync();

            return MapListItem(user);
        }

        public async Task<AdminUserListItemDto> ReinstateUserAsync(long userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
                throw new ApiException("User not found.", 404);

            if (!user.IsSuspended)
                throw new ApiException("User is not suspended.", 400);

            user.IsSuspended = false;
            await _db.SaveChangesAsync();

            return MapListItem(user);
        }

        public async Task<AdminUserListItemDto> ChangeRoleAsync(long userId, Role? newRole, long actingAdminId)
        {
            if (newRole is null)
                throw new ApiException("You must choose a role.", 400);

            if (userId == actingAdminId)
                throw new ApiException("You cannot modify your own account.", 400);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
                throw new ApiException("User not found.", 404);

            user.Role = newRole.Value;
            await _db.SaveChangesAsync();

            return MapListItem(user);
        }

        public async Task<PlatformStatsDto> GetPlatformStatsAsync()
        {
            return new PlatformStatsDto
            {
                TotalUsers = await _db.Users.CountAsync(),
                StudentCount = await _db.Users.CountAsync(u => u.Role == Role.Student),
                InstructorCount = await _db.Users.CountAsync(u => u.Role == Role.Instructor),
                AdminCount = await _db.Users.CountAsync(u => u.Role == Role.Admin),
                SuspendedCount = await _db.Users.CountAsync(u => u.IsSuspended),

                TotalCourses = await _db.Courses.CountAsync(),
                DraftCourseCount = await _db.Courses.CountAsync(c => c.Status == CourseStatus.Draft),
                PendingApprovalCourseCount = await _db.Courses.CountAsync(c => c.Status == CourseStatus.PendingApproval),
                PublishedCourseCount = await _db.Courses.CountAsync(c => c.Status == CourseStatus.Published),
                RejectedCourseCount = await _db.Courses.CountAsync(c => c.Status == CourseStatus.Rejected),

                TotalEnrollments = await _db.Enrollments.CountAsync(),
                CompletedEnrollmentCount = await _db.Enrollments.CountAsync(e => e.CompletedAt != null),
                InProgressEnrollmentCount = await _db.Enrollments.CountAsync(e => e.CompletedAt == null),
            };
        }

        private static AdminUserListItemDto MapListItem(User user)
        {
            return new AdminUserListItemDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsSuspended = user.IsSuspended,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                LastActiveAt = user.LastActiveAt,
            };
        }
    }
}
