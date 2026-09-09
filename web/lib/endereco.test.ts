import { describe, expect, it } from "vitest";
import { enderecoObrigatorioPreenchido, enderecoVazio } from "@/components/imoveis/campos-endereco";

describe("enderecoObrigatorioPreenchido", () => {
  const completo = {
    cep: "01001-000",
    logradouro: "Praça da Sé",
    numero: "100",
    complemento: "",
    bairro: "Sé",
    cidade: "São Paulo",
    uf: "SP",
  };

  it("aceita endereço com CEP, logradouro, número, bairro, cidade e UF", () => {
    expect(enderecoObrigatorioPreenchido(completo)).toBe(true);
    expect(enderecoObrigatorioPreenchido({ ...completo, complemento: "Sala 1" })).toBe(true);
  });

  it("rejeita CEP incompleto ou campos em branco", () => {
    expect(enderecoObrigatorioPreenchido(enderecoVazio())).toBe(false);
    expect(enderecoObrigatorioPreenchido({ ...completo, cep: "01001" })).toBe(false);
    expect(enderecoObrigatorioPreenchido({ ...completo, logradouro: "  " })).toBe(false);
    expect(enderecoObrigatorioPreenchido({ ...completo, numero: "" })).toBe(false);
    expect(enderecoObrigatorioPreenchido({ ...completo, bairro: "" })).toBe(false);
    expect(enderecoObrigatorioPreenchido({ ...completo, cidade: "" })).toBe(false);
    expect(enderecoObrigatorioPreenchido({ ...completo, uf: "S" })).toBe(false);
  });
});
