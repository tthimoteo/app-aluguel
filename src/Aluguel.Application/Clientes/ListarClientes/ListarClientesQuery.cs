using Aluguel.Application.Abstractions;
using Aluguel.Domain.Clientes;
using MediatR;

namespace Aluguel.Application.Clientes.ListarClientes;

/// <summary>Lista clientes do tenant com filtro opcional (termo em nome/CPF/CNPJ) e paginação.</summary>
public sealed record ListarClientesQuery(
    string? Termo = null,
    TipoPessoa? TipoPessoa = null,
    int Skip = 0,
    int Take = 20) : IRequest<PaginaDto<ClienteDto>>;

public sealed class ListarClientesQueryHandler(IClienteRepository repositorio)
    : IRequestHandler<ListarClientesQuery, PaginaDto<ClienteDto>>
{
    public async Task<PaginaDto<ClienteDto>> Handle(ListarClientesQuery request, CancellationToken cancellationToken)
    {
        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 100);

        var total = await repositorio.ContarAsync(request.Termo, request.TipoPessoa, cancellationToken);
        var clientes = await repositorio.ListarAsync(request.Termo, request.TipoPessoa, skip, take, cancellationToken);

        return new PaginaDto<ClienteDto>(clientes.Select(c => c.ParaDto()).ToList(), total, skip, take);
    }
}
