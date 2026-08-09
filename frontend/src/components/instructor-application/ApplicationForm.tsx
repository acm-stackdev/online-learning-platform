"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { submitInstructorApplication } from "@/lib/api/instructor-applications";
import { ApiError } from "@/lib/api/client";

const applicationSchema = z.object({
  message: z.string().min(20, "Tell us a bit more — at least 20 characters"),
});

type ApplicationValues = z.infer<typeof applicationSchema>;

export function ApplicationForm() {
  const router = useRouter();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ApplicationValues>({
    resolver: zodResolver(applicationSchema),
  });

  async function onSubmit(values: ApplicationValues) {
    setFormError(null);
    try {
      await submitInstructorApplication(values.message);
      router.refresh();
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "Something went wrong.");
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-1.5">
        <Label htmlFor="message">Tell us about yourself</Label>
        <Textarea
          id="message"
          rows={5}
          placeholder="What do you want to teach, and what's your experience with it?"
          {...register("message")}
        />
        {errors.message ? (
          <p className="text-xs text-destructive">{errors.message.message}</p>
        ) : null}
      </div>

      {formError ? <p className="text-sm text-destructive">{formError}</p> : null}

      <Button type="submit" disabled={isSubmitting}>
        {isSubmitting ? "Submitting..." : "Submit application"}
      </Button>
    </form>
  );
}
