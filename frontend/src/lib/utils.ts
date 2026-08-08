import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function initials(username: string) {
  return username.slice(0, 2).toUpperCase()
}

// Lesson/section durations are stored in seconds (same unit as
// UpdateLessonProgressDto.WatchSeconds, so they're directly comparable).
export function formatDuration(seconds: number) {
  const minutes = Math.round(seconds / 60)
  const hours = Math.floor(minutes / 60)
  const mins = minutes % 60
  return hours > 0 ? `${hours}h ${mins}m` : `${mins}m`
}
