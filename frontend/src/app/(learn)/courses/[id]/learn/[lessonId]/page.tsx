import { notFound, redirect } from "next/navigation";
import type { Metadata } from "next";

import { CurriculumRail } from "@/components/learn/CurriculumRail";
import { LessonView } from "@/components/learn/LessonView";
import { getCourseDetail } from "@/lib/api/courses";
import { getMyEnrollments } from "@/lib/api/my-enrollments";
import { getEnrollmentProgress } from "@/lib/api/progress";
import { getCurrentUser } from "@/lib/api/me";
import { Role } from "@/types/auth";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ id: string; lessonId: string }>;
}): Promise<Metadata> {
  const { id } = await params;
  const course = await getCourseDetail(Number(id));
  return { title: course ? `${course.title} — LearnHub` : "Lesson — LearnHub" };
}

export default async function LessonPage({
  params,
}: {
  params: Promise<{ id: string; lessonId: string }>;
}) {
  const { id, lessonId } = await params;
  const courseId = Number(id);
  const currentLessonId = Number(lessonId);

  const user = await getCurrentUser();
  if (!user) redirect("/login");
  const isAdmin = user.role === Role.Admin;

  const course = await getCourseDetail(courseId);
  if (!course) notFound();

  const allLessons = course.sections.flatMap((s) => s.lessons);
  const currentIndex = allLessons.findIndex((l) => l.id === currentLessonId);
  if (currentIndex === -1) notFound();

  const lesson = allLessons[currentIndex];
  const nextLessonId = allLessons[currentIndex + 1]?.id ?? null;

  // Admin has no Enrollment row (blocked server-side) and can't reach the
  // enrollment/progress endpoints (Student/Instructor only) — preview only, no progress.
  let completedIds = new Set<number>();

  if (!isAdmin) {
    const enrollments = await getMyEnrollments();
    const enrollment = enrollments.find((e) => e.course.id === courseId);
    if (!enrollment) redirect(`/courses/${courseId}`);

    const progress = await getEnrollmentProgress(enrollment.id);
    completedIds = new Set(
      (progress?.lessons ?? []).filter((l) => l.isCompleted).map((l) => l.lessonId)
    );
  }

  return (
    <div className="mx-auto grid max-w-6xl grid-cols-1 gap-8 px-4 py-6 sm:px-6 lg:grid-cols-[280px_1fr]">
      <aside className="order-2 lg:order-1">
        <p className="mb-3 text-sm font-medium">{course.title}</p>
        <CurriculumRail
          courseId={courseId}
          sections={course.sections}
          currentLessonId={currentLessonId}
          completedIds={completedIds}
        />
      </aside>

      <div className="order-1 lg:order-2">
        <LessonView
          courseId={courseId}
          lesson={lesson}
          nextLessonId={nextLessonId}
          initialIsCompleted={completedIds.has(currentLessonId)}
          isAdminView={isAdmin}
        />

        {course.description ? (
          <div className="mt-6 border-t border-border pt-6">
            <h2 className="mb-2 text-sm font-medium">Overview</h2>
            <p className="text-sm leading-relaxed text-muted-foreground">
              {course.description}
            </p>
          </div>
        ) : null}
      </div>
    </div>
  );
}
