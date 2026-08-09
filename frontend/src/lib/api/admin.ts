import { serverApiFetch } from "@/lib/api/server";
import type { PagedResult } from "@/types/course";
import type { AdminUser, PlatformStats } from "@/types/admin";

export async function getUsers(params: {
  page?: number;
  search?: string;
}): Promise<PagedResult<AdminUser>> {
  const query = new URLSearchParams({ page: String(params.page ?? 1), pageSize: "20" });
  if (params.search) query.set("search", params.search);

  const result = await serverApiFetch<PagedResult<AdminUser>>(
    `/api/admin/users?${query.toString()}`
  );
  return result ?? { items: [], page: 1, pageSize: 20, totalCount: 0 };
}

export function getPlatformStats() {
  return serverApiFetch<PlatformStats>("/api/admin/stats");
}
