"use client";

import { useState } from "react";
import Link from "next/link";
import { LogOut, Settings } from "lucide-react";

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Badge } from "@/components/ui/badge";
import { apiFetch } from "@/lib/api/client";
import { hardNavigate } from "@/lib/hard-navigate";
import { initials } from "@/lib/utils";
import { roleLabels, type UserResponse } from "@/types/auth";

export function UserMenu({ user }: { user: UserResponse }) {
  const [loggingOut, setLoggingOut] = useState(false);

  async function handleLogout() {
    setLoggingOut(true);
    try {
      await apiFetch("/api/auth/logout", { method: "POST" });
    } finally {
      hardNavigate("/");
    }
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        className="flex size-8 items-center justify-center rounded-full bg-secondary text-xs font-medium text-secondary-foreground outline-none"
        title={user.username}
      >
        {initials(user.username)}
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" className="w-64">
        <div className="space-y-1 px-1.5 py-1.5">
          <div className="flex items-center gap-2">
            <p className="truncate text-sm font-medium">{user.username}</p>
            <Badge variant="secondary">{roleLabels[user.role]}</Badge>
          </div>
          <p className="truncate text-xs text-muted-foreground">{user.email}</p>
        </div>

        <DropdownMenuSeparator />

        <DropdownMenuItem render={<Link href="/account" />}>
          <Settings />
          Account settings
        </DropdownMenuItem>

        <DropdownMenuSeparator />

        <DropdownMenuItem
          variant="destructive"
          disabled={loggingOut}
          onClick={handleLogout}
        >
          <LogOut />
          {loggingOut ? "Logging out..." : "Log out"}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
