import { IndicadorCard } from "@/components/data/data-list";
import { formatarMoeda } from "@/lib/format";
import { cn } from "@/lib/utils";

export type IndicadorItem = {
  titulo: string;
  valor: string | number;
  detalhe?: string;
  destaque?: "danger" | "ok" | "muted";
};

export function IndicadoresGrid({
  itens,
  className,
}: {
  itens: IndicadorItem[];
  className?: string;
}) {
  return (
    <div className={cn("grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4", className)}>
      {itens.map((item) => (
        <IndicadorCard
          key={item.titulo}
          titulo={item.titulo}
          valor={item.valor}
          detalhe={item.detalhe}
          className={
            item.destaque === "danger"
              ? "ring-destructive/40"
              : item.destaque === "ok"
                ? "ring-emerald-500/30"
                : undefined
          }
        />
      ))}
    </div>
  );
}

export function moeda(v: number) {
  return formatarMoeda(v);
}

export function pct(v: number) {
  return `${v.toLocaleString("pt-BR", { maximumFractionDigits: 2 })}%`;
}
