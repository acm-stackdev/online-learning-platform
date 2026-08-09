// Matches LearnHub.Models.Entities.CourseStatus — backend serializes enums as
// their underlying int (no JsonStringEnumConverter configured).
export enum CourseStatus {
  Draft = 0,
  PendingApproval = 1,
  Published = 2,
  Rejected = 3,
}

export interface CourseListItem {
  id: number;
  title: string;
  description: string;
  thumbnailUrl: string | null;
  category: string | null;
  status: CourseStatus;
  instructorId: number;
  instructorName: string;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

// Matches LearnHub.Models.Entities.ContentType.
export enum ContentType {
  Video = 0,
  Pdf = 1,
}

export interface Lesson {
  id: number;
  title: string;
  contentType: ContentType;
  contentUrl: string | null;
  duration: number;
  order: number;
}

export interface Section {
  id: number;
  title: string;
  order: number;
  lessons: Lesson[];
}

export interface CourseDetail extends CourseListItem {
  sections: Section[];
  isEnrolled: boolean;
  isOwner: boolean;
}
