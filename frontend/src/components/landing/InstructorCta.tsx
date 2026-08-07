import Link from "next/link";

import { buttonVariants } from "@/components/ui/button";

export function InstructorCta() {
  return (
    <section className="border-t border-border bg-muted/40">
      <div className="mx-auto flex max-w-6xl flex-col items-start gap-4 px-4 py-16 sm:px-6">
        <h2 className="text-2xl font-semibold tracking-tight">Teach on LearnHub</h2>
        <p className="max-w-md text-sm text-muted-foreground">
          Apply once, get approved, then publish courses with video and documents.
          Your students, your curriculum, no revenue share to think about.
        </p>
        <Link
          href="/become-instructor"
          className={buttonVariants({ variant: "outline" })}
        >
          Apply to teach
        </Link>
      </div>
    </section>
  );
}
