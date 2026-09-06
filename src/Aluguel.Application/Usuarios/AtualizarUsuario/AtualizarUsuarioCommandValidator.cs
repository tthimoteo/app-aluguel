using Aluguel.Domain.Usuarios;
using FluentValidation;

namespace Aluguel.Application.Usuarios.AtualizarUsuario;

public sealed class AtualizarUsuarioCommandValidator : AbstractValidator<AtualizarUsuarioCommand>
{
    public AtualizarUsuarioCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150);

        RuleFor(x => x.Telefone).MaximumLength(20);

        RuleFor(x => x.Perfil)
            .Must(p => p is PerfilUsuario.Gestor or PerfilUsuario.Analista)
            .WithMessage("Perfil deve ser Gestor ou Analista.");

        RuleFor(x => x.Status).IsInEnum();
    }
}
