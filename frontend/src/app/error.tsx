"use client";

import { useEffect } from "react";
import { Alert } from "@/components/ui";

/**
 * Errores inesperados al renderizar (los errores conocidos de la API se muestran en cada página).
 */
export default function GlobalError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <div className="space-y-4">
      <Alert tone="error" title="Ocurrió un error inesperado">
        <p>No fue posible mostrar esta página. Intente de nuevo; si el problema continúa, contacte a soporte.</p>
        {error.digest ? (
          <p className="mt-2 text-xs opacity-75">
            Referencia: <code className="font-mono">{error.digest}</code>
          </p>
        ) : null}
      </Alert>
      <button type="button" onClick={reset} className="rounded-lg bg-emerald-700 px-3 py-2 text-sm font-medium text-white hover:bg-emerald-800">
        Reintentar
      </button>
    </div>
  );
}
