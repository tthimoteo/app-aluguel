import { describe, expect, it } from "vitest";
import { clientePreselecionado, filtrarClientesAcessiveis } from "./clientes-acesso";

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

describe("clientePreselecionado", () => {
  const clientes = [{ id: "a" }, { id: "b" }];

  it("usa o primeiro preferido que existe na lista", () => {
    expect(clientePreselecionado(clientes, "x", "b", "a")).toBe("b");
  });

  it("cai no primeiro cliente quando nenhum preferido bate", () => {
    expect(clientePreselecionado(clientes, "z")).toBe("a");
  });

  it("retorna vazio sem clientes", () => {
    expect(clientePreselecionado([], "a")).toBe("");
  });
});
