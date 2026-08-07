"use client";

import { useState } from "react";
import Link from "next/link";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { RoleToggle } from "@/components/auth/RoleToggle";
import { register as registerAccount } from "@/lib/api/auth";
import { Role } from "@/types/auth";
import { ApiError } from "@/lib/api/client";

const registerSchema = z.object({
  username: z.string().min(2, "Username must be at least 2 characters").max(50),
  email: z.email("Enter a valid email address"),
  password: z.string().min(8, "Password must be at least 8 characters"),
  role: z.enum(Role, { error: "Choose whether you're here to learn or teach" }),
});

type RegisterValues = z.infer<typeof registerSchema>;

export function RegisterForm() {
  const [submitted, setSubmitted] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isSubmitting },
  } = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
  });

  async function onSubmit(values: RegisterValues) {
    setFormError(null);
    try {
      await registerAccount(values);
      setSubmitted(true);
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "Something went wrong.");
    }
  }

  if (submitted) {
    return (
      <div className="space-y-2 rounded-lg border border-border bg-muted/30 p-4 text-sm">
        <p className="font-medium">Check your email</p>
        <p className="text-muted-foreground">
          We&apos;ve sent a verification link to your inbox. Verify your account,
          then{" "}
          <Link href="/login" className="text-primary hover:underline">
            log in
          </Link>
          .
        </p>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-1.5">
        <Label htmlFor="username">Username</Label>
        <Input id="username" placeholder="priya_s" {...register("username")} />
        {errors.username ? (
          <p className="text-xs text-destructive">{errors.username.message}</p>
        ) : null}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="email">Email</Label>
        <Input id="email" type="email" placeholder="you@example.com" {...register("email")} />
        {errors.email ? (
          <p className="text-xs text-destructive">{errors.email.message}</p>
        ) : null}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="password">Password</Label>
        <Input id="password" type="password" placeholder="8+ characters" {...register("password")} />
        {errors.password ? (
          <p className="text-xs text-destructive">{errors.password.message}</p>
        ) : null}
      </div>

      <div className="space-y-1.5">
        <Label>I&apos;m here to</Label>
        <Controller
          name="role"
          control={control}
          render={({ field }) => (
            <RoleToggle value={field.value} onChange={field.onChange} />
          )}
        />
        {errors.role ? (
          <p className="text-xs text-destructive">{errors.role.message}</p>
        ) : null}
      </div>

      {formError ? <p className="text-sm text-destructive">{formError}</p> : null}

      <Button type="submit" disabled={isSubmitting} className="w-full">
        {isSubmitting ? "Creating account..." : "Create account"}
      </Button>

      <p className="text-xs text-muted-foreground">
        We&apos;ll email a verification link. By signing up you agree to the
        Terms and Privacy Policy.
      </p>
    </form>
  );
}
