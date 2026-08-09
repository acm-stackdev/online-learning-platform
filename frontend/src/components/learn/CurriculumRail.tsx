import Link from "next/link";
import { Check, PlayCircle } from "lucide-react";

import { cn, formatDuration } from "@/lib/utils";
import type { Section } from "@/types/course";

export function CurriculumRail({
  courseId,
  sections,
  currentLessonId,
  completedIds,
}: {
  courseId: number;
  sections: Section[];
  currentLessonId: number;
  completedIds: Set<number>;
}) {
  return (
    <nav className="space-y-4">
      {sections.map((section) => (
        <div key={section.id}>
          <p className="mb-1 text-xs font-medium uppercase tracking-wide text-muted-foreground">
            {section.order}. {section.title}
          </p>
          <ul className="space-y-0.5">
            {section.lessons.map((lesson) => {
              const isCurrent = lesson.id === currentLessonId;
              const isCompleted = completedIds.has(lesson.id);

              return (
                <li key={lesson.id}>
                  <Link
                    href={`/courses/${courseId}/learn/${lesson.id}`}
                    className={cn(
                      "flex items-center gap-2 rounded-md px-2 py-1.5 text-sm",
                      isCurrent
                        ? "bg-accent text-accent-foreground"
                        : "text-muted-foreground hover:bg-muted"
                    )}
                  >
                    {isCompleted ? (
                      <Check className="size-4 shrink-0 text-primary" />
                    ) : (
                      <PlayCircle className="size-4 shrink-0" />
                    )}
                    <span className="flex-1 truncate">{lesson.title}</span>
                    <span className="shrink-0 text-xs">
                      {formatDuration(lesson.duration)}
                    </span>
                  </Link>
                </li>
              );
            })}
          </ul>
        </div>
      ))}
    </nav>
  );
}
