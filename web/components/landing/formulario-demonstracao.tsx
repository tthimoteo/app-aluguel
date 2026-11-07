"use client";

import Link from "next/link";
import { useState, useTransition, type FormEvent } from "react";
import { toast } from "sonner";
import { Logo } from "@/components/brand/logo";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { mascaraTelefone } from "@/lib/format";

export function FormularioDemonstracao() {
  const [nome, setNome] = useState("");
  const [email, setEmail] = useState("");
  const [telefone, setTelefone] = useState("");
  const [mensagem, setMensagem] = useState("");
  const [erro, setErro] = useState<string | null>(null);
  const [enviado, setEnviado] = useState(false);
  const [pending, startTransition] = useTransition();

  function onSubmit(e: FormEvent) {
    e.preventDefault();
    setErro(null);
    startTransition(async () => {
      try {
        const res = await fetch("/api/cadastro-publico/demonstracao", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ nome, email, telefone, mensagem }),
        });
        const body = (await res.json().catch(() => null)) as { erro?: string; mensagem?: string } | null;
        if (!res.ok) {
          setErro(body?.erro ?? "Não foi possível enviar.");
          return;
        }
        setEnviado(true);
        toast.success(body?.mensagem ?? "Solicitação enviada.");
      } catch {
        setErro("Falha de rede. Tente novamente.");
      }
    });
  }

  return (
    <div className="mx-auto flex min-h-svh max-w-lg flex-col justify-center px-4 py-10">
      <div className="rounded-[8px] border border-[#e9ecef] bg-white p-6 shadow-[0_2px_8px_rgba(0,0,0,0.06)] md:p-8">
        <Link href="/" className="inline-block text-[#2c3e50]">
          <Logo />
        </Link>
        <h1 className="mt-6 text-2xl font-bold text-[#003366]">Solicitar demonstração · Plano Pro</h1>
        <p className="mt-2 text-sm text-[#6c757d]">
          Sem limites de utilização. Nossa equipe apresenta a solução e as condições comerciais.
        </p>

        {enviado ? (
          <div className="mt-6 rounded-[8px] bg-[#d4edda] px-4 py-3 text-sm text-[#155724]">
            Recebemos seu pedido. Em breve a Lucrare entrará em contato.
            <div className="mt-4">
              <Link href="/" className="font-semibold text-[#e85d15] hover:underline">
                Voltar à página inicial
              </Link>
            </div>
          </div>
        ) : (
          <form onSubmit={onSubmit} className="mt-6 space-y-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="nome">Nome</Label>
              <Input
                id="nome"
                required
                value={nome}
                onChange={(e) => setNome(e.target.value)}
                className="h-11 rounded-[4px]"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="email">E-mail</Label>
              <Input
                id="email"
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="h-11 rounded-[4px]"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="telefone">Telefone</Label>
              <Input
                id="telefone"
                required
                value={telefone}
                onChange={(e) => setTelefone(mascaraTelefone(e.target.value))}
                className="h-11 rounded-[4px]"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="mensagem">Mensagem (opcional)</Label>
              <textarea
                id="mensagem"
                value={mensagem}
                onChange={(e) => setMensagem(e.target.value)}
                rows={4}
                className="rounded-[4px] border border-input bg-transparent px-3 py-2 text-sm outline-none focus-visible:border-primary focus-visible:ring-[3px] focus-visible:ring-primary/20"
              />
            </div>
            {erro ? (
              <p className="rounded-[4px] bg-[#f8d7da] px-3 py-2 text-sm text-[#721c24]" role="alert">
                {erro}
              </p>
            ) : null}
            <Button type="submit" disabled={pending} className="h-11 w-full rounded-[4px] text-base">
              {pending ? "Enviando..." : "Solicitar Demonstração"}
            </Button>
          </form>
        )}
      </div>
    </div>
  );
}
