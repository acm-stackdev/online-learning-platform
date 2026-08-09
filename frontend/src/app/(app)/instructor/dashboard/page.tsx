import Link from "next/link";
import { redirect } from "next/navigation";
import type { Metadata } from "next";

import { StatTile } from "@/components/dashboard/StatTile";
import { CourseStatusBadge } from "@/components/instructor/CourseStatusBadge";
import { buttonVariants } from "@/components/ui/button";
import { getCurrentUser } from "@/lib/api/me";
import { getInstructorDashboard } from "@/lib/api/dashboard";
import { getCourseDetail } from "@/lib/api/courses";
import { getCourseRoster } from "@/lib/api/roster";
import { Role } from "@/types/auth";

export const metadata: Metadata = {
  title: "Your courses — LearnHub",
};

export default async function InstructorDashboardPage() {
  const user = await getCurrentUser();
  if (!user) redirect("/login");
  if (user.role !== Role.Instructor) redirect("/dashboard");

  const dashboard = await getInstructorDashboard();
  const courses = dashboard?.courses ?? [];

  const enriched = await Promise.all(
    courses.map(async (course) => {
      const [detail, roster] = await Promise.all([
        getCourseDetail(course.id),
        getCourseRoster(course.id),
      ]);
      const lessonCount =
        detail?.sections.reduce((sum, s) => sum + s.lessons.length, 0) ?? 0;
      return { course, lessonCount, studentCount: roster.length };
    })
  );

  return (
    <div className="mx-auto max-w-6xl space-y-6 px-4 py-8 sm:px-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Your courses</h1>
          <p className="text-sm text-muted-foreground">
            {dashboard?.totalCourses ?? 0} courses &middot;{" "}
            {dashboard?.totalStudentsEnrolled ?? 0} students enrolled
          </p>
        </div>
        <Link href="/instructor/courses/new" className={buttonVariants({})}>
          + New course
        </Link>
      </div>

      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <StatTile label="Published" value={dashboard?.publishedCourseCount ?? 0} />
        <StatTile label="Pending review" value={dashboard?.pendingApprovalCourseCount ?? 0} />
        <StatTile label="Drafts" value={dashboard?.draftCourseCount ?? 0} />
        <StatTile label="Total students" value={dashboard?.totalStudentsEnrolled ?? 0} />
      </div>

      {enriched.length > 0 ? (
        <div className="overflow-hidden rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="border-b border-border bg-muted/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-2 font-medium">Course</th>
                <th className="px-4 py-2 font-medium">Status</th>
                <th className="px-4 py-2 font-medium">Lessons</th>
                <th className="px-4 py-2 font-medium">Students</th>
                <th className="px-4 py-2 font-medium" />
              </tr>
            </thead>
            <tbody>
              {enriched.map(({ course, lessonCount, studentCount }) => (
                <tr key={course.id} className="border-b border-border last:border-b-0">
                  <td className="px-4 py-3 font-medium">{course.title}</td>
                  <td className="px-4 py-3">
                    <CourseStatusBadge status={course.status} />
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{lessonCount}</td>
                  <td className="px-4 py-3 text-muted-foreground">{studentCount}</td>
                  <td className="px-4 py-3 text-right">
                    <Link
                      href={`/instructor/courses/${course.id}/edit`}
                      className="font-medium text-primary hover:underline"
                    >
                      Edit
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">
          You haven&apos;t created a course yet.
        </p>
      )}
    </div>
  );
}
