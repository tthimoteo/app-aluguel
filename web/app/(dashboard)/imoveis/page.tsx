import Link from "next/link";
import { CardField, DataList, DesktopTable, MobileCard } from "@/components/data/data-list";
import { EmptyState } from "@/components/data/empty-state";
import { PageHeader } from "@/components/data/page-header";
import { SearchForm } from "@/components/data/search-form";
import { StatusBadge } from "@/components/data/status-badge";
import { SeletorCliente } from "@/components/clientes/seletor-cliente";
import { buttonVariants } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { ApiError, api } from "@/lib/api/server";
import { listarClientesAcessiveis } from "@/lib/clientes-acesso.server";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { formatarEndereco } from "@/lib/format";
import { cn } from "@/lib/utils";

export default async function ImoveisPage({
  searchParams,
}: {
  searchParams: Promise<{ termo?: string; clienteId?: string }>;
}) {
  const usuario = await requireSession();
  const ehAdmin = temPerfil(usuario, "Administrador");
  const podeIncluir = temPerfil(usuario, "Administrador", "Gestor");
  const { termo, clienteId } = await searchParams;
  const clientes = await listarClientesAcessiveis({ termo: clienteId ? undefined : termo });

  if (!clienteId) {
    if (clientes.length === 1 && !termo) {
      return (
        <ListaImoveisDoCliente
          clienteId={clientes[0]!.id}
          clienteNome={clientes[0]!.nome}
          termo={termo}
          mostrarVoltar={false}
          podeIncluir={podeIncluir}
        />
      );
    }

    return (
      <div>
        <PageHeader
          titulo="Imóveis"
          descricao="Selecione um cliente para ver os imóveis cadastrados."
          acao={
            podeIncluir ? (
              <Link href="/imoveis/novo" className={cn(buttonVariants(), "h-9 rounded-[4px] px-4")}>
                Incluir imóvel
              </Link>
            ) : null
          }
        />
        <SearchForm placeholder="Buscar cliente" defaultValue={termo} />
        <SeletorCliente clientes={clientes} hrefBase="/imoveis" vazio="Nenhum cliente disponível." />
      </div>
    );
  }

  const daLista = clientes.find((c) => c.id === clienteId);
  const clienteAdmin = !daLista && ehAdmin ? await api.cliente(clienteId).catch(() => null) : null;
  const clienteNome = daLista?.nome ?? clienteAdmin?.nomeExibicao;
  if (!clienteNome) {
    return (
      <div>
        <PageHeader titulo="Imóveis" descricao="Cliente não encontrado ou sem acesso." />
        <SeletorCliente clientes={clientes} hrefBase="/imoveis" />
      </div>
    );
  }

  return (
    <ListaImoveisDoCliente
      clienteId={clienteId}
      clienteNome={clienteNome}
      termo={termo}
      mostrarVoltar={clientes.length > 1}
      podeIncluir={podeIncluir}
    />
  );
}

async function ListaImoveisDoCliente({
  clienteId,
  clienteNome,
  termo,
  mostrarVoltar,
  podeIncluir,
}: {
  clienteId: string;
  clienteNome: string;
  termo?: string;
  mostrarVoltar: boolean;
  podeIncluir: boolean;
}) {
  let pagina;
  try {
    pagina = await api.imoveis({ termo, take: 50, clienteId });
  } catch (e) {
    if (e instanceof ApiError) {
      return (
        <div>
          {mostrarVoltar ? (
            <p className="mb-3">
              <Link href="/imoveis" className="text-sm font-medium text-primary hover:text-primary/80">
                ← Clientes
              </Link>
            </p>
          ) : null}
          <PageHeader titulo="Imóveis" descricao={e.message} />
        </div>
      );
    }
    throw e;
  }

  return (
    <div>
      {mostrarVoltar ? (
        <p className="mb-3">
          <Link href="/imoveis" className="text-sm font-medium text-primary hover:text-primary/80">
            ← Clientes
          </Link>
        </p>
      ) : null}
      <PageHeader
        titulo="Imóveis"
        descricao={`Cadastro de imóveis de ${clienteNome} (UC003).`}
        acao={
          <div className="flex flex-wrap gap-2">
            {podeIncluir ? (
              <Link
                href={`/imoveis/novo?clienteId=${clienteId}`}
                className={cn(buttonVariants(), "h-9 rounded-[4px] px-4")}
              >
                Incluir imóvel
              </Link>
            ) : null}
            <Link
              href={`/inquilinos?clienteId=${clienteId}`}
              className={cn(buttonVariants({ variant: "outline" }), "h-9 rounded-[4px] px-4")}
            >
              Inquilinos
            </Link>
            <Link
              href={`/contratos?clienteId=${clienteId}`}
              className={cn(buttonVariants({ variant: "outline" }), "h-9 rounded-[4px] px-4")}
            >
              Contratos
            </Link>
          </div>
        }
      />
      <SearchForm placeholder="Nome, matrícula ou cidade" defaultValue={termo} hidden={{ clienteId }} />
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
