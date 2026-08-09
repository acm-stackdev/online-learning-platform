import type { CourseListItem } from "@/types/course";

// Matches LearnHub.Models.Entities.ApplicationStatus.
export enum ApplicationStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
}

export interface Enrollment {
  id: number;
  course: CourseListItem;
  enrolledAt: string;
  completedAt: string | null;
}

export interface StudentDashboard {
  totalEnrollments: number;
  completedCount: number;
  inProgressCount: number;
  certificateCount: number;
  instructorApplicationStatus: ApplicationStatus | null;
  enrollments: Enrollment[];
}

export interface InstructorDashboard {
  totalCourses: number;
  draftCourseCount: number;
  pendingApprovalCourseCount: number;
  publishedCourseCount: number;
  rejectedCourseCount: number;
  totalStudentsEnrolled: number;
  courses: CourseListItem[];
}
