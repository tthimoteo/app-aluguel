import { proxyToApi } from "@/lib/api/proxy";

export async function GET(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return proxyToApi(`/api/clientes/${id}/integracao-boleto`);
}

export async function PUT(request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const body = await request.text();
  return proxyToApi(`/api/clientes/${id}/integracao-boleto`, {
    method: "PUT",
    body,
  });
}
