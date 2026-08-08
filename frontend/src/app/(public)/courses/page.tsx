import type { Metadata } from "next";

import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { CourseCard } from "@/components/landing/CourseCard";
import { Pagination } from "@/components/courses/Pagination";
import { getCourses } from "@/lib/api/courses";

export const metadata: Metadata = {
  title: "Browse courses — LearnHub",
};

export default async function CoursesPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string; search?: string }>;
}) {
  const { page: pageParam, search } = await searchParams;
  const page = Math.max(1, Number(pageParam) || 1);

  const result = await getCourses({ page, search });

  return (
    <div className="mx-auto max-w-6xl space-y-8 px-4 py-10 sm:px-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">All courses</h1>
        <p className="text-sm text-muted-foreground">
          {result.totalCount} course{result.totalCount === 1 ? "" : "s"}
        </p>
      </div>

      <form className="flex gap-2">
        <Input
          type="search"
          name="search"
          defaultValue={search}
          placeholder="Search courses, topics, instructors"
          className="max-w-md"
        />
        <Button type="submit">Search</Button>
      </form>

      {result.items.length > 0 ? (
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {result.items.map((course) => (
            <CourseCard key={course.id} course={course} />
          ))}
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">
          No courses match your search.
        </p>
      )}

      <Pagination
        page={result.page}
        pageSize={result.pageSize}
        totalCount={result.totalCount}
        search={search}
      />
    </div>
  );
}
