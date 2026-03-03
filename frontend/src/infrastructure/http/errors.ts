/** Normalized error shape consumed by every layer above infrastructure. */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string,
    message: string,
    public readonly detail?: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/** Codes that are safe to present directly to the user. */
export type ApiErrorCode =
  | "BAD_REQUEST"
  | "NOT_FOUND"
  | "CONFLICT"
  | "SERVER_ERROR"
  | "NETWORK_ERROR"
  | "UNKNOWN";

function codeFromStatus(status: number): ApiErrorCode {
  if (status === 400) return "BAD_REQUEST";
  if (status === 404) return "NOT_FOUND";
  if (status === 409) return "CONFLICT";
  if (status >= 500) return "SERVER_ERROR";
  return "UNKNOWN";
}

/**
 * Build a normalized ApiError from a failed fetch Response.
 * Attempts to read the error body; falls back gracefully.
 */
export async function normalizeErrorResponse(res: Response): Promise<ApiError> {
  let message = `HTTP ${res.status}`;
  let detail: string | undefined;

  try {
    const body = await res.json();
    // Backend error envelope: { title, detail, errors[] }
    if (typeof body?.title === "string") message = body.title;
    else if (typeof body?.message === "string") message = body.message;
    if (typeof body?.detail === "string") detail = body.detail;
    else if (Array.isArray(body?.errors) && body.errors.length > 0) {
      detail = body.errors.join("; ");
    }
  } catch {
    // body is not JSON — use status text
    message = res.statusText || message;
  }

  return new ApiError(res.status, codeFromStatus(res.status), message, detail);
}
