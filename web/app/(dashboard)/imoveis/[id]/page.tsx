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

  const [contratos, vinculadosPagina, inquilinosCliente] = await Promise.all([
    api.contratos({ imovelId: id, take: 50 }),
    api.inquilinos({ clienteId: imovel.clienteId, imovelId: id, take: 20 }),
    api.inquilinos({ clienteId: imovel.clienteId, take: 100 }),
  ]);

  const contratoAtivo = contratos.itens.find((c) => c.status === "Ativo") ?? null;
  const ultimoContrato = [...contratos.itens].sort((a, b) => b.dataInicio.localeCompare(a.dataInicio))[0];
  const peloImovel =
    vinculadosPagina.itens.find((i) => i.status === "Ativo") ?? vinculadosPagina.itens[0] ?? null;
  const alvoId = contratoAtivo?.inquilinoId ?? peloImovel?.id ?? inquilinoId ?? ultimoContrato?.inquilinoId;

  let inquilino: Inquilino | null = null;
  if (alvoId) {
    if (peloImovel?.id === alvoId) {
      inquilino = peloImovel;
    } else {
      try {
        const candidato = await api.inquilino(alvoId);
        if (candidato.clienteId === imovel.clienteId) inquilino = candidato;
      } catch (e) {
        if (!(e instanceof ApiError && e.status === 404)) throw e;
      }
    }
  }

  return (
    <CadastroImovel
      imovel={imovel}
      inquilino={inquilino}
      contratoAtivo={contratoAtivo}
      inquilinosCliente={inquilinosCliente.itens}
      podeGerenciar={temPerfil(usuario, "Administrador", "Gestor")}
    />
  );
}
