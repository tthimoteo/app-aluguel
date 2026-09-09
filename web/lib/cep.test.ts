import { describe, expect, it } from "vitest";
import { formatarCep, parseRespostaCep, somenteDigitosCep } from "./cep";

describe("formatarCep", () => {
  it("mascara com hífen após 5 dígitos", () => {
    expect(formatarCep("01001000")).toBe("01001-000");
    expect(formatarCep("01001")).toBe("01001");
  });
});

describe("somenteDigitosCep", () => {
  it("limita a 8 dígitos", () => {
    expect(somenteDigitosCep("01001-00099")).toBe("01001000");
  });
});

describe("parseRespostaCep", () => {
  it("lê o JSON do ViaCEP", () => {
    const r = parseRespostaCep({
      cep: "01001-000",
      logradouro: "Praça da Sé",
      bairro: "Sé",
      localidade: "São Paulo",
      uf: "SP",
    });
    expect(r).toEqual({
      cep: "01001-000",
      logradouro: "Praça da Sé",
      bairro: "Sé",
      cidade: "São Paulo",
      uf: "SP",
      complemento: "",
    });
  });

  it("retorna nulo quando o CEP não existe", () => {
    expect(parseRespostaCep({ erro: true })).toBeNull();
  });
});
