import { cn } from "@/lib/utils";

export function DataList({
  desktop,
  mobile,
}: {
  desktop: React.ReactNode;
  mobile: React.ReactNode;
}) {
  return (
    <>
      <div className="hidden md:block">{desktop}</div>
      <div className="grid gap-3 md:hidden">{mobile}</div>
    </>
  );
}

export function DesktopTable({ children }: { children: React.ReactNode }) {
  return (
    <div className="overflow-hidden rounded-lg bg-card shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
      {children}
    </div>
  );
}

export function MobileCard({ children }: { children: React.ReactNode }) {
  return (
    <article className="rounded-xl border border-border bg-card p-4 shadow-[0_2px_8px_rgba(0,0,0,0.1)] transition-[transform,box-shadow] duration-200 hover:-translate-y-0.5 hover:shadow-[0_4px_12px_rgba(0,0,0,0.15)]">
      {children}
    </article>
  );
}

export function CardField({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-3 border-b border-muted py-2 last:border-0">
      <span className="min-w-20 text-sm font-semibold text-[#495057] dark:text-foreground">{label}</span>
      <span className="text-right text-sm text-muted-foreground">{children}</span>
    </div>
  );
}

export function IndicadorCard({
  titulo,
  valor,
  detalhe,
  className,
}: {
  titulo: string;
  valor: string | number;
  detalhe?: string;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "rounded-lg bg-card p-4 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border",
        className,
      )}
    >
      <p className="text-xs font-medium tracking-wide text-muted-foreground uppercase">{titulo}</p>
      <p className="mt-1 text-2xl font-bold text-foreground">{valor}</p>
      {detalhe ? <p className="mt-1 text-xs text-muted-foreground">{detalhe}</p> : null}
    </div>
  );
}
