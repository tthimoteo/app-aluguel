import { describe, expect, it } from "vitest";
import { itensVisiveis, tituloDaRota } from "./navigation";

describe("itensVisiveis", () => {
  it("mostra Início e cadastros operacionais para qualquer perfil", () => {
    const labels = itensVisiveis(["Analista"]).map((i) => i.label);
    expect(labels).toContain("Início");
    expect(labels).toContain("Imóveis");
    expect(labels).toContain("Relatórios");
    expect(labels).not.toContain("Inquilinos");
    expect(labels).not.toContain("Contratos");
  });

  it("esconde Clientes para Gestor e Analista (menu §13)", () => {
    expect(itensVisiveis(["Gestor"]).some((i) => i.href === "/clientes")).toBe(false);
    expect(itensVisiveis(["Analista"]).some((i) => i.href === "/clientes")).toBe(false);
  });

  it("mostra Clientes, Relatórios e Auditoria para Administrador", () => {
    const hrefs = itensVisiveis(["Administrador"]).map((i) => i.href);
    expect(hrefs).toContain("/clientes");
    expect(hrefs).toContain("/relatorios");
    expect(hrefs).toContain("/auditoria");
    expect(hrefs).not.toContain("/minha-conta");
  });

  it("mostra Minha Conta e Dados para Contabilidade só para Gestor", () => {
    const hrefs = itensVisiveis(["Gestor"]).map((i) => i.href);
    expect(hrefs).toContain("/minha-conta");
    expect(hrefs).toContain("/contabilidade");
    expect(hrefs).toContain("/usuarios");
    expect(hrefs).toContain("/relatorios");
    expect(hrefs).not.toContain("/clientes");
  });
});

describe("tituloDaRota", () => {
  it("resolve o título a partir do caminho", () => {
    expect(tituloDaRota("/")).toBe("Início");
    expect(tituloDaRota("/imoveis")).toBe("Imóveis");
    expect(tituloDaRota("/clientes/abc")).toBe("Clientes");
    expect(tituloDaRota("/inquilinos")).toBe("Inquilinos");
    expect(tituloDaRota("/contratos")).toBe("Contratos");
  });
});
