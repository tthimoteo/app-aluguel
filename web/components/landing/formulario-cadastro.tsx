"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useState, useTransition, type FormEvent, type ReactNode } from "react";
import { toast } from "sonner";
import { Logo } from "@/components/brand/logo";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { Plano } from "@/lib/api/types";
import { mascaraCpfCnpj, mascaraTelefone } from "@/lib/format";
import { beneficiosDoPlano, precoRotulo } from "@/lib/planos";
import { cn } from "@/lib/utils";

type Props = {
  planos: Plano[];
  planoInicialId?: string | null;
  modoInicial?: "trial" | "assinar";
};

export function FormularioCadastro({ planos, planoInicialId, modoInicial = "trial" }: Props) {
  const router = useRouter();
  const pagos = useMemo(
    () =>
      planos
        .filter((p) => p.codigo !== "TRIAL" && p.valorMensal != null)
        .sort((a, b) => (a.valorMensal ?? 0) - (b.valorMensal ?? 0)),
    [planos],
  );

  const [passo, setPasso] = useState<1 | 2>(modoInicial === "assinar" ? 2 : 1);
  const [nome, setNome] = useState("");
  const [email, setEmail] = useState("");
  const [telefone, setTelefone] = useState("");
  const [cpf, setCpf] = useState("");
  const [senha, setSenha] = useState("");
  const [planoId, setPlanoId] = useState<string | null>(
    planoInicialId && pagos.some((p) => p.id === planoInicialId) ? planoInicialId : null,
  );
  const [erro, setErro] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function validarPasso1(): boolean {
    if (nome.trim().length < 3) {
      setErro("Informe o nome completo.");
      return false;
    }
    if (!email.includes("@")) {
      setErro("Informe um e-mail válido.");
      return false;
    }
    if (telefone.replace(/\D/g, "").length < 10) {
      setErro("Informe um telefone com DDD.");
      return false;
    }
    if (cpf.replace(/\D/g, "").length !== 11) {
      setErro("Informe um CPF válido.");
      return false;
    }
    if (senha.length < 10) {
      setErro("A senha deve ter pelo menos 10 caracteres.");
      return false;
    }
    setErro(null);
    return true;
  }

  function avancarParaPlanos(e: FormEvent) {
    e.preventDefault();
    if (!validarPasso1()) return;
    setPasso(2);
  }

  async function enviar(comPlano: boolean) {
    if (!validarPasso1()) {
      setPasso(1);
      return;
    }
    if (comPlano && !planoId) {
      setErro("Selecione um plano para assinar.");
      return;
    }

    setErro(null);
    startTransition(async () => {
      try {
        const res = await fetch("/api/cadastro-publico", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            nome: nome.trim(),
            email: email.trim(),
            telefone,
            cpf,
            senha,
            planoId: comPlano ? planoId : null,
          }),
        });
        const body = (await res.json().catch(() => null)) as
          | { erro?: string; mensagem?: string; pagamentoId?: string | null; operacao?: string }
          | null;
        if (!res.ok) {
          setErro(body?.erro ?? "Não foi possível concluir o cadastro.");
          return;
        }
        toast.success(body?.mensagem ?? "Conta criada.");
        if (body?.pagamentoId) {
          router.push(`/minha-conta/pagamento-pix/${body.pagamentoId}`);
        } else {
          router.push("/inicio");
        }
        router.refresh();
      } catch {
        setErro("Falha de rede ao cadastrar. Tente novamente.");
      }
    });
  }

  return (
    <div className="mx-auto grid min-h-svh max-w-5xl gap-8 px-4 py-10 md:grid-cols-[0.9fr_1.1fr] md:items-start md:px-6">
      <aside className="rounded-[8px] bg-[#003366] p-6 text-white shadow-[0_10px_25px_rgba(0,0,0,0.2)] md:sticky md:top-8">
        <Link href="/" className="inline-block text-white">
          <Logo />
        </Link>
        <h1 className="mt-6 text-2xl font-bold leading-tight">
          {passo === 1 ? "Crie sua conta" : "Escolha como começar"}
        </h1>
        <p className="mt-3 text-sm text-white/80">
          Dados básicos para liberar o acesso. Assinatura paga gera PIX imediato (Banco Inter /
          TTHIMOTEO). Trial libera 7 dias sem cobrança.
        </p>
        <ol className="mt-6 space-y-2 text-sm">
          <li className={cn("font-semibold", passo === 1 ? "text-[#f4a574]" : "text-white/70")}>
            1. Dados da conta
          </li>
          <li className={cn("font-semibold", passo === 2 ? "text-[#f4a574]" : "text-white/70")}>
            2. Plano e pagamento
          </li>
        </ol>
        <p className="mt-8 text-sm text-white/70">
          Já tem conta?{" "}
          <Link href="/login" className="font-semibold text-white underline">
            Entrar
          </Link>
        </p>
      </aside>

      <div className="rounded-[8px] border border-[#e9ecef] bg-white p-6 shadow-[0_2px_8px_rgba(0,0,0,0.06)] md:p-8">
        {passo === 1 ? (
          <form onSubmit={avancarParaPlanos} className="space-y-4">
            <Campo label="Nome completo" htmlFor="nome">
              <Input
                id="nome"
                value={nome}
                onChange={(e) => setNome(e.target.value)}
                required
                className="h-11 rounded-[4px]"
              />
            </Campo>
            <Campo label="E-mail" htmlFor="email">
              <Input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                className="h-11 rounded-[4px]"
              />
            </Campo>
            <div className="grid gap-4 sm:grid-cols-2">
              <Campo label="Telefone" htmlFor="telefone">
                <Input
                  id="telefone"
                  value={telefone}
                  onChange={(e) => setTelefone(mascaraTelefone(e.target.value))}
                  required
                  className="h-11 rounded-[4px]"
                />
              </Campo>
              <Campo label="CPF" htmlFor="cpf">
                <Input
                  id="cpf"
                  value={cpf}
                  onChange={(e) => setCpf(mascaraCpfCnpj(e.target.value))}
                  required
                  className="h-11 rounded-[4px]"
                />
              </Campo>
            </div>
            <Campo label="Senha (mín. 10 caracteres)" htmlFor="senha">
              <Input
                id="senha"
                type="password"
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                required
                minLength={10}
                className="h-11 rounded-[4px]"
              />
            </Campo>
            {erro ? (
              <p className="rounded-[4px] bg-[#f8d7da] px-3 py-2 text-sm text-[#721c24]" role="alert">
                {erro}
              </p>
            ) : null}
            <Button type="submit" className="h-11 w-full rounded-[4px] text-base">
              Continuar
            </Button>
          </form>
        ) : (
          <div className="space-y-5">
            <button
              type="button"
              className="text-sm font-semibold text-[#e85d15] hover:underline"
              onClick={() => setPasso(1)}
            >
              ← Voltar aos dados
            </button>
            <div className="rounded-[8px] border border-[#e9ecef] bg-[#f8f9fa] p-4">
              <p className="font-bold text-[#2c3e50]">Trial gratuito · 7 dias</p>
              <p className="mt-1 text-sm text-[#6c757d]">
                Explore a plataforma sem cobrança. NFS-e e boleto seguem as regras do plano Trial.
              </p>
              <Button
                type="button"
                disabled={pending}
                className="mt-4 h-10 rounded-[4px]"
                onClick={() => void enviar(false)}
              >
                {pending ? "Criando conta..." : "Começar Trial gratuito"}
              </Button>
            </div>

            <div>
              <p className="mb-3 font-bold text-[#2c3e50]">Ou assine agora via PIX</p>
              <div className="grid gap-3">
                {pagos.map((plano) => {
                  const ativo = planoId === plano.id;
                  return (
                    <button
                      key={plano.id}
                      type="button"
                      onClick={() => setPlanoId(plano.id)}
                      className={cn(
                        "rounded-[8px] border p-4 text-left transition",
                        ativo
                          ? "border-[#e85d15] bg-[#fff6f0] shadow-[0_2px_8px_rgba(232,93,21,0.15)]"
                          : "border-[#e9ecef] bg-white hover:border-[#e85d15]/50",
                      )}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="font-bold text-[#2c3e50]">{plano.nome}</p>
                          <p className="text-sm text-[#e85d15]">{precoRotulo(plano)}</p>
                        </div>
                        <span
                          className={cn(
                            "mt-1 size-4 rounded-full border-2",
                            ativo ? "border-[#e85d15] bg-[#e85d15]" : "border-[#ced4da]",
                          )}
                        />
                      </div>
                      <ul className="mt-2 space-y-1 text-xs text-[#6c757d]">
                        {beneficiosDoPlano(plano)
                          .slice(0, 3)
                          .map((b) => (
                            <li key={b}>• {b}</li>
                          ))}
                      </ul>
                    </button>
                  );
                })}
              </div>
              {erro ? (
                <p className="mt-3 rounded-[4px] bg-[#f8d7da] px-3 py-2 text-sm text-[#721c24]" role="alert">
                  {erro}
                </p>
              ) : null}
              <Button
                type="button"
                disabled={pending || !planoId}
                className="mt-4 h-11 w-full rounded-[4px] text-base"
                onClick={() => void enviar(true)}
              >
                {pending ? "Gerando PIX..." : "Assinar e pagar com PIX"}
              </Button>
              <p className="mt-3 text-center text-sm text-[#6c757d]">
                Plano Pro?{" "}
                <Link href="/demonstracao" className="font-semibold text-[#e85d15] hover:underline">
                  Solicitar demonstração
                </Link>
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function Campo({
  label,
  htmlFor,
  children,
}: {
  label: string;
  htmlFor: string;
  children: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={htmlFor} className="text-[#333]">
        {label}
      </Label>
      {children}
    </div>
  );
}
