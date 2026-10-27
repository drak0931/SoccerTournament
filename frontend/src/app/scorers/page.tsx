import type { Metadata } from "next";
import { ErrorPanel } from "@/components/error-panel";
import { Field, FilterForm, inputClass } from "@/components/filter-form";
import { Pagination } from "@/components/pagination";
import { EmptyState, PageHeader } from "@/components/ui";
import { load } from "@/core/api-error";
import { ScorersTable } from "@/features/scorers/scorers-table";
import { getPaging, getString, type RawSearchParams } from "@/lib/search-params";
import { getTeamOptions } from "@/services/teams-service";
import { getTopScorers, SCORER_SORT_FIELDS } from "@/services/tournament-service";

export const metadata: Metadata = { title: "Goleadores" };

const PATHNAME = "/scorers";

export default async function ScorersPage({ searchParams }: { searchParams: Promise<RawSearchParams> }) {
  const params = await searchParams;
  const paging = getPaging(params, SCORER_SORT_FIELDS);
  const teamId = getString(params, "teamId");

  const [result, teams] = await Promise.all([
    load(() => getTopScorers({ ...paging, teamId })),
    load(() => getTeamOptions()),
  ]);

  return (
    <div>
      <PageHeader title="Goleadores" description="Jugadores con la misma cantidad de goles comparten puesto." />

      <FilterForm pathname={PATHNAME} paging={paging}>
        <Field label="Equipo">
          <select name="teamId" defaultValue={teamId ?? ""} className={inputClass}>
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
      </FilterForm>

      {!result.ok ? (
        <ErrorPanel error={result.error} />
      ) : result.data.data.length === 0 ? (
        <EmptyState message="No hay goles registrados para este filtro." />
      ) : (
        <>
          <ScorersTable rows={result.data.data} sortable={{ pathname: PATHNAME, searchParams: params }} />
          <Pagination page={result.data} pathname={PATHNAME} searchParams={params} />
        </>
      )}
    </div>
  );
}
