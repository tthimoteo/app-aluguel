import { proxyToApi } from "@/lib/api/proxy";

export async function POST(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return proxyToApi(`/api/assinaturas/pagamentos/${id}/simular-pagamento`, { method: "POST" });
}
