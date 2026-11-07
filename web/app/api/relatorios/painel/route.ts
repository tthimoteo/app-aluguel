import { proxyToApi } from "@/lib/api/proxy";

export async function GET(request: Request) {
  const { search } = new URL(request.url);
  return proxyToApi(`/api/relatorios/painel${search}`);
}
