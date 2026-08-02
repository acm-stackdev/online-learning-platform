using LearnHub.Data;
using LearnHub.Models.DTOs.Common;
using LearnHub.Models.DTOs.Messaging;
using LearnHub.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services
{
    public class MessagingService
    {
        private const int MaxContentLength = 4000;

        private readonly AppDbContext _db;
        private readonly IPresenceTracker _presenceTracker;

        public MessagingService(AppDbContext db, IPresenceTracker presenceTracker)
        {
            _db = db;
            _presenceTracker = presenceTracker;
        }

        public async Task<SendMessageResultDto> SendMessageAsync(long enrollmentId, long senderId, string content)
        {
            var trimmedContent = content?.Trim();
            if (string.IsNullOrEmpty(trimmedContent))
                throw new ApiException("Message content is required.", 400);

            if (trimmedContent.Length > MaxContentLength)
                throw new ApiException("Message is too long.", 400);

            var enrollment = await _db.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Student)
                .Include(e => e.Conversation)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment is null)
                throw new ApiException("Enrollment not found.", 404);

            var isEnrolledStudent = enrollment.StudentId == senderId;
            var isOwningInstructor = enrollment.Course.InstructorId == senderId;
            if (!isEnrolledStudent && !isOwningInstructor)
                throw new ApiException("You are not a participant in this conversation.", 403);

            var conversation = enrollment.Conversation;
            if (conversation is null)
            {
                conversation = new Conversation
                {
                    EnrollmentId = enrollmentId,
                    CreatedAt = DateTime.UtcNow,
                };
                _db.Conversations.Add(conversation);
            }

            var message = new Message
            {
                Conversation = conversation,
                SenderId = senderId,
                Content = trimmedContent,
                SentAt = DateTime.UtcNow,
                ReadAt = null,
            };
            _db.Messages.Add(message);

            await _db.SaveChangesAsync();

            var senderUsername = isEnrolledStudent ? enrollment.Student.Username : (await GetUsernameAsync(senderId));
            var recipientId = isEnrolledStudent ? enrollment.Course.InstructorId : enrollment.StudentId;

            return new SendMessageResultDto
            {
                Message = MapMessage(message, senderUsername),
                RecipientId = recipientId,
            };
        }

        public async Task<List<ConversationListItemDto>> GetMyConversationsAsync(long userId)
        {
            var enrollments = await _db.Enrollments
                .Include(e => e.Course)
                    .ThenInclude(c => c.Instructor)
                .Include(e => e.Student)
                .Include(e => e.Conversation)
                    .ThenInclude(c => c.Messages)
                .Where(e => e.StudentId == userId || e.Course.InstructorId == userId)
                .ToListAsync();

            var items = enrollments.Select(enrollment =>
            {
                var isEnrolledStudent = enrollment.StudentId == userId;
                var otherParty = isEnrolledStudent ? enrollment.Course.Instructor : enrollment.Student;

                var messages = enrollment.Conversation?.Messages;
                var lastMessage = messages?.OrderByDescending(m => m.SentAt).FirstOrDefault();
                var unreadCount = messages?.Count(m => m.SenderId != userId && m.ReadAt == null) ?? 0;

                var dto = new ConversationListItemDto
                {
                    EnrollmentId = enrollment.Id,
                    ConversationId = enrollment.Conversation?.Id,
                    CourseId = enrollment.CourseId,
                    CourseTitle = enrollment.Course.Title,
                    OtherPartyId = otherParty.Id,
                    OtherPartyUsername = otherParty.Username,
                    OtherPartyAvatarUrl = otherParty.AvatarUrl,
                    OtherPartyPresence = _presenceTracker.IsOnline(otherParty.Id) ? otherParty.PresenceStatus.ToString() : "Offline",
                    LastMessagePreview = lastMessage?.Content,
                    LastMessageSenderId = lastMessage?.SenderId,
                    LastMessageAt = lastMessage?.SentAt,
                    UnreadCount = unreadCount,
                };

                var sortKey = lastMessage?.SentAt ?? enrollment.EnrolledAt;
                return (Dto: dto, SortKey: sortKey);
            })
            .OrderByDescending(x => x.SortKey)
            .Select(x => x.Dto)
            .ToList();

            return items;
        }

        public async Task<PagedResult<MessageDto>> GetConversationHistoryAsync(long conversationId, long requesterId, int page, int pageSize)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var conversation = await _db.Conversations
                .Include(c => c.Enrollment)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation is null)
                throw new ApiException("Conversation not found.", 404);

            EnsureParticipant(conversation.Enrollment, requesterId);

            var query = _db.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId);

            var totalCount = await query.CountAsync();

            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<MessageDto>
            {
                Items = messages.Select(m => MapMessage(m, m.Sender.Username)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<MarkReadResultDto> MarkConversationReadAsync(long conversationId, long requesterId)
        {
            var conversation = await _db.Conversations
                .Include(c => c.Enrollment)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation is null)
                throw new ApiException("Conversation not found.", 404);

            var enrollment = conversation.Enrollment;
            EnsureParticipant(enrollment, requesterId);

            var otherPartyId = enrollment.StudentId == requesterId ? enrollment.Course.InstructorId : enrollment.StudentId;

            var unreadMessages = await _db.Messages
                .Where(m => m.ConversationId == conversationId && m.SenderId != requesterId && m.ReadAt == null)
                .ToListAsync();

            var readAt = DateTime.UtcNow;
            foreach (var message in unreadMessages)
                message.ReadAt = readAt;

            if (unreadMessages.Count > 0)
                await _db.SaveChangesAsync();

            return new MarkReadResultDto
            {
                MessageIds = unreadMessages.Select(m => m.Id).ToList(),
                OtherPartyId = otherPartyId,
                ReadAt = readAt,
            };
        }

        public async Task<List<long>> GetContactUserIdsAsync(long userId)
        {
            var enrollments = await _db.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == userId || e.Course.InstructorId == userId)
                .ToListAsync();

            return enrollments
                .Select(e => e.StudentId == userId ? e.Course.InstructorId : e.StudentId)
                .Distinct()
                .ToList();
        }

        public async Task<PresenceStatus> GetStoredPresenceStatusAsync(long userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
                throw new ApiException("User not found.", 404);

            return user.PresenceStatus;
        }

        public async Task SetPresenceStatusAsync(long userId, PresenceStatus status)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
                throw new ApiException("User not found.", 404);

            user.PresenceStatus = status;
            await _db.SaveChangesAsync();
        }

        public async Task<DateTime> UpdateLastActiveAsync(long userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
                throw new ApiException("User not found.", 404);

            user.LastActiveAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return user.LastActiveAt.Value;
        }

        private static void EnsureParticipant(Enrollment enrollment, long userId)
        {
            var isEnrolledStudent = enrollment.StudentId == userId;
            var isOwningInstructor = enrollment.Course.InstructorId == userId;
            if (!isEnrolledStudent && !isOwningInstructor)
                throw new ApiException("You are not a participant in this conversation.", 403);
        }

        private async Task<string> GetUsernameAsync(long userId)
        {
            var username = await _db.Users.Where(u => u.Id == userId).Select(u => u.Username).FirstOrDefaultAsync();
            return username ?? string.Empty;
        }

        private static MessageDto MapMessage(Message message, string senderUsername) => new()
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderUsername = senderUsername,
            Content = message.Content,
            SentAt = message.SentAt,
            ReadAt = message.ReadAt,
        };
    }
}
