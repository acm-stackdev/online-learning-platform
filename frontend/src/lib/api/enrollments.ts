import { apiFetch } from "@/lib/api/client";

export function enrol(courseId: number) {
  return apiFetch<{ id: number }>("/api/enrollments", {
    method: "POST",
    body: JSON.stringify({ courseId }),
  });
}
