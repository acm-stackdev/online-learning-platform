export interface LessonProgress {
  lessonId: number;
  lessonTitle: string;
  isCompleted: boolean;
  watchSeconds: number;
  lastWatchedAt: string;
}

export interface EnrollmentProgress {
  enrollmentId: number;
  totalLessons: number;
  completedLessons: number;
  percentComplete: number;
  isCourseCompleted: boolean;
  lessons: LessonProgress[];
}
