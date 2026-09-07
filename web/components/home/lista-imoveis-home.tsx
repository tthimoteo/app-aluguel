import { Building2 } from "lucide-react";
import { EmptyState } from "@/components/data/empty-state";
import { StatusBadge } from "@/components/data/status-badge";
import type { Imovel } from "@/lib/api/types";
import { formatarEndereco } from "@/lib/format";
import Link from "next/link";

export function ListaImoveisHome({ imoveis }: { imoveis: { itens: Imovel[]; total: number } }) {
  return (
    <section>
      <div className="mb-4 flex items-center justify-between">
        <h3 className="text-sm font-semibold">Imóveis</h3>
        <Link href="/imoveis" className="text-sm font-medium text-primary hover:text-primary/80">
          Ver todos
        </Link>
      </div>

      {imoveis.itens.length === 0 ? (
        <EmptyState mensagem="Nenhum imóvel cadastrado. Inclua o primeiro imóvel para começar." />
      ) : (
        <ul className="grid gap-3">
          {imoveis.itens.map((imovel) => (
            <li
              key={imovel.id}
              className="flex flex-col gap-3 rounded-lg bg-card p-4 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border sm:flex-row sm:items-center sm:justify-between"
            >
              <div className="flex items-start gap-3">
                <div className="flex size-10 items-center justify-center rounded-[4px] bg-primary/10 text-primary">
                  <Building2 className="size-5" />
                </div>
                <div>
                  <p className="font-semibold">{imovel.nome}</p>
                  <p className="text-sm text-muted-foreground">{formatarEndereco(imovel.endereco)}</p>
                  <p className="mt-1 text-xs text-muted-foreground">{imovel.tipo}</p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <StatusBadge status={imovel.status} />
                <span className="hidden text-xs text-muted-foreground sm:inline">
                  NFS-e e pagamentos em breve
                </span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
