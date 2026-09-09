import { api } from "@/lib/api/server";
import { filtrarClientesAcessiveis, type ClienteOpcao } from "@/lib/clientes-acesso";
import { requireSession, temPerfil } from "@/lib/auth/session";

export async function listarClientesAcessiveis(opts?: {
  termo?: string;
  somenteGestor?: boolean;
}): Promise<ClienteOpcao[]> {
  const usuario = await requireSession();
  if (temPerfil(usuario, "Administrador")) {
    const pagina = await api.clientes({ termo: opts?.termo, take: 50 });
    return pagina.itens.map((c) => ({
      id: c.id,
      nome: c.nomeExibicao,
      detalhe: c.email ?? c.cpf ?? c.cnpj ?? undefined,
    }));
  }

  const me = await api.me();
  const opcoes = (me.clientes ?? [])
    .filter((c) => c.status === "Ativo")
    .filter((c) => !opts?.somenteGestor || c.perfil === "Gestor")
    .map((c) => ({
      id: c.clienteId,
      nome: c.nome,
      detalhe: c.perfil,
      perfil: c.perfil,
    }));

  return filtrarClientesAcessiveis(opcoes, opts?.termo);
}
