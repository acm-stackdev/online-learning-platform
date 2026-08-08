import { serverApiFetch } from "@/lib/api/server";
import type { StudentDashboard } from "@/types/dashboard";

export function getStudentDashboard() {
  return serverApiFetch<StudentDashboard>("/api/dashboard/student");
}
