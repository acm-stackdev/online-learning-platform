"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { GoogleSignInButton } from "@/components/auth/GoogleSignInButton";
import { login } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";
import { isUserResponse } from "@/types/auth";

const loginSchema = z.object({
  email: z.email("Enter a valid email address"),
  password: z.string().min(1, "Password is required"),
});

type LoginValues = z.infer<typeof loginSchema>;

export function LoginForm() {
  const router = useRouter();
  const [formError, setFormError] = useState<string | null>(null);
  const [checkEmail, setCheckEmail] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
  });

  async function onSubmit(values: LoginValues) {
    setFormError(null);
    try {
      await login(values);
      router.push("/dashboard");
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "Something went wrong.");
    }
  }

  if (checkEmail) {
    return (
      <div className="space-y-2 rounded-lg border border-border bg-muted/30 p-4 text-sm">
        <p className="font-medium">Check your email</p>
        <p className="text-muted-foreground">
          We&apos;ve sent a verification link to your inbox. Verify your account,
          then log in again.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="email">Email</Label>
          <Input id="email" type="email" placeholder="you@example.com" {...register("email")} />
          {errors.email ? (
            <p className="text-xs text-destructive">{errors.email.message}</p>
          ) : null}
        </div>

        <div className="space-y-1.5">
          <div className="flex items-center justify-between">
            <Label htmlFor="password">Password</Label>
            <Link href="/forgot-password" className="text-xs text-primary hover:underline">
              Forgot Password ?
            </Link>
          </div>
          <Input id="password" type="password" {...register("password")} />
          {errors.password ? (
            <p className="text-xs text-destructive">{errors.password.message}</p>
          ) : null}
        </div>

        {formError ? <p className="text-sm text-destructive">{formError}</p> : null}

        <Button type="submit" disabled={isSubmitting} className="w-full">
          {isSubmitting ? "Logging in..." : "Log in"}
        </Button>
      </form>

      <div className="flex items-center gap-3 text-xs text-muted-foreground">
        <div className="h-px flex-1 bg-border" />
        or
        <div className="h-px flex-1 bg-border" />
      </div>

      <GoogleSignInButton
        onSuccess={(result) => {
          setFormError(null);
          if (isUserResponse(result)) {
            router.push("/dashboard");
          } else {
            setCheckEmail(true);
          }
        }}
        onError={setFormError}
      />

      <p className="text-center text-sm text-muted-foreground">
        New here?{" "}
        <Link href="/register" className="text-primary hover:underline">
          Create an account
        </Link>
      </p>
    </div>
  );
}
