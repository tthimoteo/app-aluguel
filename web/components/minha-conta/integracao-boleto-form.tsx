"use client";

import { useMemo, useState, type ReactNode } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { selectClass } from "@/components/imoveis/campos-endereco";
import { bff, BffError } from "@/lib/api/browser";
import type { IntegracaoBoletoCliente, ProvedorBoleto } from "@/lib/api/types";

export function IntegracaoBoletoForm({
  clienteId,
  provedores,
  integracaoInicial,
}: {
  clienteId: string;
  provedores: ProvedorBoleto[];
  integracaoInicial: IntegracaoBoletoCliente | null;
}) {
  const [integracao, setIntegracao] = useState(integracaoInicial);
  const [provedorCodigo, setProvedorCodigo] = useState(
    integracaoInicial?.provedorCodigo ?? provedores[0]?.codigo ?? "",
  );
  const [valores, setValores] = useState<Record<string, string>>(() =>
    carregarValoresIniciais(integracaoInicial, provedores),
  );
  const [pending, setPending] = useState(false);

  const provedor = useMemo(
    () => provedores.find((p) => p.codigo === provedorCodigo) ?? null,
    [provedores, provedorCodigo],
  );

  function onTrocarProvedor(codigo: string) {
    setProvedorCodigo(codigo);
    const atual = provedores.find((p) => p.codigo === codigo);
    const next: Record<string, string> = {};
    if (atual && integracao?.provedorCodigo === codigo) {
      for (const c of atual.campos) {
        if (c.tipo === "Segredo") {
          next[c.chave] = "";
        } else {
          next[c.chave] = integracao.camposPublicos[c.chave] ?? "";
        }
      }
    } else if (atual) {
      for (const c of atual.campos) next[c.chave] = "";
    }
    setValores(next);
  }

  async function onSalvar() {
    if (!provedor) {
      toast.error("Selecione o banco / provedor de boleto.");
      return;
    }

    for (const c of provedor.campos) {
      const v = (valores[c.chave] ?? "").trim();
      if (!c.obrigatorio) continue;
      if (c.tipo === "Segredo") {
        const jaTem =
          integracao?.provedorCodigo === provedor.codigo &&
          integracao.camposSegredoPreenchidos.includes(c.chave);
        if (!v && !jaTem) {
          toast.error(`Informe: ${c.rotulo}`);
          return;
        }
      } else if (!v) {
        toast.error(`Informe: ${c.rotulo}`);
        return;
      }
    }

    setPending(true);
    try {
      const campos: Record<string, string | null> = {};
      for (const c of provedor.campos) {
        const v = (valores[c.chave] ?? "").trim();
        campos[c.chave] = v || null;
      }
      const salva = await bff.salvarIntegracaoBoleto(clienteId, {
        provedorCodigo: provedor.codigo,
        campos,
      });
      setIntegracao(salva);
      const limpos = { ...valores };
      for (const c of provedor.campos) {
        if (c.tipo === "Segredo") limpos[c.chave] = "";
      }
      setValores(limpos);
      toast.success(`Integração ${salva.provedorNome} salva. Segredos ficam cifrados no servidor.`);
    } catch (e) {
      toast.error(e instanceof BffError ? e.message : "Não foi possível salvar a integração.");
    } finally {
      setPending(false);
    }
  }

  if (provedores.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">Nenhum provedor de boleto disponível no catálogo.</p>
    );
  }

  return (
    <div className="space-y-4">
      {integracao ? (
        <p className="rounded-[4px] border border-border bg-[#F8F9FA] px-3 py-2 text-sm dark:bg-muted/40">
          Integração ativa: <span className="font-medium">{integracao.provedorNome}</span>
          {integracao.camposSegredoPreenchidos.length > 0
            ? ` · ${integracao.camposSegredoPreenchidos.length} segredo(s) cadastrado(s)`
            : null}
        </p>
      ) : (
        <p className="text-sm text-muted-foreground">
          Nenhuma integração bancária cadastrada. Sem ela não é possível gerar boleto na cobrança.
        </p>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label htmlFor="provedorBoleto" className="text-[#333] dark:text-foreground">
            Banco / provedor
          </Label>
          <select
            id="provedorBoleto"
            value={provedorCodigo}
            onChange={(e) => onTrocarProvedor(e.target.value)}
            className={selectClass}
          >
            {provedores.map((p) => (
              <option key={p.codigo} value={p.codigo}>
                {p.nome}
                {p.codigoBanco ? ` (banco ${p.codigoBanco})` : ""}
              </option>
            ))}
          </select>
        </div>

        {provedor?.campos.map((c) => {
          const segredoPreenchido =
            c.tipo === "Segredo" &&
            integracao?.provedorCodigo === provedor.codigo &&
            integracao.camposSegredoPreenchidos.includes(c.chave);
          return (
            <Campo key={c.chave} rotulo={`${c.rotulo}${c.obrigatorio ? "" : " (opcional)"}`} htmlFor={`boleto-${c.chave}`}>
              <Input
                id={`boleto-${c.chave}`}
                type={c.tipo === "Segredo" ? "password" : "text"}
                inputMode={c.tipo === "Numero" ? "numeric" : undefined}
                autoComplete="off"
                placeholder={
                  segredoPreenchido
                    ? "•••••••• (já cadastrado — deixe em branco para manter)"
                    : (c.placeholder ?? undefined)
                }
                value={valores[c.chave] ?? ""}
                onChange={(e) => setValores((prev) => ({ ...prev, [c.chave]: e.target.value }))}
                className="h-10 rounded-[4px]"
              />
              {c.ajuda ? <p className="text-xs text-muted-foreground">{c.ajuda}</p> : null}
            </Campo>
          );
        })}
      </div>

      <div className="flex justify-end">
        <Button
          type="button"
          variant="outline"
          className="h-9 rounded-[4px]"
          disabled={pending}
          onClick={() => void onSalvar()}
        >
          {pending ? "Salvando…" : "Salvar integração de boleto"}
        </Button>
      </div>
    </div>
  );
}

function carregarValoresIniciais(
  integracao: IntegracaoBoletoCliente | null,
  provedores: ProvedorBoleto[],
): Record<string, string> {
  const codigo = integracao?.provedorCodigo ?? provedores[0]?.codigo;
  const def = provedores.find((p) => p.codigo === codigo);
  const out: Record<string, string> = {};
  if (!def) return out;
  for (const c of def.campos) {
    if (c.tipo === "Segredo") out[c.chave] = "";
    else out[c.chave] = integracao?.camposPublicos[c.chave] ?? "";
  }
  return out;
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
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={htmlFor} className="text-[#333] dark:text-foreground">
        {rotulo}
      </Label>
      {children}
    </div>
  );
}
