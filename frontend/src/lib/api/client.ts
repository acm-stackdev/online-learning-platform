export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

interface ValidationErrorBody {
  errors?: Record<string, string[]>;
}

async function parseErrorMessage(res: Response): Promise<string> {
  try {
    const body: { message?: string } & ValidationErrorBody = await res.json();
    if (body.message) return body.message;
    if (body.errors) {
      const first = Object.values(body.errors)[0];
      if (first?.[0]) return first[0];
    }
  } catch {
    // response wasn't JSON — fall through to the generic message
  }
  return "Something went wrong. Please try again.";
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const method = options.method ?? "GET";
  const headers = new Headers(options.headers);

  if (method !== "GET") {
    headers.set("X-Requested-With", "LearnHub");
  }
  // For FormData bodies (multipart file uploads), leave Content-Type unset —
  // the browser fills in the correct boundary automatically. Setting it
  // manually would drop the boundary and break the upload.
  if (options.body && !(options.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}${path}`, {
    ...options,
    method,
    headers,
    credentials: "include",
  });

  if (!res.ok) {
    throw new ApiError(await parseErrorMessage(res), res.status);
  }

  if (res.status === 204) return undefined as T;
  return res.json();
}
