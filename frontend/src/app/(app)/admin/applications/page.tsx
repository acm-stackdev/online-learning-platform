import type { Metadata } from "next";

import { ApplicationReviewRow } from "@/components/admin/ApplicationReviewRow";
import { getPendingApplications } from "@/lib/api/instructor-applications-review";

export const metadata: Metadata = {
  title: "Instructor applications — LearnHub",
};

export default async function AdminApplicationsPage() {
  const applications = await getPendingApplications();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Instructor applications</h1>
        <p className="text-sm text-muted-foreground">
          {applications.length} pending
        </p>
      </div>

      {applications.length > 0 ? (
        <div className="space-y-3">
          {applications.map((application) => (
            <ApplicationReviewRow key={application.id} application={application} />
          ))}
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">No pending applications.</p>
      )}
    </div>
  );
}
