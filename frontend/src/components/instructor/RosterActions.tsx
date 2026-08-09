"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Trash2 } from "lucide-react";

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
import { unenrol } from "@/lib/api/enrollments";
import { ApiError } from "@/lib/api/client";

export function RosterActions({
  enrollmentId,
  studentUsername,
}: {
  enrollmentId: number;
  studentUsername: string;
}) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleRemove() {
    setLoading(true);
    setError(null);
    try {
      await unenrol(enrollmentId);
      router.refresh();
      setOpen(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <Button variant="ghost" size="icon-sm" onClick={() => setOpen(true)} aria-label="Remove student">
        <Trash2 className="size-4" />
      </Button>

      <AlertDialog open={open} onOpenChange={setOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove {studentUsername} from this course?</AlertDialogTitle>
            <AlertDialogDescription>
              This deletes their progress, certificate (if issued), and your message history
              with them for this course. This can&apos;t be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          {error ? <p className="text-sm text-destructive">{error}</p> : null}
          <AlertDialogFooter>
            <AlertDialogCancel disabled={loading}>Cancel</AlertDialogCancel>
            <AlertDialogAction variant="destructive" disabled={loading} onClick={handleRemove}>
              {loading ? "Removing..." : "Remove"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
