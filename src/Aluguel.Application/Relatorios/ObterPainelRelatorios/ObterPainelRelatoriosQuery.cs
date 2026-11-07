using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Relatorios.ObterPainelRelatorios;

public sealed record ObterPainelRelatoriosQuery(
    int? Ano = null,
    int? Mes = null,
    DateOnly? DataInicio = null,
    DateOnly? DataFim = null,
    Guid? ClienteId = null,
    Guid? ImovelId = null,
    int? DiasAtrasoMinimos = null) : IRequest<RelatorioPainelDto>;

public sealed class ObterPainelRelatoriosQueryHandler(
    IRelatorioRepository relatorios,
    ICurrentUser currentUser) : IRequestHandler<ObterPainelRelatoriosQuery, RelatorioPainelDto>
{
    public Task<RelatorioPainelDto> Handle(ObterPainelRelatoriosQuery request, CancellationToken cancellationToken)
    {
        var clienteId = EscopoCliente.Resolver(currentUser, request.ClienteId);
        if (!currentUser.EhAdministrador && clienteId is null)
            throw new InvalidOperationException("Selecione um cliente no contexto da sessão.");

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        return relatorios.ObterPainelAsync(
            clienteId,
            request.ImovelId,
            request.Ano,
            request.Mes,
            request.DataInicio,
            request.DataFim,
            request.DiasAtrasoMinimos,
            hoje,
            cancellationToken);
    }
}
