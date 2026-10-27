import { describe, expect, it } from "vitest";
import { endOfLocalDayUtc, formatDate, formatDateTime, formatYear, startOfLocalDayUtc } from "./datetime";

describe("datetime (America/El_Salvador, UTC-6)", () => {
  it("formatea fechas UTC como dd/MM/yyyy en hora local", () => {
    // 02:00 UTC del 11/10 son las 20:00 del 10/10 en El Salvador
    expect(formatDate("2026-10-11T02:00:00Z")).toBe("10/10/2026");
  });

  it("formatea fecha y hora en formato de 24 horas", () => {
    expect(formatDateTime("2026-10-11T01:00:00Z")).toBe("10/10/2026 19:00");
  });

  it("muestra un guion cuando no hay fecha", () => {
    expect(formatDate(null)).toBe("—");
    expect(formatDateTime(undefined)).toBe("—");
    expect(formatYear(null)).toBe("—");
  });

  it("extrae el año de la fecha de fundación", () => {
    expect(formatYear("1925-03-15T00:00:00Z")).toBe("1925");
  });

  it("convierte un día local al rango UTC equivalente", () => {
    expect(startOfLocalDayUtc("2026-10-26")).toBe("2026-10-26T06:00:00.000Z");
    expect(endOfLocalDayUtc("2026-10-26")).toBe("2026-10-27T05:59:59.000Z");
  });

  it("ignora valores que no son yyyy-MM-dd", () => {
    expect(startOfLocalDayUtc("26/10/2026")).toBeUndefined();
    expect(endOfLocalDayUtc("")).toBeUndefined();
  });
});
