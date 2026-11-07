"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import QRCode from "qrcode";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { bff, BffError } from "@/lib/api/browser";
import type { PagamentoPlanoPix } from "@/lib/api/types";
import { formatarCpfCnpj, formatarMoeda } from "@/lib/format";

function formatarCnpjBeneficiario(cnpj: string): string {
  const d = cnpj.replace(/\D/g, "");
  if (d.length !== 14) return formatarCpfCnpj(cnpj);
  return d.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/, "$1.$2.$3/$4-$5");
}

export function PainelPagamentoPix({
  pagamentoInicial,
  permitirSimular = false,
}: {
  pagamentoInicial: PagamentoPlanoPix;
  /** Em desenvolvimento, permite liquidar o PIX mock sem webhook real. */
  permitirSimular?: boolean;
}) {
  const router = useRouter();
  const [pagamento, setPagamento] = useState(pagamentoInicial);
  const [qrDataUrl, setQrDataUrl] = useState<string | null>(
    pagamentoInicial.pixQrCodeBase64
      ? `data:image/png;base64,${pagamentoInicial.pixQrCodeBase64}`
      : null,
  );
  const [simulando, setSimulando] = useState(false);
  const pago = pagamento.status.toLowerCase() === "pago";

  async function atualizarStatus() {
    try {
      const atual = await bff.pagamentoPlano(pagamento.id);
      setPagamento(atual);
      if (atual.status.toLowerCase() === "pago") {
        toast.success(`Plano ${atual.planoNome} ativado com sucesso.`);
        router.refresh();
        setTimeout(() => router.push("/minha-conta"), 1200);
      }
    } catch {
      /* polling silencioso */
    }
  }

  useEffect(() => {
    const copia = pagamento.pixCopiaCola;
    if (!copia || pagamento.pixQrCodeBase64) return;
    let cancelado = false;
    void QRCode.toDataURL(copia, { width: 240, margin: 2, color: { dark: "#2C3E50", light: "#FFFFFF" } }).then(
      (url) => {
        if (!cancelado) setQrDataUrl(url);
      },
    );
    return () => {
      cancelado = true;
    };
  }, [pagamento.pixCopiaCola, pagamento.pixQrCodeBase64]);

  useEffect(() => {
    if (pago) return;
    const id = window.setInterval(() => {
      void atualizarStatus();
    }, 3000);
    return () => window.clearInterval(id);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- polling intencional do status do PIX
  }, [pago, pagamento.id]);

  async function copiarCodigo() {
    if (!pagamento.pixCopiaCola) return;
    try {
      await navigator.clipboard.writeText(pagamento.pixCopiaCola);
      toast.success("Código PIX copiado.");
    } catch {
      toast.error("Não foi possível copiar o código.");
    }
  }

  async function onSimular() {
    setSimulando(true);
    try {
      await bff.simularPagamentoPix(pagamento.id);
      await atualizarStatus();
    } catch (e) {
      toast.error(e instanceof BffError ? e.message : "Falha ao simular pagamento.");
    } finally {
      setSimulando(false);
    }
  }

  const b = pagamento.beneficiario;

  return (
    <div className="mx-auto max-w-lg space-y-5">
      <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
        <h2 className="text-sm font-semibold text-[#2C3E50]">Pagamento via PIX</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Plano <span className="font-medium text-foreground">{pagamento.planoNome}</span>
          {" · "}
          {formatarMoeda(pagamento.valor)}
        </p>

        {pago ? (
          <p className="mt-4 rounded-[4px] bg-[#27AE60]/10 px-3 py-2 text-sm text-[#27AE60]">
            Pagamento confirmado. Sua assinatura está sendo ativada…
          </p>
        ) : (
          <>
            <div className="mt-4 flex justify-center">
              {qrDataUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={qrDataUrl}
                  alt="QR Code PIX para pagamento da assinatura"
                  className="h-60 w-60 rounded-[4px] bg-white p-2 ring-1 ring-border"
                />
              ) : (
                <div className="flex h-60 w-60 items-center justify-center rounded-[4px] bg-muted text-sm text-muted-foreground">
                  Gerando QR Code…
                </div>
              )}
            </div>

            <div className="mt-4 space-y-2">
              <p className="text-xs font-medium text-muted-foreground">PIX copia e cola</p>
              <textarea
                readOnly
                value={pagamento.pixCopiaCola ?? ""}
                className="h-24 w-full resize-none rounded-[4px] border border-border bg-muted/40 p-2 font-mono text-xs"
              />
              <Button type="button" className="h-9 w-full rounded-[4px]" onClick={() => void copiarCodigo()}>
                Copiar código PIX
              </Button>
            </div>

            <p className="mt-3 text-center text-xs text-muted-foreground">
              Aguardando liquidação do Banco Inter. Esta página atualiza automaticamente.
            </p>
          </>
        )}
      </section>

      <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
        <h3 className="text-sm font-semibold text-[#2C3E50]">Beneficiário</h3>
        <dl className="mt-3 space-y-2 text-sm">
          <div>
            <dt className="text-xs text-muted-foreground">Razão social</dt>
            <dd className="font-medium">{b.nome}</dd>
          </div>
          <div>
            <dt className="text-xs text-muted-foreground">CNPJ</dt>
            <dd>{formatarCnpjBeneficiario(b.cnpj)}</dd>
          </div>
          <div>
            <dt className="text-xs text-muted-foreground">Banco</dt>
            <dd>
              {b.banco} · CC {b.contaCorrente}
            </dd>
          </div>
        </dl>
      </section>

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          variant="secondary"
          className="h-9 rounded-[4px] bg-[#95A5A6] text-white hover:bg-[#7F8C8D]"
          onClick={() => router.push("/minha-conta")}
        >
          Voltar para Minha Conta
        </Button>
        {permitirSimular && !pago ? (
          <Button
            type="button"
            variant="outline"
            className="h-9 rounded-[4px]"
            disabled={simulando}
            onClick={() => void onSimular()}
          >
            {simulando ? "Confirmando…" : "Simular pagamento (dev)"}
          </Button>
        ) : null}
      </div>
    </div>
  );
}
