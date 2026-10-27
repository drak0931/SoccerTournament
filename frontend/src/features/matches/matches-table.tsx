import type { MatchResponse } from "@/core/api-types";
import { SortableHeader } from "@/components/sortable-header";
import { StatusBadge, TableShell, Td, TextLink, Th } from "@/components/ui";
import { formatDateTime } from "@/lib/datetime";
import type { RawSearchParams } from "@/lib/search-params";

export function MatchesTable({
  rows,
  sortable,
}: {
  rows: MatchResponse[];
  sortable?: { pathname: string; searchParams: RawSearchParams };
}) {
  return (
    <TableShell>
      <thead>
        <tr>
          {sortable ? <SortableHeader label="Fecha" field="scheduledAt" {...sortable} /> : <Th>Fecha</Th>}
          <Th align="right">Local</Th>
          <Th align="center">Marcador</Th>
          <Th>Visitante</Th>
          {sortable ? <SortableHeader label="Estado" field="status" {...sortable} /> : <Th>Estado</Th>}
          <Th />
        </tr>
      </thead>
      <tbody className="divide-y divide-slate-100">
        {rows.map((match) => (
          <tr key={match.id} className="hover:bg-slate-50">
            <Td>{formatDateTime(match.scheduledAt)}</Td>
            <Td align="right">{match.homeTeamName}</Td>
            <Td align="center" strong>
              {match.status === "finished" ? `${match.homeGoals} - ${match.awayGoals}` : "vs"}
            </Td>
            <Td>{match.awayTeamName}</Td>
            <Td>
              <StatusBadge status={match.status} />
            </Td>
            <Td align="right">
              <TextLink href={`/matches/${match.id}`}>{match.status === "scheduled" ? "Registrar resultado" : "Ver detalle"}</TextLink>
            </Td>
          </tr>
        ))}
      </tbody>
    </TableShell>
  );
}
