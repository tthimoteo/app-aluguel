"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useState, useTransition } from "react";
import { ThemeToggle } from "@/components/theme-toggle";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [erro, setErro] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function destinoSeguro(): string {
    const from = searchParams.get("from");
    if (from && from.startsWith("/") && !from.startsWith("//") && !from.startsWith("/login")) {
      return from;
    }
    return "/";
  }

  async function onSubmit(formData: FormData) {
    setErro(null);
    const email = String(formData.get("email") ?? "").trim();
    const senha = String(formData.get("senha") ?? "");

    const res = await fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, senha }),
    });

    if (!res.ok) {
      const body = (await res.json().catch(() => null)) as { erro?: string } | null;
      setErro(body?.erro ?? "Não foi possível entrar.");
      return;
    }

    startTransition(() => {
      router.push(destinoSeguro());
      router.refresh();
    });
  }

  return (
    <div className="relative flex min-h-svh items-center justify-center bg-[#f5f5f5] p-4 dark:bg-background">
      <div className="absolute top-4 right-4">
        <ThemeToggle className="text-foreground hover:bg-muted" />
      </div>
      <div className="w-full max-w-[400px] rounded-lg bg-card p-8 shadow-[0_4px_6px_rgba(0,0,0,0.1)] ring-1 ring-border">
        <h1 className="mb-6 text-center text-xl font-semibold text-secondary dark:text-foreground">Entrar</h1>
        <form
          action={(formData) => {
            void onSubmit(formData);
          }}
          className="flex flex-col gap-4"
        >
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="email" className="text-[#333] dark:text-foreground">
              E-mail
            </Label>
            <Input
              id="email"
              name="email"
              type="email"
              autoComplete="username"
              required
              placeholder="ethan.b@example.com"
              className="h-11 rounded-[4px] px-3 text-base focus-visible:border-primary focus-visible:ring-[3px] focus-visible:ring-primary/20"
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="senha" className="text-[#333] dark:text-foreground">
              Senha
            </Label>
            <Input
              id="senha"
              name="senha"
              type="password"
              autoComplete="current-password"
              required
              className="h-11 rounded-[4px] px-3 text-base focus-visible:border-primary focus-visible:ring-[3px] focus-visible:ring-primary/20"
            />
          </div>
          {erro ? (
            <p className="rounded-[4px] bg-[#f8d7da] px-3 py-2 text-sm text-[#721c24]" role="alert">
              {erro}
            </p>
          ) : null}
          <Button
            type="submit"
            disabled={pending}
            className="h-11 w-full rounded-[4px] text-base disabled:bg-[#bdc3c7] disabled:text-white"
          >
            {pending ? "Entrando..." : "Entrar"}
          </Button>
        </form>
        {process.env.NODE_ENV === "development" ? (
          <p className="mt-6 text-center text-xs text-muted-foreground">
            Demo: <span className="font-medium">gestor@demo.local</span> / Gestor@123456
          </p>
        ) : null}
      </div>
    </div>
  );
}
