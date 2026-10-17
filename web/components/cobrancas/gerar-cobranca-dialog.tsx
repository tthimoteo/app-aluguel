"use client";

import { useEffect, useMemo, useState } from "react";
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
import {
  competenciaValida,
  formatarMoeda,
  mascaraCompetencia,
  mascaraMoeda,
  moedaParaInput,
  parseMoeda,
} from "@/lib/format";

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

  const [incluirCondo, setIncluirCondo] = useState(true);
  const [incluirIptu, setIncluirIptu] = useState(true);
  const [emitirNfse, setEmitirNfse] = useState(true);
  const [aluguelTxt, setAluguelTxt] = useState("");
  const [condoTxt, setCondoTxt] = useState("");
  const [iptuTxt, setIptuTxt] = useState("");
  const [diaVencimento, setDiaVencimento] = useState("10");

  const selecionado = useMemo(
    () => imoveis.find((i) => i.id === imovelId) ?? null,
    [imoveis, imovelId],
  );

  useEffect(() => {
    if (!selecionado) return;
    setAluguelTxt(moedaParaInput(selecionado.valorAluguel ?? 0));
    setCondoTxt(moedaParaInput(selecionado.despesasCondominiais ?? 0));
    setIptuTxt(moedaParaInput(selecionado.valorIptu ?? 0));
    setIncluirCondo((selecionado.despesasCondominiais ?? 0) > 0);
    setIncluirIptu((selecionado.valorIptu ?? 0) > 0);
    const dia = selecionado.diaVencimentoCobranca ?? selecionado.diaVencimentoContrato ?? 10;
    setDiaVencimento(String(dia));
  }, [selecionado]);

  const aluguel = parseMoeda(aluguelTxt);
  const condo = incluirCondo ? parseMoeda(condoTxt) : 0;
  const iptu = incluirIptu ? parseMoeda(iptuTxt) : 0;
  const total =
    (Number.isNaN(aluguel) ? 0 : aluguel) +
    (Number.isNaN(condo) ? 0 : condo) +
    (Number.isNaN(iptu) ? 0 : iptu);

  async function onGerar() {
    if (!selecionado) {
      toast.error("Selecione um imóvel.");
      return;
    }
    if (!competenciaValida(competencia)) {
      toast.error("Informe a competência no formato MM/AAAA.");
      return;
    }
    if (!selecionado.temContratoAtivo) {
      toast.error("O imóvel precisa de contrato ativo para gerar a cobrança.");
      return;
    }
    if (!selecionado.competenciaInicial) {
      toast.error("Cadastre os dados de cobrança do imóvel (competência inicial) antes de gerar.");
      return;
    }
    if (Number.isNaN(aluguel) || aluguel < 0) {
      toast.error("Informe um valor válido para o aluguel.");
      return;
    }
    if (incluirCondo && (Number.isNaN(condo) || condo < 0)) {
      toast.error("Informe um valor válido para o condomínio.");
      return;
    }
    if (incluirIptu && (Number.isNaN(iptu) || iptu < 0)) {
      toast.error("Informe um valor válido para o IPTU.");
      return;
    }
    const dia = Number(diaVencimento);
    if (!Number.isInteger(dia) || dia < 1 || dia > 31) {
      toast.error("Dia de cobrança deve ser um número entre 1 e 31.");
      return;
    }
    if (total <= 0) {
      toast.error("O valor total da cobrança deve ser maior que zero.");
      return;
    }

    setPending(true);
    try {
      const criada = await bff.gerarCobranca({
        imovelId: selecionado.id,
        competencia,
        valorAluguel: aluguel,
        incluirCondominio: incluirCondo,
        valorCondominio: incluirCondo ? condo : 0,
        incluirIptu,
        valorIptu: incluirIptu ? iptu : 0,
        diaVencimento: dia,
        emitirNfse,
      });
      toast.success(
        emitirNfse
          ? `Cobrança ${criada.competencia} gerada com NFS-e.`
          : `Cobrança ${criada.competencia} gerada (sem NFS-e).`,
      );
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
          <div className="m-[10px] flex max-h-[80vh] flex-col gap-4 overflow-y-auto">
            <DialogHeader className="border-0 p-0 pr-10">
              <DialogTitle>Gerar nova cobrança</DialogTitle>
              <DialogDescription>
                Ajuste os valores a partir do cadastro do imóvel e do contrato. Um imóvel não pode ter
                duas cobranças na mesma competência.
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

              <div className="grid gap-3 sm:grid-cols-2">
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
                      Inicial do imóvel: {selecionado.competenciaInicial}
                    </p>
                  ) : null}
                </div>
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="gerar-dia">Dia de cobrança</Label>
                  <Input
                    id="gerar-dia"
                    type="number"
                    min={1}
                    max={31}
                    value={diaVencimento}
                    onChange={(e) => setDiaVencimento(e.target.value)}
                    className="h-10 rounded-[4px]"
                  />
                </div>
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="gerar-aluguel">Valor do aluguel</Label>
                <Input
                  id="gerar-aluguel"
                  value={aluguelTxt}
                  onChange={(e) => setAluguelTxt(mascaraMoeda(e.target.value))}
                  className="h-10 rounded-[4px]"
                />
              </div>

              <div className="rounded-[4px] border border-border p-3">
                <label className="flex items-center gap-2 text-sm font-medium">
                  <input
                    type="checkbox"
                    checked={incluirCondo}
                    onChange={(e) => setIncluirCondo(e.target.checked)}
                    className="size-4 rounded border-input accent-[#DB6838]"
                  />
                  Considerar condomínio
                </label>
                <div className="mt-2 flex flex-col gap-1.5">
                  <Label htmlFor="gerar-condo" className="text-xs text-muted-foreground">
                    Valor do condomínio
                  </Label>
                  <Input
                    id="gerar-condo"
                    value={condoTxt}
                    onChange={(e) => setCondoTxt(mascaraMoeda(e.target.value))}
                    disabled={!incluirCondo}
                    className="h-10 rounded-[4px]"
                  />
                </div>
              </div>

              <div className="rounded-[4px] border border-border p-3">
                <label className="flex items-center gap-2 text-sm font-medium">
                  <input
                    type="checkbox"
                    checked={incluirIptu}
                    onChange={(e) => setIncluirIptu(e.target.checked)}
                    className="size-4 rounded border-input accent-[#DB6838]"
                  />
                  Considerar IPTU
                </label>
                <div className="mt-2 flex flex-col gap-1.5">
                  <Label htmlFor="gerar-iptu" className="text-xs text-muted-foreground">
                    Valor do IPTU
                  </Label>
                  <Input
                    id="gerar-iptu"
                    value={iptuTxt}
                    onChange={(e) => setIptuTxt(mascaraMoeda(e.target.value))}
                    disabled={!incluirIptu}
                    className="h-10 rounded-[4px]"
                  />
                </div>
              </div>

              <label className="flex items-center gap-2 rounded-[4px] border border-border p-3 text-sm font-medium">
                <input
                  type="checkbox"
                  checked={emitirNfse}
                  onChange={(e) => setEmitirNfse(e.target.checked)}
                  className="size-4 rounded border-input accent-[#DB6838]"
                />
                Emitir NFS-e nesta cobrança
              </label>
            </div>

            <dl className="grid gap-2 rounded-[4px] border border-border bg-[#F8F9FA] p-3 text-sm dark:bg-muted/40">
              <Linha rotulo="Aluguel" valor={formatarMoeda(Number.isNaN(aluguel) ? 0 : aluguel)} />
              <Linha
                rotulo="Condomínio"
                valor={incluirCondo ? formatarMoeda(Number.isNaN(condo) ? 0 : condo) : "Não considerado"}
              />
              <Linha
                rotulo="IPTU"
                valor={incluirIptu ? formatarMoeda(Number.isNaN(iptu) ? 0 : iptu) : "Não considerado"}
              />
              <Linha rotulo="Total da cobrança" valor={formatarMoeda(total)} destaque />
              <Linha rotulo="NFS-e" valor={emitirNfse ? "Será emitida" : "Não emitir"} />
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
