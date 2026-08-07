import Link from "next/link";
import Image from "next/image";

import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import type { CourseListItem } from "@/types/course";

export function CourseCard({ course }: { course: CourseListItem }) {
  return (
    <Link href={`/courses/${course.id}`}>
      <Card className="h-full transition-shadow hover:shadow-md">
        <div className="relative aspect-video w-full overflow-hidden bg-muted">
          {course.thumbnailUrl ? (
            <Image
              src={course.thumbnailUrl}
              alt={course.title}
              fill
              className="object-cover"
            />
          ) : null}
        </div>
        <CardContent className="flex flex-col gap-2">
          {course.category ? (
            <Badge variant="outline" className="w-fit uppercase tracking-wide">
              {course.category}
            </Badge>
          ) : null}
          <h3 className="font-heading text-base font-medium leading-snug">
            {course.title}
          </h3>
          <p className="text-sm text-muted-foreground">{course.instructorName}</p>
        </CardContent>
      </Card>
    </Link>
  );
}
