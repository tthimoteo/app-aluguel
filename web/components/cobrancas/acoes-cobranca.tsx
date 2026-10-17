"use client";

import { useState } from "react";
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
import { formatarMoeda, mascaraMoeda, moedaParaInput, parseMoeda } from "@/lib/format";

function hojeIso(): string {
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

type Painel = "pagamento" | "remover" | null;

export function AcoesCobranca({
  cobrancaId,
  competencia,
  valorFaturado,
  temPagamento,
}: {
  cobrancaId: string;
  competencia: string;
  valorFaturado: number;
  temPagamento: boolean;
}) {
  const router = useRouter();
  const [painel, setPainel] = useState<Painel>(null);
  const [pending, setPending] = useState(false);
  const [dataPagamento, setDataPagamento] = useState(hojeIso());
  const [valorTxt, setValorTxt] = useState(moedaParaInput(valorFaturado));

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
      await bff.removerCobranca(cobrancaId);
      toast.success("Cobrança removida.");
      setPainel(null);
      router.push("/cobrancas");
      router.refresh();
    } catch (e) {
      toast.error(e instanceof BffError ? e.message : "Não foi possível remover a cobrança.");
    } finally {
      setPending(false);
    }
  }

  return (
    <>
      <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border lg:col-span-2">
        <h3 className="mb-2 text-sm font-semibold">Ações</h3>
        <p className="mb-4 text-sm text-muted-foreground">
          {temPagamento
            ? "Esta cobrança já está paga e não pode ser removida."
            : "Marque o pagamento quando receber o valor, ou remova a cobrança se foi gerada por engano."}
        </p>
        <div className="flex flex-wrap gap-2">
          {!temPagamento ? (
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
          {!temPagamento ? (
            <Button
              type="button"
              variant="outline"
              className="h-9 rounded-[4px] border-[#E74C3C] text-[#E74C3C] hover:bg-[#f8d7da] hover:text-[#721c24]"
              onClick={() => setPainel("remover")}
            >
              Remover cobrança
            </Button>
          ) : (
            <p className="text-sm font-medium text-[#155724]">Pagamento registrado · remoção bloqueada</p>
          )}
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
            <DialogDescription className="pl-4">
              A cobrança da competência {competencia} será excluída (NFS-e e boleto vinculados). Esta ação não
              pode ser desfeita.
            </DialogDescription>
          </DialogHeader>
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
              className="h-9 rounded-[4px] bg-[#E74C3C] px-4 text-white hover:bg-[#C0392B]"
              onClick={() => void onRemover()}
            >
              Remover
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
