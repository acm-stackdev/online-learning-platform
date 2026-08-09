import { serverApiFetch } from "@/lib/api/server";
import type { PagedResult } from "@/types/course";
import type { InstructorApplication } from "@/types/instructorApplication";

export async function getPendingApplications(): Promise<InstructorApplication[]> {
  const result = await serverApiFetch<PagedResult<InstructorApplication>>(
    "/api/instructor-applications?status=0&pageSize=50"
  );
  return result?.items ?? [];
}
