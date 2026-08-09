import { redirect } from "next/navigation";
import type { Metadata } from "next";

import { ApplicationForm } from "@/components/instructor-application/ApplicationForm";
import { getCurrentUser } from "@/lib/api/me";
import { getMyInstructorApplications } from "@/lib/api/my-instructor-application";
import { Role } from "@/types/auth";
import { ApplicationStatus } from "@/types/dashboard";

export const metadata: Metadata = {
  title: "Become an instructor — LearnHub",
};

const statusCopy: Record<ApplicationStatus, { heading: string; body: string }> = {
  [ApplicationStatus.Pending]: {
    heading: "Application pending",
    body: "We're reviewing your application. We'll let you know once a decision is made.",
  },
  [ApplicationStatus.Approved]: {
    heading: "Application approved",
    body: "You've been approved as an instructor — log out and back in to see instructor tools.",
  },
  [ApplicationStatus.Rejected]: {
    heading: "Application not approved",
    body: "Your last application wasn't approved. You're welcome to apply again below.",
  },
};

export default async function BecomeInstructorPage() {
  const user = await getCurrentUser();
  if (!user) redirect("/login");

  if (user.role !== Role.Student) {
    return (
      <div className="mx-auto max-w-2xl space-y-2 px-4 py-8 sm:px-6">
        <h1 className="text-2xl font-semibold tracking-tight">Become an instructor</h1>
        <p className="text-sm text-muted-foreground">
          This is only for Student accounts.
        </p>
      </div>
    );
  }

  const applications = await getMyInstructorApplications();
  const latest = applications[0] ?? null;
  const canApply = !latest || latest.status !== ApplicationStatus.Pending;

  // We only reach this point when the user is currently a Student (checked above), so a
  // latest application of "Approved" can't still be true — an admin must have reverted the
  // role back to Student since. Treat it as stale rather than showing an outdated success message.
  const isStaleApproval = latest?.status === ApplicationStatus.Approved;

  return (
    <div className="mx-auto max-w-2xl space-y-6 px-4 py-8 sm:px-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Become an instructor</h1>
        <p className="text-sm text-muted-foreground">
          Apply once, get approved, then publish courses with video and documents.
        </p>
      </div>

      {latest && isStaleApproval ? (
        <div className="space-y-1 rounded-lg border border-border bg-muted/30 p-4 text-sm">
          <p className="font-medium">Instructor access removed</p>
          <p className="text-muted-foreground">
            Your previous application was approved, but instructor access has since been
            removed from your account. You&apos;re welcome to apply again below.
          </p>
        </div>
      ) : null}

      {latest && !isStaleApproval ? (
        <div className="space-y-1 rounded-lg border border-border bg-muted/30 p-4 text-sm">
          <p className="font-medium">{statusCopy[latest.status].heading}</p>
          <p className="text-muted-foreground">{statusCopy[latest.status].body}</p>
        </div>
      ) : null}

      {canApply ? <ApplicationForm /> : null}
    </div>
  );
}
