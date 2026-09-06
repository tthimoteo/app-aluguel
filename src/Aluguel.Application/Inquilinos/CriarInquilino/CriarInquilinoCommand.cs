using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes;
using Aluguel.Application.Common.Validacoes;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Inquilinos;
using MediatR;

namespace Aluguel.Application.Inquilinos.CriarInquilino;

/// <summary>Cadastra um inquilino (destinatário da NFS-e). O <c>ClienteId</c> já vem resolvido pelo endpoint.</summary>
public sealed record CriarInquilinoCommand(
    Guid ClienteId,
    TipoPessoa TipoPessoa,
    string Nome,
    string Documento,
    string? InscricaoMunicipal,
    string? Telefone,
    string? Email,
    EnderecoInput? Endereco) : IRequest<InquilinoDto>;

public sealed class CriarInquilinoCommandHandler(IInquilinoRepository repositorio, ICurrentTenant tenant)
    : IRequestHandler<CriarInquilinoCommand, InquilinoDto>
{
    public async Task<InquilinoDto> Handle(CriarInquilinoCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenant.TenantId
            ?? throw new InvalidOperationException("Tenant não resolvido no token da requisição.");

        var inquilino = new Inquilino(
            tenantId,
            request.ClienteId,
            request.TipoPessoa,
            request.Nome.Trim(),
            Documento.SomenteDigitos(request.Documento)!,
            request.Telefone,
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            request.Endereco?.ParaValueObject(),
            string.IsNullOrWhiteSpace(request.InscricaoMunicipal) ? null : request.InscricaoMunicipal.Trim());

        repositorio.Adicionar(inquilino);
        await repositorio.SalvarAlteracoesAsync(cancellationToken);

        return inquilino.ParaDto();
    }
}
