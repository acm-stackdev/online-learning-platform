"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

import { refreshSession } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

// Access token lives 60 minutes (Jwt:ExpiryMinutes) — refresh a bit early so
// an active user never actually hits expiry mid-session. The refresh token
// itself lasts 7 days server-side; only a failure here (it's invalid,
// expired, or revoked) means the session is genuinely over.
const REFRESH_INTERVAL_MS = 50 * 60 * 1000;

export function SessionRefresher() {
  const router = useRouter();

  useEffect(() => {
    const id = setInterval(async () => {
      try {
        await refreshSession();
      } catch (err) {
        if (err instanceof ApiError && err.status === 401) {
          router.push("/login?expired=1");
        }
        // Other errors (e.g. a network blip) are left for the next tick.
      }
    }, REFRESH_INTERVAL_MS);

    return () => clearInterval(id);
  }, [router]);

  return null;
}
