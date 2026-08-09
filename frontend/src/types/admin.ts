import type { Role } from "@/types/auth";

export interface AdminUser {
  id: number;
  username: string;
  email: string;
  role: Role;
  isSuspended: boolean;
  isEmailVerified: boolean;
  createdAt: string;
  lastActiveAt: string | null;
}

export interface PlatformStats {
  totalUsers: number;
  studentCount: number;
  instructorCount: number;
  adminCount: number;
  suspendedCount: number;
  totalCourses: number;
  draftCourseCount: number;
  pendingApprovalCourseCount: number;
  publishedCourseCount: number;
  rejectedCourseCount: number;
  totalEnrollments: number;
  completedEnrollmentCount: number;
  inProgressEnrollmentCount: number;
}
