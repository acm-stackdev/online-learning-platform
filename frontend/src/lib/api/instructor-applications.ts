import { apiFetch } from "@/lib/api/client";
import type { InstructorApplication } from "@/types/instructorApplication";

export function submitInstructorApplication(message: string) {
  return apiFetch<InstructorApplication>("/api/instructor-applications", {
    method: "POST",
    body: JSON.stringify({ message }),
  });
}
