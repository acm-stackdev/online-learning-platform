import Link from "next/link";
import { redirect } from "next/navigation";

import { getCurrentUser } from "@/lib/api/me";
import { Role } from "@/types/auth";

const navLinks = [
  { href: "/admin", label: "Overview" },
  { href: "/admin/courses", label: "Course review" },
  { href: "/admin/courses/published", label: "Published courses" },
  { href: "/admin/applications", label: "Applications" },
  { href: "/admin/users", label: "Users" },
];

export default async function AdminLayout({ children }: { children: React.ReactNode }) {
  const user = await getCurrentUser();
  if (!user) redirect("/login");
  if (user.role !== Role.Admin) redirect("/dashboard");

  return (
    <div className="mx-auto flex w-full max-w-6xl flex-1 gap-8 px-4 py-8 sm:px-6">
      <aside className="w-44 shrink-0 space-y-1">
        {navLinks.map((link) => (
          <Link
            key={link.href}
            href={link.href}
            className="block rounded-md px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
          >
            {link.label}
          </Link>
        ))}
      </aside>
      <div className="min-w-0 flex-1">{children}</div>
    </div>
  );
}
