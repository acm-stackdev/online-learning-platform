using LearnHub.Data;
using LearnHub.Models.DTOs.Chatbot;
using LearnHub.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services
{
    public class ChatbotService
    {
        private readonly AppDbContext _db;
        private readonly IGeminiClient _geminiClient;

        public ChatbotService(AppDbContext db, IGeminiClient geminiClient)
        {
            _db = db;
            _geminiClient = geminiClient;
        }

        public async Task<ChatResponseDto> AskAsync(long courseId, long? requestingUserId, bool isAdmin, ChatRequestDto dto)
        {
            var course = await _db.Courses
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Lessons)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course is null)
                throw new ApiException("Course not found.", 404);

            var isOwner = requestingUserId.HasValue && course.InstructorId == requestingUserId.Value;

            if (!isOwner && !isAdmin && course.Status != CourseStatus.Published)
                throw new ApiException("You do not have access to this course's tutor.", 403);

            var isEnrolled = requestingUserId.HasValue &&
                await _db.Enrollments.AnyAsync(e => e.StudentId == requestingUserId.Value && e.CourseId == courseId);

            var relationship = isOwner
                ? "This user is the course's instructor."
                : isAdmin
                    ? "This user is a platform Admin previewing the course."
                    : isEnrolled
                        ? "This user is already enrolled in the course."
                        : "This user has not enrolled in the course yet - they're deciding whether to. " +
                          "Be welcoming and help them understand what they'd learn and how the course is taught.";

            var syllabus = string.Join("\n", course.Sections
                .OrderBy(s => s.Order).ThenBy(s => s.Id)
                .SelectMany(s => s.Lessons.OrderBy(l => l.Order).ThenBy(l => l.Id).Select(l => $"- {s.Title}: {l.Title}")));

            var systemInstruction =
                $"You are a helpful tutor for the course \"{course.Title}\".\n" +
                $"Course description: {course.Description}\n" +
                $"Syllabus:\n{syllabus}\n" +
                $"{relationship}\n" +
                "You only have the course's title, description, and lesson titles above - you do not have " +
                "access to the actual video or document content of any lesson. If asked about specific lesson " +
                "content in detail, say so honestly and suggest the student review that lesson directly, while " +
                "still helping with general understanding of the topic.";

            var reply = await _geminiClient.GenerateReplyAsync(systemInstruction, dto.History ?? new List<ChatMessageDto>(), dto.Message);
            return new ChatResponseDto { Reply = reply };
        }
    }
}
