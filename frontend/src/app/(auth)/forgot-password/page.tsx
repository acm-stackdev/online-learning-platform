import type { Metadata } from "next";

import { AuthCard } from "@/components/auth/AuthCard";
import { ForgotPasswordForm } from "@/components/auth/ForgotPasswordForm";

export const metadata: Metadata = {
  title: "Reset your password — LearnHub",
};

export default function ForgotPasswordPage() {
  return (
    <AuthCard
      heading="Reset your password"
      subheading="Enter your email and we'll send you a reset link."
    >
      <ForgotPasswordForm />
    </AuthCard>
  );
}
