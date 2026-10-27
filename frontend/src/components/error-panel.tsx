import type { ApiError } from "@/core/api-error";
import { Alert } from "./ui";

/**
 * Muestra un error de la API de forma comprensible. El traceId permite a soporte ubicar
 * el request en los logs y en Jaeger.
 */
export function ErrorPanel({ error, context }: { error: ApiError; context?: string }) {
  const fieldMessages = Object.values(error.fieldErrors).flat();

  return (
    <Alert tone="error" title={context ? `${context}: ${error.title}` : error.title}>
      <p>{error.message}</p>
      {fieldMessages.length > 0 ? (
        <ul className="mt-2 list-inside list-disc">
          {fieldMessages.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      ) : null}
      {error.traceId ? (
        <p className="mt-2 text-xs opacity-75">
          Código de seguimiento: <code className="font-mono">{error.traceId}</code>
        </p>
      ) : null}
    </Alert>
  );
}
