"use client";

import { useState, type ReactNode } from "react";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { bff, BffError } from "@/lib/api/browser";
import { formatarCep, somenteDigitosCep } from "@/lib/cep";
import { cn } from "@/lib/utils";

export const UFS = [
  "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG",
  "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
] as const;

export const selectClass =
  "h-10 w-full rounded-[4px] border border-input bg-transparent px-3 text-sm outline-none focus-visible:border-primary focus-visible:ring-[3px] focus-visible:ring-primary/20 dark:bg-input/30";

export type EnderecoFormulario = {
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string;
  bairro: string;
  cidade: string;
  uf: string;
};

export const enderecoVazio = (): EnderecoFormulario => ({
  cep: "",
  logradouro: "",
  numero: "",
  complemento: "",
  bairro: "",
  cidade: "",
  uf: "",
});

export function enderecoDeApi(e?: {
  logradouro?: string | null;
  numero?: string | null;
  complemento?: string | null;
  bairro?: string | null;
  cidade?: string | null;
  uf?: string | null;
  cep?: string | null;
} | null): EnderecoFormulario {
  return {
    cep: formatarCep(e?.cep ?? ""),
    logradouro: e?.logradouro ?? "",
    numero: e?.numero ?? "",
    complemento: e?.complemento ?? "",
    bairro: e?.bairro ?? "",
    cidade: e?.cidade ?? "",
    uf: e?.uf ?? "",
  };
}

export function enderecoParaApi(e: EnderecoFormulario): {
  logradouro: string | null;
  numero: string | null;
  complemento: string | null;
  bairro: string | null;
  cidade: string | null;
  uf: string | null;
  cep: string | null;
} | null {
  const payload = {
    logradouro: e.logradouro.trim() || null,
    numero: e.numero.trim() || null,
    complemento: e.complemento.trim() || null,
    bairro: e.bairro.trim() || null,
    cidade: e.cidade.trim() || null,
    uf: e.uf.trim().toUpperCase() || null,
    cep: somenteDigitosCep(e.cep) || null,
  };
  return Object.values(payload).some(Boolean) ? payload : null;
}

export function CamposEndereco({
  value,
  onChange,
}: {
  value: EnderecoFormulario;
  onChange: (proximo: EnderecoFormulario) => void;
}) {
  const [consultando, setConsultando] = useState(false);
  const [erroCep, setErroCep] = useState<string | null>(null);

  async function consultar(digitos: string, atual: EnderecoFormulario) {
    if (digitos.length !== 8) return;
    setConsultando(true);
    setErroCep(null);
    try {
      const end = await bff.consultarCep(digitos);
      onChange({
        ...atual,
        cep: formatarCep(end.cep || digitos),
        logradouro: end.logradouro,
        bairro: end.bairro,
        cidade: end.cidade,
        uf: end.uf,
      });
    } catch (e) {
      setErroCep(e instanceof BffError || e instanceof Error ? e.message : "CEP não encontrado.");
    } finally {
      setConsultando(false);
    }
  }

  function aoCep(bruto: string) {
    const mascarado = formatarCep(bruto);
    const digitos = somenteDigitosCep(mascarado);
    const proximo = { ...value, cep: mascarado };
    onChange(proximo);
    if (digitos.length === 8) void consultar(digitos, proximo);
    else setErroCep(null);
  }

  return (
    <div className="grid gap-4 sm:grid-cols-6">
      <Campo rotulo="CEP" htmlFor="cep" classe="sm:col-span-2">
        <Input
          id="cep"
          name="cep"
          inputMode="numeric"
          placeholder="00000-000"
          maxLength={9}
          value={value.cep}
          onChange={(e) => aoCep(e.target.value)}
          onBlur={() => void consultar(somenteDigitosCep(value.cep), value)}
          className="h-10 rounded-[4px]"
          autoComplete="postal-code"
        />
        {consultando ? <p className="mt-1 text-xs text-muted-foreground">Consultando CEP…</p> : null}
        {erroCep ? (
          <p className="mt-1 text-xs text-[#721c24]" role="alert">
            {erroCep}
          </p>
        ) : (
          <p className="mt-1 text-xs text-muted-foreground">Preencha o CEP para buscar o endereço.</p>
        )}
      </Campo>
      <div className="hidden sm:block sm:col-span-4" />

      <Campo rotulo="Logradouro" htmlFor="logradouro" classe="sm:col-span-4">
        <Input
          id="logradouro"
          name="logradouro"
          value={value.logradouro}
          onChange={(e) => onChange({ ...value, logradouro: e.target.value })}
          className="h-10 rounded-[4px]"
        />
      </Campo>
      <Campo rotulo="Número" htmlFor="numero" classe="sm:col-span-2">
        <Input
          id="numero"
          name="numero"
          value={value.numero}
          onChange={(e) => onChange({ ...value, numero: e.target.value })}
          className="h-10 rounded-[4px]"
        />
      </Campo>
      <Campo rotulo="Complemento" htmlFor="complemento" classe="sm:col-span-3">
        <Input
          id="complemento"
          name="complemento"
          value={value.complemento}
          onChange={(e) => onChange({ ...value, complemento: e.target.value })}
          className="h-10 rounded-[4px]"
        />
      </Campo>
      <Campo rotulo="Bairro" htmlFor="bairro" classe="sm:col-span-3">
        <Input
          id="bairro"
          name="bairro"
          value={value.bairro}
          onChange={(e) => onChange({ ...value, bairro: e.target.value })}
          className="h-10 rounded-[4px]"
        />
      </Campo>
      <Campo rotulo="Cidade" htmlFor="cidade" classe="sm:col-span-3">
        <Input
          id="cidade"
          name="cidade"
          value={value.cidade}
          onChange={(e) => onChange({ ...value, cidade: e.target.value })}
          className="h-10 rounded-[4px]"
        />
      </Campo>
      <Campo rotulo="UF" htmlFor="uf" classe="sm:col-span-1">
        <select
          id="uf"
          name="uf"
          value={value.uf}
          onChange={(e) => onChange({ ...value, uf: e.target.value })}
          className={selectClass}
        >
          <option value="">—</option>
          {UFS.map((uf) => (
            <option key={uf} value={uf}>
              {uf}
            </option>
          ))}
        </select>
      </Campo>
    </div>
  );
}

function Campo({
  rotulo,
  htmlFor,
  children,
  classe,
}: {
  rotulo: string;
  htmlFor: string;
  children: ReactNode;
  classe?: string;
}) {
  return (
    <div className={cn("flex flex-col gap-1.5", classe)}>
      <Label htmlFor={htmlFor} className="text-[#333] dark:text-foreground">
        {rotulo}
      </Label>
      {children}
    </div>
  );
}
