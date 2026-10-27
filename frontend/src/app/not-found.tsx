import { ButtonLink, PageHeader } from "@/components/ui";

export default function NotFound() {
  return (
    <div>
      <PageHeader title="No encontrado" description="El recurso solicitado no existe o fue eliminado." />
      <ButtonLink href="/">Volver al inicio</ButtonLink>
    </div>
  );
}
