/** Extrai mensagem amigável de ProblemDetails / ValidationProblem da API. */
export function mensagemApiErro(body: unknown, fallback = "Não foi possível concluir a operação."): string {
  if (!body || typeof body !== "object") return fallback;
  const o = body as Record<string, unknown>;
  if (typeof o.detail === "string" && o.detail.trim()) return o.detail;
  if (typeof o.erro === "string" && o.erro.trim()) return o.erro;
  if (o.errors && typeof o.errors === "object") {
    const msgs = Object.values(o.errors as Record<string, unknown>)
      .flatMap((v) => (Array.isArray(v) ? v : [v]))
      .filter((v): v is string => typeof v === "string" && v.trim().length > 0);
    if (msgs.length) return msgs.join(" ");
  }
  if (typeof o.title === "string" && o.title.trim()) return o.title;
  return fallback;
}
