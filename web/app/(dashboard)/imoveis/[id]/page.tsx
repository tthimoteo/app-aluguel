import { notFound } from "next/navigation";
import { CadastroImovel } from "@/components/imoveis/cadastro-imovel";
import type { HistoricoInquilinoItem } from "@/components/imoveis/historico-types";
import { ApiError, api } from "@/lib/api/server";
import type { Contrato, Inquilino } from "@/lib/api/types";
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
    api.contratos({ imovelId: id, take: 100 }),
    api.inquilinos({ clienteId: imovel.clienteId, imovelId: id, take: 50 }),
    api.inquilinos({ clienteId: imovel.clienteId, take: 100 }),
  ]);

  const contratoAtivo = contratos.itens.find((c) => c.status === "Ativo") ?? null;
  const historicoContratos = [...contratos.itens].sort((a, b) => b.dataInicio.localeCompare(a.dataInicio));
  const ultimoContrato = historicoContratos[0];
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

  const historicoInquilinos = await montarHistoricoInquilinos(
    historicoContratos,
    vinculadosPagina.itens,
  );

  return (
    <CadastroImovel
      imovel={imovel}
      inquilino={inquilino}
      contratoAtivo={contratoAtivo}
      inquilinosCliente={inquilinosCliente.itens}
      historicoContratos={historicoContratos}
      historicoInquilinos={historicoInquilinos}
      podeGerenciar={temPerfil(usuario, "Administrador", "Gestor")}
    />
  );
}

async function montarHistoricoInquilinos(
  contratos: Contrato[],
  vinculados: Inquilino[],
): Promise<HistoricoInquilinoItem[]> {
  const porId = new Map<string, Inquilino>();
  for (const i of vinculados) porId.set(i.id, i);

  const idsContrato = [...new Set(contratos.map((c) => c.inquilinoId))];
  for (const iid of idsContrato) {
    if (porId.has(iid)) continue;
    try {
      porId.set(iid, await api.inquilino(iid));
    } catch (e) {
      if (!(e instanceof ApiError && e.status === 404)) throw e;
    }
  }

  const ordenados = [...idsContrato].sort((a, b) => {
    const ca = contratos.filter((c) => c.inquilinoId === a);
    const cb = contratos.filter((c) => c.inquilinoId === b);
    const maxA = ca.reduce((m, c) => (c.dataInicio > m ? c.dataInicio : m), "");
    const maxB = cb.reduce((m, c) => (c.dataInicio > m ? c.dataInicio : m), "");
    return maxB.localeCompare(maxA);
  });

  const itens: HistoricoInquilinoItem[] = ordenados.map((iid) => {
    const inq = porId.get(iid);
    const doInq = contratos.filter((c) => c.inquilinoId === iid);
    const inicio = doInq.reduce<string | null>((m, c) => (!m || c.dataInicio < m ? c.dataInicio : m), null);
    const fim = doInq.reduce<string | null>((m, c) => {
      const f = c.dataFimPrevista;
      if (!f) return m;
      return !m || f > m ? f : m;
    }, null);
    return {
      inquilinoId: iid,
      nome: inq?.nome ?? "Inquilino (cadastro removido)",
      documento: inq?.documento ?? null,
      status: inq?.status ?? null,
      removido: !inq,
      dataInicio: inicio,
      dataFim: fim,
      detalhe: inq ?? null,
    };
  });

  for (const i of vinculados) {
    if (idsContrato.includes(i.id)) continue;
    itens.unshift({
      inquilinoId: i.id,
      nome: i.nome,
      documento: i.documento,
      status: i.status,
      removido: false,
      dataInicio: null,
      dataFim: null,
      detalhe: i,
    });
  }

  return itens;
}
