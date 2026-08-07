import { apiFetch } from "@/lib/api/client";
import type {
  GoogleLoginResult,
  LoginResult,
  RegisterResult,
} from "@/types/auth";
import type { Role } from "@/types/auth";

export function register(payload: {
  username: string;
  email: string;
  password: string;
  role: Role;
}) {
  return apiFetch<RegisterResult>("/api/auth/register", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function login(payload: { email: string; password: string }) {
  return apiFetch<LoginResult>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function googleLogin(idToken: string) {
  return apiFetch<GoogleLoginResult>("/api/auth/google", {
    method: "POST",
    body: JSON.stringify({ idToken }),
  });
}

export function verifyEmail(token: string) {
  return apiFetch<{ message: string }>("/api/auth/verify-email", {
    method: "POST",
    body: JSON.stringify({ token }),
  });
}

export function forgotPassword(email: string) {
  return apiFetch<{ message: string }>("/api/auth/forgot-password", {
    method: "POST",
    body: JSON.stringify({ email }),
  });
}

export function resetPassword(token: string, newPassword: string) {
  return apiFetch<{ message: string }>("/api/auth/reset-password", {
    method: "POST",
    body: JSON.stringify({ token, newPassword }),
  });
}
