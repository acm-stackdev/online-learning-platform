import { serverApiFetch } from "@/lib/api/server";
import type { CourseListItem, PagedResult } from "@/types/course";

export async function getPendingCourses(): Promise<CourseListItem[]> {
  const result = await serverApiFetch<PagedResult<CourseListItem>>(
    "/api/courses/pending?pageSize=50"
  );
  return result?.items ?? [];
}
