import type { Metadata } from "next";
import { ErrorPanel } from "@/components/error-panel";
import { Field, FilterForm, inputClass } from "@/components/filter-form";
import { Pagination } from "@/components/pagination";
import { EmptyState, PageHeader } from "@/components/ui";
import { load } from "@/core/api-error";
import { StandingsTable } from "@/features/standings/standings-table";
import { getPaging, getString, type RawSearchParams } from "@/lib/search-params";
import { getStandings, STANDING_SORT_FIELDS } from "@/services/tournament-service";

export const metadata: Metadata = { title: "Tabla de posiciones" };

const PATHNAME = "/standings";

export default async function StandingsPage({ searchParams }: { searchParams: Promise<RawSearchParams> }) {
  const params = await searchParams;
  const paging = getPaging(params, STANDING_SORT_FIELDS);
  const teamName = getString(params, "teamName");

  const result = await load(() => getStandings({ ...paging, teamName }));

  return (
    <div>
      <PageHeader
        title="Tabla de posiciones"
        description="Orden oficial: 1) puntos, 2) diferencia de gol, 3) goles a favor. Victoria 3 pts, empate 1 pt."
      />

      <FilterForm pathname={PATHNAME} paging={paging}>
        <Field label="Equipo">
          <input name="teamName" defaultValue={teamName} placeholder="Nombre del equipo" className={inputClass} />
        </Field>
      </FilterForm>

      {!result.ok ? (
        <ErrorPanel error={result.error} />
      ) : result.data.data.length === 0 ? (
        <EmptyState message="No hay equipos que coincidan con el filtro." />
      ) : (
        <>
          <StandingsTable rows={result.data.data} sortable={{ pathname: PATHNAME, searchParams: params }} />
          <Pagination page={result.data} pathname={PATHNAME} searchParams={params} />
        </>
      )}
    </div>
  );
}
