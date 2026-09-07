import { describe, expect, it } from "vitest";
import { formatarData, formatarMoeda, iniciais } from "./format";
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
});
