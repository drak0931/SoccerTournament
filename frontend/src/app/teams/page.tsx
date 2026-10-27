import type { Metadata } from "next";
import { ErrorPanel } from "@/components/error-panel";
import { Field, FilterForm, inputClass } from "@/components/filter-form";
import { Pagination } from "@/components/pagination";
import { SortableHeader } from "@/components/sortable-header";
import { EmptyState, PageHeader, TableShell, Td, TextLink, Th } from "@/components/ui";
import { load } from "@/core/api-error";
import { formatYear } from "@/lib/datetime";
import { getPaging, getPositiveInt, getString, type RawSearchParams } from "@/lib/search-params";
import { getTeams, TEAM_SORT_FIELDS } from "@/services/teams-service";

export const metadata: Metadata = { title: "Equipos" };

const PATHNAME = "/teams";

export default async function TeamsPage({ searchParams }: { searchParams: Promise<RawSearchParams> }) {
  const params = await searchParams;
  const paging = getPaging(params, TEAM_SORT_FIELDS);
  const name = getString(params, "name");
  const foundedFrom = getPositiveInt(params, "foundedFrom", 0) || undefined;
  const foundedTo = getPositiveInt(params, "foundedTo", 0) || undefined;

  const result = await load(() => getTeams({ ...paging, name, foundedFrom, foundedTo }));
  const sortable = { pathname: PATHNAME, searchParams: params };

  return (
    <div>
      <PageHeader title="Equipos" description="Equipos participantes del torneo." />

      <FilterForm pathname={PATHNAME} paging={paging}>
        <Field label="Nombre">
          <input name="name" defaultValue={name} placeholder="Nombre o abreviatura" className={inputClass} />
        </Field>
        <Field label="Fundado desde (año)">
          <input name="foundedFrom" type="number" min={1850} max={2100} defaultValue={foundedFrom} className={inputClass} />
        </Field>
        <Field label="Fundado hasta (año)">
          <input name="foundedTo" type="number" min={1850} max={2100} defaultValue={foundedTo} className={inputClass} />
        </Field>
      </FilterForm>

      {!result.ok ? (
        <ErrorPanel error={result.error} />
      ) : result.data.data.length === 0 ? (
        <EmptyState message="No hay equipos que coincidan con los filtros." />
      ) : (
        <>
          <TableShell>
            <thead>
              <tr>
                <SortableHeader label="Equipo" field="fullName" {...sortable} />
                <SortableHeader label="Abreviatura" field="shortName" {...sortable} />
                <SortableHeader label="Fundación" field="foundedYear" {...sortable} />
                <Th />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {result.data.data.map((team) => (
                <tr key={team.id} className="hover:bg-slate-50">
                  <Td strong>{team.fullName}</Td>
                  <Td>{team.shortName}</Td>
                  <Td>{formatYear(team.foundedYear)}</Td>
                  <Td align="right">
                    <TextLink href={`/teams/${team.id}`}>Ver integrantes</TextLink>
                  </Td>
                </tr>
              ))}
            </tbody>
          </TableShell>
          <Pagination page={result.data} pathname={PATHNAME} searchParams={params} />
        </>
      )}
    </div>
  );
}
