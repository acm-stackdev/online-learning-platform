import { cache } from "react";

import { serverApiFetch } from "@/lib/api/server";
import type { UserResponse } from "@/types/auth";

// Wrapped in React's cache() so the (app) layout and any page under it can
// both call this within the same request without a duplicate round trip.
export const getCurrentUser = cache(() =>
  serverApiFetch<UserResponse>("/api/auth/me")
);
