import Link from "next/link";
import type { ReactNode } from "react";
import { PAGE_SIZES, type Paging } from "@/lib/search-params";

/**
 * Formulario de filtros por GET: el resultado queda en la URL (compartible) y funciona sin JavaScript.
 * Conserva el orden actual y reinicia la paginación a la página 1.
 */
export function FilterForm({ pathname, paging, children }: { pathname: string; paging: Paging; children: ReactNode }) {
  return (
    <form method="get" action={pathname} className="mb-4 flex flex-wrap items-end gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      {children}

      <Field label="Por página">
        <select name="pageSize" defaultValue={String(paging.pageSize)} className={inputClass}>
          {PAGE_SIZES.map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>
      </Field>

      {paging.sortBy ? <input type="hidden" name="sortBy" value={paging.sortBy} /> : null}
      {paging.sortDirection ? <input type="hidden" name="sortDirection" value={paging.sortDirection} /> : null}

      <div className="flex gap-2">
        <button type="submit" className="rounded-lg bg-emerald-700 px-3 py-2 text-sm font-medium text-white hover:bg-emerald-800">
          Filtrar
        </button>
        <Link href={pathname} className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">
          Limpiar
        </Link>
      </div>
    </form>
  );
}

export const inputClass =
  "block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-emerald-600 focus:outline-none focus:ring-1 focus:ring-emerald-600";

export function Field({ label, children, htmlFor }: { label: string; children: ReactNode; htmlFor?: string }) {
  return (
    <label htmlFor={htmlFor} className="flex min-w-36 flex-col gap-1 text-xs font-medium text-slate-600">
      {label}
      {children}
    </label>
  );
}
