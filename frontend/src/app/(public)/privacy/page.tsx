import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Privacy — LearnHub",
};

const sections = [
  {
    title: "1. Information we collect",
    body: "When you create an account, we store your username, email address, and password (hashed, never in plain text). If you sign in with Google, we receive your name, email, and profile picture from your Google account instead. We also store the courses you enrol in, your lesson progress, and any messages you send through the platform.",
  },
  {
    title: "2. How we use your information",
    body: "Your information is used to run the platform: authenticating you, tracking course progress, issuing certificates, enabling messaging between students and instructors, and sending account-related emails such as verification and password reset links.",
  },
  {
    title: "3. Third-party services",
    body: "Uploaded images and videos (avatars, course thumbnails, lesson content) are stored with Cloudinary. Google Sign-In is used as an optional login method. The AI course tutor is powered by Google's Gemini API and processes the course content and questions you ask it in that chat.",
  },
  {
    title: "4. Cookies",
    body: "LearnHub uses a small number of essential, HTTP-only cookies to keep you signed in. These are required for the platform to function and aren't used for advertising or tracking.",
  },
  {
    title: "5. Data retention",
    body: "Your data is kept for as long as your account exists. If you'd like your account and associated data removed, this can be requested through an administrator.",
  },
  {
    title: "6. A note on this policy",
    body: "LearnHub is a demo platform built for a university final-year project. This policy describes how the demo actually handles data, but it isn't a formal legal document.",
  },
];

export default function PrivacyPage() {
  return (
    <div className="mx-auto max-w-3xl space-y-8 px-4 py-16 sm:px-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight">Privacy Policy</h1>
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
