import { serverApiFetch } from "@/lib/api/server";
import type { CourseDetail, CourseListItem, PagedResult } from "@/types/course";

export async function getFeaturedCourses(): Promise<CourseListItem[]> {
  try {
    const res = await fetch(`${process.env.API_URL}/api/courses?pageSize=3`, {
      next: { revalidate: 60 },
    });

    if (!res.ok) return [];

    const data: PagedResult<CourseListItem> = await res.json();
    return data.items;
  } catch {
    return [];
  }
}

export async function getCourses({
  page = 1,
  search,
}: {
  page?: number;
  search?: string;
}): Promise<PagedResult<CourseListItem>> {
  const params = new URLSearchParams({ page: String(page) });
  if (search) params.set("search", search);

  const result = await serverApiFetch<PagedResult<CourseListItem>>(
    `/api/courses?${params.toString()}`
  );

  return result ?? { items: [], page, pageSize: 12, totalCount: 0 };
}

export function getCourseDetail(id: number) {
  return serverApiFetch<CourseDetail>(`/api/courses/${id}`);
}
