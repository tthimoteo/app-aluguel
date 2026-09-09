export type EnderecoViaCep = {
  cep: string;
  logradouro: string;
  bairro: string;
  cidade: string;
  uf: string;
  complemento: string;
};

export function somenteDigitosCep(valor: string): string {
  return valor.replace(/\D/g, "").slice(0, 8);
}

export function formatarCep(valor: string): string {
  const d = somenteDigitosCep(valor);
  if (d.length <= 5) return d;
  return `${d.slice(0, 5)}-${d.slice(5)}`;
}

export function parseRespostaCep(body: unknown): EnderecoViaCep | null {
  if (!body || typeof body !== "object") return null;
  const o = body as Record<string, unknown>;
  if (o.erro === true || o.erro === "true") return null;
  const uf = String(o.uf ?? "").trim().toUpperCase();
  const cidade = String(o.localidade ?? o.cidade ?? "").trim();
  if (!uf || !cidade) return null;
  return {
    cep: formatarCep(String(o.cep ?? "")),
    logradouro: String(o.logradouro ?? "").trim(),
    bairro: String(o.bairro ?? "").trim(),
    cidade,
    uf,
    complemento: String(o.complemento ?? "").trim(),
  };
}
