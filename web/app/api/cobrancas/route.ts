import { proxyToApi } from "@/lib/api/proxy";

export async function GET(request: Request) {
  const url = new URL(request.url);
  return proxyToApi(`/api/cobrancas${url.search}`);
}

export async function POST(request: Request) {
  const body = await request.text();
  return proxyToApi("/api/cobrancas", { method: "POST", body });
}
