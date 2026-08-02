using LearnHub.Helpers;
using LearnHub.Models.DTOs.Messaging;
using LearnHub.Models.Entities;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LearnHub.Hubs
{
    [Authorize]
    public class MessagingHub : Hub
    {
        private readonly MessagingService _messagingService;
        private readonly IPresenceTracker _presenceTracker;

        public MessagingHub(MessagingService messagingService, IPresenceTracker presenceTracker)
        {
            _messagingService = messagingService;
            _presenceTracker = presenceTracker;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User!.GetUserId();

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));

            var wasFirstConnection = _presenceTracker.AddConnection(userId, Context.ConnectionId);
            if (wasFirstConnection)
            {
                var status = await _messagingService.GetStoredPresenceStatusAsync(userId);
                await BroadcastPresenceAsync(userId, status.ToString(), lastActiveAt: null);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User!.GetUserId();

            var isNowOffline = _presenceTracker.RemoveConnection(userId, Context.ConnectionId);
            if (isNowOffline)
            {
                var lastActiveAt = await _messagingService.UpdateLastActiveAsync(userId);
                await BroadcastPresenceAsync(userId, "Offline", lastActiveAt);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(SendMessageRequestDto dto)
        {
            var senderId = Context.User!.GetUserId();

            SendMessageResultDto result;
            try
            {
                result = await _messagingService.SendMessageAsync(dto.EnrollmentId, senderId, dto.Content);
            }
            catch (ApiException ex)
            {
                throw new HubException(ex.Message);
            }

            await Clients.Groups(GroupName(senderId), GroupName(result.RecipientId))
                .SendAsync("ReceiveMessage", result.Message);
        }

        public async Task MarkRead(long conversationId)
        {
            var userId = Context.User!.GetUserId();

            MarkReadResultDto result;
            try
            {
                result = await _messagingService.MarkConversationReadAsync(conversationId, userId);
            }
            catch (ApiException ex)
            {
                throw new HubException(ex.Message);
            }

            if (result.MessageIds.Count > 0)
            {
                await Clients.Groups(GroupName(userId), GroupName(result.OtherPartyId))
                    .SendAsync("MessagesRead", new
                    {
                        conversationId,
                        messageIds = result.MessageIds,
                        readAt = result.ReadAt,
                    });
            }
        }

        public async Task SetPresence(string status)
        {
            var userId = Context.User!.GetUserId();

            if (!Enum.TryParse<PresenceStatus>(status, ignoreCase: true, out var parsed))
                throw new HubException("Invalid presence status. Must be 'Online' or 'Busy'.");

            try
            {
                await _messagingService.SetPresenceStatusAsync(userId, parsed);
            }
            catch (ApiException ex)
            {
                throw new HubException(ex.Message);
            }

            await BroadcastPresenceAsync(userId, parsed.ToString(), lastActiveAt: null);
        }

        private async Task BroadcastPresenceAsync(long userId, string status, DateTime? lastActiveAt)
        {
            var contactIds = await _messagingService.GetContactUserIdsAsync(userId);
            if (contactIds.Count == 0)
                return;

            await Clients.Groups(contactIds.Select(GroupName)).SendAsync("PresenceChanged", new PresenceDto
            {
                UserId = userId,
                Status = status,
                LastActiveAt = lastActiveAt,
            });
        }

        private static string GroupName(long userId) => $"user:{userId}";
    }
}
