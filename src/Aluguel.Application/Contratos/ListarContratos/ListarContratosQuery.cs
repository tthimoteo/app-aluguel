using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Contratos;
using MediatR;

namespace Aluguel.Application.Contratos.ListarContratos;

/// <summary>Lista contratos com filtros (imóvel/inquilino/status) e paginação, escopados ao cliente do usuário.</summary>
public sealed record ListarContratosQuery(
    Guid? ClienteId = null,
    Guid? ImovelId = null,
    Guid? InquilinoId = null,
    StatusContrato? Status = null,
    int Skip = 0,
    int Take = 20) : IRequest<PaginaDto<ContratoDto>>;

public sealed class ListarContratosQueryHandler(IContratoRepository repositorio, ICurrentUser currentUser)
    : IRequestHandler<ListarContratosQuery, PaginaDto<ContratoDto>>
{
    public async Task<PaginaDto<ContratoDto>> Handle(ListarContratosQuery request, CancellationToken cancellationToken)
    {
        var clienteId = EscopoCliente.Resolver(currentUser, request.ClienteId);

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 100);

        var total = await repositorio.ContarAsync(clienteId, request.ImovelId, request.InquilinoId, request.Status, cancellationToken);
        var itens = await repositorio.ListarAsync(clienteId, request.ImovelId, request.InquilinoId, request.Status, skip, take, cancellationToken);

        return new PaginaDto<ContratoDto>(itens.Select(c => c.ParaDto()).ToList(), total, skip, take);
    }
}
