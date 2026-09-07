const moeda = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

export function formatarMoeda(valor: number | null | undefined): string {
  if (valor == null || Number.isNaN(valor)) return "—";
  return moeda.format(valor);
}

export function formatarData(iso: string | null | undefined): string {
  if (!iso) return "—";
  const parte = iso.slice(0, 10);
  const [ano, mes, dia] = parte.split("-");
  if (!ano || !mes || !dia) return iso;
  return `${dia}/${mes}/${ano}`;
}

export function formatarEndereco(endereco: {
  logradouro?: string | null;
  numero?: string | null;
  bairro?: string | null;
  cidade?: string | null;
  uf?: string | null;
} | null | undefined): string {
  if (!endereco) return "—";
  const linha = [
    [endereco.logradouro, endereco.numero].filter(Boolean).join(", "),
    endereco.bairro,
    [endereco.cidade, endereco.uf].filter(Boolean).join("/"),
  ].filter(Boolean);
  return linha.length ? linha.join(" · ") : "—";
}

export function iniciais(nome: string): string {
  const partes = nome.trim().split(/\s+/).filter(Boolean);
  if (partes.length === 0) return "?";
  if (partes.length === 1) return partes[0]!.slice(0, 2).toUpperCase();
  return `${partes[0]![0] ?? ""}${partes[partes.length - 1]![0] ?? ""}`.toUpperCase();
}

function soDigitos(valor: string): string {
  return valor.replace(/\D/g, "");
}

/** Formata CPF (11 dígitos) ou CNPJ (14). Outros comprimentos retornam o original. */
export function formatarCpfCnpj(valor: string | null | undefined): string {
  if (!valor) return "—";
  const d = soDigitos(valor);
  if (d.length === 11) {
    return d.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, "$1.$2.$3-$4");
  }
  if (d.length === 14) {
    return d.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, "$1.$2.$3/$4-$5");
  }
  return valor;
}

/** CPF ou CNPJ do cliente — o que estiver preenchido. */
export function documentoCliente(cliente: {
  cpf?: string | null;
  cnpj?: string | null;
}): string {
  return formatarCpfCnpj(cliente.cpf || cliente.cnpj);
}

/** Razão social (PJ) ou nome (PF), com fallback para nomeExibicao. */
export function nomeCliente(cliente: {
  nome?: string | null;
  razaoSocial?: string | null;
  nomeExibicao?: string | null;
}): string {
  return cliente.razaoSocial || cliente.nome || cliente.nomeExibicao || "—";
}

export function nomeDoPlano(
  planos: readonly { id: string; nome: string }[],
  planoId: string | null | undefined,
): string {
  if (!planoId) return "—";
  return planos.find((p) => p.id === planoId)?.nome ?? "—";
}

const ROTULOS_STATUS: Record<string, string> = {
  trial: "Trial",
  pendentepagamento: "Pendente pagamento",
  ativa: "Ativa",
  ativo: "Ativo",
  inativo: "Inativo",
  suspensa: "Suspensa",
  cancelada: "Cancelada",
  cancelado: "Cancelado",
  rascunho: "Rascunho",
  vigente: "Vigente",
  encerrado: "Encerrado",
  pendente: "Pendente",
  pago: "Pago",
  atrasado: "Atrasado",
  emitida: "Emitida",
  erro: "Erro",
};

/** Rótulo em pt-BR para status da API (enums PascalCase inclusive). */
export function rotuloStatus(status: string | null | undefined): string {
  if (!status) return "—";
  const chave = status.replace(/[\s_-]/g, "").toLowerCase();
  return ROTULOS_STATUS[chave] ?? status;
}
