import Link from "next/link";
import { EmptyState } from "@/components/data/empty-state";
import type { ClienteOpcao } from "@/lib/clientes-acesso";

export function SeletorCliente({
  clientes,
  hrefBase,
  vazio = "Nenhum cliente encontrado.",
}: {
  clientes: ClienteOpcao[];
  hrefBase: string;
  vazio?: string;
}) {
  if (clientes.length === 0) {
    return <EmptyState mensagem={vazio} />;
  }

  return (
    <ul className="grid gap-3">
      {clientes.map((c) => (
        <li key={c.id}>
          <Link
            href={`${hrefBase}?clienteId=${c.id}`}
            className="block rounded-lg bg-card p-4 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border transition-transform hover:-translate-y-0.5"
          >
            <p className="font-semibold">{c.nome}</p>
            {c.detalhe ? <p className="text-sm text-muted-foreground">{c.detalhe}</p> : null}
          </Link>
        </li>
      ))}
    </ul>
  );
}
