import { PageHeader } from "@/components/data/page-header";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { redirect } from "next/navigation";

export default async function RelatoriosPage() {
  const usuario = await requireSession();
  if (!temPerfil(usuario, "Gestor", "Analista")) redirect("/");

  return (
    <div>
      <PageHeader
        titulo="Relatórios"
        descricao="Extrair histórico de faturamentos, pagamentos, IPTU e demais despesas (UC008 — Sprint 7)."
      />
      <div className="rounded-lg bg-card p-6 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
        <p className="text-sm text-muted-foreground">
          Os relatórios ficarão disponíveis quando o módulo financeiro e fiscal estiver ativo. Enquanto
          isso, consulte os cadastros de imóveis, inquilinos e contratos no menu.
        </p>
      </div>
    </div>
  );
}
