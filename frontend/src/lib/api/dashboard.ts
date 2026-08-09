import { serverApiFetch } from "@/lib/api/server";
import type { InstructorDashboard, StudentDashboard } from "@/types/dashboard";

export function getStudentDashboard() {
  return serverApiFetch<StudentDashboard>("/api/dashboard/student");
}

export function getInstructorDashboard() {
  return serverApiFetch<InstructorDashboard>("/api/dashboard/instructor");
}
