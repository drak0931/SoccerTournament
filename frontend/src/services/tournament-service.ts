import type { PagedResult, StandingResponse, TopScorerResponse } from "@/core/api-types";
import { apiGet } from "@/infrastructure/api-client";
import type { Paging } from "@/lib/search-params";

export const STANDING_SORT_FIELDS = ["position", "points", "goalDifference", "goalsFor", "teamName"] as const;
export const SCORER_SORT_FIELDS = ["goals", "playerName", "teamName"] as const;

export interface StandingFilters extends Paging {
  teamName?: string;
}

export interface ScorerFilters extends Paging {
  teamId?: string;
}

/** Tabla de posiciones: orden oficial por puntos, diferencia de gol y goles a favor (calculado en la API). */
export function getStandings(filters: StandingFilters): Promise<PagedResult<StandingResponse>> {
  return apiGet("/api/standings", {
    query: {
      pageNumber: filters.pageNumber,
      pageSize: filters.pageSize,
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection,
      teamName: filters.teamName,
    },
  });
}

export function getTopScorers(filters: ScorerFilters): Promise<PagedResult<TopScorerResponse>> {
  return apiGet("/api/scorers", {
    query: {
      pageNumber: filters.pageNumber,
      pageSize: filters.pageSize,
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection,
      teamId: filters.teamId,
    },
  });
}
