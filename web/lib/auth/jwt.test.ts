import { describe, expect, it } from "vitest";
import { decodeAccessToken } from "./jwt";

describe("decodeAccessToken", () => {
  it("lê claims padrão da API (sub, email, name, tenant_id, role)", () => {
    const payload = Buffer.from(
      JSON.stringify({
        sub: "11111111-1111-1111-1111-111111111111",
        email: "gestor@demo.local",
        name: "Gestor Demo",
        tenant_id: "22222222-2222-2222-2222-222222222222",
        cliente_id: "33333333-3333-3333-3333-333333333333",
        role: "Gestor",
      }),
    ).toString("base64url");
    const token = `header.${payload}.sig`;
    const user = decodeAccessToken(token);
    expect(user?.nome).toBe("Gestor Demo");
    expect(user?.roles).toEqual(["Gestor"]);
    expect(user?.clienteId).toBe("33333333-3333-3333-3333-333333333333");
  });

  it("retorna null para token inválido", () => {
    expect(decodeAccessToken("nao-e-jwt")).toBeNull();
  });
});
