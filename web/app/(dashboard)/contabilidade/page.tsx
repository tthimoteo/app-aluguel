import { PageHeader } from "@/components/data/page-header";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { redirect } from "next/navigation";

export default async function ContabilidadePage() {
  const usuario = await requireSession();
  if (!temPerfil(usuario, "Gestor")) redirect("/");

  return (
    <div>
      <PageHeader
        titulo="Dados para Contabilidade"
        descricao="Exportar Contas a Receber (NFS-e) e Contas a Pagar (IPTU e despesas) em XLSX/CSV."
      />
      <div className="grid gap-4 md:grid-cols-2">
        <article className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
          <h3 className="font-semibold">Contas a Receber</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Competência, valor, cliente e número da NFS-e. Disponível após a emissão fiscal.
          </p>
        </article>
        <article className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
          <h3 className="font-semibold">Contas a Pagar</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Competência, fornecedor, valor e categoria (IPTU e outras despesas).
          </p>
        </article>
      </div>
    </div>
  );
}
