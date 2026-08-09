import type { Metadata } from "next";

import { CourseReviewRow } from "@/components/admin/CourseReviewRow";
import { getPendingCourses } from "@/lib/api/course-review";

export const metadata: Metadata = {
  title: "Course review — LearnHub",
};

export default async function AdminCoursesPage() {
  const courses = await getPendingCourses();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Course review queue</h1>
        <p className="text-sm text-muted-foreground">
          {courses.length} course{courses.length === 1 ? "" : "s"} waiting
        </p>
      </div>

      {courses.length > 0 ? (
        <div className="space-y-3">
          {courses.map((course) => (
            <CourseReviewRow key={course.id} course={course} />
          ))}
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">Nothing waiting for review.</p>
      )}
    </div>
  );
}
