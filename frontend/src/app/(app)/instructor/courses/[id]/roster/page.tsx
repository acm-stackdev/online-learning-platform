import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import type { Metadata } from "next";

import { RosterActions } from "@/components/instructor/RosterActions";
import { getCourseDetail } from "@/lib/api/courses";
import { getCourseRoster } from "@/lib/api/roster";
import { getCurrentUser } from "@/lib/api/me";
import { Role } from "@/types/auth";

export const metadata: Metadata = {
  title: "Roster — LearnHub",
};

export default async function RosterPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const courseId = Number(id);

  const user = await getCurrentUser();
  if (!user) redirect("/login");
  if (user.role !== Role.Instructor && user.role !== Role.Admin) redirect("/dashboard");

  const course = await getCourseDetail(courseId);
  if (!course) notFound();
  if (user.role === Role.Instructor && course.instructorId !== user.id) {
    redirect("/instructor/dashboard");
  }

  const roster = await getCourseRoster(courseId);

  return (
    <div className="mx-auto max-w-4xl space-y-6 px-4 py-8 sm:px-6">
      <div>
        <Link
          href={`/instructor/courses/${courseId}/edit`}
          className="text-sm text-muted-foreground hover:text-foreground"
        >
          &larr; {course.title}
        </Link>
        <h1 className="text-2xl font-semibold tracking-tight">Roster</h1>
        <p className="text-sm text-muted-foreground">
          {roster.length} student{roster.length === 1 ? "" : "s"} enrolled
        </p>
      </div>

      {roster.length > 0 ? (
        <div className="overflow-hidden rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="border-b border-border bg-muted/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-2 font-medium">Student</th>
                <th className="px-4 py-2 font-medium">Email</th>
                <th className="px-4 py-2 font-medium">Enrolled</th>
                <th className="px-4 py-2 font-medium">Completed</th>
                <th className="px-4 py-2 font-medium text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {roster.map((item) => (
                <tr key={item.enrollmentId} className="border-b border-border last:border-b-0">
                  <td className="px-4 py-3 font-medium">{item.studentUsername}</td>
                  <td className="px-4 py-3 text-muted-foreground">{item.studentEmail}</td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {new Date(item.enrolledAt).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {item.completedAt
                      ? new Date(item.completedAt).toLocaleDateString()
                      : "—"}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <RosterActions
                      enrollmentId={item.enrollmentId}
                      studentUsername={item.studentUsername}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">No students enrolled yet.</p>
      )}
    </div>
  );
}
