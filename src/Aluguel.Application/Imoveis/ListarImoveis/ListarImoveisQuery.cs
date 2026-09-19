using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Common;
using Aluguel.Domain.Imoveis;
using MediatR;

namespace Aluguel.Application.Imoveis.ListarImoveis;

/// <summary>Lista imóveis com filtros (termo/tipo/status) e paginação, escopados ao cliente do usuário.</summary>
public sealed record ListarImoveisQuery(
    Guid? ClienteId = null,
    string? Termo = null,
    TipoImovel? Tipo = null,
    StatusAtivoInativo? Status = null,
    int Skip = 0,
    int Take = 20) : IRequest<PaginaDto<ImovelDto>>;

public sealed class ListarImoveisQueryHandler(
    IImovelRepository repositorio,
    ICurrentUser currentUser,
    IAuthService auth)
    : IRequestHandler<ListarImoveisQuery, PaginaDto<ImovelDto>>
{
    public async Task<PaginaDto<ImovelDto>> Handle(ListarImoveisQuery request, CancellationToken cancellationToken)
    {
        var clienteId = await EscopoVinculo.ResolverListagemAsync(
            currentUser, request.ClienteId, auth, cancellationToken);

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 100);

        var total = await repositorio.ContarAsync(clienteId, request.Termo, request.Tipo, request.Status, cancellationToken);
        var itens = await repositorio.ListarAsync(clienteId, request.Termo, request.Tipo, request.Status, skip, take, cancellationToken);

        return new PaginaDto<ImovelDto>(itens.Select(i => i.ParaDto()).ToList(), total, skip, take);
    }
}
