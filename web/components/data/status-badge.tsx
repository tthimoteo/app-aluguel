import { cn } from "@/lib/utils";
import { semanticaStatus, type SemanticaStatus } from "@/lib/status";

const ESTILOS: Record<SemanticaStatus, string> = {
  sucesso: "bg-[#d4edda] text-[#155724] dark:bg-success/20 dark:text-[#d4edda]",
  perigo: "bg-[#f8d7da] text-[#721c24] dark:bg-destructive/20 dark:text-[#f8d7da]",
  aviso: "bg-[#fff3cd] text-[#856404] dark:bg-warning/20 dark:text-warning",
  info: "bg-[#17a2b8] text-white",
  destaque: "bg-primary text-primary-foreground",
  neutro: "bg-muted text-muted-foreground",
};

export function StatusBadge({ status }: { status: string }) {
  return (
    <span
      className={cn(
        "inline-block rounded-xl px-2 py-0.5 text-[11px] font-semibold uppercase tracking-wide",
        ESTILOS[semanticaStatus(status)],
      )}
    >
      {status}
    </span>
  );
}
