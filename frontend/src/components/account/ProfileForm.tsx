"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { updateProfile } from "@/lib/api/account";
import { ApiError } from "@/lib/api/client";
import type { UserResponse } from "@/types/auth";

const profileSchema = z.object({
  username: z.string().min(2, "Username must be at least 2 characters").max(50),
  avatarUrl: z.union([z.url("Enter a valid URL"), z.literal("")]),
});

type ProfileValues = z.infer<typeof profileSchema>;

export function ProfileForm({ user }: { user: UserResponse }) {
  const router = useRouter();
  const [formError, setFormError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isSubmitting },
  } = useForm<ProfileValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      username: user.username,
      avatarUrl: user.avatarUrl ?? "",
    },
  });

  async function onSubmit(values: ProfileValues) {
    setFormError(null);
    setSuccess(false);
    try {
      await updateProfile({
        username: values.username,
        avatarUrl: values.avatarUrl || null,
      });
      setSuccess(true);
      router.refresh();
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "Something went wrong.");
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-1.5">
        <Label>Avatar</Label>
        <Controller
          name="avatarUrl"
          control={control}
          render={({ field }) => (
            <ImageUpload value={field.value || null} onChange={field.onChange} shape="circle" />
          )}
        />
        {errors.avatarUrl ? (
          <p className="text-xs text-destructive">{errors.avatarUrl.message}</p>
        ) : null}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="username">Username</Label>
        <Input id="username" {...register("username")} />
        {errors.username ? (
          <p className="text-xs text-destructive">{errors.username.message}</p>
        ) : null}
      </div>

      {formError ? <p className="text-sm text-destructive">{formError}</p> : null}
      {success ? <p className="text-sm text-primary">Profile updated.</p> : null}

      <Button type="submit" disabled={isSubmitting}>
        {isSubmitting ? "Saving..." : "Save changes"}
      </Button>
    </form>
  );
}
