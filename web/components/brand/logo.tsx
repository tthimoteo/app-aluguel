import { cn } from "@/lib/utils";

export function Logo({ className, compact = false }: { className?: string; compact?: boolean }) {
  return (
    <div className={cn("flex items-center gap-2.5", className)}>
      <svg viewBox="0 0 40 40" className="size-9 shrink-0" aria-hidden>
        <circle cx="20" cy="20" r="19" fill="#2c3e50" />
        <circle cx="20" cy="20" r="19" fill="none" stroke="#db6838" strokeWidth="2" />
        <path d="M20 20 L20 6 A14 14 0 0 1 32.1 27 Z" fill="#db6838" />
        <path d="M20 20 L32.1 27 A14 14 0 1 1 20 6 Z" fill="#ffffff" fillOpacity="0.92" />
        <circle cx="20" cy="20" r="3.2" fill="#2c3e50" />
      </svg>
      {!compact && (
        <div className="flex flex-col leading-tight">
          <span className="text-sm font-bold tracking-[0.14em]">LUCRARE</span>
          <span className="text-[11px] font-medium tracking-wide opacity-80">APP Aluguel</span>
        </div>
      )}
    </div>
  );
}
