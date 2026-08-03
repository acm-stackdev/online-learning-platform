using LearnHub.Data;
using LearnHub.Models.DTOs.Common;
using LearnHub.Models.DTOs.InstructorApplication;
using LearnHub.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services
{
    public class InstructorApplicationService
    {
        private readonly AppDbContext _db;

        public InstructorApplicationService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<InstructorApplicationDto> SubmitAsync(long userId, SubmitInstructorApplicationDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
                throw new ApiException("User not found.", 404);

            if (user.Role != Role.Student)
                throw new ApiException("Only students can apply to become an instructor.", 400);

            var hasPending = await _db.InstructorApplications
                .AnyAsync(a => a.UserId == userId && a.Status == ApplicationStatus.Pending);
            if (hasPending)
                throw new ApiException("You already have a pending instructor application.", 409);

            var application = new Models.Entities.InstructorApplication
            {
                UserId = userId,
                Message = dto.Message,
                Status = ApplicationStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
            };

            _db.InstructorApplications.Add(application);
            await _db.SaveChangesAsync();

            return MapApplication(application, user, null);
        }

        public async Task<List<InstructorApplicationDto>> GetMineAsync(long userId)
        {
            var applications = await _db.InstructorApplications
                .Include(a => a.Applicant)
                .Include(a => a.ReviewedBy)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            return applications.Select(a => MapApplication(a, a.Applicant, a.ReviewedBy)).ToList();
        }

        public async Task<PagedResult<InstructorApplicationDto>> GetAllAsync(int page, int pageSize, ApplicationStatus? status)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _db.InstructorApplications
                .Include(a => a.Applicant)
                .Include(a => a.ReviewedBy)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            var totalCount = await query.CountAsync();
            var applications = await query
                .OrderByDescending(a => a.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<InstructorApplicationDto>
            {
                Items = applications.Select(a => MapApplication(a, a.Applicant, a.ReviewedBy)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<InstructorApplicationDto> ApproveAsync(long applicationId, long adminUserId)
        {
            var application = await GetPendingForReviewAsync(applicationId);

            application.Status = ApplicationStatus.Approved;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedByUserId = adminUserId;
            application.Applicant.Role = Role.Instructor;

            await _db.SaveChangesAsync();

            var reviewer = await _db.Users.FirstOrDefaultAsync(u => u.Id == adminUserId);
            return MapApplication(application, application.Applicant, reviewer);
        }

        public async Task<InstructorApplicationDto> RejectAsync(long applicationId, long adminUserId)
        {
            var application = await GetPendingForReviewAsync(applicationId);

            application.Status = ApplicationStatus.Rejected;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedByUserId = adminUserId;

            await _db.SaveChangesAsync();

            var reviewer = await _db.Users.FirstOrDefaultAsync(u => u.Id == adminUserId);
            return MapApplication(application, application.Applicant, reviewer);
        }

        private async Task<Models.Entities.InstructorApplication> GetPendingForReviewAsync(long applicationId)
        {
            var application = await _db.InstructorApplications
                .Include(a => a.Applicant)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application is null)
                throw new ApiException("Instructor application not found.", 404);

            if (application.Status != ApplicationStatus.Pending)
                throw new ApiException("This application has already been reviewed.", 409);

            return application;
        }

        private static InstructorApplicationDto MapApplication(Models.Entities.InstructorApplication application, User applicant, User? reviewer) => new()
        {
            Id = application.Id,
            UserId = application.UserId,
            ApplicantUsername = applicant.Username,
            ApplicantEmail = applicant.Email,
            Message = application.Message,
            Status = application.Status,
            SubmittedAt = application.SubmittedAt,
            ReviewedAt = application.ReviewedAt,
            ReviewedByUserId = application.ReviewedByUserId,
            ReviewedByUsername = reviewer?.Username,
        };
    }
}
