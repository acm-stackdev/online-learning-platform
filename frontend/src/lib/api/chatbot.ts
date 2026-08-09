import { apiFetch } from "@/lib/api/client";
import type { ChatMessage, ChatResponse } from "@/types/chatbot";

export function askCourseTutor(
  courseId: number,
  payload: { message: string; history: ChatMessage[] }
) {
  return apiFetch<ChatResponse>(`/api/courses/${courseId}/chat`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}
