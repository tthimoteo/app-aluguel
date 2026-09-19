import { proxyToApi } from "@/lib/api/proxy";

export async function GET(request: Request) {
  const url = new URL(request.url);
  const clienteId = url.searchParams.get("clienteId") ?? "";
  const cpf = url.searchParams.get("cpf") ?? "";
  return proxyToApi(`/api/usuarios/por-cpf?clienteId=${encodeURIComponent(clienteId)}&cpf=${encodeURIComponent(cpf)}`);
}
