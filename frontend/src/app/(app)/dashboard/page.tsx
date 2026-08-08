import Link from "next/link";
import type { Metadata } from "next";

import { StatTile } from "@/components/dashboard/StatTile";
import { ContinueLearningCard } from "@/components/dashboard/ContinueLearningCard";
import { getCurrentUser } from "@/lib/api/me";
import { getStudentDashboard } from "@/lib/api/dashboard";
import { getEnrollmentProgress } from "@/lib/api/progress";

export const metadata: Metadata = {
  title: "Dashboard — LearnHub",
};

const MAX_CONTINUE_LEARNING = 4;

export default async function DashboardPage() {
  const [user, dashboard] = await Promise.all([
    getCurrentUser(),
    getStudentDashboard(),
  ]);

  const inProgress = (dashboard?.enrollments ?? [])
    .filter((e) => e.completedAt === null)
    .sort((a, b) => (a.enrolledAt < b.enrolledAt ? 1 : -1))
    .slice(0, MAX_CONTINUE_LEARNING);

  const progressByEnrollment = new Map(
    (
      await Promise.all(
        inProgress.map(async (e) => [e.id, await getEnrollmentProgress(e.id)] as const)
      )
    )
  );

  const headline = inProgress[0]
    ? `You're ${Math.round(
        progressByEnrollment.get(inProgress[0].id)?.percentComplete ?? 0
      )}% through ${inProgress[0].course.title}.`
    : "Browse the catalogue to start your first course.";

  return (
    <div className="mx-auto max-w-6xl space-y-8 px-4 py-8 sm:px-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">
          Welcome back, {user?.username}
        </h1>
        <p className="text-sm text-muted-foreground">{headline}</p>
      </div>

      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <StatTile label="Enrolled" value={dashboard?.totalEnrollments ?? 0} />
        <StatTile label="In progress" value={dashboard?.inProgressCount ?? 0} />
        <StatTile label="Completed" value={dashboard?.completedCount ?? 0} />
        <StatTile label="Certificates" value={dashboard?.certificateCount ?? 0} />
      </div>

      <div>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold tracking-tight">Continue learning</h2>
          <Link href="/my-courses" className="text-sm font-medium text-primary hover:underline">
            All my courses &rarr;
          </Link>
        </div>

        {inProgress.length > 0 ? (
          <div className="space-y-3">
            {inProgress.map((enrollment) => (
              <ContinueLearningCard
                key={enrollment.id}
                enrollment={enrollment}
                progress={progressByEnrollment.get(enrollment.id) ?? null}
              />
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">
            You haven&apos;t started a course yet.
          </p>
        )}
      </div>
    </div>
  );
}
