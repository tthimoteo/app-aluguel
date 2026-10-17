"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { selectClass } from "@/components/imoveis/campos-endereco";
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
import { competenciaValida, formatarMoeda, mascaraCompetencia } from "@/lib/format";

export type ImovelParaCobranca = {
  id: string;
  nome: string;
  competenciaInicial: string | null;
  diaVencimentoCobranca: number | null;
  despesasCondominiais: number | null;
  valorIptu: number | null;
  valorAluguel: number | null;
  diaVencimentoContrato: number | null;
  temContratoAtivo: boolean;
};

function competenciaAtual(): string {
  const agora = new Date();
  const mes = String(agora.getMonth() + 1).padStart(2, "0");
  return `${mes}/${agora.getFullYear()}`;
}

export function GerarCobrancaDialog({ imoveis }: { imoveis: ImovelParaCobranca[] }) {
  const router = useRouter();
  const [aberto, setAberto] = useState(false);
  const [imovelId, setImovelId] = useState(imoveis[0]?.id ?? "");
  const [competencia, setCompetencia] = useState(competenciaAtual());
  const [pending, setPending] = useState(false);

  const selecionado = useMemo(
    () => imoveis.find((i) => i.id === imovelId) ?? null,
    [imoveis, imovelId],
  );

  const aluguel = selecionado?.valorAluguel ?? 0;
  const condo = selecionado?.despesasCondominiais ?? 0;
  const iptu = selecionado?.valorIptu ?? 0;
  const total = aluguel + condo + iptu;
  const diaVenc =
    selecionado?.diaVencimentoCobranca ?? selecionado?.diaVencimentoContrato ?? null;

  async function onGerar() {
    if (!selecionado) {
      toast.error("Selecione um imóvel.");
      return;
    }
    if (!competenciaValida(competencia)) {
      toast.error("Informe a competência no formato MM/AAAA.");
      return;
    }
    if (!selecionado.temContratoAtivo || selecionado.valorAluguel == null) {
      toast.error("O imóvel precisa de contrato ativo com valor de aluguel.");
      return;
    }
    if (!selecionado.competenciaInicial) {
      toast.error("Cadastre os dados de cobrança do imóvel (competência inicial) antes de gerar.");
      return;
    }

    setPending(true);
    try {
      const criada = await bff.gerarCobranca({ imovelId: selecionado.id, competencia });
      toast.success(`Cobrança ${criada.competencia} gerada.`);
      setAberto(false);
      router.push(`/cobrancas/${criada.id}`);
      router.refresh();
    } catch (e) {
      toast.error(e instanceof BffError ? e.message : "Não foi possível gerar a cobrança.");
    } finally {
      setPending(false);
    }
  }

  if (imoveis.length === 0) return null;

  return (
    <>
      <Button type="button" className="h-9 rounded-[4px] px-4" onClick={() => setAberto(true)}>
        Gerar nova cobrança
      </Button>
      <Dialog open={aberto} onOpenChange={setAberto}>
      <DialogContent className="sm:max-w-lg">
        <div className="m-[10px] flex flex-col gap-4">
          <DialogHeader className="border-0 p-0 pr-10">
            <DialogTitle>Gerar nova cobrança</DialogTitle>
            <DialogDescription>
              Os valores consideram o aluguel do contrato ativo e os encargos cadastrados no imóvel
              (condomínio e IPTU).
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="gerar-imovel">Imóvel</Label>
              <select
                id="gerar-imovel"
                value={imovelId}
                onChange={(e) => setImovelId(e.target.value)}
                className={selectClass}
              >
                {imoveis.map((i) => (
                  <option key={i.id} value={i.id}>
                    {i.nome}
                    {!i.temContratoAtivo ? " (sem contrato ativo)" : ""}
                    {!i.competenciaInicial ? " (sem dados de cobrança)" : ""}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="gerar-competencia">Competência</Label>
              <Input
                id="gerar-competencia"
                value={competencia}
                onChange={(e) => setCompetencia(mascaraCompetencia(e.target.value))}
                placeholder="MM/AAAA"
                className="h-10 rounded-[4px]"
              />
              {selecionado?.competenciaInicial ? (
                <p className="text-xs text-muted-foreground">
                  Competência inicial do imóvel: {selecionado.competenciaInicial}
                  {diaVenc != null ? ` · vencimento dia ${diaVenc}` : ""}
                </p>
              ) : null}
            </div>
          </div>

          <dl className="grid gap-2 rounded-[4px] border border-border bg-[#F8F9FA] p-3 text-sm dark:bg-muted/40">
            <Linha rotulo="Aluguel (contrato)" valor={formatarMoeda(aluguel)} />
            <Linha rotulo="Despesas condominiais" valor={formatarMoeda(condo)} />
            <Linha rotulo="IPTU" valor={formatarMoeda(iptu)} />
            <Linha rotulo="Total da cobrança" valor={formatarMoeda(total)} destaque />
          </dl>

          <DialogFooter className="border-0 p-0">
            <Button
              type="button"
              variant="secondary"
              className="h-9 rounded-[4px] bg-[#95A5A6] text-white hover:bg-[#7F8C8D]"
              onClick={() => setAberto(false)}
              disabled={pending}
            >
              Cancelar
            </Button>
            <Button type="button" className="h-9 rounded-[4px]" disabled={pending} onClick={() => void onGerar()}>
              {pending ? "Gerando…" : "Gerar cobrança"}
            </Button>
          </DialogFooter>
        </div>
      </DialogContent>
      </Dialog>
    </>
  );
}

function Linha({
  rotulo,
  valor,
  destaque,
}: {
  rotulo: string;
  valor: string;
  destaque?: boolean;
}) {
  return (
    <div className="flex items-center justify-between gap-3">
      <dt className={destaque ? "font-semibold" : "text-muted-foreground"}>{rotulo}</dt>
      <dd className={destaque ? "font-semibold text-primary" : "font-medium"}>{valor}</dd>
    </div>
  );
}
