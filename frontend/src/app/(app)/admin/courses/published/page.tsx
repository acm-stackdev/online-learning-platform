import type { Metadata } from "next";

import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Pagination } from "@/components/courses/Pagination";
import { PublishedCourseRow } from "@/components/admin/PublishedCourseRow";
import { getCourses } from "@/lib/api/courses";

export const metadata: Metadata = {
  title: "Published courses — LearnHub",
};

export default async function AdminPublishedCoursesPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string; search?: string }>;
}) {
  const { page: pageParam, search } = await searchParams;
  const page = Math.max(1, Number(pageParam) || 1);

  const result = await getCourses({ page, search });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Published courses</h1>
        <p className="text-sm text-muted-foreground">
          {result.totalCount} course{result.totalCount === 1 ? "" : "s"} live
        </p>
      </div>

      <form className="flex gap-2">
        <Input
          type="search"
          name="search"
          defaultValue={search}
          placeholder="Search published courses"
          className="max-w-md"
        />
        <Button type="submit">Search</Button>
      </form>

      {result.items.length > 0 ? (
        <div className="space-y-3">
          {result.items.map((course) => (
            <PublishedCourseRow key={course.id} course={course} />
          ))}
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">No published courses match your search.</p>
      )}

      <Pagination
        basePath="/admin/courses/published"
        page={result.page}
        pageSize={result.pageSize}
        totalCount={result.totalCount}
        search={search}
      />
    </div>
  );
}
