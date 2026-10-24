import Link from "next/link";
import { redirect } from "next/navigation";
import { CardField, DataList, DesktopTable, MobileCard } from "@/components/data/data-list";
import { EmptyState } from "@/components/data/empty-state";
import { PageHeader } from "@/components/data/page-header";
import { StatusBadge } from "@/components/data/status-badge";
import { FiltrosCobranca } from "@/components/cobrancas/filtros-cobranca";
import { GerarCobrancaDialog, type ImovelParaCobranca } from "@/components/cobrancas/gerar-cobranca-dialog";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { ApiError, api } from "@/lib/api/server";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { formatarMoeda } from "@/lib/format";

export default async function CobrancasPage({
  searchParams,
}: {
  searchParams: Promise<{ competencia?: string; imovelId?: string; clienteId?: string }>;
}) {
  const usuario = await requireSession();
  const ehAdmin = temPerfil(usuario, "Administrador");
  const me = await api.me();
  const { competencia, imovelId, clienteId } = await searchParams;

  if (!ehAdmin) {
    const vinculo =
      (me.clienteId ? me.clientes?.find((c) => c.clienteId === me.clienteId) : undefined) ??
      me.clientes?.[0];
    if (!vinculo?.permiteCobranca) redirect("/");
  }

  const clienteFiltro = ehAdmin ? clienteId : me.clienteId ?? undefined;

  let pagina;
  try {
    pagina = await api.cobrancas({
      clienteId: clienteFiltro,
      imovelId,
      competencia,
      take: 50,
    });
  } catch (e) {
    if (e instanceof ApiError && e.status === 400) {
      return (
        <div>
          <PageHeader titulo="Cobrança" descricao="Histórico de cobranças de aluguel com NFS-e e boletos." />
          <p className="text-sm text-destructive">{e.message}</p>
        </div>
      );
    }
    throw e;
  }

  const podeGerar = temPerfil(usuario, "Administrador", "Gestor", "Analista");

  const [imoveisPagina, clientesPagina, contratosPagina] = await Promise.all([
    api.imoveis({ clienteId: clienteFiltro, take: 100 }).catch(() => ({ itens: [], total: 0, skip: 0, take: 0 })),
    ehAdmin
      ? api.clientes({ take: 100 }).catch(() => ({ itens: [], total: 0, skip: 0, take: 0 }))
      : Promise.resolve({ itens: [], total: 0, skip: 0, take: 0 }),
    api
      .contratos({ clienteId: clienteFiltro, status: "Ativo", take: 100 })
      .catch(() => ({ itens: [], total: 0, skip: 0, take: 0 })),
  ]);

  const contratosPorImovel = new Map(
    contratosPagina.itens.map((c) => [c.imovelId, c] as const),
  );
  const imoveisParaGerar: ImovelParaCobranca[] = imoveisPagina.itens
    .filter((i) => i.status === "Ativo")
    .map((i) => {
      const contrato = contratosPorImovel.get(i.id);
      return {
        id: i.id,
        nome: i.nome,
        competenciaInicial: i.competenciaInicial,
        diaVencimentoCobranca: i.diaVencimentoCobranca,
        despesasCondominiais: i.despesasCondominiais,
        valorIptu: i.valorIptu,
        valorAluguel: contrato?.valorAluguel ?? null,
        diaVencimentoContrato: contrato?.diaVencimento ?? null,
        temContratoAtivo: Boolean(contrato),
      };
    });

  return (
    <div>
      <PageHeader
        titulo="Cobrança"
        descricao="Histórico de cobranças por competência. Consulte NFS-e (XML/PDF) e boleto quando disponíveis."
        acao={podeGerar ? <GerarCobrancaDialog imoveis={imoveisParaGerar} /> : null}
      />
      <FiltrosCobranca
        competencia={competencia}
        imovelId={imovelId}
        clienteId={clienteId}
        mostrarCliente={ehAdmin}
        imoveis={imoveisPagina.itens.map((i) => ({ id: i.id, nome: i.nome }))}
        clientes={clientesPagina.itens.map((c) => ({ id: c.id, nome: c.nomeExibicao }))}
      />
      {pagina.itens.length === 0 ? (
        <EmptyState mensagem="Nenhuma cobrança encontrada. Ajuste os filtros ou aguarde novas emissões." />
      ) : (
        <DataList
          desktop={
            <DesktopTable>
              <Table>
                <TableHeader className="bg-[#34495e] [&_th]:text-white">
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="text-white">Competência</TableHead>
                    {ehAdmin ? <TableHead className="text-white">Cliente</TableHead> : null}
                    <TableHead className="text-white">Imóvel</TableHead>
                    <TableHead className="text-white">Valor</TableHead>
                    <TableHead className="text-white">NFS-e</TableHead>
                    <TableHead className="text-white">Pagamento</TableHead>
                    <TableHead className="text-white">Docs</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {pagina.itens.map((c) => (
                    <TableRow key={c.id} className="cursor-pointer hover:bg-muted/40">
                      <TableCell className="font-medium">
                        <Link href={`/cobrancas/${c.id}`} className="text-primary hover:underline">
                          {c.competencia}
                        </Link>
                      </TableCell>
                      {ehAdmin ? (
                        <TableCell>
                          <Link href={`/cobrancas/${c.id}`} className="block">
                            {c.clienteNome}
                          </Link>
                        </TableCell>
                      ) : null}
                      <TableCell>
                        <Link href={`/cobrancas/${c.id}`} className="block">
                          {c.imovelNome}
                        </Link>
                      </TableCell>
                      <TableCell>
                        <Link href={`/cobrancas/${c.id}`} className="block">
                          {formatarMoeda(c.valorFaturado)}
                        </Link>
                      </TableCell>
                      <TableCell>
                        <Link href={`/cobrancas/${c.id}`} className="block">
                          <StatusBadge status={c.statusNfse} />
                        </Link>
                      </TableCell>
                      <TableCell>
                        <Link href={`/cobrancas/${c.id}`} className="block">
                          <StatusBadge status={c.temPagamento ? "Pago" : "Pendente"} />
                        </Link>
                      </TableCell>
                      <TableCell className="text-xs text-muted-foreground">
                        <Link href={`/cobrancas/${c.id}`} className="block">
                          {[c.temXml && "XML", c.temPdfNfse && "PDF", c.temBoletoPdf && "Boleto"]
                            .filter(Boolean)
                            .join(" · ") || "—"}
                        </Link>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </DesktopTable>
          }
          mobile={pagina.itens.map((c) => (
            <MobileCard key={c.id}>
              <Link href={`/cobrancas/${c.id}`} className="block space-y-2">
                <p className="font-semibold text-primary">
                  {c.competencia} · {c.imovelNome}
                </p>
                {ehAdmin ? <CardField label="Cliente">{c.clienteNome}</CardField> : null}
                <CardField label="Valor">{formatarMoeda(c.valorFaturado)}</CardField>
                <CardField label="NFS-e">
                  <StatusBadge status={c.statusNfse} />
                </CardField>
                <CardField label="Pagamento">
                  <StatusBadge status={c.temPagamento ? "Pago" : "Pendente"} />
                </CardField>
              </Link>
            </MobileCard>
          ))}
        />
      )}
      <p className="mt-3 text-xs text-muted-foreground">{pagina.total} cobrança(s)</p>
    </div>
  );
}
