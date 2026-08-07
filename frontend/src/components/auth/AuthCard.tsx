import Link from "next/link";
import { GraduationCap } from "lucide-react";

export function AuthCard({
  heading,
  subheading,
  children,
}: {
  heading: string;
  subheading: string;
  children: React.ReactNode;
}) {
  return (
    <div className="w-full max-w-sm rounded-xl border border-border bg-card p-8">
      <Link href="/" className="mb-6 flex items-center gap-2 font-semibold">
        <span className="flex size-7 items-center justify-center rounded-md bg-primary text-primary-foreground">
          <GraduationCap className="size-4" />
        </span>
        LearnHub
      </Link>

      <h1 className="text-xl font-semibold tracking-tight">{heading}</h1>
      <p className="mb-6 text-sm text-muted-foreground">{subheading}</p>

      {children}
    </div>
  );
}
