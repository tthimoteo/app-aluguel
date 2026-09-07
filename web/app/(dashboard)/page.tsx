import { FileText, Receipt, Users } from "lucide-react";
import { IndicadorCard } from "@/components/data/data-list";
import { PageHeader } from "@/components/data/page-header";
import { ListaClientesHome } from "@/components/home/lista-clientes-home";
import { ListaImoveisHome } from "@/components/home/lista-imoveis-home";
import { api } from "@/lib/api/server";
import type { Cliente, Pagina } from "@/lib/api/types";
import { requireSession, temPerfil } from "@/lib/auth/session";
import Link from "next/link";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export default async function HomePage() {
  const usuario = await requireSession();
  const ehAdmin = temPerfil(usuario, "Administrador");
  const podeIncluirImovel = temPerfil(usuario, "Gestor");

  const [imoveis, inquilinos, contratos, planos, clientes] = await Promise.all([
    api.imoveis({ take: ehAdmin ? 1 : 8 }),
    api.inquilinos({ take: 1 }),
    api.contratos({ take: 1 }),
    api.planos(),
    ehAdmin ? api.clientes({ take: 20 }) : Promise.resolve<Pagina<Cliente> | null>(null),
  ]);

  return (
    <div>
      <PageHeader
        titulo="Painel"
        descricao={
          ehAdmin
            ? "Visão administrativa dos clientes da plataforma."
            : "Indicadores operacionais do cadastro atual. Faturamento e inadimplência entram com o módulo fiscal."
        }
        acao={
          ehAdmin ? (
            <Link href="/clientes" className={cn(buttonVariants(), "h-9 rounded-[4px] px-4")}>
              Incluir cliente
            </Link>
          ) : podeIncluirImovel ? (
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

      {ehAdmin && clientes ? (
        <ListaClientesHome clientes={clientes} planos={planos} />
      ) : (
        <ListaImoveisHome imoveis={imoveis} />
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
