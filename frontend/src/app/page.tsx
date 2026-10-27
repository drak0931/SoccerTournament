import { ErrorPanel } from "@/components/error-panel";
import { Card, EmptyState, PageHeader, TextLink } from "@/components/ui";
import { load } from "@/core/api-error";
import { MatchesTable } from "@/features/matches/matches-table";
import { ScorersTable } from "@/features/scorers/scorers-table";
import { StandingsTable } from "@/features/standings/standings-table";
import { getMatches } from "@/services/matches-service";
import { getStandings, getTopScorers } from "@/services/tournament-service";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  // Consultas independientes en paralelo; cada sección maneja su propio error
  const [standings, scorers, pendingMatches] = await Promise.all([
    load(() => getStandings({ pageNumber: 1, pageSize: 10 })),
    load(() => getTopScorers({ pageNumber: 1, pageSize: 5 })),
    load(() => getMatches({ pageNumber: 1, pageSize: 5, status: "scheduled", sortBy: "scheduledAt", sortDirection: "asc" })),
  ]);

  return (
    <div className="space-y-6">
      <PageHeader title="Resumen del torneo" description="Tabla de posiciones, goleadores y próximos partidos." />

      <div className="grid gap-6 lg:grid-cols-[2fr_1fr]">
        <Card title="Tabla de posiciones" action={<TextLink href="/standings">Ver completa</TextLink>}>
          {standings.ok ? <StandingsTable rows={standings.data.data} /> : <ErrorPanel error={standings.error} />}
        </Card>

        <Card title="Goleadores" action={<TextLink href="/scorers">Ver todos</TextLink>}>
          {!scorers.ok ? (
            <ErrorPanel error={scorers.error} />
          ) : scorers.data.data.length === 0 ? (
            <EmptyState message="Aún no hay goles registrados." />
          ) : (
            <ScorersTable rows={scorers.data.data} />
          )}
        </Card>
      </div>

      <Card title="Partidos pendientes" action={<TextLink href="/matches?status=scheduled">Ver calendario</TextLink>}>
        {!pendingMatches.ok ? (
          <ErrorPanel error={pendingMatches.error} />
        ) : pendingMatches.data.data.length === 0 ? (
          <EmptyState message="No hay partidos pendientes." />
        ) : (
          <MatchesTable rows={pendingMatches.data.data} />
        )}
      </Card>
    </div>
  );
}
