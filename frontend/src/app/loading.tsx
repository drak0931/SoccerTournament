export default function Loading() {
  return (
    <div role="status" aria-live="polite" className="animate-pulse space-y-3">
      <div className="h-7 w-48 rounded bg-slate-200" />
      <div className="h-4 w-80 rounded bg-slate-200" />
      <div className="h-64 rounded-xl bg-slate-200" />
      <span className="sr-only">Cargando…</span>
    </div>
  );
}
