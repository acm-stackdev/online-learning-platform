import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Terms — LearnHub",
};

const sections = [
  {
    title: "1. Acceptance of terms",
    body: "By creating an account or using LearnHub, you agree to these terms. This is a demo platform built for a university final-year project — these terms are illustrative and not a binding commercial agreement.",
  },
  {
    title: "2. Accounts",
    body: "You're responsible for the accuracy of the information you provide and for keeping your account credentials secure. Accounts may be suspended for abuse of the platform, such as posting harmful content or attempting to circumvent enrolment rules.",
  },
  {
    title: "3. Course content",
    body: "Instructors retain ownership of the courses they publish. Course content is reviewed before publication and may be rejected or removed if it violates platform guidelines.",
  },
  {
    title: "4. Enrolment and certificates",
    body: "Enrolment in a published course is free. Certificates are generated on course completion and reflect participation in this demo environment only — they don't carry formal accreditation.",
  },
  {
    title: "5. Acceptable use",
    body: "Don't use LearnHub to upload unlawful, infringing, or harmful content, or to interfere with other users' access to the platform.",
  },
  {
    title: "6. Changes",
    body: "These terms may be updated as the platform evolves. Continued use of LearnHub after a change means you accept the updated terms.",
  },
];

export default function TermsPage() {
  return (
    <div className="mx-auto max-w-3xl space-y-8 px-4 py-16 sm:px-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight">Terms of Service</h1>
        <p className="text-sm text-muted-foreground">Last updated 2026</p>
      </div>

      <div className="space-y-6">
        {sections.map((section) => (
          <div key={section.title} className="space-y-1.5">
            <h2 className="text-lg font-semibold tracking-tight">{section.title}</h2>
            <p className="text-sm leading-relaxed text-muted-foreground">{section.body}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
