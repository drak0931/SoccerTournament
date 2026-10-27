import type { MatchStatus, PlayerPosition } from "@/core/api-types";

export const MATCH_STATUS_LABELS: Record<MatchStatus, string> = {
  scheduled: "Programado",
  finished: "Finalizado",
  cancelled: "Cancelado",
};

export const PLAYER_POSITION_LABELS: Record<PlayerPosition, string> = {
  goalkeeper: "Portero",
  defender: "Defensa",
  midfielder: "Mediocampista",
  forward: "Delantero",
};

export const MATCH_STATUSES = Object.keys(MATCH_STATUS_LABELS) as MatchStatus[];
export const PLAYER_POSITIONS = Object.keys(PLAYER_POSITION_LABELS) as PlayerPosition[];

export function playerFullName(player: { firstName: string | null; lastName: string }): string {
  return player.firstName ? `${player.firstName} ${player.lastName}` : player.lastName;
}

export function signed(value: number): string {
  return value > 0 ? `+${value}` : String(value);
}
