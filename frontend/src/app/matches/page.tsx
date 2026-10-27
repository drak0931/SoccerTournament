import type { Metadata } from "next";
import { ErrorPanel } from "@/components/error-panel";
import { Field, FilterForm, inputClass } from "@/components/filter-form";
import { Pagination } from "@/components/pagination";
import { EmptyState, PageHeader } from "@/components/ui";
import { load } from "@/core/api-error";
import { MatchesTable } from "@/features/matches/matches-table";
import { MATCH_STATUS_LABELS, MATCH_STATUSES } from "@/lib/labels";
import { getOneOf, getPaging, getString, type RawSearchParams } from "@/lib/search-params";
import { getMatches, MATCH_SORT_FIELDS } from "@/services/matches-service";
import { getTeamOptions } from "@/services/teams-service";

export const metadata: Metadata = { title: "Partidos" };

const PATHNAME = "/matches";

export default async function MatchesPage({ searchParams }: { searchParams: Promise<RawSearchParams> }) {
  const params = await searchParams;
  const paging = getPaging(params, MATCH_SORT_FIELDS);
  const filters = {
    dateFrom: getString(params, "dateFrom"),
    dateTo: getString(params, "dateTo"),
    teamId: getString(params, "teamId"),
    status: getOneOf(params, "status", MATCH_STATUSES),
  };

  const [result, teams] = await Promise.all([
    load(() => getMatches({ ...paging, ...filters })),
    load(() => getTeamOptions()),
  ]);

  return (
    <div>
      <PageHeader title="Partidos" description="Calendario y resultados. Seleccione un partido programado para registrar su resultado." />

      <FilterForm pathname={PATHNAME} paging={paging}>
        <Field label="Desde">
          <input type="date" name="dateFrom" defaultValue={filters.dateFrom} className={inputClass} />
        </Field>
        <Field label="Hasta">
          <input type="date" name="dateTo" defaultValue={filters.dateTo} className={inputClass} />
        </Field>
        <Field label="Equipo">
          <select name="teamId" defaultValue={filters.teamId ?? ""} className={inputClass}>
            <option value="">Todos</option>
            {teams.ok
              ? teams.data.map((team) => (
                  <option key={team.id} value={team.id}>
                    {team.fullName}
                  </option>
                ))
              : null}
          </select>
        </Field>
        <Field label="Estado">
          <select name="status" defaultValue={filters.status ?? ""} className={inputClass}>
            <option value="">Todos</option>
            {MATCH_STATUSES.map((status) => (
              <option key={status} value={status}>
                {MATCH_STATUS_LABELS[status]}
              </option>
            ))}
          </select>
        </Field>
      </FilterForm>

      {!result.ok ? (
        <ErrorPanel error={result.error} />
      ) : result.data.data.length === 0 ? (
        <EmptyState message="No hay partidos que coincidan con los filtros." />
      ) : (
        <>
          <MatchesTable rows={result.data.data} sortable={{ pathname: PATHNAME, searchParams: params }} />
          <Pagination page={result.data} pathname={PATHNAME} searchParams={params} />
        </>
      )}
    </div>
  );
}
