import { describe, expect, it } from "vitest";
import {
  documentoCliente,
  formatarCpfCnpj,
  formatarData,
  formatarDataHora,
  formatarMoeda,
  iniciais,
  nomeCliente,
  nomeDoPlano,
  rotuloStatus,
} from "./format";
import { semanticaStatus } from "./status";

describe("formatarMoeda", () => {
  it("formata em real brasileiro", () => {
    expect(formatarMoeda(2500)).toMatch(/R\$\s?2\.500,00/);
  });

  it("retorna traço para vazio", () => {
    expect(formatarMoeda(null)).toBe("—");
  });
});

describe("formatarData", () => {
  it("converte ISO para pt-BR", () => {
    expect(formatarData("2026-03-01")).toBe("01/03/2026");
  });
});

describe("formatarDataHora", () => {
  it("retorna traço para vazio", () => {
    expect(formatarDataHora(null)).toBe("—");
    expect(formatarDataHora(undefined)).toBe("—");
  });
});

describe("iniciais", () => {
  it("usa primeiro e último nome", () => {
    expect(iniciais("Gestor Demo")).toBe("GD");
  });
});

describe("semanticaStatus", () => {
  it("mapeia ativo/inativo/admin", () => {
    expect(semanticaStatus("Ativo")).toBe("sucesso");
    expect(semanticaStatus("Inativo")).toBe("perigo");
    expect(semanticaStatus("Administrador")).toBe("destaque");
  });

  it("mapeia status comercial do cliente", () => {
    expect(semanticaStatus("Ativa")).toBe("sucesso");
    expect(semanticaStatus("Trial")).toBe("info");
    expect(semanticaStatus("PendentePagamento")).toBe("aviso");
    expect(semanticaStatus("Cancelada")).toBe("perigo");
  });
});

describe("formatarCpfCnpj", () => {
  it("formata CPF e CNPJ", () => {
    expect(formatarCpfCnpj("39053344705")).toBe("390.533.447-05");
    expect(formatarCpfCnpj("12345678000190")).toBe("12.345.678/0001-90");
    expect(formatarCpfCnpj(null)).toBe("—");
  });
});

describe("documento e nome do cliente", () => {
  it("escolhe CPF ou CNPJ e o nome de exibição", () => {
    expect(documentoCliente({ cpf: "39053344705", cnpj: null })).toBe("390.533.447-05");
    expect(documentoCliente({ cpf: null, cnpj: "12345678000190" })).toBe("12.345.678/0001-90");
    expect(
      nomeCliente({
        nome: "Maria Silva",
        razaoSocial: null,
        nomeExibicao: "Maria",
      }),
    ).toBe("Maria Silva");
    expect(
      nomeCliente({
        nome: null,
        razaoSocial: "Imobiliária Demo Ltda",
        nomeExibicao: "Demo",
      }),
    ).toBe("Imobiliária Demo Ltda");
  });
});

describe("nomeDoPlano", () => {
  it("resolve o nome pelo id", () => {
    const planos = [{ id: "p1", nome: "Intermediário" }];
    expect(nomeDoPlano(planos, "p1")).toBe("Intermediário");
    expect(nomeDoPlano(planos, null)).toBe("—");
    expect(nomeDoPlano(planos, "outro")).toBe("—");
  });
});

describe("rotuloStatus", () => {
  it("traduz enums PascalCase", () => {
    expect(rotuloStatus("PendentePagamento")).toBe("Pendente pagamento");
    expect(rotuloStatus("Ativa")).toBe("Ativa");
    expect(rotuloStatus("Trial")).toBe("Trial");
  });
});
