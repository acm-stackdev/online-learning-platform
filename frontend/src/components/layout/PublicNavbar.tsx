import Link from "next/link";
import { GraduationCap } from "lucide-react";

import { buttonVariants } from "@/components/ui/button";
import { UserMenu } from "@/components/layout/UserMenu";
import { ThemeToggle } from "@/components/theme-toggle";
import { getCurrentUser } from "@/lib/api/me";
import { Role } from "@/types/auth";

const baseNavLinks = [{ href: "/courses", label: "Courses" }];

const teachLink = { href: "/become-instructor", label: "Teach" };

export async function PublicNavbar() {
  const user = await getCurrentUser();
  const isAdmin = user?.role === Role.Admin;

  const navLinks = isAdmin ? baseNavLinks : [...baseNavLinks, teachLink];

  return (
    <header className="border-b border-border bg-background">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6">
        <Link href="/" className="flex items-center gap-2 font-semibold">
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
        </nav>

        <div className="flex items-center gap-3">
          <ThemeToggle />
          {user ? (
            <>
              <Link
                href={isAdmin ? "/admin" : "/dashboard"}
                className={buttonVariants({ size: "sm" })}
              >
                {isAdmin ? "Admin" : "Dashboard"}
              </Link>
              <UserMenu user={user} />
            </>
          ) : (
            <>
              <Link
                href="/login"
                className="text-sm font-medium text-muted-foreground transition-colors hover:text-foreground"
              >
                Log in
              </Link>
              <Link href="/register" className={buttonVariants({ size: "sm" })}>
                Sign up free
              </Link>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
