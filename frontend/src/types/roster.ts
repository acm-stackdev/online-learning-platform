export interface RosterItem {
  enrollmentId: number;
  studentId: number;
  studentUsername: string;
  studentEmail: string;
  enrolledAt: string;
  completedAt: string | null;
}
