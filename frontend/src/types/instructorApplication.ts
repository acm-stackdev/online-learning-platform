import type { ApplicationStatus } from "@/types/dashboard";

export interface InstructorApplication {
  id: number;
  userId: number;
  applicantUsername: string;
  applicantEmail: string;
  message: string;
  status: ApplicationStatus;
  submittedAt: string;
  reviewedAt: string | null;
  reviewedByUserId: number | null;
  reviewedByUsername: string | null;
}
