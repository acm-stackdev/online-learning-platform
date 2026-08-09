import { redirect } from "next/navigation";
import type { Metadata } from "next";

import { CourseDetailsForm } from "@/components/instructor/CourseDetailsForm";
import { getCurrentUser } from "@/lib/api/me";
import { Role } from "@/types/auth";

export const metadata: Metadata = {
  title: "New course — LearnHub",
};

export default async function NewCoursePage() {
  const user = await getCurrentUser();
  if (!user) redirect("/login");
  if (user.role !== Role.Instructor) redirect("/dashboard");

  return (
    <div className="mx-auto max-w-3xl space-y-6 px-4 py-8 sm:px-6">
      <h1 className="text-2xl font-semibold tracking-tight">New course</h1>
      <CourseDetailsForm />
    </div>
  );
}
