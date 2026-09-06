"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { ChevronDown, ChevronUp, Pencil, Plus, Trash2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
import { LessonFormDialog } from "@/components/instructor/LessonFormDialog";
import { SectionRenameDialog } from "@/components/instructor/SectionRenameDialog";
import {
  createSection,
  deleteLesson,
  deleteSection,
  reorderLessons,
  reorderSections,
} from "@/lib/api/course-builder";
import { ApiError } from "@/lib/api/client";
import { formatDuration } from "@/lib/utils";
import type { Lesson, Section } from "@/types/course";

export function CurriculumEditor({
  courseId,
  sections,
}: {
  courseId: number;
  sections: Section[];
}) {
  const router = useRouter();
  const [newSectionTitle, setNewSectionTitle] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [lessonDialog, setLessonDialog] = useState<{
    sectionId: number;
    lesson?: Lesson;
  } | null>(null);
  const [renameSection, setRenameSection] = useState<Section | null>(null);
  const [pendingDelete, setPendingDelete] = useState<
    { type: "section"; id: number } | { type: "lesson"; id: number } | null
  >(null);
  const [deleting, setDeleting] = useState(false);
  const [reordering, setReordering] = useState(false);

  function reportError(err: unknown) {
    setError(err instanceof ApiError ? err.message : "Something went wrong.");
  }

  async function handleAddSection(e: React.FormEvent) {
    e.preventDefault();
    if (!newSectionTitle.trim()) return;
    setError(null);
    try {
      await createSection(courseId, newSectionTitle.trim());
      setNewSectionTitle("");
      router.refresh();
    } catch (err) {
      reportError(err);
    }
  }

  async function handleMoveSection(index: number, direction: -1 | 1) {
    if (reordering) return;
    const target = index + direction;
    if (target < 0 || target >= sections.length) return;
    const ids = sections.map((s) => s.id);
    [ids[index], ids[target]] = [ids[target], ids[index]];
    setError(null);
    setReordering(true);
    try {
      await reorderSections(courseId, ids);
      router.refresh();
    } catch (err) {
      reportError(err);
    } finally {
      setReordering(false);
    }
  }

  async function handleConfirmDelete() {
    if (!pendingDelete) return;
    setError(null);
    setDeleting(true);
    try {
      if (pendingDelete.type === "section") {
        await deleteSection(pendingDelete.id);
      } else {
        await deleteLesson(pendingDelete.id);
      }
      router.refresh();
      setPendingDelete(null);
    } catch (err) {
      reportError(err);
    } finally {
      setDeleting(false);
    }
  }

  async function handleMoveLesson(section: Section, index: number, direction: -1 | 1) {
    if (reordering) return;
    const target = index + direction;
    if (target < 0 || target >= section.lessons.length) return;
    const ids = section.lessons.map((l) => l.id);
    [ids[index], ids[target]] = [ids[target], ids[index]];
    setError(null);
    setReordering(true);
    try {
      await reorderLessons(section.id, ids);
      router.refresh();
    } catch (err) {
      reportError(err);
    } finally {
      setReordering(false);
    }
  }

  return (
    <div className="space-y-6">
      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      {sections.map((section, sIndex) => (
        <div key={section.id} className="rounded-lg border border-border p-4">
          <div className="mb-3 flex items-center justify-between">
            <p className="font-medium">
              {section.order}. {section.title}
            </p>
            <div className="flex items-center gap-1">
              <Button
                variant="ghost"
                size="icon-sm"
                onClick={() => handleMoveSection(sIndex, -1)}
                disabled={sIndex === 0 || reordering}
              >
                <ChevronUp className="size-4" />
              </Button>
              <Button
                variant="ghost"
                size="icon-sm"
                onClick={() => handleMoveSection(sIndex, 1)}
                disabled={sIndex === sections.length - 1 || reordering}
              >
                <ChevronDown className="size-4" />
              </Button>
              <Button variant="ghost" size="icon-sm" onClick={() => setRenameSection(section)}>
                <Pencil className="size-4" />
              </Button>
              <Button
                variant="ghost"
                size="icon-sm"
                onClick={() => setPendingDelete({ type: "section", id: section.id })}
              >
                <Trash2 className="size-4" />
              </Button>
            </div>
          </div>

          <ul className="space-y-1">
            {section.lessons.map((lesson, lIndex) => (
              <li
                key={lesson.id}
                className="flex items-center justify-between gap-2 rounded-md px-2 py-1.5 text-sm hover:bg-muted"
              >
                <span className="flex-1 truncate">{lesson.title}</span>
                <span className="shrink-0 text-xs text-muted-foreground">
                  {formatDuration(lesson.duration)}
                </span>
                <div className="flex shrink-0 items-center gap-1">
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    onClick={() => handleMoveLesson(section, lIndex, -1)}
                    disabled={lIndex === 0 || reordering}
                  >
                    <ChevronUp className="size-3.5" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    onClick={() => handleMoveLesson(section, lIndex, 1)}
                    disabled={lIndex === section.lessons.length - 1 || reordering}
                  >
                    <ChevronDown className="size-3.5" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    onClick={() => setLessonDialog({ sectionId: section.id, lesson })}
                  >
                    <Pencil className="size-3.5" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    onClick={() => setPendingDelete({ type: "lesson", id: lesson.id })}
                  >
                    <Trash2 className="size-3.5" />
                  </Button>
                </div>
              </li>
            ))}
          </ul>

          <Button
            variant="outline"
            size="sm"
            className="mt-2"
            onClick={() => setLessonDialog({ sectionId: section.id })}
          >
            <Plus className="size-4" />
            Add lesson
          </Button>
        </div>
      ))}

      <form onSubmit={handleAddSection} className="flex gap-2">
        <Input
          value={newSectionTitle}
          onChange={(e) => setNewSectionTitle(e.target.value)}
          placeholder="New section title"
        />
        <Button type="submit">
          <Plus className="size-4" />
          Add section
        </Button>
      </form>

      {lessonDialog ? (
        <LessonFormDialog
          sectionId={lessonDialog.sectionId}
          lesson={lessonDialog.lesson}
          open
          onOpenChange={(open) => !open && setLessonDialog(null)}
        />
      ) : null}

      {renameSection ? (
        <SectionRenameDialog
          section={renameSection}
          open
          onOpenChange={(open) => !open && setRenameSection(null)}
        />
      ) : null}

      <AlertDialog
        open={pendingDelete !== null}
        onOpenChange={(open) => !open && setPendingDelete(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {pendingDelete?.type === "section" ? "Delete section?" : "Delete lesson?"}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {pendingDelete?.type === "section"
                ? "This will also delete every lesson inside it. This can't be undone."
                : "This can't be undone."}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleting}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              disabled={deleting}
              onClick={handleConfirmDelete}
            >
              {deleting ? "Deleting..." : "Delete"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
