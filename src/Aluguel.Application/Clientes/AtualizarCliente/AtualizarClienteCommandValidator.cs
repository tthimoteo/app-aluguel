using Aluguel.Application.Common.Validacoes;
using FluentValidation;

namespace Aluguel.Application.Clientes.AtualizarCliente;

/// <summary>
/// Valida formatos na atualização. O tipo de pessoa é imutável, então as obrigatoriedades específicas
/// (Nome para PF / Razão social para PJ) são garantidas pelo agregado de domínio.
/// </summary>
public sealed class AtualizarClienteCommandValidator : AbstractValidator<AtualizarClienteCommand>
{
    public AtualizarClienteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Nome).MaximumLength(150);
        RuleFor(x => x.RazaoSocial).MaximumLength(150);
        RuleFor(x => x.NomeFantasia).MaximumLength(150);
        RuleFor(x => x.InscricaoMunicipal).MaximumLength(30);
        RuleFor(x => x.CnaePrincipal).MaximumLength(10);
        RuleFor(x => x.Telefone).MaximumLength(20);

        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.DataNascimento)
            .Must(d => d is null || d.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Data de nascimento deve estar no passado.");

        RuleFor(x => x.Endereco!.Uf).Length(2).When(x => !string.IsNullOrWhiteSpace(x.Endereco?.Uf));
        RuleFor(x => x.Endereco!.Cep)
            .Must(cep => Documento.SomenteDigitos(cep)!.Length == 8)
            .When(x => !string.IsNullOrWhiteSpace(x.Endereco?.Cep))
            .WithMessage("CEP deve conter 8 dígitos.");
    }
}
