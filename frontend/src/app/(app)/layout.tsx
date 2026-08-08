import { redirect } from "next/navigation";

import { AppNavbar } from "@/components/layout/AppNavbar";
import { SessionRefresher } from "@/components/auth/SessionRefresher";
import { getCurrentUser } from "@/lib/api/me";

export default async function AppLayout({ children }: { children: React.ReactNode }) {
  const user = await getCurrentUser();

  // Belt-and-braces: middleware already redirects unauthenticated requests
  // for these routes, this just keeps the layout safe to reuse elsewhere.
  if (!user) redirect("/login");

  return (
    <div className="flex flex-1 flex-col">
      <SessionRefresher />
      <AppNavbar user={user} />
      <main className="flex-1">{children}</main>
    </div>
  );
}
