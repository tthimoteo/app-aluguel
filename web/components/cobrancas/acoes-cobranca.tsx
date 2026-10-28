"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { bff, BffError } from "@/lib/api/browser";
import type { RemoverCobrancaResultado } from "@/lib/api/types";
import { formatarMoeda, mascaraMoeda, moedaParaInput, parseMoeda } from "@/lib/format";

const PRAZO_CANCELAMENTO_MS = 24 * 60 * 60 * 1000;

function hojeIso(): string {
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function nfseEmitidaHaMenosDe24h(dataEmissao: string | null, statusNfse: string): boolean {
  if (!dataEmissao) return false;
  if (statusNfse.toLowerCase() !== "emitida") return false;
  const em = Date.parse(dataEmissao);
  if (Number.isNaN(em)) return false;
  return Date.now() - em < PRAZO_CANCELAMENTO_MS;
}

type Painel = "pagamento" | "remover" | null;

export function AcoesCobranca({
  cobrancaId,
  competencia,
  valorFaturado,
  temPagamento,
  statusNfse,
  dataEmissao,
  chaveAcesso,
}: {
  cobrancaId: string;
  competencia: string;
  valorFaturado: number;
  temPagamento: boolean;
  statusNfse: string;
  dataEmissao: string | null;
  chaveAcesso: string | null;
}) {
  const router = useRouter();
  const [painel, setPainel] = useState<Painel>(null);
  const [pending, setPending] = useState(false);
  const [dataPagamento, setDataPagamento] = useState(hojeIso());
  const [valorTxt, setValorTxt] = useState(moedaParaInput(valorFaturado));

  const cancelada = statusNfse.toLowerCase() === "cancelada";
  const podeRemover = !temPagamento && !cancelada;
  const cancelamentoAutomatico = useMemo(
    () => Boolean(chaveAcesso) && nfseEmitidaHaMenosDe24h(dataEmissao, statusNfse),
    [chaveAcesso, dataEmissao, statusNfse],
  );
  const requerCancelamentoManual = Boolean(chaveAcesso) && statusNfse.toLowerCase() === "emitida" && !cancelamentoAutomatico;

  async function onMarcarPagamento() {
    const valor = parseMoeda(valorTxt);
    if (!Number.isFinite(valor) || valor <= 0) {
      toast.error("Informe um valor pago válido.");
      return;
    }
    if (!dataPagamento) {
      toast.error("Informe a data do pagamento.");
      return;
    }

    setPending(true);
    try {
      await bff.registrarPagamentoCobranca(cobrancaId, {
        dataPagamento,
        valorPago: valor,
      });
      toast.success("Pagamento registrado.");
      setPainel(null);
      router.refresh();
    } catch (e) {
      toast.error(e instanceof BffError ? e.message : "Não foi possível registrar o pagamento.");
    } finally {
      setPending(false);
    }
  }

  async function onRemover() {
    setPending(true);
    try {
      const resultado: RemoverCobrancaResultado = await bff.removerCobranca(cobrancaId);
      if (resultado.cancelamentoNfse === "RequerCancelamentoManual") {
        toast.warning(resultado.mensagem, { duration: 12_000 });
      } else {
        toast.success(resultado.mensagem || "Cobrança removida.");
      }
      setPainel(null);
      router.push("/cobrancas");
      router.refresh();
    } catch (e) {
      toast.error(e instanceof BffError ? e.message : "Não foi possível remover a cobrança.");
    } finally {
      setPending(false);
    }
  }

  const descricaoRemocao = cancelamentoAutomatico
    ? `A cobrança da competência ${competencia} será removida e a NFS-e será cancelada automaticamente na SEFIN (emitida há menos de 24 horas).`
    : requerCancelamentoManual
      ? `A cobrança da competência ${competencia} será cancelada no sistema. Como a NFS-e foi emitida há mais de 24 horas, o cancelamento fiscal deverá ser feito manualmente no portal nacional da NFS-e (gov.br).`
      : `A cobrança da competência ${competencia} será excluída (documentos e boleto vinculados). Esta ação não pode ser desfeita.`;

  return (
    <>
      <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border lg:col-span-2">
        <h3 className="mb-2 text-sm font-semibold">Ações</h3>
        <p className="mb-4 text-sm text-muted-foreground">
          {cancelada
            ? "Esta cobrança está cancelada. A NFS-e permanece no histórico fiscal."
            : temPagamento
              ? "Esta cobrança já está paga e não pode ser removida."
              : "Marque o pagamento quando receber o valor, ou remova a cobrança se foi gerada por engano."}
        </p>
        <div className="flex flex-wrap gap-2">
          {podeRemover ? (
            <Button
              type="button"
              className="h-9 rounded-[4px]"
              onClick={() => {
                setDataPagamento(hojeIso());
                setValorTxt(moedaParaInput(valorFaturado));
                setPainel("pagamento");
              }}
            >
              Marcar pagamento
            </Button>
          ) : null}
          {podeRemover ? (
            <Button
              type="button"
              variant="outline"
              className="h-9 rounded-[4px] border-[#E74C3C] text-[#E74C3C] hover:bg-[#f8d7da] hover:text-[#721c24]"
              onClick={() => setPainel("remover")}
            >
              Remover cobrança
            </Button>
          ) : temPagamento ? (
            <p className="text-sm font-medium text-[#155724]">Pagamento registrado · remoção bloqueada</p>
          ) : null}
        </div>
      </section>

      <Dialog open={painel === "pagamento"} onOpenChange={(o) => !o && setPainel(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="pl-4">Marcar pagamento</DialogTitle>
            <DialogDescription className="pl-4">
              Competência {competencia} · valor faturado {formatarMoeda(valorFaturado)}.
            </DialogDescription>
          </DialogHeader>
          <div className="m-[10px] space-y-3">
            <div className="space-y-1.5">
              <Label htmlFor="dataPagamento">Data do pagamento</Label>
              <Input
                id="dataPagamento"
                type="date"
                value={dataPagamento}
                onChange={(e) => setDataPagamento(e.target.value)}
                className="h-10 rounded-[4px]"
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="valorPago">Valor pago</Label>
              <Input
                id="valorPago"
                value={valorTxt}
                onChange={(e) => setValorTxt(mascaraMoeda(e.target.value))}
                className="h-10 rounded-[4px]"
                inputMode="decimal"
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="secondary"
              className="h-9 rounded-[4px] bg-[#95A5A6] text-white hover:bg-[#7F8C8D]"
              onClick={() => setPainel(null)}
            >
              Cancelar
            </Button>
            <Button
              type="button"
              disabled={pending}
              className="h-9 rounded-[4px] px-4"
              onClick={() => void onMarcarPagamento()}
            >
              Confirmar pagamento
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={painel === "remover"} onOpenChange={(o) => !o && setPainel(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="pl-4">Remover cobrança</DialogTitle>
            <DialogDescription className="pl-4">{descricaoRemocao}</DialogDescription>
          </DialogHeader>
          {requerCancelamentoManual ? (
            <p className="mx-[10px] rounded-[4px] bg-[#fff3cd] px-3 py-2 text-sm text-[#856404]">
              Guarde a chave de acesso da NFS-e para o cancelamento no portal do governo
              {chaveAcesso ? (
                <>
                  : <span className="break-all font-mono text-xs">{chaveAcesso}</span>
                </>
              ) : null}
              .
            </p>
          ) : null}
          <DialogFooter>
            <Button
              type="button"
              variant="secondary"
              className="h-9 rounded-[4px] bg-[#95A5A6] text-white hover:bg-[#7F8C8D]"
              onClick={() => setPainel(null)}
            >
              Voltar
            </Button>
            <Button
              type="button"
              disabled={pending}
              className="h-9 rounded-[4px] bg-[#E74C3C] px-4 text-white hover:bg-[#C0392B]"
              onClick={() => void onRemover()}
            >
              Confirmar remoção
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
