import Link from "next/link";
import { GraduationCap } from "lucide-react";

export default function LearnLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col">
      <header className="border-b border-border bg-background">
        <div className="flex h-14 items-center px-4 sm:px-6">
          <Link href="/dashboard" className="flex items-center gap-2 font-semibold">
            <span className="flex size-7 items-center justify-center rounded-md bg-primary text-primary-foreground">
              <GraduationCap className="size-4" />
            </span>
            LearnHub
          </Link>
        </div>
      </header>
      <main className="flex-1">{children}</main>
    </div>
  );
}
