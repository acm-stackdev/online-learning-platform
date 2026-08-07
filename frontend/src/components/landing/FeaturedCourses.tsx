import Link from "next/link";

import { getFeaturedCourses } from "@/lib/api/courses";
import { CourseCard } from "@/components/landing/CourseCard";

export async function FeaturedCourses() {
  const courses = await getFeaturedCourses();

  return (
    <section className="mx-auto max-w-6xl px-4 py-16 sm:px-6">
      <div className="mb-8 flex items-center justify-between">
        <h2 className="text-2xl font-semibold tracking-tight">Featured courses</h2>
        <Link
          href="/courses"
          className="text-sm font-medium text-primary hover:underline"
        >
          View all courses &rarr;
        </Link>
      </div>

      {courses.length > 0 ? (
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {courses.map((course) => (
            <CourseCard key={course.id} course={course} />
          ))}
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">
          Courses will show up here once they&apos;re published.
        </p>
      )}
    </section>
  );
}
