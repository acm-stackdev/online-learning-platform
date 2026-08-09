import { apiFetch } from "@/lib/api/client";
import type { ContentType, CourseListItem, Section } from "@/types/course";

export interface CourseFormValues {
  title: string;
  description: string;
  thumbnailUrl: string | null;
  category: string | null;
}

export function createCourse(values: CourseFormValues) {
  return apiFetch<CourseListItem>("/api/courses", {
    method: "POST",
    body: JSON.stringify(values),
  });
}

export function updateCourse(courseId: number, values: CourseFormValues) {
  return apiFetch<CourseListItem>(`/api/courses/${courseId}`, {
    method: "PUT",
    body: JSON.stringify(values),
  });
}

export function submitForReview(courseId: number) {
  return apiFetch<CourseListItem>(`/api/courses/${courseId}/submit-for-review`, {
    method: "POST",
  });
}

export function unpublishCourse(courseId: number) {
  return apiFetch<CourseListItem>(`/api/courses/${courseId}/unpublish`, {
    method: "PUT",
  });
}

export function createSection(courseId: number, title: string) {
  return apiFetch<Section>("/api/sections", {
    method: "POST",
    body: JSON.stringify({ courseId, title }),
  });
}

export function updateSection(sectionId: number, title: string) {
  return apiFetch<Section>(`/api/sections/${sectionId}`, {
    method: "PUT",
    body: JSON.stringify({ title }),
  });
}

export function deleteSection(sectionId: number) {
  return apiFetch<void>(`/api/sections/${sectionId}`, { method: "DELETE" });
}

export function reorderSections(courseId: number, orderedSectionIds: number[]) {
  return apiFetch<void>("/api/sections/reorder", {
    method: "PUT",
    body: JSON.stringify({ courseId, orderedSectionIds }),
  });
}

export function createLesson(payload: {
  sectionId: number;
  title: string;
  contentType: ContentType;
  duration: number;
  file: File;
}) {
  const form = new FormData();
  form.append("sectionId", String(payload.sectionId));
  form.append("title", payload.title);
  form.append("contentType", String(payload.contentType));
  form.append("duration", String(payload.duration));
  form.append("file", payload.file);

  return apiFetch<void>("/api/lessons", { method: "POST", body: form });
}

export function updateLesson(
  lessonId: number,
  payload: { title: string; duration: number; file?: File }
) {
  const form = new FormData();
  form.append("title", payload.title);
  form.append("duration", String(payload.duration));
  if (payload.file) form.append("file", payload.file);

  return apiFetch<void>(`/api/lessons/${lessonId}`, { method: "PUT", body: form });
}

export function deleteLesson(lessonId: number) {
  return apiFetch<void>(`/api/lessons/${lessonId}`, { method: "DELETE" });
}

export function reorderLessons(sectionId: number, orderedLessonIds: number[]) {
  return apiFetch<void>("/api/lessons/reorder", {
    method: "PUT",
    body: JSON.stringify({ sectionId, orderedLessonIds }),
  });
}
