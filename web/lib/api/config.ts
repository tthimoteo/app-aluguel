/** URL da API .NET. Usada apenas no servidor (Route Handlers, Server Components, middleware). */
export function getApiUrl(): string {
  return (
    process.env.API_URL?.replace(/\/$/, "") ||
    process.env.NEXT_PUBLIC_API_URL?.replace(/\/$/, "") ||
    "http://127.0.0.1:5272"
  );
}
