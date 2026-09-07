import Link from "next/link";
import { CardField, DataList, DesktopTable, MobileCard } from "@/components/data/data-list";
import { EmptyState } from "@/components/data/empty-state";
import { PageHeader } from "@/components/data/page-header";
import { SearchForm } from "@/components/data/search-form";
import { StatusBadge } from "@/components/data/status-badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { api } from "@/lib/api/server";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { formatarData } from "@/lib/format";
import { redirect } from "next/navigation";

export default async function UsuariosPage({
  searchParams,
}: {
  searchParams: Promise<{ termo?: string; clienteId?: string }>;
}) {
  const usuario = await requireSession();
  if (!temPerfil(usuario, "Administrador", "Gestor")) redirect("/");

  const { termo, clienteId } = await searchParams;
  const ehAdmin = temPerfil(usuario, "Administrador");

  if (ehAdmin && !clienteId) {
    const clientes = await api.clientes({ termo, take: 50 });
    return (
      <div>
        <PageHeader
          titulo="Usuários"
          descricao="Selecione um cliente para gerenciar os usuários (Administrador)."
        />
        <SearchForm placeholder="Buscar cliente" defaultValue={termo} />
        {clientes.itens.length === 0 ? (
          <EmptyState mensagem="Nenhum cliente encontrado." />
        ) : (
          <ul className="grid gap-3">
            {clientes.itens.map((c) => (
              <li key={c.id}>
                <Link
                  href={`/usuarios?clienteId=${c.id}`}
                  className="block rounded-lg bg-card p-4 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border transition-transform hover:-translate-y-0.5"
                >
                  <p className="font-semibold">{c.nomeExibicao}</p>
                  <p className="text-sm text-muted-foreground">{c.email ?? c.cpf ?? c.cnpj}</p>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </div>
    );
  }

  const pagina = await api.usuarios({ termo, take: 50, clienteId });

  return (
    <div>
      <PageHeader titulo="Usuários" descricao="Usuários do cliente (Gestor e Analista). Limite conforme o plano." />
      <SearchForm placeholder="Nome ou e-mail" defaultValue={termo} hidden={clienteId ? { clienteId } : undefined} />
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
                    <TableHead className="text-white">E-mail</TableHead>
                    <TableHead className="text-white">Perfil</TableHead>
                    <TableHead className="text-white">Último login</TableHead>
                    <TableHead className="text-white">Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {pagina.itens.map((u) => (
                    <TableRow key={u.id}>
                      <TableCell className="font-medium">{u.nome}</TableCell>
                      <TableCell>{u.email}</TableCell>
                      <TableCell>
                        <StatusBadge status={u.perfil} />
                      </TableCell>
                      <TableCell>{formatarData(u.ultimoLogin)}</TableCell>
                      <TableCell>
                        <StatusBadge status={u.status} />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </DesktopTable>
          }
          mobile={pagina.itens.map((u) => (
            <MobileCard key={u.id}>
              <p className="mb-2 font-semibold">{u.nome}</p>
              <CardField label="E-mail">{u.email}</CardField>
              <CardField label="Perfil">
                <StatusBadge status={u.perfil} />
              </CardField>
              <CardField label="Status">
                <StatusBadge status={u.status} />
              </CardField>
            </MobileCard>
          ))}
        />
      )}
      <p className="mt-3 text-xs text-muted-foreground">{pagina.total} usuário(s)</p>
    </div>
  );
}
