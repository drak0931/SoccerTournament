import { ApiError } from "@/core/api-error";
import type { ProblemDetails } from "@/core/api-types";

// Cliente HTTP de la API. Solo se usa en el servidor de Next.js (Server Components y Server Actions):
// la URL de la API nunca llega al navegador y no se requiere CORS.

const REQUEST_TIMEOUT_MS = 10_000;

export type QueryValue = string | number | boolean | null | undefined;

export interface RequestOptions {
  query?: Record<string, QueryValue>;
  idempotencyKey?: string;
  correlationId?: string;
}

export function apiBaseUrl(): string {
  return (process.env.API_BASE_URL ?? "http://localhost:5019").replace(/\/+$/, "");
}

/** Arma la query string omitiendo valores vacíos. */
export function buildQuery(query: Record<string, QueryValue> | undefined): string {
  if (!query) return "";

  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== null && value !== "") {
      params.set(key, String(value));
    }
  }

  const text = params.toString();
  return text ? `?${text}` : "";
}

export function apiGet<T>(path: string, options: RequestOptions = {}): Promise<T> {
  return request<T>("GET", path, undefined, options);
}

export function apiPost<T>(path: string, body: unknown, options: RequestOptions = {}): Promise<T> {
  return request<T>("POST", path, body, options);
}

async function request<T>(method: string, path: string, body: unknown, options: RequestOptions): Promise<T> {
  const headers: Record<string, string> = {
    Accept: "application/json",
    // Permite seguir la operación en los logs de la API (X-Correlation-Id)
    "X-Correlation-Id": options.correlationId ?? `web-${crypto.randomUUID()}`,
  };

  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (options.idempotencyKey) headers["Idempotency-Key"] = options.idempotencyKey;

  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl()}${path}${buildQuery(options.query)}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      cache: "no-store",
      signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
    });
  } catch {
    // Red caída, API detenida o tiempo de espera agotado
    throw ApiError.unreachable();
  }

  if (!response.ok) {
    throw ApiError.fromProblem(response.status, await readProblem(response), response.headers.get("X-Trace-Id"));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return null;
  }
}
