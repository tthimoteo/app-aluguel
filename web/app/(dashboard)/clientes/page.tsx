import Link from "next/link";
import { redirect } from "next/navigation";
import { CardField, DataList, DesktopTable, MobileCard } from "@/components/data/data-list";
import { EmptyState } from "@/components/data/empty-state";
import { PageHeader } from "@/components/data/page-header";
import { SearchForm } from "@/components/data/search-form";
import { StatusBadge } from "@/components/data/status-badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { api } from "@/lib/api/server";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { formatarCpfCnpj } from "@/lib/format";

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
                    <TableRow key={c.id} className="cursor-pointer hover:bg-muted/40">
                      <TableCell className="font-medium">
                        <Link href={`/clientes/${c.id}`} className="text-primary hover:underline">
                          {c.nomeExibicao}
                        </Link>
                      </TableCell>
                      <TableCell>
                        <Link href={`/clientes/${c.id}`} className="block">
                          {c.tipoPessoa}
                        </Link>
                      </TableCell>
                      <TableCell>
                        <Link href={`/clientes/${c.id}`} className="block">
                          {formatarCpfCnpj(c.cpf ?? c.cnpj)}
                        </Link>
                      </TableCell>
                      <TableCell>
                        <Link href={`/clientes/${c.id}`} className="block">
                          {c.email ?? "—"}
                        </Link>
                      </TableCell>
                      <TableCell>
                        <Link href={`/clientes/${c.id}`} className="block">
                          <StatusBadge status={c.status} />
                        </Link>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </DesktopTable>
          }
          mobile={pagina.itens.map((c) => (
            <Link key={c.id} href={`/clientes/${c.id}`} className="block">
              <MobileCard>
                <p className="mb-2 font-semibold text-primary">{c.nomeExibicao}</p>
                <CardField label="Tipo">{c.tipoPessoa}</CardField>
                <CardField label="Documento">{formatarCpfCnpj(c.cpf ?? c.cnpj)}</CardField>
                <CardField label="E-mail">{c.email ?? "—"}</CardField>
                <CardField label="Status">
                  <StatusBadge status={c.status} />
                </CardField>
              </MobileCard>
            </Link>
          ))}
        />
      )}
      <p className="mt-3 text-xs text-muted-foreground">{pagina.total} cliente(s)</p>
    </div>
  );
}
