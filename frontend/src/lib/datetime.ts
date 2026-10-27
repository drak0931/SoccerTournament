// Fechas: la API trabaja en UTC; la interfaz muestra y filtra en hora de El Salvador.
// El Salvador usa UTC-06:00 todo el año (sin horario de verano), por eso el desplazamiento es fijo.

export const TIME_ZONE = "America/El_Salvador";
const UTC_OFFSET = "-06:00";

const dateFormatter = new Intl.DateTimeFormat("es-SV", {
  timeZone: TIME_ZONE,
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
});

const timeFormatter = new Intl.DateTimeFormat("es-SV", {
  timeZone: TIME_ZONE,
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
});

/** dd/MM/yyyy en hora de El Salvador. */
export function formatDate(isoUtc: string | null | undefined): string {
  if (!isoUtc) return "—";
  return dateFormatter.format(new Date(isoUtc));
}

/** dd/MM/yyyy HH:mm en hora de El Salvador. */
export function formatDateTime(isoUtc: string | null | undefined): string {
  if (!isoUtc) return "—";
  const date = new Date(isoUtc);
  return `${dateFormatter.format(date)} ${timeFormatter.format(date)}`;
}

/** Solo el año (para fechas de fundación). */
export function formatYear(isoUtc: string | null | undefined): string {
  if (!isoUtc) return "—";
  return isoUtc.slice(0, 4);
}

const DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/;

/**
 * Convierte un día local (yyyy-MM-dd del input type=date) al instante UTC en que empieza ese día en El Salvador.
 * Ej.: 2026-10-26 → 2026-10-26T06:00:00.000Z
 */
export function startOfLocalDayUtc(localDate: string | undefined): string | undefined {
  if (!localDate || !DATE_ONLY.test(localDate)) return undefined;
  return new Date(`${localDate}T00:00:00${UTC_OFFSET}`).toISOString();
}

/**
 * Último instante (con segundos) del día local, en UTC. Se envía con hora para que la API lo trate como inclusivo.
 * Ej.: 2026-10-26 → 2026-10-27T05:59:59.000Z
 */
export function endOfLocalDayUtc(localDate: string | undefined): string | undefined {
  if (!localDate || !DATE_ONLY.test(localDate)) return undefined;
  return new Date(`${localDate}T23:59:59${UTC_OFFSET}`).toISOString();
}
