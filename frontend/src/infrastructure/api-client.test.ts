import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError, load } from "@/core/api-error";
import { apiGet, apiPost, buildQuery } from "./api-client";

function jsonResponse(status: number, body: unknown, headers: Record<string, string> = {}): Response {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json", ...headers },
  });
}

describe("api-client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("omite parámetros vacíos en la query string", () => {
    expect(buildQuery({ pageNumber: 1, name: "", teamId: undefined, status: null, sortDirection: "desc" })).toBe(
      "?pageNumber=1&sortDirection=desc",
    );
  });

  it("devuelve el cuerpo JSON en respuestas exitosas y envía X-Correlation-Id", async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(200, { data: [], pageNumber: 1, pageSize: 10, totalRecords: 0, totalPages: 0 }));
    vi.stubGlobal("fetch", fetchMock);

    const page = await apiGet<{ totalRecords: number }>("/api/teams", { query: { pageNumber: 1 } });

    expect(page.totalRecords).toBe(0);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toMatch(/\/api\/teams\?pageNumber=1$/);
    expect((init.headers as Record<string, string>)["X-Correlation-Id"]).toMatch(/^web-/);
  });

  it("envía la Idempotency-Key en los POST", async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(201, { matchId: "x" }));
    vi.stubGlobal("fetch", fetchMock);

    await apiPost("/api/matches/1/result", { homeGoals: 0 }, { idempotencyKey: "clave-123" });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect((init.headers as Record<string, string>)["Idempotency-Key"]).toBe("clave-123");
    expect(init.method).toBe("POST");
  });

  it("convierte ProblemDetails en ApiError con código, traceId y errores por campo", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        jsonResponse(409, {
          title: "Conflicto con el estado actual del recurso",
          status: 409,
          detail: "El partido ya tiene un resultado registrado.",
          errorCode: "Match.ResultAlreadyRegistered",
          traceId: "abc123",
          errors: { homeGoals: ["Valor no válido."] },
        }),
      ),
    );

    const error = await apiGet("/api/matches/1").catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(ApiError);
    const apiError = error as ApiError;
    expect(apiError.isConflict).toBe(true);
    expect(apiError.message).toBe("El partido ya tiene un resultado registrado.");
    expect(apiError.errorCode).toBe("Match.ResultAlreadyRegistered");
    expect(apiError.traceId).toBe("abc123");
    expect(apiError.fieldErrors.homeGoals).toEqual(["Valor no válido."]);
  });

  it("usa el header X-Trace-Id si el cuerpo del error no es JSON", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("Bad Gateway", { status: 502, headers: { "X-Trace-Id": "trace-502" } })));

    const error = (await apiGet("/api/teams").catch((caught: unknown) => caught)) as ApiError;

    expect(error.status).toBe(502);
    expect(error.traceId).toBe("trace-502");
    expect(error.title).toBe("Error del servidor");
  });

  it("informa que la API no está disponible cuando falla la red", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("fetch failed")));

    const result = await load(() => apiGet("/api/teams"));

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.error.errorCode).toBe("Api.Unreachable");
      expect(result.error.status).toBe(503);
    }
  });

  it("load no oculta errores que no provienen de la API", async () => {
    await expect(load(() => Promise.reject(new RangeError("bug")))).rejects.toThrow(RangeError);
  });
});
