import type {
  MatchDetailResponse,
  MatchResponse,
  MatchResultResponse,
  MatchStatus,
  PagedResult,
  RegisterMatchResultRequest,
} from "@/core/api-types";
import { apiGet, apiPost } from "@/infrastructure/api-client";
import { endOfLocalDayUtc, startOfLocalDayUtc } from "@/lib/datetime";
import type { Paging } from "@/lib/search-params";

export const MATCH_SORT_FIELDS = ["scheduledAt", "status", "createdAt"] as const;

export interface MatchFilters extends Paging {
  /** Día local (yyyy-MM-dd) en hora de El Salvador. */
  dateFrom?: string;
  /** Día local (yyyy-MM-dd) en hora de El Salvador, inclusivo. */
  dateTo?: string;
  teamId?: string;
  status?: MatchStatus;
}

export function getMatches(filters: MatchFilters): Promise<PagedResult<MatchResponse>> {
  return apiGet("/api/matches", {
    query: {
      pageNumber: filters.pageNumber,
      pageSize: filters.pageSize,
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection,
      // La API filtra en UTC: se convierte el rango de días locales a instantes UTC
      dateFrom: startOfLocalDayUtc(filters.dateFrom),
      dateTo: endOfLocalDayUtc(filters.dateTo),
      teamId: filters.teamId,
      status: filters.status,
    },
  });
}

export function getMatch(matchId: string): Promise<MatchDetailResponse> {
  return apiGet(`/api/matches/${encodeURIComponent(matchId)}`);
}

/**
 * Registra el resultado. La misma idempotencyKey en un reintento devuelve la respuesta original sin duplicar.
 */
export function registerMatchResult(
  matchId: string,
  request: RegisterMatchResultRequest,
  idempotencyKey: string,
): Promise<MatchResultResponse> {
  return apiPost(`/api/matches/${encodeURIComponent(matchId)}/result`, request, { idempotencyKey });
}
