"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";

import { Button, buttonVariants } from "@/components/ui/button";
import { enrol } from "@/lib/api/enrollments";
import { ApiError } from "@/lib/api/client";

export function EnrolButton({
  courseId,
  isLoggedIn,
  isEnrolled,
  isOwner,
  isAdmin,
}: {
  courseId: number;
  isLoggedIn: boolean;
  isEnrolled: boolean;
  isOwner: boolean;
  isAdmin: boolean;
}) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (isEnrolled) {
    return (
      <Link
        href={`/courses/${courseId}/learn`}
        className={buttonVariants({ className: "w-full" })}
      >
        Continue
      </Link>
    );
  }

  if (isOwner) {
    return (
      <Link
        href={`/instructor/courses/${courseId}/edit`}
        className={buttonVariants({ variant: "outline", className: "w-full" })}
      >
        Edit course
      </Link>
    );
  }

  if (isAdmin) {
    return (
      <Link
        href={`/courses/${courseId}/learn`}
        className={buttonVariants({ variant: "outline", className: "w-full" })}
      >
        Preview content
      </Link>
    );
  }

  if (!isLoggedIn) {
    return (
      <Link href="/login" className={buttonVariants({ className: "w-full" })}>
        Enrol now
      </Link>
    );
  }

  async function handleEnrol() {
    setLoading(true);
    setError(null);
    try {
      await enrol(courseId);
      router.refresh();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
      setLoading(false);
    }
  }

  return (
    <div className="space-y-2">
      <Button onClick={handleEnrol} disabled={loading} className="w-full">
        {loading ? "Enrolling..." : "Enrol now"}
      </Button>
      {error ? <p className="text-sm text-destructive">{error}</p> : null}
    </div>
  );
}
