import { IndicadorCard } from "@/components/data/data-list";
import { PageHeader } from "@/components/data/page-header";
import { ListaClientesHome } from "@/components/home/lista-clientes-home";
import { ListaImoveisHome } from "@/components/home/lista-imoveis-home";
import { api } from "@/lib/api/server";
import type { Cliente, CobrancaResumo, Pagina } from "@/lib/api/types";
import { requireSession, temPerfil } from "@/lib/auth/session";
import Link from "next/link";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export default async function HomePage() {
  const usuario = await requireSession();
  const ehAdmin = temPerfil(usuario, "Administrador");
  const podeIncluirImovel = temPerfil(usuario, "Gestor");
  const me = await api.me();

  const permiteCobranca =
    ehAdmin ||
    Boolean(
      (me.clienteId ? me.clientes?.find((c) => c.clienteId === me.clienteId) : undefined)?.permiteCobranca ??
        me.clientes?.[0]?.permiteCobranca,
    );

  const [imoveis, contratos, planos, clientes, resumoCobranca] = await Promise.all([
    api.imoveis({ take: ehAdmin ? 1 : 8 }),
    ehAdmin ? Promise.resolve(null) : api.contratos({ take: 1, status: "Ativo" }),
    ehAdmin ? api.planos() : Promise.resolve([]),
    ehAdmin ? api.clientes({ take: 20 }) : Promise.resolve<Pagina<Cliente> | null>(null),
    permiteCobranca
      ? api.cobrancaResumo({ clienteId: ehAdmin ? undefined : me.clienteId ?? undefined }).catch(
          (): CobrancaResumo => ({ emAberto: 0, atrasadas: 0 }),
        )
      : Promise.resolve<CobrancaResumo | null>(null),
  ]);

  return (
    <div>
      <PageHeader
        titulo="Painel"
        descricao={
          ehAdmin
            ? "Visão administrativa dos clientes da plataforma."
            : "Indicadores operacionais do cadastro atual."
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

      <div className="mb-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
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
        {resumoCobranca ? (
          <Link
            href="/cobrancas"
            className="rounded-lg bg-card p-4 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border transition hover:-translate-y-0.5 hover:shadow-[0_4px_12px_rgba(0,0,0,0.15)]"
          >
            <p className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
              Cobranças emitidas
            </p>
            <div className="mt-2 flex items-end gap-6">
              <div>
                <p className="text-2xl font-bold text-foreground">{resumoCobranca.emAberto}</p>
                <p className="text-xs text-muted-foreground">Vencimento em aberto</p>
              </div>
              <div>
                <p
                  className={cn(
                    "text-2xl font-bold",
                    resumoCobranca.atrasadas > 0 ? "text-[#E74C3C]" : "text-foreground",
                  )}
                >
                  {resumoCobranca.atrasadas}
                </p>
                <p className="text-xs text-muted-foreground">Atrasadas de pagamento</p>
              </div>
            </div>
          </Link>
        ) : null}
      </div>

      {ehAdmin && clientes ? (
        <ListaClientesHome clientes={clientes} planos={planos} />
      ) : (
        <ListaImoveisHome imoveis={imoveis} />
      )}
    </div>
  );
}
