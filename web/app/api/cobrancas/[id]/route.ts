import { proxyToApi } from "@/lib/api/proxy";

export async function GET(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return proxyToApi(`/api/cobrancas/${id}`);
}
