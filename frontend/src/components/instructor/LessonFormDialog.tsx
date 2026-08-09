"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { createLesson, updateLesson } from "@/lib/api/course-builder";
import { ApiError } from "@/lib/api/client";
import { ContentType, type Lesson } from "@/types/course";

export function LessonFormDialog({
  sectionId,
  lesson,
  open,
  onOpenChange,
}: {
  sectionId: number;
  lesson?: Lesson;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const router = useRouter();
  const [title, setTitle] = useState(lesson?.title ?? "");
  const [contentType, setContentType] = useState<ContentType>(
    lesson?.contentType ?? ContentType.Video
  );
  const [minutes, setMinutes] = useState(
    lesson ? Math.floor(lesson.duration / 60) : 0
  );
  const [seconds, setSeconds] = useState(lesson ? lesson.duration % 60 : 0);
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const isEdit = Boolean(lesson);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (!isEdit && !file) {
      setError("Choose a file to upload.");
      return;
    }

    const duration = minutes * 60 + seconds;

    setSaving(true);
    try {
      if (isEdit && lesson) {
        await updateLesson(lesson.id, {
          title,
          duration,
          file: file ?? undefined,
        });
      } else if (file) {
        await createLesson({ sectionId, title, contentType, duration, file });
      }
      router.refresh();
      onOpenChange(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEdit ? "Edit lesson" : "Add lesson"}</DialogTitle>
          <DialogDescription>
            {isEdit
              ? "Update the title, duration, or replace the file."
              : "Upload a video or document for this lesson."}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="lesson-title">Title</Label>
            <Input
              id="lesson-title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
              minLength={1}
            />
          </div>

          {!isEdit ? (
            <div className="space-y-1.5">
              <Label htmlFor="lesson-type">Content type</Label>
              <select
                id="lesson-type"
                value={contentType}
                onChange={(e) => setContentType(Number(e.target.value) as ContentType)}
                className="h-8 w-full rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
              >
                <option value={ContentType.Video}>Video</option>
                <option value={ContentType.Pdf}>Document (PDF)</option>
              </select>
            </div>
          ) : null}

          <div className="space-y-1.5">
            <Label>Duration</Label>
            <div className="flex items-center gap-2">
              <Input
                type="number"
                min={0}
                value={minutes}
                onChange={(e) => setMinutes(Number(e.target.value))}
                className="w-20"
              />
              <span className="text-sm text-muted-foreground">min</span>
              <Input
                type="number"
                min={0}
                max={59}
                value={seconds}
                onChange={(e) => setSeconds(Number(e.target.value))}
                className="w-20"
              />
              <span className="text-sm text-muted-foreground">sec</span>
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="lesson-file">
              {isEdit ? "Replace file (optional)" : "File"}
            </Label>
            <Input
              id="lesson-file"
              type="file"
              accept={contentType === ContentType.Pdf ? ".pdf" : "video/*"}
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            />
            <p className="text-xs text-muted-foreground">Up to 500MB.</p>
          </div>

          {error ? <p className="text-sm text-destructive">{error}</p> : null}

          <Button type="submit" disabled={saving} className="w-full">
            {saving ? "Uploading..." : isEdit ? "Save changes" : "Add lesson"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
}
