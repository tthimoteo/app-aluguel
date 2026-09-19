import Link from "next/link";
import { CardField, DataList, DesktopTable, MobileCard } from "@/components/data/data-list";
import { EmptyState } from "@/components/data/empty-state";
import { PageHeader } from "@/components/data/page-header";
import { StatusBadge } from "@/components/data/status-badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { api } from "@/lib/api/server";
import { formatarData, formatarMoeda } from "@/lib/format";

export default async function ContratosPage({
  searchParams,
}: {
  searchParams: Promise<{ clienteId?: string }>;
}) {
  const { clienteId } = await searchParams;
  const pagina = await api.contratos({ take: 50, clienteId });
  const voltarImoveis = clienteId ? `/imoveis?clienteId=${clienteId}` : "/imoveis";

  return (
    <div>
      <p className="mb-3">
        <Link href={voltarImoveis} className="text-sm font-medium text-primary hover:text-primary/80">
          ← Imóveis
        </Link>
      </p>
      <PageHeader
        titulo="Contratos"
        descricao="Contratos de locação. Um imóvel admite apenas um contrato ativo (CASO 3)."
      />
      {pagina.itens.length === 0 ? (
        <EmptyState mensagem="Nenhum contrato cadastrado." />
      ) : (
        <DataList
          desktop={
            <DesktopTable>
              <Table>
                <TableHeader className="bg-[#34495e] [&_th]:text-white">
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="text-white">Número</TableHead>
                    <TableHead className="text-white">Início</TableHead>
                    <TableHead className="text-white">Vencimento</TableHead>
                    <TableHead className="text-white">Aluguel</TableHead>
                    <TableHead className="text-white">Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {pagina.itens.map((c) => (
                    <TableRow key={c.id}>
                      <TableCell className="font-medium">{c.numeroContrato}</TableCell>
                      <TableCell>{formatarData(c.dataInicio)}</TableCell>
                      <TableCell>Dia {c.diaVencimento}</TableCell>
                      <TableCell>{formatarMoeda(c.valorAluguel)}</TableCell>
                      <TableCell>
                        <StatusBadge status={c.status} />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </DesktopTable>
          }
          mobile={pagina.itens.map((c) => (
            <MobileCard key={c.id}>
              <p className="mb-2 font-semibold">{c.numeroContrato}</p>
              <CardField label="Início">{formatarData(c.dataInicio)}</CardField>
              <CardField label="Vencimento">Dia {c.diaVencimento}</CardField>
              <CardField label="Aluguel">{formatarMoeda(c.valorAluguel)}</CardField>
              <CardField label="Status">
                <StatusBadge status={c.status} />
              </CardField>
            </MobileCard>
          ))}
        />
      )}
      <p className="mt-3 text-xs text-muted-foreground">{pagina.total} contrato(s)</p>
    </div>
  );
}
