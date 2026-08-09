import { apiFetch } from "@/lib/api/client";
import type { UserResponse } from "@/types/auth";

export function updateProfile(payload: { username: string; avatarUrl: string | null }) {
  return apiFetch<UserResponse>("/api/auth/me", {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export function changePassword(payload: { currentPassword: string; newPassword: string }) {
  return apiFetch<{ message: string }>("/api/auth/change-password", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}
