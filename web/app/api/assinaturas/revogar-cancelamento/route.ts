import { proxyToApi } from "@/lib/api/proxy";

export async function POST() {
  return proxyToApi("/api/assinaturas/revogar-cancelamento", { method: "POST" });
}
