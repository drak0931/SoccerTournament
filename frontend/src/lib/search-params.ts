import type { SortDirection } from "@/core/api-types";

// Lectura segura de parámetros de URL (filtros, paginación y orden).

export type RawSearchParams = Record<string, string | string[] | undefined>;

export const PAGE_SIZES = [5, 10, 20, 50] as const;
export const DEFAULT_PAGE_SIZE = 10;

export function getString(params: RawSearchParams, key: string): string | undefined {
  const value = params[key];
  const text = (Array.isArray(value) ? value[0] : value)?.trim();
  return text ? text : undefined;
}

export function getPositiveInt(params: RawSearchParams, key: string, fallback: number): number {
  const parsed = Number.parseInt(getString(params, key) ?? "", 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

/** Valor de una lista permitida; cualquier otro se ignora. */
export function getOneOf<T extends string>(params: RawSearchParams, key: string, allowed: readonly T[]): T | undefined {
  const value = getString(params, key);
  return allowed.find((item) => item === value);
}

export interface Paging {
  pageNumber: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: SortDirection;
}

export function getPaging(params: RawSearchParams, sortableFields: readonly string[]): Paging {
  const pageSize = getPositiveInt(params, "pageSize", DEFAULT_PAGE_SIZE);

  return {
    pageNumber: getPositiveInt(params, "pageNumber", 1),
    pageSize: (PAGE_SIZES as readonly number[]).includes(pageSize) ? pageSize : DEFAULT_PAGE_SIZE,
    sortBy: getOneOf(params, "sortBy", sortableFields),
    sortDirection: getOneOf(params, "sortDirection", ["asc", "desc"] as const),
  };
}

/** Construye un href conservando los parámetros actuales y aplicando cambios (undefined elimina). */
export function hrefWith(pathname: string, current: RawSearchParams, changes: Record<string, string | number | undefined>): string {
  const params = new URLSearchParams();

  for (const [key, value] of Object.entries(current)) {
    const first = Array.isArray(value) ? value[0] : value;
    if (first) params.set(key, first);
  }

  for (const [key, value] of Object.entries(changes)) {
    if (value === undefined || value === "") params.delete(key);
    else params.set(key, String(value));
  }

  const query = params.toString();
  return query ? `${pathname}?${query}` : pathname;
}
