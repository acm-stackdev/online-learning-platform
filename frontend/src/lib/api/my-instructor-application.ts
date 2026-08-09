import { serverApiFetch } from "@/lib/api/server";
import type { InstructorApplication } from "@/types/instructorApplication";

// GET .../mine returns ALL of the user's applications (resubmission after a
// rejection is allowed), newest first — callers want [0] for "current status".
export async function getMyInstructorApplications(): Promise<InstructorApplication[]> {
  const result = await serverApiFetch<InstructorApplication[]>(
    "/api/instructor-applications/mine"
  );
  return result ?? [];
}
