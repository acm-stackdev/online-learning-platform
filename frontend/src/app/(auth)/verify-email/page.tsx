import { Suspense } from "react";
import type { Metadata } from "next";

import { AuthCard } from "@/components/auth/AuthCard";
import { VerifyEmailStatus } from "@/components/auth/VerifyEmailStatus";

export const metadata: Metadata = {
  title: "Verify your email — LearnHub",
};

export default function VerifyEmailPage() {
  return (
    <AuthCard heading="Verify your email" subheading="One more step before you can log in.">
      <Suspense fallback={<p className="text-sm text-muted-foreground">Loading...</p>}>
        <VerifyEmailStatus />
      </Suspense>
    </AuthCard>
  );
}
