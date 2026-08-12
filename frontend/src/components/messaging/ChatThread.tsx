"use client";

import { forwardRef, useEffect, useImperativeHandle, useState } from "react";

import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { getConversationHistory } from "@/lib/api/messages";
import { cn } from "@/lib/utils";
import type { Conversation, Message } from "@/types/messaging";

export interface ChatThreadHandle {
  appendMessage: (message: Message) => void;
}

export const ChatThread = forwardRef<
  ChatThreadHandle,
  {
    conversation: Conversation;
    currentUserId: number;
    onSend: (content: string) => void;
    onOpened: (conversationId: number) => void;
  }
>(function ChatThread({ conversation, currentUserId, onSend, onOpened }, ref) {
  const [messages, setMessages] = useState<Message[]>([]);
  const [loading, setLoading] = useState(Boolean(conversation.conversationId));
  const [draft, setDraft] = useState("");

  useImperativeHandle(ref, () => ({
    appendMessage: (message) => setMessages((prev) => [...prev, message]),
  }));

  useEffect(() => {
    if (!conversation.conversationId) return;

    let cancelled = false;
    getConversationHistory(conversation.conversationId)
      .then((result) => {
        if (cancelled) return;
        setMessages(result.items.slice().reverse());
        onOpened(conversation.conversationId!);
      })
      .catch(() => {})
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [conversation.conversationId]);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const content = draft.trim();
    if (!content) return;
    onSend(content);
    setDraft("");
  }

  return (
    <div className="flex h-full flex-col">
      <div className="border-b border-border px-4 py-3">
        <div className="flex items-center gap-1.5">
          <p className="text-sm font-medium">{conversation.otherPartyUsername}</p>
          <span
            className={cn(
              "size-2 rounded-full",
              conversation.otherPartyPresence === "Online"
                ? "bg-primary"
                : conversation.otherPartyPresence === "Busy"
                  ? "bg-destructive"
                  : "bg-muted-foreground"
            )}
          />
          <span className="text-xs text-muted-foreground">
            {conversation.otherPartyPresence}
          </span>
        </div>
        <p className="text-xs text-muted-foreground">{conversation.courseTitle}</p>
      </div>

      <div className="flex-1 space-y-3 overflow-y-auto p-4">
        {loading ? (
          <p className="text-sm text-muted-foreground">Loading...</p>
        ) : messages.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No messages yet — say hello to get started.
          </p>
        ) : (
          messages.map((message) => {
            const isMine = message.senderId === currentUserId;
            return (
              <div
                key={message.id}
                className={cn("flex", isMine ? "justify-end" : "justify-start")}
              >
                <div
                  className={cn(
                    "max-w-xs rounded-lg px-3 py-2 text-sm sm:max-w-sm",
                    isMine
                      ? "bg-primary text-primary-foreground"
                      : "bg-muted text-foreground"
                  )}
                >
                  {message.content}
                </div>
              </div>
            );
          })
        )}
      </div>

      <form onSubmit={handleSubmit} className="flex gap-2 border-t border-border p-3">
        <Input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder="Write a message"
          className="flex-1"
        />
        <Button type="submit" disabled={!draft.trim()}>
          Send
        </Button>
      </form>
    </div>
  );
});
