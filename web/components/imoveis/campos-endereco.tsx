"use client";

import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { Label } from "@/components/ui/label";
import { buscarEnderecoPorCep, formatarCep, somenteDigitosCep } from "@/lib/cep";
import { cn } from "@/lib/utils";

export const UFS = [
  "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG",
  "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
] as const;

export const selectClass =
  "h-10 w-full rounded-[4px] border border-input bg-transparent px-3 text-sm outline-none focus-visible:border-primary focus-visible:ring-[3px] focus-visible:ring-primary/20 dark:bg-input/30";

const fieldInputClass =
  "h-10 w-full min-w-0 rounded-[4px] border border-input bg-transparent px-3 py-2 text-base outline-none focus-visible:border-primary focus-visible:ring-[3px] focus-visible:ring-primary/20 md:text-sm";

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

export function enderecoObrigatorioPreenchido(e: EnderecoFormulario): boolean {
  return (
    somenteDigitosCep(e.cep).length === 8 &&
    e.logradouro.trim().length > 0 &&
    e.numero.trim().length > 0 &&
    e.bairro.trim().length > 0 &&
    e.cidade.trim().length > 0 &&
    e.uf.trim().length === 2
  );
}

export function CamposEndereco({
  value,
  onChange,
  idPrefix,
  obrigatorio = false,
}: {
  value: EnderecoFormulario;
  onChange: (proximo: EnderecoFormulario) => void;
  idPrefix?: string;
  obrigatorio?: boolean;
}) {
  const autoId = useId();
  const prefixo = idPrefix ?? autoId.replace(/:/g, "");
  const campoId = (nome: string) => `${prefixo}-${nome}`;
  const [consultando, setConsultando] = useState(false);
  const [erroCep, setErroCep] = useState<string | null>(null);
  const pedido = useRef(0);
  const abortRef = useRef<AbortController | null>(null);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const valueRef = useRef(value);
  valueRef.current = value;

  useEffect(() => {
    return () => {
      abortRef.current?.abort();
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, []);

  async function consultar(digitos: string) {
    if (digitos.length !== 8) return;
    abortRef.current?.abort();
    const ac = new AbortController();
    abortRef.current = ac;
    const seq = ++pedido.current;
    setConsultando(true);
    setErroCep(null);
    try {
      const end = await buscarEnderecoPorCep(digitos, ac.signal);
      if (seq !== pedido.current) return;
      const atual = valueRef.current;
      onChange({
        ...atual,
        cep: formatarCep(end.cep || digitos),
        logradouro: end.logradouro || atual.logradouro,
        bairro: end.bairro || atual.bairro,
        cidade: end.cidade,
        uf: end.uf,
      });
    } catch (e) {
      if (ac.signal.aborted) return;
      if (seq !== pedido.current) return;
      setErroCep(e instanceof Error ? e.message : "CEP não encontrado.");
    } finally {
      if (seq === pedido.current) setConsultando(false);
    }
  }

  function aoCep(bruto: string) {
    const mascarado = formatarCep(String(bruto ?? ""));
    const digitos = somenteDigitosCep(mascarado);
    onChange({ ...value, cep: mascarado });
    if (timerRef.current) clearTimeout(timerRef.current);
    if (digitos.length !== 8) {
      abortRef.current?.abort();
      setConsultando(false);
      setErroCep(null);
      return;
    }
    timerRef.current = setTimeout(() => {
      void consultar(digitos);
    }, 280);
  }

  return (
    <div className="flex flex-col gap-4">
      <Campo rotulo="CEP" htmlFor={campoId("cep")} classe="w-full sm:max-w-[12rem]" obrigatorio={obrigatorio}>
        <input
          id={campoId("cep")}
          name="cep"
          inputMode="numeric"
          placeholder="00000-000"
          maxLength={9}
          value={value.cep}
          required={obrigatorio}
          onChange={(e) => aoCep(e.target.value)}
          onBlur={() => {
            const d = somenteDigitosCep(valueRef.current.cep);
            if (d.length === 8) void consultar(d);
          }}
          onKeyDown={(e) => {
            if (e.key === "Enter") e.preventDefault();
          }}
          className={fieldInputClass}
          autoComplete="postal-code"
          data-campo="cep"
        />
        {consultando ? <p className="mt-1 text-xs text-muted-foreground">Consultando CEP…</p> : null}
        {erroCep ? (
          <p className="mt-1 text-xs text-[#721c24]" role="alert">
            {erroCep}
          </p>
        ) : (
          <p className="mt-1 text-xs text-muted-foreground">
            Informe o CEP para preencher logradouro, bairro, cidade e UF.
          </p>
        )}
      </Campo>

      <div className="grid gap-4 sm:grid-cols-6">
        <Campo rotulo="Logradouro" htmlFor={campoId("logradouro")} classe="sm:col-span-4" obrigatorio={obrigatorio}>
          <input
            id={campoId("logradouro")}
            name="logradouro"
            value={value.logradouro}
            required={obrigatorio}
            onChange={(e) => onChange({ ...value, logradouro: e.target.value })}
            className={fieldInputClass}
            autoComplete="address-line1"
          />
        </Campo>
        <Campo rotulo="Número" htmlFor={campoId("numero")} classe="sm:col-span-2" obrigatorio={obrigatorio}>
          <input
            id={campoId("numero")}
            name="numero"
            value={value.numero}
            required={obrigatorio}
            onChange={(e) => onChange({ ...value, numero: e.target.value })}
            className={fieldInputClass}
            autoComplete="address-line2"
          />
        </Campo>
        <Campo rotulo="Complemento" htmlFor={campoId("complemento")} classe="sm:col-span-3">
          <input
            id={campoId("complemento")}
            name="complemento"
            value={value.complemento}
            onChange={(e) => onChange({ ...value, complemento: e.target.value })}
            className={fieldInputClass}
          />
        </Campo>
        <Campo rotulo="Bairro" htmlFor={campoId("bairro")} classe="sm:col-span-3" obrigatorio={obrigatorio}>
          <input
            id={campoId("bairro")}
            name="bairro"
            value={value.bairro}
            required={obrigatorio}
            onChange={(e) => onChange({ ...value, bairro: e.target.value })}
            className={fieldInputClass}
          />
        </Campo>
        <Campo rotulo="Cidade" htmlFor={campoId("cidade")} classe="sm:col-span-4" obrigatorio={obrigatorio}>
          <input
            id={campoId("cidade")}
            name="cidade"
            value={value.cidade}
            required={obrigatorio}
            onChange={(e) => onChange({ ...value, cidade: e.target.value })}
            className={fieldInputClass}
            autoComplete="address-level2"
          />
        </Campo>
        <Campo rotulo="UF" htmlFor={campoId("uf")} classe="sm:col-span-2" obrigatorio={obrigatorio}>
          <select
            id={campoId("uf")}
            name="uf"
            value={value.uf}
            required={obrigatorio}
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
    </div>
  );
}

function Campo({
  rotulo,
  htmlFor,
  children,
  classe,
  obrigatorio,
}: {
  rotulo: string;
  htmlFor: string;
  children: ReactNode;
  classe?: string;
  obrigatorio?: boolean;
}) {
  return (
    <div className={cn("flex flex-col gap-1.5", classe)}>
      <Label htmlFor={htmlFor} className="text-[#333] dark:text-foreground">
        {rotulo}
        {obrigatorio ? <span className="text-primary"> *</span> : null}
      </Label>
      {children}
    </div>
  );
}
