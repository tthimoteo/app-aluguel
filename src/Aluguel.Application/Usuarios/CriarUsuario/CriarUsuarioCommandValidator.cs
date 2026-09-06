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
                // Limite de usuários do plano do cliente (UC002).
                RuleFor(x => x.ClienteId)
                    .MustAsync((clienteId, ct) => usuarios.PodeAdicionarUsuarioAsync(clienteId, ct))
                    .WithName("Plano")
                    .WithMessage("Limite de usuários do plano atingido.");
            });

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MaximumLength(256)
            .DependentRules(() =>
            {
                RuleFor(x => x.Email)
                    .MustAsync(async (email, ct) => !await usuarios.EmailEmUsoAsync(email.Trim(), null, ct))
                    .WithMessage("Já existe um usuário com este e-mail.");
            });

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(10).WithMessage("A senha deve ter ao menos 10 caracteres.");

        // Este módulo só provisiona perfis de cliente: Gestor ou Analista (§6).
        RuleFor(x => x.Perfil)
            .Must(p => p is PerfilUsuario.Gestor or PerfilUsuario.Analista)
            .WithMessage("Perfil deve ser Gestor ou Analista.");

        RuleFor(x => x.Telefone).MaximumLength(20);

        // CPF é opcional; quando informado deve ser válido e único no cliente (CASO 1).
        When(x => !string.IsNullOrWhiteSpace(x.Cpf), () =>
        {
            RuleFor(x => x.Cpf)
                .Must(cpf => Documento.CpfValido(Documento.SomenteDigitos(cpf)))
                .WithMessage("CPF inválido.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.Cpf)
                        .MustAsync(async (cmd, cpf, _, ct) =>
                            !await usuarios.CpfEmUsoNoClienteAsync(cmd.ClienteId, Documento.SomenteDigitos(cpf)!, null, ct))
                        .WithMessage("Já existe um usuário com este CPF neste cliente.");
                });
        });
    }
}
