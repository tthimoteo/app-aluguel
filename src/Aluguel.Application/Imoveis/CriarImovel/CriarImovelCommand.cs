using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes;
using Aluguel.Domain.Imoveis;
using MediatR;

namespace Aluguel.Application.Imoveis.CriarImovel;

/// <summary>Cadastra um imóvel para o cliente (UC003). O <c>ClienteId</c> já vem resolvido pelo endpoint.</summary>
public sealed record CriarImovelCommand(
    Guid ClienteId,
    string Nome,
    TipoImovel Tipo,
    string? NumeroIptu,
    string? NumeroMatricula,
    EnderecoInput? Endereco) : IRequest<ImovelDto>;

public sealed class CriarImovelCommandHandler(IImovelRepository repositorio, ICurrentTenant tenant)
    : IRequestHandler<CriarImovelCommand, ImovelDto>
{
    public async Task<ImovelDto> Handle(CriarImovelCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenant.TenantId
            ?? throw new InvalidOperationException("Tenant não resolvido no token da requisição.");

        // Revalidação defensiva do limite do plano (a validação de entrada roda no pipeline do MediatR).
        if (!await repositorio.PodeAdicionarImovelAsync(request.ClienteId, cancellationToken))
            throw new InvalidOperationException("Limite de imóveis do plano atingido.");

        var imovel = new Imovel(
            tenantId,
            request.ClienteId,
            request.Nome.Trim(),
            request.Tipo,
            request.Endereco?.ParaValueObject(),
            string.IsNullOrWhiteSpace(request.NumeroIptu) ? null : request.NumeroIptu.Trim(),
            string.IsNullOrWhiteSpace(request.NumeroMatricula) ? null : request.NumeroMatricula.Trim());

        repositorio.Adicionar(imovel);
        await repositorio.SalvarAlteracoesAsync(cancellationToken);

        return imovel.ParaDto();
    }
}
