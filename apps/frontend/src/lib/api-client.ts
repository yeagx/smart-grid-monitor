/**
 * Shared API client for the Smart Grid Monitor backend.
 *
 * All API requests go through this module to centralise
 * base URL resolution, error handling, and request configuration.
 */

/** Base URL read from the Vite environment at build time. */
const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/+$/, '') ?? 'http://localhost:5091';

/** Standard API error shape surfaced to the UI. */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
    public readonly detail?: string,
  ) {
    super(detail ?? `${status} ${statusText}`);
    this.name = 'ApiError';
  }
}

/**
 * Typed fetch wrapper.
 *
 * - Prepends `API_BASE_URL` to relative paths.
 * - Checks the HTTP status before parsing JSON.
 * - Throws {@link ApiError} for non-2xx responses.
 * - Propagates `AbortSignal` for request cancellation.
 */
export async function apiFetch<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const url = `${API_BASE_URL}${path}`;

  let response: Response;
  try {
    response = await fetch(url, {
      ...init,
      headers: {
        Accept: 'application/json',
        ...init?.headers,
      },
    });
  } catch (err) {
    if (err instanceof DOMException && err.name === 'AbortError') {
      throw err;
    }
    throw new ApiError(0, 'Network Error', 'Unable to reach the backend. Is the API server running?');
  }

  if (!response.ok) {
    let detail: string | undefined;
    try {
      const body = await response.json();
      detail = body?.detail ?? body?.title ?? undefined;
    } catch {
      /* ignore parse failures for error bodies */
    }
    throw new ApiError(response.status, response.statusText, detail);
  }

  return response.json() as Promise<T>;
}
