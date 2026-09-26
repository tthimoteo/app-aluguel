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

  const [imoveis, contratos, planos, clientes] = await Promise.all([
    api.imoveis({ take: ehAdmin ? 1 : 8 }),
    ehAdmin ? Promise.resolve(null) : api.contratos({ take: 1, status: "Ativo" }),
    ehAdmin ? api.planos() : Promise.resolve([]),
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
            <Link href="/imoveis/novo" className={cn(buttonVariants(), "h-9 rounded-[4px] px-4")}>
              Incluir imóvel
            </Link>
          ) : null
        }
      />

      <div className="mb-6 grid gap-3 sm:grid-cols-2">
        {ehAdmin ? (
          <IndicadorCard
            titulo="Clientes"
            valor={clientes?.total ?? 0}
            detalhe="Cadastro da plataforma"
          />
        ) : null}
        <IndicadorCard
          titulo="Imóveis"
          valor={imoveis.total}
          detalhe="Cadastro ativo no tenant"
        />
        {ehAdmin ? null : (
          <IndicadorCard
            titulo="Contratos ativos"
            valor={contratos?.total ?? 0}
            detalhe="Contratos vigentes no cadastro"
          />
        )}
      </div>

      {ehAdmin && clientes ? (
        <ListaClientesHome clientes={clientes} planos={planos} />
      ) : (
        <ListaImoveisHome imoveis={imoveis} />
      )}
    </div>
  );
}
