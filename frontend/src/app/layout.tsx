import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";
import "./globals.css";

export const metadata: Metadata = {
  title: { default: "Torneo de Fútbol", template: "%s · Torneo de Fútbol" },
  description: "Equipos, partidos, resultados, tabla de posiciones y goleadores del torneo.",
};

const NAVIGATION = [
  { href: "/", label: "Inicio" },
  { href: "/standings", label: "Posiciones" },
  { href: "/scorers", label: "Goleadores" },
  { href: "/matches", label: "Partidos" },
  { href: "/teams", label: "Equipos" },
];

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="es-SV">
      <body className="min-h-screen antialiased">
        <header className="border-b border-slate-200 bg-white">
          <nav aria-label="Principal" className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-6 gap-y-2 px-4 py-3">
            <Link href="/" className="text-base font-bold text-emerald-800">
              ⚽ Torneo de Fútbol
            </Link>
            <ul className="flex flex-wrap gap-4 text-sm font-medium text-slate-600">
              {NAVIGATION.map((item) => (
                <li key={item.href}>
                  <Link href={item.href} className="hover:text-emerald-700">
                    {item.label}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>
        </header>
        <main className="mx-auto max-w-6xl px-4 py-8">{children}</main>
        <footer className="mx-auto max-w-6xl px-4 pb-8 text-xs text-slate-500">Horarios en hora de El Salvador (UTC-6).</footer>
      </body>
    </html>
  );
}
