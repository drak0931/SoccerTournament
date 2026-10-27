import Link from "next/link";
import type { ReactNode } from "react";
import type { MatchStatus } from "@/core/api-types";
import { MATCH_STATUS_LABELS } from "@/lib/labels";

// Componentes de presentación sin estado, compartidos por todas las páginas.

export function PageHeader({ title, description, actions }: { title: string; description?: string; actions?: ReactNode }) {
  return (
    <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">{title}</h1>
        {description ? <p className="mt-1 text-sm text-slate-600">{description}</p> : null}
      </div>
      {actions}
    </div>
  );
}

export function Card({ title, action, children }: { title?: string; action?: ReactNode; children: ReactNode }) {
  return (
    <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
      {title ? (
        <header className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
          <h2 className="text-sm font-semibold text-slate-800">{title}</h2>
          {action}
        </header>
      ) : null}
      <div className="p-4">{children}</div>
    </section>
  );
}

export function TableShell({ children }: { children: ReactNode }) {
  return (
    <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm">
      <table className="min-w-full divide-y divide-slate-200 text-sm">{children}</table>
    </div>
  );
}

export function Th({ children, align = "left" }: { children?: ReactNode; align?: "left" | "right" | "center" }) {
  const alignment = align === "right" ? "text-right" : align === "center" ? "text-center" : "text-left";
  return (
    <th scope="col" className={`whitespace-nowrap bg-slate-50 px-3 py-2 text-xs font-semibold uppercase tracking-wide text-slate-600 ${alignment}`}>
      {children}
    </th>
  );
}

export function Td({ children, align = "left", strong = false }: { children: ReactNode; align?: "left" | "right" | "center"; strong?: boolean }) {
  const alignment = align === "right" ? "text-right tabular-nums" : align === "center" ? "text-center tabular-nums" : "text-left";
  return <td className={`whitespace-nowrap px-3 py-2 ${alignment} ${strong ? "font-semibold text-slate-900" : "text-slate-700"}`}>{children}</td>;
}

export function EmptyState({ message }: { message: string }) {
  return <p className="rounded-xl border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500">{message}</p>;
}

const STATUS_STYLES: Record<MatchStatus, string> = {
  scheduled: "bg-sky-50 text-sky-700 ring-sky-200",
  finished: "bg-emerald-50 text-emerald-700 ring-emerald-200",
  cancelled: "bg-slate-100 text-slate-600 ring-slate-300",
};

export function StatusBadge({ status }: { status: MatchStatus }) {
  return (
    <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${STATUS_STYLES[status]}`}>
      {MATCH_STATUS_LABELS[status]}
    </span>
  );
}

export function TextLink({ href, children }: { href: string; children: ReactNode }) {
  return (
    <Link href={href} className="font-medium text-emerald-700 underline-offset-2 hover:underline">
      {children}
    </Link>
  );
}

export function ButtonLink({ href, children, variant = "primary" }: { href: string; children: ReactNode; variant?: "primary" | "secondary" }) {
  const styles =
    variant === "primary"
      ? "bg-emerald-700 text-white hover:bg-emerald-800"
      : "border border-slate-300 bg-white text-slate-700 hover:bg-slate-50";
  return (
    <Link href={href} className={`inline-flex items-center rounded-lg px-3 py-2 text-sm font-medium ${styles}`}>
      {children}
    </Link>
  );
}

export function Alert({ tone, title, children }: { tone: "success" | "error" | "info"; title: string; children?: ReactNode }) {
  const styles = {
    success: "border-emerald-200 bg-emerald-50 text-emerald-900",
    error: "border-red-200 bg-red-50 text-red-900",
    info: "border-sky-200 bg-sky-50 text-sky-900",
  }[tone];

  return (
    <div role={tone === "error" ? "alert" : "status"} className={`rounded-xl border p-4 text-sm ${styles}`}>
      <p className="font-semibold">{title}</p>
      {children ? <div className="mt-1">{children}</div> : null}
    </div>
  );
}
