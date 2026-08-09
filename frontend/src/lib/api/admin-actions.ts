import { apiFetch } from "@/lib/api/client";
import type { Role } from "@/types/auth";

export function changeUserRole(userId: number, role: Role) {
  return apiFetch<void>(`/api/admin/users/${userId}/role`, {
    method: "PUT",
    body: JSON.stringify({ role }),
  });
}

export function suspendUser(userId: number) {
  return apiFetch<void>(`/api/admin/users/${userId}/suspend`, { method: "POST" });
}

export function reinstateUser(userId: number) {
  return apiFetch<void>(`/api/admin/users/${userId}/reinstate`, { method: "POST" });
}

export function approveCourse(courseId: number) {
  return apiFetch<void>(`/api/courses/${courseId}/approve`, { method: "POST" });
}

export function rejectCourse(courseId: number) {
  return apiFetch<void>(`/api/courses/${courseId}/reject`, { method: "POST" });
}

export function approveApplication(applicationId: number) {
  return apiFetch<void>(`/api/instructor-applications/${applicationId}/approve`, {
    method: "POST",
  });
}

export function rejectApplication(applicationId: number) {
  return apiFetch<void>(`/api/instructor-applications/${applicationId}/reject`, {
    method: "POST",
  });
}
