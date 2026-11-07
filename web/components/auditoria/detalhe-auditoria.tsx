"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";

function formatarJson(raw: string | null): string {
  if (!raw) return "—";
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

export function DetalheAuditoria({
  valoresAntes,
  valoresDepois,
}: {
  valoresAntes: string | null;
  valoresDepois: string | null;
}) {
  const [aberto, setAberto] = useState(false);
  if (!valoresAntes && !valoresDepois) return <span className="text-muted-foreground">—</span>;

  return (
    <div>
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="h-8 rounded-[4px] text-xs"
        onClick={() => setAberto((v) => !v)}
      >
        {aberto ? "Ocultar" : "Ver valores"}
      </Button>
      {aberto ? (
        <div className="mt-2 grid gap-2 md:grid-cols-2">
          <pre className="max-h-56 overflow-auto rounded-[4px] bg-muted/60 p-2 text-[11px] leading-relaxed text-foreground">
            <div className="mb-1 font-semibold text-[#495057] dark:text-muted-foreground">Antes</div>
            {formatarJson(valoresAntes)}
          </pre>
          <pre className="max-h-56 overflow-auto rounded-[4px] bg-muted/60 p-2 text-[11px] leading-relaxed text-foreground">
            <div className="mb-1 font-semibold text-[#495057] dark:text-muted-foreground">Depois</div>
            {formatarJson(valoresDepois)}
          </pre>
        </div>
      ) : null}
    </div>
  );
}
