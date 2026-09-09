import { describe, expect, it } from "vitest";
import { filtrarClientesAcessiveis } from "./clientes-acesso";

describe("filtrarClientesAcessiveis", () => {
  const clientes = [
    { id: "1", nome: "Cliente Demo", detalhe: "Gestor" },
    { id: "2", nome: "Cliente Extra", detalhe: "Analista" },
  ];

  it("retorna todos sem termo", () => {
    expect(filtrarClientesAcessiveis(clientes)).toHaveLength(2);
  });

  it("filtra por nome", () => {
    const r = filtrarClientesAcessiveis(clientes, "extra");
    expect(r).toHaveLength(1);
    expect(r[0]?.id).toBe("2");
  });
});
