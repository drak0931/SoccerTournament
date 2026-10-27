import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ErrorPanel } from "@/components/error-panel";
import { Field, FilterForm, inputClass } from "@/components/filter-form";
import { Pagination } from "@/components/pagination";
import { SortableHeader } from "@/components/sortable-header";
import { ButtonLink, EmptyState, PageHeader, TableShell, Td } from "@/components/ui";
import { load } from "@/core/api-error";
import { formatDate } from "@/lib/datetime";
import { PLAYER_POSITION_LABELS, PLAYER_POSITIONS } from "@/lib/labels";
import { getOneOf, getPaging, getString, type RawSearchParams } from "@/lib/search-params";
import { getTeam, getTeamPlayers, PLAYER_SORT_FIELDS } from "@/services/teams-service";

export const metadata: Metadata = { title: "Integrantes del equipo" };

export default async function TeamDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ teamId: string }>;
  searchParams: Promise<RawSearchParams>;
}) {
  const { teamId } = await params;
  const query = await searchParams;
  const pathname = `/teams/${teamId}`;
  const paging = getPaging(query, PLAYER_SORT_FIELDS);
  const name = getString(query, "name");
  const position = getOneOf(query, "position", PLAYER_POSITIONS);

  const [team, players] = await Promise.all([
    load(() => getTeam(teamId)),
    load(() => getTeamPlayers(teamId, { ...paging, name, position })),
  ]);

  if (!team.ok && team.error.isNotFound) notFound();
  if (!team.ok) return <ErrorPanel error={team.error} context="Equipo" />;

  const sortable = { pathname, searchParams: query };

  return (
    <div>
      <PageHeader
        title={team.data.fullName}
        description={`${team.data.shortName} · Fundado el ${formatDate(team.data.foundedYear)}`}
        actions={
          <div className="flex gap-2">
            <ButtonLink href={`/matches?teamId=${team.data.id}`} variant="secondary">
              Partidos
            </ButtonLink>
            <ButtonLink href={`/scorers?teamId=${team.data.id}`} variant="secondary">
              Goleadores
            </ButtonLink>
          </div>
        }
      />

      <h2 className="mb-3 text-lg font-semibold text-slate-800">Integrantes</h2>

      <FilterForm pathname={pathname} paging={paging}>
        <Field label="Nombre">
          <input name="name" defaultValue={name} placeholder="Nombre o apellido" className={inputClass} />
        </Field>
        <Field label="Posición">
          <select name="position" defaultValue={position ?? ""} className={inputClass}>
            <option value="">Todas</option>
            {PLAYER_POSITIONS.map((item) => (
              <option key={item} value={item}>
                {PLAYER_POSITION_LABELS[item]}
              </option>
            ))}
          </select>
        </Field>
      </FilterForm>

      {!players.ok ? (
        <ErrorPanel error={players.error} context="Integrantes" />
      ) : players.data.data.length === 0 ? (
        <EmptyState message="No hay integrantes que coincidan con los filtros." />
      ) : (
        <>
          <TableShell>
            <thead>
              <tr>
                <SortableHeader label="Dorsal" field="jerseyNumber" align="center" {...sortable} />
                <SortableHeader label="Apellido" field="lastName" {...sortable} />
                <SortableHeader label="Nombre" field="firstName" {...sortable} />
                <SortableHeader label="Posición" field="position" {...sortable} />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {players.data.data.map((player) => (
                <tr key={player.id} className="hover:bg-slate-50">
                  <Td align="center" strong>
                    {player.jerseyNumber}
                  </Td>
                  <Td strong>{player.lastName}</Td>
                  <Td>{player.firstName ?? "—"}</Td>
                  <Td>{PLAYER_POSITION_LABELS[player.position]}</Td>
                </tr>
              ))}
            </tbody>
          </TableShell>
          <Pagination page={players.data} pathname={pathname} searchParams={query} />
        </>
      )}
    </div>
  );
}
