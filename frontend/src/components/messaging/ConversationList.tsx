"use client";

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn, initials } from "@/lib/utils";
import type { Conversation } from "@/types/messaging";

function formatTimestamp(iso: string | null) {
  if (!iso) return "";
  const date = new Date(iso);
  const now = new Date();
  const sameDay = date.toDateString() === now.toDateString();
  return sameDay
    ? date.toLocaleTimeString([], { hour: "numeric", minute: "2-digit" })
    : date.toLocaleDateString();
}

const presenceDotClass: Record<string, string> = {
  Online: "bg-primary",
  Busy: "bg-destructive",
};

export function ConversationList({
  conversations,
  selectedEnrollmentId,
  onSelect,
  myPresence,
  onSetPresence,
}: {
  conversations: Conversation[];
  selectedEnrollmentId: number | null;
  onSelect: (enrollmentId: number) => void;
  myPresence: string;
  onSetPresence: (status: "Online" | "Busy") => void;
}) {
  return (
    <div className="flex h-full flex-col border-r border-border">
      <div className="flex items-center justify-between border-b border-border px-4 py-3">
        <h1 className="text-lg font-semibold tracking-tight">Messages</h1>

        <DropdownMenu>
          <DropdownMenuTrigger className="flex items-center gap-1.5 rounded-lg border border-border px-2 py-1 text-xs font-medium text-foreground outline-none hover:bg-muted">
            <span
              className={cn(
                "size-2 rounded-full",
                presenceDotClass[myPresence] ?? "bg-muted-foreground"
              )}
            />
            {myPresence}
          </DropdownMenuTrigger>

          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => onSetPresence("Online")}>
              <span className="size-2 rounded-full bg-primary" />
              Online
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => onSetPresence("Busy")}>
              <span className="size-2 rounded-full bg-destructive" />
              Busy
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {conversations.length === 0 ? (
        <p className="p-4 text-sm text-muted-foreground">
          No conversations yet — messages with your instructor or students will show up here.
        </p>
      ) : (
        <ul className="flex-1 overflow-y-auto">
          {conversations.map((c) => (
            <li key={c.enrollmentId}>
              <button
                type="button"
                onClick={() => onSelect(c.enrollmentId)}
                className={cn(
                  "flex w-full items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-muted",
                  selectedEnrollmentId === c.enrollmentId && "bg-accent"
                )}
              >
                <span className="relative flex size-9 shrink-0 items-center justify-center rounded-full bg-secondary text-xs font-medium text-secondary-foreground">
                  {initials(c.otherPartyUsername)}
                  <span
                    className={cn(
                      "absolute -bottom-0.5 -right-0.5 size-2.5 rounded-full ring-2 ring-background",
                      c.otherPartyPresence === "Online"
                        ? "bg-primary"
                        : c.otherPartyPresence === "Busy"
                          ? "bg-destructive"
                          : "bg-muted-foreground"
                    )}
                  />
                </span>

                <div className="min-w-0 flex-1">
                  <div className="flex items-center justify-between gap-2">
                    <p className="truncate text-sm font-medium">{c.otherPartyUsername}</p>
                    <span className="shrink-0 text-xs text-muted-foreground">
                      {formatTimestamp(c.lastMessageAt)}
                    </span>
                  </div>
                  <p className="truncate text-xs text-muted-foreground">{c.courseTitle}</p>
                  <p className="truncate text-xs text-muted-foreground">
                    {c.lastMessagePreview ?? "No messages yet"}
                  </p>
                </div>

                {c.unreadCount > 0 ? (
                  <span className="flex size-5 shrink-0 items-center justify-center rounded-full bg-primary text-[10px] font-medium text-primary-foreground">
                    {c.unreadCount}
                  </span>
                ) : null}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
