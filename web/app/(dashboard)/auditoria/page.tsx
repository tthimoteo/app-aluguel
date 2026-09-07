import { PageHeader } from "@/components/data/page-header";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { redirect } from "next/navigation";

export default async function AuditoriaPage() {
  const usuario = await requireSession();
  if (!temPerfil(usuario, "Administrador", "Gestor")) redirect("/");

  return (
    <div>
      <PageHeader
        titulo="Auditoria"
        descricao="Trilha de login, emissão, cancelamento, pagamento, despesas e alteração cadastral."
      />
      <div className="rounded-lg bg-card p-6 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
        <p className="text-sm text-muted-foreground">
          A consulta aos logs de auditoria será liberada no incremento de Relatórios e Auditoria. O
          backend já registra eventos no login e nas alterações cadastrais.
        </p>
      </div>
    </div>
  );
}
