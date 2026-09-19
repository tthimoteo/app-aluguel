import { SeletorCliente } from "@/components/clientes/seletor-cliente";
import { PageHeader } from "@/components/data/page-header";
import { SearchForm } from "@/components/data/search-form";
import { GestaoUsuarios } from "@/components/usuarios/gestao-usuarios";
import { ApiError, api } from "@/lib/api/server";
import { listarClientesAcessiveis } from "@/lib/clientes-acesso.server";
import { requireSession, temPerfil } from "@/lib/auth/session";
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
  const clientes = await listarClientesAcessiveis({
    termo: clienteId ? undefined : termo,
    somenteGestor: !ehAdmin,
  });

  if (!clienteId) {
    return (
      <div>
        <PageHeader
          titulo="Usuários"
          descricao="Selecione um cliente para gerenciar os usuários cadastrados."
        />
        <SearchForm placeholder="Buscar cliente" defaultValue={termo} />
        <SeletorCliente clientes={clientes} hrefBase="/usuarios" vazio="Nenhum cliente disponível." />
      </div>
    );
  }

  const daLista = clientes.find((c) => c.id === clienteId);
  const clienteAdmin = !daLista && ehAdmin ? await api.cliente(clienteId).catch(() => null) : null;
  const clienteNome = daLista?.nome ?? clienteAdmin?.nomeExibicao;
  if (!clienteNome) {
    redirect("/usuarios");
  }

  try {
    const pagina = await api.usuarios({ termo, take: 50, clienteId });
    return (
      <GestaoUsuarios
        usuarios={pagina.itens}
        total={pagina.total}
        clienteId={clienteId}
        clienteNome={clienteNome}
        termo={termo}
      />
    );
  } catch (e) {
    if (e instanceof ApiError) {
      return (
        <div>
          <PageHeader titulo="Usuários" descricao={e.message} />
          <SeletorCliente clientes={clientes} hrefBase="/usuarios" />
        </div>
      );
    }
    throw e;
  }
}
