import { serverApiFetch } from "@/lib/api/server";
import type { RosterItem } from "@/types/roster";

export async function getCourseRoster(courseId: number): Promise<RosterItem[]> {
  const result = await serverApiFetch<RosterItem[]>(`/api/enrollments/course/${courseId}`);
  return result ?? [];
}
