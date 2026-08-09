"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { changeUserRole, reinstateUser, suspendUser } from "@/lib/api/admin-actions";
import { ApiError } from "@/lib/api/client";
import { Role, roleLabels } from "@/types/auth";
import type { AdminUser } from "@/types/admin";

export function UserRow({
  user,
  isSelf,
}: {
  user: AdminUser;
  isSelf: boolean;
}) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleRoleChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const role = Number(e.target.value) as Role;
    if (role === user.role) return;
    setLoading(true);
    setError(null);
    try {
      await changeUserRole(user.id, role);
      router.refresh();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
    } finally {
      setLoading(false);
    }
  }

  async function handleSuspendToggle() {
    setLoading(true);
    setError(null);
    try {
      await (user.isSuspended ? reinstateUser : suspendUser)(user.id);
      router.refresh();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <tr className="border-b border-border last:border-b-0">
      <td className="px-4 py-3">
        <p className="font-medium">{user.username}</p>
        <p className="text-xs text-muted-foreground">{user.email}</p>
        {error ? <p className="text-xs text-destructive">{error}</p> : null}
      </td>
      <td className="px-4 py-3">
        <select
          value={user.role}
          onChange={handleRoleChange}
          disabled={loading || isSelf}
          className="h-8 rounded-lg border border-input bg-transparent px-2 text-sm outline-none disabled:opacity-50"
        >
          {Object.entries(roleLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </td>
      <td className="px-4 py-3">
        {user.isSuspended ? (
          <Badge variant="destructive">Suspended</Badge>
        ) : (
          <Badge variant="outline">Active</Badge>
        )}
      </td>
      <td className="px-4 py-3 text-right">
        <Button
          variant="outline"
          size="sm"
          disabled={loading || isSelf}
          onClick={handleSuspendToggle}
        >
          {user.isSuspended ? "Reinstate" : "Suspend"}
        </Button>
      </td>
    </tr>
  );
}
