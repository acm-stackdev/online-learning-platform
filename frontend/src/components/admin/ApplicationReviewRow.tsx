"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

import { Button } from "@/components/ui/button";
import { approveApplication, rejectApplication } from "@/lib/api/admin-actions";
import { ApiError } from "@/lib/api/client";
import type { InstructorApplication } from "@/types/instructorApplication";

export function ApplicationReviewRow({
  application,
}: {
  application: InstructorApplication;
}) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handle(action: "approve" | "reject") {
    setLoading(true);
    setError(null);
    try {
      await (action === "approve" ? approveApplication : rejectApplication)(
        application.id
      );
      router.refresh();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
      setLoading(false);
    }
  }

  return (
    <div className="space-y-2 rounded-lg border border-border p-4">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="font-medium">{application.applicantUsername}</p>
          <p className="text-xs text-muted-foreground">{application.applicantEmail}</p>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={loading}
            onClick={() => handle("reject")}
          >
            Reject
          </Button>
          <Button size="sm" disabled={loading} onClick={() => handle("approve")}>
            Approve
          </Button>
        </div>
      </div>
      <p className="text-sm text-muted-foreground">{application.message}</p>
      {error ? <p className="text-xs text-destructive">{error}</p> : null}
    </div>
  );
}
