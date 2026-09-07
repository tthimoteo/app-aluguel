import { CardField, DataList, DesktopTable, MobileCard } from "@/components/data/data-list";
import { EmptyState } from "@/components/data/empty-state";
import { PageHeader } from "@/components/data/page-header";
import { SearchForm } from "@/components/data/search-form";
import { StatusBadge } from "@/components/data/status-badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { api } from "@/lib/api/server";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { redirect } from "next/navigation";

export default async function ClientesPage({
  searchParams,
}: {
  searchParams: Promise<{ termo?: string }>;
}) {
  const usuario = await requireSession();
  if (!temPerfil(usuario, "Administrador")) redirect("/");

  const { termo } = await searchParams;
  const pagina = await api.clientes({ termo, take: 50 });

  return (
    <div>
      <PageHeader
        titulo="Clientes"
        descricao="Gestão de clientes da plataforma (somente Administrador)."
      />
      <SearchForm placeholder="Nome, CPF ou CNPJ" defaultValue={termo} />
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
                  {pagina.itens.map((c) => (
                    <TableRow key={c.id}>
                      <TableCell className="font-medium">{c.nomeExibicao}</TableCell>
                      <TableCell>{c.tipoPessoa}</TableCell>
                      <TableCell>{c.cpf ?? c.cnpj ?? "—"}</TableCell>
                      <TableCell>{c.email ?? "—"}</TableCell>
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
              <p className="mb-2 font-semibold">{c.nomeExibicao}</p>
              <CardField label="Tipo">{c.tipoPessoa}</CardField>
              <CardField label="Documento">{c.cpf ?? c.cnpj ?? "—"}</CardField>
              <CardField label="E-mail">{c.email ?? "—"}</CardField>
              <CardField label="Status">
                <StatusBadge status={c.status} />
              </CardField>
            </MobileCard>
          ))}
        />
      )}
      <p className="mt-3 text-xs text-muted-foreground">{pagina.total} cliente(s)</p>
    </div>
  );
}
