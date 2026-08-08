import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Check } from "lucide-react";
import type { Metadata } from "next";

import { Badge } from "@/components/ui/badge";
import { CurriculumSection } from "@/components/courses/CurriculumSection";
import { EnrolButton } from "@/components/courses/EnrolButton";
import { getCourseDetail } from "@/lib/api/courses";
import { getCurrentUser } from "@/lib/api/me";
import { formatDuration } from "@/lib/utils";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ id: string }>;
}): Promise<Metadata> {
  const { id } = await params;
  const course = await getCourseDetail(Number(id));
  return { title: course ? `${course.title} — LearnHub` : "Course — LearnHub" };
}

export default async function CourseDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const courseId = Number(id);

  const [course, user] = await Promise.all([
    getCourseDetail(courseId),
    getCurrentUser(),
  ]);

  if (!course) notFound();

  const totalLessons = course.sections.reduce((sum, s) => sum + s.lessons.length, 0);
  const totalSeconds = course.sections.reduce(
    (sum, s) => sum + s.lessons.reduce((lsum, l) => lsum + l.duration, 0),
    0
  );
  const hasAccess = course.sections.some((s) => s.lessons.some((l) => l.contentUrl));

  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6">
      <nav className="mb-4 text-sm text-muted-foreground">
        <Link href="/courses" className="hover:text-foreground">
          Courses
        </Link>
        {course.category ? (
          <>
            {" / "}
            <Link
              href={`/courses?search=${encodeURIComponent(course.category)}`}
              className="hover:text-foreground"
            >
              {course.category}
            </Link>
          </>
        ) : null}
        {" / "}
        <span>{course.title}</span>
      </nav>

      <div className="grid grid-cols-1 gap-10 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          {course.category ? (
            <Badge variant="outline" className="uppercase tracking-wide">
              {course.category}
            </Badge>
          ) : null}

          <h1 className="text-3xl font-semibold tracking-tight">{course.title}</h1>

          <p className="text-sm text-muted-foreground">{course.instructorName}</p>

          <p className="text-sm leading-relaxed">{course.description}</p>

          <div>
            <h2 className="mb-1 text-lg font-semibold tracking-tight">Curriculum</h2>
            <p className="mb-4 text-sm text-muted-foreground">
              {course.sections.length} sections &middot; {totalLessons} lessons
              &middot; {formatDuration(totalSeconds)} total
            </p>

            <div className="rounded-lg border border-border px-4">
              {course.sections.map((section, i) => (
                <CurriculumSection key={section.id} section={section} defaultOpen={i === 0} />
              ))}
            </div>

            {!hasAccess ? (
              <p className="mt-3 text-xs text-muted-foreground">
                Locked lessons unlock the moment you enrol — enrolment is free.
              </p>
            ) : null}
          </div>
        </div>

        <div className="space-y-4">
          <div className="relative aspect-video w-full overflow-hidden rounded-lg bg-muted">
            {course.thumbnailUrl ? (
              <Image
                src={course.thumbnailUrl}
                alt={course.title}
                fill
                className="object-cover"
              />
            ) : null}
          </div>

          <div className="space-y-1">
            <p className="text-lg font-semibold">Free</p>
            <p className="text-xs text-muted-foreground">Full lifetime access</p>
          </div>

          <EnrolButton courseId={course.id} isLoggedIn={!!user} hasAccess={hasAccess} />

          <ul className="space-y-1.5 text-sm text-muted-foreground">
            {[
              `${totalLessons} lessons, video and documents`,
              `${formatDuration(totalSeconds)} of content`,
              "Certificate on completion",
              "Message the instructor",
            ].map((item) => (
              <li key={item} className="flex items-center gap-2">
                <Check className="size-4 shrink-0 text-primary" />
                {item}
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}
