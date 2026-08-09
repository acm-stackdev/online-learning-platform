import { apiFetch } from "@/lib/api/client";

export function updateLessonProgress(
  lessonId: number,
  payload: { watchSeconds: number; isCompleted: boolean }
) {
  return apiFetch<void>(`/api/lessons/${lessonId}/progress`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}
