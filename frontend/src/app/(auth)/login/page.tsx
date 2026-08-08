import type { Metadata } from "next";

import { AuthCard } from "@/components/auth/AuthCard";
import { LoginForm } from "@/components/auth/LoginForm";

export const metadata: Metadata = {
  title: "Log in — LearnHub",
};

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ expired?: string }>;
}) {
  const { expired } = await searchParams;

  return (
    <AuthCard heading="Welcome back" subheading="Log in to pick up where you left off.">
      {expired ? (
        <p className="mb-4 rounded-lg border border-border bg-muted/30 p-3 text-sm text-muted-foreground">
          Your session has expired. Please log in again.
        </p>
      ) : null}
      <LoginForm />
    </AuthCard>
  );
}
