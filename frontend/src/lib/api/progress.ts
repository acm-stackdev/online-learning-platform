import { serverApiFetch } from "@/lib/api/server";
import type { EnrollmentProgress } from "@/types/progress";

export function getEnrollmentProgress(enrollmentId: number) {
  return serverApiFetch<EnrollmentProgress>(
    `/api/enrollments/${enrollmentId}/progress`
  );
}
