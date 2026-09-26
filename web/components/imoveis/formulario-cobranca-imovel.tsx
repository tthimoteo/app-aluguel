"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent, type ReactNode } from "react";
import { toast } from "sonner";
import { selectClass } from "@/components/imoveis/campos-endereco";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { bff, type CobrancaImovelInput } from "@/lib/api/browser";
import type { Imovel, PropositoLocacao } from "@/lib/api/types";
import {
  competenciaValida,
  formatarMoeda,
  mascaraCompetencia,
  mascaraMoeda,
  moedaParaInput,
  parseMoeda,
} from "@/lib/format";

const PROPOSITOS: { valor: PropositoLocacao; rotulo: string }[] = [
  { valor: "Comercial", rotulo: "Comercial" },
  { valor: "Residencial", rotulo: "Residencial" },
  { valor: "AdministracaoDeImoveis", rotulo: "Administração de imóveis" },
  { valor: "IntermediacaoImobiliaria", rotulo: "Intermediação imobiliária" },
];

function rotuloProposito(valor: string | null | undefined): string {
  if (!valor) return "—";
  return PROPOSITOS.find((p) => p.valor === valor)?.rotulo ?? valor;
}

export function FormularioCobrancaImovel({
  imovel,
  podeGerenciar,
}: {
  imovel: Imovel;
  podeGerenciar: boolean;
}) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [competencia, setCompetencia] = useState(imovel.competenciaInicial ?? "");
  const [proposito, setProposito] = useState(imovel.propositoLocacao ?? "");
  const [diaVencimento, setDiaVencimento] = useState(
    imovel.diaVencimentoCobranca != null ? String(imovel.diaVencimentoCobranca) : "",
  );
  const [condominio, setCondominio] = useState(moedaParaInput(imovel.despesasCondominiais));
  const [iptu, setIptu] = useState(moedaParaInput(imovel.valorIptu));

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setErro(null);

    if (!competenciaValida(competencia)) {
      setErro("Competência inicial deve estar no formato MM/AAAA.");
      return;
    }

    let dia: number | null = null;
    if (diaVencimento.trim()) {
      dia = Number(diaVencimento);
      if (!Number.isInteger(dia) || dia < 1 || dia > 31) {
        setErro("Dia para vencimento da cobrança deve ser um número entre 1 e 31.");
        return;
      }
    }

    const despesas = condominio.trim() ? parseMoeda(condominio) : null;
    if (despesas != null && (Number.isNaN(despesas) || despesas < 0)) {
      setErro("Informe um valor válido para despesas condominiais.");
      return;
    }

    const valorIptu = iptu.trim() ? parseMoeda(iptu) : null;
    if (valorIptu != null && (Number.isNaN(valorIptu) || valorIptu < 0)) {
      setErro("Informe um valor válido para IPTU.");
      return;
    }

    const dados: CobrancaImovelInput = {
      competenciaInicial: competencia.trim() || null,
      propositoLocacao: (proposito || null) as CobrancaImovelInput["propositoLocacao"],
      diaVencimentoCobranca: dia,
      despesasCondominiais: despesas,
      valorIptu,
    };

    setPending(true);
    try {
      await bff.definirCobrancaImovel(imovel.id, dados);
      toast.success("Cobrança do imóvel salva.");
      router.refresh();
    } catch (err) {
      setErro(err instanceof Error ? err.message : "Não foi possível salvar a cobrança.");
    } finally {
      setPending(false);
    }
  }

  if (!podeGerenciar) {
    return (
      <>
        <div className="mb-4">
          <h3 className="text-sm font-semibold">Cobrança</h3>
          <p className="mt-1 text-xs text-muted-foreground">
            Parâmetros de cobrança do imóvel. Sem permissão para editar.
          </p>
        </div>
        <dl className="grid gap-4 sm:grid-cols-2">
          <CampoDetalhe rotulo="Competência inicial" valor={imovel.competenciaInicial ?? "—"} />
          <CampoDetalhe rotulo="Propósito da locação" valor={rotuloProposito(imovel.propositoLocacao)} />
          <CampoDetalhe
            rotulo="Dia para vencimento da cobrança"
            valor={imovel.diaVencimentoCobranca != null ? String(imovel.diaVencimentoCobranca) : "—"}
          />
          <CampoDetalhe
            rotulo="Despesas condominiais"
            valor={formatarMoeda(imovel.despesasCondominiais)}
          />
          <CampoDetalhe rotulo="IPTU" valor={formatarMoeda(imovel.valorIptu)} />
        </dl>
      </>
    );
  }

  return (
    <form onSubmit={onSubmit}>
      <div className="mb-4">
        <h3 className="text-sm font-semibold">Cobrança</h3>
        <p className="mt-1 text-xs text-muted-foreground">
          Competência, propósito e encargos usados nas cobranças deste imóvel.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <Campo rotulo="Competência inicial" htmlFor="competenciaInicial">
          <Input
            id="competenciaInicial"
            name="competenciaInicial"
            inputMode="numeric"
            placeholder="MM/AAAA"
            maxLength={7}
            value={competencia}
            onChange={(e) => setCompetencia(mascaraCompetencia(e.target.value))}
            className="h-10 rounded-[4px]"
          />
        </Campo>

        <Campo rotulo="Propósito da locação" htmlFor="propositoLocacao">
          <select
            id="propositoLocacao"
            name="propositoLocacao"
            value={proposito}
            onChange={(e) => setProposito(e.target.value)}
            className={selectClass}
          >
            <option value="">Selecione…</option>
            {PROPOSITOS.map((p) => (
              <option key={p.valor} value={p.valor}>
                {p.rotulo}
              </option>
            ))}
          </select>
        </Campo>

        <Campo rotulo="Dia para vencimento da cobrança" htmlFor="diaVencimentoCobranca">
          <Input
            id="diaVencimentoCobranca"
            name="diaVencimentoCobranca"
            type="number"
            min={1}
            max={31}
            inputMode="numeric"
            value={diaVencimento}
            onChange={(e) => setDiaVencimento(e.target.value)}
            className="h-10 rounded-[4px]"
          />
        </Campo>

        <Campo rotulo="Despesas condominiais" htmlFor="despesasCondominiais">
          <Input
            id="despesasCondominiais"
            name="despesasCondominiais"
            inputMode="numeric"
            placeholder="R$ 0,00"
            value={condominio}
            onChange={(e) => setCondominio(mascaraMoeda(e.target.value))}
            className="h-10 rounded-[4px]"
          />
        </Campo>

        <Campo rotulo="IPTU" htmlFor="valorIptu">
          <Input
            id="valorIptu"
            name="valorIptu"
            inputMode="numeric"
            placeholder="R$ 0,00"
            value={iptu}
            onChange={(e) => setIptu(mascaraMoeda(e.target.value))}
            className="h-10 rounded-[4px]"
          />
        </Campo>
      </div>

      {erro ? (
        <p className="mt-4 rounded-[4px] bg-[#f8d7da] px-3 py-2 text-sm text-[#721c24]" role="alert">
          {erro}
        </p>
      ) : null}

      <div className="mt-6">
        <Button type="submit" disabled={pending} className="h-9 rounded-[4px] px-4">
          {pending ? "Salvando…" : "Salvar cobrança"}
        </Button>
      </div>
    </form>
  );
}

function Campo({
  rotulo,
  htmlFor,
  children,
}: {
  rotulo: string;
  htmlFor: string;
  children: ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <Label htmlFor={htmlFor}>{rotulo}</Label>
      {children}
    </div>
  );
}

function CampoDetalhe({ rotulo, valor }: { rotulo: string; valor: string }) {
  return (
    <div>
      <dt className="text-xs font-medium text-muted-foreground">{rotulo}</dt>
      <dd className="mt-0.5 text-sm">{valor}</dd>
    </div>
  );
}
