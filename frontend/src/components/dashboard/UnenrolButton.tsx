"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { LogOut } from "lucide-react";

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

export function UnenrolButton({
  enrollmentId,
  courseTitle,
  hasCertificate = false,
}: {
  enrollmentId: number;
  courseTitle: string;
  hasCertificate?: boolean;
}) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleUnenrol() {
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
      <Button
        variant="ghost"
        size="icon-sm"
        onClick={() => setOpen(true)}
        aria-label="Unenrol"
      >
        <LogOut className="size-4" />
      </Button>

      <AlertDialog open={open} onOpenChange={setOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Unenrol from &ldquo;{courseTitle}&rdquo;?</AlertDialogTitle>
            <AlertDialogDescription>
              {hasCertificate
                ? "This deletes your certificate for this course and all your progress. This can't be undone."
                : "This deletes your progress in this course. This can't be undone."}
            </AlertDialogDescription>
          </AlertDialogHeader>
          {error ? <p className="text-sm text-destructive">{error}</p> : null}
          <AlertDialogFooter>
            <AlertDialogCancel disabled={loading}>Cancel</AlertDialogCancel>
            <AlertDialogAction variant="destructive" disabled={loading} onClick={handleUnenrol}>
              {loading ? "Unenrolling..." : "Unenrol"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
