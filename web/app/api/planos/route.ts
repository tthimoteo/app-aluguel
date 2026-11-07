import { NextResponse } from "next/server";
import { getApiUrl } from "@/lib/api/config";

export async function GET() {
  try {
    const res = await fetch(`${getApiUrl()}/api/planos`, {
      headers: { Accept: "application/json" },
      cache: "no-store",
    });
    const body = await res.text();
    return new NextResponse(body, {
      status: res.status,
      headers: { "Content-Type": "application/json" },
    });
  } catch {
    return NextResponse.json({ erro: "Não foi possível carregar os planos." }, { status: 502 });
  }
}
