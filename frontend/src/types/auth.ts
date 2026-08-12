// Matches LearnHub.Models.Entities.Role — backend serializes enums as
// their underlying int (no JsonStringEnumConverter configured).
export enum Role {
  Student = 0,
  Instructor = 1,
  Admin = 2,
}

export const roleLabels: Record<Role, string> = {
  [Role.Student]: "Student",
  [Role.Instructor]: "Instructor",
  [Role.Admin]: "Admin",
};

export interface UserResponse {
  id: number;
  username: string;
  email: string;
  role: Role;
  avatarUrl: string | null;
  presenceStatus: string;
}

export interface MessageResponse {
  message: string;
}

export type RegisterResult = MessageResponse;
export type LoginResult = UserResponse;
export type GoogleLoginResult = UserResponse | MessageResponse;

export function isUserResponse(
  result: GoogleLoginResult
): result is UserResponse {
  return "role" in result;
}
