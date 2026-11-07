import { NextResponse, type NextRequest } from "next/server";
import { getApiUrl } from "@/lib/api/config";
import type { TokensAutenticacao } from "@/lib/api/types";
import { applyAuthCookies, clearAuthCookies } from "@/lib/auth/cookie-response";
import { ACCESS_COOKIE, REFRESH_COOKIE } from "@/lib/auth/constants";

const PUBLIC_EXACT = new Set(["/", "/login", "/cadastro", "/demonstracao"]);
const PUBLIC_PREFIXES = ["/login", "/cadastro", "/demonstracao", "/api/auth/login", "/api/planos", "/api/cadastro-publico"];

function isPublic(pathname: string): boolean {
  if (PUBLIC_EXACT.has(pathname)) return true;
  if (PUBLIC_PREFIXES.some((p) => pathname === p || pathname.startsWith(`${p}/`))) return true;
  if (pathname.startsWith("/_next")) return true;
  if (pathname === "/favicon.ico") return true;
  return false;
}

async function renovarTokens(refreshToken: string): Promise<TokensAutenticacao | null> {
  try {
    const res = await fetch(`${getApiUrl()}/api/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({ refreshToken }),
      cache: "no-store",
    });
    if (!res.ok) return null;
    return (await res.json()) as TokensAutenticacao;
  } catch {
    return null;
  }
}

export async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const access = request.cookies.get(ACCESS_COOKIE)?.value;
  const refresh = request.cookies.get(REFRESH_COOKIE)?.value;

  const response = NextResponse.next();
  let autenticado = Boolean(access);

  if (!access && refresh) {
    const tokens = await renovarTokens(refresh);
    if (tokens) {
      autenticado = true;
      applyAuthCookies(response, tokens);
    } else {
      clearAuthCookies(response);
    }
  }

  if (!autenticado && !isPublic(pathname) && !pathname.startsWith("/api/")) {
    const login = request.nextUrl.clone();
    login.pathname = "/login";
    if (pathname !== "/") {
      login.searchParams.set("from", pathname);
    }
    return NextResponse.redirect(login);
  }

  if (autenticado && (pathname === "/login" || pathname === "/")) {
    return NextResponse.redirect(new URL("/inicio", request.url));
  }

  return response;
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)"],
};
