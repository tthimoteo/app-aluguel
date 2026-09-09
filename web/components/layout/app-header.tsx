"use client";

import { usePathname } from "next/navigation";
import { MobileSidebar } from "@/components/layout/mobile-sidebar";
import { ThemeToggle } from "@/components/theme-toggle";
import { UserMenu } from "@/components/layout/user-menu";
import type { UsuarioAutenticado, VinculoCliente } from "@/lib/api/types";
import type { NavItem } from "@/lib/navigation";
import { tituloDaRota } from "@/lib/navigation";

export function AppHeader({
  usuario,
  itens,
  clientes = [],
}: {
  usuario: UsuarioAutenticado;
  itens: NavItem[];
  clientes?: VinculoCliente[];
}) {
  const pathname = usePathname();
  const titulo = tituloDaRota(pathname);

  return (
    <header className="flex h-14 shrink-0 items-center justify-between gap-3 bg-secondary px-3 text-secondary-foreground shadow-[0_2px_4px_rgba(0,0,0,0.1)] md:px-6">
      <div className="flex min-w-0 items-center gap-2">
        <MobileSidebar itens={itens} />
        <h1 className="truncate text-base font-semibold tracking-tight md:text-lg">{titulo}</h1>
      </div>
      <div className="flex items-center gap-1">
        <ThemeToggle />
        <UserMenu usuario={usuario} clientes={clientes} />
      </div>
    </header>
  );
}
