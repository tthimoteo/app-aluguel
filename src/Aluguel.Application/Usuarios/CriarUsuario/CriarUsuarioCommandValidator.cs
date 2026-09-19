using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Validacoes;
using Aluguel.Domain.Usuarios;
using FluentValidation;

namespace Aluguel.Application.Usuarios.CriarUsuario;

public sealed class CriarUsuarioCommandValidator : AbstractValidator<CriarUsuarioCommand>
{
    public CriarUsuarioCommandValidator(IUsuarioService usuarios)
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("Cliente é obrigatório.")
            .DependentRules(() =>
            {
                RuleFor(x => x.ClienteId)
                    .MustAsync((clienteId, ct) => usuarios.PodeAdicionarUsuarioAsync(clienteId, ct))
                    .WithName("Plano")
                    .WithMessage("Limite de usuários do plano atingido.");
            });

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150);

        RuleFor(x => x.Perfil)
            .Must(p => p is PerfilUsuario.Gestor or PerfilUsuario.Analista)
            .WithMessage("Perfil deve ser Gestor ou Analista.");

        RuleFor(x => x.Telefone).MaximumLength(20);

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("CPF é obrigatório.")
            .Must(cpf => Documento.CpfValido(Documento.SomenteDigitos(cpf)))
            .WithMessage("CPF inválido.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Cpf)
                    .MustAsync(async (cmd, cpf, _, ct) =>
                        !await usuarios.CpfEmUsoNoClienteAsync(cmd.ClienteId, Documento.SomenteDigitos(cpf)!, null, ct))
                    .WithMessage("Já existe um usuário com este CPF neste cliente.");
            });

        RuleFor(x => x.Email)
            .MustAsync(async (cmd, email, _, ct) =>
            {
                if (await EstaVinculandoAsync(usuarios, cmd, ct))
                    return true;
                if (string.IsNullOrWhiteSpace(email) || email.Length > 256)
                    return false;
                return email.Contains('@');
            })
            .WithMessage("E-mail inválido.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Email)
                    .MustAsync(async (cmd, email, _, ct) =>
                    {
                        if (await EstaVinculandoAsync(usuarios, cmd, ct))
                            return true;
                        return !await usuarios.EmailEmUsoAsync(email.Trim(), null, ct);
                    })
                    .WithMessage("Já existe um usuário com este e-mail.");
            });

        RuleFor(x => x.Senha)
            .MustAsync(async (cmd, senha, _, ct) =>
            {
                if (await EstaVinculandoAsync(usuarios, cmd, ct))
                    return true;
                return !string.IsNullOrWhiteSpace(senha) && senha.Length >= 10;
            })
            .WithMessage("A senha deve ter ao menos 10 caracteres.");
    }

    private static async Task<bool> EstaVinculandoAsync(IUsuarioService usuarios, CriarUsuarioCommand cmd,
        CancellationToken ct)
    {
        var cpf = Documento.SomenteDigitos(cmd.Cpf);
        if (string.IsNullOrEmpty(cpf) || !Documento.CpfValido(cpf))
            return false;
        var existente = await usuarios.ObterPorCpfAsync(cpf, cmd.ClienteId, ct);
        return existente is { JaNoCliente: false };
    }
}
