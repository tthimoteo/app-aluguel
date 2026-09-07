import { Building2, FileText, Receipt, Users } from "lucide-react";
import { IndicadoresChart } from "@/components/charts/indicadores-chart";
import { EmptyState } from "@/components/data/empty-state";
import { IndicadorCard } from "@/components/data/data-list";
import { PageHeader } from "@/components/data/page-header";
import { StatusBadge } from "@/components/data/status-badge";
import { api } from "@/lib/api/server";
import { requireSession } from "@/lib/auth/session";
import { formatarEndereco } from "@/lib/format";
import Link from "next/link";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export default async function HomePage() {
  const usuario = await requireSession();
  const [imoveis, inquilinos, contratos, planos] = await Promise.all([
    api.imoveis({ take: 8 }),
    api.inquilinos({ take: 1 }),
    api.contratos({ take: 1 }),
    api.planos(),
  ]);

  const indicadores = [
    { nome: "Imóveis", quantidade: imoveis.total },
    { nome: "Inquilinos", quantidade: inquilinos.total },
    { nome: "Contratos", quantidade: contratos.total },
    { nome: "Planos", quantidade: planos.length },
  ];

  const podeIncluir = usuario.roles.some((r) => r === "Administrador" || r === "Gestor");

  return (
    <div>
      <PageHeader
        titulo="Painel"
        descricao="Indicadores operacionais do cadastro atual. Faturamento e inadimplência entram com o módulo fiscal."
        acao={
          podeIncluir ? (
            <Link href="/imoveis" className={cn(buttonVariants(), "h-9 rounded-[4px] px-4")}>
              Incluir imóvel
            </Link>
          ) : null
        }
      />

      <div className="mb-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <IndicadorCard
          titulo="Imóveis"
          valor={imoveis.total}
          detalhe="Cadastro ativo no tenant"
        />
        <IndicadorCard
          titulo="Inquilinos"
          valor={inquilinos.total}
          detalhe="Pessoas físicas e jurídicas"
        />
        <IndicadorCard
          titulo="Contratos"
          valor={contratos.total}
          detalhe="Ativos, encerrados e cancelados"
        />
        <IndicadorCard
          titulo="Planos"
          valor={planos.length}
          detalhe="Catálogo disponível"
        />
      </div>

      <div className="mb-6 rounded-lg bg-card p-4 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
        <h3 className="mb-3 text-sm font-semibold">Cadastros</h3>
        <IndicadoresChart dados={indicadores} />
      </div>

      <div className="mb-4 flex items-center justify-between">
        <h3 className="text-sm font-semibold">Imóveis</h3>
        <Link href="/imoveis" className="text-sm font-medium text-primary hover:text-primary/80">
          Ver todos
        </Link>
      </div>

      {imoveis.itens.length === 0 ? (
        <EmptyState mensagem="Nenhum imóvel cadastrado. Inclua o primeiro imóvel para começar." />
      ) : (
        <ul className="grid gap-3">
          {imoveis.itens.map((imovel) => (
            <li
              key={imovel.id}
              className="flex flex-col gap-3 rounded-lg bg-card p-4 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border sm:flex-row sm:items-center sm:justify-between"
            >
              <div className="flex items-start gap-3">
                <div className="flex size-10 items-center justify-center rounded-[4px] bg-primary/10 text-primary">
                  <Building2 className="size-5" />
                </div>
                <div>
                  <p className="font-semibold">{imovel.nome}</p>
                  <p className="text-sm text-muted-foreground">{formatarEndereco(imovel.endereco)}</p>
                  <p className="mt-1 text-xs text-muted-foreground">{imovel.tipo}</p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <StatusBadge status={imovel.status} />
                <span className="hidden text-xs text-muted-foreground sm:inline">NFS-e e pagamentos em breve</span>
              </div>
            </li>
          ))}
        </ul>
      )}

      <div className="mt-8 grid gap-3 sm:grid-cols-3">
        <Atalho href="/inquilinos" icone={Users} titulo="Inquilinos" />
        <Atalho href="/contratos" icone={FileText} titulo="Contratos" />
        <Atalho href="/relatorios" icone={Receipt} titulo="Relatórios" />
      </div>
    </div>
  );
}

function Atalho({
  href,
  icone: Icone,
  titulo,
}: {
  href: string;
  icone: typeof Users;
  titulo: string;
}) {
  return (
    <Link
      href={href}
      className="flex items-center gap-3 rounded-lg bg-card p-4 text-sm font-medium shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border transition-transform duration-200 hover:-translate-y-0.5"
    >
      <Icone className="size-4 text-primary" />
      {titulo}
    </Link>
  );
}
