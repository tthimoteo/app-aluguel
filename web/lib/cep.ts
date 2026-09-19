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

/** Consulta a ViaCEP (browser ou servidor). Aborta com AbortError se `signal` for cancelado. */
export async function buscarEnderecoPorCep(cep: string, signal?: AbortSignal): Promise<EnderecoViaCep> {
  const d = somenteDigitosCep(cep);
  if (d.length !== 8) {
    throw new Error("CEP deve conter 8 dígitos.");
  }
  let res: Response;
  try {
    res = await fetch(`https://viacep.com.br/ws/${d}/json/`, { cache: "no-store", signal });
  } catch (e) {
    if (e instanceof DOMException && e.name === "AbortError") throw e;
    throw new Error("Não foi possível consultar o CEP. Tente de novo.");
  }
  if (!res.ok) {
    throw new Error("Não foi possível consultar o CEP. Tente de novo.");
  }
  const body: unknown = await res.json();
  const endereco = parseRespostaCep(body);
  if (!endereco) {
    throw new Error("CEP não encontrado.");
  }
  return endereco;
}
