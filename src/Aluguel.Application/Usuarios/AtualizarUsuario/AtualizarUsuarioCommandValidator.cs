using Aluguel.Application.Abstractions;
using Aluguel.Domain.Usuarios;
using FluentValidation;

namespace Aluguel.Application.Usuarios.AtualizarUsuario;

public sealed class AtualizarUsuarioCommandValidator : AbstractValidator<AtualizarUsuarioCommand>
{
    public AtualizarUsuarioCommandValidator(IUsuarioService usuarios)
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(256)
            .Must(email => email.Contains('@'))
            .WithMessage("E-mail inválido.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Email)
                    .MustAsync(async (cmd, email, _, ct) =>
                        !await usuarios.EmailEmUsoAsync(email.Trim(), cmd.Id, ct))
                    .WithMessage("Já existe um usuário com este e-mail.");
            });

        RuleFor(x => x.Telefone).MaximumLength(20);

        RuleFor(x => x.Perfil)
            .Must(p => p is PerfilUsuario.Gestor or PerfilUsuario.Analista)
            .WithMessage("Perfil deve ser Gestor ou Analista.");

        RuleFor(x => x.Status).IsInEnum();
    }
}
