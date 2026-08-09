"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { createCourse, updateCourse } from "@/lib/api/course-builder";
import { ApiError } from "@/lib/api/client";
import type { CourseDetail } from "@/types/course";

const courseSchema = z.object({
  title: z.string().min(3, "Title must be at least 3 characters").max(200),
  description: z.string().min(10, "Description must be at least 10 characters").max(2000),
  thumbnailUrl: z.union([z.url("Enter a valid URL"), z.literal("")]),
  category: z.string().max(50),
});

type CourseValues = z.infer<typeof courseSchema>;

export function CourseDetailsForm({ course }: { course?: CourseDetail }) {
  const router = useRouter();
  const [formError, setFormError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isSubmitting },
  } = useForm<CourseValues>({
    resolver: zodResolver(courseSchema),
    defaultValues: {
      title: course?.title ?? "",
      description: course?.description ?? "",
      thumbnailUrl: course?.thumbnailUrl ?? "",
      category: course?.category ?? "",
    },
  });

  async function onSubmit(values: CourseValues) {
    setFormError(null);
    setSuccess(false);
    const payload = {
      title: values.title,
      description: values.description,
      thumbnailUrl: values.thumbnailUrl || null,
      category: values.category || null,
    };

    try {
      if (course) {
        await updateCourse(course.id, payload);
        setSuccess(true);
        router.refresh();
      } else {
        const created = await createCourse(payload);
        router.push(`/instructor/courses/${created.id}/edit`);
      }
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "Something went wrong.");
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="max-w-xl space-y-4">
      <div className="space-y-1.5">
        <Label htmlFor="title">Title</Label>
        <Input id="title" {...register("title")} />
        {errors.title ? (
          <p className="text-xs text-destructive">{errors.title.message}</p>
        ) : null}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="description">Description</Label>
        <Textarea id="description" rows={4} {...register("description")} />
        {errors.description ? (
          <p className="text-xs text-destructive">{errors.description.message}</p>
        ) : null}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="category">Category</Label>
        <Input id="category" placeholder="Development" {...register("category")} />
      </div>

      <div className="space-y-1.5">
        <Label>Thumbnail</Label>
        <Controller
          name="thumbnailUrl"
          control={control}
          render={({ field }) => (
            <ImageUpload value={field.value || null} onChange={field.onChange} />
          )}
        />
        {errors.thumbnailUrl ? (
          <p className="text-xs text-destructive">{errors.thumbnailUrl.message}</p>
        ) : null}
      </div>

      {formError ? <p className="text-sm text-destructive">{formError}</p> : null}
      {success ? <p className="text-sm text-primary">Saved.</p> : null}

      <Button type="submit" disabled={isSubmitting}>
        {isSubmitting ? "Saving..." : course ? "Save changes" : "Create course"}
      </Button>
    </form>
  );
}
