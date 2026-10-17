import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { selectClass } from "@/components/imoveis/campos-endereco";

export type OpcaoFiltro = { id: string; nome: string };

export function FiltrosCobranca({
  competencia,
  imovelId,
  clienteId,
  imoveis,
  clientes,
  mostrarCliente,
}: {
  competencia?: string;
  imovelId?: string;
  clienteId?: string;
  imoveis: OpcaoFiltro[];
  clientes?: OpcaoFiltro[];
  mostrarCliente?: boolean;
}) {
  return (
    <form method="get" className="mb-4 grid gap-3 rounded-lg bg-card p-4 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border sm:grid-cols-2 lg:grid-cols-4">
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="competencia">Competência</Label>
        <Input
          id="competencia"
          name="competencia"
          placeholder="MM/AAAA"
          defaultValue={competencia ?? ""}
          className="h-10 rounded-[4px]"
          pattern="(0[1-9]|1[0-2])\/\d{4}"
          title="Use o formato MM/AAAA"
        />
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="imovelId">Imóvel</Label>
        <select id="imovelId" name="imovelId" defaultValue={imovelId ?? ""} className={selectClass}>
          <option value="">Todos</option>
          {imoveis.map((i) => (
            <option key={i.id} value={i.id}>
              {i.nome}
            </option>
          ))}
        </select>
      </div>
      {mostrarCliente ? (
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="clienteId">Cliente</Label>
          <select id="clienteId" name="clienteId" defaultValue={clienteId ?? ""} className={selectClass}>
            <option value="">Todos</option>
            {(clientes ?? []).map((c) => (
              <option key={c.id} value={c.id}>
                {c.nome}
              </option>
            ))}
          </select>
        </div>
      ) : null}
      <div className="flex items-end gap-2">
        <Button type="submit" className="h-10 rounded-[4px] px-4">
          Filtrar
        </Button>
        <a
          href="/cobrancas"
          className="inline-flex h-10 items-center justify-center rounded-[4px] border border-input bg-background px-4 text-sm font-medium hover:bg-accent"
        >
          Limpar
        </a>
      </div>
    </form>
  );
}
