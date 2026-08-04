using LearnHub.Data;
using LearnHub.Models.DTOs.Chatbot;
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
            var isEnrolled = requestingUserId.HasValue &&
                await _db.Enrollments.AnyAsync(e => e.StudentId == requestingUserId.Value && e.CourseId == courseId);

            if (!isOwner && !isAdmin && !isEnrolled)
                throw new ApiException("You do not have access to this course's tutor.", 403);

            var syllabus = string.Join("\n", course.Sections
                .OrderBy(s => s.Order)
                .SelectMany(s => s.Lessons.OrderBy(l => l.Order).Select(l => $"- {s.Title}: {l.Title}")));

            var systemInstruction =
                $"You are a helpful tutor for the course \"{course.Title}\".\n" +
                $"Course description: {course.Description}\n" +
                $"Syllabus:\n{syllabus}\n" +
                "You only have the course's title, description, and lesson titles above - you do not have " +
                "access to the actual video or document content of any lesson. If asked about specific lesson " +
                "content in detail, say so honestly and suggest the student review that lesson directly, while " +
                "still helping with general understanding of the topic.";

            var reply = await _geminiClient.GenerateReplyAsync(systemInstruction, dto.History ?? new List<ChatMessageDto>(), dto.Message);
            return new ChatResponseDto { Reply = reply };
        }
    }
}
