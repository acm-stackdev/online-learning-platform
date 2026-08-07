"use client";

import { GoogleLogin, GoogleOAuthProvider } from "@react-oauth/google";

import { googleLogin } from "@/lib/api/auth";
import type { GoogleLoginResult } from "@/types/auth";

export function GoogleSignInButton({
  onSuccess,
  onError,
}: {
  onSuccess: (result: GoogleLoginResult) => void;
  onError: (message: string) => void;
}) {
  const clientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;
  if (!clientId) return null;

  return (
    <GoogleOAuthProvider clientId={clientId}>
      <GoogleLogin
        onSuccess={async (credentialResponse) => {
          if (!credentialResponse.credential) {
            onError("Google sign-in did not return a token.");
            return;
          }
          try {
            const result = await googleLogin(credentialResponse.credential);
            onSuccess(result);
          } catch (err) {
            onError(
              err instanceof Error ? err.message : "Google sign-in failed."
            );
          }
        }}
        onError={() => onError("Google sign-in failed.")}
        width="100%"
      />
    </GoogleOAuthProvider>
  );
}
