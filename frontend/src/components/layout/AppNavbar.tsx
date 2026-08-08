import Link from "next/link";
import { GraduationCap } from "lucide-react";

import { LogoutButton } from "@/components/layout/LogoutButton";
import { Role, type UserResponse } from "@/types/auth";
import { initials } from "@/lib/utils";

const navLinks = [
  { href: "/dashboard", label: "Dashboard" },
  { href: "/my-courses", label: "My courses" },
  { href: "/courses", label: "Browse" },
  { href: "/messages", label: "Messages" },
];

export function AppNavbar({ user }: { user: UserResponse }) {
  return (
    <header className="border-b border-border bg-background">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6">
        <div className="flex items-center gap-8">
          <Link href="/dashboard" className="flex items-center gap-2 font-semibold">
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
            {user.role === Role.Instructor ? (
              <Link href="/instructor/dashboard" className="transition-colors hover:text-foreground">
                Teach
              </Link>
            ) : null}
          </nav>
        </div>

        <div className="flex items-center gap-4">
          <LogoutButton />
          <Link
            href="/account"
            className="flex size-8 items-center justify-center rounded-full bg-secondary text-xs font-medium text-secondary-foreground"
            title={user.username}
          >
            {initials(user.username)}
          </Link>
        </div>
      </div>
    </header>
  );
}
