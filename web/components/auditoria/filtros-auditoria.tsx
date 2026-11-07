import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { selectClass } from "@/components/imoveis/campos-endereco";

export type OpcaoFiltro = { id: string; nome: string };

const TABELAS: OpcaoFiltro[] = [
  { id: "imovel", nome: "Imóvel" },
  { id: "inquilino", nome: "Inquilino" },
  { id: "contrato", nome: "Contrato" },
  { id: "cliente", nome: "Cliente" },
  { id: "usuario", nome: "Usuário" },
  { id: "cobranca", nome: "Cobrança" },
  { id: "pagamento", nome: "Pagamento" },
];

const ACOES: OpcaoFiltro[] = [
  { id: "Cadastro", nome: "Cadastro" },
  { id: "Alteracao", nome: "Alteração" },
  { id: "Remocao", nome: "Remoção" },
  { id: "Pagamento", nome: "Pagamento" },
  { id: "Emissao", nome: "Emissão" },
  { id: "Cancelamento", nome: "Cancelamento" },
  { id: "Login", nome: "Login" },
];

export function FiltrosAuditoria({
  tabela,
  acao,
  clienteId,
  clientes,
  mostrarCliente,
}: {
  tabela?: string;
  acao?: string;
  clienteId?: string;
  clientes?: OpcaoFiltro[];
  mostrarCliente?: boolean;
}) {
  return (
    <form
      method="get"
      className="mb-4 grid gap-3 rounded-lg bg-card p-4 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border sm:grid-cols-2 lg:grid-cols-4"
    >
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="tabela">Registro</Label>
        <select id="tabela" name="tabela" defaultValue={tabela ?? ""} className={selectClass}>
          <option value="">Todos</option>
          {TABELAS.map((t) => (
            <option key={t.id} value={t.id}>
              {t.nome}
            </option>
          ))}
        </select>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="acao">Ação</Label>
        <select id="acao" name="acao" defaultValue={acao ?? ""} className={selectClass}>
          <option value="">Todas</option>
          {ACOES.map((a) => (
            <option key={a.id} value={a.id}>
              {a.nome}
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
          href="/auditoria"
          className="inline-flex h-10 items-center justify-center rounded-[4px] border border-input bg-background px-4 text-sm font-medium hover:bg-accent"
        >
          Limpar
        </a>
      </div>
    </form>
  );
}
