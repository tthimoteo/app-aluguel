import { proxyToApi } from "@/lib/api/proxy";

function comCliente(id: string, request: Request) {
  const clienteId = new URL(request.url).searchParams.get("clienteId");
  return `/api/usuarios/${id}${clienteId ? `?clienteId=${encodeURIComponent(clienteId)}` : ""}`;
}

export async function GET(request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return proxyToApi(comCliente(id, request));
}

export async function PUT(request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const body = await request.text();
  return proxyToApi(comCliente(id, request), { method: "PUT", body });
}

export async function DELETE(request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return proxyToApi(comCliente(id, request), { method: "DELETE" });
}
