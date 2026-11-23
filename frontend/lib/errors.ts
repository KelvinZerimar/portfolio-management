import { isAxiosError } from "axios";

export interface ApiError {
  code: string | null;
  description: string | null;
  metadata: Record<string, unknown>;
}

// Every backend error response is `List<Error>` (ErrorOr), serialized as an array of
// { code, description, metadata, ... }. The creation POSTs replay their response from
// the idempotency cache, which re-serializes it with PascalCase keys
// ({ Code, Description, Metadata }), so both spellings are read.
export function getApiError(error: unknown): ApiError | null {
  if (!isAxiosError(error)) return null;
  const data: unknown = error.response?.data;
  if (!Array.isArray(data) || typeof data[0] !== "object" || data[0] === null) return null;
  const first = data[0] as Record<string, unknown>;
  const pick = (camel: string, pascal: string) => first[camel] ?? first[pascal];
  const code = pick("code", "Code");
  const description = pick("description", "Description");
  const metadata = pick("metadata", "Metadata");
  return {
    code: typeof code === "string" ? code : null,
    description: typeof description === "string" ? description : null,
    metadata: typeof metadata === "object" && metadata !== null ? (metadata as Record<string, unknown>) : {},
  };
}

// The first error's human-readable text — e.g. "A portfolio with the name 'Principal'
// already exists." — instead of a generic "something went wrong" that hides what the
// user actually needs to fix.
export function getApiErrorMessage(error: unknown): string | null {
  return getApiError(error)?.description ?? null;
}
