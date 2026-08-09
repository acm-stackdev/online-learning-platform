import { apiFetch } from "@/lib/api/client";
import type { PagedResult } from "@/types/course";
import type { Message } from "@/types/messaging";

export function getConversationHistory(conversationId: number, page = 1) {
  return apiFetch<PagedResult<Message>>(
    `/api/messaging/conversations/${conversationId}/messages?page=${page}&pageSize=50`
  );
}
