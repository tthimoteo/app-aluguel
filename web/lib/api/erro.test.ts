import { describe, expect, it } from "vitest";
import { mensagemApiErro } from "./erro";

describe("mensagemApiErro", () => {
  it("usa detail de ProblemDetails", () => {
    expect(mensagemApiErro({ detail: "Limite de usuários do plano atingido." })).toBe(
      "Limite de usuários do plano atingido.",
    );
  });

  it("concatena errors de ValidationProblem", () => {
    expect(
      mensagemApiErro({
        title: "Falha de validação",
        errors: { Email: ["Já existe um usuário com este e-mail."] },
      }),
    ).toBe("Já existe um usuário com este e-mail.");
  });

  it("cai no fallback quando o corpo é vazio", () => {
    expect(mensagemApiErro(null, "Falha")).toBe("Falha");
  });
});
