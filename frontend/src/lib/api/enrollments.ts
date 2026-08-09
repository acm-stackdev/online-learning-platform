import { apiFetch } from "@/lib/api/client";

export function enrol(courseId: number) {
  return apiFetch<{ id: number }>("/api/enrollments", {
    method: "POST",
    body: JSON.stringify({ courseId }),
  });
}

export function unenrol(enrollmentId: number) {
  return apiFetch<void>(`/api/enrollments/${enrollmentId}`, { method: "DELETE" });
}
