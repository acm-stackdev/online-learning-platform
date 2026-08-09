import type { Metadata } from "next";

import { StatTile } from "@/components/dashboard/StatTile";
import { getPlatformStats } from "@/lib/api/admin";

export const metadata: Metadata = {
  title: "Admin overview — LearnHub",
};

export default async function AdminOverviewPage() {
  const stats = await getPlatformStats();

  return (
    <div className="space-y-8">
      <h1 className="text-2xl font-semibold tracking-tight">Platform stats</h1>

      <div>
        <h2 className="mb-3 text-sm font-medium uppercase tracking-wide text-muted-foreground">
          Users
        </h2>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-5">
          <StatTile label="Total" value={stats?.totalUsers ?? 0} />
          <StatTile label="Students" value={stats?.studentCount ?? 0} />
          <StatTile label="Instructors" value={stats?.instructorCount ?? 0} />
          <StatTile label="Admins" value={stats?.adminCount ?? 0} />
          <StatTile label="Suspended" value={stats?.suspendedCount ?? 0} />
        </div>
      </div>

      <div>
        <h2 className="mb-3 text-sm font-medium uppercase tracking-wide text-muted-foreground">
          Courses
        </h2>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
          <StatTile label="Published" value={stats?.publishedCourseCount ?? 0} />
          <StatTile label="Pending review" value={stats?.pendingApprovalCourseCount ?? 0} />
          <StatTile label="Drafts" value={stats?.draftCourseCount ?? 0} />
          <StatTile label="Rejected" value={stats?.rejectedCourseCount ?? 0} />
        </div>
      </div>

      <div>
        <h2 className="mb-3 text-sm font-medium uppercase tracking-wide text-muted-foreground">
          Enrollments
        </h2>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
          <StatTile label="Total" value={stats?.totalEnrollments ?? 0} />
          <StatTile label="In progress" value={stats?.inProgressEnrollmentCount ?? 0} />
          <StatTile label="Completed" value={stats?.completedEnrollmentCount ?? 0} />
        </div>
      </div>
    </div>
  );
}
