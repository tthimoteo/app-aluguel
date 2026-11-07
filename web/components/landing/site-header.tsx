"use client";

import Link from "next/link";
import { useState } from "react";
import { Menu, X } from "lucide-react";
import { Logo } from "@/components/brand/logo";
import { cn } from "@/lib/utils";

const LINKS = [
  { href: "/#dor", label: "Desafios" },
  { href: "/#solucao", label: "Solução" },
  { href: "/#reforma", label: "Reforma Tributária" },
  { href: "/#planos", label: "Planos" },
  { href: "/#faq", label: "FAQ" },
];

export function SiteHeader() {
  const [aberto, setAberto] = useState(false);

  return (
    <header className="sticky top-0 z-40 border-b border-[#e9ecef]/90 bg-white/95 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-4 px-4 md:px-6">
        <Link href="/" className="shrink-0 text-[#2c3e50]" onClick={() => setAberto(false)}>
          <Logo />
        </Link>
        <nav className="hidden items-center gap-6 lg:flex">
          {LINKS.map((l) => (
            <a
              key={l.href}
              href={l.href}
              className="text-sm font-semibold text-[#e85d15] transition hover:text-[#c55a32]"
            >
              {l.label}
            </a>
          ))}
        </nav>
        <div className="hidden items-center gap-2 sm:flex">
          <Link
            href="/login"
            className="rounded-[4px] px-3 py-2 text-sm font-semibold text-[#2c3e50] hover:bg-[#f5f5f5]"
          >
            Entrar
          </Link>
          <Link
            href="/cadastro"
            className="rounded-[4px] bg-[#e85d15] px-3.5 py-2 text-sm font-bold text-white shadow-[0_2px_8px_rgba(232,93,21,0.25)] transition hover:bg-[#c55a32]"
          >
            Testar grátis
          </Link>
        </div>
        <button
          type="button"
          className="inline-flex size-10 items-center justify-center rounded-[4px] text-[#2c3e50] lg:hidden"
          aria-label={aberto ? "Fechar menu" : "Abrir menu"}
          onClick={() => setAberto((v) => !v)}
        >
          {aberto ? <X className="size-5" /> : <Menu className="size-5" />}
        </button>
      </div>
      <div
        className={cn(
          "border-t border-[#e9ecef] bg-white lg:hidden",
          aberto ? "block" : "hidden",
        )}
      >
        <div className="mx-auto flex max-w-6xl flex-col gap-1 px-4 py-3">
          {LINKS.map((l) => (
            <a
              key={l.href}
              href={l.href}
              className="rounded-[4px] px-3 py-2.5 text-sm font-semibold text-[#e85d15]"
              onClick={() => setAberto(false)}
            >
              {l.label}
            </a>
          ))}
          <Link
            href="/login"
            className="rounded-[4px] px-3 py-2.5 text-sm font-semibold text-[#2c3e50]"
            onClick={() => setAberto(false)}
          >
            Entrar
          </Link>
          <Link
            href="/cadastro"
            className="mt-1 rounded-[4px] bg-[#e85d15] px-3 py-2.5 text-center text-sm font-bold text-white"
            onClick={() => setAberto(false)}
          >
            Testar grátis
          </Link>
        </div>
      </div>
    </header>
  );
}
