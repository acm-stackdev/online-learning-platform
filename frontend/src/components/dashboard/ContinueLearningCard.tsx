import Image from "next/image";
import Link from "next/link";

import { Card } from "@/components/ui/card";
import { buttonVariants } from "@/components/ui/button";
import { UnenrolButton } from "@/components/dashboard/UnenrolButton";
import type { Enrollment } from "@/types/dashboard";
import type { EnrollmentProgress } from "@/types/progress";

export function ContinueLearningCard({
  enrollment,
  progress,
}: {
  enrollment: Enrollment;
  progress: EnrollmentProgress | null;
}) {
  const percent = progress?.percentComplete ?? 0;
  const started = (progress?.completedLessons ?? 0) > 0;
  const label = progress
    ? `Lesson ${progress.completedLessons} of ${progress.totalLessons}`
    : null;

  return (
    <Card className="flex-row items-center gap-4 p-4">
      <div className="relative aspect-video w-24 shrink-0 overflow-hidden rounded-md bg-muted">
        {enrollment.course.thumbnailUrl ? (
          <Image
            src={enrollment.course.thumbnailUrl}
            alt={enrollment.course.title}
            fill
            className="object-cover"
          />
        ) : null}
      </div>

      <div className="min-w-0 flex-1 space-y-1.5">
        <p className="truncate text-sm font-medium">{enrollment.course.title}</p>
        {label ? <p className="text-xs text-muted-foreground">{label}</p> : null}
        <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
          <div
            className="h-full rounded-full bg-primary"
            style={{ width: `${percent}%` }}
          />
        </div>
      </div>

      <div className="shrink-0 space-y-1 text-right">
        <p className="text-sm text-muted-foreground">{Math.round(percent)}%</p>
        <div className="flex items-center gap-1">
          <UnenrolButton enrollmentId={enrollment.id} courseTitle={enrollment.course.title} />
          <Link
            href={`/courses/${enrollment.course.id}/learn`}
            className={buttonVariants({ size: "sm" })}
          >
            {started ? "Resume" : "Start"}
          </Link>
        </div>
      </div>
    </Card>
  );
}
