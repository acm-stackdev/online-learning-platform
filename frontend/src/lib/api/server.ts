import { cookies } from "next/headers";

/**
 * For Server Components/Route Handlers calling the backend on behalf of the
 * signed-in user. Browser cookies aren't forwarded to server-side fetch()
 * automatically, so they're read from the incoming request and re-attached
 * here. Returns null on any non-2xx (401 for an expired/missing session is
 * the expected case — callers decide how to handle that, e.g. redirect).
 */
export async function serverApiFetch<T>(path: string): Promise<T | null> {
  const cookieStore = await cookies();

  const res = await fetch(`${process.env.API_URL}${path}`, {
    headers: { cookie: cookieStore.toString() },
    cache: "no-store",
  });

  if (!res.ok) return null;
  return res.json();
}
