import Link from "next/link";
import type { PagedResult } from "@/core/api-types";
import { hrefWith, type RawSearchParams } from "@/lib/search-params";

/**
 * Paginación basada en la URL: conserva filtros y orden actuales. Los datos vienen paginados desde la API.
 */
export function Pagination<T>({ page, pathname, searchParams }: { page: PagedResult<T>; pathname: string; searchParams: RawSearchParams }) {
  const { pageNumber, totalPages, totalRecords, pageSize } = page;
  const first = totalRecords === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const last = Math.min(pageNumber * pageSize, totalRecords);

  return (
    <nav aria-label="Paginación" className="mt-4 flex flex-wrap items-center justify-between gap-3 text-sm text-slate-600">
      <p>
        {totalRecords === 0 ? "Sin registros" : `Mostrando ${first}–${last} de ${totalRecords} registros`}
        {totalPages > 0 ? ` · Página ${pageNumber} de ${totalPages}` : null}
      </p>
      <div className="flex gap-2">
        <PageLink disabled={pageNumber <= 1} href={hrefWith(pathname, searchParams, { pageNumber: pageNumber - 1 })}>
          ← Anterior
        </PageLink>
        <PageLink disabled={pageNumber >= totalPages} href={hrefWith(pathname, searchParams, { pageNumber: pageNumber + 1 })}>
          Siguiente →
        </PageLink>
      </div>
    </nav>
  );
}

function PageLink({ href, disabled, children }: { href: string; disabled: boolean; children: React.ReactNode }) {
  if (disabled) {
    return <span className="cursor-not-allowed rounded-lg border border-slate-200 px-3 py-1.5 text-slate-400">{children}</span>;
  }

  return (
    <Link href={href} className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 font-medium text-slate-700 hover:bg-slate-50">
      {children}
    </Link>
  );
}
