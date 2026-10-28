import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { AcoesCobranca } from "@/components/cobrancas/acoes-cobranca";
import { PageHeader } from "@/components/data/page-header";
import { StatusBadge } from "@/components/data/status-badge";
import { buttonVariants } from "@/components/ui/button";
import { ApiError, api } from "@/lib/api/server";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { formatarData, formatarDataHora, formatarMoeda } from "@/lib/format";
import { cn } from "@/lib/utils";
import type { ReactNode } from "react";

export default async function DetalheCobrancaPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const usuario = await requireSession();
  const ehAdmin = temPerfil(usuario, "Administrador");
  const podeGerenciar = temPerfil(usuario, "Administrador", "Gestor", "Analista");
  const me = await api.me();

  if (!ehAdmin) {
    const vinculo =
      (me.clienteId ? me.clientes?.find((c) => c.clienteId === me.clienteId) : undefined) ??
      me.clientes?.[0];
    if (!vinculo?.permiteCobranca) redirect("/");
  }

  const { id } = await params;
  let cobranca;
  try {
    cobranca = await api.cobranca(id);
  } catch (e) {
    if (e instanceof ApiError && (e.status === 404 || e.status === 400)) notFound();
    throw e;
  }

  return (
    <div>
      <p className="mb-3">
        <Link href="/cobrancas" className="text-sm font-medium text-primary hover:text-primary/80">
          ← Cobrança
        </Link>
      </p>
      <PageHeader
        titulo={`Cobrança ${cobranca.competencia}`}
        descricao={`${cobranca.imovelNome}${ehAdmin ? ` · ${cobranca.clienteNome}` : ""}`}
      />

      <div className="grid gap-4 lg:grid-cols-2">
        <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
          <h3 className="mb-3 text-sm font-semibold">Dados da cobrança</h3>
          <dl className="space-y-2 text-sm">
            <Linha rotulo="Competência" valor={cobranca.competencia} />
            {ehAdmin ? <Linha rotulo="Cliente" valor={cobranca.clienteNome} /> : null}
            <Linha rotulo="Imóvel" valor={cobranca.imovelNome} />
            <Linha rotulo="Inquilino" valor={cobranca.inquilinoNome ?? "—"} />
            <Linha rotulo="Valor do serviço" valor={formatarMoeda(cobranca.valorServico)} />
            <Linha rotulo="Desconto" valor={formatarMoeda(cobranca.desconto)} />
            <Linha rotulo="Multa" valor={formatarMoeda(cobranca.multa)} />
            <Linha rotulo="Juros" valor={formatarMoeda(cobranca.juros)} />
            <Linha rotulo="Valor faturado" valor={formatarMoeda(cobranca.valorFaturado)} />
            <Linha
              rotulo="Pagamento"
              valor={
                cobranca.temPagamento ? (
                  <span className="inline-flex flex-col items-end gap-1">
                    <StatusBadge status="Pago" />
                    <span className="text-xs font-normal text-muted-foreground">
                      {formatarData(cobranca.dataPagamento)} · {formatarMoeda(cobranca.valorPago)}
                    </span>
                  </span>
                ) : (
                  <StatusBadge status="Pendente" />
                )
              }
            />
          </dl>
        </section>

        <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
          <h3 className="mb-3 text-sm font-semibold">NFS-e</h3>
          <dl className="mb-4 space-y-2 text-sm">
            <Linha rotulo="Status" valor={<StatusBadge status={cobranca.statusNfse} />} />
            <Linha rotulo="Número" valor={cobranca.numeroNfse?.toString() ?? "—"} />
            <Linha rotulo="Série" valor={cobranca.serie ?? "—"} />
            <Linha
              rotulo="Chave de acesso"
              valor={
                cobranca.chaveAcesso ? (
                  <span className="break-all font-mono text-xs">{cobranca.chaveAcesso}</span>
                ) : (
                  "—"
                )
              }
            />
            <Linha rotulo="Emissão" valor={formatarDataHora(cobranca.dataEmissao)} />
          </dl>
          <div className="flex flex-wrap gap-2">
            {cobranca.temXml ? (
              <a
                href={`/api/cobrancas/${cobranca.id}/nfse.xml`}
                className={cn(buttonVariants({ variant: "outline" }), "h-9 rounded-[4px]")}
              >
                Baixar XML
              </a>
            ) : null}
            {cobranca.temPdfNfse ? (
              <a
                href={`/api/cobrancas/${cobranca.id}/nfse.pdf`}
                className={cn(buttonVariants({ variant: "outline" }), "h-9 rounded-[4px]")}
              >
                Baixar PDF NFS-e
              </a>
            ) : null}
            {!cobranca.temXml && !cobranca.temPdfNfse ? (
              <p className="text-sm text-muted-foreground">Documentos fiscais ainda não disponíveis.</p>
            ) : null}
          </div>
        </section>

        <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border lg:col-span-2">
          <h3 className="mb-3 text-sm font-semibold">Boleto</h3>
          {cobranca.temBoletoPdf || cobranca.boletoId ? (
            <>
              <dl className="mb-4 grid gap-2 text-sm sm:grid-cols-2">
                <Linha rotulo="Status" valor={cobranca.statusBoleto ?? "—"} />
                <Linha rotulo="Vencimento" valor={formatarData(cobranca.dataVencimentoBoleto)} />
                <Linha rotulo="Valor" valor={formatarMoeda(cobranca.valorBoleto)} />
                <Linha rotulo="Nosso número" valor={cobranca.nossoNumero ?? "—"} />
                <Linha
                  rotulo="Linha digitável"
                  valor={
                    cobranca.linhaDigitavel ? (
                      <span className="break-all font-mono text-xs">{cobranca.linhaDigitavel}</span>
                    ) : (
                      "—"
                    )
                  }
                />
              </dl>
              {cobranca.temBoletoPdf ? (
                <a
                  href={`/api/cobrancas/${cobranca.id}/boleto.pdf`}
                  className={cn(buttonVariants(), "h-9 rounded-[4px]")}
                >
                  Baixar boleto (PDF)
                </a>
              ) : (
                <p className="text-sm text-muted-foreground">PDF do boleto ainda não gerado.</p>
              )}
            </>
          ) : (
            <p className="text-sm text-muted-foreground">Nenhum boleto vinculado a esta cobrança.</p>
          )}
        </section>

        {podeGerenciar ? (
          <AcoesCobranca
            cobrancaId={cobranca.id}
            competencia={cobranca.competencia}
            valorFaturado={cobranca.valorFaturado}
            temPagamento={cobranca.temPagamento}
            statusNfse={cobranca.statusNfse}
            dataEmissao={cobranca.dataEmissao}
            chaveAcesso={cobranca.chaveAcesso}
          />
        ) : null}
      </div>
    </div>
  );
}

function Linha({ rotulo, valor }: { rotulo: string; valor: ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-4 border-b border-muted py-2 last:border-0">
      <dt className="text-muted-foreground">{rotulo}</dt>
      <dd className="text-right font-medium">{valor}</dd>
    </div>
  );
}
