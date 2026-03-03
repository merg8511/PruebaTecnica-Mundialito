import type { ApiError } from "@/infrastructure/http/errors";

interface Props {
  error: ApiError | Error | unknown;
}

export function ErrorMessage({ error }: Props) {
  const isApiError =
    error instanceof Error && error.name === "ApiError";

  const message =
    isApiError
      ? (error as ApiError).message
      : error instanceof Error
        ? error.message
        : "An unexpected error occurred.";

  const detail =
    isApiError ? (error as ApiError).detail : undefined;

  const status =
    isApiError ? (error as ApiError).status : undefined;

  const label =
    status === 404
      ? "Not Found"
      : status === 409
        ? "Conflict"
        : status === 400
          ? "Validation Error"
          : status && status >= 500
            ? "Server Error"
            : "Error";

  return (
    <div style={{ border: "1px solid #c00", padding: "12px", borderRadius: 4 }}>
      <strong>{label}: </strong>
      {message}
      {detail && <p style={{ marginTop: 4, fontSize: "0.875em" }}>{detail}</p>}
    </div>
  );
}
