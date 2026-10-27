import type { ProblemDetails } from "./api-types";

/**
 * Error de la API normalizado a partir de ProblemDetails. Conserva el traceId para soporte técnico.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  readonly errorCode?: string;
  readonly traceId?: string;
  readonly fieldErrors: Record<string, string[]>;

  constructor(init: {
    status: number;
    title: string;
    message: string;
    errorCode?: string;
    traceId?: string;
    fieldErrors?: Record<string, string[]>;
  }) {
    super(init.message);
    this.name = "ApiError";
    this.status = init.status;
    this.title = init.title;
    this.errorCode = init.errorCode;
    this.traceId = init.traceId;
    this.fieldErrors = init.fieldErrors ?? {};
  }

  get isNotFound(): boolean {
    return this.status === 404;
  }

  get isConflict(): boolean {
    return this.status === 409;
  }

  get isValidation(): boolean {
    return this.status === 400;
  }

  /** Crea el error a partir del cuerpo ProblemDetails (o de un cuerpo inesperado). */
  static fromProblem(status: number, problem: ProblemDetails | null, traceIdHeader?: string | null): ApiError {
    return new ApiError({
      status,
      title: problem?.title ?? defaultTitle(status),
      message: problem?.detail ?? problem?.title ?? defaultTitle(status),
      errorCode: problem?.errorCode,
      traceId: problem?.traceId ?? traceIdHeader ?? undefined,
      fieldErrors: problem?.errors,
    });
  }

  /** La API no respondió (caída, red o tiempo de espera agotado). */
  static unreachable(): ApiError {
    return new ApiError({
      status: 503,
      title: "Servicio no disponible",
      message: "No se pudo conectar con la API del torneo. Verifique que esté en ejecución e intente de nuevo.",
      errorCode: "Api.Unreachable",
    });
  }
}

function defaultTitle(status: number): string {
  if (status === 400) return "Datos de entrada no válidos";
  if (status === 404) return "Recurso no encontrado";
  if (status === 409) return "Conflicto con el estado actual del recurso";
  if (status >= 500) return "Error del servidor";
  return "Error al procesar la solicitud";
}

/** Resultado explícito para las vistas: evita usar excepciones como control de flujo al renderizar. */
export type Loaded<T> = { ok: true; data: T } | { ok: false; error: ApiError };

export async function load<T>(operation: () => Promise<T>): Promise<Loaded<T>> {
  try {
    return { ok: true, data: await operation() };
  } catch (error) {
    if (error instanceof ApiError) {
      return { ok: false, error };
    }
    throw error;
  }
}
