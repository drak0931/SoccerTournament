// Contrato de la API REST (SoccerTournament.Api). Fechas en ISO 8601 UTC; enums en camelCase.

export type MatchStatus = "scheduled" | "finished" | "cancelled";

export type PlayerPosition = "goalkeeper" | "defender" | "midfielder" | "forward";

export type SortDirection = "asc" | "desc";

export interface PagedResult<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
}

export interface TeamResponse {
  id: string;
  fullName: string;
  shortName: string;
  foundedYear: string | null;
}

export interface PlayerResponse {
  id: string;
  teamId: string;
  teamName: string;
  firstName: string | null;
  lastName: string;
  jerseyNumber: number;
  position: PlayerPosition;
}

export interface MatchResponse {
  id: string;
  homeTeamId: string;
  homeTeamName: string;
  awayTeamId: string;
  awayTeamName: string;
  scheduledAt: string;
  status: MatchStatus;
  homeGoals: number | null;
  awayGoals: number | null;
  resultRegisteredAt: string | null;
}

export interface GoalResponse {
  id: string;
  playerId: string;
  playerName: string;
  teamId: string;
  minute: number;
}

export interface MatchDetailResponse {
  match: MatchResponse;
  goals: GoalResponse[];
}

export interface StandingResponse {
  position: number;
  teamId: string;
  teamName: string;
  teamShortName: string;
  played: number;
  won: number;
  drawn: number;
  lost: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  points: number;
}

export interface TopScorerResponse {
  rank: number;
  playerId: string;
  playerName: string;
  teamId: string;
  teamName: string;
  goals: number;
}

export interface GoalInput {
  playerId: string;
  teamId: string;
  minute: number;
}

export interface RegisterMatchResultRequest {
  homeGoals: number;
  awayGoals: number;
  goals: GoalInput[];
}

export interface MatchResultResponse {
  matchId: string;
  status: MatchStatus;
  homeGoals: number;
  awayGoals: number;
  resultRegisteredAt: string;
}

/** RFC 7807 con las extensiones que agrega la API. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errorCode?: string;
  errors?: Record<string, string[]>;
}
