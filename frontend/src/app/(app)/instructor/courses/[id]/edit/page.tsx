import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import type { Metadata } from "next";

import { CourseDetailsForm } from "@/components/instructor/CourseDetailsForm";
import { CurriculumEditor } from "@/components/instructor/CurriculumEditor";
import { PublishPanel } from "@/components/instructor/PublishPanel";
import { getCourseDetail } from "@/lib/api/courses";
import { getCurrentUser } from "@/lib/api/me";
import { Role } from "@/types/auth";

export const metadata: Metadata = {
  title: "Edit course — LearnHub",
};

export default async function EditCoursePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const courseId = Number(id);

  const user = await getCurrentUser();
  if (!user) redirect("/login");
  if (user.role !== Role.Instructor) redirect("/dashboard");

  const course = await getCourseDetail(courseId);
  if (!course) notFound();
  if (course.instructorId !== user.id) redirect("/instructor/dashboard");

  return (
    <div className="mx-auto max-w-4xl space-y-8 px-4 py-8 sm:px-6">
      <div className="flex items-center justify-between">
        <div>
          <Link
            href="/instructor/dashboard"
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            &larr; Your courses
          </Link>
          <h1 className="text-2xl font-semibold tracking-tight">{course.title}</h1>
        </div>
        <Link
          href={`/instructor/courses/${course.id}/roster`}
          className="text-sm font-medium text-primary hover:underline"
        >
          View roster
        </Link>
      </div>

      <PublishPanel courseId={course.id} status={course.status} />

      <div>
        <h2 className="mb-4 text-lg font-semibold tracking-tight">Details</h2>
        <CourseDetailsForm course={course} />
      </div>

      <div>
        <h2 className="mb-4 text-lg font-semibold tracking-tight">Curriculum</h2>
        <CurriculumEditor courseId={course.id} sections={course.sections} />
      </div>
    </div>
  );
}
