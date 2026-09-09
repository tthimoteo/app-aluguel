"use client";

import Link from "next/link";
import { useMemo, useState, type FormEvent, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { CamposEndereco, enderecoParaApi, enderecoVazio, selectClass, type EnderecoFormulario } from "@/components/imoveis/campos-endereco";
import { Button, buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { bff, BffError, type NovoImovelInput } from "@/lib/api/browser";
import { filtrarClientesAcessiveis, type ClienteOpcao } from "@/lib/clientes-acesso";
import { cn } from "@/lib/utils";

const TIPOS: { valor: NovoImovelInput["tipo"]; rotulo: string }[] = [
  { valor: "Residencial", rotulo: "Residencial" },
  { valor: "Comercial", rotulo: "Comercial" },
  { valor: "Galpao", rotulo: "Galpão" },
  { valor: "Sala", rotulo: "Sala" },
  { valor: "Outro", rotulo: "Outro" },
];

export function FormularioImovel({
  clientes,
  clienteInicial,
}: {
  clientes: ClienteOpcao[];
  clienteInicial: string;
}) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [filtroCliente, setFiltroCliente] = useState("");
  const [clienteId, setClienteId] = useState(clienteInicial);
  const [endereco, setEndereco] = useState<EnderecoFormulario>(() => enderecoVazio());

  const opcoesCliente = useMemo(() => {
    const filtrados = filtrarClientesAcessiveis(clientes, filtroCliente);
    const atual = clientes.find((c) => c.id === clienteId);
    if (atual && !filtrados.some((c) => c.id === atual.id)) return [atual, ...filtrados];
    return filtrados;
  }, [clientes, filtroCliente, clienteId]);

  async function onSubmit(ev: FormEvent<HTMLFormElement>) {
    ev.preventDefault();
    setErro(null);
    const form = new FormData(ev.currentTarget);
    const tipoBruto = String(form.get("tipo") ?? "");
    const tipo = TIPOS.some((t) => t.valor === tipoBruto)
      ? (tipoBruto as NovoImovelInput["tipo"])
      : "Residencial";

    const dados: NovoImovelInput = {
      clienteId: String(form.get("clienteId") ?? clienteId),
      nome: String(form.get("nome") ?? "").trim(),
      tipo,
      numeroIptu: textoOuNulo(form.get("numeroIptu")),
      numeroMatricula: textoOuNulo(form.get("numeroMatricula")),
      endereco: enderecoParaApi(endereco),
    };

    setPending(true);
    try {
      await bff.criarImovel(dados);
      toast.success("Imóvel incluído.");
      router.push(`/imoveis?clienteId=${encodeURIComponent(dados.clienteId)}`);
      router.refresh();
    } catch (e) {
      setErro(
        e instanceof BffError || e instanceof Error
          ? e.message
          : "Não foi possível incluir o imóvel.",
      );
    } finally {
      setPending(false);
    }
  }

  return (
    <form
      onSubmit={(ev) => void onSubmit(ev)}
      className="max-w-3xl rounded-lg bg-card p-6 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border"
    >
      <div className="grid gap-4 sm:grid-cols-2">
        <Campo rotulo="Cliente" htmlFor="clienteId" classe="sm:col-span-2">
          {clientes.length > 1 ? (
            <Input
              type="search"
              value={filtroCliente}
              onChange={(e) => setFiltroCliente(e.target.value)}
              placeholder="Buscar cliente"
              className="mb-2 h-10 rounded-[4px]"
              aria-label="Buscar cliente"
            />
          ) : null}
          <select
            id="clienteId"
            name="clienteId"
            required
            value={clienteId}
            onChange={(e) => setClienteId(e.target.value)}
            className={selectClass}
            data-cliente-inicial={clienteInicial}
          >
            {opcoesCliente.map((c) => (
              <option key={c.id} value={c.id}>
                {c.nome}
                {c.detalhe ? ` (${c.detalhe})` : ""}
              </option>
            ))}
          </select>
        </Campo>

        <Campo rotulo="Nome" htmlFor="nome">
          <Input id="nome" name="nome" required maxLength={150} className="h-10 rounded-[4px]" />
        </Campo>
        <Campo rotulo="Tipo" htmlFor="tipo">
          <select id="tipo" name="tipo" required defaultValue="Residencial" className={selectClass}>
            {TIPOS.map((t) => (
              <option key={t.valor} value={t.valor}>
                {t.rotulo}
              </option>
            ))}
          </select>
        </Campo>
        <Campo rotulo="Nr. IPTU" htmlFor="numeroIptu">
          <Input id="numeroIptu" name="numeroIptu" maxLength={30} className="h-10 rounded-[4px]" />
        </Campo>
        <Campo rotulo="Nr. Matrícula" htmlFor="numeroMatricula">
          <Input id="numeroMatricula" name="numeroMatricula" maxLength={30} className="h-10 rounded-[4px]" />
        </Campo>
      </div>

      <h3 className="mt-6 mb-3 text-sm font-semibold">Endereço</h3>
      <CamposEndereco idPrefix="imovel" value={endereco} onChange={setEndereco} />

      {erro ? (
        <p className="mt-4 rounded-[4px] bg-[#f8d7da] px-3 py-2 text-sm text-[#721c24]" role="alert">
          {erro}
        </p>
      ) : null}

      <div className="mt-6 flex flex-wrap gap-2">
        <Button type="submit" disabled={pending} className="h-9 rounded-[4px] px-4">
          {pending ? "Salvando…" : "Salvar"}
        </Button>
        <Link
          href={clienteId ? `/imoveis?clienteId=${encodeURIComponent(clienteId)}` : "/imoveis"}
          className={cn(buttonVariants({ variant: "secondary" }), "h-9 rounded-[4px] bg-[#95A5A6] px-4 text-white hover:bg-[#7F8C8D]")}
        >
          Cancelar
        </Link>
      </div>
    </form>
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

function textoOuNulo(valor: FormDataEntryValue | null): string | null {
  const s = String(valor ?? "").trim();
  return s.length ? s : null;
}
