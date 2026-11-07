import { Suspense } from "react";
import { PageHeader } from "@/components/data/page-header";
import { PainelRelatorios } from "@/components/relatorios/painel-relatorios";
import { ApiError, api } from "@/lib/api/server";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { nomeCliente } from "@/lib/format";
import { redirect } from "next/navigation";

export default async function RelatoriosPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const usuario = await requireSession();
  if (!temPerfil(usuario, "Administrador", "Gestor", "Analista")) redirect("/inicio");

  const sp = await searchParams;
  const ehAdmin = temPerfil(usuario, "Administrador");
  const anoPadrao = new Date().getFullYear();

  const ano = num(sp.ano) ?? anoPadrao;
  const mes = num(sp.mes);
  const dataInicio = str(sp.dataInicio);
  const dataFim = str(sp.dataFim);
  const clienteId = str(sp.clienteId) ?? (ehAdmin ? undefined : usuario.clienteId ?? undefined);
  const imovelId = str(sp.imovelId);
  const diasAtrasoMinimos = num(sp.diasAtrasoMinimos) ?? 1;

  let painel;
  let imoveisPagina: Awaited<ReturnType<typeof api.imoveis>>;
  let clientesPagina: Awaited<ReturnType<typeof api.clientes>> | null;
  try {
    [painel, imoveisPagina, clientesPagina] = await Promise.all([
      api.relatorioPainel({
        ano: dataInicio || dataFim ? undefined : ano,
        mes: dataInicio || dataFim ? undefined : mes,
        dataInicio: dataInicio ?? undefined,
        dataFim: dataFim ?? undefined,
        clienteId: clienteId ?? undefined,
        imovelId: imovelId ?? undefined,
        diasAtrasoMinimos,
      }),
      api.imoveis({ take: 100, clienteId: clienteId ?? undefined }),
      ehAdmin ? api.clientes({ take: 100 }) : Promise.resolve(null),
    ]);
  } catch (e) {
    if (e instanceof ApiError) {
      return (
        <div>
          <PageHeader
            titulo="Relatórios"
            descricao="Dashboard financeiro, inadimplência, vacância, fluxo de caixa e painel fiscal da carteira (UC008)."
          />
          <p className="text-sm text-destructive">
            Não foi possível carregar o painel de relatórios ({e.message}). Confirme se a API está
            atualizada e tente novamente.
          </p>
        </div>
      );
    }
    throw e;
  }

  const imoveis = imoveisPagina.itens.map((i) => ({ id: i.id, nome: i.nome }));
  const clientes =
    clientesPagina?.itens.map((c) => ({ id: c.id, nome: nomeCliente(c) })) ?? [];

  return (
    <div>
      <PageHeader
        titulo="Relatórios"
        descricao="Dashboard financeiro, inadimplência, vacância, fluxo de caixa e painel fiscal da carteira (UC008)."
      />
      <Suspense fallback={<p className="text-sm text-muted-foreground">Carregando painel…</p>}>
        <PainelRelatorios
          painel={painel}
          imoveis={imoveis}
          clientes={clientes}
          ehAdmin={ehAdmin}
          anoPadrao={anoPadrao}
        />
      </Suspense>
    </div>
  );
}

function str(v: string | string[] | undefined): string | null {
  if (typeof v === "string" && v.trim()) return v.trim();
  return null;
}

function num(v: string | string[] | undefined): number | null {
  const s = str(v);
  if (!s) return null;
  const n = Number(s);
  return Number.isFinite(n) ? n : null;
}
