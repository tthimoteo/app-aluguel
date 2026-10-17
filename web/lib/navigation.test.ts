import { describe, expect, it } from "vitest";
import { itensVisiveis, tituloDaRota } from "./navigation";

describe("itensVisiveis", () => {
  it("mostra Início e cadastros operacionais para qualquer perfil", () => {
    const labels = itensVisiveis(["Analista"], { permiteCobranca: true }).map((i) => i.label);
    expect(labels).toContain("Início");
    expect(labels).toContain("Imóveis");
    expect(labels).toContain("Relatórios");
    expect(labels).toContain("Cobrança");
    expect(labels).not.toContain("Inquilinos");
    expect(labels).not.toContain("Contratos");
  });

  it("esconde Cobrança para Gestor/Analista sem plano NFS-e", () => {
    expect(itensVisiveis(["Gestor"], { permiteCobranca: false }).some((i) => i.href === "/cobrancas")).toBe(
      false,
    );
    expect(itensVisiveis(["Analista"]).some((i) => i.href === "/cobrancas")).toBe(false);
  });

  it("mostra Cobrança para Administrador mesmo sem flag de plano", () => {
    expect(
      itensVisiveis(["Administrador"], { ehAdministrador: true }).some((i) => i.href === "/cobrancas"),
    ).toBe(true);
  });

  it("esconde Clientes para Gestor e Analista (menu §13)", () => {
    expect(itensVisiveis(["Gestor"], { permiteCobranca: true }).some((i) => i.href === "/clientes")).toBe(
      false,
    );
    expect(itensVisiveis(["Analista"], { permiteCobranca: true }).some((i) => i.href === "/clientes")).toBe(
      false,
    );
  });

  it("mostra Clientes, Relatórios e Auditoria para Administrador", () => {
    const hrefs = itensVisiveis(["Administrador"], { ehAdministrador: true }).map((i) => i.href);
    expect(hrefs).toContain("/clientes");
    expect(hrefs).toContain("/relatorios");
    expect(hrefs).toContain("/auditoria");
    expect(hrefs).toContain("/cobrancas");
    expect(hrefs).not.toContain("/minha-conta");
  });

  it("mostra Minha Conta e Dados para Contabilidade só para Gestor", () => {
    const hrefs = itensVisiveis(["Gestor"], { permiteCobranca: true }).map((i) => i.href);
    expect(hrefs).toContain("/minha-conta");
    expect(hrefs).toContain("/contabilidade");
    expect(hrefs).toContain("/usuarios");
    expect(hrefs).toContain("/relatorios");
    expect(hrefs).toContain("/cobrancas");
    expect(hrefs).not.toContain("/clientes");
  });
});

describe("tituloDaRota", () => {
  it("resolve o título a partir do caminho", () => {
    expect(tituloDaRota("/")).toBe("Início");
    expect(tituloDaRota("/imoveis")).toBe("Imóveis");
    expect(tituloDaRota("/imoveis/novo")).toBe("Incluir imóvel");
    expect(tituloDaRota("/imoveis/a1b2c3d4-e5f6-7890-abcd-ef1234567890")).toBe("Imóvel");
    expect(tituloDaRota("/clientes/abc")).toBe("Clientes");
    expect(tituloDaRota("/clientes/a1b2c3d4-e5f6-7890-abcd-ef1234567890")).toBe("Editar cliente");
    expect(tituloDaRota("/cobrancas")).toBe("Cobrança");
    expect(tituloDaRota("/cobrancas/a1b2c3d4-e5f6-7890-abcd-ef1234567890")).toBe("Detalhe da cobrança");
    expect(tituloDaRota("/inquilinos")).toBe("Inquilinos");
    expect(tituloDaRota("/contratos")).toBe("Contratos");
  });
});
