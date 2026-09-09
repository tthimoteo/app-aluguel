"use client";

import { useRouter } from "next/navigation";
import { Building2, Check, LogOut } from "lucide-react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { bff } from "@/lib/api/browser";
import type { UsuarioAutenticado, VinculoCliente } from "@/lib/api/types";
import { iniciais } from "@/lib/format";

export function UserMenu({
  usuario,
  clientes = [],
}: {
  usuario: UsuarioAutenticado;
  clientes?: VinculoCliente[];
}) {
  const router = useRouter();
  const perfil = usuario.roles[0] ?? "Usuário";
  const atual = clientes.find((c) => c.clienteId === usuario.clienteId);

  async function sair() {
    await fetch("/api/auth/logout", { method: "POST" });
    router.push("/login");
    router.refresh();
  }

  async function trocarCliente(clienteId: string) {
    if (clienteId === usuario.clienteId) return;
    await bff.selecionarCliente(clienteId);
    router.refresh();
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={
          <Button
            type="button"
            variant="ghost"
            className="h-9 gap-2 rounded-[4px] px-2 text-white hover:bg-white/10 hover:text-white"
          />
        }
      >
        <Avatar size="sm" className="bg-primary text-primary-foreground">
          <AvatarFallback className="bg-primary text-[10px] font-semibold text-white">
            {iniciais(usuario.nome)}
          </AvatarFallback>
        </Avatar>
        <span className="hidden max-w-[140px] truncate text-left text-sm font-medium sm:block">
          {usuario.nome}
        </span>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="min-w-56">
        <DropdownMenuGroup>
          <DropdownMenuLabel>
            <div className="flex flex-col gap-0.5">
              <span className="text-foreground">{usuario.nome}</span>
              <span className="font-normal">{usuario.email}</span>
              <span className="font-normal text-primary">
                {atual ? `${perfil} · ${atual.nome}` : perfil}
              </span>
            </div>
          </DropdownMenuLabel>
        </DropdownMenuGroup>
        {clientes.length > 1 ? (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuGroup>
              <DropdownMenuLabel>Atuar como</DropdownMenuLabel>
              {clientes.map((c) => (
                <DropdownMenuItem key={c.clienteId} onClick={() => void trocarCliente(c.clienteId)}>
                  <Building2 />
                  <span className="flex min-w-0 flex-1 flex-col">
                    <span className="truncate">{c.nome}</span>
                    <span className="text-xs font-normal text-muted-foreground">{c.perfil}</span>
                  </span>
                  {c.clienteId === usuario.clienteId ? <Check className="text-primary" /> : null}
                </DropdownMenuItem>
              ))}
            </DropdownMenuGroup>
          </>
        ) : null}
        <DropdownMenuSeparator />
        <DropdownMenuItem variant="destructive" onClick={() => void sair()}>
          <LogOut />
          Sair
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
