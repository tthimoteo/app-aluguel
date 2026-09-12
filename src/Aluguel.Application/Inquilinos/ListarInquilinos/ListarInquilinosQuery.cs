using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using MediatR;

namespace Aluguel.Application.Inquilinos.ListarInquilinos;

/// <summary>Lista inquilinos com filtros (termo/tipo pessoa/status/imóvel) e paginação, escopados ao cliente do usuário.</summary>
public sealed record ListarInquilinosQuery(
    Guid? ClienteId = null,
    string? Termo = null,
    TipoPessoa? TipoPessoa = null,
    StatusAtivoInativo? Status = null,
    Guid? ImovelId = null,
    int Skip = 0,
    int Take = 20) : IRequest<PaginaDto<InquilinoDto>>;

public sealed class ListarInquilinosQueryHandler(
    IInquilinoRepository repositorio,
    ICurrentUser currentUser,
    IAuthService auth)
    : IRequestHandler<ListarInquilinosQuery, PaginaDto<InquilinoDto>>
{
    public async Task<PaginaDto<InquilinoDto>> Handle(ListarInquilinosQuery request, CancellationToken cancellationToken)
    {
        var clienteId = await EscopoVinculo.ResolverListagemAsync(
            currentUser, request.ClienteId, auth, cancellationToken);

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 100);

        var total = await repositorio.ContarAsync(
            clienteId, request.Termo, request.TipoPessoa, request.Status, request.ImovelId, cancellationToken);
        var itens = await repositorio.ListarAsync(
            clienteId, request.Termo, request.TipoPessoa, request.Status, request.ImovelId, skip, take, cancellationToken);

        return new PaginaDto<InquilinoDto>(itens.Select(i => i.ParaDto()).ToList(), total, skip, take);
    }
}
