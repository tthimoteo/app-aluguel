import { cookies } from "next/headers";
import {
  ACCESS_COOKIE,
  ACCESS_MAX_AGE_SECONDS,
  REFRESH_COOKIE,
  REFRESH_MAX_AGE_SECONDS,
} from "@/lib/auth/constants";
import { authCookieBase } from "@/lib/auth/cookie-response";
import type { TokensAutenticacao } from "@/lib/api/types";

export async function setAuthCookiesOnJar(
  tokens: Pick<TokensAutenticacao, "accessToken" | "refreshToken">,
) {
  const jar = await cookies();
  const base = authCookieBase();
  jar.set(ACCESS_COOKIE, tokens.accessToken, { ...base, maxAge: ACCESS_MAX_AGE_SECONDS });
  jar.set(REFRESH_COOKIE, tokens.refreshToken, { ...base, maxAge: REFRESH_MAX_AGE_SECONDS });
}

export async function clearAuthCookiesOnJar() {
  const jar = await cookies();
  jar.delete(ACCESS_COOKIE);
  jar.delete(REFRESH_COOKIE);
}

export async function getAccessTokenFromJar(): Promise<string | undefined> {
  return (await cookies()).get(ACCESS_COOKIE)?.value;
}

export async function getRefreshTokenFromJar(): Promise<string | undefined> {
  return (await cookies()).get(REFRESH_COOKIE)?.value;
}
