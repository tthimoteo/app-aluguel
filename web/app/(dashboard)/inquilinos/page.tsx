import { CardField, DataList, DesktopTable, MobileCard } from "@/components/data/data-list";
import { EmptyState } from "@/components/data/empty-state";
import { PageHeader } from "@/components/data/page-header";
import { SearchForm } from "@/components/data/search-form";
import { StatusBadge } from "@/components/data/status-badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { api } from "@/lib/api/server";

export default async function InquilinosPage({
  searchParams,
}: {
  searchParams: Promise<{ termo?: string }>;
}) {
  const { termo } = await searchParams;
  const pagina = await api.inquilinos({ termo, take: 50 });

  return (
    <div>
      <PageHeader titulo="Inquilinos" descricao="Locatários vinculados aos imóveis do cliente." />
      <SearchForm placeholder="Nome, documento ou e-mail" defaultValue={termo} />
      {pagina.itens.length === 0 ? (
        <EmptyState />
      ) : (
        <DataList
          desktop={
            <DesktopTable>
              <Table>
                <TableHeader className="bg-[#34495e] [&_th]:text-white">
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="text-white">Nome</TableHead>
                    <TableHead className="text-white">Tipo</TableHead>
                    <TableHead className="text-white">Documento</TableHead>
                    <TableHead className="text-white">E-mail</TableHead>
                    <TableHead className="text-white">Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {pagina.itens.map((i) => (
                    <TableRow key={i.id}>
                      <TableCell className="font-medium">{i.nome}</TableCell>
                      <TableCell>{i.tipoPessoa}</TableCell>
                      <TableCell>{i.documento}</TableCell>
                      <TableCell>{i.email ?? "—"}</TableCell>
                      <TableCell>
                        <StatusBadge status={i.status} />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </DesktopTable>
          }
          mobile={pagina.itens.map((i) => (
            <MobileCard key={i.id}>
              <p className="mb-2 font-semibold">{i.nome}</p>
              <CardField label="Tipo">{i.tipoPessoa}</CardField>
              <CardField label="Documento">{i.documento}</CardField>
              <CardField label="E-mail">{i.email ?? "—"}</CardField>
              <CardField label="Status">
                <StatusBadge status={i.status} />
              </CardField>
            </MobileCard>
          ))}
        />
      )}
      <p className="mt-3 text-xs text-muted-foreground">{pagina.total} inquilino(s)</p>
    </div>
  );
}
