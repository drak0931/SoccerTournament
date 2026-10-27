import type { TopScorerResponse } from "@/core/api-types";
import { SortableHeader } from "@/components/sortable-header";
import { TableShell, Td, TextLink, Th } from "@/components/ui";
import type { RawSearchParams } from "@/lib/search-params";

export function ScorersTable({
  rows,
  sortable,
}: {
  rows: TopScorerResponse[];
  sortable?: { pathname: string; searchParams: RawSearchParams };
}) {
  return (
    <TableShell>
      <thead>
        <tr>
          <Th align="center">#</Th>
          {sortable ? <SortableHeader label="Jugador" field="playerName" {...sortable} /> : <Th>Jugador</Th>}
          {sortable ? <SortableHeader label="Equipo" field="teamName" {...sortable} /> : <Th>Equipo</Th>}
          {sortable ? <SortableHeader label="Goles" field="goals" align="center" {...sortable} /> : <Th align="center">Goles</Th>}
        </tr>
      </thead>
      <tbody className="divide-y divide-slate-100">
        {rows.map((scorer) => (
          <tr key={`${scorer.playerId}-${scorer.teamId}`} className="hover:bg-slate-50">
            <Td align="center" strong>
              {scorer.rank}
            </Td>
            <Td>{scorer.playerName}</Td>
            <Td>
              <TextLink href={`/teams/${scorer.teamId}`}>{scorer.teamName}</TextLink>
            </Td>
            <Td align="center" strong>
              {scorer.goals}
            </Td>
          </tr>
        ))}
      </tbody>
    </TableShell>
  );
}
