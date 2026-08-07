import type { Metadata } from "next";

import { AuthCard } from "@/components/auth/AuthCard";
import { RegisterForm } from "@/components/auth/RegisterForm";

export const metadata: Metadata = {
  title: "Create your account — LearnHub",
};

export default function RegisterPage() {
  return (
    <AuthCard heading="Create your account" subheading="Free to join. No card, ever.">
      <RegisterForm />
    </AuthCard>
  );
}
