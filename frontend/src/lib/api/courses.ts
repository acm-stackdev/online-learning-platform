import type { CourseListItem, PagedResult } from "@/types/course";

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
