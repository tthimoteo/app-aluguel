import { proxyToApi } from "@/lib/api/proxy";

export async function PUT(request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const body = await request.text();
  return proxyToApi(`/api/inquilinos/${id}`, { method: "PUT", body });
}

export async function DELETE(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return proxyToApi(`/api/inquilinos/${id}`, { method: "DELETE" });
}
