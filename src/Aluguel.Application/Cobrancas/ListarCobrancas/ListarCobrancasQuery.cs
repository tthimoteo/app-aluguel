using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Cobrancas.ListarCobrancas;

public sealed record ListarCobrancasQuery(
    Guid? ClienteId,
    Guid? ImovelId,
    string? Competencia,
    int Skip = 0,
    int Take = 20) : IRequest<PaginaDto<CobrancaListItemDto>>;

public sealed class ListarCobrancasQueryHandler(
    ICobrancaRepository cobrancas,
    IClienteRepository clientes,
    IPlanoRepository planos,
    ICurrentUser currentUser)
    : IRequestHandler<ListarCobrancasQuery, PaginaDto<CobrancaListItemDto>>
{
    public async Task<PaginaDto<CobrancaListItemDto>> Handle(ListarCobrancasQuery request, CancellationToken cancellationToken)
    {
        var clienteEscopo = EscopoCliente.Resolver(currentUser, request.ClienteId);
        if (!currentUser.EhAdministrador && clienteEscopo is null)
            return new PaginaDto<CobrancaListItemDto>([], 0, request.Skip, request.Take);

        if (clienteEscopo is { } cid)
        {
            if (!EscopoCliente.PodeAcessar(currentUser, cid))
                return new PaginaDto<CobrancaListItemDto>([], 0, request.Skip, request.Take);

            await AcessoCobranca.GarantirPlanoPermiteAsync(currentUser, clientes, planos, cid, cancellationToken);
        }

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 100);
        var (itens, total) = await cobrancas.ListarAsync(
            clienteEscopo, request.ImovelId, request.Competencia, skip, take, cancellationToken);

        return new PaginaDto<CobrancaListItemDto>(itens, total, skip, take);
    }
}
