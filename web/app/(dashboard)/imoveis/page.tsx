import { CardField, DataList, DesktopTable, MobileCard } from "@/components/data/data-list";
import { EmptyState } from "@/components/data/empty-state";
import { PageHeader } from "@/components/data/page-header";
import { SearchForm } from "@/components/data/search-form";
import { StatusBadge } from "@/components/data/status-badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { api } from "@/lib/api/server";
import { formatarEndereco } from "@/lib/format";

export default async function ImoveisPage({
  searchParams,
}: {
  searchParams: Promise<{ termo?: string }>;
}) {
  const { termo } = await searchParams;
  const pagina = await api.imoveis({ termo, take: 50 });

  return (
    <div>
      <PageHeader titulo="Imóveis" descricao="Cadastro de imóveis do cliente (UC003)." />
      <SearchForm placeholder="Nome, matrícula ou cidade" defaultValue={termo} />
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
                    <TableHead className="text-white">Endereço</TableHead>
                    <TableHead className="text-white">IPTU</TableHead>
                    <TableHead className="text-white">Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {pagina.itens.map((i) => (
                    <TableRow key={i.id}>
                      <TableCell className="font-medium">{i.nome}</TableCell>
                      <TableCell>{i.tipo}</TableCell>
                      <TableCell>{formatarEndereco(i.endereco)}</TableCell>
                      <TableCell>{i.numeroIptu ?? "—"}</TableCell>
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
              <CardField label="Tipo">{i.tipo}</CardField>
              <CardField label="Endereço">{formatarEndereco(i.endereco)}</CardField>
              <CardField label="IPTU">{i.numeroIptu ?? "—"}</CardField>
              <CardField label="Status">
                <StatusBadge status={i.status} />
              </CardField>
            </MobileCard>
          ))}
        />
      )}
      <p className="mt-3 text-xs text-muted-foreground">{pagina.total} imóvel(is)</p>
    </div>
  );
}
