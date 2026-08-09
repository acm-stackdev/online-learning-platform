import Link from "next/link";
import { GraduationCap } from "lucide-react";

import { UserMenu } from "@/components/layout/UserMenu";
import { ThemeToggle } from "@/components/theme-toggle";
import { getConversations } from "@/lib/api/messaging";
import { Role, type UserResponse } from "@/types/auth";

const studentInstructorLinks = [
  { href: "/dashboard", label: "Dashboard" },
  { href: "/my-courses", label: "My courses" },
  { href: "/courses", label: "Browse" },
];

const adminLinks = [{ href: "/courses", label: "Browse" }];

export async function AppNavbar({ user }: { user: UserResponse }) {
  const isAdmin = user.role === Role.Admin;
  const navLinks = isAdmin ? adminLinks : studentInstructorLinks;
  const homeHref = isAdmin ? "/admin" : "/dashboard";

  const totalUnread = !isAdmin
    ? (await getConversations()).reduce((sum, c) => sum + c.unreadCount, 0)
    : 0;

  return (
    <header className="border-b border-border bg-background">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6">
        <div className="flex items-center gap-8">
          <Link href={homeHref} className="flex items-center gap-2 font-semibold">
            <span className="flex size-7 items-center justify-center rounded-md bg-primary text-primary-foreground">
              <GraduationCap className="size-4" />
            </span>
            LearnHub
          </Link>

          <nav className="hidden items-center gap-6 text-sm text-muted-foreground md:flex">
            {navLinks.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className="transition-colors hover:text-foreground"
              >
                {link.label}
              </Link>
            ))}
            {!isAdmin ? (
              <Link
                href="/messages"
                className="flex items-center gap-1.5 transition-colors hover:text-foreground"
              >
                Messages
                {totalUnread > 0 ? (
                  <span className="flex size-4 items-center justify-center rounded-full bg-primary text-[10px] font-medium text-primary-foreground">
                    {totalUnread > 9 ? "9+" : totalUnread}
                  </span>
                ) : null}
              </Link>
            ) : null}
            {user.role === Role.Instructor ? (
              <Link href="/instructor/dashboard" className="transition-colors hover:text-foreground">
                Teach
              </Link>
            ) : null}
            {isAdmin ? (
              <Link href="/admin" className="transition-colors hover:text-foreground">
                Admin
              </Link>
            ) : null}
          </nav>
        </div>

        <div className="flex items-center gap-2">
          <ThemeToggle />
          <UserMenu user={user} />
        </div>
      </div>
    </header>
  );
}
