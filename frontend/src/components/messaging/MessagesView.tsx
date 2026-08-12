"use client";

import { useCallback, useEffect, useRef, useState } from "react";

import { ConversationList } from "@/components/messaging/ConversationList";
import { ChatThread, type ChatThreadHandle } from "@/components/messaging/ChatThread";
import { useMessagingConnection } from "@/lib/signalr/useMessagingConnection";
import type { Conversation, Message, MessagesReadPayload, Presence } from "@/types/messaging";

export function MessagesView({
  initialConversations,
  currentUserId,
  initialPresence,
}: {
  initialConversations: Conversation[];
  currentUserId: number;
  initialPresence: string;
}) {
  const [conversations, setConversations] = useState(initialConversations);
  const [selectedEnrollmentId, setSelectedEnrollmentId] = useState<number | null>(
    initialConversations[0]?.enrollmentId ?? null
  );
  const [myPresence, setMyPresence] = useState(initialPresence);

  const selected =
    conversations.find((c) => c.enrollmentId === selectedEnrollmentId) ?? null;

  // Refs so the SignalR handlers (created once, see useMessagingConnection)
  // always see which thread is currently open without needing to reconnect.
  const activeConversationIdRef = useRef<number | null>(null);
  const selectedOtherPartyIdRef = useRef<number | null>(null);
  const chatThreadRef = useRef<ChatThreadHandle>(null);

  useEffect(() => {
    activeConversationIdRef.current = selected?.conversationId ?? null;
    selectedOtherPartyIdRef.current = selected?.otherPartyId ?? null;
  }, [selected?.conversationId, selected?.otherPartyId]);

  const handleReceiveMessage = useCallback(
    (message: Message) => {
      setConversations((prev) =>
        prev.map((c) => {
          const matches =
            c.conversationId === message.conversationId ||
            (c.conversationId === null && c.otherPartyId === message.senderId);
          if (!matches) return c;
          return {
            ...c,
            conversationId: c.conversationId ?? message.conversationId,
            lastMessagePreview: message.content,
            lastMessageSenderId: message.senderId,
            lastMessageAt: message.sentAt,
            unreadCount:
              message.senderId !== currentUserId &&
              activeConversationIdRef.current !== message.conversationId
                ? c.unreadCount + 1
                : c.unreadCount,
          };
        })
      );

      // A brand-new conversation (no messages sent yet) has no conversationId
      // client-side until its first message arrives. MessageDto doesn't carry
      // enrollmentId, so we can't always match precisely — but a message we
      // just sent ourselves always belongs to whichever thread is open.
      const belongsToOpenThread =
        message.conversationId === activeConversationIdRef.current ||
        (activeConversationIdRef.current === null &&
          message.senderId === selectedOtherPartyIdRef.current) ||
        message.senderId === currentUserId;

      if (belongsToOpenThread) {
        activeConversationIdRef.current = message.conversationId;
        chatThreadRef.current?.appendMessage(message);
      }
    },
    [currentUserId]
  );

  const handleMessagesRead = useCallback((payload: MessagesReadPayload) => {
    setConversations((prev) =>
      prev.map((c) =>
        c.conversationId === payload.conversationId ? { ...c, unreadCount: 0 } : c
      )
    );
  }, []);

  const handlePresenceChanged = useCallback((presence: Presence) => {
    setConversations((prev) =>
      prev.map((c) =>
        c.otherPartyId === presence.userId
          ? { ...c, otherPartyPresence: presence.status }
          : c
      )
    );
  }, []);

  const { markRead, sendMessage, setPresence } = useMessagingConnection({
    onReceiveMessage: handleReceiveMessage,
    onMessagesRead: handleMessagesRead,
    onPresenceChanged: handlePresenceChanged,
  });

  function handleThreadOpened(conversationId: number) {
    const unread = conversations.find((c) => c.conversationId === conversationId)
      ?.unreadCount;
    if (unread && unread > 0) markRead(conversationId);
  }

  function handleSetPresence(status: "Online" | "Busy") {
    setMyPresence(status);
    setPresence(status);
  }

  return (
    <div className="grid flex-1 grid-cols-1 md:grid-cols-[320px_1fr]">
      <ConversationList
        conversations={conversations}
        selectedEnrollmentId={selectedEnrollmentId}
        onSelect={setSelectedEnrollmentId}
        myPresence={myPresence}
        onSetPresence={handleSetPresence}
      />

      {selected ? (
        <ChatThread
          key={selected.enrollmentId}
          ref={chatThreadRef}
          conversation={selected}
          currentUserId={currentUserId}
          onSend={(content) => sendMessage(selected.enrollmentId, content)}
          onOpened={handleThreadOpened}
        />
      ) : (
        <div className="hidden items-center justify-center text-sm text-muted-foreground md:flex">
          Select a conversation
        </div>
      )}
    </div>
  );
}
