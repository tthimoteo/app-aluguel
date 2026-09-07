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
