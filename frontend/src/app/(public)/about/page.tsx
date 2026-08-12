import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "About — LearnHub",
};

export default function AboutPage() {
  return (
    <div className="mx-auto max-w-3xl space-y-8 px-4 py-16 sm:px-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight">About LearnHub</h1>
        <p className="text-sm text-muted-foreground">
          A course platform built as a final-year project at the University of
          Westminster.
        </p>
      </div>

      <div className="space-y-4 text-sm leading-relaxed text-muted-foreground">
        <p>
          LearnHub is an online learning platform where instructors publish
          courses and students learn at their own pace. It covers the core
          pieces of a real course marketplace: course creation and review,
          enrolment, video and document lessons, progress tracking,
          certificates, messaging between students and instructors, and an
          AI-powered course tutor.
        </p>
        <p>
          This project exists to demonstrate a full-stack, cloud-native build
          — a Next.js frontend and an ASP.NET Core backend, deployed
          independently and backed by managed cloud services. It is a
          student project, not a commercial product.
        </p>
      </div>
    </div>
  );
}
