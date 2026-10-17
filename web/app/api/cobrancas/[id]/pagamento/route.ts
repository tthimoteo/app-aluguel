import { proxyToApi } from "@/lib/api/proxy";

export async function POST(request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const body = await request.text();
  return proxyToApi(`/api/cobrancas/${id}/pagamento`, { method: "POST", body });
}
