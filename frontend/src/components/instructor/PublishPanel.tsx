"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

import { Button } from "@/components/ui/button";
import { CourseStatusBadge } from "@/components/instructor/CourseStatusBadge";
import { submitForReview, unpublishCourse } from "@/lib/api/course-builder";
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
    </div>
  );
}
