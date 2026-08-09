import type { Metadata } from "next";

import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { UserRow } from "@/components/admin/UserRow";
import { Pagination } from "@/components/courses/Pagination";
import { getUsers } from "@/lib/api/admin";
import { getCurrentUser } from "@/lib/api/me";

export const metadata: Metadata = {
  title: "Users — LearnHub",
};

export default async function AdminUsersPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string; search?: string }>;
}) {
  const { page: pageParam, search } = await searchParams;
  const page = Math.max(1, Number(pageParam) || 1);

  const [currentUser, result] = await Promise.all([
    getCurrentUser(),
    getUsers({ page, search }),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Users</h1>
        <p className="text-sm text-muted-foreground">{result.totalCount} users</p>
      </div>

      <form className="flex gap-2">
        <Input
          type="search"
          name="search"
          defaultValue={search}
          placeholder="Search by username or email"
          className="max-w-md"
        />
        <Button type="submit">Search</Button>
      </form>

      {result.items.length > 0 ? (
        <div className="overflow-hidden rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="border-b border-border bg-muted/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-2 font-medium">User</th>
                <th className="px-4 py-2 font-medium">Role</th>
                <th className="px-4 py-2 font-medium">Status</th>
                <th className="px-4 py-2 font-medium" />
              </tr>
            </thead>
            <tbody>
              {result.items.map((user) => (
                <UserRow key={user.id} user={user} isSelf={user.id === currentUser?.id} />
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">No users match your search.</p>
      )}

      <Pagination
        basePath="/admin/users"
        page={result.page}
        pageSize={result.pageSize}
        totalCount={result.totalCount}
        search={search}
      />
    </div>
  );
}
