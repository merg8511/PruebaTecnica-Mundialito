/**
 * Build a URLSearchParams string from an arbitrary params object.
 * Undefined/null values are omitted. Arrays are expanded to repeated keys.
 */
export function buildQueryParams(
  params: Record<string, string | number | boolean | null | undefined>,
): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") continue;
    search.append(key, String(value));
  }

  const str = search.toString();
  return str ? `?${str}` : "";
}
