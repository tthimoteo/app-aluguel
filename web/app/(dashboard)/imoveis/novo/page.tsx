import { FormularioImovel } from "@/components/imoveis/formulario-imovel";
import { EmptyState } from "@/components/data/empty-state";
import { PageHeader } from "@/components/data/page-header";
import { listarClientesAcessiveis } from "@/lib/clientes-acesso.server";
import { clientePreselecionado } from "@/lib/clientes-acesso";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { redirect } from "next/navigation";

export default async function NovoImovelPage({
  searchParams,
}: {
  searchParams: Promise<{ clienteId?: string }>;
}) {
  const usuario = await requireSession();
  if (!temPerfil(usuario, "Administrador", "Gestor")) redirect("/imoveis");

  const { clienteId } = await searchParams;
  const clientes = await listarClientesAcessiveis({
    somenteGestor: !temPerfil(usuario, "Administrador"),
  });
  const clienteInicial = clientePreselecionado(clientes, clienteId, usuario.clienteId);

  return (
    <div>
      <PageHeader
        titulo="Incluir imóvel"
        descricao="Cadastro do imóvel (UC003). O cliente do contexto já vem selecionado e pode ser alterado."
      />
      {clientes.length === 0 ? (
        <EmptyState mensagem="Nenhum cliente disponível para incluir imóvel." />
      ) : (
        <FormularioImovel clientes={clientes} clienteInicial={clienteInicial} />
      )}
    </div>
  );
}
