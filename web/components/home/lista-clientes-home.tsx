import { CardField, DataList, DesktopTable, MobileCard } from "@/components/data/data-list";
import { EmptyState } from "@/components/data/empty-state";
import { StatusBadge } from "@/components/data/status-badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { Cliente, Plano } from "@/lib/api/types";
import { documentoCliente, nomeCliente, nomeDoPlano } from "@/lib/format";
import Link from "next/link";

export function ListaClientesHome({
  clientes,
  planos,
}: {
  clientes: { itens: Cliente[]; total: number };
  planos: Plano[];
}) {
  return (
    <section>
      <div className="mb-4 flex items-center justify-between">
        <h3 className="text-sm font-semibold">Clientes</h3>
        <Link href="/clientes" className="text-sm font-medium text-primary hover:text-primary/80">
          Ver todos
        </Link>
      </div>

      {clientes.itens.length === 0 ? (
        <EmptyState mensagem="Nenhum cliente cadastrado." />
      ) : (
        <DataList
          desktop={
            <DesktopTable>
              <Table>
                <TableHeader className="bg-[#34495e] [&_th]:text-white">
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="text-white">Nome (razão social)</TableHead>
                    <TableHead className="text-white">CPF ou CNPJ</TableHead>
                    <TableHead className="text-white">Plano</TableHead>
                    <TableHead className="text-white">Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {clientes.itens.map((cliente) => (
                    <TableRow key={cliente.id}>
                      <TableCell className="font-medium">{nomeCliente(cliente)}</TableCell>
                      <TableCell>{documentoCliente(cliente)}</TableCell>
                      <TableCell>{nomeDoPlano(planos, cliente.planoId)}</TableCell>
                      <TableCell>
                        <StatusBadge status={cliente.status} />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </DesktopTable>
          }
          mobile={clientes.itens.map((cliente) => (
            <MobileCard key={cliente.id}>
              <p className="mb-2 font-semibold">{nomeCliente(cliente)}</p>
              <CardField label="CPF ou CNPJ">{documentoCliente(cliente)}</CardField>
              <CardField label="Plano">{nomeDoPlano(planos, cliente.planoId)}</CardField>
              <CardField label="Status">
                <StatusBadge status={cliente.status} />
              </CardField>
            </MobileCard>
          ))}
        />
      )}
    </section>
  );
}
