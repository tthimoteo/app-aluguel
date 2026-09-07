import { PageHeader } from "@/components/data/page-header";
import { StatusBadge } from "@/components/data/status-badge";
import { api } from "@/lib/api/server";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { formatarMoeda } from "@/lib/format";
import { redirect } from "next/navigation";

export default async function MinhaContaPage() {
  const usuario = await requireSession();
  if (!temPerfil(usuario, "Gestor")) redirect("/");

  const [me, planos] = await Promise.all([api.me(), api.planos()]);

  return (
    <div>
      <PageHeader
        titulo="Minha Conta"
        descricao="Dados do usuário autenticado e catálogo de planos. Upgrade e histórico de cobrança entram no módulo de assinaturas."
      />
      <div className="grid gap-4 lg:grid-cols-2">
        <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
          <h3 className="mb-4 text-sm font-semibold">Usuário</h3>
          <dl className="grid gap-3 text-sm">
            <Linha rotulo="Nome" valor={me.nome ?? usuario.nome} />
            <Linha rotulo="E-mail" valor={me.email ?? usuario.email} />
            <Linha
              rotulo="Perfil"
              valor={
                <span className="flex flex-wrap justify-end gap-1">
                  {(me.roles.length ? me.roles : usuario.roles).map((r) => (
                    <StatusBadge key={r} status={r} />
                  ))}
                </span>
              }
            />
            <Linha rotulo="Cliente" valor={me.clienteId ?? "—"} />
          </dl>
        </section>
        <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
          <h3 className="mb-4 text-sm font-semibold">Planos disponíveis</h3>
          <ul className="grid gap-3">
            {planos.map((p) => (
              <li key={p.id} className="rounded-[4px] border border-border p-3">
                <div className="flex items-center justify-between gap-2">
                  <p className="font-semibold">{p.nome}</p>
                  <span className="text-sm font-medium text-primary">
                    {p.valorMensal != null ? formatarMoeda(p.valorMensal) : "Sob consulta"}
                  </span>
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  Até {p.maxImoveis ?? "∞"} imóveis · {p.maxUsuarios ?? "∞"} usuários
                  {p.permiteNfse ? " · NFS-e" : ""}
                </p>
              </li>
            ))}
          </ul>
        </section>
      </div>
    </div>
  );
}

function Linha({ rotulo, valor }: { rotulo: string; valor: React.ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-4 border-b border-muted py-2 last:border-0">
      <dt className="text-muted-foreground">{rotulo}</dt>
      <dd className="text-right font-medium">{valor}</dd>
    </div>
  );
}
