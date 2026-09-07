"use client";

import { useRouter } from "next/navigation";
import { LogOut } from "lucide-react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { UsuarioAutenticado } from "@/lib/api/types";
import { iniciais } from "@/lib/format";

export function UserMenu({ usuario }: { usuario: UsuarioAutenticado }) {
  const router = useRouter();
  const perfil = usuario.roles[0] ?? "Usuário";

  async function sair() {
    await fetch("/api/auth/logout", { method: "POST" });
    router.push("/login");
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
        <DropdownMenuLabel>
          <div className="flex flex-col gap-0.5">
            <span className="text-foreground">{usuario.nome}</span>
            <span className="font-normal">{usuario.email}</span>
            <span className="font-normal text-primary">{perfil}</span>
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem variant="destructive" onClick={() => void sair()}>
          <LogOut />
          Sair
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
