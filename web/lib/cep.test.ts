import { describe, expect, it, vi, afterEach } from "vitest";
import { buscarEnderecoPorCep, formatarCep, parseRespostaCep, somenteDigitosCep } from "./cep";

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
    expect(parseRespostaCep({ erro: "true" })).toBeNull();
  });
});

describe("buscarEnderecoPorCep", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("lê logradouro, bairro, cidade e UF da ViaCEP", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          cep: "01310-100",
          logradouro: "Avenida Paulista",
          bairro: "Bela Vista",
          localidade: "São Paulo",
          uf: "SP",
        }),
      }),
    );
    await expect(buscarEnderecoPorCep("01310-100")).resolves.toMatchObject({
      logradouro: "Avenida Paulista",
      bairro: "Bela Vista",
      cidade: "São Paulo",
      uf: "SP",
    });
  });

  it("rejeita CEP inexistente", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({ erro: true }),
      }),
    );
    await expect(buscarEnderecoPorCep("00000000")).rejects.toThrow("CEP não encontrado.");
  });
});
