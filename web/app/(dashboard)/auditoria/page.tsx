import { FiltrosAuditoria } from "@/components/auditoria/filtros-auditoria";
import { DetalheAuditoria } from "@/components/auditoria/detalhe-auditoria";
import { CardField, DataList, DesktopTable, MobileCard } from "@/components/data/data-list";
import { EmptyState } from "@/components/data/empty-state";
import { PageHeader } from "@/components/data/page-header";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { ApiError, api } from "@/lib/api/server";
import { requireSession, temPerfil } from "@/lib/auth/session";

const ROTULO_TABELA: Record<string, string> = {
  imovel: "Imóvel",
  inquilino: "Inquilino",
  contrato: "Contrato",
  cliente: "Cliente",
  usuario: "Usuário",
  cobranca: "Cobrança",
  pagamento: "Pagamento",
};

const ROTULO_ACAO: Record<string, string> = {
  Cadastro: "Cadastro",
  Alteracao: "Alteração",
  Remocao: "Remoção",
  Pagamento: "Pagamento",
  Emissao: "Emissão",
  Cancelamento: "Cancelamento",
  Login: "Login",
  Despesa: "Despesa",
  AlteracaoCadastral: "Alteração cadastral",
};

function formatarDataHoraSp(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "medium",
    timeZone: "America/Sao_Paulo",
  }).format(d);
}

export default async function AuditoriaPage({
  searchParams,
}: {
  searchParams: Promise<{ tabela?: string; acao?: string; clienteId?: string }>;
}) {
  const usuario = await requireSession();
  const ehAdmin = temPerfil(usuario, "Administrador");
  const { tabela, acao, clienteId } = await searchParams;

  let pagina;
  try {
    pagina = await api.auditoria({
      tabela,
      acao,
      clienteId: ehAdmin ? clienteId : undefined,
      take: 50,
    });
  } catch (e) {
    if (e instanceof ApiError) {
      return (
        <div>
          <PageHeader
            titulo="Auditoria"
            descricao="Trilha de cadastros e alterações consultável por todos os perfis."
          />
          <p className="text-sm text-destructive">{e.message}</p>
        </div>
      );
    }
    throw e;
  }

  const clientesPagina = ehAdmin
    ? await api.clientes({ take: 100 }).catch(() => ({ itens: [], total: 0, skip: 0, take: 0 }))
    : { itens: [], total: 0, skip: 0, take: 0 };

  return (
    <div>
      <PageHeader
        titulo="Auditoria"
        descricao="Cadastro e alteração de imóveis, inquilinos, contratos, cobranças, pagamentos, usuários e clientes."
      />
      <FiltrosAuditoria
        tabela={tabela}
        acao={acao}
        clienteId={clienteId}
        mostrarCliente={ehAdmin}
        clientes={clientesPagina.itens.map((c) => ({ id: c.id, nome: c.nomeExibicao }))}
      />
      {pagina.itens.length === 0 ? (
        <EmptyState titulo="Nenhum evento encontrado" descricao="Ajuste os filtros ou realize alterações cadastrais." />
      ) : (
        <>
          <p className="mb-3 text-sm text-muted-foreground">
            {pagina.total} evento{pagina.total === 1 ? "" : "s"}
          </p>
          <DataList
            desktop={
              <DesktopTable>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Data/hora</TableHead>
                      <TableHead>Usuário</TableHead>
                      <TableHead>Ação</TableHead>
                      <TableHead>Registro</TableHead>
                      <TableHead>Resumo</TableHead>
                      <TableHead>Detalhe</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {pagina.itens.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell className="whitespace-nowrap text-sm">
                          {formatarDataHoraSp(item.dataHora)}
                        </TableCell>
                        <TableCell className="text-sm">
                          {item.usuarioNome ?? "—"}
                          {item.ip ? (
                            <div className="text-xs text-muted-foreground">IP {item.ip}</div>
                          ) : null}
                        </TableCell>
                        <TableCell className="text-sm">
                          {ROTULO_ACAO[item.acao] ?? item.acao}
                        </TableCell>
                        <TableCell className="text-sm">
                          {item.tabela ? (ROTULO_TABELA[item.tabela] ?? item.tabela) : "—"}
                        </TableCell>
                        <TableCell className="max-w-xs text-sm">{item.resumo ?? "—"}</TableCell>
                        <TableCell>
                          <DetalheAuditoria
                            valoresAntes={item.valoresAntes}
                            valoresDepois={item.valoresDepois}
                          />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </DesktopTable>
            }
            mobile={pagina.itens.map((item) => (
              <MobileCard key={item.id}>
                <CardField label="Quando">{formatarDataHoraSp(item.dataHora)}</CardField>
                <CardField label="Usuário">{item.usuarioNome ?? "—"}</CardField>
                <CardField label="Ação">{ROTULO_ACAO[item.acao] ?? item.acao}</CardField>
                <CardField label="Registro">
                  {item.tabela ? (ROTULO_TABELA[item.tabela] ?? item.tabela) : "—"}
                </CardField>
                <CardField label="Resumo">{item.resumo ?? "—"}</CardField>
                <div className="pt-2">
                  <DetalheAuditoria
                    valoresAntes={item.valoresAntes}
                    valoresDepois={item.valoresDepois}
                  />
                </div>
              </MobileCard>
            ))}
          />
        </>
      )}
    </div>
  );
}
