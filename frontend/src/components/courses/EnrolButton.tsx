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
  hasAccess,
}: {
  courseId: number;
  isLoggedIn: boolean;
  hasAccess: boolean;
}) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (hasAccess) {
    return (
      <Link
        href={`/courses/${courseId}/learn`}
        className={buttonVariants({ className: "w-full" })}
      >
        Continue
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
