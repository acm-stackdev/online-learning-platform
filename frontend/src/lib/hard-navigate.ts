// A full page navigation, not router.push/redirect — deliberately bypasses
// Next.js's client Router Cache. Needed after any auth-state change (login,
// logout, session-refresh failure): our cookies are set by a direct fetch to
// the backend API, so Next.js has no way to know they changed and won't
// invalidate layouts (e.g. PublicNavbar) cached from before the change.
export function hardNavigate(url: string) {
  window.location.href = url;
}
