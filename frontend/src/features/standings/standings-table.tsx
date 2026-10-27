import type { StandingResponse } from "@/core/api-types";
import { SortableHeader } from "@/components/sortable-header";
import { TableShell, Td, TextLink, Th } from "@/components/ui";
import { signed } from "@/lib/labels";
import type { RawSearchParams } from "@/lib/search-params";

/**
 * Tabla de posiciones. Con "sortable" los encabezados permiten ordenamiento dinámico.
 */
export function StandingsTable({
  rows,
  sortable,
}: {
  rows: StandingResponse[];
  sortable?: { pathname: string; searchParams: RawSearchParams };
}) {
  const header = (label: string, field: string) =>
    sortable ? <SortableHeader label={label} field={field} align="center" {...sortable} /> : <Th align="center">{label}</Th>;

  return (
    <TableShell>
      <thead>
        <tr>
          {header("#", "position")}
          {sortable ? <SortableHeader label="Equipo" field="teamName" {...sortable} /> : <Th>Equipo</Th>}
          <Th align="center">PJ</Th>
          <Th align="center">G</Th>
          <Th align="center">E</Th>
          <Th align="center">P</Th>
          {header("GF", "goalsFor")}
          <Th align="center">GC</Th>
          {header("DG", "goalDifference")}
          {header("Pts", "points")}
        </tr>
      </thead>
      <tbody className="divide-y divide-slate-100">
        {rows.map((row) => (
          <tr key={row.teamId} className="hover:bg-slate-50">
            <Td align="center" strong>
              {row.position}
            </Td>
            <Td>
              <TextLink href={`/teams/${row.teamId}`}>{row.teamName}</TextLink>
            </Td>
            <Td align="center">{row.played}</Td>
            <Td align="center">{row.won}</Td>
            <Td align="center">{row.drawn}</Td>
            <Td align="center">{row.lost}</Td>
            <Td align="center">{row.goalsFor}</Td>
            <Td align="center">{row.goalsAgainst}</Td>
            <Td align="center">{signed(row.goalDifference)}</Td>
            <Td align="center" strong>
              {row.points}
            </Td>
          </tr>
        ))}
      </tbody>
    </TableShell>
  );
}
