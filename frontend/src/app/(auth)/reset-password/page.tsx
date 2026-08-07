import { Suspense } from "react";
import type { Metadata } from "next";

import { AuthCard } from "@/components/auth/AuthCard";
import { ResetPasswordForm } from "@/components/auth/ResetPasswordForm";

export const metadata: Metadata = {
  title: "Set a new password — LearnHub",
};

export default function ResetPasswordPage() {
  return (
    <AuthCard heading="Set a new password" subheading="Choose a new password for your account.">
      <Suspense fallback={<p className="text-sm text-muted-foreground">Loading...</p>}>
        <ResetPasswordForm />
      </Suspense>
    </AuthCard>
  );
}
