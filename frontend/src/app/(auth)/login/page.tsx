import type { Metadata } from "next";

import { AuthCard } from "@/components/auth/AuthCard";
import { LoginForm } from "@/components/auth/LoginForm";

export const metadata: Metadata = {
  title: "Log in — LearnHub",
};

export default function LoginPage() {
  return (
    <AuthCard heading="Welcome back" subheading="Log in to pick up where you left off.">
      <LoginForm />
    </AuthCard>
  );
}
