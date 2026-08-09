import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { apiFetch, ApiError } from "@/lib/api/client";

function mockFetchOnce(response: {
  ok: boolean;
  status: number;
  json?: () => Promise<unknown>;
}) {
  const fetchMock = vi.fn().mockResolvedValue({
    ok: response.ok,
    status: response.status,
    json: response.json ?? (async () => ({})),
  });
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

describe("apiFetch", () => {
  beforeEach(() => {
    process.env.NEXT_PUBLIC_API_URL = "http://test-api";
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("does not set X-Requested-With on a GET request", async () => {
    const fetchMock = mockFetchOnce({ ok: true, status: 200, json: async () => ({ id: 1 }) });

    await apiFetch("/api/courses");

    const headers = fetchMock.mock.calls[0][1].headers as Headers;
    expect(headers.has("X-Requested-With")).toBe(false);
  });

  it("sets X-Requested-With on a non-GET request", async () => {
    const fetchMock = mockFetchOnce({ ok: true, status: 200, json: async () => ({}) });

    await apiFetch("/api/courses", { method: "POST", body: JSON.stringify({}) });

    const headers = fetchMock.mock.calls[0][1].headers as Headers;
    expect(headers.get("X-Requested-With")).toBe("LearnHub");
  });

  it("sets Content-Type: application/json for a JSON body", async () => {
    const fetchMock = mockFetchOnce({ ok: true, status: 200, json: async () => ({}) });

    await apiFetch("/api/courses", { method: "POST", body: JSON.stringify({ title: "x" }) });

    const headers = fetchMock.mock.calls[0][1].headers as Headers;
    expect(headers.get("Content-Type")).toBe("application/json");
  });

  it("does not set Content-Type for a FormData body", async () => {
    const fetchMock = mockFetchOnce({ ok: true, status: 200, json: async () => ({}) });

    await apiFetch("/api/lessons", { method: "POST", body: new FormData() });

    const headers = fetchMock.mock.calls[0][1].headers as Headers;
    expect(headers.has("Content-Type")).toBe(false);
  });

  it("throws an ApiError with the parsed message on a non-2xx response", async () => {
    mockFetchOnce({
      ok: false,
      status: 409,
      json: async () => ({ message: "Already enrolled." }),
    });

    await expect(apiFetch("/api/enrollments", { method: "POST" })).rejects.toMatchObject({
      message: "Already enrolled.",
      status: 409,
    });
  });

  it("falls back to the first validation error when there's no message", async () => {
    mockFetchOnce({
      ok: false,
      status: 400,
      json: async () => ({ errors: { Email: ["Email is required."] } }),
    });

    await expect(apiFetch("/api/auth/register", { method: "POST" })).rejects.toMatchObject({
      message: "Email is required.",
    });
  });

  it("falls back to a generic message when the error body isn't JSON", async () => {
    mockFetchOnce({
      ok: false,
      status: 500,
      json: async () => {
        throw new Error("not json");
      },
    });

    await expect(apiFetch("/api/courses/1")).rejects.toMatchObject({
      message: "Something went wrong. Please try again.",
    });
  });

  it("rejects with an ApiError instance", async () => {
    mockFetchOnce({ ok: false, status: 404, json: async () => ({ message: "Not found." }) });

    await expect(apiFetch("/api/courses/999")).rejects.toBeInstanceOf(ApiError);
  });

  it("returns undefined for a 204 response", async () => {
    mockFetchOnce({ ok: true, status: 204 });

    const result = await apiFetch("/api/enrollments/1", { method: "DELETE" });

    expect(result).toBeUndefined();
  });

  it("returns the parsed JSON body on success", async () => {
    mockFetchOnce({ ok: true, status: 200, json: async () => ({ id: 1, title: "React" }) });

    const result = await apiFetch<{ id: number; title: string }>("/api/courses/1");

    expect(result).toEqual({ id: 1, title: "React" });
  });
});
