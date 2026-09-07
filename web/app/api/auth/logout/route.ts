import { NextResponse } from "next/server";
import { getApiUrl } from "@/lib/api/config";
import { clearAuthCookiesOnJar, getAccessTokenFromJar, getRefreshTokenFromJar } from "@/lib/auth/cookies";

export async function POST() {
  const access = await getAccessTokenFromJar();
  const refresh = await getRefreshTokenFromJar();

  if (access && refresh) {
    await fetch(`${getApiUrl()}/api/auth/logout`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${access}`,
      },
      body: JSON.stringify({ refreshToken: refresh }),
      cache: "no-store",
    }).catch(() => undefined);
  }

  await clearAuthCookiesOnJar();
  return new NextResponse(null, { status: 204 });
}
