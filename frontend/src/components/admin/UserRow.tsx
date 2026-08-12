"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
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

  async function handleRoleChange(value: string | null) {
    if (value === null) return;
    const role = Number(value) as Role;
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
        <Select
          items={roleLabels}
          value={String(user.role)}
          onValueChange={handleRoleChange}
          disabled={loading || isSelf}
        >
          <SelectTrigger size="sm" className="w-fit">
            <SelectValue />
          </SelectTrigger>
          <SelectContent alignItemWithTrigger={false}>
            {Object.entries(roleLabels).map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
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
