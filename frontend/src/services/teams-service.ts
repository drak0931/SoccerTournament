import type { PagedResult, PlayerPosition, PlayerResponse, TeamResponse } from "@/core/api-types";
import { apiGet } from "@/infrastructure/api-client";
import type { Paging } from "@/lib/search-params";

// Campos ordenables: deben coincidir con la lista blanca de la API (GetTeamsQuery / GetPlayersQuery)
export const TEAM_SORT_FIELDS = ["fullName", "shortName", "foundedYear", "createdAt"] as const;
export const PLAYER_SORT_FIELDS = ["lastName", "firstName", "jerseyNumber", "position", "teamName"] as const;

export interface TeamFilters extends Paging {
  name?: string;
  foundedFrom?: number;
  foundedTo?: number;
}

export interface PlayerFilters extends Paging {
  name?: string;
  position?: PlayerPosition;
}

export function getTeams(filters: TeamFilters): Promise<PagedResult<TeamResponse>> {
  return apiGet("/api/teams", {
    query: {
      pageNumber: filters.pageNumber,
      pageSize: filters.pageSize,
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection,
      name: filters.name,
      foundedFrom: filters.foundedFrom,
      foundedTo: filters.foundedTo,
    },
  });
}

/** Lista corta para selectores (el torneo tiene pocos equipos). */
export async function getTeamOptions(): Promise<TeamResponse[]> {
  const page = await getTeams({ pageNumber: 1, pageSize: 100, sortBy: "fullName", sortDirection: "asc" });
  return page.data;
}

export function getTeam(teamId: string): Promise<TeamResponse> {
  return apiGet(`/api/teams/${encodeURIComponent(teamId)}`);
}

export function getTeamPlayers(teamId: string, filters: PlayerFilters): Promise<PagedResult<PlayerResponse>> {
  return apiGet(`/api/teams/${encodeURIComponent(teamId)}/players`, {
    query: {
      pageNumber: filters.pageNumber,
      pageSize: filters.pageSize,
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection,
      name: filters.name,
      position: filters.position,
    },
  });
}

/** Plantilla completa de un equipo (para elegir goleadores). */
export async function getTeamRoster(teamId: string): Promise<PlayerResponse[]> {
  const page = await getTeamPlayers(teamId, { pageNumber: 1, pageSize: 100, sortBy: "jerseyNumber", sortDirection: "asc" });
  return page.data;
}
