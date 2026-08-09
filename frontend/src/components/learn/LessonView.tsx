"use client";

import { useRef, useState } from "react";
import Link from "next/link";
import { Check } from "lucide-react";

import { Button, buttonVariants } from "@/components/ui/button";
import { updateLessonProgress } from "@/lib/api/lessons";
import { ContentType, type Lesson } from "@/types/course";

const PROGRESS_SAVE_INTERVAL_S = 10;

export function LessonView({
  courseId,
  lesson,
  nextLessonId,
  initialIsCompleted,
  isAdminView = false,
}: {
  courseId: number;
  lesson: Lesson;
  nextLessonId: number | null;
  initialIsCompleted: boolean;
  isAdminView?: boolean;
}) {
  const [isCompleted, setIsCompleted] = useState(initialIsCompleted);
  const [marking, setMarking] = useState(false);
  const lastSavedAt = useRef(0);

  async function markComplete(watchSeconds: number) {
    if (isAdminView) return;
    setIsCompleted(true);
    try {
      await updateLessonProgress(lesson.id, { watchSeconds, isCompleted: true });
    } catch {
      // Best-effort — the mark-complete button lets them retry manually.
    }
  }

  function handleTimeUpdate(e: React.SyntheticEvent<HTMLVideoElement>) {
    if (isAdminView) return;
    const currentTime = Math.floor(e.currentTarget.currentTime);
    if (currentTime - lastSavedAt.current < PROGRESS_SAVE_INTERVAL_S) return;
    lastSavedAt.current = currentTime;
    updateLessonProgress(lesson.id, { watchSeconds: currentTime, isCompleted }).catch(() => {
      // Non-critical — the next timeupdate tick or the mark-complete button covers it.
    });
  }

  async function handleMarkCompleteClick() {
    setMarking(true);
    await markComplete(lesson.duration);
    setMarking(false);
  }

  return (
    <div className="space-y-4">
      <div className="aspect-video w-full overflow-hidden rounded-lg bg-black">
        {lesson.contentType === ContentType.Video ? (
          <video
            key={lesson.id}
            src={lesson.contentUrl ?? undefined}
            controls
            className="size-full"
            onTimeUpdate={handleTimeUpdate}
            onEnded={() => markComplete(lesson.duration)}
          />
        ) : (
          <iframe
            key={lesson.id}
            src={lesson.contentUrl ?? undefined}
            className="size-full bg-white"
            title={lesson.title}
          />
        )}
      </div>

      <div className="flex items-center justify-between">
        <h1 className="text-lg font-semibold tracking-tight">{lesson.title}</h1>

        <div className="flex items-center gap-2">
          {isAdminView ? (
            <span className="text-sm text-muted-foreground">Admin preview</span>
          ) : isCompleted ? (
            <span className="flex items-center gap-1.5 text-sm text-muted-foreground">
              <Check className="size-4 text-primary" />
              Completed
            </span>
          ) : (
            <Button onClick={handleMarkCompleteClick} disabled={marking} variant="outline">
              {marking ? "Marking..." : "Mark complete"}
            </Button>
          )}

          {nextLessonId ? (
            <Link
              href={`/courses/${courseId}/learn/${nextLessonId}`}
              className={buttonVariants({})}
            >
              Next lesson
            </Link>
          ) : (
            <Link href={`/courses/${courseId}`} className={buttonVariants({})}>
              Back to course
            </Link>
          )}
        </div>
      </div>
    </div>
  );
}
