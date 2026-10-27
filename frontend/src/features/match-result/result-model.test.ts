import { describe, expect, it } from "vitest";
import { newIdempotencyKey, toRequest, validateResult, type ResultDraft } from "./result-model";

const HOME = "home-team";
const AWAY = "away-team";

function draft(overrides: Partial<ResultDraft> = {}): ResultDraft {
  return { homeTeamId: HOME, awayTeamId: AWAY, homeGoals: "0", awayGoals: "0", goals: [], ...overrides };
}

describe("result-model", () => {
  it("acepta un 0-0 sin goleadores", () => {
    expect(validateResult(draft())).toEqual([]);
  });

  it("acepta un resultado cuyos goles cuadran con el marcador", () => {
    const result = draft({
      homeGoals: "2",
      awayGoals: "1",
      goals: [
        { key: "1", teamId: HOME, playerId: "p1", minute: "10" },
        { key: "2", teamId: AWAY, playerId: "p2", minute: "50" },
        { key: "3", teamId: HOME, playerId: "p1", minute: "88" },
      ],
    });

    expect(validateResult(result)).toEqual([]);
    expect(toRequest(result)).toEqual({
      homeGoals: 2,
      awayGoals: 1,
      goals: [
        { playerId: "p1", teamId: HOME, minute: 10 },
        { playerId: "p2", teamId: AWAY, minute: 50 },
        { playerId: "p1", teamId: HOME, minute: 88 },
      ],
    });
  });

  it("detecta goles que no coinciden con el marcador", () => {
    const errors = validateResult(draft({ homeGoals: "2", goals: [{ key: "1", teamId: HOME, playerId: "p1", minute: "10" }] }));

    expect(errors).toContain("Registró 1 gol(es) del local, pero el marcador indica 2.");
  });

  it("exige goleador y minuto válido en cada gol", () => {
    const errors = validateResult(draft({ homeGoals: "1", goals: [{ key: "1", teamId: HOME, playerId: "", minute: "131" }] }));

    expect(errors).toContain("Gol 1: seleccione el goleador.");
    expect(errors).toContain("Gol 1: el minuto debe estar entre 1 y 130.");
  });

  it("rechaza marcadores vacíos, negativos o con decimales", () => {
    expect(validateResult(draft({ homeGoals: "" }))).toContain("El marcador local debe ser un número entre 0 y 50.");
    expect(validateResult(draft({ awayGoals: "-1" }))).toContain("El marcador visitante debe ser un número entre 0 y 50.");
    expect(validateResult(draft({ homeGoals: "1.5" }))).toContain("El marcador local debe ser un número entre 0 y 50.");
  });

  it("genera claves de idempotencia únicas con formato UUID v4", () => {
    const first = newIdempotencyKey();
    const second = newIdempotencyKey();

    expect(first).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/);
    expect(first).not.toBe(second);
  });
});
