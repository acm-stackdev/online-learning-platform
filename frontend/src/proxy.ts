import { NextResponse, type NextRequest } from "next/server";

function mergeCookies(originalCookieHeader: string, setCookies: string[]) {
  const jar = new Map<string, string>();

  for (const pair of originalCookieHeader.split(";")) {
    const [name, ...rest] = pair.trim().split("=");
    if (name) jar.set(name, rest.join("="));
  }

  for (const setCookie of setCookies) {
    const [pair] = setCookie.split(";");
    const [name, ...rest] = pair.trim().split("=");
    if (name) jar.set(name, rest.join("="));
  }

  return Array.from(jar, ([name, value]) => `${name}=${value}`).join("; ");
}

export async function proxy(request: NextRequest) {
  const cookie = request.headers.get("cookie") ?? "";

  const meRes = await fetch(`${process.env.API_URL}/api/auth/me`, {
    headers: { cookie },
  });

  if (meRes.ok) return NextResponse.next();

  // Access token expired but the refresh token (7 days) may still be good —
  // try a silent refresh before giving up, so a closed-laptop gap shorter
  // than that doesn't force a re-login.
  const refreshRes = await fetch(`${process.env.API_URL}/api/auth/refresh`, {
    method: "POST",
    headers: { cookie, "X-Requested-With": "LearnHub" },
  });

  if (!refreshRes.ok) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("expired", "1");
    return NextResponse.redirect(loginUrl);
  }

  const setCookies = refreshRes.headers.getSetCookie();

  // Propagate the refreshed cookies two ways: onto the request headers, so
  // the Server Component render that follows this proxy sees the new
  // access token immediately (not just the browser on its *next* request),
  // and onto the response headers, so the browser actually stores them.
  const requestHeaders = new Headers(request.headers);
  requestHeaders.set("cookie", mergeCookies(cookie, setCookies));

  const response = NextResponse.next({ request: { headers: requestHeaders } });
  for (const setCookie of setCookies) {
    response.headers.append("Set-Cookie", setCookie);
  }
  return response;
}

export const config = {
  matcher: [
    "/dashboard/:path*",
    "/my-courses/:path*",
    "/account/:path*",
    "/messages/:path*",
    "/become-instructor/:path*",
    "/instructor/:path*",
    "/admin/:path*",
  ],
};
