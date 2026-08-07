"use client";

import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";

import { buttonVariants } from "@/components/ui/button";
import { verifyEmail } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

type Status = "loading" | "success" | "error";

export function VerifyEmailStatus() {
  const searchParams = useSearchParams();
  const token = searchParams.get("token");
  const [status, setStatus] = useState<Status>(token ? "loading" : "error");
  const [message, setMessage] = useState<string | null>(
    token ? null : "This verification link is missing its token."
  );

  useEffect(() => {
    if (!token) return;

    verifyEmail(token)
      .then(() => setStatus("success"))
      .catch((err) => {
        setStatus("error");
        setMessage(err instanceof ApiError ? err.message : "Something went wrong.");
      });
  }, [token]);

  if (status === "loading") {
    return <p className="text-sm text-muted-foreground">Verifying your email...</p>;
  }

  if (status === "success") {
    return (
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Your email is verified. You can now log in.
        </p>
        <Link href="/login" className={buttonVariants({ className: "w-full" })}>
          Log in
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-destructive">{message}</p>
      <Link
        href="/register"
        className={buttonVariants({ variant: "outline", className: "w-full" })}
      >
        Back to sign up
      </Link>
    </div>
  );
}
