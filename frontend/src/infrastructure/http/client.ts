import { normalizeErrorResponse, ApiError } from "./errors";

const BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ?? "";

function newCorrelationId(): string {
  // crypto.randomUUID is available in Node 19+ and modern browsers
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  return `${Date.now()}-${Math.random().toString(36).slice(2)}`;
}

type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

interface RequestOptions {
  /** Query string already built (e.g. "?foo=bar"). */
  query?: string;
  body?: unknown;
  /** Extra headers to merge in (e.g. Idempotency-Key). */
  headers?: Record<string, string>;
}

/**
 * Minimal HTTP client.
 * - Attaches X-Correlation-Id to every request.
 * - Normalizes non-2xx responses into ApiError.
 * - Returns parsed JSON body on success.
 */
async function request<T>(
  method: HttpMethod,
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const url = `${BASE_URL}/${path.replace(/^\//, "")}${options.query ?? ""}`;

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    "X-Correlation-Id": newCorrelationId(),
    ...options.headers,
  };

  let res: Response;
  try {
    res = await fetch(url, {
      method,
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    });
  } catch {
    // Network-level error (no response)
    throw new ApiError(0, "NETWORK_ERROR", "Network error — could not reach the server.");
  }

  if (!res.ok) {
    throw await normalizeErrorResponse(res);
  }

  // 204 No Content — return empty object cast to T
  if (res.status === 204) return {} as T;

  return res.json() as Promise<T>;
}

export const httpClient = {
  get: <T>(path: string, query?: string) =>
    request<T>("GET", path, { query }),

  post: <T>(path: string, body: unknown, headers?: Record<string, string>) =>
    request<T>("POST", path, { body, headers }),

  put: <T>(path: string, body: unknown) =>
    request<T>("PUT", path, { body }),

  delete: <T = void>(path: string) =>
    request<T>("DELETE", path),
};
