import { notFound } from "next/navigation";
import { CadastroImovel } from "@/components/imoveis/cadastro-imovel";
import { ApiError, api } from "@/lib/api/server";
import type { Inquilino } from "@/lib/api/types";
import { requireSession, temPerfil } from "@/lib/auth/session";

export default async function ImovelPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ inquilinoId?: string }>;
}) {
  const usuario = await requireSession();
  const { id } = await params;
  const { inquilinoId } = await searchParams;

  let imovel;
  try {
    imovel = await api.imovel(id);
  } catch (e) {
    if (e instanceof ApiError && (e.status === 404 || e.status === 400)) notFound();
    throw e;
  }

  const [contratos, inquilinosPagina] = await Promise.all([
    api.contratos({ imovelId: id, take: 50 }),
    api.inquilinos({ clienteId: imovel.clienteId, take: 100 }),
  ]);

  const contratoAtivo = contratos.itens.find((c) => c.status === "Ativo") ?? null;
  const ultimoContrato = [...contratos.itens].sort((a, b) => b.dataInicio.localeCompare(a.dataInicio))[0];
  const alvoId = contratoAtivo?.inquilinoId ?? inquilinoId ?? ultimoContrato?.inquilinoId;

  let inquilino: Inquilino | null = null;
  if (alvoId) {
    try {
      const candidato = await api.inquilino(alvoId);
      if (candidato.clienteId === imovel.clienteId) inquilino = candidato;
    } catch (e) {
      if (!(e instanceof ApiError && e.status === 404)) throw e;
    }
  }

  return (
    <CadastroImovel
      imovel={imovel}
      inquilino={inquilino}
      contratoAtivo={contratoAtivo}
      inquilinosCliente={inquilinosPagina.itens}
      podeGerenciar={temPerfil(usuario, "Administrador", "Gestor")}
    />
  );
}
