"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError } from "@/core/api-error";
import type { RegisterMatchResultRequest } from "@/core/api-types";
import { registerMatchResult } from "@/services/matches-service";

export type RegisterResultState =
  | { status: "idle" }
  | {
      status: "error";
      httpStatus: number;
      title: string;
      message: string;
      errorCode?: string;
      traceId?: string;
      fieldErrors: string[];
    };

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * Server Action: envía el resultado a la API desde el servidor de Next.js (la API no se expone al navegador).
 * Ante éxito refresca las vistas afectadas (partido, posiciones, goleadores) y redirige al detalle.
 */
export async function registerResultAction(_previous: RegisterResultState, formData: FormData): Promise<RegisterResultState> {
  const matchId = String(formData.get("matchId") ?? "");
  const idempotencyKey = String(formData.get("idempotencyKey") ?? "");
  const payload = parsePayload(formData.get("payload"));

  if (!GUID.test(matchId) || !idempotencyKey || payload === null) {
    return invalid("La solicitud no es válida. Recargue la página e intente de nuevo.");
  }

  try {
    await registerMatchResult(matchId, payload, idempotencyKey);
  } catch (error) {
    if (error instanceof ApiError) {
      return {
        status: "error",
        httpStatus: error.status,
        title: error.title,
        message: error.message,
        errorCode: error.errorCode,
        traceId: error.traceId,
        fieldErrors: Object.values(error.fieldErrors).flat(),
      };
    }
    throw error;
  }

  // El resultado cambia el partido, la tabla de posiciones y los goleadores
  revalidatePath("/", "layout");
  redirect(`/matches/${matchId}?registered=1`);
}

function parsePayload(value: FormDataEntryValue | null): RegisterMatchResultRequest | null {
  if (typeof value !== "string") return null;

  try {
    const parsed = JSON.parse(value) as Partial<RegisterMatchResultRequest>;
    if (typeof parsed.homeGoals !== "number" || typeof parsed.awayGoals !== "number" || !Array.isArray(parsed.goals)) {
      return null;
    }
    return { homeGoals: parsed.homeGoals, awayGoals: parsed.awayGoals, goals: parsed.goals };
  } catch {
    return null;
  }
}

function invalid(message: string): RegisterResultState {
  return { status: "error", httpStatus: 400, title: "Datos no válidos", message, fieldErrors: [] };
}
