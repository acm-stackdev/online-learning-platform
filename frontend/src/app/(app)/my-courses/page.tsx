import Link from "next/link";
import type { Metadata } from "next";

import { ContinueLearningCard } from "@/components/dashboard/ContinueLearningCard";
import { CertificateCard } from "@/components/dashboard/CertificateCard";
import { getStudentDashboard } from "@/lib/api/dashboard";
import { getEnrollmentProgress } from "@/lib/api/progress";
import { getCertificate } from "@/lib/api/certificates";
import { cn } from "@/lib/utils";
import type { Certificate } from "@/types/certificate";

export const metadata: Metadata = {
  title: "My courses — LearnHub",
};

type Filter = "all" | "in-progress" | "completed";

function parseFilter(value: string | undefined): Filter {
  return value === "in-progress" || value === "completed" ? value : "all";
}

export default async function MyCoursesPage({
  searchParams,
}: {
  searchParams: Promise<{ filter?: string }>;
}) {
  const { filter: filterParam } = await searchParams;
  const filter = parseFilter(filterParam);

  const dashboard = await getStudentDashboard();
  const enrollments = dashboard?.enrollments ?? [];

  const filtered = enrollments.filter((e) => {
    if (filter === "in-progress") return e.completedAt === null;
    if (filter === "completed") return e.completedAt !== null;
    return true;
  });

  const inProgress = filtered.filter((e) => e.completedAt === null);
  const completed = filtered.filter((e) => e.completedAt !== null);

  const progressByEnrollment = new Map(
    await Promise.all(
      inProgress.map(async (e) => [e.id, await getEnrollmentProgress(e.id)] as const)
    )
  );

  const certificateByEnrollment = new Map(
    (await Promise.all(completed.map((e) => getCertificate(e.id))))
      .filter((c): c is Certificate => c !== null)
      .map((c) => [c.enrollmentId, c])
  );

  const tabs: { value: Filter; label: string; count: number }[] = [
    { value: "all", label: "All", count: dashboard?.totalEnrollments ?? 0 },
    { value: "in-progress", label: "In progress", count: dashboard?.inProgressCount ?? 0 },
    { value: "completed", label: "Completed", count: dashboard?.completedCount ?? 0 },
  ];

  return (
    <div className="mx-auto max-w-6xl space-y-6 px-4 py-8 sm:px-6">
      <h1 className="text-2xl font-semibold tracking-tight">My courses</h1>

      <div className="flex items-center gap-2 border-b border-border">
        {tabs.map((tab) => (
          <Link
            key={tab.value}
            href={tab.value === "all" ? "/my-courses" : `/my-courses?filter=${tab.value}`}
            className={cn(
              "border-b-2 px-3 py-2 text-sm font-medium transition-colors",
              filter === tab.value
                ? "border-primary text-foreground"
                : "border-transparent text-muted-foreground hover:text-foreground"
            )}
          >
            {tab.label} {tab.count}
          </Link>
        ))}
      </div>

      {filtered.length > 0 ? (
        <div className="space-y-3">
          {filtered.map((enrollment) => {
            if (enrollment.completedAt === null) {
              return (
                <ContinueLearningCard
                  key={enrollment.id}
                  enrollment={enrollment}
                  progress={progressByEnrollment.get(enrollment.id) ?? null}
                />
              );
            }

            const certificate = certificateByEnrollment.get(enrollment.id);
            return certificate ? (
              <CertificateCard key={enrollment.id} certificate={certificate} />
            ) : null;
          })}
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">No courses here yet.</p>
      )}
    </div>
  );
}
