"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";

import { Button, buttonVariants } from "@/components/ui/button";
import { approveCourse, rejectCourse } from "@/lib/api/admin-actions";
import { ApiError } from "@/lib/api/client";
import type { CourseListItem } from "@/types/course";

export function CourseReviewRow({ course }: { course: CourseListItem }) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handle(action: "approve" | "reject") {
    setLoading(true);
    setError(null);
    try {
      await (action === "approve" ? approveCourse : rejectCourse)(course.id);
      router.refresh();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
      setLoading(false);
    }
  }

  return (
    <div className="flex items-center justify-between gap-4 rounded-lg border border-border p-4">
      <div className="min-w-0 flex-1">
        <Link
          href={`/courses/${course.id}`}
          target="_blank"
          className="font-medium hover:underline"
        >
          {course.title}
        </Link>
        <p className="text-sm text-muted-foreground">
          {course.instructorName} &middot; {course.category ?? "Uncategorised"}
        </p>
        {error ? <p className="mt-1 text-xs text-destructive">{error}</p> : null}
      </div>

      <div className="flex shrink-0 items-center gap-2">
        <Link
          href={`/courses/${course.id}`}
          target="_blank"
          className={buttonVariants({ variant: "outline", size: "sm" })}
        >
          Open
        </Link>
        <Button
          variant="outline"
          size="sm"
          disabled={loading}
          onClick={() => handle("reject")}
        >
          Reject
        </Button>
        <Button size="sm" disabled={loading} onClick={() => handle("approve")}>
          Approve
        </Button>
      </div>
    </div>
  );
}
