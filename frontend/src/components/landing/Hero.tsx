import Link from "next/link";

import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";

export function Hero() {
  return (
    <section className="bg-hero">
      <div className="mx-auto flex max-w-6xl flex-col items-center gap-6 px-4 py-20 text-center sm:px-6 sm:py-28">
        <Badge variant="secondary" className="h-auto rounded-full px-3 py-1">
          Every course, free — no paywalls
        </Badge>

        <h1 className="max-w-2xl text-4xl font-semibold tracking-tight text-balance sm:text-6xl">
          Learn anything. Free, forever.
        </h1>

        <p className="max-w-md text-base text-muted-foreground sm:text-lg">
          Courses built by working instructors. Enrol at your own pace, and earn a
          certificate when you finish.
        </p>

        <div className="flex flex-col gap-3 sm:flex-row">
          <Link href="/courses" className={buttonVariants({ size: "lg" })}>
            Browse courses
          </Link>
          <Link
            href="/become-instructor"
            className={buttonVariants({ variant: "outline", size: "lg" })}
          >
            Become an instructor
          </Link>
        </div>
      </div>
    </section>
  );
}
