"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";

import { Button, buttonVariants } from "@/components/ui/button";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { forceUnpublishCourse } from "@/lib/api/admin-actions";
import { ApiError } from "@/lib/api/client";
import type { CourseListItem } from "@/types/course";

export function PublishedCourseRow({ course }: { course: CourseListItem }) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);

  async function handleForceUnpublish() {
    setLoading(true);
    setError(null);
    try {
      await forceUnpublishCourse(course.id);
      router.refresh();
      setConfirmOpen(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
    } finally {
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
          variant="destructive"
          size="sm"
          disabled={loading}
          onClick={() => setConfirmOpen(true)}
        >
          Force unpublish
        </Button>
      </div>

      <AlertDialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Force unpublish this course?</AlertDialogTitle>
            <AlertDialogDescription>
              &ldquo;{course.title}&rdquo; will be taken off the catalogue immediately and moved
              back to Draft. Enrolled students will lose access until the instructor republishes it.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={loading}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              disabled={loading}
              onClick={handleForceUnpublish}
            >
              {loading ? "Unpublishing..." : "Force unpublish"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
