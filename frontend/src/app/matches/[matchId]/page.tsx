import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ErrorPanel } from "@/components/error-panel";
import { Alert, ButtonLink, Card, EmptyState, PageHeader, StatusBadge, TableShell, Td, Th } from "@/components/ui";
import { load } from "@/core/api-error";
import type { MatchDetailResponse } from "@/core/api-types";
import { newIdempotencyKey } from "@/features/match-result/result-model";
import { RegisterResultForm } from "@/features/match-result/register-result-form";
import { formatDateTime } from "@/lib/datetime";
import { getString, type RawSearchParams } from "@/lib/search-params";
import { getMatch } from "@/services/matches-service";
import { getTeamRoster } from "@/services/teams-service";

export const metadata: Metadata = { title: "Detalle del partido" };

export default async function MatchDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ matchId: string }>;
  searchParams: Promise<RawSearchParams>;
}) {
  const { matchId } = await params;
  const justRegistered = getString(await searchParams, "registered") === "1";

  const detail = await load(() => getMatch(matchId));
  if (!detail.ok && detail.error.isNotFound) notFound();
  if (!detail.ok) return <ErrorPanel error={detail.error} context="Partido" />;

  const { match, goals } = detail.data;

  return (
    <div className="space-y-6">
      <PageHeader
        title={`${match.homeTeamName} vs ${match.awayTeamName}`}
        description={formatDateTime(match.scheduledAt)}
        actions={<ButtonLink href="/matches" variant="secondary">← Volver al calendario</ButtonLink>}
      />

      {justRegistered ? (
        <Alert tone="success" title="Resultado registrado">
          La tabla de posiciones y los goleadores ya reflejan este resultado.
        </Alert>
      ) : null}

      <Card>
        <div className="flex flex-wrap items-center justify-center gap-6 text-center">
          <p className="min-w-40 text-lg font-semibold">{match.homeTeamName}</p>
          <p className="text-4xl font-bold tabular-nums text-slate-900">
            {match.status === "finished" ? `${match.homeGoals} - ${match.awayGoals}` : "vs"}
          </p>
          <p className="min-w-40 text-lg font-semibold">{match.awayTeamName}</p>
        </div>
        <div className="mt-3 flex justify-center gap-3 text-sm text-slate-600">
          <StatusBadge status={match.status} />
          {match.resultRegisteredAt ? <span>Registrado el {formatDateTime(match.resultRegisteredAt)}</span> : null}
        </div>
      </Card>

      {match.status === "finished" ? (
        <Card title="Goles">
          {goals.length === 0 ? (
            <EmptyState message="Partido sin goles." />
          ) : (
            <TableShell>
              <thead>
                <tr>
                  <Th align="center">Minuto</Th>
                  <Th>Jugador</Th>
                  <Th>Equipo</Th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {goals.map((goal) => (
                  <tr key={goal.id}>
                    <Td align="center" strong>
                      {goal.minute}&apos;
                    </Td>
                    <Td>{goal.playerName}</Td>
                    <Td>{goal.teamId === match.homeTeamId ? match.homeTeamName : match.awayTeamName}</Td>
                  </tr>
                ))}
              </tbody>
            </TableShell>
          )}
        </Card>
      ) : null}

      {match.status === "scheduled" ? <ResultSection detail={detail.data} /> : null}

      {match.status === "cancelled" ? <Alert tone="info" title="Partido cancelado">No admite registro de resultado.</Alert> : null}
    </div>
  );
}

// Formulario de resultado con las plantillas de ambos equipos para elegir goleadores
async function ResultSection({ detail }: { detail: MatchDetailResponse }) {
  const { match } = detail;
  const [homePlayers, awayPlayers] = await Promise.all([
    load(() => getTeamRoster(match.homeTeamId)),
    load(() => getTeamRoster(match.awayTeamId)),
  ]);

  if (!homePlayers.ok) return <ErrorPanel error={homePlayers.error} context="Plantilla local" />;
  if (!awayPlayers.ok) return <ErrorPanel error={awayPlayers.error} context="Plantilla visitante" />;

  return (
    <Card title="Registrar resultado">
      <RegisterResultForm
        match={match}
        homePlayers={homePlayers.data}
        awayPlayers={awayPlayers.data}
        initialIdempotencyKey={newIdempotencyKey()}
      />
    </Card>
  );
}
