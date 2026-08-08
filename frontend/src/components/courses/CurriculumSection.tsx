"use client";

import { useState } from "react";
import { ChevronDown, FileText, Lock, PlayCircle } from "lucide-react";

import { cn, formatDuration } from "@/lib/utils";
import { ContentType, type Section } from "@/types/course";

export function CurriculumSection({
  section,
  defaultOpen = false,
}: {
  section: Section;
  defaultOpen?: boolean;
}) {
  const [open, setOpen] = useState(defaultOpen);
  const totalSeconds = section.lessons.reduce((sum, l) => sum + l.duration, 0);

  return (
    <div className="border-b border-border last:border-b-0">
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className="flex w-full items-center justify-between py-3 text-left"
      >
        <span className="text-sm font-medium">
          {section.order}. {section.title}
        </span>
        <span className="flex items-center gap-2 text-xs text-muted-foreground">
          {section.lessons.length} lessons &middot; {formatDuration(totalSeconds)}
          <ChevronDown
            className={cn("size-4 transition-transform", open && "rotate-180")}
          />
        </span>
      </button>

      {open ? (
        <ul className="space-y-2 pb-3">
          {section.lessons.map((lesson) => {
            const locked = lesson.contentUrl === null;
            const Icon = locked
              ? Lock
              : lesson.contentType === ContentType.Pdf
                ? FileText
                : PlayCircle;

            return (
              <li
                key={lesson.id}
                className="flex items-center justify-between gap-2 text-sm text-muted-foreground"
              >
                <span className="flex items-center gap-2">
                  <Icon className="size-4 shrink-0" />
                  {lesson.title}
                </span>
                <span className="shrink-0 text-xs">{formatDuration(lesson.duration)}</span>
              </li>
            );
          })}
        </ul>
      ) : null}
    </div>
  );
}
