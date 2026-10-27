import Link from "next/link";
import { hrefWith, type RawSearchParams } from "@/lib/search-params";

/**
 * Encabezado de columna con ordenamiento dinámico (sortBy / sortDirection en la URL).
 * Al cambiar el orden se vuelve a la primera página.
 */
export function SortableHeader({
  label,
  field,
  pathname,
  searchParams,
  align = "left",
}: {
  label: string;
  field: string;
  pathname: string;
  searchParams: RawSearchParams;
  align?: "left" | "right" | "center";
}) {
  const currentField = typeof searchParams.sortBy === "string" ? searchParams.sortBy : undefined;
  const currentDirection = searchParams.sortDirection === "desc" ? "desc" : "asc";
  const isActive = currentField === field;
  const nextDirection = isActive && currentDirection === "asc" ? "desc" : "asc";
  const alignment = align === "right" ? "justify-end" : align === "center" ? "justify-center" : "justify-start";

  return (
    <th
      scope="col"
      aria-sort={isActive ? (currentDirection === "asc" ? "ascending" : "descending") : "none"}
      className="whitespace-nowrap bg-slate-50 px-3 py-2 text-xs font-semibold uppercase tracking-wide text-slate-600"
    >
      <Link
        href={hrefWith(pathname, searchParams, { sortBy: field, sortDirection: nextDirection, pageNumber: undefined })}
        className={`flex items-center gap-1 hover:text-slate-900 ${alignment}`}
      >
        {label}
        <span aria-hidden className={isActive ? "text-emerald-700" : "text-slate-300"}>
          {isActive ? (currentDirection === "asc" ? "▲" : "▼") : "↕"}
        </span>
      </Link>
    </th>
  );
}
