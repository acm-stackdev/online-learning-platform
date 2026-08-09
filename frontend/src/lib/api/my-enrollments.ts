import { serverApiFetch } from "@/lib/api/server";
import type { Enrollment } from "@/types/dashboard";

// Kept separate from lib/api/enrollments.ts (client-side `enrol()`, imported
// by the "use client" EnrolButton) — this uses serverApiFetch, which pulls
// in next/headers and must never end up in a client bundle.
export async function getMyEnrollments(): Promise<Enrollment[]> {
  const result = await serverApiFetch<Enrollment[]>("/api/enrollments");
  return result ?? [];
}
