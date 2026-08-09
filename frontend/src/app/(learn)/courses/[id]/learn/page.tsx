import { notFound, redirect } from "next/navigation";

import { getCourseDetail } from "@/lib/api/courses";
import { getMyEnrollments } from "@/lib/api/my-enrollments";
import { getEnrollmentProgress } from "@/lib/api/progress";
import { getCurrentUser } from "@/lib/api/me";
import { Role } from "@/types/auth";

export default async function LearnRedirectPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const courseId = Number(id);

  const user = await getCurrentUser();
  if (!user) redirect("/login");

  const course = await getCourseDetail(courseId);
  if (!course) notFound();

  const allLessons = course.sections.flatMap((s) => s.lessons);
  if (allLessons.length === 0) redirect(`/courses/${courseId}`);

  // Admin can never hold a real Enrollment row (blocked server-side), so there's no
  // "resume where you left off" progress to read — just preview from the first lesson.
  if (user.role === Role.Admin) {
    redirect(`/courses/${courseId}/learn/${allLessons[0].id}`);
  }

  const enrollments = await getMyEnrollments();
  const enrollment = enrollments.find((e) => e.course.id === courseId);
  if (!enrollment) redirect(`/courses/${courseId}`);

  const progress = await getEnrollmentProgress(enrollment.id);
  const completedIds = new Set(
    (progress?.lessons ?? []).filter((l) => l.isCompleted).map((l) => l.lessonId)
  );

  const firstIncomplete = allLessons.find((l) => !completedIds.has(l.id));
  const target = firstIncomplete ?? allLessons[0];

  redirect(`/courses/${courseId}/learn/${target.id}`);
}
