"use client";

import { useActionState, useEffect, useMemo, useState, type FormEvent } from "react";
import type { MatchResponse, PlayerResponse } from "@/core/api-types";
import { Field, inputClass } from "@/components/filter-form";
import { Alert } from "@/components/ui";
import { playerFullName } from "@/lib/labels";
import { registerResultAction, type RegisterResultState } from "./actions";
import { MAX_GOALS, MAX_MINUTE, MIN_MINUTE, newIdempotencyKey, toRequest, validateResult, type GoalRow, type ResultDraft } from "./result-model";

const INITIAL_STATE: RegisterResultState = { status: "idle" };

/**
 * Formulario de registro de resultado y goleadores.
 * La Idempotency-Key se conserva entre reintentos del mismo envío (doble clic, red lenta) para no duplicar;
 * solo se renueva cuando la API rechazó definitivamente el envío y el usuario debe corregir datos.
 */
export function RegisterResultForm({
  match,
  homePlayers,
  awayPlayers,
  initialIdempotencyKey,
}: {
  match: MatchResponse;
  homePlayers: PlayerResponse[];
  awayPlayers: PlayerResponse[];
  initialIdempotencyKey: string;
}) {
  const [state, formAction, isPending] = useActionState(registerResultAction, INITIAL_STATE);
  const [idempotencyKey, setIdempotencyKey] = useState(initialIdempotencyKey);
  const [clientErrors, setClientErrors] = useState<string[]>([]);
  const [draft, setDraft] = useState<ResultDraft>({
    homeTeamId: match.homeTeamId,
    awayTeamId: match.awayTeamId,
    homeGoals: "0",
    awayGoals: "0",
    goals: [],
  });

  useEffect(() => {
    // Rechazo definitivo (400/404/409): el usuario corregirá datos → nueva clave para el siguiente envío.
    // Si la API no respondió o el envío sigue en proceso, se mantiene la clave para que el reintento no duplique.
    const definitiveRejection =
      state.status === "error" &&
      [400, 404, 409].includes(state.httpStatus) &&
      state.errorCode !== "Idempotency.RequestInProgress";

    if (definitiveRejection) setIdempotencyKey(newIdempotencyKey());
  }, [state]);

  const playersByTeam = useMemo(
    () => ({ [match.homeTeamId]: homePlayers, [match.awayTeamId]: awayPlayers }),
    [match.homeTeamId, match.awayTeamId, homePlayers, awayPlayers],
  );

  const updateGoal = (key: string, changes: Partial<GoalRow>) =>
    setDraft((current) => ({
      ...current,
      goals: current.goals.map((goal) => (goal.key === key ? { ...goal, ...changes } : goal)),
    }));

  const addGoal = (teamId: string) =>
    setDraft((current) => ({
      ...current,
      goals: [...current.goals, { key: newIdempotencyKey(), teamId, playerId: "", minute: "" }],
    }));

  const removeGoal = (key: string) =>
    setDraft((current) => ({ ...current, goals: current.goals.filter((goal) => goal.key !== key) }));

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    const errors = validateResult(draft);
    setClientErrors(errors);
    if (errors.length > 0) event.preventDefault();
  };

  return (
    <form action={formAction} onSubmit={onSubmit} className="space-y-5" noValidate>
      <input type="hidden" name="matchId" value={match.id} />
      <input type="hidden" name="idempotencyKey" value={idempotencyKey} />
      <input type="hidden" name="payload" value={JSON.stringify(toRequest(draft))} />

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={`Goles de ${match.homeTeamName} (local)`}>
          <input
            type="number"
            min={0}
            max={MAX_GOALS}
            required
            value={draft.homeGoals}
            onChange={(event) => setDraft({ ...draft, homeGoals: event.target.value })}
            className={inputClass}
          />
        </Field>
        <Field label={`Goles de ${match.awayTeamName} (visitante)`}>
          <input
            type="number"
            min={0}
            max={MAX_GOALS}
            required
            value={draft.awayGoals}
            onChange={(event) => setDraft({ ...draft, awayGoals: event.target.value })}
            className={inputClass}
          />
        </Field>
      </div>

      <fieldset className="space-y-3">
        <legend className="text-sm font-semibold text-slate-800">Goleadores</legend>

        {draft.goals.length === 0 ? (
          <p className="text-sm text-slate-500">Sin goles registrados. Para un 0-0 no agregue goleadores.</p>
        ) : null}

        {draft.goals.map((goal, index) => (
          <div key={goal.key} className="grid items-end gap-3 rounded-lg border border-slate-200 p-3 sm:grid-cols-[1fr_2fr_7rem_auto]">
            <Field label={`Gol ${index + 1}: equipo`}>
              <select
                value={goal.teamId}
                onChange={(event) => updateGoal(goal.key, { teamId: event.target.value, playerId: "" })}
                className={inputClass}
              >
                <option value={match.homeTeamId}>{match.homeTeamName}</option>
                <option value={match.awayTeamId}>{match.awayTeamName}</option>
              </select>
            </Field>
            <Field label="Jugador">
              <select value={goal.playerId} onChange={(event) => updateGoal(goal.key, { playerId: event.target.value })} className={inputClass}>
                <option value="">Seleccione…</option>
                {(playersByTeam[goal.teamId] ?? []).map((player) => (
                  <option key={player.id} value={player.id}>
                    #{player.jerseyNumber} {playerFullName(player)}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Minuto">
              <input
                type="number"
                min={MIN_MINUTE}
                max={MAX_MINUTE}
                value={goal.minute}
                onChange={(event) => updateGoal(goal.key, { minute: event.target.value })}
                className={inputClass}
              />
            </Field>
            <button
              type="button"
              onClick={() => removeGoal(goal.key)}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-700 hover:bg-slate-50"
              aria-label={`Quitar gol ${index + 1}`}
            >
              Quitar
            </button>
          </div>
        ))}

        <div className="flex flex-wrap gap-2">
          <button type="button" onClick={() => addGoal(match.homeTeamId)} className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm hover:bg-slate-50">
            + Gol de {match.homeTeamName}
          </button>
          <button type="button" onClick={() => addGoal(match.awayTeamId)} className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm hover:bg-slate-50">
            + Gol de {match.awayTeamName}
          </button>
        </div>
      </fieldset>

      {clientErrors.length > 0 ? (
        <Alert tone="error" title="Revise el resultado">
          <ul className="list-inside list-disc">
            {clientErrors.map((error) => (
              <li key={error}>{error}</li>
            ))}
          </ul>
        </Alert>
      ) : null}

      {state.status === "error" && clientErrors.length === 0 ? (
        <Alert tone="error" title={state.title}>
          <p>{state.message}</p>
          {state.fieldErrors.length > 0 ? (
            <ul className="mt-1 list-inside list-disc">
              {state.fieldErrors.map((error) => (
                <li key={error}>{error}</li>
              ))}
            </ul>
          ) : null}
          {state.traceId ? (
            <p className="mt-2 text-xs opacity-75">
              Código de seguimiento: <code className="font-mono">{state.traceId}</code>
            </p>
          ) : null}
        </Alert>
      ) : null}

      <button
        type="submit"
        disabled={isPending}
        className="rounded-lg bg-emerald-700 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-800 disabled:cursor-wait disabled:opacity-60"
      >
        {isPending ? "Registrando…" : "Registrar resultado"}
      </button>
    </form>
  );
}
