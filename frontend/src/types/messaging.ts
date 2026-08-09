export interface Conversation {
  enrollmentId: number;
  conversationId: number | null;
  courseId: number;
  courseTitle: string;
  otherPartyId: number;
  otherPartyUsername: string;
  otherPartyAvatarUrl: string | null;
  otherPartyPresence: string;
  lastMessagePreview: string | null;
  lastMessageSenderId: number | null;
  lastMessageAt: string | null;
  unreadCount: number;
}

export interface Message {
  id: number;
  conversationId: number;
  senderId: number;
  senderUsername: string;
  content: string;
  sentAt: string;
  readAt: string | null;
}

export interface Presence {
  userId: number;
  status: string;
  lastActiveAt: string | null;
}

export interface MessagesReadPayload {
  conversationId: number;
  messageIds: number[];
  readAt: string;
}
