import type { GoalInput, RegisterMatchResultRequest } from "@/core/api-types";

// Modelo del formulario de resultado: reglas puras (sin React) para validar antes de enviar.
// La API vuelve a validar todo; esto solo mejora la experiencia del usuario.

export const MIN_MINUTE = 1;
export const MAX_MINUTE = 130;
export const MAX_GOALS = 50;

export interface GoalRow {
  key: string;
  teamId: string;
  playerId: string;
  minute: string;
}

export interface ResultDraft {
  homeTeamId: string;
  awayTeamId: string;
  homeGoals: string;
  awayGoals: string;
  goals: GoalRow[];
}

export function validateResult(draft: ResultDraft): string[] {
  const errors: string[] = [];
  const home = parseScore(draft.homeGoals);
  const away = parseScore(draft.awayGoals);

  if (home === null) errors.push(`El marcador local debe ser un número entre 0 y ${MAX_GOALS}.`);
  if (away === null) errors.push(`El marcador visitante debe ser un número entre 0 y ${MAX_GOALS}.`);

  draft.goals.forEach((goal, index) => {
    const label = `Gol ${index + 1}`;
    if (goal.teamId !== draft.homeTeamId && goal.teamId !== draft.awayTeamId) errors.push(`${label}: seleccione el equipo.`);
    if (!goal.playerId) errors.push(`${label}: seleccione el goleador.`);

    const minute = Number(goal.minute);
    if (!Number.isInteger(minute) || minute < MIN_MINUTE || minute > MAX_MINUTE) {
      errors.push(`${label}: el minuto debe estar entre ${MIN_MINUTE} y ${MAX_MINUTE}.`);
    }
  });

  if (home !== null && away !== null) {
    const homeScored = draft.goals.filter((goal) => goal.teamId === draft.homeTeamId).length;
    const awayScored = draft.goals.filter((goal) => goal.teamId === draft.awayTeamId).length;

    if (homeScored !== home) errors.push(`Registró ${homeScored} gol(es) del local, pero el marcador indica ${home}.`);
    if (awayScored !== away) errors.push(`Registró ${awayScored} gol(es) del visitante, pero el marcador indica ${away}.`);
  }

  return errors;
}

export function toRequest(draft: ResultDraft): RegisterMatchResultRequest {
  return {
    homeGoals: Number(draft.homeGoals),
    awayGoals: Number(draft.awayGoals),
    goals: draft.goals.map<GoalInput>((goal) => ({
      playerId: goal.playerId,
      teamId: goal.teamId,
      minute: Number(goal.minute),
    })),
  };
}

function parseScore(value: string): number | null {
  if (value.trim() === "") return null;
  const score = Number(value);
  return Number.isInteger(score) && score >= 0 && score <= MAX_GOALS ? score : null;
}

/**
 * Clave de idempotencia. Usa getRandomValues (disponible también fuera de HTTPS, a diferencia de randomUUID).
 */
export function newIdempotencyKey(): string {
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6]! & 0x0f) | 0x40;
  bytes[8] = (bytes[8]! & 0x3f) | 0x80;
  const hex = Array.from(bytes, (byte) => byte.toString(16).padStart(2, "0")).join("");
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
