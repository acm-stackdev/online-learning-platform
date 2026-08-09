"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

import { Button } from "@/components/ui/button";
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
import { CourseStatusBadge } from "@/components/instructor/CourseStatusBadge";
import { deleteCourse, submitForReview, unpublishCourse } from "@/lib/api/course-builder";
import { ApiError } from "@/lib/api/client";
import { CourseStatus } from "@/types/course";

export function PublishPanel({
  courseId,
  status,
}: {
  courseId: number;
  status: CourseStatus;
}) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false);

  async function handleSubmit() {
    setLoading(true);
    setError(null);
    try {
      await submitForReview(courseId);
      router.refresh();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
    } finally {
      setLoading(false);
    }
  }

  async function handleUnpublish() {
    setLoading(true);
    setError(null);
    try {
      await unpublishCourse(courseId);
      router.refresh();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
    } finally {
      setLoading(false);
    }
  }

  async function handleDelete() {
    setLoading(true);
    setError(null);
    try {
      await deleteCourse(courseId);
      router.push("/instructor/dashboard");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
      setLoading(false);
    }
  }

  return (
    <div className="flex items-center gap-3 rounded-lg border border-border p-4">
      <CourseStatusBadge status={status} />

      <div className="flex-1" />

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      {status === CourseStatus.Draft || status === CourseStatus.Rejected ? (
        <Button onClick={handleSubmit} disabled={loading}>
          {loading ? "Submitting..." : "Submit for review"}
        </Button>
      ) : null}

      {status === CourseStatus.Published ? (
        <Button variant="outline" onClick={handleUnpublish} disabled={loading}>
          {loading ? "Unpublishing..." : "Unpublish"}
        </Button>
      ) : null}

      {status !== CourseStatus.Published ? (
        <Button
          variant="destructive"
          disabled={loading}
          onClick={() => setConfirmDeleteOpen(true)}
        >
          Delete course
        </Button>
      ) : null}

      <AlertDialog open={confirmDeleteOpen} onOpenChange={setConfirmDeleteOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this course?</AlertDialogTitle>
            <AlertDialogDescription>
              This deletes the course along with all its sections and lessons. This can&apos;t
              be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={loading}>Cancel</AlertDialogCancel>
            <AlertDialogAction variant="destructive" disabled={loading} onClick={handleDelete}>
              {loading ? "Deleting..." : "Delete"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
